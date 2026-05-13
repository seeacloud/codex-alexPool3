using System.Collections.Generic;
using PoolAimTrainer.Core;
using PoolAimTrainer.SceneObjects;
using PoolAimTrainer.Trajectory;
using UnityEngine;

namespace PoolAimTrainer.Visualization
{
    public class PottingToleranceFanRenderer : DashLine
    {
        const float TARGET_BOUNDARY_SEARCH_DEGREES = 89f;
        const float TARGET_BOUNDARY_STEP_DEGREES = 0.25f;
        const int BOUNDARY_SEARCH_ITERATIONS = 14;

        [Tooltip("已废弃：容错区域不再为了可见性而扩张到最小角度")]
        public float minHalfAngleDegrees = 0f;
        [Tooltip("最大显示半扇形角度，避免极端位置下覆盖过多桌面")]
        public float maxHalfAngleDegrees = 40f;
        [Tooltip("扇形边缘采样段数")]
        public int segmentCount = 32;
        public Color fanColor = new Color(0.2f, 0.9f, 1f, 0.18f);
        [Tooltip("相对桌面的向上偏移，避免 z-fighting")]
        public float surfaceYOffset = 0.004f;
        public Color lowerTargetPathColor = new Color(0.2f, 0.95f, 1f, 1f);
        public Color upperTargetPathColor = new Color(0.2f, 0.95f, 1f, 1f);

        GameObject meshObject;
        MeshFilter meshFilter;
        MeshRenderer meshRenderer;
        Mesh mesh;
        LineRenderer lowerTargetPath;
        LineRenderer upperTargetPath;

        public struct FanGeometry
        {
            public Vector3 origin;
            public Vector3 leftDir;
            public Vector3 rightDir;
            public Vector3 idealDir;
            public Vector3 targetBallCenter;
            public Vector3 lowerTargetDir;
            public Vector3 upperTargetDir;
        }

        public void Show(
            Vector3 cueBallCenter, Vector3 targetBallCenter, PocketMarker pocket,
            float ballRadius, TableController table, float jawGap)
        {
            if (!TryComputeFanGeometry(
                    cueBallCenter, targetBallCenter, pocket, ballRadius, table, jawGap,
                    minHalfAngleDegrees, maxHalfAngleDegrees, out var geometry))
            {
                Hide();
                return;
            }

            EnsureObjects();
            BuildMesh(geometry, table);
            meshObject.SetActive(ReferenceLineVisibility.IsLayerVisible(ReferenceVisualLayer.ToleranceFanArea));
            ShowTargetBoundaryPath(
                lowerTargetPath, geometry.targetBallCenter, geometry.lowerTargetDir,
                table, ReferenceVisualLayer.ToleranceLowerTargetPath);
            ShowTargetBoundaryPath(
                upperTargetPath, geometry.targetBallCenter, geometry.upperTargetDir,
                table, ReferenceVisualLayer.ToleranceUpperTargetPath);
        }

        public void Hide()
        {
            if (meshObject != null) meshObject.SetActive(false);
            HideLine(lowerTargetPath);
            HideLine(upperTargetPath);
        }

        public static bool TryComputeFanGeometry(
            Vector3 cueBallCenter, Vector3 targetBallCenter, PocketMarker pocket,
            float ballRadius, TableController table, float jawGap,
            float minHalfAngleDegrees, float maxHalfAngleDegrees,
            out FanGeometry geometry)
        {
            geometry = default;
            if (table == null || pocket == null || table.Pockets == null || ballRadius <= 0f)
                return false;
            if (FlatDistanceSqr(cueBallCenter, targetBallCenter) < 1e-8f)
                return false;

            int pocketIndex = table.Pockets.IndexOf(pocket);
            if (pocketIndex < 0)
                return false;

            var mouth = TableGeometry.BuildPocketMouth(table, pocketIndex, jawGap);
            var cushions = TableGeometry.BuildCushionSegments(table, jawGap);
            Vector3 pottingPoint = TableGeometry.GetPottingPoint(table, pocketIndex, targetBallCenter, jawGap);
            Vector3 idealTargetDir = pottingPoint - targetBallCenter;
            idealTargetDir.y = 0f;
            if (idealTargetDir.sqrMagnitude < 1e-8f)
                return false;
            idealTargetDir.Normalize();

            if (!CanTargetReachPocket(targetBallCenter, idealTargetDir, cushions, mouth, ballRadius)
                || !TryCueDirectionForTargetOutbound(cueBallCenter, targetBallCenter, idealTargetDir, ballRadius, out var idealDir)
                || !TryFindPocketableBoundaryTargetDir(targetBallCenter, idealTargetDir, -1f, cushions, mouth, ballRadius, out var lowerTargetDir)
                || !TryFindPocketableBoundaryTargetDir(targetBallCenter, idealTargetDir, 1f, cushions, mouth, ballRadius, out var upperTargetDir)
                || !TryCueDirectionForTargetOutbound(cueBallCenter, targetBallCenter, lowerTargetDir, ballRadius, out var leftDir)
                || !TryCueDirectionForTargetOutbound(cueBallCenter, targetBallCenter, upperTargetDir, ballRadius, out var rightDir))
            {
                return false;
            }

            float angle1 = Vector3.SignedAngle(idealDir, leftDir, Vector3.up);
            float angle2 = Vector3.SignedAngle(idealDir, rightDir, Vector3.up);
            float leftAngle = Mathf.Min(angle1, angle2);
            float rightAngle = Mathf.Max(angle1, angle2);

            float maxHalf = Mathf.Max(0f, maxHalfAngleDegrees);
            leftAngle = Mathf.Clamp(leftAngle, -maxHalf, 0f);
            rightAngle = Mathf.Clamp(rightAngle, 0f, maxHalf);

            geometry = new FanGeometry
            {
                origin = WithHeight(cueBallCenter, ballRadius),
                idealDir = idealDir,
                leftDir = (Quaternion.AngleAxis(leftAngle, Vector3.up) * idealDir).normalized,
                rightDir = (Quaternion.AngleAxis(rightAngle, Vector3.up) * idealDir).normalized,
                targetBallCenter = WithHeight(targetBallCenter, ballRadius),
                lowerTargetDir = lowerTargetDir,
                upperTargetDir = upperTargetDir,
            };
            return true;
        }

        public static Vector3 ClipRayToPlayfield(Vector3 start, Vector3 dir, TableController table)
        {
            if (table == null)
                return start;

            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-8f)
                return start;
            dir.Normalize();

            float halfLength = table.playfieldHalfLength;
            float halfWidth = table.playfieldHalfWidth;
            float best = float.MaxValue;
            if (Mathf.Abs(dir.x) > 1e-6f)
            {
                float tx = (dir.x > 0f ? halfLength - start.x : -halfLength - start.x) / dir.x;
                if (tx > 0f && tx < best) best = tx;
            }
            if (Mathf.Abs(dir.z) > 1e-6f)
            {
                float tz = (dir.z > 0f ? halfWidth - start.z : -halfWidth - start.z) / dir.z;
                if (tz > 0f && tz < best) best = tz;
            }

            if (best == float.MaxValue)
                best = 0f;

            Vector3 hit = start + dir * best;
            hit.x = Mathf.Clamp(hit.x, -halfLength, halfLength);
            hit.z = Mathf.Clamp(hit.z, -halfWidth, halfWidth);
            return hit;
        }

        void EnsureObjects()
        {
            if (meshObject == null)
            {
                meshObject = new GameObject("PottingToleranceFanMesh");
                meshObject.transform.SetParent(transform, false);
                meshFilter = meshObject.AddComponent<MeshFilter>();
                meshRenderer = meshObject.AddComponent<MeshRenderer>();
                mesh = new Mesh { name = "PottingToleranceFanMesh" };
                meshFilter.sharedMesh = mesh;
                meshRenderer.sharedMaterial = CreateTransparentMaterial(fanColor);
            }

            if (meshRenderer.sharedMaterial != null)
                meshRenderer.sharedMaterial.color = fanColor;

            if (lowerTargetPath == null)
                lowerTargetPath = CreateTargetBoundaryPath("LR_ToleranceLowerTargetPath", lowerTargetPathColor);
            if (upperTargetPath == null)
                upperTargetPath = CreateTargetBoundaryPath("LR_ToleranceUpperTargetPath", upperTargetPathColor);
        }

        void BuildMesh(FanGeometry geometry, TableController table)
        {
            int segments = Mathf.Clamp(segmentCount, 2, 128);
            int vertexCount = segments + 2;
            var vertices = new Vector3[vertexCount];
            var triangles = new int[segments * 3];

            Vector3 origin = geometry.origin;
            origin.y += surfaceYOffset;
            vertices[0] = transform.InverseTransformPoint(origin);

            float leftAngle = Vector3.SignedAngle(geometry.idealDir, geometry.leftDir, Vector3.up);
            float rightAngle = Vector3.SignedAngle(geometry.idealDir, geometry.rightDir, Vector3.up);
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float angle = Mathf.Lerp(leftAngle, rightAngle, t);
                Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * geometry.idealDir;
                Vector3 end = ClipRayToPlayfield(geometry.origin, dir, table);
                end.y = origin.y;
                vertices[i + 1] = transform.InverseTransformPoint(end);
            }

            for (int i = 0; i < segments; i++)
            {
                int tri = i * 3;
                triangles[tri] = 0;
                triangles[tri + 1] = i + 1;
                triangles[tri + 2] = i + 2;
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
        }

        LineRenderer CreateTargetBoundaryPath(string lineName, Color color)
        {
            var line = CreateDashLineRenderer(lineName, color);
            if (line.GetComponent<LineWidthCompensator>() == null)
                line.gameObject.AddComponent<LineWidthCompensator>();
            return line;
        }

        void ShowTargetBoundaryPath(
            LineRenderer line, Vector3 targetBallCenter, Vector3 targetDir,
            TableController table, ReferenceVisualLayer layer)
        {
            if (!ReferenceLineVisibility.IsLayerVisible(layer))
            {
                HideLine(line);
                return;
            }

            Vector3 start = targetBallCenter;
            Vector3 end = ClipRayToPlayfield(targetBallCenter, targetDir, table);
            start.y += surfaceYOffset;
            end.y = start.y;
            SetLine(line, start, end);
        }

        static bool TryFindPocketableBoundaryTargetDir(
            Vector3 targetBallCenter, Vector3 idealTargetDir, float side,
            List<TableGeometry.CushionSegment> cushions, TableGeometry.PocketMouth mouth,
            float ballRadius, out Vector3 boundaryDir)
        {
            boundaryDir = idealTargetDir;
            float lastGood = 0f;
            float firstBad = TARGET_BOUNDARY_SEARCH_DEGREES;

            for (float probe = TARGET_BOUNDARY_STEP_DEGREES;
                 probe <= TARGET_BOUNDARY_SEARCH_DEGREES + 1e-4f;
                 probe += TARGET_BOUNDARY_STEP_DEGREES)
            {
                Vector3 dir = (Quaternion.AngleAxis(side * probe, Vector3.up) * idealTargetDir).normalized;
                if (CanTargetReachPocket(targetBallCenter, dir, cushions, mouth, ballRadius))
                {
                    lastGood = probe;
                    continue;
                }

                firstBad = probe;
                break;
            }

            for (int i = 0; i < BOUNDARY_SEARCH_ITERATIONS; i++)
            {
                float mid = (lastGood + firstBad) * 0.5f;
                Vector3 dir = (Quaternion.AngleAxis(side * mid, Vector3.up) * idealTargetDir).normalized;
                if (CanTargetReachPocket(targetBallCenter, dir, cushions, mouth, ballRadius))
                    lastGood = mid;
                else
                    firstBad = mid;
            }

            boundaryDir = (Quaternion.AngleAxis(side * lastGood, Vector3.up) * idealTargetDir).normalized;
            return true;
        }

        static bool CanTargetReachPocket(
            Vector3 targetBallCenter, Vector3 targetDir,
            List<TableGeometry.CushionSegment> cushions, TableGeometry.PocketMouth mouth,
            float ballRadius)
        {
            targetDir.y = 0f;
            if (targetDir.sqrMagnitude < 1e-8f)
                return false;
            targetDir.Normalize();

            float tRail = float.MaxValue;
            if (cushions != null)
            {
                foreach (var seg in cushions)
                {
                    float t = ShotSimulator.BallLineSegmentCollisionTime(
                        targetBallCenter, targetDir, seg.p1, seg.p2, ballRadius);
                    if (t < tRail) tRail = t;
                }
            }

            float tPocket = ShotSimulator.RayMouthEntryTime(targetBallCenter, targetDir, mouth, ballRadius);
            return tPocket < tRail;
        }

        static bool TryCueDirectionForTargetOutbound(
            Vector3 cueBallCenter, Vector3 targetBallCenter, Vector3 outbound,
            float ballRadius, out Vector3 cueDir)
        {
            cueDir = Vector3.zero;
            outbound.y = 0f;
            if (outbound.sqrMagnitude < 1e-8f)
                return false;

            outbound.Normalize();
            Vector3 ghost = targetBallCenter - outbound * (2f * ballRadius);
            Vector3 dir = ghost - cueBallCenter;
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-8f)
                return false;

            cueDir = dir.normalized;
            return true;
        }

        static float FlatDistanceSqr(Vector3 a, Vector3 b)
        {
            Vector3 d = a - b;
            d.y = 0f;
            return d.sqrMagnitude;
        }

        static Vector3 WithHeight(Vector3 v, float y)
        {
            v.y = y;
            return v;
        }

        static Material CreateTransparentMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            var mat = new Material(shader);
            ConfigureTransparentMaterial(mat);
            mat.color = color;
            return mat;
        }

        static void ConfigureTransparentMaterial(Material mat)
        {
            if (mat == null) return;
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
    }
}
