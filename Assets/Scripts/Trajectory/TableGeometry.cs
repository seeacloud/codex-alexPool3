using UnityEngine;
using System.Collections.Generic;
using PoolAimTrainer.SceneObjects;

namespace PoolAimTrainer.Trajectory
{
    /// <summary>
    /// Cushion + pocket geometry derived from PocketMarker positions in the scene.
    /// Models the table as:
    ///   - 6 pocket mouths between jaw tips, with rounded jaw clearances
    ///   - 6 linear cushion segments between pockets, with a jaw gap at each end
    ///
    /// Following the pooltool approach (Kiefl 2020):
    ///   https://ekiefl.github.io/2020/12/20/pooltool-alg/
    /// The ball sphere swept along targetDir — for each cushion segment we solve
    /// the earliest time t such that distance(ballCenter(t), segment) = R.
    /// For each pocket mouth we solve the earliest clean crossing of the usable
    /// opening after shrinking it by the ball radius.
    /// Whichever fires first is the outcome.
    /// </summary>
    public static class TableGeometry
    {
        const float RAIL_TOLERANCE = 0.1f; // meters — how close to half-extent counts as "on rail"

        public struct CushionSegment
        {
            public Vector3 p1;
            public Vector3 p2;
        }

        public struct PocketDisc
        {
            public Vector3 center;
            public float radius;
            public int index; // index into table.Pockets
        }

        public struct PocketMouth
        {
            public Vector3 p1;
            public Vector3 p2;
            public Vector3 center;
            public Vector3 jawCenter1;
            public Vector3 jawCenter2;
            public float jawRadius;
            public int index;
            public bool isCorner;
        }

        /// <summary>
        /// Build cushion segments from the table's pocket markers. Pockets are
        /// expected to be arranged in a rectangle with 4 corner + 2 middle pockets
        /// (snooker / 8-ball layout). For each pair of adjacent pockets along the
        /// same rail, we insert one linear cushion segment between them, shortened
        /// at both ends by jawGap so the ball can enter the pocket.
        /// </summary>
        public static List<CushionSegment> BuildCushionSegments(TableController table, float jawGap)
        {
            var result = new List<CushionSegment>();
            if (table == null || table.Pockets == null || table.Pockets.Count < 2) return result;

            // Classify pockets by which rail they belong to: top (+Z), bottom (-Z),
            // left (-X), right (+X). A middle pocket sits on exactly one rail.
            float halfL = table.playfieldHalfLength;
            float halfW = table.playfieldHalfWidth;

            // Sort each rail's pockets by position along that rail.
            var topPockets = new List<PocketMarker>();     // +Z rail (sort by X)
            var bottomPockets = new List<PocketMarker>();  // -Z rail (sort by X)
            var leftPockets = new List<PocketMarker>();    // -X rail (sort by Z)
            var rightPockets = new List<PocketMarker>();   // +X rail (sort by Z)

            foreach (var p in table.Pockets)
            {
                Vector3 c = p.Position;
                // Top rail?
                if (halfW - c.z < RAIL_TOLERANCE) topPockets.Add(p);
                else if (c.z + halfW < RAIL_TOLERANCE) bottomPockets.Add(p);
                // (Left/right rails — corners also fit here, but they're
                // already in top/bottom. We only add side-only pockets.)
                // Corners count in 2 rails. We add left/right only if not already
                // in top/bottom and they're at X extremum.
                if (halfL - c.x < RAIL_TOLERANCE) rightPockets.Add(p);
                else if (c.x + halfL < RAIL_TOLERANCE) leftPockets.Add(p);
            }

            topPockets.Sort((a, b) => a.Position.x.CompareTo(b.Position.x));
            bottomPockets.Sort((a, b) => a.Position.x.CompareTo(b.Position.x));
            leftPockets.Sort((a, b) => a.Position.z.CompareTo(b.Position.z));
            rightPockets.Sort((a, b) => a.Position.z.CompareTo(b.Position.z));

            AddSegmentsAlongRail(result, topPockets, jawGap, RailSide.Top, halfL, halfW);
            AddSegmentsAlongRail(result, bottomPockets, jawGap, RailSide.Bottom, halfL, halfW);
            AddSegmentsAlongRail(result, leftPockets, jawGap, RailSide.Left, halfL, halfW);
            AddSegmentsAlongRail(result, rightPockets, jawGap, RailSide.Right, halfL, halfW);

            return result;
        }

        enum RailSide
        {
            Top,
            Bottom,
            Left,
            Right,
        }

        static void AddSegmentsAlongRail(
            List<CushionSegment> output,
            List<PocketMarker> railPockets,
            float jawGap,
            RailSide side,
            float halfLength,
            float halfWidth)
        {
            if (railPockets.Count < 2) return;
            for (int i = 0; i < railPockets.Count - 1; i++)
            {
                Vector3 a = railPockets[i].Position;
                Vector3 b = railPockets[i + 1].Position;
                // Shorten both ends by jawGap along the rail axis direction.
                Vector3 dir = (b - a).normalized;
                Vector3 p1 = a + dir * jawGap;
                Vector3 p2 = b - dir * jawGap;
                ProjectOntoCushionNose(ref p1, side, halfLength, halfWidth);
                ProjectOntoCushionNose(ref p2, side, halfLength, halfWidth);
                p1.y = 0f; p2.y = 0f;
                if ((p2 - p1).sqrMagnitude > 1e-8f)
                {
                    output.Add(new CushionSegment { p1 = p1, p2 = p2 });
                }
            }
        }

        static void ProjectOntoCushionNose(ref Vector3 point, RailSide side, float halfLength, float halfWidth)
        {
            switch (side)
            {
                case RailSide.Top:
                    point.z = halfWidth;
                    break;
                case RailSide.Bottom:
                    point.z = -halfWidth;
                    break;
                case RailSide.Left:
                    point.x = -halfLength;
                    break;
                case RailSide.Right:
                    point.x = halfLength;
                    break;
            }
        }

        public static List<PocketDisc> BuildPocketDiscs(TableController table, float pocketRadius)
        {
            var result = new List<PocketDisc>();
            if (table == null || table.Pockets == null) return result;
            for (int i = 0; i < table.Pockets.Count; i++)
            {
                Vector3 c = table.Pockets[i].Position; c.y = 0f;
                result.Add(new PocketDisc { center = c, radius = pocketRadius, index = i });
            }
            return result;
        }

        public static List<PocketMouth> BuildPocketMouths(TableController table, float jawGap)
        {
            var result = new List<PocketMouth>();
            if (table == null || table.Pockets == null) return result;
            for (int i = 0; i < table.Pockets.Count; i++)
                result.Add(BuildPocketMouth(table, i, jawGap));
            return result;
        }

        public static Vector3 GetPottingPoint(TableController table, int pocketIndex, Vector3 ballCenter, float jawGap)
        {
            if (table == null || table.Pockets == null || pocketIndex < 0 || pocketIndex >= table.Pockets.Count)
                return Vector3.zero;

            PocketMouth mouth = BuildPocketMouth(table, pocketIndex, jawGap);
            Vector3 ball = ballCenter; ball.y = 0f;

            // If the ball is already inside the jaws, the pocket center is the
            // point of no return. Otherwise aim at the center of the mouth opening.
            if (AreOnSameSide(mouth.p1, mouth.p2, ball, mouth.center))
                return mouth.center;

            Vector3 point = (mouth.p1 + mouth.p2) * 0.5f;
            point.y = 0f;
            return point;
        }

        public static PocketMouth BuildPocketMouth(TableController table, int pocketIndex, float jawGap)
        {
            Vector3 pocketCenter = table.Pockets[pocketIndex].Position;
            pocketCenter.y = 0f;

            float halfL = table.playfieldHalfLength;
            float halfW = table.playfieldHalfWidth;
            float sx = pocketCenter.x >= 0f ? 1f : -1f;
            float sz = pocketCenter.z >= 0f ? 1f : -1f;
            bool nearX = Mathf.Abs(pocketCenter.x) >= halfL - RAIL_TOLERANCE;
            bool nearZ = Mathf.Abs(pocketCenter.z) >= halfW - RAIL_TOLERANCE;

            Vector3 p1;
            Vector3 p2;
            bool isCorner = nearX && nearZ;
            float jawRadius = isCorner ? 0.02095f : 0.00795f;
            if (isCorner)
            {
                p1 = new Vector3(sx * Mathf.Max(0f, halfL - jawGap), 0f, sz * halfW);
                p2 = new Vector3(sx * halfL, 0f, sz * Mathf.Max(0f, halfW - jawGap));
            }
            else if (nearZ)
            {
                p1 = new Vector3(pocketCenter.x - jawGap, 0f, sz * halfW);
                p2 = new Vector3(pocketCenter.x + jawGap, 0f, sz * halfW);
            }
            else if (nearX)
            {
                p1 = new Vector3(sx * halfL, 0f, pocketCenter.z - jawGap);
                p2 = new Vector3(sx * halfL, 0f, pocketCenter.z + jawGap);
            }
            else
            {
                p1 = pocketCenter + Vector3.left * jawGap;
                p2 = pocketCenter + Vector3.right * jawGap;
            }

            Vector3 jawCenter1 = OffsetJawCenterOutward(p1, nearX, nearZ, sx, sz, jawRadius, halfL, halfW);
            Vector3 jawCenter2 = OffsetJawCenterOutward(p2, nearX, nearZ, sx, sz, jawRadius, halfL, halfW);

            return new PocketMouth
            {
                p1 = p1,
                p2 = p2,
                center = pocketCenter,
                jawCenter1 = jawCenter1,
                jawCenter2 = jawCenter2,
                jawRadius = jawRadius,
                index = pocketIndex,
                isCorner = isCorner,
            };
        }

        static Vector3 OffsetJawCenterOutward(
            Vector3 jawPoint, bool nearX, bool nearZ, float sx, float sz, float radius,
            float halfLength, float halfWidth)
        {
            Vector3 center = jawPoint;
            if (nearX && !nearZ) center.x += sx * radius;
            else if (nearZ && !nearX) center.z += sz * radius;
            else if (nearX && nearZ)
            {
                float distanceToTopBottomRail = Mathf.Abs(Mathf.Abs(jawPoint.z) - halfWidth);
                float distanceToLeftRightRail = Mathf.Abs(Mathf.Abs(jawPoint.x) - halfLength);
                if (distanceToTopBottomRail <= distanceToLeftRightRail) center.z += sz * radius;
                else center.x += sx * radius;
            }

            center.y = 0f;
            return center;
        }

        static bool AreOnSameSide(Vector3 a, Vector3 b, Vector3 p, Vector3 q)
        {
            Vector3 ab = b - a;
            Vector3 ap = p - a;
            Vector3 aq = q - a;
            float cp = ab.x * ap.z - ab.z * ap.x;
            float cq = ab.x * aq.z - ab.z * aq.x;
            return cp * cq >= 0f;
        }
    }
}
