using UnityEngine;

namespace PoolAimTrainer.Interaction
{
    /// <summary>
    /// Unified input abstraction: mouse on desktop, touch on mobile.
    /// Single finger = primary (drag balls, click HUD).
    /// Two fingers = secondary (camera orbit + pinch zoom).
    /// </summary>
    public static class InputRouter
    {
        public static bool PrimaryDown
        {
            get
            {
                if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began) return true;
                return Input.GetMouseButtonDown(0);
            }
        }

        public static bool PrimaryHeld
        {
            get
            {
                if (Input.touchCount == 1)
                {
                    var p = Input.GetTouch(0).phase;
                    return p == TouchPhase.Moved || p == TouchPhase.Stationary;
                }
                return Input.GetMouseButton(0);
            }
        }

        public static bool PrimaryUp
        {
            get
            {
                if (Input.touchCount >= 1 && (Input.GetTouch(0).phase == TouchPhase.Ended
                                           || Input.GetTouch(0).phase == TouchPhase.Canceled))
                    return true;
                return Input.GetMouseButtonUp(0);
            }
        }

        public static Vector2 PrimaryScreenPosition
        {
            get
            {
                if (Input.touchCount > 0) return Input.GetTouch(0).position;
                return Input.mousePosition;
            }
        }

        public static bool SecondaryHeld => Input.GetMouseButton(1) || Input.touchCount == 2;

        public static Vector2 TwoFingerDelta
        {
            get
            {
                if (Input.touchCount == 2)
                {
                    var t0 = Input.GetTouch(0);
                    var t1 = Input.GetTouch(1);
                    return (t0.deltaPosition + t1.deltaPosition) * 0.5f;
                }
                return Vector2.zero;
            }
        }

        public static float PinchDelta
        {
            get
            {
                if (Input.touchCount == 2)
                {
                    var t0 = Input.GetTouch(0);
                    var t1 = Input.GetTouch(1);
                    Vector2 prev0 = t0.position - t0.deltaPosition;
                    Vector2 prev1 = t1.position - t1.deltaPosition;
                    float prevDist = (prev0 - prev1).magnitude;
                    float curDist = (t0.position - t1.position).magnitude;
                    return (curDist - prevDist) * 0.01f;
                }
                return Input.mouseScrollDelta.y;
            }
        }

        public static Vector2 ScrollDelta => new Vector2(0f, PinchDelta);
    }
}
