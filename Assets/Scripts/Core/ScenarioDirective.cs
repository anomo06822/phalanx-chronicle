using System.Collections.Generic;

namespace PhalanxChronicle.Core
{
    public sealed class ScenarioDirective
    {
        private ScenarioDirective(
            ScenarioDirectiveType type,
            IReadOnlyList<ScenarioDialogueLine> dialogueLines,
            ObjectiveState objectiveState,
            IReadOnlyList<UnitSpawnData> unitSpawns,
            TurnSide? winningSide,
            string flagName,
            BattlefieldMutation battlefieldMutation)
        {
            Type = type;
            DialogueLines = dialogueLines;
            ObjectiveState = objectiveState;
            UnitSpawns = unitSpawns;
            WinningSide = winningSide;
            FlagName = flagName;
            BattlefieldMutation = battlefieldMutation;
        }

        public ScenarioDirectiveType Type { get; }

        public IReadOnlyList<ScenarioDialogueLine> DialogueLines { get; }

        public ObjectiveState ObjectiveState { get; }

        public IReadOnlyList<UnitSpawnData> UnitSpawns { get; }

        public TurnSide? WinningSide { get; }

        public string FlagName { get; }

        public BattlefieldMutation BattlefieldMutation { get; }

        public static ScenarioDirective QueueDialogue(IReadOnlyList<ScenarioDialogueLine> dialogueLines)
        {
            return new ScenarioDirective(ScenarioDirectiveType.QueueDialogue, dialogueLines, null, null, null, null, null);
        }

        public static ScenarioDirective UpdateObjective(ObjectiveState objectiveState)
        {
            return new ScenarioDirective(ScenarioDirectiveType.UpdateObjective, null, objectiveState, null, null, null, null);
        }

        public static ScenarioDirective SpawnUnits(IReadOnlyList<UnitSpawnData> unitSpawns)
        {
            return new ScenarioDirective(ScenarioDirectiveType.SpawnUnits, null, null, unitSpawns, null, null, null);
        }

        public static ScenarioDirective SetBattleOutcome(TurnSide winningSide)
        {
            return new ScenarioDirective(ScenarioDirectiveType.SetBattleOutcome, null, null, null, winningSide, null, null);
        }

        public static ScenarioDirective SetFlag(string flagName)
        {
            return new ScenarioDirective(ScenarioDirectiveType.SetFlag, null, null, null, null, flagName, null);
        }

        public static ScenarioDirective ApplyBattlefieldMutation(BattlefieldMutation battlefieldMutation)
        {
            return new ScenarioDirective(ScenarioDirectiveType.ApplyBattlefieldMutation, null, null, null, null, null, battlefieldMutation);
        }
    }
}
