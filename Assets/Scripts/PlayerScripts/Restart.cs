using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerController))]
public class PlayerReset : MonoBehaviour
{
    [Tooltip("Tecla para reiniciar la posición y la gravedad")]
    public Key resetKey = Key.R;

    private Rigidbody2D rb;
    private PlayerController controller;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controller = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current[resetKey].wasPressedThisFrame)
        {
            ResetPlayer();
        }
    }

    void ResetPlayer()
    {
        // Quita la velocidad acumulada
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        // Mueve al jugador a (0, 0, 0)
        transform.position = Vector3.zero;

        // Regresa la gravedad a su dirección original (positiva)
        controller.ResetGravity();
    }
}