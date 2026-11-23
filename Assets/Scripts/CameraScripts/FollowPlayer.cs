using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FollowPlayer : MonoBehaviour
{
  [Header("Objetivo")]
  [SerializeField] private Transform target;

  [Header("Seguimiento")]
  [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
  [SerializeField] private float smoothSpeed = 5f;
  [Tooltip("Dónde queda el jugador en la pantalla, de izquierda (0) a derecha (1). 0.25 = a un cuarto desde la izquierda.")]
  [Range(0f, 1f)] [SerializeField] private float playerScreenX = 0.25f;
  [Tooltip("Seguir al jugador en X sin retraso (si no, con la velocidad se va quedando a la derecha de su lugar).")]
  [SerializeField] private bool lockHorizontal = true;

  [Header("Zoom por velocidad")]
  [Tooltip("Tamaño de la cámara cuando el jugador está quieto")]
  [SerializeField] private float minZoom = 5f;
  [Tooltip("Tamaño de la cámara a la velocidad máxima")]
  [SerializeField] private float maxZoom = 12f;
  [Tooltip("Velocidad a la que la cámara llega al zoom máximo")]
  [SerializeField] private float speedForMaxZoom = 15f;
  [Tooltip("Qué tan suave es el cambio de zoom")]
  [SerializeField] private float zoomSmoothSpeed = 2f;

  [Header("Inicio")]
  [Tooltip("Al empezar, colocar la cámara directamente sobre el jugador (sin deslizarse desde donde está en la escena).")]
  [SerializeField] private bool snapOnStart = true;

  private Camera cam;
  private Rigidbody2D targetRb;

  private void Awake()
  {
    cam = GetComponent<Camera>();

    if (target != null)
    {
      targetRb = target.GetComponent<Rigidbody2D>();
    }
  }

  private void Start()
  {
    if (snapOnStart && target != null)
    {
      if (cam.orthographic) cam.orthographicSize = minZoom;
      transform.position = new Vector3(DesiredX(), transform.position.y, transform.position.z);
    }
  }

  private void LateUpdate()
  {
    if (target == null)
    {
      return;
    }

    // Seguimiento horizontal: el jugador queda en playerScreenX del ancho de la pantalla
    float x = lockHorizontal
        ? DesiredX()
        : Mathf.Lerp(transform.position.x, DesiredX(), 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
    transform.position = new Vector3(x, transform.position.y, transform.position.z);

    // Zoom según la velocidad
    if (targetRb != null && cam.orthographic)
    {
      float speed = Mathf.Abs(targetRb.linearVelocity.x); float t = Mathf.Clamp01(speed / speedForMaxZoom);
      float desiredZoom = Mathf.Lerp(minZoom, maxZoom, t);

      cam.orthographicSize = Mathf.Lerp(
          cam.orthographicSize,
          desiredZoom,
          1f - Mathf.Exp(-zoomSmoothSpeed * Time.deltaTime)
      );
    }
  }

  // X de la cámara para que el jugador quede en playerScreenX (se adapta al zoom actual)
  private float DesiredX()
  {
    float halfWidth = cam.orthographic ? cam.orthographicSize * cam.aspect : 0f;
    return target.position.x + offset.x + (0.5f - playerScreenX) * 2f * halfWidth;
  }
}
