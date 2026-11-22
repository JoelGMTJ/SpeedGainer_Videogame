using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class AutoRunning : MonoBehaviour
{
    [Tooltip("Velocidad horizontal constante hacia la derecha")]
    public float forwardSpeed = 8f;

    [Tooltip("Qué tan rápido alcanza forwardSpeed al arrancar (ventilador)")]
    public float acceleration = 10f;

    [Tooltip("Magnitud de la gravedad")]
    public float gravityMagnitude = 5f;

    [Tooltip("Dirección actual de la gravedad (+1 o -1)")]
    public float gravity = 1f;

    [Tooltip("Tecla para cambiar la gravedad")]
    public Key gravitySwitchKey = Key.Space;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        gravityMagnitude = Mathf.Abs(gravityMagnitude);
        gravity = gravity >= 0f ? gravityMagnitude : -gravityMagnitude;
        rb.gravityScale = gravity;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current[gravitySwitchKey].wasPressedThisFrame)
        {
            gravity = -Mathf.Sign(gravity) * gravityMagnitude;
            rb.gravityScale = gravity;
        }
    }

    void FixedUpdate()
    {
        // Empuje constante hacia la derecha. Solo acelera si vas más lento que forwardSpeed,
        // así una pendiente puede seguir acelerándote por encima de ese valor.
        float currentX = rb.linearVelocity.x;

        if (currentX < forwardSpeed)
        {
            float newX = Mathf.MoveTowards(currentX, forwardSpeed, acceleration * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
        }
    }

    public void ResetGravity()
    {
        gravity = gravityMagnitude;
        rb.gravityScale = gravity;
    }
}