using UnityEngine;
using System.Collections.Generic;
using PoolAimTrainer.SceneObjects;

namespace PoolAimTrainer.Trajectory
{
    /// <summary>
    /// Pure-geometry shot predictor. Uses pooltool-style cushion segment collision
    /// plus pocket-mouth entry geometry (Kiefl 2020):
    ///   https://ekiefl.github.io/2020/12/20/pooltool-alg/
    ///
    /// For each cushion segment: compute earliest time t such that the ball center
    /// (traveling along aimDir) is at distance R from the segment, with the closest
    /// point lying on the segment (not beyond either endpoint).
    ///
    /// For each pocket mouth: compute earliest time t such that the ball center
    /// crosses the radius-shrunken mouth opening without first entering a rounded
    /// jaw-tip clearance circle.
    ///
    /// Whichever physical contact fires first becomes the displayed farthest
    /// reachable target-ball center. Pocket entry is no longer classified here;
    /// this predictor is used as a visual training aid rather than a rules engine.
    /// </summary>
    public class ShotSimulator : MonoBehaviour
    {
        [Tooltip("袋口半径（米）—— 球心进入此半径范围算进袋")]
        public float pocketCatchRadius = 0.06f;

        [Tooltip("袋口两侧胶条缺口（米）—— 胶条在袋口处向内偏移这么远给球让路")]
        public float jawGap = 0.08f;

        [Tooltip("主球撞击后的偏折显示长度（米，仅可视化）")]
        public float cueAfterImpactDistance = 0.3f;

        // Cached geometry (rebuild when table changes).
        List<TableGeometry.CushionSegment> _cushions;
        List<TableGeometry.PocketDisc> _pockets;
        List<TableGeometry.PocketMouth> _mouths;
        TableController _cachedTable;
        float _cachedJawGap;
        float _cachedPocketR;

        void EnsureGeometry(TableController table)
        {
            if (_cushions != null && _cachedTable == table && _cachedJawGap == jawGap && _cachedPocketR == pocketCatchRadius) return;
            _cushions = TableGeometry.BuildCushionSegments(table, jawGap);
            _pockets = TableGeometry.BuildPocketDiscs(table, pocketCatchRadius);
            _mouths = TableGeometry.BuildPocketMouths(table, jawGap);
            _cachedTable = table;
            _cachedJawGap = jawGap;
            _cachedPocketR = pocketCatchRadius;
        }

        public SimulationResult Run(
            Vector3 cuePos, Vector3 targetPos, Vector3 aimDir,
            float ballRadius, TableController table)
        {
            EnsureGeometry(table);

            var result = new SimulationResult();
            aimDir = aimDir.normalized;
            aimDir.y = 0f;

            // Ray-sphere intersection: does the ray (cuePos, aimDir) hit sphere(targetPos, 2R)?
            float contactRadius = 2f * ballRadius;
            Vector3 toTarget = targetPos - cuePos; toTarget.y = 0f;
            float b = Vector3.Dot(aimDir, toTarget);
            float c = toTarget.sqrMagnitude - contactRadius * contactRadius;
            float discriminant = b * b - c;

            if (discriminant < 0f || b < 0f)
            {
                result.state = TargetBallEndState.NotHit;
                result.targetBallEndPos = targetPos;
                result.targetBallTrajectory = new[] { targetPos, targetPos };

                float tRailCue = FirstCushionHit(cuePos, aimDir, ballRadius);
                if (tRailCue == float.MaxValue)
                    tRailCue = PlayfieldBoundsHit(cuePos, aimDir, table, ballRadius);
                Vector3 cueEnd = cuePos + aimDir * Mathf.Max(0f, tRailCue);
                cueEnd.y = ballRadius;
                result.cueBallTrajectory = new[] { cuePos, cueEnd };
                return result;
            }

            float tImpact = b - Mathf.Sqrt(discriminant);
            Vector3 ghostPos = cuePos + aimDir * tImpact; ghostPos.y = ballRadius;
            Vector3 targetDir = (targetPos - ghostPos); targetDir.y = 0f; targetDir.Normalize();
            Vector3 contactPoint = targetPos - targetDir * ballRadius;
            contactPoint.y = ballRadius;

            result.hasBallCollision = true;
            result.collisionCueBallCenter = ghostPos;
            result.collisionTargetBallCenter = targetPos;
            result.collisionContactPoint = contactPoint;

            float tEnd = FirstTargetTravelLimit(targetPos, targetDir, ballRadius, out int pocketIdx);
            Vector3 endCenter = targetPos + targetDir * tEnd;
            endCenter.y = ballRadius;
            result.state = TargetBallEndState.AgainstRail;
            result.targetBallEndPos = endCenter;
            if (pocketIdx >= 0 && _pockets != null && pocketIdx < _pockets.Count)
                result.pocketHitPos = _pockets[pocketIdx].center;

            result.targetBallTrajectory = new[] { targetPos, result.targetBallEndPos };

            // Cue ball path: cue → ghost, then perpendicular deflection.
            Vector3 cueAfterDir = aimDir - Vector3.Dot(aimDir, targetDir) * targetDir;
            if (cueAfterDir.sqrMagnitude < 1e-8f)
            {
                result.cueBallTrajectory = new[] { cuePos, ghostPos };
            }
            else
            {
                cueAfterDir.y = 0f;
                cueAfterDir.Normalize();
                Vector3 cueEnd = ghostPos + cueAfterDir * cueAfterImpactDistance;
                cueEnd.y = ballRadius;
                result.cueBallTrajectory = new[] { cuePos, ghostPos, cueEnd };
            }
            return result;
        }

        /// <summary>
        /// Time until the ball (moving along dir from start) makes distance = R
        /// contact with any cushion segment, with contact point lying within the
        /// segment's endpoints. Returns float.MaxValue if no collision.
        /// </summary>
        public float FirstCushionHit(Vector3 start, Vector3 dir, float ballRadius)
        {
            if (_cushions == null) return float.MaxValue;
            float tBest = float.MaxValue;
            foreach (var seg in _cushions)
            {
                float t = BallLineSegmentCollisionTime(start, dir, seg.p1, seg.p2, ballRadius);
                if (t < tBest) tBest = t;
            }
            return tBest;
        }

        /// <summary>
        /// Time until the ball center crosses the usable pocket mouth. The mouth is
        /// narrowed by the ball radius, and rounded jaw tips block paths that skim
        /// too close before the mouth crossing.
        /// </summary>
        public float FirstPocketHit(Vector3 start, Vector3 dir, float ballRadius, out int pocketIdx)
        {
            pocketIdx = -1;
            if (_mouths == null) return float.MaxValue;
            float tBest = float.MaxValue;
            for (int i = 0; i < _mouths.Count; i++)
            {
                var mouth = _mouths[i];
                float t = RayMouthEntryTime(start, dir, mouth, ballRadius);
                if (t < tBest) { tBest = t; pocketIdx = mouth.index; }
            }
            return tBest;
        }

        public float FirstTargetTravelLimit(Vector3 start, Vector3 dir, float ballRadius, out int pocketIdx)
        {
            pocketIdx = -1;
            float tBest = FirstCushionHit(start, dir, ballRadius);
            float tBounds = PlayfieldOuterBoundsHit(start, dir, _cachedTable);
            if (tBounds < tBest) tBest = tBounds;

            if (_mouths != null)
            {
                for (int i = 0; i < _mouths.Count; i++)
                {
                    var mouth = _mouths[i];
                    float jawClearance = ballRadius + Mathf.Max(0f, mouth.jawRadius);
                    float tJaw1 = RayCircleEntryTime(start, dir, mouth.jawCenter1, jawClearance);
                    float tJaw2 = RayCircleEntryTime(start, dir, mouth.jawCenter2, jawClearance);
                    if (tJaw1 < tBest) { tBest = tJaw1; pocketIdx = -1; }
                    if (tJaw2 < tBest) { tBest = tJaw2; pocketIdx = -1; }
                }
            }

            return tBest == float.MaxValue ? 0f : tBest;
        }

        // ===== Geometry primitives (XZ plane) =====

        /// <summary>
        /// Earliest t > 0 such that the moving ball (center = start + dir*t, radius R)
        /// makes contact with the infinite line through (p1, p2), with the contact
        /// point on the closed segment [p1, p2].
        ///
        /// Math: let the line have unit tangent e = (p2-p1)/|p2-p1| and unit normal n.
        /// The ball center distance from the line at time t is
        ///   d(t) = n · (start + dir*t - p1)
        ///        = (n·start - n·p1) + t·(n·dir)
        ///        = d0 + t·dDot
        /// Collision when |d(t)| = R. There are two solutions; we want the one
        /// where the ball is APPROACHING the cushion (d(t) going toward 0), i.e.
        /// the root where the ball's distance shrinks to R.
        /// </summary>
        public static float BallLineSegmentCollisionTime(Vector3 start, Vector3 dir, Vector3 p1, Vector3 p2, float R)
        {
            Vector3 e = p2 - p1; e.y = 0f;
            float eLen = e.magnitude;
            if (eLen < 1e-6f) return float.MaxValue;
            e /= eLen;
            Vector3 n = new Vector3(-e.z, 0f, e.x); // 90° CCW rotation in XZ plane

            Vector3 rel = start - p1; rel.y = 0f;
            float d0 = Vector3.Dot(rel, n);
            float dDot = Vector3.Dot(dir, n);

            if (Mathf.Abs(dDot) < 1e-9f)
            {
                // Motion parallel to cushion → never approaches.
                return float.MaxValue;
            }

            // We want the time t > 0 such that d(t) shrinks toward 0 from |d0| and
            // reaches exactly ±R. Approaching side is where d0 and (d(∞)) have
            // opposite signs, i.e. dDot and d0 have opposite signs.
            // Solve d0 + t*dDot = +R or -R, take the earliest positive t.
            float tPlus = (R - d0) / dDot;
            float tMinus = (-R - d0) / dDot;

            float tCandidate = float.MaxValue;
            if (tPlus > 1e-6f) tCandidate = tPlus;
            if (tMinus > 1e-6f && tMinus < tCandidate) tCandidate = tMinus;
            if (tCandidate >= float.MaxValue) return float.MaxValue;

            // Verify the contact point lies within the segment (projected along e).
            Vector3 contact = start + dir * tCandidate;
            // Push contact point from ball center toward cushion by R to get
            // the actual contact location on the cushion line.
            // Determine which side of the line the ball is on now to get correct sign.
            float sideSign = (d0 + tCandidate * dDot) > 0f ? 1f : -1f;
            Vector3 cushionContact = contact - n * sideSign * R;
            Vector3 relContact = cushionContact - p1; relContact.y = 0f;
            float u = Vector3.Dot(relContact, e);
            if (u < 0f || u > eLen) return float.MaxValue;

            return tCandidate;
        }

        /// <summary>
        /// Earliest t > 0 such that |start + dir*t - center| = radius, with the
        /// ball entering (i.e. approaching) the disc.
        /// </summary>
        public static float RayCircleEntryTime(Vector3 start, Vector3 dir, Vector3 center, float radius)
        {
            Vector3 f = start - center; f.y = 0f;
            Vector3 d = dir; d.y = 0f;
            float a = Vector3.Dot(d, d);
            if (a < 1e-9f) return float.MaxValue;
            float bHalf = Vector3.Dot(f, d);
            float cTerm = f.sqrMagnitude - radius * radius;
            float disc = bHalf * bHalf - a * cTerm;
            if (disc < 0f) return float.MaxValue;
            float sqrtDisc = Mathf.Sqrt(disc);
            float t1 = (-bHalf - sqrtDisc) / a;
            float t2 = (-bHalf + sqrtDisc) / a;
            // Entry time = smallest positive root.
            if (t1 > 1e-6f) return t1;
            if (t2 > 1e-6f) return t2;
            return float.MaxValue;
        }

        public static float RayMouthEntryTime(
            Vector3 start, Vector3 dir, TableGeometry.PocketMouth mouth, float ballRadius)
        {
            Vector3 a = mouth.p1;
            Vector3 b = mouth.p2;
            Vector3 mouthDir = b - a; mouthDir.y = 0f;
            float length = mouthDir.magnitude;
            if (length <= ballRadius * 2f + 1e-6f) return float.MaxValue;
            mouthDir /= length;

            a += mouthDir * ballRadius;
            b -= mouthDir * ballRadius;

            float tMouth = RaySegmentIntersectionTime(start, dir, a, b);
            if (tMouth == float.MaxValue) return float.MaxValue;

            float jawClearance = ballRadius + Mathf.Max(0f, mouth.jawRadius);
            float tJaw1 = RayCircleEntryTime(start, dir, mouth.jawCenter1, jawClearance);
            float tJaw2 = RayCircleEntryTime(start, dir, mouth.jawCenter2, jawClearance);
            if (Mathf.Min(tJaw1, tJaw2) <= tMouth) return float.MaxValue;

            return tMouth;
        }

        public static float RaySegmentIntersectionTime(Vector3 start, Vector3 dir, Vector3 a, Vector3 b)
        {
            Vector3 d = dir; d.y = 0f;
            Vector3 v = b - a; v.y = 0f;
            float det = -d.x * v.z + d.z * v.x;
            if (Mathf.Abs(det) < 1e-9f) return float.MaxValue;

            Vector3 s = a - start; s.y = 0f;
            float t = (-v.z * s.x + v.x * s.z) / det;
            float u = (-d.z * s.x + d.x * s.z) / det;
            if (t <= 1e-6f || u < 0f || u > 1f) return float.MaxValue;
            return t;
        }

        static float PlayfieldBoundsHit(Vector3 start, Vector3 dir, TableController table, float ballRadius)
        {
            if (table == null) return 0f;

            float maxX = Mathf.Max(0f, table.playfieldHalfLength - ballRadius);
            float maxZ = Mathf.Max(0f, table.playfieldHalfWidth - ballRadius);
            float tBest = float.MaxValue;

            if (Mathf.Abs(dir.x) > 1e-6f)
            {
                float tx = (dir.x > 0f ? maxX - start.x : -maxX - start.x) / dir.x;
                if (tx > 1e-6f && tx < tBest) tBest = tx;
            }

            if (Mathf.Abs(dir.z) > 1e-6f)
            {
                float tz = (dir.z > 0f ? maxZ - start.z : -maxZ - start.z) / dir.z;
                if (tz > 1e-6f && tz < tBest) tBest = tz;
            }

            return tBest == float.MaxValue ? 0f : tBest;
        }

        static float PlayfieldOuterBoundsHit(Vector3 start, Vector3 dir, TableController table)
        {
            if (table == null) return float.MaxValue;

            float maxX = Mathf.Max(0f, table.playfieldHalfLength);
            float maxZ = Mathf.Max(0f, table.playfieldHalfWidth);
            float tBest = float.MaxValue;

            if (Mathf.Abs(dir.x) > 1e-6f)
            {
                float tx = (dir.x > 0f ? maxX - start.x : -maxX - start.x) / dir.x;
                if (tx > 1e-6f && tx < tBest) tBest = tx;
            }

            if (Mathf.Abs(dir.z) > 1e-6f)
            {
                float tz = (dir.z > 0f ? maxZ - start.z : -maxZ - start.z) / dir.z;
                if (tz > 1e-6f && tz < tBest) tBest = tz;
            }

            return tBest;
        }

        // ===== Static helpers for PuzzleGenerator =====

        /// <summary>
        /// Static variant that builds geometry on-the-fly. For PuzzleGenerator
        /// which doesn't have a ShotSimulator instance context.
        /// </summary>
        public static bool CanBallReachPocket(
            Vector3 start, Vector3 dir, TableController table,
            float ballRadius, float pocketRadius, float jawGapParam,
            out int pocketIdx)
        {
            pocketIdx = -1;
            var cushions = TableGeometry.BuildCushionSegments(table, jawGapParam);
            var mouths = TableGeometry.BuildPocketMouths(table, jawGapParam);

            float tRail = float.MaxValue;
            foreach (var seg in cushions)
            {
                float t = BallLineSegmentCollisionTime(start, dir, seg.p1, seg.p2, ballRadius);
                if (t < tRail) tRail = t;
            }

            float tPocket = float.MaxValue;
            for (int i = 0; i < mouths.Count; i++)
            {
                var mouth = mouths[i];
                float t = RayMouthEntryTime(start, dir, mouth, ballRadius);
                if (t < tPocket) { tPocket = t; pocketIdx = mouth.index; }
            }

            if (tPocket < tRail) return true;
            pocketIdx = -1;
            return false;
        }

        /// <summary>
        /// Check that the ball travels a given distance without cushion contact.
        /// </summary>
        public static bool PathClearToPoint(Vector3 start, Vector3 end, TableController table,
            float ballRadius, float jawGapParam)
        {
            Vector3 d = end - start; d.y = 0f;
            float len = d.magnitude;
            if (len < 1e-6f) return true;
            d /= len;
            var cushions = TableGeometry.BuildCushionSegments(table, jawGapParam);
            foreach (var seg in cushions)
            {
                float t = BallLineSegmentCollisionTime(start, d, seg.p1, seg.p2, ballRadius);
                if (t < len) return false;
            }
            return true;
        }
    }
}
