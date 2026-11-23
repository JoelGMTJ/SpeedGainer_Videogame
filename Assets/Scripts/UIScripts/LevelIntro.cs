using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Al empezar el nivel: ventana con dos tarjetas (imagen + texto) que explican las mecánicas,
/// botón Continuar y luego una cuenta regresiva 3, 2, 1, ¡YA!
/// Mientras tanto el juego está pausado (Time.timeScale = 0) y el jugador no puede usar teclas.
/// Al reintentar el mismo nivel la ventana se salta y solo se muestra la cuenta regresiva.
/// </summary>
[DefaultExecutionOrder(-40)]
public class LevelIntro : MonoBehaviour
{
    [Serializable]
    public class Card
    {
        public Sprite icon;
        [TextArea(2, 4)] public string text;
    }

    [Header("Contenido")]
    public string title = "Nivel 1";
    public Card leftCard = new Card();
    public Card rightCard = new Card();

    [Header("Referencias")]
    public PlayerMovement player;
    public LevelTimer timer;
    [Tooltip("Objeto raíz de la ventana de introducción.")]
    public GameObject introRoot;
    public TMP_Text titleText;
    public Image leftImage;
    public TMP_Text leftText;
    public Image rightImage;
    public TMP_Text rightText;
    public Button continueButton;
    [Tooltip("Texto grande de la cuenta regresiva.")]
    public TMP_Text countdownText;

    [Header("Cuenta regresiva")]
    [Min(0)] public int countdownFrom = 3;
    public float secondsPerNumber = 0.7f;
    public string goText = "¡Ya!";
    [Tooltip("Mostrar la ventana aunque se esté reintentando el mismo nivel.")]
    public bool showIntroOnRetry = false;

    // Lo activa el botón Reintentar: la siguiente carga del nivel salta la ventana (solo cuenta regresiva)
    private static bool skipNextIntro;
    private bool waitingForContinue;

    /// <summary>Llamar antes de recargar el nivel para que solo se muestre la cuenta regresiva.</summary>
    public static void SkipIntroOnNextLoad() => skipNextIntro = true;

    // Con "Enter Play Mode Options" (sin recarga de dominio) las variables static sobreviven entre partidas:
    // se limpian al iniciar el juego.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => skipNextIntro = false;

    void Awake()
    {
        if (player == null) player = FindAnyObjectByType<PlayerMovement>();
        if (timer == null) timer = FindAnyObjectByType<LevelTimer>();
        if (continueButton != null) continueButton.onClick.AddListener(Continue);

        Time.timeScale = 0f;
        if (player != null) player.SetInputLocked(true);
        if (countdownText != null) countdownText.gameObject.SetActive(false);

        bool retry = skipNextIntro;
        skipNextIntro = false;

        if (retry && !showIntroOnRetry)
        {
            if (introRoot != null) introRoot.SetActive(false);
            StartCoroutine(Countdown());
        }
        else
        {
            ShowIntro();
        }
    }

    void ShowIntro()
    {
        if (titleText != null) titleText.text = title;
        if (leftImage != null) { leftImage.sprite = leftCard.icon; leftImage.preserveAspect = true; }
        if (leftText != null) leftText.text = leftCard.text;
        if (rightImage != null) { rightImage.sprite = rightCard.icon; rightImage.preserveAspect = true; }
        if (rightText != null) rightText.text = rightCard.text;
        if (introRoot != null) introRoot.SetActive(true);
        waitingForContinue = true;
    }

    void Update()
    {
        // Enter también continúa
        if (waitingForContinue && Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
            Continue();
    }

    public void Continue()
    {
        if (!waitingForContinue) return;
        waitingForContinue = false;
        if (introRoot != null) introRoot.SetActive(false);
        StartCoroutine(Countdown());
    }

    IEnumerator Countdown()
    {
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            for (int n = countdownFrom; n > 0; n--)
                yield return ShowNumber(n.ToString());
        }

        // ¡A jugar!
        Time.timeScale = 1f;
        if (player != null) player.SetInputLocked(false);
        if (timer != null) timer.RestartTimer();

        if (countdownText != null)
        {
            yield return ShowNumber(goText);
            countdownText.gameObject.SetActive(false);
        }
    }

    // Número que aparece grande y se encoge / desvanece
    IEnumerator ShowNumber(string text)
    {
        countdownText.text = text;
        Transform t = countdownText.transform;
        for (float e = 0f; e < secondsPerNumber; e += Time.unscaledDeltaTime)
        {
            float k = e / secondsPerNumber;
            t.localScale = Vector3.one * Mathf.Lerp(1.4f, 0.9f, k);
            Color c = countdownText.color;
            c.a = k < 0.7f ? 1f : Mathf.Lerp(1f, 0f, (k - 0.7f) / 0.3f);
            countdownText.color = c;
            yield return null;
        }
    }

    void OnDestroy()
    {
        // Nunca dejar el juego pausado al cambiar de escena
        Time.timeScale = 1f;
    }
}
