using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Todo el movimiento del jugador en un solo script:
/// control con A/D o flechas, viento del nivel, cambio de gravedad, límite de velocidad, reinicio
/// y lectura de velocidad para la UI.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Viento del nivel")]
    [Tooltip("Empuje horizontal constante (unidades/seg²). Positivo = hacia la derecha (a favor), negativo = hacia la izquierda (en contra), 0 = sin viento.")]
    public float windForce = 0f;
    [Tooltip("Velocidad máxima a la que el viento por sí solo puede llevarte en su dirección (las pendientes pueden superarla).")]
    public float windTopSpeed = 10f;

    [Header("Control")]
    [Tooltip("Velocidad máxima que se alcanza solo con las teclas (las pendientes pueden superarla).")]
    public float moveSpeed = 5f;
    [Tooltip("Qué tan rápido gana velocidad en la dirección presionada.")]
    public float acceleration = 30f;
    [Tooltip("Qué tan rápido frena al presionar la dirección contraria.")]
    public float deceleration = 15f;

    [Header("Límite de velocidad")]
    [Tooltip("Velocidad máxima total (unidades/seg). 0 = sin límite.")]
    public float maxSpeed = 0f;

    [Header("Gravedad")]
    [Tooltip("Fuerza de la gravedad (multiplica la gravedad global de Physics 2D).")]
    public float gravityMagnitude = 5f;
    [Tooltip("Empezar con la gravedad hacia arriba.")]
    public bool startInverted = false;

    [Header("Controles")]
    public Key gravitySwitchKey = Key.Space;
    public Key leftKey = Key.A;
    public Key rightKey = Key.D;
    [Tooltip("Las flechas izquierda/derecha también mueven.")]
    public bool allowArrowKeys = true;
    public Key resetKey = Key.R;

    [Header("Reinicio")]
    [Tooltip("Al reiniciar vuelve a la posición donde empezó la partida. Si está apagado usa resetPosition.")]
    public bool resetToStartPosition = true;
    public Vector2 resetPosition = Vector2.zero;

    [Header("Meta")]
    [Tooltip("Qué tan rápido frena el jugador al cruzar la meta (sin viento ni controles).")]
    public float finishBrake = 25f;

    [Header("Física")]
    [Tooltip("Detección continua: evita que el jugador atraviese o se hunda en el suelo a alta velocidad.")]
    public bool continuousCollision = true;

    // ---- Lectura de velocidad (para UI, cámara, etc.) ----
    public float CurrentSpeed => rb != null ? rb.linearVelocity.magnitude : 0f;
    public float HorizontalSpeed => rb != null ? rb.linearVelocity.x : 0f;
    public float VerticalSpeed => rb != null ? rb.linearVelocity.y : 0f;
    public bool IsGravityInverted => gravityInverted;
    /// <summary>Falso mientras se muestra la introducción / cuenta regresiva: ninguna tecla hace nada.</summary>
    public bool InputLocked { get; private set; }
    /// <summary>Verdadero después de cruzar la meta: sin viento ni controles, solo frena.</summary>
    public bool IsStoppedAtFinish { get; private set; }

    private Rigidbody2D rb;
    private Vector2 startPosition;
    private bool gravityInverted;
    private float inputX;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = rb.position;
        gravityInverted = startInverted;
        ApplyPhysicsSettings();
        ApplyGravity();
    }

    // Permite cambiar valores en el Inspector mientras juegas y verlos al instante
    void OnValidate()
    {
        gravityMagnitude = Mathf.Abs(gravityMagnitude);
        if (rb != null && Application.isPlaying)
        {
            ApplyPhysicsSettings();
            ApplyGravity();
        }
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        inputX = 0f;
        if (kb == null || InputLocked) return;

        if (kb[resetKey].wasPressedThisFrame)
            ResetPlayer();

        if (IsStoppedAtFinish) return;

        if (kb[gravitySwitchKey].wasPressedThisFrame)
            FlipGravity();

        float h = 0f;
        if (kb[leftKey].isPressed || (allowArrowKeys && kb.leftArrowKey.isPressed)) h -= 1f;
        if (kb[rightKey].isPressed || (allowArrowKeys && kb.rightArrowKey.isPressed)) h += 1f;
        inputX = h;
    }

    void FixedUpdate()
    {
        float vx = rb.linearVelocity.x;

        // Después de la meta: sin viento ni controles, solo frenar
        if (IsStoppedAtFinish)
        {
            vx = Mathf.MoveTowards(vx, 0f, finishBrake * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
            return;
        }

        if (Mathf.Abs(inputX) > 0.01f)
        {
            // Sin teclas no se toca la velocidad: la física conserva el impulso.
            float target = inputX * moveSpeed;
            bool sameDirection = Mathf.Sign(vx) == Mathf.Sign(target) || Mathf.Approximately(vx, 0f);

            // Si ya vas más rápido que moveSpeed en esa dirección (por una pendiente o el viento), no te frena.
            if (!(sameDirection && Mathf.Abs(vx) >= moveSpeed))
            {
                float rate = sameDirection ? acceleration : deceleration;
                vx = Mathf.MoveTowards(vx, target, rate * Time.fixedDeltaTime);
            }
        }

        // El viento empuja siempre, se presionen teclas o no, hasta windTopSpeed en su dirección.
        if (windForce != 0f && vx * Mathf.Sign(windForce) < windTopSpeed)
            vx += windForce * Time.fixedDeltaTime;

        Vector2 velocity = new Vector2(vx, rb.linearVelocity.y);

        if (maxSpeed > 0f && velocity.magnitude > maxSpeed)
            velocity = velocity.normalized * maxSpeed;

        rb.linearVelocity = velocity;
    }

    /// <summary>Se llama cada vez que se cambia la gravedad con la tecla (para efectos, sonidos, etc.).</summary>
    public event System.Action<bool> GravityFlipped;

    public void FlipGravity()
    {
        gravityInverted = !gravityInverted;
        ApplyGravity();
        GravityFlipped?.Invoke(gravityInverted);
    }

    /// <summary>Se llama cuando el jugador se reinicia (tecla R).</summary>
    public event System.Action PlayerReset;

    /// <summary>Bloquea / desbloquea todas las teclas (introducción y cuenta regresiva).</summary>
    public void SetInputLocked(bool locked)
    {
        InputLocked = locked;
        if (locked) inputX = 0f;
    }

    /// <summary>Al cruzar la meta: quita viento y controles y frena hasta detenerse.</summary>
    public void StopAtFinish()
    {
        IsStoppedAtFinish = true;
        inputX = 0f;
    }

    public void ResetPlayer()
    {
        IsStoppedAtFinish = false;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        Vector2 target = resetToStartPosition ? startPosition : resetPosition;
        rb.position = target;
        transform.position = new Vector3(target.x, target.y, transform.position.z);

        gravityInverted = startInverted;
        ApplyGravity();
        PlayerReset?.Invoke();
    }

    void ApplyGravity()
    {
        rb.gravityScale = gravityInverted ? -gravityMagnitude : gravityMagnitude;
    }

    void ApplyPhysicsSettings()
    {
        rb.collisionDetectionMode = continuousCollision
            ? CollisionDetectionMode2D.Continuous
            : CollisionDetectionMode2D.Discrete;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }
}
