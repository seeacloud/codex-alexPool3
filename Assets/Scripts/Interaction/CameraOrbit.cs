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

        Vector2 lastMouse;

        void LateUpdate()
        {
            if (InputRouter.SecondaryHeld)
            {
                Vector2 delta = (Vector2)Input.mousePosition - lastMouse;
                yawDegrees += delta.x * rotateSpeed * 0.1f;
                pitchDegrees = Mathf.Clamp(pitchDegrees - delta.y * rotateSpeed * 0.1f, 15f, 85f);
            }
            lastMouse = Input.mousePosition;

            distance = Mathf.Clamp(distance - InputRouter.ScrollDelta.y * zoomSpeed, minDistance, maxDistance);

            Quaternion rot = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
            Vector3 offset = rot * (Vector3.back * distance);
            transform.position = (pivot != null ? pivot.position : Vector3.zero) + offset;
            transform.rotation = rot;
        }
    }
}
