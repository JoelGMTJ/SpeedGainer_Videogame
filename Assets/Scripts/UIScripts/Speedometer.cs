using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SpeedReader : MonoBehaviour
{
    private Rigidbody2D rb;

    // Velocidad actual en unidades por segundo
    public float CurrentSpeed { get; private set; }

    // Velocidad horizontal y vertical por separado
    public float HorizontalSpeed => rb.linearVelocity.x;
    public float VerticalSpeed => rb.linearVelocity.y;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        // Magnitud del vector de velocidad (qué tan rápido se mueve, sin importar la dirección)
        CurrentSpeed = rb.linearVelocity.magnitude;
    }
}