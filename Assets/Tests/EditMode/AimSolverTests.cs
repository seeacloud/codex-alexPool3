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
    }
}
