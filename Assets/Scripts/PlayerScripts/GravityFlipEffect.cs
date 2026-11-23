using UnityEngine;

/// <summary>
/// Efecto de partículas alrededor del astronauta cada vez que cambia la gravedad (tecla Espacio).
/// Va en el mismo objeto que PlayerMovement (Ball). El efecto sigue al jugador mientras dura.
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
public class GravityFlipEffect : MonoBehaviour
{
    [Tooltip("Prefab del efecto (CFXR4 Sword Trail PLAIN (360 Thin Spiral)).")]
    public ParticleSystem effectPrefab;

    [Tooltip("Tamaño del efecto en el mundo (1 = 4 unidades de ancho, el tamaño original).")]
    public float effectSize = 0.75f;

    [Tooltip("Orden de dibujado: mayor que el del astronauta (0) para que se vea delante.")]
    public int sortingOrder = 1;

    [Tooltip("Voltear el giro del efecto cuando la gravedad queda invertida.")]
    public bool mirrorWhenInverted = true;

    private PlayerMovement movement;

    void Awake() => movement = GetComponent<PlayerMovement>();
    void OnEnable() { if (movement != null) movement.GravityFlipped += Play; }
    void OnDisable() { if (movement != null) movement.GravityFlipped -= Play; }

    void Play(bool inverted)
    {
        if (effectPrefab == null) return;

        ParticleSystem fx = Instantiate(effectPrefab, transform);
        fx.transform.localPosition = Vector3.zero;
        fx.transform.localRotation = Quaternion.identity;

        // Compensar la escala del jugador para que el tamaño final sea effectSize
        Vector3 s = transform.lossyScale;
        float flip = mirrorWhenInverted && inverted ? -1f : 1f;
        fx.transform.localScale = new Vector3(effectSize / s.x, flip * effectSize / s.y, 1f);

        foreach (ParticleSystemRenderer r in fx.GetComponentsInChildren<ParticleSystemRenderer>())
            r.sortingOrder = sortingOrder;

        var main = fx.main;
        Destroy(fx.gameObject, main.duration + main.startLifetime.constantMax + 0.2f);
        fx.Play(true);
    }
}
