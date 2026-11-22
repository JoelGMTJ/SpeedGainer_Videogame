using System.Collections;
using UnityEngine;

/// <summary>
/// Anillo de impulso: si el jugador pasa a través de él, gana un poco de velocidad
/// en la dirección en la que ya se está moviendo (funciona con gravedad invertida y hacia la izquierda).
///
/// El aro tiene dos mitades: "Back" (se dibuja detrás del jugador) y "Front" (delante),
/// así el astronauta se ve pasando a través. La animación de giro (como un frisbee) está en el
/// Animator del objeto raíz y cambia los sprites de las dos mitades (ventana Animation para editarla).
/// Necesita un Collider2D marcado como "Is Trigger" (el prefab trae un BoxCollider2D en el hueco).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BoostRing : MonoBehaviour
{
    [Header("Impulso")]
    [Tooltip("Velocidad que se suma (unidades/seg) en la dirección del movimiento.")]
    public float boostAmount = 4f;
    [Tooltip("Si el jugador va más lento que esto, el impulso se aplica hacia la derecha.")]
    public float minSpeedForDirection = 0.5f;

    [Header("Uso")]
    [Tooltip("Segundos antes de que el mismo anillo pueda volver a dar impulso.")]
    public float cooldown = 1f;
    [Tooltip("Desaparece después de usarse una vez.")]
    public bool singleUse = false;

    [Header("Efecto visual")]
    [Tooltip("Cuánto crece el anillo al usarse (1 = nada).")]
    public float popScale = 1.35f;
    [Tooltip("Duración del efecto al usarse.")]
    public float popDuration = 0.25f;
    [Tooltip("Transparencia del anillo mientras está en cooldown.")]
    [Range(0f, 1f)] public float usedAlpha = 0.4f;

    private float readyTime;
    private SpriteRenderer[] renderers;
    private Vector3 baseScale;

    void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        baseScale = transform.localScale;
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnEnable()
    {
        readyTime = 0f;
        transform.localScale = baseScale == Vector3.zero ? Vector3.one : baseScale;
        SetAlpha(1f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (Time.time < readyTime) return;

        Rigidbody2D rb = other.attachedRigidbody;
        if (rb == null || rb.GetComponent<PlayerMovement>() == null) return;

        Vector2 v = rb.linearVelocity;
        Vector2 dir = v.magnitude >= minSpeedForDirection ? v.normalized : Vector2.right;
        rb.linearVelocity = v + dir * boostAmount;

        readyTime = Time.time + cooldown;
        StopAllCoroutines();
        StartCoroutine(Pop());
    }

    IEnumerator Pop()
    {
        float t = 0f;
        while (t < popDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / popDuration);
            // crece rápido y regresa
            float s = Mathf.Lerp(popScale, 1f, k * k);
            transform.localScale = baseScale * s;
            SetAlpha(Mathf.Lerp(1f, singleUse ? 0f : usedAlpha, k));
            yield return null;
        }
        transform.localScale = baseScale;

        if (singleUse)
        {
            gameObject.SetActive(false);
            yield break;
        }

        // esperar el resto del cooldown y volver a la normalidad
        float wait = readyTime - Time.time;
        if (wait > 0f) yield return new WaitForSeconds(wait);
        SetAlpha(1f);
    }

    void SetAlpha(float a)
    {
        if (renderers == null) return;
        foreach (SpriteRenderer sr in renderers)
        {
            Color c = sr.color;
            c.a = a;
            sr.color = c;
        }
    }
}
