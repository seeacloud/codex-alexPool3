using UnityEngine;
using PoolAimTrainer.SceneObjects;

namespace PoolAimTrainer.Interaction
{
    public class DragInput : MonoBehaviour
    {
        public Camera cam;
        public LayerMask ballLayer = ~0;
        public float pickRadius = 0.05f;

        BallController dragging;
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
                }
            }
            else if (InputRouter.PrimaryHeld && dragging != null)
            {
                Ray r = cam.ScreenPointToRay(InputRouter.PrimaryScreenPosition);
                if (tablePlane.Raycast(r, out float t))
                {
                    dragging.MoveTo(r.GetPoint(t));
                }
            }
            else if (InputRouter.PrimaryUp)
            {
                dragging = null;
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
    }
}
