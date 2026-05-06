using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PoolAimTrainer.Core;
using PoolAimTrainer.SceneObjects;

namespace PoolAimTrainer.UI
{
    /// <summary>
    /// HUD that renders the target ball as viewed from the cue ball's position.
    /// Clicking on the HUD image converts the click into a 3D aim point and tells
    /// AimManager to use that aim direction (overrides auto-ghost until cleared).
    /// </summary>
    public class AimHudController : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IDragHandler
    {
        public Camera aimCamera;
        public RawImage rawImage;
        public RectTransform rawImageRect;
        public BallController cueBall;
        public BallController targetBall;
        public AimManager aimManager;

        [Tooltip("HUD 正交相机的视野大小（世界单位）。应略大于球直径，显示目标球+周边空间")]
        public float orthoSize = 0.08f;

        [Tooltip("HUD 相机 near plane 距离主球的偏移（米）")]
        public float nearOffset = 0.001f;

        void LateUpdate()
        {
            if (aimCamera == null || cueBall == null || targetBall == null) return;
            Vector3 dir = targetBall.Center - cueBall.Center;
            float dist = dir.magnitude;
            if (dist < 1e-4f) return;
            Vector3 forward = dir / dist;
            aimCamera.transform.position = cueBall.Center + forward * nearOffset;
            aimCamera.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            aimCamera.orthographic = true;
            aimCamera.orthographicSize = orthoSize;
            aimCamera.nearClipPlane = 0.001f;
            aimCamera.farClipPlane = Mathf.Max(2f, dist + 1f);
        }

        public void OnPointerClick(PointerEventData e) { ProcessClick(e); }
        public void OnPointerDown(PointerEventData e) { ProcessClick(e); }
        public void OnDrag(PointerEventData e) { ProcessClick(e); }

        void ProcessClick(PointerEventData e)
        {
            if (aimCamera == null || rawImageRect == null || cueBall == null) return;
            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rawImageRect, e.position, e.pressEventCamera, out localPoint))
                return;

            Rect rect = rawImageRect.rect;
            if (rect.width <= 0f || rect.height <= 0f) return;

            // Normalize to [0, 1] viewport coords in the aim camera.
            float u = (localPoint.x - rect.xMin) / rect.width;
            float v = (localPoint.y - rect.yMin) / rect.height;

            // Sample a world point at the target ball's distance so the ray through the
            // camera passes through that point on the image plane.
            float zDepth = Mathf.Max(nearOffset + 0.01f,
                Vector3.Distance(cueBall.Center, targetBall != null ? targetBall.Center : aimCamera.transform.position + aimCamera.transform.forward));
            Vector3 worldPoint = aimCamera.ViewportToWorldPoint(new Vector3(u, v, zDepth));
            Vector3 aimDir = (worldPoint - cueBall.Center).normalized;

            if (aimManager != null) aimManager.SetManualAimDir(aimDir);
        }
    }
}
