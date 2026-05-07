using UnityEngine;
using TMPro;

namespace PoolAimTrainer.Puzzles
{
    public class PuzzleUI : MonoBehaviour
    {
        public PuzzleGenerator generator;
        public TMP_Dropdown angleDropdown;
        public TMP_Dropdown pocketDropdown;

        [Header("Distance Buttons")]
        public UnityEngine.UI.Button btnTargetNear;
        public UnityEngine.UI.Button btnTargetMid;
        public UnityEngine.UI.Button btnTargetFar;
        public UnityEngine.UI.Button btnCueNear;
        public UnityEngine.UI.Button btnCueMid;
        public UnityEngine.UI.Button btnCueFar;

        [Header("Action Buttons")]
        public UnityEngine.UI.Button btnGenerate;
        public UnityEngine.UI.Button btnRandom;

        DistanceOption targetDist = DistanceOption.Medium;
        DistanceOption cueDist = DistanceOption.Medium;

        Color activeColor = new Color(0.2f, 0.8f, 0.4f, 1f);
        Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);

        void Start()
        {
            SetupAngleDropdown();
            SetupPocketDropdown();

            btnTargetNear.onClick.AddListener(() => SetTargetDist(DistanceOption.Near));
            btnTargetMid.onClick.AddListener(() => SetTargetDist(DistanceOption.Medium));
            btnTargetFar.onClick.AddListener(() => SetTargetDist(DistanceOption.Far));
            btnCueNear.onClick.AddListener(() => SetCueDist(DistanceOption.Near));
            btnCueMid.onClick.AddListener(() => SetCueDist(DistanceOption.Medium));
            btnCueFar.onClick.AddListener(() => SetCueDist(DistanceOption.Far));

            btnGenerate.onClick.AddListener(OnGenerate);
            btnRandom.onClick.AddListener(OnRandom);

            UpdateDistButtons();
        }

        void SetupAngleDropdown()
        {
            if (angleDropdown == null) return;
            angleDropdown.ClearOptions();
            var opts = new System.Collections.Generic.List<string> { "Any" };
            for (int a = 0; a <= 90; a += 5)
                opts.Add(a + "°");
            angleDropdown.AddOptions(opts);
            angleDropdown.value = 0;
        }

        void SetupPocketDropdown()
        {
            if (pocketDropdown == null) return;
            pocketDropdown.ClearOptions();
            var opts = new System.Collections.Generic.List<string> { "Random", "1", "2", "3", "4", "5", "6" };
            pocketDropdown.AddOptions(opts);
            pocketDropdown.value = 0;
        }

        void SetTargetDist(DistanceOption d)
        {
            targetDist = d;
            UpdateDistButtons();
        }

        void SetCueDist(DistanceOption d)
        {
            cueDist = d;
            UpdateDistButtons();
        }

        void UpdateDistButtons()
        {
            SetBtnColor(btnTargetNear, targetDist == DistanceOption.Near);
            SetBtnColor(btnTargetMid, targetDist == DistanceOption.Medium);
            SetBtnColor(btnTargetFar, targetDist == DistanceOption.Far);
            SetBtnColor(btnCueNear, cueDist == DistanceOption.Near);
            SetBtnColor(btnCueMid, cueDist == DistanceOption.Medium);
            SetBtnColor(btnCueFar, cueDist == DistanceOption.Far);
        }

        void SetBtnColor(UnityEngine.UI.Button btn, bool active)
        {
            if (btn == null) return;
            var img = btn.GetComponent<UnityEngine.UI.Image>();
            if (img != null) img.color = active ? activeColor : inactiveColor;
        }

        PuzzleParams BuildParams()
        {
            var p = new PuzzleParams();

            int angleIdx = angleDropdown != null ? angleDropdown.value : 0;
            if (angleIdx == 0)
            {
                p.anyAngle = true;
                p.cutAngleDeg = -1f;
            }
            else
            {
                p.anyAngle = false;
                p.cutAngleDeg = (angleIdx - 1) * 5f;
            }

            p.targetToPocket = targetDist;
            p.cueToTarget = cueDist;
            p.pocketIndex = pocketDropdown != null ? pocketDropdown.value : 0;

            return p;
        }

        void OnGenerate()
        {
            if (generator == null) return;
            generator.Generate(BuildParams());
        }

        void OnRandom()
        {
            if (generator == null) return;
            var p = new PuzzleParams
            {
                anyAngle = true,
                cutAngleDeg = -1f,
                targetToPocket = (DistanceOption)Random.Range(0, 3),
                cueToTarget = (DistanceOption)Random.Range(0, 3),
                pocketIndex = 0
            };
            generator.Generate(p);
        }
    }
}
