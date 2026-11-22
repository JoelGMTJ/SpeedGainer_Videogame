using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

/// <summary>
/// Genera la parte visual del nivel (roca de la cueva) a partir de los colliders del nivel.
/// Une todos los colliders en una sola silueta (CompositeCollider2D) y la dibuja con un
/// Sprite Shape: borde de roca en todas las superficies y relleno oscuro por dentro.
/// La física NO cambia: los colliders originales se quedan igual, solo se ocultan sus sprites grises.
///
/// Uso: con "Live Update" activo, la roca se regenera sola en el editor al mover, escalar,
/// agregar, borrar o editar (Edit Collider) cualquier pieza dentro de levelRoot.
/// También se puede regenerar a mano: clic derecho en el componente → "Rebuild Skin".
/// </summary>
[ExecuteAlways]
public class LevelSkin : MonoBehaviour
{
    [Tooltip("Raíz de los objetos del nivel (piso, techo, rampas). Se usan todos los colliders dentro.")]
    public Transform levelRoot;

    [Tooltip("Perfil de Sprite Shape con el borde de roca y el relleno.")]
    public SpriteShape caveProfile;

    [Tooltip("Colliders que se alargan hacia abajo solo en lo visual (ej. el piso), para que no se vea el fondo debajo.")]
    public Collider2D[] extendDown;

    [Tooltip("Colliders que se alargan hacia arriba solo en lo visual (ej. el techo).")]
    public Collider2D[] extendUp;

    [Tooltip("Cuánto se alargan visualmente los colliders de extendDown / extendUp.")]
    public float extendDistance = 20f;

    [Tooltip("Une piezas que casi se tocan (huecos menores a 2 veces este valor).")]
    public float mergeDistance = 0.1f;

    [Tooltip("Orden de dibujado. Debe quedar delante del fondo (parallax) y detrás del jugador.")]
    public int sortingOrder = -5;

    [Tooltip("Ocultar los sprites grises originales de los colliders.")]
    public bool hideOriginalSprites = true;

    [Header("Extremos abiertos (partes de nivel)")]
    [Tooltip("Sin borde de roca en los lados verticales del extremo izquierdo y derecho, para que las partes se conecten sin costura. Requiere un sprite con \"blank\" en el nombre dentro del perfil.")]
    public bool openEnds = false;
    [Tooltip("Con openEnds: dejar el borde de roca del lado izquierdo (para el inicio de un nivel, que solo conecta por la derecha).")]
    public bool keepLeftEdge = false;

    [Header("Editor")]
    [Tooltip("Regenerar la roca automáticamente en el editor cuando cambia cualquier pieza del nivel.")]
    public bool liveUpdate = true;
    [Tooltip("Segundos sin cambios antes de regenerar (para que arrastrar piezas sea fluido).")]
    public float liveUpdateDelay = 0.15f;

    [ContextMenu("Rebuild Skin")]
    public void Rebuild()
    {
        if (levelRoot == null || caveProfile == null)
        {
            Debug.LogWarning("LevelSkin: asigna levelRoot y caveProfile.", this);
            return;
        }

        ClearSkin();

        List<List<Vector2>> outlines = BuildMergedOutlines();

        // Límites izquierdo/derecho de todo el contorno (en coordenadas locales), para los extremos abiertos
        float minX = float.MaxValue, maxX = float.MinValue;
        foreach (var o in outlines)
            foreach (Vector2 w in o)
            {
                float lx = transform.InverseTransformPoint(w).x;
                minX = Mathf.Min(minX, lx);
                maxX = Mathf.Max(maxX, lx);
            }

        int blank = openEnds ? BlankSpriteIndex() : -1;
        if (openEnds && blank < 0)
            Debug.LogWarning("LevelSkin: openEnds necesita un sprite con 'blank' en el nombre dentro del perfil.", this);

        for (int i = 0; i < outlines.Count; i++)
            CreateShape("Rock_" + i, outlines[i], minX, maxX, blank);

        if (hideOriginalSprites)
        {
            foreach (Collider2D col in levelRoot.GetComponentsInChildren<Collider2D>())
            {
                // Sprites grises o relleno de vista previa de LevelPiece
                Renderer r = col.GetComponent<Renderer>();
                if (r != null) r.enabled = false;
            }
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            lastHash = ComputeHash();
            if (gameObject.scene.IsValid())
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }

    /// <summary>
    /// Cambia el perfil de roca (ej. gris → café). Si los dos perfiles tienen la misma lista de sprites
    /// solo se cambia el perfil de cada forma (rápido, sirve en juego); si no, se regenera todo.
    /// </summary>
    public void SetProfile(SpriteShape profile)
    {
        if (profile == null || profile == caveProfile) return;

        bool sameLayout = caveProfile != null &&
            caveProfile.angleRanges.Count == profile.angleRanges.Count &&
            (caveProfile.angleRanges.Count == 0 ||
             caveProfile.angleRanges[0].sprites.Count == profile.angleRanges[0].sprites.Count);

        caveProfile = profile;
        if (!sameLayout || transform.childCount == 0)
        {
            Rebuild();
            return;
        }

        foreach (SpriteShapeController ssc in GetComponentsInChildren<SpriteShapeController>())
        {
            ssc.spriteShape = profile;
            ssc.RefreshSpriteShape();
        }
    }

#if UNITY_EDITOR
    // ---- Actualización en vivo (solo editor, fuera de Play) ----
    private int lastHash;
    private double changedAt = -1;

    void OnEnable()
    {
        if (Application.isPlaying) return;
        lastHash = ComputeHash();
        UnityEditor.EditorApplication.update -= EditorTick;
        UnityEditor.EditorApplication.update += EditorTick;
    }

    void OnDisable()
    {
        UnityEditor.EditorApplication.update -= EditorTick;
    }

    void EditorTick()
    {
        if (this == null) { UnityEditor.EditorApplication.update -= EditorTick; return; }
        if (Application.isPlaying || !liveUpdate || levelRoot == null || caveProfile == null) return;
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;
        // Dentro de una instancia de prefab no se pueden borrar/crear hijos: se edita abriendo el prefab.
        if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(this)) return;

        double now = UnityEditor.EditorApplication.timeSinceStartup;
        int h = ComputeHash();
        if (h != lastHash)
        {
            lastHash = h;
            changedAt = now;
        }

        if (changedAt >= 0 && now - changedAt >= liveUpdateDelay)
        {
            changedAt = -1;
            Rebuild();
        }
    }

    // Huella de todo lo que afecta la forma de la roca: si cambia, hay que regenerar.
    int ComputeHash()
    {
        if (levelRoot == null) return 0;
        unchecked
        {
            int h = 17;
            h = h * 31 + (caveProfile != null ? caveProfile.GetHashCode() : 0);
            h = h * 31 + extendDistance.GetHashCode();
            h = h * 31 + mergeDistance.GetHashCode();
            h = h * 31 + sortingOrder;
            h = h * 31 + (openEnds ? 1 : 0) + (keepLeftEdge ? 2 : 0);
            if (extendDown != null) foreach (var c in extendDown) h = h * 31 + (c != null ? c.GetHashCode() : 1);
            if (extendUp != null) foreach (var c in extendUp) h = h * 31 + (c != null ? c.GetHashCode() : 2);

            foreach (Collider2D col in levelRoot.GetComponentsInChildren<Collider2D>(true))
            {
                h = h * 31 + col.GetHashCode();
                h = h * 31 + (col.enabled && col.gameObject.activeInHierarchy && !col.isTrigger ? 1 : 0);
                // Relativo a este objeto: mover todo el nivel/parte junto no obliga a regenerar
                h = h * 31 + (transform.worldToLocalMatrix * col.transform.localToWorldMatrix).GetHashCode();
                h = h * 31 + col.offset.GetHashCode();
                if (col is BoxCollider2D box)
                    h = h * 31 + box.size.GetHashCode();
                else if (col is PolygonCollider2D poly)
                    for (int p = 0; p < poly.pathCount; p++)
                        foreach (Vector2 v in poly.GetPath(p)) h = h * 31 + v.GetHashCode();
            }
            return h;
        }
    }
#endif

    void ClearSkin()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }
    }

    // Une todos los colliders del nivel y regresa los contornos resultantes (en coordenadas del mundo).
    List<List<Vector2>> BuildMergedOutlines()
    {
        var result = new List<List<Vector2>>();

        GameObject temp = new GameObject("_LevelSkinTemp");
        temp.hideFlags = HideFlags.HideAndDontSave;

        Rigidbody2D rb = temp.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;

        CompositeCollider2D composite = temp.AddComponent<CompositeCollider2D>();
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        composite.generationType = CompositeCollider2D.GenerationType.Manual;
        composite.offsetDistance = mergeDistance;
        composite.vertexDistance = 0.0005f;

        foreach (Collider2D col in levelRoot.GetComponentsInChildren<Collider2D>())
        {
            if (col.isTrigger || !col.enabled) continue;

            foreach (List<Vector2> poly in GetWorldPolygons(col))
            {
                GameObject piece = new GameObject("piece");
                piece.hideFlags = HideFlags.HideAndDontSave;
                piece.transform.SetParent(temp.transform, false);
                PolygonCollider2D pc = piece.AddComponent<PolygonCollider2D>();
                pc.SetPath(0, poly.ToArray());
                pc.compositeOperation = Collider2D.CompositeOperation.Merge;
            }
        }

        Physics2D.SyncTransforms();
        composite.GenerateGeometry();

        for (int p = 0; p < composite.pathCount; p++)
        {
            Vector2[] pts = new Vector2[composite.GetPathPointCount(p)];
            composite.GetPath(p, pts);
            result.Add(new List<Vector2>(pts));
        }

        DestroyImmediate(temp);
        return result;
    }

    List<List<Vector2>> GetWorldPolygons(Collider2D col)
    {
        var polys = new List<List<Vector2>>();
        Transform t = col.transform;

        if (col is BoxCollider2D box)
        {
            Vector2 h = box.size * 0.5f;
            Vector2 o = box.offset;
            var pts = new List<Vector2>
            {
                t.TransformPoint(o + new Vector2(-h.x, -h.y)),
                t.TransformPoint(o + new Vector2(-h.x,  h.y)),
                t.TransformPoint(o + new Vector2( h.x,  h.y)),
                t.TransformPoint(o + new Vector2( h.x, -h.y)),
            };
            ApplyVisualExtension(col, pts);
            polys.Add(pts);
        }
        else if (col is PolygonCollider2D poly)
        {
            for (int p = 0; p < poly.pathCount; p++)
            {
                var pts = new List<Vector2>();
                foreach (Vector2 v in poly.GetPath(p))
                    pts.Add(t.TransformPoint(v + poly.offset));
                ApplyVisualExtension(col, pts);
                polys.Add(pts);
            }
        }
        return polys;
    }

    // Alarga hacia abajo/arriba los puntos de la mitad inferior/superior del collider.
    void ApplyVisualExtension(Collider2D col, List<Vector2> pts)
    {
        bool down = System.Array.IndexOf(extendDown, col) >= 0;
        bool up = System.Array.IndexOf(extendUp, col) >= 0;
        if (!down && !up) return;

        float centerY = col.bounds.center.y;
        for (int i = 0; i < pts.Count; i++)
        {
            Vector2 v = pts[i];
            if (down && v.y < centerY) v.y -= extendDistance;
            if (up && v.y > centerY) v.y += extendDistance;
            pts[i] = v;
        }
    }

    void CreateShape(string shapeName, List<Vector2> worldPoints, float minX, float maxX, int blankIndex)
    {
        var pts = new List<Vector2>();
        foreach (Vector2 w in worldPoints)
            pts.Add(transform.InverseTransformPoint(w));

        SimplifyOutline(pts);
        if (pts.Count < 3) return;

        // Sentido horario para que el borde de roca quede hacia afuera
        if (SignedArea(pts) > 0f) pts.Reverse();

        GameObject go = new GameObject(shapeName);
        go.transform.SetParent(transform, false);

        SpriteShapeController ssc = go.AddComponent<SpriteShapeController>();
        ssc.spriteShape = caveProfile;
        ssc.fillPixelsPerUnit = 16f;
        ssc.worldSpaceUVs = true;
        ssc.splineDetail = 4;

        Spline spline = ssc.spline;
        spline.Clear();
        spline.isOpenEnded = false;
        for (int i = 0; i < pts.Count; i++)
        {
            spline.InsertPointAt(i, pts[i]);
            spline.SetTangentMode(i, ShapeTangentMode.Linear);
            spline.SetHeight(i, 1f);
            // Sin sprites de esquina: el borde continúa alrededor de la esquina en lugar de cortarse
            spline.SetCorner(i, false);
            Vector2 a = pts[i], b = pts[(i + 1) % pts.Count];
            bool endEdge = blankIndex >= 0 &&
                ((!keepLeftEdge && Mathf.Abs(a.x - minX) < EndTolerance && Mathf.Abs(b.x - minX) < EndTolerance) ||
                 (Mathf.Abs(a.x - maxX) < EndTolerance && Mathf.Abs(b.x - maxX) < EndTolerance));
            spline.SetSpriteIndex(i, endEdge ? blankIndex : PickEdgeSprite(a, b, i));
        }

        SpriteShapeRenderer ssr = go.GetComponent<SpriteShapeRenderer>();
        ssr.sortingOrder = sortingOrder;

        ssc.RefreshSpriteShape();
    }

    // El perfil tiene: [0] tira de 4 tiles (para tramos largos) y [1..n] tiles sueltos (para tramos cortos,
    // así no se aplastan). En tramos cortos se elige un tile "al azar" pero siempre el mismo para ese índice.
    // (El sprite "blank" de los extremos abiertos va al final de la lista y no entra en el sorteo.)
    int PickEdgeSprite(Vector2 a, Vector2 b, int index)
    {
        int spriteCount = caveProfile.angleRanges.Count > 0 ? caveProfile.angleRanges[0].sprites.Count : 1;
        if (BlankSpriteIndex() >= 0) spriteCount--;
        if (spriteCount <= 1) return 0;

        Sprite strip = caveProfile.angleRanges[0].sprites[0];
        float stripWidth = strip != null ? strip.bounds.size.x : 4f;
        if (Vector2.Distance(a, b) >= stripWidth) return 0;

        return 1 + (index * 7 + 3) % (spriteCount - 1);
    }

    const float EndTolerance = 0.05f;

    int BlankSpriteIndex()
    {
        if (caveProfile == null || caveProfile.angleRanges.Count == 0) return -1;
        var sprites = caveProfile.angleRanges[0].sprites;
        int last = sprites.Count - 1;
        return last > 0 && sprites[last] != null && sprites[last].name.Contains("blank") ? last : -1;
    }

    // Sprite Shape no acepta puntos demasiado cercanos. El CompositeCollider redondea las esquinas
    // con puntos muy juntos: los unimos en uno solo (esquina en punta) y quitamos puntos en línea recta.
    const float MinPointDistance = 0.2f;

    static void SimplifyOutline(List<Vector2> pts)
    {
        bool changed = true;
        while (changed && pts.Count >= 3)
        {
            changed = false;

            for (int i = 0; i < pts.Count && pts.Count >= 3; i++)
            {
                int j = (i + 1) % pts.Count;
                if (Vector2.Distance(pts[i], pts[j]) < MinPointDistance)
                {
                    pts[i] = (pts[i] + pts[j]) * 0.5f;
                    pts.RemoveAt(j);
                    changed = true;
                    break;
                }
            }

            for (int i = 0; i < pts.Count && pts.Count >= 3; i++)
            {
                Vector2 prev = pts[(i - 1 + pts.Count) % pts.Count];
                Vector2 next = pts[(i + 1) % pts.Count];
                Vector2 a = (pts[i] - prev).normalized;
                Vector2 b = (next - pts[i]).normalized;
                if (Mathf.Abs(a.x * b.y - a.y * b.x) < 0.01f && Vector2.Dot(a, b) > 0f)
                {
                    pts.RemoveAt(i);
                    changed = true;
                    break;
                }
            }
        }
    }

    static float SignedArea(List<Vector2> pts)
    {
        float a = 0f;
        for (int i = 0; i < pts.Count; i++)
        {
            Vector2 p = pts[i], q = pts[(i + 1) % pts.Count];
            a += p.x * q.y - q.x * p.y;
        }
        return a * 0.5f;
    }
}
