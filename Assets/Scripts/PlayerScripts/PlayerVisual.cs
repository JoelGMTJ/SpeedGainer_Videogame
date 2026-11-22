using UnityEngine;

/// <summary>
/// Dibuja al astronauta encima de la pelota del jugador.
/// Va en un objeto hijo de la pelota: la pelota sigue rodando (física), pero el sprite
/// se queda derecho, mira hacia donde se mueve y se voltea con la gravedad.
///
/// Las animaciones (Idle / Run) están en el Animator (ventana Animation para editarlas).
/// Este script solo le manda al Animator:
///   - "Speed": velocidad horizontal (sin signo). Arriba de runThreshold pasa a Run.
///   - "RunSpeed": multiplicador de la velocidad de la animación Run (corre más rápido mientras más rápido va).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class PlayerVisual : MonoBehaviour
{
    [Tooltip("Script de movimiento del jugador (si está vacío se busca en el padre).")]
    public PlayerMovement player;

    [Tooltip("Velocidad horizontal mínima para voltear el sprite hacia donde se mueve.")]
    public float turnThreshold = 0.3f;

    [Header("Velocidad de la animación Run")]
    [Tooltip("Velocidad del jugador a la que la animación Run se reproduce a velocidad normal (1x).")]
    public float normalRunSpeed = 8f;
    [Tooltip("Multiplicador mínimo / máximo de la animación Run.")]
    public float minRunAnimSpeed = 0.5f;
    public float maxRunAnimSpeed = 2f;

    static readonly int SpeedParam = Animator.StringToHash("Speed");
    static readonly int RunSpeedParam = Animator.StringToHash("RunSpeed");

    private SpriteRenderer sr;
    private Animator anim;
    private bool facingLeft;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        if (player == null) player = GetComponentInParent<PlayerMovement>();
    }

    void LateUpdate()
    {
        // Siempre derecho aunque la pelota ruede
        transform.rotation = Quaternion.identity;

        if (player == null) return;

        float vx = player.HorizontalSpeed;
        float speed = Mathf.Abs(vx);
        if (speed > turnThreshold) facingLeft = vx < 0f;

        sr.flipX = facingLeft;
        sr.flipY = player.IsGravityInverted; // los pies apuntan al "piso" actual

        anim.SetFloat(SpeedParam, speed);
        float runAnimSpeed = normalRunSpeed > 0f ? speed / normalRunSpeed : 1f;
        anim.SetFloat(RunSpeedParam, Mathf.Clamp(runAnimSpeed, minRunAnimSpeed, maxRunAnimSpeed));
    }
}
