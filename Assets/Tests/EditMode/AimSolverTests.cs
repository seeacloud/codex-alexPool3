using NUnit.Framework;
using UnityEngine;
using PoolAimTrainer.GeometryCore;

namespace PoolAimTrainer.Tests.EditMode
{
    public class AimSolverTests
    {
        const float R = 0.0286f; // ball radius (m)

        [Test]
        public void GhostBall_StraightShot_IsBehindObjectBallAlongPocketLine()
        {
            // 目标球 (1, 0, 0)，袋口 (2, 0, 0)，ghost 应在 (1 - 2R, 0, 0)
            var objBall = new Vector3(1f, 0f, 0f);
            var pocket = new Vector3(2f, 0f, 0f);

            var center = AimSolver.ComputeGhostBallCenter(objBall, pocket, R);

            Assert.That(center.x, Is.EqualTo(1f - 2f * R).Within(1e-5f));
            Assert.That(center.z, Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void GhostBall_45DegreeShot_IsAtCorrectDiagonalOffset()
        {
            // 目标球 (0,0,0)，袋口 (1,0,1)；方向 = (1/√2, 0, 1/√2)
            // ghost = (0,0,0) - (1/√2, 0, 1/√2) * 2R
            var objBall = Vector3.zero;
            var pocket = new Vector3(1f, 0f, 1f);

            var center = AimSolver.ComputeGhostBallCenter(objBall, pocket, R);

            float inv = 1f / Mathf.Sqrt(2f);
            Assert.That(center.x, Is.EqualTo(-2f * R * inv).Within(1e-5f));
            Assert.That(center.z, Is.EqualTo(-2f * R * inv).Within(1e-5f));
        }

        [Test]
        public void CutAngle_StraightShot_IsZeroDegrees()
        {
            var cue = new Vector3(0f, 0f, 0f);
            var obj = new Vector3(1f, 0f, 0f);
            var pocket = new Vector3(2f, 0f, 0f);

            float angle = AimSolver.ComputeCutAngle(cue, obj, pocket, R);

            Assert.That(angle, Is.EqualTo(0f).Within(0.01f));
        }

        [Test]
        public void CutAngle_HalfBallHit_IsThirtyDegrees()
        {
            var obj = new Vector3(0f, 0f, 0f);
            var pocket = new Vector3(0f, 0f, 1f);
            var ghostCenter = AimSolver.ComputeGhostBallCenter(obj, pocket, R);
            var backDir = new Vector3(Mathf.Sin(30f * Mathf.Deg2Rad), 0f, -Mathf.Cos(30f * Mathf.Deg2Rad));
            var cue = ghostCenter + backDir * 0.5f;

            float angle = AimSolver.ComputeCutAngle(cue, obj, pocket, R);

            Assert.That(angle, Is.EqualTo(30f).Within(0.05f));
        }

        [Test]
        public void Compute_StraightShot_ReturnsSolvableResult()
        {
            var cue = new Vector3(0f, 0f, 0f);
            var obj = new Vector3(1f, 0f, 0f);
            var pocket = new Vector3(2f, 0f, 0f);

            var r = AimSolver.Compute(cue, obj, pocket, R);

            Assert.That(r.solvable, Is.True);
            Assert.That(r.cutAngleDegrees, Is.EqualTo(0f).Within(0.01f));
            Assert.That(r.ghostBallCenter.x, Is.EqualTo(1f - 2f * R).Within(1e-5f));
            Assert.That(r.aimLineStart, Is.EqualTo(cue));
            Assert.That(r.aimLineEnd, Is.EqualTo(r.ghostBallCenter));
            Assert.That(r.objectBallToPocketStart, Is.EqualTo(obj));
            Assert.That(r.objectBallToPocketEnd, Is.EqualTo(pocket));
        }

        [Test]
        public void Compute_CueBallOnPocketSideOfObject_ReturnsUnsolvable()
        {
            var cue = new Vector3(3f, 0f, 0f);
            var obj = new Vector3(1f, 0f, 0f);
            var pocket = new Vector3(2f, 0f, 0f);

            var r = AimSolver.Compute(cue, obj, pocket, R);

            Assert.That(r.solvable, Is.False);
        }
    }
}
