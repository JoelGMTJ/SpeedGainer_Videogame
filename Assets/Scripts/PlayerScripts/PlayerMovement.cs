using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Todo el movimiento del jugador en un solo script:
/// auto-correr / control manual, cambio de gravedad, límite de velocidad, reinicio
/// y lectura de velocidad para la UI.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    public enum MoveMode
    {
        AutoRun, // corre solo hacia la derecha
        Manual   // se mueve con A/D o flechas
    }

    [Header("Modo de movimiento")]
    [Tooltip("AutoRun: corre solo hacia la derecha. Manual: se controla con A/D o flechas.")]
    public MoveMode mode = MoveMode.AutoRun;

    [Header("Auto-correr (modo AutoRun)")]
    [Tooltip("Velocidad hacia la derecha que intenta mantener. Una pendiente puede hacerte ir más rápido que esto.")]
    public float forwardSpeed = 8f;
    [Tooltip("Qué tan rápido llega a forwardSpeed.")]
    public float forwardAcceleration = 10f;

    [Header("Control manual (modo Manual)")]
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
    [Tooltip("Las flechas izquierda/derecha también mueven en modo Manual.")]
    public bool allowArrowKeys = true;
    public Key resetKey = Key.R;

    [Header("Reinicio")]
    [Tooltip("Al reiniciar vuelve a la posición donde empezó la partida. Si está apagado usa resetPosition.")]
    public bool resetToStartPosition = true;
    public Vector2 resetPosition = Vector2.zero;

    [Header("Física")]
    [Tooltip("Detección continua: evita que el jugador atraviese o se hunda en el suelo a alta velocidad.")]
    public bool continuousCollision = true;

    // ---- Lectura de velocidad (para UI, cámara, etc.) ----
    public float CurrentSpeed => rb != null ? rb.linearVelocity.magnitude : 0f;
    public float HorizontalSpeed => rb != null ? rb.linearVelocity.x : 0f;
    public float VerticalSpeed => rb != null ? rb.linearVelocity.y : 0f;
    public bool IsGravityInverted => gravityInverted;

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
        if (kb == null) return;

        if (kb[gravitySwitchKey].wasPressedThisFrame)
            FlipGravity();

        if (kb[resetKey].wasPressedThisFrame)
            ResetPlayer();

        float h = 0f;
        if (kb[leftKey].isPressed || (allowArrowKeys && kb.leftArrowKey.isPressed)) h -= 1f;
        if (kb[rightKey].isPressed || (allowArrowKeys && kb.rightArrowKey.isPressed)) h += 1f;
        inputX = h;
    }

    void FixedUpdate()
    {
        float vx = rb.linearVelocity.x;

        if (mode == MoveMode.AutoRun)
        {
            // Solo empuja si va más lento que forwardSpeed: así una bajada puede acelerarte más.
            if (vx < forwardSpeed)
                vx = Mathf.MoveTowards(vx, forwardSpeed, forwardAcceleration * Time.fixedDeltaTime);
        }
        else if (Mathf.Abs(inputX) > 0.01f)
        {
            // Sin teclas no se toca la velocidad: la física conserva el impulso.
            float target = inputX * moveSpeed;
            bool sameDirection = Mathf.Sign(vx) == Mathf.Sign(target) || Mathf.Approximately(vx, 0f);

            // Si ya vas más rápido que moveSpeed en esa dirección (por una pendiente), no te frena.
            if (!(sameDirection && Mathf.Abs(vx) >= moveSpeed))
            {
                float rate = sameDirection ? acceleration : deceleration;
                vx = Mathf.MoveTowards(vx, target, rate * Time.fixedDeltaTime);
            }
        }

        Vector2 velocity = new Vector2(vx, rb.linearVelocity.y);

        if (maxSpeed > 0f && velocity.magnitude > maxSpeed)
            velocity = velocity.normalized * maxSpeed;

        rb.linearVelocity = velocity;
    }

    public void FlipGravity()
    {
        gravityInverted = !gravityInverted;
        ApplyGravity();
    }

    public void ResetPlayer()
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        Vector2 target = resetToStartPosition ? startPosition : resetPosition;
        rb.position = target;
        transform.position = new Vector3(target.x, target.y, transform.position.z);

        gravityInverted = startInverted;
        ApplyGravity();
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
