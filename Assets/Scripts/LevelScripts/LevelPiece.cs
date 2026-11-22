using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pieza de terreno (rampa, montaña, bloque...). La forma real es el PolygonCollider2D:
/// este script solo dibuja un relleno de color con esa forma para verla mientras armas el nivel.
/// LevelSkin oculta este relleno al generar la roca (igual que los sprites grises).
///
/// Para editar: mueve / escala / rota el objeto, o usa "Edit Collider" en el PolygonCollider2D.
/// El relleno se actualiza solo.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(PolygonCollider2D))]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class LevelPiece : MonoBehaviour
{
    [Tooltip("Color del relleno de vista previa.")]
    public Color previewColor = new Color(0.55f, 0.55f, 0.62f, 1f);

    [Tooltip("Orden de dibujado del relleno de vista previa.")]
    public int sortingOrder = -4;

    private PolygonCollider2D poly;
    private Mesh mesh;
    private int lastHash;

    void OnEnable()
    {
        poly = GetComponent<PolygonCollider2D>();
        lastHash = 0;
        Refresh();
    }

    void OnDisable()
    {
        if (mesh != null)
        {
            if (Application.isPlaying) Destroy(mesh);
            else DestroyImmediate(mesh);
            mesh = null;
        }
    }

    void OnValidate() => lastHash = 0;

    void Update()
    {
        // En el editor se revisa si cambió la forma; en juego la forma no cambia.
        if (!Application.isPlaying) Refresh();
    }

    void Refresh()
    {
        if (poly == null) return;

        int hash = ShapeHash();
        if (hash == lastHash && mesh != null) return;
        lastHash = hash;

        if (mesh == null)
        {
            mesh = new Mesh { name = "LevelPiecePreview", hideFlags = HideFlags.DontSave };
        }
        mesh.Clear();

        var verts = new List<Vector3>();
        var tris = new List<int>();
        for (int p = 0; p < poly.pathCount; p++)
        {
            Vector2[] path = poly.GetPath(p);
            int start = verts.Count;
            foreach (Vector2 v in path) verts.Add(v + poly.offset);
            foreach (int i in Triangulate(path)) tris.Add(start + i);
        }

        var colors = new Color[verts.Count];
        for (int i = 0; i < colors.Length; i++) colors[i] = previewColor;

        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.colors = colors;
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().sharedMesh = mesh;
        GetComponent<MeshRenderer>().sortingOrder = sortingOrder;
    }

    int ShapeHash()
    {
        unchecked
        {
            int h = 17;
            h = h * 31 + previewColor.GetHashCode();
            h = h * 31 + poly.offset.GetHashCode();
            for (int p = 0; p < poly.pathCount; p++)
                foreach (Vector2 v in poly.GetPath(p)) h = h * 31 + v.GetHashCode();
            return h;
        }
    }

    // Triangulación "ear clipping": funciona con formas cóncavas (sin cruces).
    static List<int> Triangulate(Vector2[] pts)
    {
        var result = new List<int>();
        int n = pts.Length;
        if (n < 3) return result;

        var idx = new List<int>(n);
        for (int i = 0; i < n; i++) idx.Add(i);

        float area = 0f;
        for (int i = 0; i < n; i++)
        {
            Vector2 a = pts[i], b = pts[(i + 1) % n];
            area += a.x * b.y - b.x * a.y;
        }
        if (area < 0f) idx.Reverse(); // trabajar en sentido antihorario

        int guard = 0;
        while (idx.Count > 3 && guard++ < 10000)
        {
            bool clipped = false;
            for (int i = 0; i < idx.Count; i++)
            {
                int i0 = idx[(i - 1 + idx.Count) % idx.Count], i1 = idx[i], i2 = idx[(i + 1) % idx.Count];
                Vector2 a = pts[i0], b = pts[i1], c = pts[i2];
                if (Cross(b - a, c - b) <= 1e-6f) continue; // vértice cóncavo

                bool inside = false;
                foreach (int j in idx)
                {
                    if (j == i0 || j == i1 || j == i2) continue;
                    if (PointInTriangle(pts[j], a, b, c)) { inside = true; break; }
                }
                if (inside) continue;

                result.Add(i0); result.Add(i1); result.Add(i2);
                idx.RemoveAt(i);
                clipped = true;
                break;
            }
            if (!clipped) break; // forma inválida: dibujar lo que se pudo
        }
        if (idx.Count == 3) { result.Add(idx[0]); result.Add(idx[1]); result.Add(idx[2]); }
        return result;
    }

    static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

    static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Cross(b - a, p - a), d2 = Cross(c - b, p - b), d3 = Cross(a - c, p - c);
        return d1 >= 0f && d2 >= 0f && d3 >= 0f;
    }
}
