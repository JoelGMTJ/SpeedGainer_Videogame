using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Música del juego. Se crea sola al iniciar (no hay que ponerla en ninguna escena) y sobrevive
/// a los cambios de escena, así la música no se corta al reintentar un nivel.
///
/// Las canciones se cargan de "Resources/Music" según el inicio de su nombre:
///  - Menu_...        → escena "Menu"
///  - Level_1_...     → escena "Level1" (igual para 2, 3, ...)
///  - Level_Clear_... → al terminar un nivel con al menos 1 estrella
///  - Level_Lost_...  → al terminar un nivel sin estrellas
///  - Game_Clear_...  → pantalla de victoria (juego completado)
/// </summary>
public class MusicManager : MonoBehaviour
{
    const string MusicFolder = "Music";

    [Range(0f, 1f)] public float volume = 0.6f;
    [Tooltip("Duración del fundido al cambiar de canción (segundos, tiempo real).")]
    public float fadeTime = 0.4f;

    public static MusicManager Instance { get; private set; }

    private AudioSource source;
    private AudioClip[] clips = new AudioClip[0];
    private Coroutine fadeRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Create()
    {
        if (Instance != null) return;
        var go = new GameObject("MusicManager");
        DontDestroyOnLoad(go);
        go.AddComponent<MusicManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true;
        source.volume = volume;

        clips = Resources.LoadAll<AudioClip>(MusicFolder);
        if (clips.Length == 0)
            Debug.LogWarning("MusicManager: no se encontró música en Resources/" + MusicFolder, this);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => PlaySceneMusic();

    // ---------- API ----------

    /// <summary>Música normal de la escena actual (menú o nivel). Si ya está sonando no se reinicia.</summary>
    public static void PlaySceneMusic()
    {
        if (Instance == null) return;
        string scene = SceneManager.GetActiveScene().name;

        AudioClip clip = null;
        Match m = Regex.Match(scene, @"^Level\s*(\d+)$", RegexOptions.IgnoreCase);
        if (m.Success) clip = Instance.Find("Level_" + m.Groups[1].Value + "_");
        else if (scene.Equals("Menu", System.StringComparison.OrdinalIgnoreCase)) clip = Instance.Find("Menu");

        Instance.Play(clip, true);
    }

    public static void PlayLevelClear() => Instance?.Play(Instance.Find("Level_Clear"), false);
    public static void PlayLevelLost() => Instance?.Play(Instance.Find("Level_Lost"), false);
    public static void PlayGameClear() => Instance?.Play(Instance.Find("Game_Clear"), false);

    // ---------- Interno ----------

    AudioClip Find(string prefix)
    {
        foreach (AudioClip c in clips)
            if (c != null && c.name.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                return c;
        return null;
    }

    void Play(AudioClip clip, bool loop)
    {
        // La misma canción en bucle ya sonando: dejarla seguir (ej. al reintentar el nivel)
        if (clip != null && source.clip == clip && source.isPlaying && source.loop == loop) return;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(SwitchTo(clip, loop));
    }

    IEnumerator SwitchTo(AudioClip clip, bool loop)
    {
        // Bajar la canción actual
        if (source.isPlaying && fadeTime > 0f)
        {
            float start = source.volume;
            for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
            {
                source.volume = Mathf.Lerp(start, 0f, t / fadeTime);
                yield return null;
            }
        }
        source.Stop();
        source.clip = clip;
        source.loop = loop;
        source.volume = volume;
        if (clip != null) source.Play();
        fadeRoutine = null;
    }
}
