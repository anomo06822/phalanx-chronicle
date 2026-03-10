using System.Collections.Generic;
using System.Text;

namespace PhalanxChronicle.Core
{
    public static class BattleScenarioCatalog
    {
        public const string JieqiaoFiresScenarioId = "scenario.jieqiao_fires";
        public const string ReinforcementsArrivedFlag = "flag.reinforcements_arrived";

        public static BattleScenarioData CreateJieqiaoFires()
        {
            List<UnitSpawnData> openingSpawns = new List<UnitSpawnData>
            {
                new UnitSpawnData(CreateDefinition("player-liu-bei", "Liu Bei", UnitFaction.Player, UnitRole.Commander, PassiveSkillType.CommandAura, ActiveSkillType.RoyalAid, 30, 9, 5, 3, 1), new GridPosition(1, 4)),
                new UnitSpawnData(CreateDefinition("player-guan-yu", "Guan Yu", UnitFaction.Player, UnitRole.Guardian, PassiveSkillType.ArmorBreak, ActiveSkillType.GreenDragonSlash, 34, 12, 5, 3, 1), new GridPosition(1, 6)),
                new UnitSpawnData(CreateDefinition("player-zhang-fei", "Zhang Fei", UnitFaction.Player, UnitRole.Guardian, PassiveSkillType.Vanguard, ActiveSkillType.WarCry, 36, 11, 6, 3, 1), new GridPosition(1, 2)),
                new UnitSpawnData(CreateDefinition("player-huang-zhong", "Huang Zhong", UnitFaction.Player, UnitRole.Ranger, PassiveSkillType.LongShot, ActiveSkillType.Volley, 28, 10, 3, 3, 2), new GridPosition(0, 5)),
                new UnitSpawnData(CreateDefinition("enemy-zhang-bao", "Zhang Bao", UnitFaction.Enemy, UnitRole.Commander, PassiveSkillType.CommandAura, ActiveSkillType.PowerStrike, 32, 10, 4, 3, 1), new GridPosition(8, 4)),
                new UnitSpawnData(CreateDefinition("enemy-han-raider", "Han Raider", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.RapidMarch, ActiveSkillType.PowerStrike, 24, 9, 3, 4, 1), new GridPosition(7, 2)),
                new UnitSpawnData(CreateDefinition("enemy-armored-captain", "Armored Captain", UnitFaction.Enemy, UnitRole.Guardian, PassiveSkillType.ShieldWall, ActiveSkillType.None, 30, 8, 6, 2, 1), new GridPosition(7, 6)),
                new UnitSpawnData(CreateDefinition("enemy-yellow-turban-archer", "Yellow Turban Archer", UnitFaction.Enemy, UnitRole.Ranger, PassiveSkillType.LongShot, ActiveSkillType.Volley, 22, 9, 3, 3, 2), new GridPosition(8, 2)),
            };

            StageDefinitionData stage = new StageDefinitionData(
                "Jieqiao Fires",
                "stage.jieqiao_fires",
                10,
                10,
                openingSpawns,
                new List<GridPosition>
                {
                    new GridPosition(3, 1),
                    new GridPosition(3, 2),
                    new GridPosition(3, 7),
                    new GridPosition(3, 8),
                    new GridPosition(4, 3),
                    new GridPosition(4, 6),
                    new GridPosition(5, 3),
                    new GridPosition(5, 6),
                    new GridPosition(6, 1),
                    new GridPosition(6, 2),
                    new GridPosition(6, 7),
                    new GridPosition(6, 8),
                });

            ObjectiveState openingObjective = new ObjectiveState(
                "objective.jieqiao.opening",
                "Defeat Han Raider and the Armored Captain.",
                "objective.jieqiao.failure",
                "Liu Bei falls or all allies are defeated.");
            ObjectiveState finalObjective = new ObjectiveState(
                "objective.jieqiao.final",
                "Defeat Zhang Bao and Zhang Liang.",
                "objective.jieqiao.failure",
                "Liu Bei falls or all allies are defeated.");

            List<UnitSpawnData> reinforcementSpawns = new List<UnitSpawnData>
            {
                new UnitSpawnData(CreateDefinition("enemy-zhang-liang", "Zhang Liang", UnitFaction.Enemy, UnitRole.Commander, PassiveSkillType.CommandAura, ActiveSkillType.PowerStrike, 30, 9, 4, 3, 1), new GridPosition(9, 1)),
                new UnitSpawnData(CreateDefinition("enemy-raider-jia", "Raider Jia", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.RapidMarch, ActiveSkillType.None, 26, 9, 4, 4, 1), new GridPosition(9, 3)),
                new UnitSpawnData(CreateDefinition("enemy-raider-yi", "Raider Yi", UnitFaction.Enemy, UnitRole.Raider, PassiveSkillType.RapidMarch, ActiveSkillType.None, 26, 9, 4, 4, 1), new GridPosition(9, 6)),
            };

            List<ScenarioDirective> reinforcementDirectives = new List<ScenarioDirective>
            {
                ScenarioDirective.SetFlag(ReinforcementsArrivedFlag),
                ScenarioDirective.SpawnUnits(reinforcementSpawns),
                ScenarioDirective.UpdateObjective(finalObjective),
                ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                {
                    new ScenarioDialogueLine("unit.zhang_liang", "Zhang Liang", "dialogue.jieqiao.mid.1", "Liu Bei, you stepped right into our encirclement!"),
                    new ScenarioDialogueLine("unit.guan_yu", "Guan Yu", "dialogue.jieqiao.mid.2", "Their reinforcements have arrived. Then we cut down both commanders."),
                    new ScenarioDialogueLine("unit.liu_bei", "Liu Bei", "dialogue.jieqiao.mid.3", "Hold the line. Take Zhang Bao and Zhang Liang before the bridge burns."),
                }),
            };

            List<ScenarioTrigger> triggers = new List<ScenarioTrigger>
            {
                new ScenarioTrigger(
                    "jieqiao-intro",
                    ScenarioCheckpoint.BattleStart,
                    new List<ScenarioDirective>
                    {
                        ScenarioDirective.UpdateObjective(openingObjective),
                        ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                        {
                            new ScenarioDialogueLine("unit.liu_bei", "Liu Bei", "dialogue.jieqiao.opening.1", "If Jieqiao's supply route falls, the whole front collapses."),
                            new ScenarioDialogueLine("unit.huang_zhong", "Huang Zhong", "dialogue.jieqiao.opening.2", "Smoke rises from the east. Zhang Bao is already near the bridgehead."),
                            new ScenarioDialogueLine("unit.zhang_fei", "Zhang Fei", "dialogue.jieqiao.opening.3", "Then let them come. I'll break their charge and roar the rest away."),
                        }),
                    }),
                new ScenarioTrigger(
                    "jieqiao-reinforcements-kill",
                    ScenarioCheckpoint.ActionResolved,
                    reinforcementDirectives,
                    requiredDefeatedUnitIds: new List<string> { "enemy-han-raider", "enemy-armored-captain" },
                    exclusivityGroupId: "reinforcements"),
                new ScenarioTrigger(
                    "jieqiao-reinforcements-round",
                    ScenarioCheckpoint.EnemyTurnStart,
                    reinforcementDirectives,
                    minimumRoundNumber: 3,
                    exclusivityGroupId: "reinforcements"),
                new ScenarioTrigger(
                    "jieqiao-liu-bei-falls",
                    ScenarioCheckpoint.ActionResolved,
                    new List<ScenarioDirective> { ScenarioDirective.SetBattleOutcome(TurnSide.Enemy) },
                    requiredDefeatedUnitIds: new List<string> { "player-liu-bei" }),
                new ScenarioTrigger(
                    "jieqiao-bosses-fall",
                    ScenarioCheckpoint.ActionResolved,
                    new List<ScenarioDirective> { ScenarioDirective.SetBattleOutcome(TurnSide.Player) },
                    requiredDefeatedUnitIds: new List<string> { "enemy-zhang-bao", "enemy-zhang-liang" },
                    requiredFlags: new List<string> { ReinforcementsArrivedFlag }),
                new ScenarioTrigger(
                    "jieqiao-victory-dialogue",
                    ScenarioCheckpoint.PreBattleOutcome,
                    new List<ScenarioDirective>
                    {
                        ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                        {
                            new ScenarioDialogueLine("unit.liu_bei", "Liu Bei", "dialogue.jieqiao.victory.1", "The raiders break. Jieqiao stands for one more night."),
                            new ScenarioDialogueLine("unit.guan_yu", "Guan Yu", "dialogue.jieqiao.victory.2", "Zhang Liang has withdrawn, but this war will not end here."),
                        }),
                    },
                    requiresBattleEnded: true,
                    requiredWinningSide: TurnSide.Player),
                new ScenarioTrigger(
                    "jieqiao-defeat-dialogue",
                    ScenarioCheckpoint.PreBattleOutcome,
                    new List<ScenarioDirective>
                    {
                        ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                        {
                            new ScenarioDialogueLine("speaker.narrator", "Narrator", "dialogue.jieqiao.defeat.1", "Flames swallow the grain road, and the line at Jieqiao collapses."),
                        }),
                    },
                    requiresBattleEnded: true,
                    requiredWinningSide: TurnSide.Enemy),
            };

            return new BattleScenarioData(
                JieqiaoFiresScenarioId,
                "Jieqiao Fires",
                "scenario.jieqiao_fires",
                stage,
                triggers);
        }

        private static UnitDefinitionData CreateDefinition(
            string id,
            string displayName,
            UnitFaction faction,
            UnitRole role,
            PassiveSkillType passiveSkill,
            ActiveSkillType activeSkill,
            int maxHp,
            int attack,
            int defense,
            int moveRange,
            int attackRange)
        {
            return new UnitDefinitionData(
                id,
                displayName,
                "unit." + id.Replace('-', '_'),
                faction,
                role,
                "role." + role.ToString().ToLowerInvariant(),
                passiveSkill,
                "skill." + ToLocalizationKeySegment(passiveSkill.ToString()) + ".name",
                "skill." + ToLocalizationKeySegment(passiveSkill.ToString()) + ".desc",
                activeSkill,
                "skill." + ToLocalizationKeySegment(activeSkill.ToString()) + ".name",
                "skill." + ToLocalizationKeySegment(activeSkill.ToString()) + ".desc",
                maxHp,
                attack,
                defense,
                moveRange,
                attackRange);
        }

        private static string ToLocalizationKeySegment(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder(value.Length + 4);
            for (int index = 0; index < value.Length; index++)
            {
                char current = value[index];
                if (char.IsUpper(current) && index > 0)
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(current));
            }

            return builder.ToString();
        }
    }
}
