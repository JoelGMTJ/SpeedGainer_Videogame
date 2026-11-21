using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FollowPlayer : MonoBehaviour
{
  [Header("Objetivo")]
  [SerializeField] private Transform target;

  [Header("Seguimiento")]
  [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
  [SerializeField] private float smoothSpeed = 5f;

  [Header("Zoom por velocidad")]
  [Tooltip("Tamaño de la cámara cuando el jugador está quieto")]
  [SerializeField] private float minZoom = 5f;
  [Tooltip("Tamaño de la cámara a la velocidad máxima")]
  [SerializeField] private float maxZoom = 12f;
  [Tooltip("Velocidad a la que la cámara llega al zoom máximo")]
  [SerializeField] private float speedForMaxZoom = 15f;
  [Tooltip("Qué tan suave es el cambio de zoom")]
  [SerializeField] private float zoomSmoothSpeed = 2f;

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

  private void LateUpdate()
  {
    if (target == null)
    {
      return;
    }

    // Seguimiento horizontal (igual que antes)
    Vector3 targetPosition = new Vector3(
        target.position.x + offset.x,
        transform.position.y,
        transform.position.z
    );
    Vector3 smoothedPosition = Vector3.Lerp(
        transform.position,
        targetPosition,
        1f - Mathf.Exp(-smoothSpeed * Time.deltaTime)
    );
    transform.position = smoothedPosition;

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
}