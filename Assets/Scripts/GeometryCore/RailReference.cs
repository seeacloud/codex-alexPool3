using UnityEngine;

namespace PoolAimTrainer.GeometryCore
{
    public static class RailReference
    {
        public static Vector3[] GenerateRailPoints(Vector3 railStart, Vector3 railEnd, int count)
        {
            var pts = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float t = (i + 1f) / (count + 1f);
                pts[i] = Vector3.Lerp(railStart, railEnd, t);
            }
            return pts;
        }

        public static int FindClosestIndex(Vector3[] points, Vector3 query)
        {
            int best = 0;
            float bestDist = float.MaxValue;
            for (int i = 0; i < points.Length; i++)
            {
                float d = (points[i] - query).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = i; }
            }
            return best;
        }
    }
}
