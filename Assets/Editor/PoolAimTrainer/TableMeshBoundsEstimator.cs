#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PoolAimTrainer.EditorTools
{
    internal static class TableMeshBoundsEstimator
    {
        public static bool TryEstimateBumperNoseBounds(
            Transform tableRoot,
            out float halfLength,
            out float halfWidth)
        {
            halfLength = 0f;
            halfWidth = 0f;

            Transform bumper = tableRoot != null ? tableRoot.Find("Bumper") : null;
            if (bumper == null) return false;

            var renderer = bumper.GetComponent<Renderer>();
            if (renderer == null) return false;

            float maxX = renderer.bounds.max.x;
            float maxZ = renderer.bounds.max.z;
            if (!TryEstimatePositiveNoseCoordinate(bumper, axis: 0, maxX - 0.12f, maxX + 0.01f, out halfLength))
                return false;
            if (!TryEstimatePositiveNoseCoordinate(bumper, axis: 2, maxZ - 0.14f, maxZ + 0.01f, out halfWidth))
                return false;

            return true;
        }

        static bool TryEstimatePositiveNoseCoordinate(
            Transform meshRoot,
            int axis,
            float min,
            float max,
            out float nose)
        {
            nose = 0f;
            var filter = meshRoot.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) return false;

            var values = new SortedSet<float>();
            foreach (Vector3 local in filter.sharedMesh.vertices)
            {
                Vector3 world = meshRoot.TransformPoint(local);
                float raw = axis == 0 ? world.x : world.z;
                float rounded = Mathf.Round(raw * 1000f) / 1000f;
                if (rounded >= min && rounded <= max)
                    values.Add(rounded);
            }

            var descending = values.OrderByDescending(v => v).ToList();
            if (descending.Count < 3) return false;

            float bestGap = 0f;
            nose = descending[0];
            for (int i = 0; i < descending.Count - 1; i++)
            {
                float gap = descending[i] - descending[i + 1];
                if (gap > bestGap)
                {
                    bestGap = gap;
                    nose = descending[i];
                }
            }

            return bestGap > 0.01f;
        }
    }
}
#endif
