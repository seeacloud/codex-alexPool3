using UnityEngine;

namespace PoolAimTrainer.Visualization
{
    public class GhostBallRenderer : MonoBehaviour
    {
        public GameObject ghostPrefab;
        GameObject instance;

        public GameObject CurrentInstance => instance;

        public void Show(Vector3 center)
        {
            if (instance == null && ghostPrefab != null)
                instance = Instantiate(ghostPrefab);
            if (instance != null)
            {
                instance.SetActive(true);
                instance.transform.position = center;
            }
        }

        public void Hide()
        {
            if (instance != null) instance.SetActive(false);
        }
    }
}
