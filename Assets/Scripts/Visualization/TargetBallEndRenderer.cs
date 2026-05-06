using UnityEngine;
using PoolAimTrainer.Trajectory;

namespace PoolAimTrainer.Visualization
{
    /// <summary>
    /// Shows the object ball's predicted final resting position as a semi-transparent
    /// ghost, with color encoding the terminal state (green = pocketed, yellow = rail,
    /// green-light = on table, orange flashing = still moving).
    /// </summary>
    public class TargetBallEndRenderer : MonoBehaviour
    {
        public GameObject ghostPrefab;
        public Color onTableColor = new Color(0.2f, 1f, 0.2f, 0.4f);
        public Color againstRailColor = new Color(1f, 0.9f, 0.2f, 0.5f);
        public Color inPocketColor = new Color(0.2f, 1f, 0.2f, 0.85f);
        public Color stillMovingColor = new Color(1f, 0.5f, 0.2f, 0.5f);

        GameObject instance;
        Renderer rend;
        SimulationResult last;

        public void Show(SimulationResult r)
        {
            last = r;
            if (r.state == TargetBallEndState.NotHit)
            {
                Hide();
                return;
            }
            if (instance == null && ghostPrefab != null)
            {
                instance = Instantiate(ghostPrefab);
                rend = instance.GetComponent<Renderer>();
            }
            if (instance == null) return;

            instance.SetActive(true);
            instance.transform.position = r.targetBallEndPos;

            if (rend != null)
            {
                Color c = r.state switch
                {
                    TargetBallEndState.InPocket => inPocketColor,
                    TargetBallEndState.AgainstRail => againstRailColor,
                    TargetBallEndState.StillMoving => stillMovingColor,
                    _ => onTableColor
                };
                rend.material.color = c;
            }
        }

        public void Hide()
        {
            if (instance != null) instance.SetActive(false);
        }

        void Update()
        {
            if (instance != null && instance.activeSelf
                && last.state == TargetBallEndState.StillMoving
                && rend != null)
            {
                Color c = rend.material.color;
                c.a = 0.2f + 0.3f * Mathf.PingPong(Time.time * 2f, 1f);
                rend.material.color = c;
            }
        }
    }
}
