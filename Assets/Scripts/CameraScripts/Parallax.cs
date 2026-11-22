using UnityEngine;

public class Parallax : MonoBehaviour
{
    [Tooltip("Cámara que sigue al jugador. Si lo dejas vacío, usa la cámara principal.")]
    public Transform cam;

    [Tooltip("0 = se mueve igual que la cámara (parece cercana o fija al mundo). 1 = no se mueve (muy lejana). Valores entre 0 y 1 para capas intermedias.")]
    [Range(0f, 1f)]
    public float parallaxFactor = 0.5f;

    [Tooltip("Repetir la capa horizontalmente para un fondo infinito.")]
    public bool infiniteHorizontal = true;

    private Vector3 startPosition;
    private float spriteWidth;

    void Start()
    {
        if (cam == null)
            cam = Camera.main.transform;

        startPosition = transform.position;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            spriteWidth = sr.bounds.size.x;
    }

    void LateUpdate()
    {
        // Cuánto se ha movido la cámara desde el inicio
        float distance = cam.position.x * (1f - parallaxFactor);
        float displacement = cam.position.x * parallaxFactor;

        transform.position = new Vector3(
            startPosition.x + displacement,
            transform.position.y,
            transform.position.z
        );

        // Repetir la imagen para que el fondo sea infinito
        if (infiniteHorizontal && spriteWidth > 0)
        {
            if (cam.position.x > transform.position.x + spriteWidth / 2f)
                startPosition.x += spriteWidth;
            else if (cam.position.x < transform.position.x - spriteWidth / 2f)
                startPosition.x -= spriteWidth;
        }
    }
}