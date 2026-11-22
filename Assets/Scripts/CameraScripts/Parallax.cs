using UnityEngine;

/// <summary>
/// Capa de fondo con efecto parallax.
/// - Se escala para cubrir siempre toda la pantalla (aunque la cámara haga zoom).
/// - Se repite horizontalmente con copias a izquierda y derecha (fondo infinito sin huecos).
/// - Se desplaza más lento que el mundo según parallaxFactor.
/// Se ejecuta después de la cámara (FollowPlayer) para evitar temblores.
/// </summary>
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(SpriteRenderer))]
public class Parallax : MonoBehaviour
{
    [Tooltip("Cámara que sigue al jugador. Si lo dejas vacío, usa la cámara principal.")]
    public Camera cam;

    [Tooltip("1 = muy lejana (no se mueve en pantalla, ej. cielo). 0 = se mueve igual que el mundo. " +
             "Las capas más cercanas deben tener valores más bajos.")]
    [Range(0f, 1f)]
    public float parallaxFactor = 0.5f;

    [Tooltip("Repetir la capa horizontalmente para un fondo infinito.")]
    public bool infiniteHorizontal = true;

    [Tooltip("Escalar la capa para que siempre cubra toda la pantalla, incluso con zoom.")]
    public bool fillScreen = true;

    [Tooltip("Movimiento automático (anchos de imagen por segundo). Útil para nubes que se mueven solas.")]
    public float autoScrollSpeed = 0f;

    [Tooltip("Desplazamiento vertical como fracción de la altura de la pantalla.")]
    public float verticalOffset = 0f;

    private SpriteRenderer sr;
    private Vector2 spriteSize;   // tamaño del sprite sin escala
    private float phase;          // posición dentro de una repetición (0..1)
    private float lastCamX;

    void Start()
    {
        if (cam == null)
            cam = Camera.main;

        sr = GetComponent<SpriteRenderer>();
        spriteSize = sr.sprite.bounds.size;
        lastCamX = cam.transform.position.x;

        if (infiniteHorizontal)
        {
            CreateCopy(-1);
            CreateCopy(1);
        }
    }

    void CreateCopy(int side)
    {
        GameObject copy = new GameObject(name + (side < 0 ? "_Left" : "_Right"));
        copy.transform.SetParent(transform, false);
        copy.transform.localPosition = new Vector3(side * spriteSize.x, 0f, 0f);

        SpriteRenderer copySr = copy.AddComponent<SpriteRenderer>();
        copySr.sprite = sr.sprite;
        copySr.color = sr.color;
        copySr.sharedMaterial = sr.sharedMaterial;
        copySr.sortingLayerID = sr.sortingLayerID;
        copySr.sortingOrder = sr.sortingOrder;
        copySr.flipX = sr.flipX;
        copySr.flipY = sr.flipY;
    }

    void LateUpdate()
    {
        Transform camT = cam.transform;
        float viewHeight = cam.orthographicSize * 2f;
        float viewWidth = viewHeight * cam.aspect;

        // Escala para cubrir la pantalla (como "cover" en CSS)
        if (fillScreen)
        {
            float scale = Mathf.Max(viewHeight / spriteSize.y, viewWidth / spriteSize.x);
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        float tileWidth = spriteSize.x * transform.localScale.x;

        // Avance de la cámara este frame, convertido a "repeticiones" de la imagen.
        // Se acumula de forma incremental para que el zoom no haga saltar el fondo.
        float camX = camT.position.x;
        float deltaX = camX - lastCamX;
        lastCamX = camX;

        phase += deltaX * (1f - parallaxFactor) / tileWidth + autoScrollSpeed * Time.deltaTime;

        if (infiniteHorizontal)
            phase -= Mathf.Floor(phase); // mantener entre 0 y 1

        transform.position = new Vector3(
            camX - phase * tileWidth,
            camT.position.y + verticalOffset * viewHeight,
            transform.position.z
        );
    }
}
