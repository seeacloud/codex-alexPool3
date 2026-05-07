using UnityEngine;

namespace PoolAimTrainer.Puzzles
{
    public enum DistanceOption { Near, Medium, Far }

    public struct DistanceRange
    {
        public float min, max;
        public DistanceRange(float min, float max) { this.min = min; this.max = max; }
        public float Sample() => Random.Range(min, max);
    }

    public struct PuzzleParams
    {
        public float cutAngleDeg;
        public bool anyAngle;
        public DistanceOption targetToPocket;
        public DistanceOption cueToTarget;
        public int pocketIndex;

        public float SampleCutAngle()
        {
            return anyAngle ? Random.Range(0f, 90f) : cutAngleDeg;
        }

        public static DistanceRange GetTargetPocketRange(DistanceOption o)
        {
            switch (o)
            {
                case DistanceOption.Near:   return new DistanceRange(0.10f, 0.30f);
                case DistanceOption.Medium: return new DistanceRange(0.35f, 0.65f);
                default:                    return new DistanceRange(0.70f, 1.00f);
            }
        }

        public static DistanceRange GetCueTargetRange(DistanceOption o)
        {
            switch (o)
            {
                case DistanceOption.Near:   return new DistanceRange(0.10f, 0.25f);
                case DistanceOption.Medium: return new DistanceRange(0.30f, 0.55f);
                default:                    return new DistanceRange(0.60f, 0.90f);
            }
        }
    }
}
