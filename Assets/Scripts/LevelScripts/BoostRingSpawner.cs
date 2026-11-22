using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pone anillos de impulso en las partes que crea el LevelManager (va en el mismo objeto).
/// Cada bloque de "oneRingEveryParts" partes tiene un anillo en una de ellas, elegida al azar.
/// El anillo va cerca de la línea media del túnel (un poco arriba o abajo) y nunca encima del terreno.
/// </summary>
[RequireComponent(typeof(LevelManager))]
public class BoostRingSpawner : MonoBehaviour
{
    [Tooltip("Prefab del anillo (Assets/Prefabs/Objects/BoostRing).")]
    public BoostRing ringPrefab;

    [Tooltip("Un anillo por cada esta cantidad de partes (4 = una de cada cuatro partes tiene anillo).")]
    [Min(1)] public int oneRingEveryParts = 4;

    [Tooltip("Cuánto puede subir o bajar el anillo respecto a la línea media del túnel.")]
    [Min(0f)] public float maxOffsetFromMiddle = 2.5f;

    [Tooltip("Distancia mínima a los extremos de la parte.")]
    [Min(0f)] public float edgeMargin = 12f;

    [Tooltip("Tamaño del espacio libre que necesita el anillo (no debe tocar el terreno).")]
    public Vector2 clearSpace = new Vector2(4f, 5f);

    [Tooltip("Capas que cuentan como terreno al buscar espacio libre.")]
    public LayerMask terrainLayers = ~0;

    [Tooltip("Intentos para encontrar un lugar libre antes de rendirse en esa parte.")]
    [Min(1)] public int placementAttempts = 15;

    private readonly HashSet<int> ringParts = new HashSet<int>();
    private System.Random rng;
    private readonly List<Collider2D> hits = new List<Collider2D>();

    /// <summary>El LevelManager llama esto al inicio para decidir qué partes llevan anillo.</summary>
    public void Plan(int partsInLevel, System.Random random)
    {
        rng = random ?? new System.Random();
        ringParts.Clear();
        for (int block = 0; block < partsInLevel; block += oneRingEveryParts)
        {
            int size = Mathf.Min(oneRingEveryParts, partsInLevel - block);
            ringParts.Add(block + rng.Next(size));
        }
    }

    /// <summary>El LevelManager llama esto con cada parte nueva.</summary>
    public void OnPartSpawned(LevelPart part, int index)
    {
        if (ringPrefab == null || !ringParts.Contains(index)) return;
        if (rng == null) rng = new System.Random();

        Physics2D.SyncTransforms();

        float middle = (part.floorTop + part.ceilingBottom) * 0.5f;
        float minX = edgeMargin, maxX = Mathf.Max(edgeMargin, part.length - edgeMargin);

        var filter = new ContactFilter2D();
        filter.SetLayerMask(terrainLayers);
        filter.useTriggers = false;

        for (int attempt = 0; attempt < placementAttempts; attempt++)
        {
            // Los primeros intentos se quedan más cerca de la línea media
            float spread = maxOffsetFromMiddle * Mathf.Min(1f, 0.5f + attempt * 0.1f);
            float lx = Mathf.Lerp(minX, maxX, (float)rng.NextDouble());
            float ly = middle + Mathf.Lerp(-spread, spread, (float)rng.NextDouble());
            Vector3 pos = part.transform.TransformPoint(new Vector3(lx, ly, 0f));

            if (Physics2D.OverlapBox(pos, clearSpace, 0f, filter, hits) > 0) continue;

            BoostRing ring = Instantiate(ringPrefab, pos, Quaternion.identity, part.transform);
            ring.name = ringPrefab.name;
            return;
        }
        // Parte muy llena: sin anillo esta vez
    }

    void OnDrawGizmosSelected()
    {
        // Muestra el rango vertical donde aparecen los anillos (sobre el inicio del nivel)
        LevelManager lm = GetComponent<LevelManager>();
        Vector3 s = lm != null && lm.startPoint != null ? lm.startPoint.position : transform.position;
        float middle = s.y + (-9.79f + 10.23f) * 0.5f;
        Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.8f);
        Gizmos.DrawWireCube(new Vector3(s.x + 56f, middle, 0f), new Vector3(112f - edgeMargin * 2f, maxOffsetFromMiddle * 2f, 0f));
    }
}
