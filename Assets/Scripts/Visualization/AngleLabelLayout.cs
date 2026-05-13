using System.Collections.Generic;
using UnityEngine;

namespace PoolAimTrainer.Visualization
{
    public static class AngleLabelLayout
    {
        const float DefaultPaddingPx = 4f;
        const float DefaultStepPx = 18f;
        const int DefaultMaxSteps = 8;

        static readonly List<Rect> frameOccupied = new List<Rect>();
        static readonly Dictionary<string, Rect> frameOccupiedByKey = new Dictionary<string, Rect>();
        static int occupiedFrame = -1;

        public static Vector2 ReserveForCurrentFrame(Vector2 preferred, Vector2 size, Vector2 direction)
        {
            ResetIfNewFrame();
            return PlaceAvoidingOverlap(preferred, size, direction, frameOccupied);
        }

        public static Vector2 ReserveForCurrentFrame(
            string key, Vector2 preferred, Vector2 size, Vector2 direction)
        {
            if (string.IsNullOrEmpty(key))
                return ReserveForCurrentFrame(preferred, size, direction);

            ResetIfNewFrame();

            frameOccupied.Clear();
            foreach (var pair in frameOccupiedByKey)
            {
                if (pair.Key != key)
                    frameOccupied.Add(pair.Value);
            }

            Vector2 anchor = PlaceAvoidingOverlap(preferred, size, direction, frameOccupied);
            frameOccupiedByKey[key] = CenteredRect(anchor, new Vector2(
                Mathf.Max(1f, size.x + DefaultPaddingPx * 2f),
                Mathf.Max(1f, size.y + DefaultPaddingPx * 2f)));
            return anchor;
        }

        static void ResetIfNewFrame()
        {
            int frame = Time.frameCount;
            if (frame != occupiedFrame)
            {
                frameOccupied.Clear();
                frameOccupiedByKey.Clear();
                occupiedFrame = frame;
            }
        }

        public static Vector2 PlaceAvoidingOverlap(
            Vector2 preferred,
            Vector2 size,
            Vector2 direction,
            IList<Rect> occupied,
            float paddingPx = DefaultPaddingPx,
            float stepPx = DefaultStepPx,
            int maxSteps = DefaultMaxSteps)
        {
            if (occupied == null)
                return preferred;

            Vector2 paddedSize = new Vector2(
                Mathf.Max(1f, size.x + paddingPx * 2f),
                Mathf.Max(1f, size.y + paddingPx * 2f));

            Vector2 candidate = preferred;
            Rect rect = CenteredRect(candidate, paddedSize);
            if (!OverlapsAny(rect, occupied))
            {
                occupied.Add(rect);
                return candidate;
            }

            Vector2 side = new Vector2(-direction.y, direction.x);
            if (side.sqrMagnitude < 1e-4f)
                side = Vector2.up;
            else
                side.Normalize();

            for (int i = 1; i <= maxSteps; i++)
            {
                for (int sign = 1; sign >= -1; sign -= 2)
                {
                    candidate = preferred + side * (stepPx * i * sign);
                    rect = CenteredRect(candidate, paddedSize);
                    if (!OverlapsAny(rect, occupied))
                    {
                        occupied.Add(rect);
                        return candidate;
                    }
                }
            }

            occupied.Add(rect);
            return candidate;
        }

        static Rect CenteredRect(Vector2 center, Vector2 size)
        {
            return new Rect(center - size * 0.5f, size);
        }

        static bool OverlapsAny(Rect rect, IList<Rect> occupied)
        {
            for (int i = 0; i < occupied.Count; i++)
            {
                if (rect.Overlaps(occupied[i]))
                    return true;
            }

            return false;
        }
    }
}
