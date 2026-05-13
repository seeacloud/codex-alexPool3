using UnityEngine;
using System.Collections.Generic;

namespace PoolAimTrainer.Trajectory
{
    public static class TrajectorySnapshotSampler
    {
        public static Vector3[] SampleInterior(Vector3[] path, int count)
        {
            if (path == null || path.Length < 2 || count <= 0)
                return new Vector3[0];

            float total = PathLength(path);
            if (total < 1e-6f)
                return new Vector3[0];

            var result = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float distance = total * (i + 1) / (count + 1);
                result[i] = PointAtDistance(path, distance);
            }
            return result;
        }

        public static Vector3[] SampleInteriorAvoiding(Vector3[] path, int count, float minDistance, Vector3[] avoidPoints)
        {
            if (path == null || path.Length < 2 || count <= 0)
                return new Vector3[0];

            float total = PathLength(path);
            if (total < 1e-6f)
                return new Vector3[0];

            float margin = Mathf.Clamp(minDistance, 0f, total * 0.45f);
            float usableStart = margin;
            float usableEnd = total - margin;
            if (usableEnd < usableStart)
                return new Vector3[0];

            var accepted = new List<Vector3>(count);
            int attempts = Mathf.Max(count * 8, 16);
            for (int i = 0; i < attempts && accepted.Count < count; i++)
            {
                float u = (i + 1f) / (attempts + 1f);
                float d = Mathf.Lerp(usableStart, usableEnd, u);
                Vector3 p = PointAtDistance(path, d);
                if (IsFarEnough(p, avoidPoints, minDistance)
                    && IsFarEnough(p, accepted, minDistance))
                {
                    accepted.Add(p);
                }
            }

            return accepted.ToArray();
        }

        public static Vector3 PointAtDistance(Vector3[] path, float distance)
        {
            if (path == null || path.Length == 0)
                return Vector3.zero;
            if (path.Length == 1 || distance <= 0f)
                return path[0];

            float remaining = distance;
            for (int i = 0; i < path.Length - 1; i++)
            {
                Vector3 a = path[i];
                Vector3 b = path[i + 1];
                float len = Vector3.Distance(a, b);
                if (len < 1e-6f) continue;
                if (remaining <= len)
                    return Vector3.Lerp(a, b, remaining / len);
                remaining -= len;
            }

            return path[path.Length - 1];
        }

        static float PathLength(Vector3[] path)
        {
            float total = 0f;
            for (int i = 0; i < path.Length - 1; i++)
                total += Vector3.Distance(path[i], path[i + 1]);
            return total;
        }

        static bool IsFarEnough(Vector3 p, Vector3[] avoidPoints, float minDistance)
        {
            if (avoidPoints == null || minDistance <= 0f) return true;
            float minSqr = minDistance * minDistance;
            for (int i = 0; i < avoidPoints.Length; i++)
            {
                Vector3 d = p - avoidPoints[i];
                d.y = 0f;
                if (d.sqrMagnitude < minSqr) return false;
            }
            return true;
        }

        static bool IsFarEnough(Vector3 p, List<Vector3> avoidPoints, float minDistance)
        {
            if (avoidPoints == null || minDistance <= 0f) return true;
            float minSqr = minDistance * minDistance;
            for (int i = 0; i < avoidPoints.Count; i++)
            {
                Vector3 d = p - avoidPoints[i];
                d.y = 0f;
                if (d.sqrMagnitude < minSqr) return false;
            }
            return true;
        }
    }
}
