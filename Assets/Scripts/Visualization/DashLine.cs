using UnityEngine;

namespace PoolAimTrainer.Visualization
{
    public abstract class DashLine : MonoBehaviour
    {
        [Tooltip("线宽（米）")]
        public float lineWidth = 0.0005f;

        // Hard-coded dash style (not serialized) so all lines stay consistent
        // regardless of stale scene-serialized values.
        const float dashLength = 0.015f;
        const float gapLength = 0.015f;

        protected LineRenderer CreateDashLineRenderer(string name, Color color)
        {
            color.a = 1f;
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.numCornerVertices = 0;
            lr.numCapVertices = 0;
            lr.alignment = LineAlignment.View;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.startColor = color;
            lr.endColor = color;
            lr.textureMode = LineTextureMode.Tile;
            lr.textureScale = new Vector2(1f / (dashLength + gapLength), 1f);
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.mainTexture = MakeDashTexture(color);
            mat.color = Color.white;
            lr.sharedMaterial = mat;
            lr.enabled = false;
            return lr;
        }

        Texture2D MakeDashTexture(Color color)
        {
            int totalPx = 32;
            int dashPx = Mathf.RoundToInt((dashLength / (dashLength + gapLength)) * totalPx);
            var tex = new Texture2D(totalPx, 1, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Point;
            Color solid = new Color(color.r, color.g, color.b, 1f);
            for (int x = 0; x < totalPx; x++)
                tex.SetPixel(x, 0, x < dashPx ? solid : Color.clear);
            tex.Apply();
            return tex;
        }

        protected static void SetLine(LineRenderer lr, Vector3 a, Vector3 b)
        {
            if (lr == null) return;
            lr.enabled = true;
            lr.positionCount = 2;
            lr.SetPosition(0, a);
            lr.SetPosition(1, b);
        }

        protected static void SetPolyline(LineRenderer lr, Vector3[] points)
        {
            if (lr == null) return;
            if (points == null || points.Length < 2)
            {
                lr.enabled = false;
                return;
            }
            lr.enabled = true;
            lr.positionCount = points.Length;
            lr.SetPositions(points);
        }

        protected static void HideLine(LineRenderer lr)
        {
            if (lr != null) lr.enabled = false;
        }
    }
}
