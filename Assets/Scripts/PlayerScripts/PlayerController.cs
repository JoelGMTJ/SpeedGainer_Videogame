using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;

    // Qué tan rápido gana velocidad (aceleración) y qué tan rápido la pierde al soltar o cambiar de dirección
    public float acceleration = 20f;
    public float deceleration = 25f;

    // Magnitud fija de la gravedad
    public float gravityMagnitude = 5f;

    // Dirección actual de la gravedad (+1 o -1)
    public float gravity = 1f;

    // Tecla para cambiar la gravedad
    public Key gravitySwitchKey = Key.Space;

    private Rigidbody2D rb;
    private float targetX;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        gravityMagnitude = Mathf.Abs(gravityMagnitude);
        gravity = gravity >= 0f ? gravityMagnitude : -gravityMagnitude;
        rb.gravityScale = gravity;
    }

    public void ResetGravity()
    {
        gravity = Mathf.Abs(gravityMagnitude);
        rb.gravityScale = gravity;
    }

    void Update()
    {
        float h = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) h -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) h += 1f;

            if (Keyboard.current[gravitySwitchKey].wasPressedThisFrame)
            {
                gravity = -Mathf.Sign(gravity) * gravityMagnitude;
                rb.gravityScale = gravity;
            }
        }

        // Velocidad objetivo según la tecla presionada
        targetX = h * moveSpeed;
    }

    void FixedUpdate()
    {
        float currentX = rb.linearVelocity.x;

        // Sin input: no tocamos la velocidad. La física conserva el momentum.
        if (Mathf.Abs(targetX) < 0.01f) return;

        bool sameDirection = Mathf.Sign(currentX) == Mathf.Sign(targetX);

        // Si ya vas más rápido que moveSpeed en la misma dirección (por una pendiente, por ejemplo),
        // no te frenamos. Solo aceleramos si vas más lento o si vas en sentido contrario.
        if (sameDirection && Mathf.Abs(currentX) >= moveSpeed) return;

        float newX = Mathf.MoveTowards(currentX, targetX, acceleration * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
    }
}