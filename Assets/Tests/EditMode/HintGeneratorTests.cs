using NUnit.Framework;
using UnityEngine;
using PoolAimTrainer.GeometryCore;

namespace PoolAimTrainer.Tests.EditMode
{
    public class HintGeneratorTests
    {
        [Test]
        public void Hint_StraightShot_ContainsStraightKeyword()
        {
            string s = HintGenerator.Generate(cutAngleDeg: 0f, offsetCm: 0f, aimSide: AimSide.Center);
            Assert.That(s, Does.Contain("直线球"));
        }

        [Test]
        public void Hint_ThinShot_ContainsThinKeyword()
        {
            string s = HintGenerator.Generate(cutAngleDeg: 70f, offsetCm: 2.5f, aimSide: AimSide.Right);
            Assert.That(s, Does.Contain("薄球"));
            Assert.That(s, Does.Contain("右"));
            Assert.That(s, Does.Contain("2.5"));
        }

        [Test]
        public void Hint_MediumAngle_ContainsAngleAndOffset()
        {
            string s = HintGenerator.Generate(cutAngleDeg: 30f, offsetCm: 1.2f, aimSide: AimSide.Left);
            Assert.That(s, Does.Contain("30"));
            Assert.That(s, Does.Contain("左"));
            Assert.That(s, Does.Contain("1.2"));
        }
    }
}
