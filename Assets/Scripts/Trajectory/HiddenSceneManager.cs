using UnityEngine;
using UnityEngine.SceneManagement;

namespace PoolAimTrainer.Trajectory
{
    /// <summary>
    /// Owns a dedicated Unity scene with its own local physics world, used for
    /// "what-if" shot simulation that doesn't disturb the main visible scene.
    /// </summary>
    public class HiddenSceneManager : MonoBehaviour
    {
        Scene hiddenScene;
        PhysicsScene hiddenPhysics;
        bool created;

        public bool Created => created;
        public PhysicsScene Physics => hiddenPhysics;
        public Scene Scene => hiddenScene;

        public void EnsureCreated()
        {
            if (created) return;
            var param = new CreateSceneParameters(LocalPhysicsMode.Physics3D);
            hiddenScene = SceneManager.CreateScene("HiddenSim", param);
            hiddenPhysics = hiddenScene.GetPhysicsScene();
            created = true;
        }

        public void Step(float dt)
        {
            if (created) hiddenPhysics.Simulate(dt);
        }
    }
}
