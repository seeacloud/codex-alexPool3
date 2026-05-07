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

        Vector2 lastMouse;

        void LateUpdate()
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
            lastMouse = Input.mousePosition;

            distance = Mathf.Clamp(distance - InputRouter.ScrollDelta.y * zoomSpeed, minDistance, maxDistance);

            Quaternion rot = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
            Vector3 offset = rot * (Vector3.back * distance);
            Vector3 center = (pivot != null ? pivot.position : Vector3.zero) + panOffset;
            transform.position = center + offset;
            transform.rotation = rot;
        }

        public void ApplyPan(Vector2 screenDelta)
        {
            Vector3 right = transform.right;
            Vector3 forward = Vector3.Cross(right, Vector3.up).normalized;
            panOffset -= (right * screenDelta.x + forward * screenDelta.y) * panSpeed * distance;
        }
    }
}
