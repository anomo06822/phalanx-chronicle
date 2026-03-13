using System.Collections.Generic;
using System.Linq;

namespace PhalanxChronicle.Core
{
    public sealed class ScenarioDirector
    {
        private readonly BattleScenarioData scenario;
        private readonly HashSet<string> firedTriggerIds = new HashSet<string>();
        private readonly HashSet<string> consumedGroupIds = new HashSet<string>();
        private readonly HashSet<string> activeFlags = new HashSet<string>();
        private readonly HashSet<string> triggeredDuelIds = new HashSet<string>();
        private readonly Queue<ScenarioDialogueLine> pendingDialogue = new Queue<ScenarioDialogueLine>();

        public ScenarioDirector(BattleScenarioData scenario)
        {
            this.scenario = scenario;
        }

        public ObjectiveState CurrentObjective { get; private set; }

        public bool HasPendingDialogue => pendingDialogue.Count > 0;

        public bool HasFlag(string flagName)
        {
            return !string.IsNullOrEmpty(flagName) && activeFlags.Contains(flagName);
        }

        public IReadOnlyList<string> ActiveFlags => activeFlags.OrderBy(flag => flag).ToList();

        public IReadOnlyList<string> TriggeredDuelIds => triggeredDuelIds.OrderBy(id => id).ToList();

        public ScenarioDialogueLine PeekDialogue()
        {
            return pendingDialogue.Count > 0 ? pendingDialogue.Peek() : null;
        }

        public bool AdvanceDialogue()
        {
            if (pendingDialogue.Count == 0)
            {
                return false;
            }

            pendingDialogue.Dequeue();
            return pendingDialogue.Count > 0;
        }

        public DuelSceneDefinition TryMatchDuel(string attackerUnitId, string defenderUnitId, BattleContext context, bool playerInitiated = true)
        {
            if (scenario?.DuelScenes == null || context == null)
            {
                return null;
            }

            foreach (DuelSceneDefinition duelScene in scenario.DuelScenes)
            {
                if (duelScene != null &&
                    duelScene.Matches(attackerUnitId, defenderUnitId, context, activeFlags, triggeredDuelIds, playerInitiated))
                {
                    return duelScene;
                }
            }

            return null;
        }

        public void RecordDuelTriggered(string duelId, IReadOnlyList<string> newFlags = null)
        {
            if (!string.IsNullOrWhiteSpace(duelId))
            {
                triggeredDuelIds.Add(duelId);
            }

            if (newFlags == null)
            {
                return;
            }

            foreach (string flag in newFlags.Where(flag => !string.IsNullOrWhiteSpace(flag)))
            {
                activeFlags.Add(flag);
            }
        }

        public ScenarioEvaluationResult Evaluate(ScenarioCheckpoint checkpoint, BattleContext context)
        {
            if (scenario == null || context == null)
            {
                return new ScenarioEvaluationResult(new List<string>(), false, false, false, false);
            }

            List<string> spawnedUnitIds = new List<string>();
            bool objectiveChanged = false;
            bool battleOutcomeChanged = false;
            bool battlefieldChanged = false;

            foreach (ScenarioTrigger trigger in scenario.Triggers)
            {
                if (trigger == null ||
                    (trigger.FireOnce && firedTriggerIds.Contains(trigger.Id)) ||
                    (!string.IsNullOrEmpty(trigger.ExclusivityGroupId) && consumedGroupIds.Contains(trigger.ExclusivityGroupId)) ||
                    !trigger.Matches(context, checkpoint, activeFlags))
                {
                    continue;
                }

                firedTriggerIds.Add(trigger.Id);
                if (!string.IsNullOrEmpty(trigger.ExclusivityGroupId))
                {
                    consumedGroupIds.Add(trigger.ExclusivityGroupId);
                }

                foreach (ScenarioDirective directive in trigger.Directives)
                {
                    switch (directive.Type)
                    {
                        case ScenarioDirectiveType.QueueDialogue:
                            if (directive.DialogueLines != null)
                            {
                                foreach (ScenarioDialogueLine line in directive.DialogueLines)
                                {
                                    pendingDialogue.Enqueue(line);
                                }
                            }
                            break;
                        case ScenarioDirectiveType.UpdateObjective:
                            CurrentObjective = directive.ObjectiveState;
                            objectiveChanged = true;
                            break;
                        case ScenarioDirectiveType.SpawnUnits:
                            if (directive.UnitSpawns != null)
                            {
                                foreach (UnitSpawnData unitSpawn in directive.UnitSpawns)
                                {
                                    UnitRuntimeState spawnedUnit = context.AddUnit(unitSpawn);
                                    if (spawnedUnit == null)
                                    {
                                        continue;
                                    }

                                    spawnedUnitIds.Add(spawnedUnit.Id);
                                }

                                if (spawnedUnitIds.Count > 0 &&
                                    context.BattleEnded &&
                                    context.WinningSide == TurnSide.Player)
                                {
                                    context.ClearBattleOutcome();
                                    battleOutcomeChanged = true;
                                }
                            }
                            break;
                        case ScenarioDirectiveType.SetBattleOutcome:
                            if (directive.WinningSide.HasValue)
                            {
                                context.SetBattleOutcome(directive.WinningSide.Value);
                                battleOutcomeChanged = true;
                            }
                            break;
                        case ScenarioDirectiveType.SetFlag:
                            if (!string.IsNullOrEmpty(directive.FlagName))
                            {
                                activeFlags.Add(directive.FlagName);
                            }
                            break;
                        case ScenarioDirectiveType.ApplyBattlefieldMutation:
                            if (directive.BattlefieldMutation != null &&
                                directive.BattlefieldMutation.HasAnyChange &&
                                context.ApplyBattlefieldMutation(directive.BattlefieldMutation))
                            {
                                battlefieldChanged = true;
                            }
                            break;
                    }
                }
            }

            return new ScenarioEvaluationResult(spawnedUnitIds, objectiveChanged, battleOutcomeChanged, pendingDialogue.Count > 0, battlefieldChanged);
        }
    }
}
