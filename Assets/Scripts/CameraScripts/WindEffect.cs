using UnityEngine;

/// <summary>
/// Efecto visual de viento: líneas blancas (wind_streak.png) que cruzan la pantalla en la dirección del viento.
/// Las líneas se mueven respecto a la cámara, así siempre se ve el viento aunque el jugador vaya rápido.
/// La dirección y la intensidad salen del windForce del jugador (PlayerMovement), o se fijan a mano.
/// </summary>
[DefaultExecutionOrder(110)] // después de la cámara (FollowPlayer) y del fondo (Parallax)
public class WindEffect : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Imagen de la línea de viento (Assets/Art/Wind/wind_streak).")]
    public Sprite streakSprite;
    [Tooltip("Cámara. Vacío = la cámara principal.")]
    public Camera cam;
    [Tooltip("Jugador del que se lee el viento. Vacío = se busca solo.")]
    public PlayerMovement windSource;

    [Header("Dirección")]
    [Tooltip("Usar la dirección del windForce del jugador (+ derecha, - izquierda, 0 = sin efecto).")]
    public bool useWindSourceDirection = true;
    [Tooltip("Dirección si no se usa la del jugador: 1 = derecha, -1 = izquierda.")]
    public float direction = 1f;

    [Header("Líneas")]
    [Tooltip("Cuántas líneas hay en pantalla a la vez.")]
    [Min(1)] public int streakCount = 28;
    [Tooltip("Velocidad de las líneas por la pantalla (unidades/seg), mínima y máxima.")]
    public Vector2 speedRange = new Vector2(30f, 55f);
    [Tooltip("Largo (escala X) mínimo y máximo de cada línea.")]
    public Vector2 lengthRange = new Vector2(0.6f, 1.8f);
    [Tooltip("Transparencia mínima y máxima de cada línea.")]
    public Vector2 alphaRange = new Vector2(0.15f, 0.45f);
    [Tooltip("Ondulación vertical suave de las líneas (0 = rectas).")]
    public float wobble = 0.25f;
    public Color color = Color.white;

    [Header("Dibujo")]
    [Tooltip("Orden de dibujado: 10 = delante de todo (como una capa de clima).")]
    public int sortingOrder = 10;

    private class Streak
    {
        public Transform t;
        public SpriteRenderer sr;
        public Vector2 offset; // posición relativa al centro de la cámara
        public float speed;
        public float phase;
    }

    private Streak[] streaks;
    private float halfW, halfH;

    void Start()
    {
        if (cam == null) cam = Camera.main;
        if (windSource == null) windSource = FindAnyObjectByType<PlayerMovement>();
        if (cam == null || streakSprite == null)
        {
            Debug.LogWarning("WindEffect: falta la cámara o el sprite de la línea.", this);
            enabled = false;
            return;
        }

        UpdateViewSize();
        streaks = new Streak[streakCount];
        for (int i = 0; i < streakCount; i++)
        {
            var go = new GameObject("WindStreak");
            go.transform.SetParent(transform, false);
            var s = new Streak { t = go.transform, sr = go.AddComponent<SpriteRenderer>() };
            s.sr.sprite = streakSprite;
            s.sr.sortingOrder = sortingOrder;
            Respawn(s, true);
            streaks[i] = s;
        }
    }

    float CurrentDirection()
    {
        if (useWindSourceDirection && windSource != null)
            return Mathf.Sign(windSource.windForce) * (Mathf.Abs(windSource.windForce) > 0.01f ? 1f : 0f);
        return Mathf.Sign(direction);
    }

    void UpdateViewSize()
    {
        halfH = cam.orthographicSize;
        halfW = halfH * cam.aspect;
    }

    // Coloca una línea al inicio (lado contrario al viento) o en cualquier parte de la pantalla.
    void Respawn(Streak s, bool anywhere)
    {
        float dir = CurrentDirection();
        if (dir == 0f) dir = 1f;
        float len = Random.Range(lengthRange.x, lengthRange.y);
        float spriteW = streakSprite.bounds.size.x * len;

        s.speed = Random.Range(speedRange.x, speedRange.y);
        s.phase = Random.value * Mathf.PI * 2f;
        s.t.localScale = new Vector3(len, 1f, 1f);

        float x = anywhere ? Random.Range(-halfW, halfW) : -dir * (halfW + spriteW);
        float y = Random.Range(-halfH * 0.95f, halfH * 0.95f);
        s.offset = new Vector2(x, y);

        Color c = color;
        c.a = Random.Range(alphaRange.x, alphaRange.y);
        s.sr.color = c;
    }

    void LateUpdate()
    {
        float dir = CurrentDirection();
        bool show = dir != 0f;
        UpdateViewSize();

        Vector3 camPos = cam.transform.position;
        foreach (Streak s in streaks)
        {
            if (s.sr.enabled != show) s.sr.enabled = show;
            if (!show) continue;

            s.offset.x += dir * s.speed * Time.deltaTime;
            s.sr.flipX = dir < 0f;

            float spriteW = streakSprite.bounds.size.x * s.t.localScale.x;
            // Salió por el lado hacia donde sopla, o quedó fuera por un cambio de zoom
            if (s.offset.x * dir > halfW + spriteW || Mathf.Abs(s.offset.y) > halfH * 1.1f)
                Respawn(s, false);

            float y = s.offset.y + Mathf.Sin(Time.time * 3f + s.phase) * wobble;
            s.t.position = new Vector3(camPos.x + s.offset.x, camPos.y + y, 0f);
        }
    }
}
