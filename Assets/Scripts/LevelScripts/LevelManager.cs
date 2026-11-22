using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Arma el nivel conectando partes (LevelPart) al azar, una detrás de otra, como un endless runner.
/// Solo existen las partes cercanas al jugador: las nuevas aparecen adelante y las que quedan muy atrás
/// se desactivan (y se vuelven a activar si el jugador regresa, ej. con el viento en contra).
///
/// Después de la última parte se coloca "endPrefab" (la pared final).
/// Si hay un BoostRingSpawner en el mismo objeto, se le avisa de cada parte nueva para poner anillos.
/// </summary>
[DefaultExecutionOrder(-50)]
public class LevelManager : MonoBehaviour
{
    [Header("Partes del nivel")]
    [Tooltip("Partes que se pueden elegir al azar.")]
    public List<LevelPart> partPrefabs = new List<LevelPart>();

    [Tooltip("Cuántas partes tiene el nivel (sin contar el inicio y la pared final).")]
    [Min(1)] public int partsInLevel = 15;

    [Tooltip("Una parte no se repite hasta que salgan esta cantidad de partes distintas.")]
    [Min(0)] public int noRepeatWindow = 4;

    [Tooltip("0 = nivel distinto cada vez. Otro número = siempre el mismo orden de partes (útil para probar).")]
    public int seed = 0;

    [Header("Inicio y final")]
    [Tooltip("Donde empieza la primera parte (el final del inicio del nivel). Se usa su posición en X e Y.")]
    public Transform startPoint;

    [Tooltip("Pieza que cierra el nivel después de la última parte (pared final).")]
    public LevelPart endPrefab;

    [Header("Apariencia")]
    [Tooltip("Perfil de roca para las partes (ej. el café en el nivel 3). Vacío = el que trae cada parte (gris).")]
    public UnityEngine.U2D.SpriteShape caveProfile;

    [Header("Aparición")]
    [Tooltip("Jugador. Si se deja vacío se busca el que tenga PlayerMovement.")]
    public Transform player;

    [Tooltip("Las partes se crean cuando su inicio está a menos de esta distancia delante del jugador.")]
    public float spawnAheadDistance = 150f;

    [Tooltip("Las partes cuyo final queda más atrás que esta distancia se desactivan.")]
    public float keepBehindDistance = 150f;

    /// <summary>Se llama con cada parte nueva y su número (0 = primera). La pared final usa número = partsInLevel.</summary>
    public event Action<LevelPart, int> PartSpawned;

    /// <summary>Cuántas partes (incluida la pared final) se han creado.</summary>
    public int SpawnedCount => spawned.Count;

    /// <summary>X (mundo) donde empieza la pared final.</summary>
    public float EndX { get; private set; }

    /// <summary>La pared final, cuando ya se creó.</summary>
    public LevelPart EndPart { get; private set; }

    private readonly List<LevelPart> spawned = new List<LevelPart>();
    private readonly List<int> sequence = new List<int>();
    private Vector3 nextStart;
    private System.Random rng;
    private BoostRingSpawner ringSpawner;

    void Awake()
    {
        rng = seed == 0 ? new System.Random() : new System.Random(seed);

        if (player == null)
        {
            PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
            if (pm != null) player = pm.transform;
        }

        nextStart = startPoint != null ? startPoint.position : transform.position;
        nextStart.z = 0f;

        BuildSequence();
        EndX = PredictEndX();

        ringSpawner = GetComponent<BoostRingSpawner>();
        if (ringSpawner != null) ringSpawner.Plan(partsInLevel, rng);
    }

    void Start()
    {
        UpdateParts();
    }

    void Update()
    {
        UpdateParts();
    }

    // Orden de las partes elegido al inicio: al azar, sin repetir dentro de noRepeatWindow.
    void BuildSequence()
    {
        sequence.Clear();
        var valid = new List<int>();
        for (int i = 0; i < partPrefabs.Count; i++)
            if (partPrefabs[i] != null) valid.Add(i);

        if (valid.Count == 0)
        {
            Debug.LogWarning("LevelManager: no hay partes en partPrefabs.", this);
            return;
        }

        int window = Mathf.Min(noRepeatWindow, valid.Count - 1);
        var recent = new Queue<int>();
        for (int n = 0; n < partsInLevel; n++)
        {
            var options = new List<int>();
            foreach (int i in valid)
                if (!recent.Contains(i)) options.Add(i);

            int pick = options[rng.Next(options.Count)];
            sequence.Add(pick);

            recent.Enqueue(pick);
            while (recent.Count > window) recent.Dequeue();
        }
    }

    float PredictEndX()
    {
        float x = nextStart.x;
        foreach (int i in sequence) x += partPrefabs[i].length;
        return x;
    }

    void UpdateParts()
    {
        if (player == null) return;
        float px = player.position.x;

        // Crear partes nuevas delante del jugador
        while (spawned.Count < sequence.Count + 1 && nextStart.x < px + spawnAheadDistance)
        {
            int index = spawned.Count;
            bool isEnd = index >= sequence.Count;
            LevelPart prefab = isEnd ? endPrefab : partPrefabs[sequence[index]];
            if (prefab == null)
            {
                // Sin pared final: solo marcar como terminado
                spawned.Add(null);
                break;
            }
            SpawnPart(prefab, index, isEnd);
        }

        // Activar / desactivar según la distancia al jugador
        foreach (LevelPart part in spawned)
        {
            if (part == null) continue;
            bool keep = part.EndPoint.x >= px - keepBehindDistance;
            if (part.gameObject.activeSelf != keep) part.gameObject.SetActive(keep);
        }
    }

    void SpawnPart(LevelPart prefab, int index, bool isEnd)
    {
        LevelPart part = Instantiate(prefab, nextStart, Quaternion.identity, transform);
        part.name = (isEnd ? "End_" : index.ToString("00") + "_") + prefab.name;

        if (caveProfile != null)
            foreach (LevelSkin skin in part.GetComponentsInChildren<LevelSkin>())
                skin.SetProfile(caveProfile);

        spawned.Add(part);
        nextStart = part.EndPoint;

        if (isEnd)
        {
            EndPart = part;
            EndX = part.transform.position.x;
        }
        else if (ringSpawner != null)
        {
            ringSpawner.OnPartSpawned(part, index);
        }

        PartSpawned?.Invoke(part, index);
    }

    void OnDrawGizmosSelected()
    {
        Vector3 s = startPoint != null ? startPoint.position : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(s + Vector3.down * 12f, s + Vector3.up * 12f);
    }
}
