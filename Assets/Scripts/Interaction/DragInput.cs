using UnityEngine;
using PoolAimTrainer.Core;
using PoolAimTrainer.SceneObjects;

namespace PoolAimTrainer.Interaction
{
    public class DragInput : MonoBehaviour
    {
        public Camera cam;
        public float pickRadius = 0.05f;
        [Tooltip("点击袋口的吸附半径（米）")]
        public float pocketPickRadius = 0.1f;
        [Tooltip("可选：如果设置，点击袋口会写入 aimManager.userSelectedPocket")]
        public AimManager aimManager;
        [Tooltip("摄像机环绕组件（用于平移）")]
        public CameraOrbit cameraOrbit;

        BallController dragging;
        bool panning;
        Vector2 lastPanScreenPos;
        Plane tablePlane;

        void Awake()
        {
            if (cam == null) cam = Camera.main;
            tablePlane = new Plane(Vector3.up, new Vector3(0f, 0.0286f, 0f));
        }

        void Update()
        {
            if (InputRouter.PrimaryDown)
            {
                Ray r = cam.ScreenPointToRay(InputRouter.PrimaryScreenPosition);
                if (tablePlane.Raycast(r, out float t))
                {
                    Vector3 hit = r.GetPoint(t);
                    dragging = FindClosestBall(hit);
                    if (dragging == null)
                    {
                        var pocket = FindClosestPocket(hit);
                        if (pocket != null && aimManager != null)
                        {
                            aimManager.SetUserPocket(pocket);
                        }
                        else
                        {
                            panning = true;
                            lastPanScreenPos = InputRouter.PrimaryScreenPosition;
                        }
                    }
                }
                else
                {
                    panning = true;
                    lastPanScreenPos = InputRouter.PrimaryScreenPosition;
                }
            }
            else if (InputRouter.PrimaryHeld)
            {
                if (dragging != null)
                {
                    Ray r = cam.ScreenPointToRay(InputRouter.PrimaryScreenPosition);
                    if (tablePlane.Raycast(r, out float t))
                        dragging.MoveTo(r.GetPoint(t));
                }
                else if (panning && cameraOrbit != null)
                {
                    Vector2 current = InputRouter.PrimaryScreenPosition;
                    Vector2 delta = current - lastPanScreenPos;
                    cameraOrbit.ApplyPan(delta);
                    lastPanScreenPos = current;
                }
            }
            else if (InputRouter.PrimaryUp)
            {
                dragging = null;
                panning = false;
            }
        }

        BallController FindClosestBall(Vector3 hit)
        {
            var balls = FindObjectsOfType<BallController>();
            BallController best = null;
            float bestDist = pickRadius;
            foreach (var b in balls)
            {
                float d = Vector3.Distance(b.Center, hit);
                if (d < bestDist) { bestDist = d; best = b; }
            }
            return best;
        }

        PocketMarker FindClosestPocket(Vector3 hit)
        {
            var pockets = FindObjectsOfType<PocketMarker>();
            PocketMarker best = null;
            float bestDist = pocketPickRadius;
            foreach (var p in pockets)
            {
                float d = Vector3.Distance(p.Position, hit);
                if (d < bestDist) { bestDist = d; best = p; }
            }
            return best;
        }
    }
}
