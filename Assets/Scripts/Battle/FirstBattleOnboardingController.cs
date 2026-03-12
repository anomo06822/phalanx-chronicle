using PhalanxChronicle.Localization;
using PhalanxChronicle.UI;

namespace PhalanxChronicle.Battle
{
    internal sealed class FirstBattleOnboardingController
    {
        public const string GuangzongScenarioId = "scenario.guangzong";

        private string latestObjectiveText;
        private string pendingObjectiveUpdateText = string.Empty;
        private bool hasSelectedUnit;
        private bool hasMovedUnit;
        private bool hasResolvedAction;
        private bool hasSeenEnemyTurn;
        private bool hasResolvedAttack;
        private bool skipped;
        private bool completed;

        public FirstBattleOnboardingController(string initialObjectiveText)
        {
            latestObjectiveText = initialObjectiveText ?? string.Empty;
        }

        public bool IsActive => !skipped && !completed;

        public bool WasSkipped => skipped;

        public bool WasCompleted => completed;

        public void MarkSelectedUnit()
        {
            hasSelectedUnit = true;
        }

        public void MarkMovedUnit()
        {
            hasMovedUnit = true;
        }

        public void MarkPlayerActionResolved(bool wasAttack)
        {
            hasResolvedAction = true;
            if (wasAttack)
            {
                hasResolvedAttack = true;
                completed = true;
            }
        }

        public void MarkEnemyTurnSeen()
        {
            hasSeenEnemyTurn = true;
        }

        public void MarkBattleCompleted()
        {
            completed = true;
        }

        public void Skip()
        {
            skipped = true;
        }

        public void UpdateObjective(string objectiveText)
        {
            if (string.IsNullOrWhiteSpace(objectiveText) || objectiveText == latestObjectiveText)
            {
                return;
            }

            latestObjectiveText = objectiveText;
            pendingObjectiveUpdateText = objectiveText;
        }

        public void DismissObjectiveUpdate()
        {
            pendingObjectiveUpdateText = string.Empty;
        }

        public BattleOnboardingModel BuildModel(
            bool hasSelection,
            bool hasSelectionMoved,
            bool hasAttackTargets,
            bool anyPlayerCanAttack,
            bool allPlayerUnitsDone)
        {
            if (!IsActive)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(pendingObjectiveUpdateText))
            {
                return new BattleOnboardingModel
                {
                    ProgressLabel = LocalizationService.Text("onboarding.first_battle.progress_update", "Objective Update"),
                    Title = LocalizationService.Text("onboarding.first_battle.title_update", "The battle plan changed."),
                    Body = LocalizationService.Format("onboarding.first_battle.body_update", "Read the refreshed objective and forecast, then push toward the new goal.\n{0}", pendingObjectiveUpdateText),
                    HintText = LocalizationService.Text("onboarding.first_battle.hint_update", "Scenario dialogue and the objective panel will always explain major battlefield changes."),
                    SkipLabel = LocalizationService.Text("ui.button.skip", "Skip"),
                    CanSkip = true,
                };
            }

            if (!hasSelectedUnit)
            {
                return new BattleOnboardingModel
                {
                    ProgressLabel = LocalizationService.Text("onboarding.first_battle.progress_1", "Step 1 / 5"),
                    Title = LocalizationService.Text("onboarding.first_battle.title_select", "Select a blue officer."),
                    Body = LocalizationService.Text("onboarding.first_battle.body_select", "Start by clicking any allied unit. The Character Dossier and movement range will update around that officer."),
                    HintText = LocalizationService.Text("onboarding.first_battle.hint_select", "Blue units are yours. Pick Liu Bei or any frontline ally to begin."),
                    SkipLabel = LocalizationService.Text("ui.button.skip", "Skip"),
                    CanSkip = true,
                };
            }

            if (!hasMovedUnit && hasSelection && !hasSelectionMoved)
            {
                return new BattleOnboardingModel
                {
                    ProgressLabel = LocalizationService.Text("onboarding.first_battle.progress_2", "Step 2 / 5"),
                    Title = LocalizationService.Text("onboarding.first_battle.title_move", "Move into position."),
                    Body = LocalizationService.Text("onboarding.first_battle.body_move", "Blue tiles show your legal movement. Click a destination tile, or click the selected officer again to act in place."),
                    HintText = LocalizationService.Text("onboarding.first_battle.hint_move", "Stay in formation and advance toward the highlighted front."),
                    SkipLabel = LocalizationService.Text("ui.button.skip", "Skip"),
                    CanSkip = true,
                };
            }

            if (!hasResolvedAction)
            {
                return new BattleOnboardingModel
                {
                    ProgressLabel = LocalizationService.Text("onboarding.first_battle.progress_3", "Step 3 / 5"),
                    Title = hasAttackTargets
                        ? LocalizationService.Text("onboarding.first_battle.title_attack", "Choose your action.")
                        : LocalizationService.Text("onboarding.first_battle.title_wait", "Finish this officer's turn."),
                    Body = hasAttackTargets
                        ? LocalizationService.Text("onboarding.first_battle.body_attack", "Red targets are in range. Use Attack to strike now, or use Skill if its range and mana line say it is ready.")
                        : LocalizationService.Text("onboarding.first_battle.body_wait", "If no target is in range, choose Wait. The action menu now tells you why Attack or Skill may be unavailable."),
                    HintText = LocalizationService.Text("onboarding.first_battle.hint_action", "Action menu details, the forecast panel, and the selected unit card now describe the same result in plain terms."),
                    SkipLabel = LocalizationService.Text("ui.button.skip", "Skip"),
                    CanSkip = true,
                };
            }

            if (!hasSeenEnemyTurn && !allPlayerUnitsDone)
            {
                return new BattleOnboardingModel
                {
                    ProgressLabel = LocalizationService.Text("onboarding.first_battle.progress_4", "Step 4 / 5"),
                    Title = LocalizationService.Text("onboarding.first_battle.title_end_turn", "Complete your turn."),
                    Body = LocalizationService.Text("onboarding.first_battle.body_end_turn", "Keep acting with the remaining blue officers. When everyone is finished, press End Turn to watch the enemy answer."),
                    HintText = LocalizationService.Text("onboarding.first_battle.hint_end_turn", "The War Overview panel shows how many allied units are still ready."),
                    SkipLabel = LocalizationService.Text("ui.button.skip", "Skip"),
                    CanSkip = true,
                };
            }

            if (!hasSeenEnemyTurn)
            {
                return null;
            }

            if (!hasResolvedAttack && anyPlayerCanAttack)
            {
                return new BattleOnboardingModel
                {
                    ProgressLabel = LocalizationService.Text("onboarding.first_battle.progress_5", "Step 5 / 5"),
                    Title = LocalizationService.Text("onboarding.first_battle.title_second_turn", "Now punish an exposed enemy."),
                    Body = LocalizationService.Text("onboarding.first_battle.body_second_turn", "When the enemy steps into range, the forecast panel will preview damage and status effects before you commit. Land one attack to finish the onboarding."),
                    HintText = LocalizationService.Text("onboarding.first_battle.hint_second_turn", "Use the objective panel and threat lines to decide which exposed foe to target first."),
                    SkipLabel = LocalizationService.Text("ui.button.skip", "Skip"),
                    CanSkip = true,
                };
            }

            return null;
        }
    }
}
