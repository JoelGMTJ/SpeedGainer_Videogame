using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD del nivel:
///  - Arriba a la izquierda: estrellas que todavía se pueden conseguir y el tiempo límite para ellas
///    (empieza en 3 estrellas; al pasar ese tiempo baja a 2 y luego a 1). Debajo, el cronómetro.
///  - Arriba al centro: barra de progreso con el astronauta avanzando (como en Geometry Dash).
///  - Arriba a la derecha: velocidad en km/h.
/// La lógica de tiempo y estrellas está en LevelTimer; este script solo la muestra.
/// </summary>
[DefaultExecutionOrder(100)]
public class LevelHUD : MonoBehaviour
{
    [Header("Referencias (vacío = se buscan solas)")]
    public PlayerMovement player;
    public LevelTimer timer;

    [Header("Velocidad")]
    public TMP_Text speedText;
    [Tooltip("Unidades/seg a km/h. 3.6 = 1 unidad es 1 metro.")]
    public float speedToKmh = 3.6f;

    [Header("Estrellas y tiempo")]
    [Tooltip("Las 3 imágenes de estrella, de izquierda a derecha.")]
    public Image[] starImages = new Image[3];
    public Sprite starFull;
    public Sprite starEmpty;
    [Tooltip("Texto pequeño con el tiempo límite de las estrellas actuales.")]
    public TMP_Text starGoalText;
    [Tooltip("Cronómetro (debajo de las estrellas).")]
    public TMP_Text timerText;
    public Color goalColor = new Color(1f, 0.85f, 0.35f);
    public Color slowColor = new Color(1f, 0.45f, 0.4f);

    [Header("Barra de progreso")]
    [Tooltip("Imagen de relleno (tipo Filled, horizontal).")]
    public Image progressFill;
    [Tooltip("Ícono del astronauta. Su ancla X se mueve de 0 a 1 dentro de su padre.")]
    public RectTransform progressIcon;
    public TMP_Text progressPercentText;

    private float startX;

    void Start()
    {
        if (player == null) player = FindAnyObjectByType<PlayerMovement>();
        if (timer == null) timer = FindAnyObjectByType<LevelTimer>();
        if (player != null) startX = player.transform.position.x;
    }

    void LateUpdate()
    {
        if (player == null || timer == null) return;

        // ---- Velocidad ----
        if (speedText != null)
            speedText.text = Mathf.RoundToInt(player.CurrentSpeed * speedToKmh) + " km/h";

        // ---- Estrellas ----
        int stars = timer.IsFinished ? timer.Stars : timer.StarsForTime(timer.ElapsedTime);
        for (int i = 0; i < starImages.Length; i++)
            if (starImages[i] != null) starImages[i].sprite = i < stars ? starFull : starEmpty;

        if (starGoalText != null)
        {
            if (stars == 0)
            {
                starGoalText.text = "Muy lento";
                starGoalText.color = slowColor;
            }
            else
            {
                starGoalText.text = timer.IsFinished ? "Superado" : "< " + LevelTimer.FormatTime(timer.TimeLimitFor(stars));
                starGoalText.color = goalColor;
            }
        }

        if (timerText != null)
            timerText.text = LevelTimer.FormatTime(timer.ElapsedTime);

        // ---- Progreso ----
        float total = timer.FinishX - startX;
        float p = total > 0.01f ? Mathf.Clamp01((player.transform.position.x - startX) / total) : 0f;
        if (timer.IsFinished) p = 1f;

        if (progressFill != null) progressFill.fillAmount = p;
        if (progressIcon != null)
        {
            progressIcon.anchorMin = new Vector2(p, progressIcon.anchorMin.y);
            progressIcon.anchorMax = new Vector2(p, progressIcon.anchorMax.y);
        }
        if (progressPercentText != null)
            progressPercentText.text = Mathf.FloorToInt(p * 100f) + "%";
    }
}
