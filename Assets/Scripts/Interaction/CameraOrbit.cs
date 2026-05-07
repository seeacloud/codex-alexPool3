using UnityEngine;

namespace PoolAimTrainer.Interaction
{
    public class CameraOrbit : MonoBehaviour
    {
        public Transform pivot;
        public float distance = 2.5f;
        public float yawDegrees = 0f;
        public float pitchDegrees = 55f;
        public float rotateSpeed = 4f;
        public float zoomSpeed = 0.3f;
        public float minDistance = 0.8f;
        public float maxDistance = 4f;
        public float panSpeed = 0.002f;

        [HideInInspector] public Vector3 panOffset;

        [Header("Top-down")]
        public bool isTopDown;
        public float topDownHeight = 2.5f;
        public float topDownRotation = 0f;
        public float topDownRotateSpeed = 60f;

        [Header("Transition")]
        public float transitionDuration = 0.6f;

        Vector2 lastMouse;

        float savedDistance;
        float savedYaw;
        float savedPitch;
        Vector3 savedPanOffset;

        // Transition state
        bool transitioning;
        float transitionT;
        Vector3 transitionStartPos;
        Quaternion transitionStartRot;
        Vector3 transitionEndPos;
        Quaternion transitionEndRot;

        public void SetTopDown(bool on)
        {
            if (on)
            {
                savedDistance = distance;
                savedYaw = yawDegrees;
                savedPitch = pitchDegrees;
                savedPanOffset = panOffset;
                topDownHeight = distance;
                topDownRotation = 0f;
                panOffset = Vector3.zero;
            }
            else
            {
                distance = savedDistance;
                yawDegrees = savedYaw;
                pitchDegrees = savedPitch;
                panOffset = Vector3.zero;
            }

            isTopDown = on;
            StartTransition();
        }

        public void ToggleTopDown()
        {
            SetTopDown(!isTopDown);
        }

        void StartTransition()
        {
            transitionStartPos = transform.position;
            transitionStartRot = transform.rotation;
            transitionEndPos = ComputeTargetPosition();
            transitionEndRot = ComputeTargetRotation();
            transitionT = 0f;
            transitioning = true;
        }

        Vector3 ComputeTargetPosition()
        {
            Vector3 center = (pivot != null ? pivot.position : Vector3.zero) + panOffset;
            if (isTopDown)
                return center + Vector3.up * topDownHeight;
            Quaternion rot = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
            return center + rot * (Vector3.back * distance);
        }

        Quaternion ComputeTargetRotation()
        {
            if (isTopDown)
                return Quaternion.Euler(90f, topDownRotation, 0f);
            return Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
        }

        void LateUpdate()
        {
            if (transitioning)
            {
                transitionT += Time.deltaTime / transitionDuration;
                float t = EaseInOut(Mathf.Clamp01(transitionT));
                transform.position = Vector3.Lerp(transitionStartPos, transitionEndPos, t);
                transform.rotation = Quaternion.Slerp(transitionStartRot, transitionEndRot, t);
                if (transitionT >= 1f)
                    transitioning = false;
                lastMouse = Input.mousePosition;
                return;
            }

            if (isTopDown)
                UpdateTopDown();
            else
                UpdateOrbit();

            lastMouse = Input.mousePosition;
        }

        static float EaseInOut(float t)
        {
            return t < 0.5f
                ? 4f * t * t * t
                : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
        }

        void UpdateTopDown()
        {
            Vector2 delta = Vector2.zero;
            if (Input.touchCount == 2)
                delta = InputRouter.TwoFingerDelta;
            else if (InputRouter.SecondaryHeld)
                delta = (Vector2)Input.mousePosition - lastMouse;

            if (delta.sqrMagnitude > 0.01f)
                topDownRotation += delta.x * topDownRotateSpeed * 0.01f;

            topDownHeight = Mathf.Clamp(topDownHeight - InputRouter.ScrollDelta.y * zoomSpeed, minDistance, maxDistance);

            Vector3 center = (pivot != null ? pivot.position : Vector3.zero) + panOffset;
            transform.position = center + Vector3.up * topDownHeight;
            transform.rotation = Quaternion.Euler(90f, topDownRotation, 0f);
        }

        void UpdateOrbit()
        {
            Vector2 delta = Vector2.zero;
            if (Input.touchCount == 2)
            {
                delta = InputRouter.TwoFingerDelta;
            }
            else if (InputRouter.SecondaryHeld)
            {
                delta = (Vector2)Input.mousePosition - lastMouse;
            }
            if (delta.sqrMagnitude > 0.01f)
            {
                yawDegrees += delta.x * rotateSpeed * 0.1f;
                pitchDegrees = Mathf.Clamp(pitchDegrees - delta.y * rotateSpeed * 0.1f, 15f, 85f);
            }

            distance = Mathf.Clamp(distance - InputRouter.ScrollDelta.y * zoomSpeed, minDistance, maxDistance);

            Quaternion rot = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
            Vector3 offset = rot * (Vector3.back * distance);
            Vector3 center = (pivot != null ? pivot.position : Vector3.zero) + panOffset;
            transform.position = center + offset;
            transform.rotation = rot;
        }

        public void ApplyPan(Vector2 screenDelta)
        {
            if (transitioning) return;
            if (isTopDown)
            {
                Vector3 right = transform.right;
                Vector3 up = transform.up;
                panOffset -= (right * screenDelta.x + up * screenDelta.y) * panSpeed * topDownHeight;
            }
            else
            {
                Vector3 right = transform.right;
                Vector3 forward = Vector3.Cross(right, Vector3.up).normalized;
                panOffset -= (right * screenDelta.x + forward * screenDelta.y) * panSpeed * distance;
            }
        }
    }
}
