using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Pantalla de fin de nivel: aparece al cruzar la meta (evento LevelFinished de LevelTimer).
/// Muestra las estrellas conseguidas, el tiempo y dos botones:
///  - Reintentar: vuelve a cargar el nivel (con partes nuevas al azar).
///  - Siguiente: carga la siguiente escena de Build Settings (o el menú si es el último nivel).
///    Solo se puede usar con al menos 1 estrella.
/// </summary>
public class LevelEndScreen : MonoBehaviour
{
    [Header("Referencias")]
    public LevelTimer timer;
    public PlayerMovement player;
    [Tooltip("Objeto raíz de la pantalla (se activa al terminar).")]
    public GameObject screenRoot;
    public CanvasGroup canvasGroup;

    [Header("Contenido")]
    public Image[] starImages = new Image[3];
    public Sprite starFull;
    public Sprite starEmpty;
    public TMP_Text titleText;
    public TMP_Text timeText;
    public TMP_Text messageText;
    public Button retryButton;
    public Button nextButton;
    public TMP_Text nextButtonLabel;

    [Header("Pantalla de victoria (solo último nivel)")]
    [Tooltip("Pantalla de felicitaciones al terminar el juego. Vacío = el último nivel vuelve directo al menú.")]
    public GameObject victoryRoot;
    public CanvasGroup victoryGroup;
    public Button victoryMenuButton;
    [Tooltip("Imagen que flota en la pantalla de victoria (el astronauta).")]
    public RectTransform victoryFloatingImage;

    [Header("Escenas")]
    [Tooltip("Escena a la que se vuelve después del último nivel.")]
    public string menuScene = "Menu";

    [Header("Animación")]
    [Tooltip("Espera después de cruzar la meta antes de mostrar la pantalla.")]
    public float showDelay = 0.6f;
    public float fadeTime = 0.3f;
    [Tooltip("Tiempo entre la aparición de cada estrella.")]
    public float starInterval = 0.25f;

    public Color normalMessageColor = new Color(0.93f, 0.93f, 0.95f);
    public Color failMessageColor = new Color(1f, 0.45f, 0.4f);

    private Coroutine showRoutine;

    void Awake()
    {
        if (timer == null) timer = FindAnyObjectByType<LevelTimer>();
        if (player == null) player = FindAnyObjectByType<PlayerMovement>();
        if (retryButton != null) retryButton.onClick.AddListener(Retry);
        if (nextButton != null) nextButton.onClick.AddListener(Next);
        if (victoryMenuButton != null) victoryMenuButton.onClick.AddListener(GoToMenu);
        if (victoryRoot != null) victoryRoot.SetActive(false);
        Hide();
    }

    void OnEnable()
    {
        if (timer != null) timer.LevelFinished += OnLevelFinished;
        if (player != null) player.PlayerReset += OnPlayerReset;
    }

    void OnDisable()
    {
        if (timer != null) timer.LevelFinished -= OnLevelFinished;
        if (player != null) player.PlayerReset -= OnPlayerReset;
    }

    void OnLevelFinished(float time, int stars)
    {
        if (showRoutine != null) StopCoroutine(showRoutine);
        showRoutine = StartCoroutine(Show(time, stars));
    }

    // Reinicio con R después de terminar: quitar la pantalla y volver a la música del nivel
    void OnPlayerReset()
    {
        Hide();
        MusicManager.PlaySceneMusic();
    }

    IEnumerator Show(float time, int stars)
    {
        yield return new WaitForSecondsRealtime(showDelay);

        bool passed = stars >= 1;
        if (passed) MusicManager.PlayLevelClear();
        else MusicManager.PlayLevelLost();
        bool isLastLevel = SceneManager.GetActiveScene().buildIndex + 1 >= SceneManager.sceneCountInBuildSettings;

        if (titleText != null) titleText.text = passed ? "Nivel completado" : "Muy lento";
        if (timeText != null) timeText.text = "Tiempo  " + LevelTimer.FormatTime(time);
        if (messageText != null)
        {
            messageText.text = passed
                ? (stars < 3 ? "3 estrellas: " + LevelTimer.FormatTime(timer.timeFor3Stars) : "Perfecto!")
                : "Necesitas 1 estrella para avanzar (" + LevelTimer.FormatTime(timer.timeFor1Star) + ")";
            messageText.color = passed ? normalMessageColor : failMessageColor;
        }
        if (nextButton != null) nextButton.interactable = passed;
        if (nextButtonLabel != null) nextButtonLabel.text = !isLastLevel ? "Siguiente" : (victoryRoot != null ? "Continuar" : "Menu");

        foreach (Image s in starImages)
            if (s != null) { s.sprite = starEmpty; s.transform.localScale = Vector3.one; }

        screenRoot.SetActive(true);

        // Aparecer
        for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            canvasGroup.alpha = t / fadeTime;
            yield return null;
        }
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        // Estrellas una por una con un pequeño "pop"
        for (int i = 0; i < starImages.Length && i < stars; i++)
        {
            yield return new WaitForSecondsRealtime(starInterval);
            if (starImages[i] == null) continue;
            starImages[i].sprite = starFull;
            StartCoroutine(Pop(starImages[i].transform));
        }
        showRoutine = null;
    }

    IEnumerator Pop(Transform t)
    {
        const float duration = 0.25f;
        for (float e = 0f; e < duration; e += Time.unscaledDeltaTime)
        {
            float k = e / duration;
            t.localScale = Vector3.one * (1f + 0.45f * Mathf.Sin(k * Mathf.PI));
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    public void Hide()
    {
        if (showRoutine != null) { StopCoroutine(showRoutine); showRoutine = null; }
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        if (screenRoot != null) screenRoot.SetActive(false);
    }

    public void Retry()
    {
        LevelIntro.SkipIntroOnNextLoad();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Next()
    {
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        if (next < SceneManager.sceneCountInBuildSettings) SceneManager.LoadScene(next);
        else if (victoryRoot != null) StartCoroutine(ShowVictory());
        else GoToMenu();
    }

    public void GoToMenu()
    {
        SceneManager.LoadScene(menuScene);
    }

    // Último nivel: cambia la pantalla de fin por la de felicitaciones
    IEnumerator ShowVictory()
    {
        canvasGroup.interactable = false;
        for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            canvasGroup.alpha = 1f - t / fadeTime;
            yield return null;
        }
        screenRoot.SetActive(false);

        MusicManager.PlayGameClear();
        victoryRoot.SetActive(true);
        if (victoryGroup != null)
        {
            victoryGroup.interactable = false;
            for (float t = 0f; t < fadeTime * 2f; t += Time.unscaledDeltaTime)
            {
                victoryGroup.alpha = t / (fadeTime * 2f);
                yield return null;
            }
            victoryGroup.alpha = 1f;
            victoryGroup.interactable = true;
            victoryGroup.blocksRaycasts = true;
        }

        // El astronauta flota mientras la pantalla está abierta
        Vector2 start = victoryFloatingImage != null ? victoryFloatingImage.anchoredPosition : Vector2.zero;
        while (victoryRoot.activeSelf && victoryFloatingImage != null)
        {
            float k = Time.unscaledTime;
            victoryFloatingImage.anchoredPosition = start + Vector2.up * Mathf.Sin(k * 2f) * 14f;
            victoryFloatingImage.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k * 1.3f) * 8f);
            yield return null;
        }
    }
}
