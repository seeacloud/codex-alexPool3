using UnityEngine;

namespace PoolAimTrainer.Interaction
{
    public static class InputRouter
    {
        public static bool PrimaryDown => Input.GetMouseButtonDown(0);
        public static bool PrimaryHeld => Input.GetMouseButton(0);
        public static bool PrimaryUp   => Input.GetMouseButtonUp(0);
        public static Vector2 PrimaryScreenPosition => Input.mousePosition;

        public static bool SecondaryHeld => Input.GetMouseButton(1);
        public static Vector2 ScrollDelta => new Vector2(0f, Input.mouseScrollDelta.y);
    }
}
