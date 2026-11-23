using System;
using TMPro;
using UnityEngine;

/// <summary>
/// Cronómetro del nivel y sistema de estrellas.
/// Empieza al cargar el nivel, se reinicia con la tecla de reinicio (R) y se detiene cuando el jugador
/// llega a la pared final (la X donde empieza el endPrefab del LevelManager).
/// Las estrellas dependen del tiempo final: menor o igual a timeFor3Stars = 3, a timeFor2Stars = 2,
/// a timeFor1Star = 1, más lento = 0. Con al menos 1 estrella se puede pasar al siguiente nivel.
/// </summary>
public class LevelTimer : MonoBehaviour
{
    [Header("Tiempos para las estrellas (segundos)")]
    [Tooltip("Tiempo máximo para conseguir 1 estrella (el mínimo para pasar de nivel).")]
    [Min(0f)] public float timeFor1Star = 90f;
    [Tooltip("Tiempo máximo para conseguir 2 estrellas.")]
    [Min(0f)] public float timeFor2Stars = 60f;
    [Tooltip("Tiempo máximo para conseguir 3 estrellas.")]
    [Min(0f)] public float timeFor3Stars = 40f;

    [Header("Referencias")]
    [Tooltip("Jugador. Vacío = se busca solo.")]
    public PlayerMovement player;
    [Tooltip("LevelManager del nivel (de él sale dónde está la meta). Vacío = se busca solo.")]
    public LevelManager levelManager;
    [Tooltip("Texto donde se muestra el tiempo (temporal, como el de la velocidad).")]
    public TMP_Text timerText;

    [Header("Meta")]
    [Tooltip("Distancia extra después del inicio de la pared final para contar como terminado (0 = al entrar a la parte final).")]
    public float finishOffset = 0f;

    /// <summary>Se llama al terminar el nivel con el tiempo final y las estrellas conseguidas.</summary>
    public event Action<float, int> LevelFinished;

    public float ElapsedTime { get; private set; }
    public bool IsRunning { get; private set; }
    public bool IsFinished { get; private set; }
    /// <summary>Estrellas del último intento terminado (0 si todavía no termina).</summary>
    public int Stars { get; private set; }
    /// <summary>Con al menos 1 estrella se puede pasar al siguiente nivel.</summary>
    public bool CanAdvance => IsFinished && Stars >= 1;
    public float FinishX => (levelManager != null ? levelManager.EndX : float.PositiveInfinity) + finishOffset;

    void Awake()
    {
        if (player == null) player = FindAnyObjectByType<PlayerMovement>();
        if (levelManager == null) levelManager = FindAnyObjectByType<LevelManager>();
    }

    void OnEnable() { if (player != null) player.PlayerReset += RestartTimer; }
    void OnDisable() { if (player != null) player.PlayerReset -= RestartTimer; }

    void Start() => RestartTimer();

    void OnValidate()
    {
        // Mantener el orden: 3 estrellas <= 2 estrellas <= 1 estrella
        timeFor2Stars = Mathf.Min(timeFor2Stars, timeFor1Star);
        timeFor3Stars = Mathf.Min(timeFor3Stars, timeFor2Stars);
    }

    public void RestartTimer()
    {
        ElapsedTime = 0f;
        Stars = 0;
        IsFinished = false;
        IsRunning = true;
        UpdateText();
    }

    void Update()
    {
        if (!IsRunning) return;

        ElapsedTime += Time.deltaTime;

        if (player != null && player.transform.position.x >= FinishX)
            Finish();

        UpdateText();
    }

    void Finish()
    {
        IsRunning = false;
        IsFinished = true;
        Stars = StarsForTime(ElapsedTime);
        Debug.Log($"Nivel terminado en {FormatTime(ElapsedTime)} → {Stars} estrella(s)", this);
        LevelFinished?.Invoke(ElapsedTime, Stars);
    }

    public int StarsForTime(float time)
    {
        if (time <= timeFor3Stars) return 3;
        if (time <= timeFor2Stars) return 2;
        if (time <= timeFor1Star) return 1;
        return 0;
    }

    /// <summary>Tiempo máximo para conseguir esa cantidad de estrellas (1 a 3).</summary>
    public float TimeLimitFor(int stars)
    {
        if (stars >= 3) return timeFor3Stars;
        if (stars == 2) return timeFor2Stars;
        return timeFor1Star;
    }

    void UpdateText()
    {
        if (timerText == null) return;
        string text = "Tiempo: " + FormatTime(ElapsedTime);
        if (IsFinished)
            text += $"\nEstrellas: {Stars}/3" + (CanAdvance ? "" : "  (muy lento)");
        timerText.text = text;
    }

    public static string FormatTime(float t)
    {
        int minutes = (int)(t / 60f);
        return $"{minutes}:{t - minutes * 60f:00.00}";
    }
}
