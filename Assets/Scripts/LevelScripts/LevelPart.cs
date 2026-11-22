using UnityEngine;

/// <summary>
/// Una parte (segmento) del nivel infinito. Las partes se conectan una tras otra:
/// el inicio está en x = 0 (local) y el final en x = length.
/// Todas las partes usan la misma altura de piso y techo para que encajen.
///
/// En la escena se dibujan líneas verdes (inicio) y rojas (final) para ver dónde conecta.
/// </summary>
public class LevelPart : MonoBehaviour
{
    [Tooltip("Largo de la parte en unidades. La siguiente parte empieza en x = length.")]
    public float length = 112f;

    [Tooltip("Altura (local) de la parte de arriba del piso.")]
    public float floorTop = -9.79f;

    [Tooltip("Altura (local) de la parte de abajo del techo.")]
    public float ceilingBottom = 10.23f;

    /// <summary>Punto (mundo) donde debe empezar la siguiente parte.</summary>
    public Vector3 EndPoint => transform.TransformPoint(new Vector3(length, 0f, 0f));

    void OnDrawGizmos()
    {
        Vector3 a = transform.TransformPoint(new Vector3(0f, floorTop - 1f, 0f));
        Vector3 b = transform.TransformPoint(new Vector3(0f, ceilingBottom + 1f, 0f));
        Vector3 c = transform.TransformPoint(new Vector3(length, floorTop - 1f, 0f));
        Vector3 d = transform.TransformPoint(new Vector3(length, ceilingBottom + 1f, 0f));
        Gizmos.color = Color.green;
        Gizmos.DrawLine(a, b);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(c, d);
    }
}
