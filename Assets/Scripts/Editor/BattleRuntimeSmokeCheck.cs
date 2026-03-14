#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using PhalanxChronicle.Battle;
using PhalanxChronicle.Battle.Grid;
using PhalanxChronicle.Battle.States;
using PhalanxChronicle.Battle.Units;
using PhalanxChronicle.Core;
using PhalanxChronicle.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Text = TMPro.TMP_Text;

namespace PhalanxChronicle.Editor
{
    public static class BattleRuntimeSmokeCheck
    {
        private const string ScenePath = "Assets/Scenes/Battle.unity";
        private const string ResultPath = "/tmp/phalanx-chronicle-unity-logs/runtime-smoke-result.txt";

        [MenuItem("Phalanx Chronicle/Run Runtime Smoke Check")]
        public static void RunFromMenu()
        {
            RunInternal(exitOnComplete: false);
        }

        public static void Run()
        {
            RunInternal(exitOnComplete: true);
        }

        private static void RunInternal(bool exitOnComplete)
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                if (!exitOnComplete)
                {
                    EditorApplication.delayCall += RunFromMenu;
                }

                return;
            }

            string originalScenePath = EditorSceneManager.GetActiveScene().path;
            try
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

                GameManager gameManager = UnityEngine.Object.FindObjectOfType<GameManager>();
                if (gameManager == null)
                {
                    GameObject gameManagerObject = new GameObject("GameManager");
                    gameManager = gameManagerObject.AddComponent<GameManager>();
                }

                InvokeLifecycle(gameManager, "Awake");

                BattleManager battleManager = gameManager.GetComponentInChildren<BattleManager>();
                if (battleManager == null)
                {
                    throw new InvalidOperationException("BattleManager was not created by GameManager.");
                }

                InvokeLifecycle(battleManager, "Awake");
                InvokeLifecycle(battleManager, "Start");

                GridManager gridManager = UnityEngine.Object.FindObjectOfType<GridManager>();
                Canvas canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
                Unit[] units = UnityEngine.Object.FindObjectsOfType<Unit>();
                GameObject selectedUnitPanel = GameObject.Find("SelectedUnitPanel");
                GameObject overviewPanel = GameObject.Find("OverviewPanel");
                GameObject forecastPanel = GameObject.Find("ForecastPanel");
                GameObject alliedRosterPanel = GameObject.Find("AlliedRosterPanel");
                GameObject enemyRosterPanel = GameObject.Find("EnemyRosterPanel");
                GameObject feedPanel = GameObject.Find("FeedPanel");
                string[] rootNames = EditorSceneManager.GetActiveScene()
                    .GetRootGameObjects()
                    .Select(root => root.name)
                    .OrderBy(name => name)
                    .ToArray();

                if (gridManager == null)
                {
                    throw new InvalidOperationException("GridManager was not created.");
                }

                if (canvas == null)
                {
                    throw new InvalidOperationException("Battle canvas was not created.");
                }

                if (!battleManager.IsCampaignOverlayVisible || battleManager.Simulation != null)
                {
                    throw new InvalidOperationException("Expected campaign stage selection to appear before any battle starts.");
                }

                BattleHUD battleHud = UnityEngine.Object.FindObjectOfType<BattleHUD>();
                Text campaignBodyLabel = GetPrivateField<Text>(battleHud, "campaignBodyLabel");
                Button campaignPrimaryButton = GetPrivateField<Button>(battleHud, "campaignPrimaryButton");
                if (campaignBodyLabel == null ||
                    !ContainsAny(campaignBodyLabel.text, "15-20", "15-20 分鐘", "15-20 minutes"))
                {
                    throw new InvalidOperationException("Expected first-launch intro copy to describe the first-session length.");
                }

                Text primaryButtonLabel = campaignPrimaryButton != null ? campaignPrimaryButton.GetComponentInChildren<Text>(true) : null;
                if (primaryButtonLabel == null ||
                    !ContainsAny(primaryButtonLabel.text, "開始首場戰鬥", "Start First Battle"))
                {
                    throw new InvalidOperationException("Expected the first-launch overlay to expose the guided first-battle CTA.");
                }

                gameManager.StartCampaignStage(0);
                units = UnityEngine.Object.FindObjectsOfType<Unit>();

                if (units.Length == 0 || battleManager.Simulation == null)
                {
                    throw new InvalidOperationException("Expected the first campaign stage to spawn runtime units.");
                }

                if (selectedUnitPanel == null || overviewPanel == null || forecastPanel == null)
                {
                    throw new InvalidOperationException("Expected the rebuilt HUD panels to exist.");
                }

                if (alliedRosterPanel == null || alliedRosterPanel.GetComponent<ScrollRect>() == null ||
                    enemyRosterPanel == null || enemyRosterPanel.GetComponent<ScrollRect>() == null ||
                    feedPanel == null || feedPanel.GetComponent<ScrollRect>() == null)
                {
                    throw new InvalidOperationException("Expected the overview ledgers and feed to use scrollable panels.");
                }

                if (!battleManager.IsDialogueVisible)
                {
                    throw new InvalidOperationException("Expected opening dialogue to be visible on startup.");
                }

                for (int index = 0; index < 8 && battleManager.IsDialogueVisible; index++)
                {
                    battleManager.AdvanceScenarioDialogue();
                }

                if (battleManager.IsRerollVisible)
                {
                    throw new InvalidOperationException("Fixed story scenario should not expose reroll.");
                }

                if (!battleManager.IsOnboardingVisible)
                {
                    throw new InvalidOperationException("Expected Guangzong onboarding prompts to appear after the opening dialogue.");
                }

                battleManager.ChangeState<UnitSelectionState>();
                Unit liuBeiView = units.Single(unit => unit.UnitId == "player-liu-bei");
                battleManager.Simulation.Context.GetUnit("player-guan-yu").ApplyDamage(6);
                InvokeMethod(battleManager, "OnUnitClicked", liuBeiView);
                InvokeMethod(battleManager, "OnUnitClicked", liuBeiView);

                if (!battleManager.IsActionMenuVisible)
                {
                    throw new InvalidOperationException("Expected clicking the selected unit to open the hold-position action menu.");
                }

                if (battleManager.Simulation.Context.GetUnit("player-liu-bei").HasActed)
                {
                    throw new InvalidOperationException("Hold-position action menu should not immediately mark the unit as acted.");
                }

                if (!ContainsAny(battleManager.CurrentActionMenuModeText, "原地行動", "Hold Position"))
                {
                    throw new InvalidOperationException("Expected the action menu to show hold-position context.");
                }

                ActionMenuPanel actionMenuPanel = UnityEngine.Object.FindObjectOfType<ActionMenuPanel>();
                Text contextHintLabel = GetPrivateField<Text>(actionMenuPanel, "contextHintLabel");
                if (contextHintLabel == null || string.IsNullOrWhiteSpace(contextHintLabel.text))
                {
                    throw new InvalidOperationException("Expected the action menu to expose the new context hint copy.");
                }

                AssertActionCardReadable(actionMenuPanel, "attackButtonView", "Attack");
                AssertActionCardReadable(actionMenuPanel, "skillButtonView", "Skill");
                AssertActionCardReadable(actionMenuPanel, "waitButtonView", "Wait");
                AssertActionCardReadable(actionMenuPanel, "backButtonView", "Back");

                if (battleManager.IsActionMenuBackEnabled)
                {
                    throw new InvalidOperationException("Back should be disabled while the unit has not moved.");
                }

                if (!battleManager.IsActionMenuSkillEnabled)
                {
                    throw new InvalidOperationException("Expected hold-position action menu to allow using a skill when a valid target exists.");
                }

                Unit guanYuView = units.Single(unit => unit.UnitId == "player-guan-yu");
                InvokeMethod(battleManager, "OnUnitClicked", guanYuView);
                if (!battleManager.IsSelectedUnit("player-guan-yu") || battleManager.IsActionMenuVisible)
                {
                    throw new InvalidOperationException("Expected clicking another allied unit before moving to switch selection instead of locking the current unit.");
                }

                battleManager.ChangeState<UnitSelectionState>();
                InvokeMethod(battleManager, "OnUnitClicked", liuBeiView);
                GridCellView destinationCell = UnityEngine.Object.FindObjectsOfType<GridCellView>().Single(cell => cell.Position == new GridPosition(2, 4));
                InvokeMethod(battleManager, "OnCellClicked", destinationCell);

                if (!battleManager.IsActionMenuVisible || !battleManager.IsActionMenuBackEnabled)
                {
                    throw new InvalidOperationException("Expected moved action menu to be visible with Back enabled.");
                }

                if (!ContainsAny(battleManager.CurrentActionMenuModeText, "移動後行動", "After Move"))
                {
                    throw new InvalidOperationException("Expected the action menu to show after-move context.");
                }

                battleManager.TryUndoSelectionMove();
                battleManager.ChangeState<UnitSelectionState>();

                Unit armoredZealotView = units.Single(unit => unit.UnitId == "enemy-armored_zealot");
                TextMeshPro armoredCaptainName = GetPrivateField<TextMeshPro>(armoredZealotView, "nameText");
                if (armoredCaptainName == null || armoredCaptainName.font == null || armoredCaptainName.fontSize > 6.4f)
                {
                    throw new InvalidOperationException("Expected world-space unit names to use TMP-authored font assets with readable sizing.");
                }

                Text selectedNameLabel = GetPrivateField<Text>(battleHud, "selectedNameLabel");
                if (selectedNameLabel == null || !selectedNameLabel.enableAutoSizing)
                {
                    throw new InvalidOperationException("Expected selected unit name label to use TMP auto sizing.");
                }

                object selectedUnitView = GetPrivateField<object>(battleHud, "selectedUnitView");
                InvokeMethod(selectedUnitView, "ToggleDetails");
                Transform detailLinesRoot = GetPrivateField<Transform>(selectedUnitView, "detailLinesRoot");
                if (detailLinesRoot == null || detailLinesRoot.childCount == 0)
                {
                    throw new InvalidOperationException("Expected selected unit details to render at least one line.");
                }

                foreach (Transform child in detailLinesRoot.Cast<Transform>().Take(3))
                {
                    Text detailText = child.GetComponent<Text>();
                    if (detailText == null)
                    {
                        continue;
                    }

                    AssertReadableText(detailText, 18f, "selected-detail");
                }

                ScrollRect alliedScroll = alliedRosterPanel.GetComponent<ScrollRect>();
                if (alliedScroll == null || alliedScroll.viewport == null)
                {
                    throw new InvalidOperationException("Expected allied roster panel to expose a viewport.");
                }

                float rosterViewportMinHeight = 5f * 84f + 4f * 6f;
                if (alliedScroll.viewport.rect.height + 0.5f < rosterViewportMinHeight)
                {
                    throw new InvalidOperationException("Expected overview roster viewport to fit at least five entries on first screen.");
                }

                DefeatUnit(battleManager.Simulation.Context, "enemy-yellow_turban_raider");
                DefeatUnit(battleManager.Simulation.Context, "enemy-armored_zealot");
                bool enteredDialogue = battleManager.ProcessScenarioCheckpointAndEnterDialogue(
                    ScenarioCheckpoint.ActionResolved,
                    typeof(UnitSelectionState));

                if (!enteredDialogue || !battleManager.IsDialogueVisible)
                {
                    throw new InvalidOperationException("Expected reinforcements to trigger scenario dialogue.");
                }

                if (!battleManager.HasScenarioFlag(BattleScenarioCatalog.GuangzongReinforcementsArrivedFlag))
                {
                    throw new InvalidOperationException("Scenario reinforcement flag was not set.");
                }

                if (battleManager.Simulation.Context.GetUnit("enemy-zhang-liang") == null)
                {
                    throw new InvalidOperationException("Reinforcement unit Zhang Liang was not spawned.");
                }

                if (!battleManager.CurrentObjectiveText.Contains("張寶") || !battleManager.CurrentObjectiveText.Contains("張梁"))
                {
                    throw new InvalidOperationException("Objective HUD did not update after reinforcements.");
                }

                gameManager.StartCampaignStage(1);
                for (int index = 0; index < 8 && battleManager.IsDialogueVisible; index++)
                {
                    battleManager.AdvanceScenarioDialogue();
                }

                if (!battleManager.CurrentObjectiveText.Contains("第五回合") && !battleManager.CurrentObjectiveText.Contains("fifth round"))
                {
                    throw new InvalidOperationException("Expected Changban objective text to mention the fifth-round hold objective.");
                }

                gameManager.StartCampaignStage(2);
                for (int index = 0; index < 8 && battleManager.IsDialogueVisible; index++)
                {
                    battleManager.AdvanceScenarioDialogue();
                }

                DefeatUnit(battleManager.Simulation.Context, "enemy-wei_vanguard_captain");
                DefeatUnit(battleManager.Simulation.Context, "enemy-wei_archer_captain");
                bool dingjunDialogue = battleManager.ProcessScenarioCheckpointAndEnterDialogue(
                    ScenarioCheckpoint.ActionResolved,
                    typeof(UnitSelectionState));

                if (!dingjunDialogue || !battleManager.HasScenarioFlag(BattleScenarioCatalog.DingjunBossArrivedFlag))
                {
                    throw new InvalidOperationException("Expected Dingjun boss phase to begin after both forward captains fall.");
                }

                if (battleManager.Simulation.Context.GetUnit("enemy-xiahou-yuan") == null)
                {
                    throw new InvalidOperationException("Expected Xiahou Yuan to spawn for the Dingjun boss phase.");
                }

                Debug.Log(
                    $"[BattleRuntimeSmokeCheck] PASS roots=[{string.Join(", ", rootNames)}] " +
                    $"units={units.Length} canvas={canvas.name} grid={gridManager.name}");
                WriteResult(
                    $"PASS roots=[{string.Join(", ", rootNames)}] " +
                    $"units={units.Length} canvas={canvas.name} grid={gridManager.name}");

                if (exitOnComplete)
                {
                    EditorApplication.Exit(0);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError("[BattleRuntimeSmokeCheck] FAIL\n" + exception);
                WriteResult("FAIL\n" + exception);
                if (exitOnComplete)
                {
                    EditorApplication.Exit(1);
                }
            }
            finally
            {
                if (!exitOnComplete)
                {
                    string sceneToRestore = string.IsNullOrEmpty(originalScenePath) ? ScenePath : originalScenePath;
                    EditorSceneManager.OpenScene(sceneToRestore, OpenSceneMode.Single);
                }
            }
        }

        private static void InvokeLifecycle(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new MissingMethodException(target.GetType().Name, methodName);
            }

            method.Invoke(target, null);
        }

        private static void InvokeMethod(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new MissingMethodException(target.GetType().Name, methodName);
            }

            method.Invoke(target, args);
        }

        private static T GetPrivateField<T>(object target, string fieldName) where T : class
        {
            Type currentType = target != null ? target.GetType() : null;
            while (currentType != null)
            {
                FieldInfo fieldInfo = currentType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (fieldInfo != null)
                {
                    return fieldInfo.GetValue(target) as T;
                }

                currentType = currentType.BaseType;
            }

            return null;
        }

        private static void AssertActionCardReadable(ActionMenuPanel panel, string fieldName, string debugName)
        {
            object card = GetPrivateField<object>(panel, fieldName);
            if (card == null)
            {
                throw new InvalidOperationException($"Expected action card '{debugName}' to exist.");
            }

            Text title = GetPrivateField<Text>(card, "<TitleLabel>k__BackingField");
            Text outcome = GetPrivateField<Text>(card, "<OutcomeLabel>k__BackingField");
            if (title == null || outcome == null)
            {
                throw new InvalidOperationException($"Expected action card '{debugName}' labels to be bound.");
            }

            AssertReadableText(title, 18f, $"{debugName}-title");
            AssertReadableText(outcome, 32f, $"{debugName}-outcome");
        }

        private static void AssertReadableText(Text text, float minimumHeight, string debugName)
        {
            if (text == null || string.IsNullOrWhiteSpace(text.text))
            {
                throw new InvalidOperationException($"Expected readable text for '{debugName}'.");
            }

            float measuredHeight = RefreshMeasuredHeight(text, minimumHeight);
            RectTransform rect = text.rectTransform;
            if (rect == null || rect.rect.height + 0.5f < minimumHeight || measuredHeight + 0.5f < minimumHeight)
            {
                throw new InvalidOperationException($"Expected '{debugName}' to reserve enough height for readable text.");
            }
        }

        private static float RefreshMeasuredHeight(Text text, float minimumHeight)
        {
            TextMeshProUGUI tmp = text as TextMeshProUGUI;
            if (tmp == null)
            {
                return minimumHeight;
            }

            LayoutElement layout = tmp.GetComponent<LayoutElement>();
            if (layout == null)
            {
                return minimumHeight;
            }

            layout.preferredHeight = -1f;
            Canvas.ForceUpdateCanvases();
            float availableWidth = tmp.rectTransform.rect.width;
            if (availableWidth <= 1f)
            {
                availableWidth = 600f;
            }

            float measuredHeight = Mathf.Ceil(tmp.GetPreferredValues(tmp.text, availableWidth, 0f).y);
            float finalHeight = Mathf.Max(minimumHeight, measuredHeight);
            layout.preferredHeight = finalHeight;
            return finalHeight;
        }

        private static bool ContainsAny(string text, params string[] candidates)
        {
            return candidates.Any(candidate => !string.IsNullOrEmpty(candidate) && text.IndexOf(candidate, StringComparison.Ordinal) >= 0);
        }

        private static void WriteResult(string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ResultPath) ?? "/tmp");
            File.WriteAllText(ResultPath, content);
        }

        private static void DefeatUnit(BattleContext context, string unitId)
        {
            UnitRuntimeState unit = context.GetUnit(unitId);
            if (unit == null)
            {
                throw new InvalidOperationException($"Missing unit {unitId}.");
            }

            unit.ApplyDamage(unit.CurrentHp);
            context.RemoveUnit(unitId);
        }
    }
}
#endif
