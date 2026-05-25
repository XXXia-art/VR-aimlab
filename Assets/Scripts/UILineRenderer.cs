using UnityEngine;
using UnityEngine.UI;

namespace VRAimLab
{
    public class UILineRenderer : Graphic
    {
        public Vector2[] points;
        public float thickness = 4f;
        public bool connectPoints = true;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (points == null || points.Length < 2) return;

            for (int i = 0; i < points.Length - 1; i++)
            {
                DrawLine(vh, points[i], points[i + 1]);
            }
        }

        void DrawLine(VertexHelper vh, Vector2 start, Vector2 end)
        {
            Vector2 dir = (end - start);
            float len = dir.magnitude;
            if (len < 0.001f) return;
            dir.Normalize();
            Vector2 normal = new Vector2(-dir.y, dir.x) * thickness * 0.5f;

            int idx = vh.currentVertCount;
            UIVertex vert = UIVertex.simpleVert;
            vert.color = color;

            vert.position = start - normal;
            vh.AddVert(vert);
            vert.position = start + normal;
            vh.AddVert(vert);
            vert.position = end + normal;
            vh.AddVert(vert);
            vert.position = end - normal;
            vh.AddVert(vert);

            vh.AddTriangle(idx, idx + 1, idx + 2);
            vh.AddTriangle(idx, idx + 2, idx + 3);
        }

        public void SetPoints(Vector2[] newPoints)
        {
            points = newPoints;
            SetVerticesDirty();
        }
    }
}
