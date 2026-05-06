using System.Globalization;

namespace PoolAimTrainer.GeometryCore
{
    public enum AimSide { Center, Left, Right }

    public static class HintGenerator
    {
        public static string Generate(float cutAngleDeg, float offsetCm, AimSide aimSide)
        {
            string category = cutAngleDeg switch
            {
                < 5f => "直线球",
                < 20f => "小角度球",
                < 40f => "中等角度球",
                < 60f => "大角度球",
                _ => "薄球"
            };
            string sideText = aimSide switch
            {
                AimSide.Left => "左",
                AimSide.Right => "右",
                _ => "中"
            };
            string offsetText = offsetCm.ToString("0.0", CultureInfo.InvariantCulture);
            return $"{category} · 切角 {cutAngleDeg:0.#}° · 瞄向目标球{sideText} {offsetText} cm 处";
        }
    }
}
