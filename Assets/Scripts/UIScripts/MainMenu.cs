using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menú principal: botón Jugar (o Enter / Espacio) carga el primer nivel.
/// También anima el título (flota) y el astronauta (corre en su lugar).
/// El fondo se mueve solo con el autoScrollSpeed de cada capa Parallax.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Tooltip("Nombre de la escena del primer nivel.")]
    public string firstLevelScene = "Level1";
    public Button playButton;

    [Header("Animación")]
    [Tooltip("Título que flota suavemente.")]
    public RectTransform title;
    public float titleBobHeight = 12f;
    public float titleBobSpeed = 1.5f;

    [Tooltip("Imagen del astronauta y los cuadros de su animación de correr.")]
    public Image astronaut;
    public Sprite[] astronautFrames;
    public float framesPerSecond = 10f;

    private Vector2 titleStart;

    void Start()
    {
        Time.timeScale = 1f;
        if (playButton != null) playButton.onClick.AddListener(Play);
        if (title != null) titleStart = title.anchoredPosition;
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
            Play();

        if (title != null)
            title.anchoredPosition = titleStart + Vector2.up * Mathf.Sin(Time.time * titleBobSpeed) * titleBobHeight;

        if (astronaut != null && astronautFrames != null && astronautFrames.Length > 0)
            astronaut.sprite = astronautFrames[(int)(Time.time * framesPerSecond) % astronautFrames.Length];
    }

    public void Play()
    {
        SceneManager.LoadScene(firstLevelScene);
    }
}
