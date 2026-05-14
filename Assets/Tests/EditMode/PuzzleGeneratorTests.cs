using NUnit.Framework;
using PoolAimTrainer.Puzzles;
using PoolAimTrainer.SceneObjects;
using PoolAimTrainer.Visualization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PoolAimTrainer.Tests.EditMode
{
    public class PuzzleGeneratorTests
    {
        GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root != null)
                Object.DestroyImmediate(root);
        }

        [Test]
        public void ComputeCuePositionForRedGreenAngle_MatchesDisplayedAngle()
        {
            var target = Vector3.zero;
            var pocketDir = Vector3.right;
            var pocket = target + pocketDir;

            Vector3 cue = PuzzleGenerator.ComputeCuePositionForRedGreenAngle(
                target, pocketDir, 5f, 0.5f, 1);

            float displayedAngle = CutAngleArcRenderer.ComputeRedGreenAngleDegrees(cue, target, pocket);

            Assert.That(displayedAngle, Is.EqualTo(5f).Within(0.001f));
        }

        [Test]
        public void Start_ReplacesAngleDropdownWithFourColumnAngleButtons()
        {
            root = new GameObject("PuzzleUITestRoot");
            var section = new GameObject("Section", typeof(RectTransform)).transform;
            section.SetParent(root.transform, false);

            var angleRow = new GameObject("AngleRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            angleRow.transform.SetParent(section, false);
            var label = new GameObject("Lbl", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            label.transform.SetParent(angleRow.transform, false);
            label.text = "切角";
            var dropdown = new GameObject("Dropdown", typeof(RectTransform), typeof(TMP_Dropdown));
            dropdown.transform.SetParent(angleRow.transform, false);

            var ui = root.AddComponent<PuzzleUI>();
            ui.angleDropdown = dropdown.GetComponent<TMP_Dropdown>();

            InvokeStart(ui);

            Transform grid = angleRow.transform.Find("AngleButtonGrid");
            Assert.That(grid, Is.Not.Null);
            Assert.That(ui.angleDropdown.gameObject.activeSelf, Is.False);
            Assert.That(grid.GetComponent<GridLayoutGroup>().constraintCount, Is.EqualTo(4));
            Assert.That(grid.childCount, Is.EqualTo(20));

            var button = grid.GetChild(2).GetComponent<Button>();
            button.onClick.Invoke();

            Assert.That(ui.angleDropdown.value, Is.EqualTo(2));
        }

        [Test]
        public void Start_ReplacesPocketDropdownWithClickablePocketMap()
        {
            root = new GameObject("PuzzleUITestRoot");
            var section = new GameObject("Section", typeof(RectTransform)).transform;
            section.SetParent(root.transform, false);

            var pocketRow = new GameObject("PocketRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            pocketRow.transform.SetParent(section, false);
            var label = new GameObject("Lbl", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            label.transform.SetParent(pocketRow.transform, false);
            label.text = "袋口";
            var dropdown = new GameObject("Dropdown", typeof(RectTransform), typeof(TMP_Dropdown));
            dropdown.transform.SetParent(pocketRow.transform, false);

            var ui = root.AddComponent<PuzzleUI>();
            ui.pocketDropdown = dropdown.GetComponent<TMP_Dropdown>();

            InvokeStart(ui);

            Transform map = pocketRow.transform.Find("PocketMap");
            Transform random = pocketRow.transform.Find("PocketRandom");
            Assert.That(map, Is.Not.Null);
            Assert.That(random, Is.Not.Null);
            Assert.That(ui.pocketDropdown.gameObject.activeSelf, Is.False);
            Assert.That(map.Find("Pocket_4").GetComponent<RectTransform>().anchorMin.x, Is.EqualTo(0.94f).Within(0.001f));
            Assert.That(map.Find("Pocket_6").GetComponent<RectTransform>().anchorMin.x, Is.EqualTo(0.06f).Within(0.001f));

            var pocket4 = map.Find("Pocket_4").GetComponent<Button>();
            pocket4.onClick.Invoke();

            Assert.That(ui.pocketDropdown.value, Is.EqualTo(4));

            random.GetComponent<Button>().onClick.Invoke();

            Assert.That(ui.pocketDropdown.value, Is.EqualTo(0));
        }

        [Test]
        public void Start_ReplacesModeDropdownWithStudyAndExamButtons()
        {
            root = new GameObject("PuzzleUITestRoot");
            var section = new GameObject("Section", typeof(RectTransform)).transform;
            section.SetParent(root.transform, false);

            var modeRow = new GameObject("ModeRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            modeRow.transform.SetParent(section, false);
            var label = new GameObject("Lbl", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            label.transform.SetParent(modeRow.transform, false);
            label.text = "模式";
            var dropdown = new GameObject("Dropdown", typeof(RectTransform), typeof(TMP_Dropdown));
            dropdown.transform.SetParent(modeRow.transform, false);

            var controller = root.AddComponent<TrainingModeController>();
            var ui = root.AddComponent<PuzzleUI>();
            ui.trainingModeController = controller;
            ui.modeDropdown = dropdown.GetComponent<TMP_Dropdown>();

            InvokeStart(ui);

            Transform study = modeRow.transform.Find("BtnStudyMode");
            Transform exam = modeRow.transform.Find("BtnExamMode");
            Assert.That(study, Is.Not.Null);
            Assert.That(exam, Is.Not.Null);
            Assert.That(ui.modeDropdown.gameObject.activeSelf, Is.False);
            Assert.That(study.GetComponentInChildren<TextMeshProUGUI>().text, Is.EqualTo("学习"));
            Assert.That(exam.GetComponentInChildren<TextMeshProUGUI>().text, Is.EqualTo("考试"));

            exam.GetComponent<Button>().onClick.Invoke();
            Assert.That(controller.mode, Is.EqualTo(TrainingMode.Exam));
            Assert.That(exam.GetComponent<Image>().color, Is.EqualTo(new Color(0.2f, 0.8f, 0.4f, 1f)));

            study.GetComponent<Button>().onClick.Invoke();
            Assert.That(controller.mode, Is.EqualTo(TrainingMode.Study));
            Assert.That(study.GetComponent<Image>().color, Is.EqualTo(new Color(0.2f, 0.8f, 0.4f, 1f)));
        }

        [Test]
        public void Start_WiresMainGenerateButtonToSameGenerateEntryPoint()
        {
            root = new GameObject("PuzzleUITestRoot");
            var toolbar = new GameObject("LeftToolbar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            toolbar.transform.SetParent(root.transform, false);
            var rightGenerateGo = new GameObject("BtnGenerate", typeof(RectTransform), typeof(Image), typeof(Button));
            rightGenerateGo.transform.SetParent(root.transform, false);
            var rightGenerate = rightGenerateGo.GetComponent<Button>();

            var ui = root.AddComponent<SpyPuzzleUI>();
            ui.btnGenerate = rightGenerate;

            InvokeStart(ui);

            Transform mainGenerate = toolbar.transform.Find("BtnMainGenerate");
            Assert.That(mainGenerate, Is.Not.Null);
            Assert.That(mainGenerate.GetComponentInChildren<TextMeshProUGUI>().text, Is.EqualTo("出题"));

            rightGenerate.onClick.Invoke();
            mainGenerate.GetComponent<Button>().onClick.Invoke();

            Assert.That(ui.generateCalls, Is.EqualTo(2));
        }

        [Test]
        public void ResolvePocketIndex_UsesPocketMarkerNumberInsteadOfListOrder()
        {
            root = new GameObject("PuzzleGeneratorTestRoot");
            var table = root.AddComponent<TableController>();
            table.Pockets.Add(CreatePocket("Pocket_TM", 2));
            table.Pockets.Add(CreatePocket("Pocket_TR", 3));
            table.Pockets.Add(CreatePocket("Pocket_TL", 1));

            var generator = root.AddComponent<PuzzleGenerator>();
            generator.table = table;

            int resolved = (int)typeof(PuzzleGenerator)
                .GetMethod("ResolvePocketIndex", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(generator, new object[] { 3 });

            Assert.That(resolved, Is.EqualTo(1));
        }

        PocketMarker CreatePocket(string name, int number)
        {
            var pocketGo = new GameObject(name);
            pocketGo.transform.SetParent(root.transform, false);
            var pocket = pocketGo.AddComponent<PocketMarker>();
            pocket.pocketNumber = number;
            return pocket;
        }

        class SpyPuzzleUI : PuzzleUI
        {
            public int generateCalls;

            public override bool GenerateCurrentPuzzle()
            {
                generateCalls++;
                return true;
            }
        }

        static void InvokeStart(PuzzleUI ui)
        {
            typeof(PuzzleUI)
                .GetMethod("Start", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(ui, null);
        }
    }
}
