using System.Collections.Generic;
using System.Text;

namespace PhalanxChronicle.Core
{
    public static class BattleScenarioCatalog
    {
        public const string GuangzongScenarioId = "scenario.guangzong";
        public const string BowangpoScenarioId = "scenario.bowangpo";
        public const string ChangbanScenarioId = "scenario.changban_rearguard";
        public const string JiangxiaScenarioId = "scenario.jiangxia_ferry";
        public const string JiamengPassScenarioId = "scenario.jiameng_pass";
        public const string BaishuiScenarioId = "scenario.baishui_pass_raid";
        public const string MianzhuScenarioId = "scenario.mianzhu_breakthrough";
        public const string LuochengScenarioId = "scenario.luocheng_siege";
        public const string YangpingScenarioId = "scenario.yangping_pass";
        public const string TiandangScenarioId = "scenario.tiandang_raid";
        public const string HanshuiScenarioId = "scenario.hanshui";
        public const string DingjunScenarioId = "scenario.dingjun_mountain";

        public const string GuangzongReinforcementsArrivedFlag = "flag.guangzong.reinforcements_arrived";
        public const string GuangzongRapidSealSecuredFlag = "flag.guangzong.rapid_seal_secured";
        public const string GuangzongRapidSealFailedFlag = "flag.guangzong.rapid_seal_failed";
        public const string BowangpoFireTrapSprungFlag = "flag.bowangpo.fire_trap_sprung";
        public const string ChangbanFlankersArrivedFlag = "flag.changban.flankers_arrived";
        public const string JiangxiaBridgesCutFlag = "flag.jiangxia.bridges_cut";
        public const string JiamengBossArrivedFlag = "flag.jiameng.boss_arrived";
        public const string JiamengDuelWindowAdvancedFlag = "flag.jiameng.duel_window_advanced";
        public const string JiamengDuelExpiredFlag = "flag.jiameng.duel_expired";
        public const string JiamengDuelWonFlag = "flag.jiameng.duel_won";
        public const string BaishuiBridgeCutFlag = "flag.baishui.bridge_cut";
        public const string BaishuiBridgeCutByKillFlag = "flag.baishui.bridge_cut_by_kill";
        public const string BaishuiBridgeCutByTimerFlag = "flag.baishui.bridge_cut_by_timer";
        public const string MianzhuBreachFlag = "flag.mianzhu.breach";
        public const string LuochengGateBreachedFlag = "flag.luocheng.gate_breached";
        public const string YangpingRockslideFlag = "flag.yangping.rockslide";
        public const string TiandangAlarmFlag = "flag.tiandang.alarm";
        public const string TiandangSignalsSecuredFlag = "flag.tiandang.signals_secured";
        public const string HanshuiCounterattackFlag = "flag.hanshui.counterattack";
        public const string DingjunBossArrivedFlag = "flag.dingjun.boss_arrived";

        public static BattleScenarioData CreateScenario(string scenarioId)
        {
            switch (scenarioId)
            {
                case BowangpoScenarioId:
                    return CreateBowangpo();
                case ChangbanScenarioId:
                    return CreateChangbanRearguard();
                case JiangxiaScenarioId:
                    return CreateJiangxiaFerry();
                case JiamengPassScenarioId:
                    return CreateJiamengPass();
                case BaishuiScenarioId:
                    return CreateBaishuiPassRaid();
                case MianzhuScenarioId:
                    return CreateMianzhuBreakthrough();
                case LuochengScenarioId:
                    return CreateLuochengSiege();
                case YangpingScenarioId:
                    return CreateYangpingPass();
                case TiandangScenarioId:
                    return CreateTiandangRaid();
                case HanshuiScenarioId:
                    return CreateHanshui();
                case DingjunScenarioId:
                    return CreateDingjunMountain();
                case GuangzongScenarioId:
                default:
                    return CreateGuangzong();
            }
        }

        public static BattleScenarioData CreateGuangzong()
        {
            List<UnitSpawnData> openingSpawns = new List<UnitSpawnData>();
            AddCoreSquad(
                openingSpawns,
                new GridPosition(1, 6),
                new GridPosition(2, 7),
                new GridPosition(2, 5),
                new GridPosition(1, 8));
            openingSpawns.Add(SpawnEnemy("enemy-zhang-bao", "Zhang Bao", UnitRole.Commander, PassiveSkillType.CommandAura, ActiveSkillType.PowerStrike, 32, 10, 4, 3, 1, new GridPosition(13, 6), AiProfileType.Boss));
            openingSpawns.Add(SpawnEnemy("enemy-yellow_turban_raider", "Yellow Turban Raider", UnitRole.Raider, PassiveSkillType.RapidMarch, ActiveSkillType.PowerStrike, 24, 9, 3, 4, 1, new GridPosition(10, 3), AiProfileType.Aggressor));
            openingSpawns.Add(SpawnEnemy("enemy-armored_zealot", "Armored Zealot", UnitRole.Guardian, PassiveSkillType.ShieldWall, ActiveSkillType.None, 30, 8, 6, 2, 1, new GridPosition(11, 9), AiProfileType.Protector));
            openingSpawns.Add(SpawnEnemy("enemy-yellow_turban_archer", "Yellow Turban Archer", UnitRole.Ranger, PassiveSkillType.LongShot, ActiveSkillType.Volley, 22, 9, 3, 3, 2, new GridPosition(12, 4), AiProfileType.Support));

            List<GridPosition> blockedCells = new List<GridPosition>
            {
                new GridPosition(4, 1),
                new GridPosition(4, 2),
                new GridPosition(5, 1),
                new GridPosition(5, 2),
                new GridPosition(4, 11),
                new GridPosition(4, 12),
                new GridPosition(5, 11),
                new GridPosition(5, 12),
                new GridPosition(6, 4),
                new GridPosition(6, 5),
                new GridPosition(6, 8),
                new GridPosition(6, 9),
                new GridPosition(8, 5),
                new GridPosition(8, 8),
                new GridPosition(9, 2),
                new GridPosition(9, 11),
                new GridPosition(10, 4),
                new GridPosition(10, 9),
                new GridPosition(11, 5),
                new GridPosition(11, 8),
                new GridPosition(12, 2),
                new GridPosition(12, 3),
                new GridPosition(12, 10),
                new GridPosition(12, 11),
                new GridPosition(13, 5),
                new GridPosition(13, 8),
            };
            List<TerrainTileData> terrainTiles = new List<TerrainTileData>
            {
                new TerrainTileData(new GridPosition(3, 2), TerrainType.Forest),
                new TerrainTileData(new GridPosition(3, 3), TerrainType.Forest),
                new TerrainTileData(new GridPosition(4, 4), TerrainType.Forest),
                new TerrainTileData(new GridPosition(5, 5), TerrainType.Forest),
                new TerrainTileData(new GridPosition(3, 10), TerrainType.Forest),
                new TerrainTileData(new GridPosition(3, 11), TerrainType.Forest),
                new TerrainTileData(new GridPosition(4, 9), TerrainType.Forest),
                new TerrainTileData(new GridPosition(5, 8), TerrainType.Forest),
                new TerrainTileData(new GridPosition(7, 6), TerrainType.Fort),
                new TerrainTileData(new GridPosition(7, 7), TerrainType.Fort),
                new TerrainTileData(new GridPosition(12, 6), TerrainType.Fort),
                new TerrainTileData(new GridPosition(12, 7), TerrainType.Fort),
                new TerrainTileData(new GridPosition(8, 6), TerrainType.Hazard),
                new TerrainTileData(new GridPosition(8, 7), TerrainType.Hazard),
                new TerrainTileData(new GridPosition(9, 6), TerrainType.Hazard),
                new TerrainTileData(new GridPosition(9, 7), TerrainType.Hazard),
                new TerrainTileData(new GridPosition(10, 6), TerrainType.Hazard),
                new TerrainTileData(new GridPosition(10, 7), TerrainType.Hazard),
            };

            StageDefinitionData stage = new StageDefinitionData(
                "Battle of Guangzong",
                "stage.guangzong",
                16,
                14,
                openingSpawns,
                blockedCells,
                terrainTiles);

            ObjectiveState openingObjective = new ObjectiveState(
                "objective.guangzong.opening",
                "Defeat the Yellow Turban Raider and the Armored Zealot.",
                "objective.guangzong.failure",
                "Liu Bei falls or all allies are defeated.");
            ObjectiveState finalObjective = new ObjectiveState(
                "objective.guangzong.final",
                "Defeat Zhang Bao and Zhang Liang.",
                "objective.guangzong.failure",
                "Liu Bei falls or all allies are defeated.");

            List<ScenarioDirective> reinforcementDirectives = new List<ScenarioDirective>
            {
                ScenarioDirective.SetFlag(GuangzongReinforcementsArrivedFlag),
                ScenarioDirective.SpawnUnits(new List<UnitSpawnData>
                {
                    SpawnEnemy("enemy-zhang-liang", "Zhang Liang", UnitRole.Commander, PassiveSkillType.CommandAura, ActiveSkillType.PowerStrike, 30, 9, 4, 3, 1, new GridPosition(15, 2), AiProfileType.Boss),
                    SpawnEnemy("enemy-yellow_turban_hunter", "Yellow Turban Hunter", UnitRole.Raider, PassiveSkillType.RapidMarch, ActiveSkillType.None, 26, 9, 4, 4, 1, new GridPosition(14, 4), AiProfileType.Aggressor),
                    SpawnEnemy("enemy-fervent_spearman", "Fervent Spearman", UnitRole.Raider, PassiveSkillType.RapidMarch, ActiveSkillType.None, 26, 9, 4, 4, 1, new GridPosition(14, 10), AiProfileType.Aggressor),
                }),
                ScenarioDirective.UpdateObjective(finalObjective),
                ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                {
                    Line("unit.enemy_zhang_liang", "Zhang Liang", "dialogue.guangzong.mid.1", "Liu Bei, you broke the outer line only to walk into the heart of Guangzong."),
                    Line("unit.guan_yu", "Guan Yu", "dialogue.guangzong.mid.2", "Then we cut down both brothers here and end this breach cleanly."),
                    Line("unit.liu_bei", "Liu Bei", "dialogue.guangzong.mid.3", "Hold formation. Strike through before the rebels can seal the road again."),
                }),
            };

            List<ScenarioTrigger> triggers = new List<ScenarioTrigger>
            {
                new ScenarioTrigger(
                    "guangzong-intro",
                    ScenarioCheckpoint.BattleStart,
                    new List<ScenarioDirective>
                    {
                        ScenarioDirective.UpdateObjective(openingObjective),
                        ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                        {
                            Line("unit.liu_bei", "Liu Bei", "dialogue.guangzong.opening.1", "Guangzong burns on every side. If this breach closes, the people behind us are lost."),
                            Line("unit.huang_zhong", "Huang Zhong", "dialogue.guangzong.opening.2", "Zhang Bao holds the center while armored zealots lock the approach."),
                            Line("unit.zhang_fei", "Zhang Fei", "dialogue.guangzong.opening.3", "Then we smash the front and drag the rebel brothers out ourselves."),
                        }),
                    }),
                new ScenarioTrigger(
                    "guangzong-reinforcements-kill",
                    ScenarioCheckpoint.ActionResolved,
                    reinforcementDirectives,
                    requiredDefeatedUnitIds: new List<string> { "enemy-yellow_turban_raider", "enemy-armored_zealot" },
                    exclusivityGroupId: "guangzong-reinforcements"),
                new ScenarioTrigger(
                    "guangzong-bonus-window-cleared",
                    ScenarioCheckpoint.ActionResolved,
                    new List<ScenarioDirective> { ScenarioDirective.SetFlag(GuangzongRapidSealSecuredFlag) },
                    requiredDefeatedUnitIds: new List<string> { "enemy-yellow_turban_raider", "enemy-armored_zealot" },
                    excludedFlags: new List<string> { GuangzongRapidSealFailedFlag }),
                new ScenarioTrigger(
                    "guangzong-reinforcements-round",
                    ScenarioCheckpoint.EnemyTurnStart,
                    reinforcementDirectives,
                    minimumRoundNumber: 3,
                    exclusivityGroupId: "guangzong-reinforcements"),
                new ScenarioTrigger(
                    "guangzong-bonus-window-failed",
                    ScenarioCheckpoint.EnemyTurnStart,
                    new List<ScenarioDirective> { ScenarioDirective.SetFlag(GuangzongRapidSealFailedFlag) },
                    minimumRoundNumber: 3,
                    excludedFlags: new List<string> { GuangzongRapidSealSecuredFlag }),
                new ScenarioTrigger(
                    "guangzong-liu-bei-falls",
                    ScenarioCheckpoint.ActionResolved,
                    new List<ScenarioDirective> { ScenarioDirective.SetBattleOutcome(TurnSide.Enemy) },
                    requiredDefeatedUnitIds: new List<string> { "player-liu-bei" }),
                new ScenarioTrigger(
                    "guangzong-bosses-fall",
                    ScenarioCheckpoint.ActionResolved,
                    new List<ScenarioDirective> { ScenarioDirective.SetBattleOutcome(TurnSide.Player) },
                    requiredDefeatedUnitIds: new List<string> { "enemy-zhang-bao", "enemy-zhang-liang" },
                    requiredFlags: new List<string> { GuangzongReinforcementsArrivedFlag }),
                new ScenarioTrigger(
                    "guangzong-victory-dialogue",
                    ScenarioCheckpoint.PreBattleOutcome,
                    new List<ScenarioDirective>
                    {
                        ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                        {
                            Line("unit.liu_bei", "Liu Bei", "dialogue.guangzong.victory.1", "The rebel line is broken. Guangzong can breathe for one more night."),
                            Line("unit.guan_yu", "Guan Yu", "dialogue.guangzong.victory.2", "Word of this field will travel. Greater wars will follow it soon enough."),
                        }),
                    },
                    requiresBattleEnded: true,
                    requiredWinningSide: TurnSide.Player),
                new ScenarioTrigger(
                    "guangzong-defeat-dialogue",
                    ScenarioCheckpoint.PreBattleOutcome,
                    new List<ScenarioDirective>
                    {
                        ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                        {
                            Line("speaker.narrator", "Narrator", "dialogue.guangzong.defeat.1", "The breach collapses beneath smoke and banners, and Guangzong is swallowed by the rebellion."),
                        }),
                    },
                    requiresBattleEnded: true,
                    requiredWinningSide: TurnSide.Enemy),
            };

            return new BattleScenarioData(
                GuangzongScenarioId,
                "Battle of Guangzong",
                "scenario.guangzong",
                stage,
                triggers,
                1,
                60,
                24,
                new RewardBundle(120, 1, "yellow-turban-signet"),
                bonusRewards: CreateGuangzongBonusRewards());
        }

        public static BattleScenarioData CreateBowangpo()
        {
            List<UnitSpawnData> openingSpawns = new List<UnitSpawnData>();
            AddCoreSquad(
                openingSpawns,
                new GridPosition(1, 4),
                new GridPosition(2, 5),
                new GridPosition(2, 3),
                new GridPosition(1, 6));
            openingSpawns.Add(SpawnPlayerZhugeLiang(new GridPosition(1, 2)));
            openingSpawns.Add(SpawnEnemy("enemy-bowang-vanguard", "Wei Vanguard", UnitRole.Guardian, PassiveSkillType.ShieldWall, ActiveSkillType.None, 30, 9, 6, 2, 1, new GridPosition(8, 4), AiProfileType.Protector));
            openingSpawns.Add(SpawnEnemy("enemy-bowang-archer", "Wei Archer", UnitRole.Ranger, PassiveSkillType.LongShot, ActiveSkillType.Volley, 24, 9, 3, 3, 2, new GridPosition(9, 2), AiProfileType.Support));
            openingSpawns.Add(SpawnEnemy("enemy-bowang-rider", "Wei Rider", UnitRole.Raider, PassiveSkillType.RapidMarch, ActiveSkillType.PowerStrike, 26, 10, 3, 4, 1, new GridPosition(9, 6), AiProfileType.Aggressor));
            openingSpawns.Add(SpawnEnemy("enemy-bowang-shield", "Wei Shieldwall", UnitRole.Guardian, PassiveSkillType.ShieldWall, ActiveSkillType.None, 28, 8, 6, 2, 1, new GridPosition(10, 4), AiProfileType.Protector));

            StageDefinitionData stage = new StageDefinitionData(
                "Bowangpo",
                "stage.bowangpo",
                12,
                10,
                openingSpawns,
                new List<GridPosition>
                {
                    new GridPosition(4, 0),
                    new GridPosition(4, 1),
                    new GridPosition(4, 8),
                    new GridPosition(4, 9),
                    new GridPosition(5, 1),
                    new GridPosition(5, 8),
                    new GridPosition(6, 1),
                    new GridPosition(6, 8),
                    new GridPosition(7, 0),
                    new GridPosition(7, 1),
                    new GridPosition(7, 8),
                    new GridPosition(7, 9),
                },
                new List<TerrainTileData>
                {
                    new TerrainTileData(new GridPosition(3, 2), TerrainType.Forest),
                    new TerrainTileData(new GridPosition(3, 7), TerrainType.Forest),
                    new TerrainTileData(new GridPosition(5, 3), TerrainType.Forest),
                    new TerrainTileData(new GridPosition(5, 6), TerrainType.Forest),
                    new TerrainTileData(new GridPosition(8, 3), TerrainType.Forest),
                    new TerrainTileData(new GridPosition(8, 6), TerrainType.Forest),
                });

            ObjectiveState openingObjective = new ObjectiveState(
                "objective.bowangpo.opening",
                "Hold the pass until Zhuge Liang springs the fire trap.",
                "objective.bowangpo.failure",
                "Liu Bei falls or all allies are defeated.");
            ObjectiveState finalObjective = new ObjectiveState(
                "objective.bowangpo.final",
                "Defeat Xiahou Dun after the flames spread through the pass.",
                "objective.bowangpo.failure",
                "Liu Bei falls or all allies are defeated.");

            BattlefieldMutation fireTrapMutation = new BattlefieldMutation(
                new List<TerrainTileData>
                {
                    new TerrainTileData(new GridPosition(7, 2), TerrainType.Hazard),
                    new TerrainTileData(new GridPosition(7, 3), TerrainType.Hazard),
                    new TerrainTileData(new GridPosition(7, 4), TerrainType.Hazard),
                    new TerrainTileData(new GridPosition(7, 5), TerrainType.Hazard),
                    new TerrainTileData(new GridPosition(7, 6), TerrainType.Hazard),
                    new TerrainTileData(new GridPosition(7, 7), TerrainType.Hazard),
                    new TerrainTileData(new GridPosition(8, 2), TerrainType.Hazard),
                    new TerrainTileData(new GridPosition(8, 3), TerrainType.Hazard),
                    new TerrainTileData(new GridPosition(8, 4), TerrainType.Hazard),
                    new TerrainTileData(new GridPosition(8, 5), TerrainType.Hazard),
                    new TerrainTileData(new GridPosition(8, 6), TerrainType.Hazard),
                    new TerrainTileData(new GridPosition(8, 7), TerrainType.Hazard),
                },
                new List<BlockedCellStateChange>
                {
                    new BlockedCellStateChange(new GridPosition(10, 1), true),
                    new BlockedCellStateChange(new GridPosition(10, 8), true),
                    new BlockedCellStateChange(new GridPosition(11, 2), true),
                    new BlockedCellStateChange(new GridPosition(11, 7), true),
                });

            List<ScenarioDirective> fireTrapDirectives = new List<ScenarioDirective>
            {
                ScenarioDirective.SetFlag(BowangpoFireTrapSprungFlag),
                ScenarioDirective.ApplyBattlefieldMutation(fireTrapMutation),
                ScenarioDirective.SpawnUnits(new List<UnitSpawnData>
                {
                    SpawnEnemy("enemy-xiahou-dun", "Xiahou Dun", UnitRole.Commander, PassiveSkillType.CommandAura, ActiveSkillType.PowerStrike, 34, 11, 5, 3, 1, new GridPosition(11, 4), AiProfileType.Boss),
                    SpawnEnemy("enemy-bowang-escort", "Wei Escort", UnitRole.Guardian, PassiveSkillType.ShieldWall, ActiveSkillType.None, 28, 8, 5, 2, 1, new GridPosition(10, 5), AiProfileType.Protector),
                    SpawnEnemy("enemy-bowang-rearguard", "Wei Rearguard", UnitRole.Raider, PassiveSkillType.RapidMarch, ActiveSkillType.None, 24, 9, 3, 4, 1, new GridPosition(10, 3), AiProfileType.Aggressor),
                    SpawnEnemy("enemy-bowang-hunter", "Hidden Bow", UnitRole.Ranger, PassiveSkillType.LongShot, ActiveSkillType.None, 22, 8, 3, 3, 2, new GridPosition(9, 7), AiProfileType.Support),
                }),
                ScenarioDirective.UpdateObjective(finalObjective),
                ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                {
                    Line("unit.player_zhuge_liang", "Zhuge Liang", "dialogue.bowangpo.mid.1", "Now. Light the slope and close the rear. Let the pass itself strike for us."),
                    Line("unit.enemy_xiahou_dun", "Xiahou Dun", "dialogue.bowangpo.mid.2", "Fire? You dare trade steel for smoke in Bowangpo?"),
                    Line("unit.liu_bei", "Liu Bei", "dialogue.bowangpo.mid.3", "The trap is sprung. Break their command before they regroup out of the flames."),
                }),
            };

            List<ScenarioTrigger> triggers = new List<ScenarioTrigger>
            {
                new ScenarioTrigger(
                    "bowangpo-intro",
                    ScenarioCheckpoint.BattleStart,
                    new List<ScenarioDirective>
                    {
                        ScenarioDirective.UpdateObjective(openingObjective),
                        ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                        {
                            Line("unit.player_zhuge_liang", "Zhuge Liang", "dialogue.bowangpo.opening.1", "Bowangpo is narrow enough. If they commit one more step, the ground will fight for us."),
                            Line("unit.liu_bei", "Liu Bei", "dialogue.bowangpo.opening.2", "Then we hold steady. Do not break formation before the signal."),
                            Line("unit.guan_yu", "Guan Yu", "dialogue.bowangpo.opening.3", "Let the vanguard press in. We only need them to believe we are yielding."),
                        }),
                    }),
                new ScenarioTrigger(
                    "bowangpo-firetrap-kill",
                    ScenarioCheckpoint.ActionResolved,
                    fireTrapDirectives,
                    requiredDefeatedUnitIds: new List<string>
                    {
                        "enemy-bowang-vanguard",
                        "enemy-bowang-rider",
                        "enemy-bowang-archer",
                        "enemy-bowang-shield",
                    },
                    exclusivityGroupId: "bowangpo-firetrap"),
                new ScenarioTrigger(
                    "bowangpo-firetrap-round",
                    ScenarioCheckpoint.EnemyTurnStart,
                    fireTrapDirectives,
                    minimumRoundNumber: 3,
                    exclusivityGroupId: "bowangpo-firetrap"),
                new ScenarioTrigger(
                    "bowangpo-liu-bei-falls",
                    ScenarioCheckpoint.ActionResolved,
                    new List<ScenarioDirective> { ScenarioDirective.SetBattleOutcome(TurnSide.Enemy) },
                    requiredDefeatedUnitIds: new List<string> { "player-liu-bei" }),
                new ScenarioTrigger(
                    "bowangpo-boss-falls",
                    ScenarioCheckpoint.ActionResolved,
                    new List<ScenarioDirective> { ScenarioDirective.SetBattleOutcome(TurnSide.Player) },
                    requiredDefeatedUnitIds: new List<string> { "enemy-xiahou-dun" },
                    requiredFlags: new List<string> { BowangpoFireTrapSprungFlag }),
                new ScenarioTrigger(
                    "bowangpo-victory-dialogue",
                    ScenarioCheckpoint.PreBattleOutcome,
                    new List<ScenarioDirective>
                    {
                        ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                        {
                            Line("unit.liu_bei", "Liu Bei", "dialogue.bowangpo.victory.1", "Bowangpo holds. The pass burned bright enough to buy us a future."),
                            Line("unit.player_zhuge_liang", "Zhuge Liang", "dialogue.bowangpo.victory.2", "If my plans are of use, then let me continue at your side from this day onward."),
                        }),
                    },
                    requiresBattleEnded: true,
                    requiredWinningSide: TurnSide.Player),
                new ScenarioTrigger(
                    "bowangpo-defeat-dialogue",
                    ScenarioCheckpoint.PreBattleOutcome,
                    new List<ScenarioDirective>
                    {
                        ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                        {
                            Line("speaker.narrator", "Narrator", "dialogue.bowangpo.defeat.1", "The fire never takes hold, and Bowangpo becomes a killing ground for Liu Bei's retreating force."),
                        }),
                    },
                    requiresBattleEnded: true,
                    requiredWinningSide: TurnSide.Enemy),
            };

            return new BattleScenarioData(
                BowangpoScenarioId,
                "Bowangpo",
                "scenario.bowangpo",
                stage,
                triggers,
                2,
                72,
                28,
                new RewardBundle(150, 2, "bowang-fire-token", new[] { "player-zhuge-liang" }));
        }

        public static BattleScenarioData CreateChangbanRearguard()
        {
            List<UnitSpawnData> openingSpawns = new List<UnitSpawnData>();
            AddCoreSquad(
                openingSpawns,
                new GridPosition(1, 4),
                new GridPosition(2, 5),
                new GridPosition(2, 3),
                new GridPosition(1, 6));
            openingSpawns.Add(SpawnPlayerZhaoYun(new GridPosition(0, 4)));
            openingSpawns.Add(SpawnEnemy("enemy-pursuit_commander", "Pursuit Commander", UnitRole.Commander, PassiveSkillType.CommandAura, ActiveSkillType.PowerStrike, 32, 10, 4, 3, 1, new GridPosition(9, 4), AiProfileType.Boss));
            openingSpawns.Add(SpawnEnemy("enemy-tiger_guard", "Tiger Guard", UnitRole.Guardian, PassiveSkillType.ShieldWall, ActiveSkillType.None, 30, 9, 6, 2, 1, new GridPosition(10, 5), AiProfileType.Protector));
            openingSpawns.Add(SpawnEnemy("enemy-wei_bow_captain", "Wei Bow Captain", UnitRole.Ranger, PassiveSkillType.LongShot, ActiveSkillType.Volley, 24, 9, 3, 3, 2, new GridPosition(9, 6), AiProfileType.Support));
            openingSpawns.Add(SpawnEnemy("enemy-cavalry_scout", "Cavalry Scout", UnitRole.Raider, PassiveSkillType.RapidMarch, ActiveSkillType.PowerStrike, 26, 9, 3, 4, 1, new GridPosition(10, 3), AiProfileType.Aggressor));

            StageDefinitionData stage = new StageDefinitionData(
                "Changban Rearguard",
                "stage.changban_rearguard",
                12,
                10,
                openingSpawns,
                new List<GridPosition>
                {
                    new GridPosition(5, 0),
                    new GridPosition(5, 1),
                    new GridPosition(5, 2),
                    new GridPosition(5, 3),
                    new GridPosition(5, 6),
                    new GridPosition(5, 7),
                    new GridPosition(5, 8),
                    new GridPosition(5, 9),
                    new GridPosition(6, 0),
                    new GridPosition(6, 1),
                    new GridPosition(6, 2),
                    new GridPosition(6, 3),
                    new GridPosition(6, 6),
                    new GridPosition(6, 7),
                    new GridPosition(6, 8),
                    new GridPosition(6, 9),
                },
                new List<TerrainTileData>
                {
                    new TerrainTileData(new GridPosition(5, 4), TerrainType.Fort),
                    new TerrainTileData(new GridPosition(5, 5), TerrainType.Fort),
                    new TerrainTileData(new GridPosition(6, 4), TerrainType.Fort),
                    new TerrainTileData(new GridPosition(6, 5), TerrainType.Fort),
                    new TerrainTileData(new GridPosition(3, 4), TerrainType.Forest),
                    new TerrainTileData(new GridPosition(3, 5), TerrainType.Forest),
                    new TerrainTileData(new GridPosition(8, 4), TerrainType.Fort),
                    new TerrainTileData(new GridPosition(8, 5), TerrainType.Fort),
                });

            ObjectiveState openingObjective = new ObjectiveState(
                "objective.changban.opening",
                "Hold the crossing until the fifth round and keep Liu Bei alive.",
                "objective.changban.failure",
                "Liu Bei falls or all allies are defeated.");
            ObjectiveState pressureObjective = new ObjectiveState(
                "objective.changban.pressure",
                "Flank riders have arrived. Keep Liu Bei alive until the fifth round.",
                "objective.changban.failure",
                "Liu Bei falls or all allies are defeated.");

            List<ScenarioDirective> flankDirectives = new List<ScenarioDirective>
            {
                ScenarioDirective.SetFlag(ChangbanFlankersArrivedFlag),
                ScenarioDirective.SpawnUnits(new List<UnitSpawnData>
                {
                    SpawnEnemy("enemy-tiger_leopard_rider_a", "Tiger Leopard Rider", UnitRole.Raider, PassiveSkillType.RapidMarch, ActiveSkillType.PowerStrike, 24, 10, 3, 4, 1, new GridPosition(0, 1), AiProfileType.Aggressor),
                    SpawnEnemy("enemy-tiger_leopard_rider_b", "Tiger Leopard Rider", UnitRole.Raider, PassiveSkillType.RapidMarch, ActiveSkillType.PowerStrike, 24, 10, 3, 4, 1, new GridPosition(0, 8), AiProfileType.Aggressor),
                    SpawnEnemy("enemy-pursuit_bowman", "Pursuit Bowman", UnitRole.Ranger, PassiveSkillType.LongShot, ActiveSkillType.None, 22, 8, 3, 3, 2, new GridPosition(1, 9), AiProfileType.Support),
                }),
                ScenarioDirective.UpdateObjective(pressureObjective),
                ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                {
                    Line("unit.huang_zhong", "Huang Zhong", "dialogue.changban.mid.1", "Horsemen on the flank. They are trying to cut behind the crossing."),
                    Line("unit.zhang_fei", "Zhang Fei", "dialogue.changban.mid.2", "Let them come. I will hold this ford even if the river itself rises against us."),
                    Line("unit.liu_bei", "Liu Bei", "dialogue.changban.mid.3", "Just a little longer. Keep the road open until the column clears."),
                }),
            };

            List<ScenarioTrigger> triggers = new List<ScenarioTrigger>
            {
                new ScenarioTrigger(
                    "changban-intro",
                    ScenarioCheckpoint.BattleStart,
                    new List<ScenarioDirective>
                    {
                        ScenarioDirective.UpdateObjective(openingObjective),
                        ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                        {
                            Line("unit.liu_bei", "Liu Bei", "dialogue.changban.opening.1", "Cao Cao is on our heels. If this crossing falls too soon, the whole retreat is finished."),
                            Line("unit.guan_yu", "Guan Yu", "dialogue.changban.opening.2", "Then we hold the ford. Let the pursuers break against us until the people are away."),
                            Line("unit.zhang_fei", "Zhang Fei", "dialogue.changban.opening.3", "Good. I have no use for a quiet road anyway."),
                        }),
                    }),
                new ScenarioTrigger(
                    "changban-flankers",
                    ScenarioCheckpoint.EnemyTurnStart,
                    flankDirectives,
                    minimumRoundNumber: 3,
                    exclusivityGroupId: "changban-flankers"),
                new ScenarioTrigger(
                    "changban-hold-complete",
                    ScenarioCheckpoint.PlayerTurnStart,
                    new List<ScenarioDirective> { ScenarioDirective.SetBattleOutcome(TurnSide.Player) },
                    requiredAliveUnitIds: new List<string> { "player-liu-bei" },
                    minimumRoundNumber: 5),
                new ScenarioTrigger(
                    "changban-liu-bei-falls",
                    ScenarioCheckpoint.ActionResolved,
                    new List<ScenarioDirective> { ScenarioDirective.SetBattleOutcome(TurnSide.Enemy) },
                    requiredDefeatedUnitIds: new List<string> { "player-liu-bei" }),
                new ScenarioTrigger(
                    "changban-victory-dialogue",
                    ScenarioCheckpoint.PreBattleOutcome,
                    new List<ScenarioDirective>
                    {
                        ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                        {
                            Line("unit.liu_bei", "Liu Bei", "dialogue.changban.victory.1", "The road is clear. Pull back in order, every one of you."),
                            Line("unit.player_zhao_yun", "Zhao Yun", "dialogue.changban.victory.2", "If you will have me, I will ride with this banner from Changban onward."),
                        }),
                    },
                    requiresBattleEnded: true,
                    requiredWinningSide: TurnSide.Player),
                new ScenarioTrigger(
                    "changban-defeat-dialogue",
                    ScenarioCheckpoint.PreBattleOutcome,
                    new List<ScenarioDirective>
                    {
                        ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                        {
                            Line("speaker.narrator", "Narrator", "dialogue.changban.defeat.1", "The ford is overrun, and the retreat at Changban dissolves into chaos."),
                        }),
                    },
                    requiresBattleEnded: true,
                    requiredWinningSide: TurnSide.Enemy),
            };

            return new BattleScenarioData(
                ChangbanScenarioId,
                "Changban Rearguard",
                "scenario.changban_rearguard",
                stage,
                triggers,
                3,
                85,
                30,
                new RewardBundle(180, 2, "changban-scout-map", new[] { "player-zhao-yun" }),
                bonusRewards: CreateChangbanBonusRewards(),
                duelScenes: CreateChangbanDuelScenes());
        }

        public static BattleScenarioData CreateJiangxiaFerry()
        {
            List<UnitSpawnData> openingSpawns = new List<UnitSpawnData>();
            AddCoreSquad(
                openingSpawns,
                new GridPosition(1, 6),
                new GridPosition(2, 7),
                new GridPosition(2, 5),
                new GridPosition(1, 8));
            openingSpawns.Add(SpawnPlayerZhugeLiang(new GridPosition(0, 5)));
            openingSpawns.Add(SpawnPlayerZhaoYun(new GridPosition(1, 4)));
            openingSpawns.Add(SpawnEnemy("enemy-jiangxia-bridge-captain", "Bridge Captain", UnitRole.Guardian, PassiveSkillType.ShieldWall, ActiveSkillType.None, 30, 9, 6, 2, 1, new GridPosition(12, 6), AiProfileType.Protector));
            openingSpawns.Add(SpawnEnemy("enemy-jiangxia-bow-chief", "River Bow Chief", UnitRole.Ranger, PassiveSkillType.LongShot, ActiveSkillType.Volley, 24, 9, 3, 3, 2, new GridPosition(14, 3), AiProfileType.Support));
            openingSpawns.Add(SpawnEnemy("enemy-jiangxia-outer-warden-a", "Outer Bridge Warden", UnitRole.Guardian, PassiveSkillType.ShieldWall, ActiveSkillType.None, 26, 8, 5, 2, 1, new GridPosition(12, 2), AiProfileType.Protector));
            openingSpawns.Add(SpawnEnemy("enemy-jiangxia-outer-warden-b", "Outer Bridge Warden", UnitRole.Guardian, PassiveSkillType.ShieldWall, ActiveSkillType.None, 26, 8, 5, 2, 1, new GridPosition(12, 11), AiProfileType.Protector));

            List<GridPosition> blockedCells = new List<GridPosition>();
            for (int x = 7; x <= 10; x++)
            {
                for (int y = 0; y < 14; y++)
                {
                    if (y == 2 || y == 6 || y == 7 || y == 11)
                    {
                        continue;
                    }

                    blockedCells.Add(new GridPosition(x, y));
                }
            }

            List<TerrainTileData> terrainTiles = new List<TerrainTileData>();
            for (int x = 7; x <= 10; x++)
            {
                terrainTiles.Add(new TerrainTileData(new GridPosition(x, 2), TerrainType.Fort));
                terrainTiles.Add(new TerrainTileData(new GridPosition(x, 6), TerrainType.Fort));
                terrainTiles.Add(new TerrainTileData(new GridPosition(x, 7), TerrainType.Fort));
                terrainTiles.Add(new TerrainTileData(new GridPosition(x, 11), TerrainType.Fort));
            }

            terrainTiles.Add(new TerrainTileData(new GridPosition(5, 2), TerrainType.Forest));
            terrainTiles.Add(new TerrainTileData(new GridPosition(5, 3), TerrainType.Forest));
            terrainTiles.Add(new TerrainTileData(new GridPosition(5, 10), TerrainType.Forest));
            terrainTiles.Add(new TerrainTileData(new GridPosition(5, 11), TerrainType.Forest));
            terrainTiles.Add(new TerrainTileData(new GridPosition(12, 4), TerrainType.Hazard));
            terrainTiles.Add(new TerrainTileData(new GridPosition(12, 9), TerrainType.Hazard));
            terrainTiles.Add(new TerrainTileData(new GridPosition(13, 5), TerrainType.Hazard));
            terrainTiles.Add(new TerrainTileData(new GridPosition(13, 8), TerrainType.Hazard));

            StageDefinitionData stage = new StageDefinitionData(
                "Jiangxia Ferry",
                "stage.jiangxia_ferry",
                18,
                14,
                openingSpawns,
                blockedCells,
                terrainTiles);

            ObjectiveState openingObjective = new ObjectiveState(
                "objective.jiangxia.opening",
                "Hold the middle bridge until the outer crossings are cut.",
                "objective.jiangxia.failure",
                "Liu Bei falls or all allies are defeated.");
            ObjectiveState finalObjective = new ObjectiveState(
                "objective.jiangxia.final",
                "Defeat the Jiangxia ferry captain through the center bridge.",
                "objective.jiangxia.failure",
                "Liu Bei falls or all allies are defeated.");

            BattlefieldMutation bridgeCutMutation = new BattlefieldMutation(
                new List<TerrainTileData>
                {
                    new TerrainTileData(new GridPosition(7, 2), TerrainType.Hazard),
                    new TerrainTileData(new GridPosition(8, 2), TerrainType.Hazard),
                    new TerrainTileData(new GridPosition(9, 2), TerrainType.Hazard),
                    new TerrainTileData(new GridPosition(10, 2), TerrainType.Hazard),
                    new TerrainTileData(new GridPosition(7, 11), TerrainType.Hazard),
                    new TerrainTileData(new GridPosition(8, 11), TerrainType.Hazard),
                    new TerrainTileData(new GridPosition(9, 11), TerrainType.Hazard),
                    new TerrainTileData(new GridPosition(10, 11), TerrainType.Hazard),
                },
                new List<BlockedCellStateChange>
                {
                    new BlockedCellStateChange(new GridPosition(7, 2), true),
                    new BlockedCellStateChange(new GridPosition(8, 2), true),
                    new BlockedCellStateChange(new GridPosition(9, 2), true),
                    new BlockedCellStateChange(new GridPosition(10, 2), true),
                    new BlockedCellStateChange(new GridPosition(7, 11), true),
                    new BlockedCellStateChange(new GridPosition(8, 11), true),
                    new BlockedCellStateChange(new GridPosition(9, 11), true),
                    new BlockedCellStateChange(new GridPosition(10, 11), true),
                });

            List<ScenarioDirective> bridgeCutDirectives = new List<ScenarioDirective>
            {
                ScenarioDirective.SetFlag(JiangxiaBridgesCutFlag),
                ScenarioDirective.ApplyBattlefieldMutation(bridgeCutMutation),
                ScenarioDirective.SpawnUnits(new List<UnitSpawnData>
                {
                    SpawnEnemy("enemy-jiangxia-ferry-captain", "Jiangxia Ferry Captain", UnitRole.Commander, PassiveSkillType.CommandAura, ActiveSkillType.PowerStrike, 34, 11, 5, 3, 1, new GridPosition(16, 6), AiProfileType.Boss),
                    SpawnEnemy("enemy-jiangxia-river-rider", "River Rider", UnitRole.Raider, PassiveSkillType.RapidMarch, ActiveSkillType.None, 25, 9, 3, 4, 1, new GridPosition(15, 5), AiProfileType.Aggressor),
                    SpawnEnemy("enemy-jiangxia-river-rider-b", "River Rider", UnitRole.Raider, PassiveSkillType.RapidMarch, ActiveSkillType.None, 25, 9, 3, 4, 1, new GridPosition(15, 8), AiProfileType.Aggressor),
                }),
                ScenarioDirective.UpdateObjective(finalObjective),
                ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                {
                    Line("unit.player_zhuge_liang", "Zhuge Liang", "dialogue.jiangxia.mid.1", "Cut the outer bridges. Force the whole ferry line through the center and let their command stack there."),
                    Line("unit.player_zhao_yun", "Zhao Yun", "dialogue.jiangxia.mid.2", "The side crossings are gone. We strike straight through the middle before they reset."),
                    Line("unit.liu_bei", "Liu Bei", "dialogue.jiangxia.mid.3", "Hold the center bridge and break their captain cleanly."),
                }),
            };

            List<ScenarioTrigger> triggers = new List<ScenarioTrigger>
            {
                new ScenarioTrigger(
                    "jiangxia-intro",
                    ScenarioCheckpoint.BattleStart,
                    new List<ScenarioDirective>
                    {
                        ScenarioDirective.UpdateObjective(openingObjective),
                        ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                        {
                            Line("unit.liu_bei", "Liu Bei", "dialogue.jiangxia.opening.1", "Jiangxia's ferry line is too wide to force head-on. We keep the center while the outer bridges are cut."),
                            Line("unit.player_zhuge_liang", "Zhuge Liang", "dialogue.jiangxia.opening.2", "Once the flanks are severed, the defenders must answer on the middle bridge alone."),
                            Line("unit.player_zhao_yun", "Zhao Yun", "dialogue.jiangxia.opening.3", "Then I will hold the lane until that order lands."),
                        }),
                    }),
                new ScenarioTrigger(
                    "jiangxia-bridges-cut-kill",
                    ScenarioCheckpoint.ActionResolved,
                    bridgeCutDirectives,
                    requiredDefeatedUnitIds: new List<string> { "enemy-jiangxia-outer-warden-a", "enemy-jiangxia-outer-warden-b" },
                    exclusivityGroupId: "jiangxia-bridges"),
                new ScenarioTrigger(
                    "jiangxia-bridges-cut-round",
                    ScenarioCheckpoint.EnemyTurnStart,
                    bridgeCutDirectives,
                    minimumRoundNumber: 3,
                    exclusivityGroupId: "jiangxia-bridges"),
                new ScenarioTrigger(
                    "jiangxia-liu-bei-falls",
                    ScenarioCheckpoint.ActionResolved,
                    new List<ScenarioDirective> { ScenarioDirective.SetBattleOutcome(TurnSide.Enemy) },
                    requiredDefeatedUnitIds: new List<string> { "player-liu-bei" }),
                new ScenarioTrigger(
                    "jiangxia-boss-falls",
                    ScenarioCheckpoint.ActionResolved,
                    new List<ScenarioDirective> { ScenarioDirective.SetBattleOutcome(TurnSide.Player) },
                    requiredDefeatedUnitIds: new List<string> { "enemy-jiangxia-ferry-captain" },
                    requiredFlags: new List<string> { JiangxiaBridgesCutFlag }),
                new ScenarioTrigger(
                    "jiangxia-victory-dialogue",
                    ScenarioCheckpoint.PreBattleOutcome,
                    new List<ScenarioDirective>
                    {
                        ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                        {
                            Line("unit.liu_bei", "Liu Bei", "dialogue.jiangxia.victory.1", "The crossing is ours. Jiangxia's line broke the moment the river narrowed to one bridge."),
                            Line("unit.player_zhao_yun", "Zhao Yun", "dialogue.jiangxia.victory.2", "Then we move before the next bank can harden against us."),
                        }),
                    },
                    requiresBattleEnded: true,
                    requiredWinningSide: TurnSide.Player),
            };

            return new BattleScenarioData(
                JiangxiaScenarioId,
                "Jiangxia Ferry",
                "scenario.jiangxia_ferry",
                stage,
                triggers,
                4,
                92,
                32,
                new RewardBundle(205, 3, "jiangxia-river-reins"));
        }

        public static BattleScenarioData CreateJiamengPass()
        {
            List<UnitSpawnData> openingSpawns = new List<UnitSpawnData>();
            AddCoreSquad(
                openingSpawns,
                new GridPosition(4, 1),
                new GridPosition(3, 2),
                new GridPosition(5, 2),
                new GridPosition(2, 1));
            openingSpawns.Add(SpawnPlayerZhugeLiang(new GridPosition(6, 1)));
            openingSpawns.Add(SpawnPlayerZhaoYun(new GridPosition(4, 3)));
            openingSpawns.Add(SpawnPlayerMaChao(new GridPosition(7, 2)));
            openingSpawns.Add(SpawnEnemy("enemy-jiameng-gatewarden", "Gate Warden", UnitRole.Guardian, PassiveSkillType.ShieldWall, ActiveSkillType.None, 32, 10, 6, 2, 1, new GridPosition(4, 10), AiProfileType.Protector));
            openingSpawns.Add(SpawnEnemy("enemy-jiameng-bow-captain", "Bow Captain", UnitRole.Ranger, PassiveSkillType.LongShot, ActiveSkillType.Volley, 25, 9, 3, 3, 2, new GridPosition(5, 11), AiProfileType.Support));
            openingSpawns.Add(SpawnEnemy("enemy-jiameng-lancer", "Pass Lancer", UnitRole.Raider, PassiveSkillType.RapidMarch, ActiveSkillType.PowerStrike, 26, 10, 3, 4, 1, new GridPosition(4, 12), AiProfileType.Aggressor));
            openingSpawns.Add(SpawnEnemy("enemy-jiameng-sentry", "Stonewall Sentry", UnitRole.Guardian, PassiveSkillType.ShieldWall, ActiveSkillType.None, 28, 8, 5, 2, 1, new GridPosition(5, 9), AiProfileType.Protector));

            StageDefinitionData stage = new StageDefinitionData(
                "Jiameng Pass",
                "stage.jiameng_pass",
                10,
                14,
                openingSpawns,
                new List<GridPosition>
                {
                    new GridPosition(1, 4),
                    new GridPosition(1, 5),
                    new GridPosition(1, 6),
                    new GridPosition(1, 7),
                    new GridPosition(1, 8),
                    new GridPosition(2, 5),
                    new GridPosition(2, 8),
                    new GridPosition(3, 6),
                    new GridPosition(3, 7),
                    new GridPosition(6, 6),
                    new GridPosition(6, 7),
                    new GridPosition(7, 5),
                    new GridPosition(7, 8),
                    new GridPosition(8, 4),
                    new GridPosition(8, 5),
                    new GridPosition(8, 6),
                    new GridPosition(8, 7),
                    new GridPosition(8, 8),
                },
                new List<TerrainTileData>
                {
                    new TerrainTileData(new GridPosition(4, 5), TerrainType.Fort),
                    new TerrainTileData(new GridPosition(5, 5), TerrainType.Fort),
                    new TerrainTileData(new GridPosition(4, 8), TerrainType.Fort),
                    new TerrainTileData(new GridPosition(5, 8), TerrainType.Fort),
                    new TerrainTileData(new GridPosition(2, 10), TerrainType.Forest),
                    new TerrainTileData(new GridPosition(7, 10), TerrainType.Forest),
                });

            ObjectiveState openingObjective = new ObjectiveState(
                "objective.jiameng.opening",
                "Break the gate warden line and force Jiameng Pass open.",
                "objective.jiameng.failure",
                "Liu Bei falls or all allies are defeated.");
            ObjectiveState finalObjective = new ObjectiveState(
                "objective.jiameng.final",
                "Defeat the Jiameng commandant after the pass line breaks.",
                "objective.jiameng.failure",
                "Liu Bei falls or all allies are defeated.");

            List<ScenarioDirective> bossDirectives = new List<ScenarioDirective>
            {
                ScenarioDirective.SetFlag(JiamengBossArrivedFlag),
                ScenarioDirective.SpawnUnits(new List<UnitSpawnData>
                {
                    SpawnEnemy("enemy-jiameng-commandant", "Jiameng Commandant", UnitRole.Commander, PassiveSkillType.CommandAura, ActiveSkillType.PowerStrike, 34, 11, 5, 3, 1, new GridPosition(4, 13), AiProfileType.Boss),
                    SpawnEnemy("enemy-jiameng-guard-a", "Gate Guard", UnitRole.Guardian, PassiveSkillType.ShieldWall, ActiveSkillType.None, 28, 9, 5, 2, 1, new GridPosition(3, 12), AiProfileType.Protector),
                    SpawnEnemy("enemy-jiameng-guard-b", "Gate Guard", UnitRole.Guardian, PassiveSkillType.ShieldWall, ActiveSkillType.None, 28, 9, 5, 2, 1, new GridPosition(6, 12), AiProfileType.Protector),
                }),
                ScenarioDirective.UpdateObjective(finalObjective),
                ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                {
                    Line("unit.enemy_jiameng_commandant", "Jiameng Commandant", "dialogue.jiameng.mid.1", "You cracked the outer wall, but Jiameng still answers to me."),
                    Line("unit.player_zhao_yun", "Zhao Yun", "dialogue.jiameng.mid.2", "The pass is narrow enough. One last push and their center breaks completely."),
                    Line("unit.liu_bei", "Liu Bei", "dialogue.jiameng.mid.3", "Forward. End the stand-off here before they can seal the ridge again."),
                }),
            };

            List<ScenarioTrigger> triggers = new List<ScenarioTrigger>
            {
                new ScenarioTrigger(
                    "jiameng-intro",
                    ScenarioCheckpoint.BattleStart,
                    new List<ScenarioDirective>
                    {
                        ScenarioDirective.UpdateObjective(openingObjective),
                        ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                        {
                            Line("unit.player_zhuge_liang", "Zhuge Liang", "dialogue.jiameng.opening.1", "Jiameng Pass is long but thin. Break one joint in the line and the whole gate wavers."),
                            Line("unit.liu_bei", "Liu Bei", "dialogue.jiameng.opening.2", "Then we strike the center and keep the pressure forward."),
                            Line("unit.zhang_fei", "Zhang Fei", "dialogue.jiameng.opening.3", "Good. I am tired of staring at their walls from a distance."),
                        }),
                    }),
                new ScenarioTrigger(
                    "jiameng-boss-arrives-kill",
                    ScenarioCheckpoint.ActionResolved,
                    bossDirectives,
                    requiredDefeatedUnitIds: new List<string> { "enemy-jiameng-gatewarden", "enemy-jiameng-bow-captain" },
                    exclusivityGroupId: "jiameng-boss"),
                new ScenarioTrigger(
                    "jiameng-boss-arrives-round",
                    ScenarioCheckpoint.EnemyTurnStart,
                    bossDirectives,
                    minimumRoundNumber: 4,
                    exclusivityGroupId: "jiameng-boss"),
                new ScenarioTrigger(
                    "jiameng-duel-window-advance",
                    ScenarioCheckpoint.PlayerTurnStart,
                    new List<ScenarioDirective> { ScenarioDirective.SetFlag(JiamengDuelWindowAdvancedFlag) },
                    requiredFlags: new List<string> { JiamengBossArrivedFlag },
                    excludedFlags: new List<string> { JiamengDuelWindowAdvancedFlag, JiamengDuelExpiredFlag, JiamengDuelWonFlag }),
                new ScenarioTrigger(
                    "jiameng-duel-window-expire",
                    ScenarioCheckpoint.PlayerTurnStart,
                    new List<ScenarioDirective> { ScenarioDirective.SetFlag(JiamengDuelExpiredFlag) },
                    requiredFlags: new List<string> { JiamengBossArrivedFlag, JiamengDuelWindowAdvancedFlag },
                    excludedFlags: new List<string> { JiamengDuelExpiredFlag, JiamengDuelWonFlag }),
                new ScenarioTrigger(
                    "jiameng-liu-bei-falls",
                    ScenarioCheckpoint.ActionResolved,
                    new List<ScenarioDirective> { ScenarioDirective.SetBattleOutcome(TurnSide.Enemy) },
                    requiredDefeatedUnitIds: new List<string> { "player-liu-bei" }),
                new ScenarioTrigger(
                    "jiameng-boss-falls",
                    ScenarioCheckpoint.ActionResolved,
                    new List<ScenarioDirective> { ScenarioDirective.SetBattleOutcome(TurnSide.Player) },
                    requiredDefeatedUnitIds: new List<string> { "enemy-jiameng-commandant" },
                    requiredFlags: new List<string> { JiamengBossArrivedFlag }),
                new ScenarioTrigger(
                    "jiameng-victory-dialogue",
                    ScenarioCheckpoint.PreBattleOutcome,
                    new List<ScenarioDirective>
                    {
                        ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                        {
                            Line("unit.liu_bei", "Liu Bei", "dialogue.jiameng.victory.1", "Jiameng Pass is open. This field changes more than the road ahead."),
                            Line("unit.player_ma_chao", "Ma Chao", "dialogue.jiameng.victory.2", "A line like that deserves riders who can keep pace. From this battle on, I will ride under your banner."),
                        }),
                    },
                    requiresBattleEnded: true,
                    requiredWinningSide: TurnSide.Player),
                new ScenarioTrigger(
                    "jiameng-defeat-dialogue",
                    ScenarioCheckpoint.PreBattleOutcome,
                    new List<ScenarioDirective>
                    {
                        ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                        {
                            Line("speaker.narrator", "Narrator", "dialogue.jiameng.defeat.1", "The mountain gate holds fast, and Jiameng remains closed under its defenders."),
                        }),
                    },
                    requiresBattleEnded: true,
                    requiredWinningSide: TurnSide.Enemy),
            };

            return new BattleScenarioData(
                JiamengPassScenarioId,
                "Jiameng Pass",
                "scenario.jiameng_pass",
                stage,
                triggers,
                5,
                95,
                34,
                new RewardBundle(220, 3, "jiameng-oath-banner", new[] { "player-ma-chao" }),
                bonusRewards: CreateJiamengBonusRewards(),
                duelScenes: CreateJiamengDuelScenes());
        }

        public static BattleScenarioData CreateBaishuiPassRaid()
        {
            List<UnitSpawnData> openingSpawns = new List<UnitSpawnData>();
            AddCoreSquad(
                openingSpawns,
                new GridPosition(1, 4),
                new GridPosition(2, 5),
                new GridPosition(2, 3),
                new GridPosition(1, 6));
            openingSpawns.Add(SpawnPlayerZhugeLiang(new GridPosition(0, 4)));
            openingSpawns.Add(SpawnPlayerZhaoYun(new GridPosition(1, 2)));
            openingSpawns.Add(SpawnPlayerMaChao(new GridPosition(1, 7)));
            openingSpawns.Add(SpawnEnemy("enemy-baishui-bridge-captain", "Baishui Bridge Captain", UnitRole.Guardian, PassiveSkillType.ShieldWall, ActiveSkillType.None, 33, 10, 6, 2, 1, new GridPosition(7, 6), AiProfileType.Protector));
            openingSpawns.Add(SpawnEnemy("enemy-baishui-bow-chief", "Baishui Bow Chief", UnitRole.Ranger, PassiveSkillType.LongShot, ActiveSkillType.Volley, 26, 10, 3, 3, 2, new GridPosition(7, 2), AiProfileType.Support));
            openingSpawns.Add(SpawnEnemy("enemy-baishui-pass-guard", "Pass Guard", UnitRole.Guardian, PassiveSkillType.ShieldWall, ActiveSkillType.None, 29, 9, 5, 2, 1, new GridPosition(8, 3), AiProfileType.Protector));
            openingSpawns.Add(SpawnEnemy("enemy-baishui-lancer", "Baishui Lancer", UnitRole.Raider, PassiveSkillType.RapidMarch, ActiveSkillType.PowerStrike, 27, 10, 3, 4, 1, new GridPosition(8, 7), AiProfileType.Aggressor));

            List<GridPosition> blockedCells = new List<GridPosition>();
            for (int y = 0; y < 10; y++)
            {
                if (y == 2 || y == 6)
                {
                    continue;
                }

                blockedCells.Add(new GridPosition(5, y));
                blockedCells.Add(new GridPosition(6, y));
            }

            blockedCells.Add(new GridPosition(9, 1));
            blockedCells.Add(new GridPosition(9, 8));

            List<TerrainTileData> terrainTiles = new List<TerrainTileData>
            {
                new TerrainTileData(new GridPosition(5, 2), TerrainType.Fort),
                new TerrainTileData(new GridPosition(6, 2), TerrainType.Fort),
                new TerrainTileData(new GridPosition(5, 6), TerrainType.Fort),
                new TerrainTileData(new GridPosition(6, 6), TerrainType.Fort),
                new TerrainTileData(new GridPosition(3, 1), TerrainType.Forest),
                new TerrainTileData(new GridPosition(3, 2), TerrainType.Forest),
                new TerrainTileData(new GridPosition(3, 7), TerrainType.Forest),
                new TerrainTileData(new GridPosition(3, 8), TerrainType.Forest),
                new TerrainTileData(new GridPosition(9, 4), TerrainType.Hazard),
                new TerrainTileData(new GridPosition(10, 4), TerrainType.Hazard),
            };

            StageDefinitionData stage = new StageDefinitionData(
                "Baishui Pass Raid",
                "stage.baishui_pass_raid",
                12,
                10,
                openingSpawns,
                blockedCells,
                terrainTiles);

            ObjectiveState openingObjective = new ObjectiveState(
                "objective.baishui.opening",
                "Break the bridge captain and bow chief before Baishui closes its lower crossing.",
                "objective.baishui.failure",
                "Liu Bei falls or all allies are defeated.");
            ObjectiveState finalObjective = new ObjectiveState(
                "objective.baishui.final",
                "Defeat the Baishui commandant after the lower bridge is cut.",
                "objective.baishui.failure",
                "Liu Bei falls or all allies are defeated.");

            BattlefieldMutation bridgeCutMutation = new BattlefieldMutation(
                new List<TerrainTileData>
                {
                    new TerrainTileData(new GridPosition(5, 6), TerrainType.Hazard),
                    new TerrainTileData(new GridPosition(6, 6), TerrainType.Hazard),
                },
                new List<BlockedCellStateChange>
                {
                    new BlockedCellStateChange(new GridPosition(5, 6), true),
                    new BlockedCellStateChange(new GridPosition(6, 6), true),
                });

            List<ScenarioDirective> bridgeCutDirectives = new List<ScenarioDirective>
            {
                ScenarioDirective.SetFlag(BaishuiBridgeCutFlag),
                ScenarioDirective.ApplyBattlefieldMutation(bridgeCutMutation),
                ScenarioDirective.SpawnUnits(new List<UnitSpawnData>
                {
                    SpawnEnemy("enemy-baishui-commandant", "Baishui Commandant", UnitRole.Commander, PassiveSkillType.CommandAura, ActiveSkillType.PowerStrike, 35, 11, 5, 3, 1, new GridPosition(10, 4), AiProfileType.Boss),
                    SpawnEnemy("enemy-baishui-rider-a", "Bridge Pursuer", UnitRole.Raider, PassiveSkillType.RapidMarch, ActiveSkillType.None, 26, 10, 3, 4, 1, new GridPosition(0, 8), AiProfileType.Aggressor),
                    SpawnEnemy("enemy-baishui-rider-b", "Bridge Pursuer", UnitRole.Raider, PassiveSkillType.RapidMarch, ActiveSkillType.None, 26, 10, 3, 4, 1, new GridPosition(1, 9), AiProfileType.Aggressor),
                }),
                ScenarioDirective.UpdateObjective(finalObjective),
                ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                {
                    Line("unit.enemy_baishui_commandant", "Baishui Commandant", "dialogue.baishui.mid.1", "Cut the lower bridge and squeeze them against the upper pass. No one slips through Baishui today."),
                    Line("unit.player_zhao_yun", "Zhao Yun", "dialogue.baishui.mid.2", "The lower crossing is gone. We press the upper lane before their riders close our rear."),
                    Line("unit.liu_bei", "Liu Bei", "dialogue.baishui.mid.3", "Forward through the upper pass. Break the command post before the trap fully tightens."),
                }),
            };
            List<ScenarioDirective> bridgeCutKillDirectives = new List<ScenarioDirective> { ScenarioDirective.SetFlag(BaishuiBridgeCutByKillFlag) };
            bridgeCutKillDirectives.AddRange(bridgeCutDirectives);
            List<ScenarioDirective> bridgeCutRoundDirectives = new List<ScenarioDirective> { ScenarioDirective.SetFlag(BaishuiBridgeCutByTimerFlag) };
            bridgeCutRoundDirectives.AddRange(bridgeCutDirectives);

            List<ScenarioTrigger> triggers = new List<ScenarioTrigger>
            {
                new ScenarioTrigger(
                    "baishui-intro",
                    ScenarioCheckpoint.BattleStart,
                    new List<ScenarioDirective>
                    {
                        ScenarioDirective.UpdateObjective(openingObjective),
                        ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                        {
                            Line("unit.player_zhuge_liang", "Zhuge Liang", "dialogue.baishui.opening.1", "Baishui gives us two crossings, but only one will remain when their signal drops."),
                            Line("unit.player_zhao_yun", "Zhao Yun", "dialogue.baishui.opening.2", "Then I break the lower line fast and keep the column moving."),
                            Line("unit.liu_bei", "Liu Bei", "dialogue.baishui.opening.3", "Take the outer captain and bow chief first. Do not let the pass close around us."),
                        }),
                    }),
                new ScenarioTrigger(
                    "baishui-bridge-cut-kill",
                    ScenarioCheckpoint.ActionResolved,
                    bridgeCutKillDirectives,
                    requiredDefeatedUnitIds: new List<string> { "enemy-baishui-bridge-captain", "enemy-baishui-bow-chief" },
                    exclusivityGroupId: "baishui-bridge"),
                new ScenarioTrigger(
                    "baishui-bridge-cut-round",
                    ScenarioCheckpoint.EnemyTurnStart,
                    bridgeCutRoundDirectives,
                    minimumRoundNumber: 4,
                    exclusivityGroupId: "baishui-bridge"),
                new ScenarioTrigger(
                    "baishui-liu-bei-falls",
                    ScenarioCheckpoint.ActionResolved,
                    new List<ScenarioDirective> { ScenarioDirective.SetBattleOutcome(TurnSide.Enemy) },
                    requiredDefeatedUnitIds: new List<string> { "player-liu-bei" }),
                new ScenarioTrigger(
                    "baishui-boss-falls",
                    ScenarioCheckpoint.ActionResolved,
                    new List<ScenarioDirective> { ScenarioDirective.SetBattleOutcome(TurnSide.Player) },
                    requiredDefeatedUnitIds: new List<string> { "enemy-baishui-commandant" },
                    requiredFlags: new List<string> { BaishuiBridgeCutFlag }),
                new ScenarioTrigger(
                    "baishui-victory-dialogue",
                    ScenarioCheckpoint.PreBattleOutcome,
                    new List<ScenarioDirective>
                    {
                        ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                        {
                            Line("unit.liu_bei", "Liu Bei", "dialogue.baishui.victory.1", "Baishui's choke point is ours. Their bridge trap broke the moment we outran the collapse."),
                            Line("unit.player_zhao_yun", "Zhao Yun", "dialogue.baishui.victory.2", "Then we keep pressing. A pass taken at speed should never be handed back."),
                        }),
                    },
                    requiresBattleEnded: true,
                    requiredWinningSide: TurnSide.Player),
                new ScenarioTrigger(
                    "baishui-defeat-dialogue",
                    ScenarioCheckpoint.PreBattleOutcome,
                    new List<ScenarioDirective>
                    {
                        ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                        {
                            Line("speaker.narrator", "Narrator", "dialogue.baishui.defeat.1", "The bridge falls, the pass seals, and Baishui closes like a fist around Liu Bei's vanguard."),
                        }),
                    },
                    requiresBattleEnded: true,
                    requiredWinningSide: TurnSide.Enemy),
            };

            return new BattleScenarioData(
                BaishuiScenarioId,
                "Baishui Pass Raid",
                "scenario.baishui_pass_raid",
                stage,
                triggers,
                6,
                101,
                35,
                new RewardBundle(228, 3, "baishui-signal-spear"),
                bonusRewards: CreateBaishuiBonusRewards(),
                duelScenes: CreateBaishuiDuelScenes());
        }

        public static BattleScenarioData CreateMianzhuBreakthrough()
        {
            List<UnitSpawnData> openingSpawns = new List<UnitSpawnData>();
            AddCoreSquad(
                openingSpawns,
                new GridPosition(6, 1),
                new GridPosition(7, 1),
                new GridPosition(5, 1),
                new GridPosition(8, 1));
            openingSpawns.Add(SpawnPlayerZhugeLiang(new GridPosition(6, 0)));
            openingSpawns.Add(SpawnPlayerZhaoYun(new GridPosition(4, 1)));
            openingSpawns.Add(SpawnPlayerMaChao(new GridPosition(9, 1)));
            openingSpawns.Add(SpawnEnemy("enemy-mianzhu-gate-captain", "Mianzhu Gate Captain", UnitRole.Guardian, PassiveSkillType.ShieldWall, ActiveSkillType.None, 33, 10, 6, 2, 1, new GridPosition(6, 4), AiProfileType.Protector));
            openingSpawns.Add(SpawnEnemy("enemy-mianzhu-oil-bow-chief", "Oil Bow Chief", UnitRole.Ranger, PassiveSkillType.LongShot, ActiveSkillType.Volley, 26, 10, 3, 3, 2, new GridPosition(8, 4), AiProfileType.Support));
            openingSpawns.Add(SpawnEnemy("enemy-mianzhu-front-guard-a", "Front Guard", UnitRole.Guardian, PassiveSkillType.ShieldWall, ActiveSkillType.None, 29, 9, 5, 2, 1, new GridPosition(5, 4), AiProfileType.Protector));
            openingSpawns.Add(SpawnEnemy("enemy-mianzhu-front-guard-b", "Front Guard", UnitRole.Guardian, PassiveSkillType.ShieldWall, ActiveSkillType.None, 29, 9, 5, 2, 1, new GridPosition(7, 4), AiProfileType.Protector));
            openingSpawns.Add(SpawnEnemy("enemy-mianzhu-outer-rider", "Outer Rider", UnitRole.Raider, PassiveSkillType.RapidMarch, ActiveSkillType.None, 27, 10, 3, 4, 1, new GridPosition(9, 3), AiProfileType.Aggressor));

            List<GridPosition> blockedCells = new List<GridPosition>();
            for (int x = 3; x <= 10; x++)
            {
                blockedCells.Add(new GridPosition(x, 5));
            }

            blockedCells.Add(new GridPosition(2, 6));
            blockedCells.Add(new GridPosition(2, 7));
            blockedCells.Add(new GridPosition(11, 6));
            blockedCells.Add(new GridPosition(11, 7));

            List<TerrainTileData> terrainTiles = new List<TerrainTileData>
            {
                new TerrainTileData(new GridPosition(4, 5), TerrainType.Fort),
                new TerrainTileData(new GridPosition(5, 5), TerrainType.Fort),
                new TerrainTileData(new GridPosition(6, 5), TerrainType.Fort),
                new TerrainTileData(new GridPosition(7, 5), TerrainType.Fort),
                new TerrainTileData(new GridPosition(8, 5), TerrainType.Fort),
                new TerrainTileData(new GridPosition(9, 5), TerrainType.Fort),
                new TerrainTileData(new GridPosition(5, 3), TerrainType.Forest),
                new TerrainTileData(new GridPosition(6, 3), TerrainType.Hazard),
                new TerrainTileData(new GridPosition(7, 3), TerrainType.Hazard),
                new TerrainTileData(new GridPosition(10, 3), TerrainType.Forest),
                new TerrainTileData(new GridPosition(4, 7), TerrainType.Forest),
                new TerrainTileData(new GridPosition(9, 7), TerrainType.Forest),
            };

            StageDefinitionData stage = new StageDefinitionData(
                "Mianzhu Breakthrough",
                "stage.mianzhu_breakthrough",
                14,
                10,
                openingSpawns,
                blockedCells,
                terrainTiles);

            ObjectiveState openingObjective = new ObjectiveState(
                "objective.mianzhu.opening",
                "Break the front line and breach the Mianzhu gate.",
                "objective.mianzhu.failure",
                "Liu Bei falls or all allies are defeated.");
            ObjectiveState finalObjective = new ObjectiveState(
                "objective.mianzhu.final",
                "Push through the breach and defeat the Mianzhu commandant.",
                "objective.mianzhu.failure",
                "Liu Bei falls or all allies are defeated.");

            BattlefieldMutation breachMutation = new BattlefieldMutation(
                new List<TerrainTileData>
                {
                    new TerrainTileData(new GridPosition(6, 5), TerrainType.Plain),
                    new TerrainTileData(new GridPosition(7, 5), TerrainType.Plain),
                    new TerrainTileData(new GridPosition(6, 6), TerrainType.Hazard),
                    new TerrainTileData(new GridPosition(7, 6), TerrainType.Hazard),
                },
                new List<BlockedCellStateChange>
                {
                    new BlockedCellStateChange(new GridPosition(6, 5), false),
                    new BlockedCellStateChange(new GridPosition(7, 5), false),
                });

            List<ScenarioDirective> breachDirectives = new List<ScenarioDirective>
            {
                ScenarioDirective.SetFlag(MianzhuBreachFlag),
                ScenarioDirective.ApplyBattlefieldMutation(breachMutation),
                ScenarioDirective.SpawnUnits(new List<UnitSpawnData>
                {
                    SpawnEnemy("enemy-mianzhu-commandant", "Mianzhu Commandant", UnitRole.Commander, PassiveSkillType.CommandAura, ActiveSkillType.PowerStrike, 36, 11, 5, 3, 1, new GridPosition(6, 8), AiProfileType.Boss),
                    SpawnEnemy("enemy-mianzhu-inner-guard-a", "Inner Guard", UnitRole.Guardian, PassiveSkillType.ShieldWall, ActiveSkillType.None, 30, 9, 5, 2, 1, new GridPosition(5, 7), AiProfileType.Protector),
                    SpawnEnemy("enemy-mianzhu-inner-guard-b", "Inner Guard", UnitRole.Guardian, PassiveSkillType.ShieldWall, ActiveSkillType.None, 30, 9, 5, 2, 1, new GridPosition(8, 7), AiProfileType.Protector),
                }),
                ScenarioDirective.UpdateObjective(finalObjective),
                ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                {
                    Line("unit.player_zhuge_liang", "Zhuge Liang", "dialogue.mianzhu.mid.1", "The gate is open, but they lit the center lane. Push through before the fire line hardens."),
                    Line("unit.zhang_fei", "Zhang Fei", "dialogue.mianzhu.mid.2", "Good. Burning planks are still softer than a closed gate."),
                    Line("unit.liu_bei", "Liu Bei", "dialogue.mianzhu.mid.3", "Take the breach and end their command inside the wall."),
                }),
            };

            List<ScenarioTrigger> triggers = new List<ScenarioTrigger>
            {
                new ScenarioTrigger(
                    "mianzhu-intro",
                    ScenarioCheckpoint.BattleStart,
                    new List<ScenarioDirective>
                    {
                        ScenarioDirective.UpdateObjective(openingObjective),
                        ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                        {
                            Line("unit.liu_bei", "Liu Bei", "dialogue.mianzhu.opening.1", "Mianzhu's front is layered. Break the first line and the gate becomes the only thing left between us and the city road."),
                            Line("unit.player_zhuge_liang", "Zhuge Liang", "dialogue.mianzhu.opening.2", "Watch the oil jars at center. Once the gate cracks, they will try to turn the breach into a fire trap."),
                            Line("unit.player_ma_chao", "Ma Chao", "dialogue.mianzhu.opening.3", "Then we strike through before the flames can own the lane."),
                        }),
                    }),
                new ScenarioTrigger(
                    "mianzhu-breach-kill",
                    ScenarioCheckpoint.ActionResolved,
                    breachDirectives,
                    requiredDefeatedUnitIds: new List<string> { "enemy-mianzhu-gate-captain", "enemy-mianzhu-oil-bow-chief" },
                    exclusivityGroupId: "mianzhu-breach"),
                new ScenarioTrigger(
                    "mianzhu-breach-round",
                    ScenarioCheckpoint.EnemyTurnStart,
                    breachDirectives,
                    minimumRoundNumber: 4,
                    exclusivityGroupId: "mianzhu-breach"),
                new ScenarioTrigger(
                    "mianzhu-liu-bei-falls",
                    ScenarioCheckpoint.ActionResolved,
                    new List<ScenarioDirective> { ScenarioDirective.SetBattleOutcome(TurnSide.Enemy) },
                    requiredDefeatedUnitIds: new List<string> { "player-liu-bei" }),
                new ScenarioTrigger(
                    "mianzhu-boss-falls",
                    ScenarioCheckpoint.ActionResolved,
                    new List<ScenarioDirective> { ScenarioDirective.SetBattleOutcome(TurnSide.Player) },
                    requiredDefeatedUnitIds: new List<string> { "enemy-mianzhu-commandant" },
                    requiredFlags: new List<string> { MianzhuBreachFlag }),
                new ScenarioTrigger(
                    "mianzhu-victory-dialogue",
                    ScenarioCheckpoint.PreBattleOutcome,
                    new List<ScenarioDirective>
                    {
                        ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                        {
                            Line("unit.liu_bei", "Liu Bei", "dialogue.mianzhu.victory.1", "Mianzhu's breach is ours. The gate never recovered once the first line broke."),
                            Line("unit.player_zhuge_liang", "Zhuge Liang", "dialogue.mianzhu.victory.2", "Then we press inland before the next wall can learn from this one."),
                        }),
                    },
                    requiresBattleEnded: true,
                    requiredWinningSide: TurnSide.Player),
                new ScenarioTrigger(
                    "mianzhu-defeat-dialogue",
                    ScenarioCheckpoint.PreBattleOutcome,
                    new List<ScenarioDirective>
                    {
                        ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                        {
                            Line("speaker.narrator", "Narrator", "dialogue.mianzhu.defeat.1", "Mianzhu's fire line holds, and the breach closes under smoke and shouting."),
                        }),
                    },
                    requiresBattleEnded: true,
                    requiredWinningSide: TurnSide.Enemy),
            };

            return new BattleScenarioData(
                MianzhuScenarioId,
                "Mianzhu Breakthrough",
                "scenario.mianzhu_breakthrough",
                stage,
                triggers,
                7,
                110,
                38,
                new RewardBundle(240, 4, "mianzhu-feather-sigil"));
        }

        public static BattleScenarioData CreateLuochengSiege()
        {
            List<UnitSpawnData> openingSpawns = new List<UnitSpawnData>();
            AddCoreSquad(
                openingSpawns,
                new GridPosition(7, 2),
                new GridPosition(8, 3),
                new GridPosition(6, 3),
                new GridPosition(7, 4));
            openingSpawns.Add(SpawnPlayerZhugeLiang(new GridPosition(9, 2)));
            openingSpawns.Add(SpawnPlayerZhaoYun(new GridPosition(6, 1)));
            openingSpawns.Add(SpawnPlayerMaChao(new GridPosition(10, 2)));
            openingSpawns.Add(SpawnEnemy("enemy-luocheng-gate-captain", "Gate Captain", UnitRole.Guardian, PassiveSkillType.ShieldWall, ActiveSkillType.None, 32, 10, 6, 2, 1, new GridPosition(8, 7), AiProfileType.Protector));
            openingSpawns.Add(SpawnEnemy("enemy-luocheng-wall-bow", "Wall Bow Captain", UnitRole.Ranger, PassiveSkillType.LongShot, ActiveSkillType.Volley, 25, 9, 3, 3, 2, new GridPosition(10, 9), AiProfileType.Support));
            openingSpawns.Add(SpawnEnemy("enemy-luocheng-outer-guard", "Outer Guard", UnitRole.Guardian, PassiveSkillType.ShieldWall, ActiveSkillType.None, 28, 9, 5, 2, 1, new GridPosition(6, 7), AiProfileType.Protector));
            openingSpawns.Add(SpawnEnemy("enemy-wei_shieldman", "Wei Shieldman", UnitRole.Guardian, PassiveSkillType.ShieldWall, ActiveSkillType.None, 28, 8, 6, 2, 1, new GridPosition(11, 7), AiProfileType.Protector));
            openingSpawns.Add(SpawnEnemy("enemy-wei_deadeye", "Wei Deadeye", UnitRole.Ranger, PassiveSkillType.LongShot, ActiveSkillType.Volley, 24, 9, 3, 3, 2, new GridPosition(14, 10), AiProfileType.Support));

            List<GridPosition> blockedCells = new List<GridPosition>();
            for (int x = 3; x <= 14; x++)
            {
                blockedCells.Add(new GridPosition(x, 8));
            }

            blockedCells.Add(new GridPosition(3, 9));
            blockedCells.Add(new GridPosition(4, 9));
            blockedCells.Add(new GridPosition(13, 9));
            blockedCells.Add(new GridPosition(14, 9));
            blockedCells.Add(new GridPosition(4, 10));
            blockedCells.Add(new GridPosition(4, 11));
            blockedCells.Add(new GridPosition(13, 10));
            blockedCells.Add(new GridPosition(13, 11));
            blockedCells.Add(new GridPosition(5, 12));
            blockedCells.Add(new GridPosition(6, 12));
            blockedCells.Add(new GridPosition(11, 12));
            blockedCells.Add(new GridPosition(12, 12));

            List<TerrainTileData> terrainTiles = new List<TerrainTileData>
            {
                new TerrainTileData(new GridPosition(4, 8), TerrainType.Fort),
                new TerrainTileData(new GridPosition(7, 8), TerrainType.Fort),
                new TerrainTileData(new GridPosition(8, 8), TerrainType.Fort),
                new TerrainTileData(new GridPosition(9, 8), TerrainType.Fort),
                new TerrainTileData(new GridPosition(10, 8), TerrainType.Fort),
                new TerrainTileData(new GridPosition(13, 8), TerrainType.Fort),
                new TerrainTileData(new GridPosition(7, 6), TerrainType.Hazard),
                new TerrainTileData(new GridPosition(8, 6), TerrainType.Hazard),
                new TerrainTileData(new GridPosition(9, 6), TerrainType.Hazard),
                new TerrainTileData(new GridPosition(10, 6), TerrainType.Hazard),
                new TerrainTileData(new GridPosition(8, 10), TerrainType.Hazard),
                new TerrainTileData(new GridPosition(9, 10), TerrainType.Hazard),
                new TerrainTileData(new GridPosition(8, 11), TerrainType.Hazard),
                new TerrainTileData(new GridPosition(9, 11), TerrainType.Hazard),
                new TerrainTileData(new GridPosition(5, 10), TerrainType.Forest),
                new TerrainTileData(new GridPosition(5, 11), TerrainType.Forest),
                new TerrainTileData(new GridPosition(12, 10), TerrainType.Forest),
                new TerrainTileData(new GridPosition(12, 11), TerrainType.Forest),
            };

            StageDefinitionData stage = new StageDefinitionData(
                "Luocheng Siege",
                "stage.luocheng_siege",
                18,
                16,
                openingSpawns,
                blockedCells,
                terrainTiles);

            ObjectiveState openingObjective = new ObjectiveState(
                "objective.luocheng.opening",
                "Break the outer gate line and breach Luocheng.",
                "objective.luocheng.failure",
                "Liu Bei falls or all allies are defeated.");
            ObjectiveState finalObjective = new ObjectiveState(
                "objective.luocheng.final",
                "Push through the breach and defeat the Luocheng commandant.",
                "objective.luocheng.failure",
                "Liu Bei falls or all allies are defeated.");

            BattlefieldMutation gateBreachMutation = new BattlefieldMutation(
                new List<TerrainTileData>
                {
                    new TerrainTileData(new GridPosition(8, 8), TerrainType.Plain),
                    new TerrainTileData(new GridPosition(9, 8), TerrainType.Plain),
                    new TerrainTileData(new GridPosition(8, 10), TerrainType.Hazard),
                    new TerrainTileData(new GridPosition(9, 10), TerrainType.Hazard),
                    new TerrainTileData(new GridPosition(8, 11), TerrainType.Hazard),
                    new TerrainTileData(new GridPosition(9, 11), TerrainType.Hazard),
                },
                new List<BlockedCellStateChange>
                {
                    new BlockedCellStateChange(new GridPosition(8, 8), false),
                    new BlockedCellStateChange(new GridPosition(9, 8), false),
                });

            List<ScenarioDirective> breachDirectives = new List<ScenarioDirective>
            {
                ScenarioDirective.SetFlag(LuochengGateBreachedFlag),
                ScenarioDirective.ApplyBattlefieldMutation(gateBreachMutation),
                ScenarioDirective.SpawnUnits(new List<UnitSpawnData>
                {
                    SpawnEnemy("enemy-luocheng-commandant", "Luocheng Commandant", UnitRole.Commander, PassiveSkillType.CommandAura, ActiveSkillType.PowerStrike, 35, 11, 5, 3, 1, new GridPosition(9, 13), AiProfileType.Boss),
                    SpawnEnemy("enemy-luocheng-street-guard-a", "Street Guard", UnitRole.Guardian, PassiveSkillType.ShieldWall, ActiveSkillType.None, 29, 9, 5, 2, 1, new GridPosition(7, 11), AiProfileType.Protector),
                    SpawnEnemy("enemy-luocheng-street-guard-b", "Street Guard", UnitRole.Guardian, PassiveSkillType.ShieldWall, ActiveSkillType.None, 29, 9, 5, 2, 1, new GridPosition(11, 11), AiProfileType.Protector),
                }),
                ScenarioDirective.UpdateObjective(finalObjective),
                ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                {
                    Line("unit.player_zhuge_liang", "Zhuge Liang", "dialogue.luocheng.mid.1", "The gate is open. Drive into the inner street before they can stack a second wall behind it."),
                    Line("unit.zhang_fei", "Zhang Fei", "dialogue.luocheng.mid.2", "At last. I am done arguing with a wooden gate."),
                    Line("unit.liu_bei", "Liu Bei", "dialogue.luocheng.mid.3", "Press through the breach and end the city command."),
                }),
            };

            List<ScenarioTrigger> triggers = new List<ScenarioTrigger>
            {
                new ScenarioTrigger(
                    "luocheng-intro",
                    ScenarioCheckpoint.BattleStart,
                    new List<ScenarioDirective>
                    {
                        ScenarioDirective.UpdateObjective(openingObjective),
                        ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                        {
                            Line("unit.liu_bei", "Liu Bei", "dialogue.luocheng.opening.1", "Luocheng will not yield to patience. We break the gate, then take the street before they settle."),
                            Line("unit.player_zhuge_liang", "Zhuge Liang", "dialogue.luocheng.opening.2", "The outer line is short but dense. Once the gate cracks, the city turns into a funnel."),
                            Line("unit.player_ma_chao", "Ma Chao", "dialogue.luocheng.opening.3", "Then hit hard enough that they never recover the lane."),
                        }),
                    }),
                new ScenarioTrigger(
                    "luocheng-breach-kill",
                    ScenarioCheckpoint.ActionResolved,
                    breachDirectives,
                    requiredDefeatedUnitIds: new List<string> { "enemy-luocheng-gate-captain", "enemy-luocheng-wall-bow" },
                    exclusivityGroupId: "luocheng-breach"),
                new ScenarioTrigger(
                    "luocheng-breach-round",
                    ScenarioCheckpoint.EnemyTurnStart,
                    breachDirectives,
                    minimumRoundNumber: 4,
                    exclusivityGroupId: "luocheng-breach"),
                new ScenarioTrigger(
                    "luocheng-liu-bei-falls",
                    ScenarioCheckpoint.ActionResolved,
                    new List<ScenarioDirective> { ScenarioDirective.SetBattleOutcome(TurnSide.Enemy) },
                    requiredDefeatedUnitIds: new List<string> { "player-liu-bei" }),
                new ScenarioTrigger(
                    "luocheng-boss-falls",
                    ScenarioCheckpoint.ActionResolved,
                    new List<ScenarioDirective> { ScenarioDirective.SetBattleOutcome(TurnSide.Player) },
                    requiredDefeatedUnitIds: new List<string> { "enemy-luocheng-commandant" },
                    requiredFlags: new List<string> { LuochengGateBreachedFlag }),
            };

            return new BattleScenarioData(
                LuochengScenarioId,
                "Luocheng Siege",
                "scenario.luocheng_siege",
                stage,
                triggers,
                6,
                108,
                38,
                new RewardBundle(235, 4, "luocheng-breach-hammer"));
        }

        public static BattleScenarioData CreateYangpingPass()
        {
            List<UnitSpawnData> openingSpawns = new List<UnitSpawnData>
            {
                SpawnPlayerLiuBei(new GridPosition(1, 5)),
                SpawnPlayerGuanYu(new GridPosition(2, 6)),
                SpawnPlayerZhangFei(new GridPosition(2, 4)),
                SpawnPlayerHuangZhong(new GridPosition(1, 7)),
                SpawnPlayerZhugeLiang(new GridPosition(0, 5)),
                SpawnPlayerZhaoYun(new GridPosition(1, 3)),
                SpawnPlayerMaChao(new GridPosition(1, 8)),
                SpawnEnemy("enemy-yangping-left-sentry", "Left Sentry", UnitRole.Guardian, PassiveSkillType.ShieldWall, ActiveSkillType.None, 31, 10, 6, 2, 1, new GridPosition(8, 3), AiProfileType.Protector),
                SpawnEnemy("enemy-yangping-right-sentry", "Right Sentry", UnitRole.Guardian, PassiveSkillType.ShieldWall, ActiveSkillType.None, 31, 10, 6, 2, 1, new GridPosition(8, 8), AiProfileType.Protector),
                SpawnEnemy("enemy-yangping-bow-chief", "Pass Bow Chief", UnitRole.Ranger, PassiveSkillType.LongShot, ActiveSkillType.Volley, 26, 9, 3, 3, 2, new GridPosition(9, 5), AiProfileType.Support),
            };

            StageDefinitionData stage = new StageDefinitionData(
                "Yangping Pass",
                "stage.yangping_pass",
                12,
                12,
                openingSpawns,
                new List<GridPosition>
                {
                    new GridPosition(4, 2),
                    new GridPosition(4, 3),
                    new GridPosition(4, 8),
                    new GridPosition(4, 9),
                    new GridPosition(5, 4),
                    new GridPosition(5, 7),
                    new GridPosition(6, 4),
                    new GridPosition(6, 7),
                    new GridPosition(7, 2),
                    new GridPosition(7, 3),
                    new GridPosition(7, 8),
                    new GridPosition(7, 9),
                    new GridPosition(9, 1),
                    new GridPosition(9, 2),
                },
                new List<TerrainTileData>
                {
                    new TerrainTileData(new GridPosition(5, 2), TerrainType.Fort),
                    new TerrainTileData(new GridPosition(6, 2), TerrainType.Fort),
                    new TerrainTileData(new GridPosition(5, 9), TerrainType.Fort),
                    new TerrainTileData(new GridPosition(6, 9), TerrainType.Fort),
                    new TerrainTileData(new GridPosition(9, 8), TerrainType.Forest),
                    new TerrainTileData(new GridPosition(10, 8), TerrainType.Forest),
                    new TerrainTileData(new GridPosition(3, 6), TerrainType.Forest),
                    new TerrainTileData(new GridPosition(8, 6), TerrainType.Forest),
                });

            ObjectiveState openingObjective = new ObjectiveState(
                "objective.yangping.opening",
                "Break the pass sentries before the mountain closes around the center road.",
                "objective.yangping.failure",
                "Liu Bei falls or all allies are defeated.");
            ObjectiveState finalObjective = new ObjectiveState(
                "objective.yangping.final",
                "Use the opened flank and defeat the Yangping commandant.",
                "objective.yangping.failure",
                "Liu Bei falls or all allies are defeated.");

            BattlefieldMutation rockslideMutation = new BattlefieldMutation(
                new List<TerrainTileData>
                {
                    new TerrainTileData(new GridPosition(5, 5), TerrainType.Hazard),
                    new TerrainTileData(new GridPosition(6, 5), TerrainType.Hazard),
                    new TerrainTileData(new GridPosition(9, 8), TerrainType.Fort),
                    new TerrainTileData(new GridPosition(9, 9), TerrainType.Fort),
                },
                new List<BlockedCellStateChange>
                {
                    new BlockedCellStateChange(new GridPosition(5, 5), true),
                    new BlockedCellStateChange(new GridPosition(6, 5), true),
                    new BlockedCellStateChange(new GridPosition(9, 8), false),
                    new BlockedCellStateChange(new GridPosition(9, 9), false),
                });

            List<ScenarioDirective> rockslideDirectives = new List<ScenarioDirective>
            {
                ScenarioDirective.SetFlag(YangpingRockslideFlag),
                ScenarioDirective.ApplyBattlefieldMutation(rockslideMutation),
                ScenarioDirective.SpawnUnits(new List<UnitSpawnData>
                {
                    SpawnEnemy("enemy-yangping-commandant", "Yangping Commandant", UnitRole.Commander, PassiveSkillType.CommandAura, ActiveSkillType.PowerStrike, 36, 12, 5, 3, 1, new GridPosition(11, 9), AiProfileType.Boss),
                    SpawnEnemy("enemy-yangping-flank-rider", "Flank Rider", UnitRole.Raider, PassiveSkillType.RapidMarch, ActiveSkillType.None, 26, 10, 3, 4, 1, new GridPosition(10, 9), AiProfileType.Aggressor),
                    SpawnEnemy("enemy-yangping-flank-rider-b", "Flank Rider", UnitRole.Raider, PassiveSkillType.RapidMarch, ActiveSkillType.None, 26, 10, 3, 4, 1, new GridPosition(10, 8), AiProfileType.Aggressor),
                }),
                ScenarioDirective.UpdateObjective(finalObjective),
                ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                {
                    Line("unit.player_zhuge_liang", "Zhuge Liang", "dialogue.yangping.mid.1", "Rockfall on the center road. Good. Their left lane is sealed, but the right flank is suddenly open."),
                    Line("unit.player_ma_chao", "Ma Chao", "dialogue.yangping.mid.2", "Then give me the flank and I will break the pass commander from the side."),
                    Line("unit.liu_bei", "Liu Bei", "dialogue.yangping.mid.3", "Shift right. Take the new lane before they understand what the mountain has given us."),
                }),
            };

            List<ScenarioTrigger> triggers = new List<ScenarioTrigger>
            {
                new ScenarioTrigger(
                    "yangping-intro",
                    ScenarioCheckpoint.BattleStart,
                    new List<ScenarioDirective>
                    {
                        ScenarioDirective.UpdateObjective(openingObjective),
                        ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                        {
                            Line("unit.liu_bei", "Liu Bei", "dialogue.yangping.opening.1", "Yangping Pass has two teeth but only one throat. We crack the sentries before the mountain decides the route for us."),
                            Line("unit.player_zhuge_liang", "Zhuge Liang", "dialogue.yangping.opening.2", "Watch the slopes. One collapse could close a road and open another."),
                            Line("unit.player_ma_chao", "Ma Chao", "dialogue.yangping.opening.3", "Then we ride fast enough to take whichever lane survives."),
                        }),
                    }),
                new ScenarioTrigger(
                    "yangping-rockslide-kill",
                    ScenarioCheckpoint.ActionResolved,
                    rockslideDirectives,
                    requiredDefeatedUnitIds: new List<string> { "enemy-yangping-left-sentry", "enemy-yangping-right-sentry" },
                    exclusivityGroupId: "yangping-rockslide"),
                new ScenarioTrigger(
                    "yangping-rockslide-round",
                    ScenarioCheckpoint.EnemyTurnStart,
                    rockslideDirectives,
                    minimumRoundNumber: 3,
                    exclusivityGroupId: "yangping-rockslide"),
                new ScenarioTrigger(
                    "yangping-liu-bei-falls",
                    ScenarioCheckpoint.ActionResolved,
                    new List<ScenarioDirective> { ScenarioDirective.SetBattleOutcome(TurnSide.Enemy) },
                    requiredDefeatedUnitIds: new List<string> { "player-liu-bei" }),
                new ScenarioTrigger(
                    "yangping-boss-falls",
                    ScenarioCheckpoint.ActionResolved,
                    new List<ScenarioDirective> { ScenarioDirective.SetBattleOutcome(TurnSide.Player) },
                    requiredDefeatedUnitIds: new List<string> { "enemy-yangping-commandant" },
                    requiredFlags: new List<string> { YangpingRockslideFlag }),
            };

            return new BattleScenarioData(
                YangpingScenarioId,
                "Yangping Pass",
                "scenario.yangping_pass",
                stage,
                triggers,
                7,
                114,
                40,
                new RewardBundle(245, 4, "yangping-stone-route"));
        }

        public static BattleScenarioData CreateTiandangRaid()
        {
            List<UnitSpawnData> openingSpawns = new List<UnitSpawnData>
            {
                SpawnPlayerLiuBei(new GridPosition(1, 8)),
                SpawnPlayerGuanYu(new GridPosition(2, 9)),
                SpawnPlayerZhangFei(new GridPosition(2, 7)),
                SpawnPlayerHuangZhong(new GridPosition(1, 10)),
                SpawnPlayerZhugeLiang(new GridPosition(0, 8)),
                SpawnPlayerZhaoYun(new GridPosition(1, 6)),
                SpawnPlayerMaChao(new GridPosition(1, 11)),
                SpawnEnemy("enemy-tiandang-left-warden", "Left Beacon Warden", UnitRole.Guardian, PassiveSkillType.ShieldWall, ActiveSkillType.None, 32, 10, 6, 2, 1, new GridPosition(5, 4), AiProfileType.Protector),
                SpawnEnemy("enemy-tiandang-right-warden", "Right Beacon Warden", UnitRole.Ranger, PassiveSkillType.LongShot, ActiveSkillType.Volley, 26, 10, 3, 3, 2, new GridPosition(8, 4), AiProfileType.Support),
                SpawnEnemy("enemy-tiandang-camp-bow-chief", "Camp Bow Chief", UnitRole.Ranger, PassiveSkillType.LongShot, ActiveSkillType.Volley, 25, 9, 3, 3, 2, new GridPosition(7, 2), AiProfileType.Support),
                SpawnEnemy("enemy-tiandang-front-guard", "Front Guard", UnitRole.Guardian, PassiveSkillType.ShieldWall, ActiveSkillType.None, 29, 9, 5, 2, 1, new GridPosition(6, 3), AiProfileType.Protector),
            };

            StageDefinitionData stage = new StageDefinitionData(
                "Tiandang Raid",
                "stage.tiandang_raid",
                12,
                12,
                openingSpawns,
                new List<GridPosition>
                {
                    new GridPosition(3, 5),
                    new GridPosition(3, 6),
                    new GridPosition(4, 5),
                    new GridPosition(4, 6),
                    new GridPosition(4, 7),
                    new GridPosition(6, 5),
                    new GridPosition(6, 6),
                    new GridPosition(6, 7),
                    new GridPosition(7, 6),
                    new GridPosition(7, 7),
                    new GridPosition(9, 4),
                    new GridPosition(9, 5),
                    new GridPosition(9, 6),
                    new GridPosition(9, 7),
                    new GridPosition(9, 8),
                    new GridPosition(10, 5),
                    new GridPosition(10, 6),
                },
                new List<TerrainTileData>
                {
                    new TerrainTileData(new GridPosition(5, 4), TerrainType.Fort),
                    new TerrainTileData(new GridPosition(8, 4), TerrainType.Fort),
                    new TerrainTileData(new GridPosition(2, 4), TerrainType.Forest),
                    new TerrainTileData(new GridPosition(3, 4), TerrainType.Forest),
                    new TerrainTileData(new GridPosition(8, 8), TerrainType.Forest),
                    new TerrainTileData(new GridPosition(5, 2), TerrainType.Hazard),
                    new TerrainTileData(new GridPosition(8, 2), TerrainType.Hazard),
                });

            ObjectiveState openingObjective = new ObjectiveState(
                "objective.tiandang.opening",
                "Secure both signal beacons before the Tiandang camp can raise the alarm.",
                "objective.tiandang.failure",
                "Liu Bei falls or all allies are defeated.");
            ObjectiveState finalObjective = new ObjectiveState(
                "objective.tiandang.final",
                "Defeat the Tiandang commandant before the camp regains control of the ridge.",
                "objective.tiandang.failure",
                "Liu Bei falls or all allies are defeated.");

            BattlefieldMutation alarmMutation = new BattlefieldMutation(
                new List<TerrainTileData>
                {
                    new TerrainTileData(new GridPosition(5, 2), TerrainType.Hazard),
                    new TerrainTileData(new GridPosition(8, 2), TerrainType.Hazard),
                    new TerrainTileData(new GridPosition(9, 7), TerrainType.Fort),
                    new TerrainTileData(new GridPosition(9, 8), TerrainType.Fort),
                },
                new List<BlockedCellStateChange>
                {
                    new BlockedCellStateChange(new GridPosition(9, 7), false),
                    new BlockedCellStateChange(new GridPosition(9, 8), false),
                });

            List<ScenarioDirective> securedDirectives = new List<ScenarioDirective>
            {
                ScenarioDirective.SetFlag(TiandangSignalsSecuredFlag),
                ScenarioDirective.SpawnUnits(new List<UnitSpawnData>
                {
                    SpawnEnemy("enemy-tiandang-commandant", "Tiandang Commandant", UnitRole.Commander, PassiveSkillType.CommandAura, ActiveSkillType.PowerStrike, 36, 11, 5, 3, 1, new GridPosition(10, 2), AiProfileType.Boss),
                }),
                ScenarioDirective.UpdateObjective(finalObjective),
                ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                {
                    Line("unit.huang_zhong", "Huang Zhong", "dialogue.tiandang.mid.secured.1", "Both beacons are dark. Their camp has lost the ridge before the alarm could travel."),
                    Line("unit.liu_bei", "Liu Bei", "dialogue.tiandang.mid.secured.2", "Good. Strike the command tent now and end the raid cleanly."),
                }),
            };

            List<ScenarioDirective> alarmDirectives = new List<ScenarioDirective>
            {
                ScenarioDirective.SetFlag(TiandangAlarmFlag),
                ScenarioDirective.ApplyBattlefieldMutation(alarmMutation),
                ScenarioDirective.SpawnUnits(new List<UnitSpawnData>
                {
                    SpawnEnemy("enemy-tiandang-commandant", "Tiandang Commandant", UnitRole.Commander, PassiveSkillType.CommandAura, ActiveSkillType.PowerStrike, 36, 11, 5, 3, 1, new GridPosition(10, 2), AiProfileType.Boss),
                    SpawnEnemy("enemy-tiandang-flank-rider-a", "Alarm Rider", UnitRole.Raider, PassiveSkillType.RapidMarch, ActiveSkillType.None, 27, 10, 3, 4, 1, new GridPosition(10, 8), AiProfileType.Aggressor),
                    SpawnEnemy("enemy-tiandang-flank-rider-b", "Alarm Rider", UnitRole.Raider, PassiveSkillType.RapidMarch, ActiveSkillType.None, 27, 10, 3, 4, 1, new GridPosition(11, 8), AiProfileType.Aggressor),
                }),
                ScenarioDirective.UpdateObjective(finalObjective),
                ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                {
                    Line("unit.enemy_tiandang_commandant", "Tiandang Commandant", "dialogue.tiandang.mid.alarm.1", "Beacon fire to the flank! Open the ridge path and ride them down."),
                    Line("unit.player_ma_chao", "Ma Chao", "dialogue.tiandang.mid.alarm.2", "They opened the side lane themselves. Good. I'll meet the riders there."),
                    Line("unit.liu_bei", "Liu Bei", "dialogue.tiandang.mid.alarm.3", "Hold the ridge and cut down the commandant before the alarm becomes a full counterattack."),
                }),
            };

            List<ScenarioTrigger> triggers = new List<ScenarioTrigger>
            {
                new ScenarioTrigger(
                    "tiandang-intro",
                    ScenarioCheckpoint.BattleStart,
                    new List<ScenarioDirective>
                    {
                        ScenarioDirective.UpdateObjective(openingObjective),
                        ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                        {
                            Line("unit.player_zhuge_liang", "Zhuge Liang", "dialogue.tiandang.opening.1", "Tiandang's ridge is held by its beacons. If both fires go dark, the camp loses its timing."),
                            Line("unit.huang_zhong", "Huang Zhong", "dialogue.tiandang.opening.2", "Then we silence the signal towers before their command can spread across the slope."),
                            Line("unit.liu_bei", "Liu Bei", "dialogue.tiandang.opening.3", "Take the beacons first. If the alarm rises, this raid turns into a grind on the ridge."),
                        }),
                    }),
                new ScenarioTrigger(
                    "tiandang-signals-secured",
                    ScenarioCheckpoint.ActionResolved,
                    securedDirectives,
                    requiredDefeatedUnitIds: new List<string> { "enemy-tiandang-left-warden", "enemy-tiandang-right-warden" },
                    exclusivityGroupId: "tiandang-resolution"),
                new ScenarioTrigger(
                    "tiandang-alarm",
                    ScenarioCheckpoint.EnemyTurnStart,
                    alarmDirectives,
                    minimumRoundNumber: 4,
                    exclusivityGroupId: "tiandang-resolution"),
                new ScenarioTrigger(
                    "tiandang-liu-bei-falls",
                    ScenarioCheckpoint.ActionResolved,
                    new List<ScenarioDirective> { ScenarioDirective.SetBattleOutcome(TurnSide.Enemy) },
                    requiredDefeatedUnitIds: new List<string> { "player-liu-bei" }),
                new ScenarioTrigger(
                    "tiandang-boss-falls",
                    ScenarioCheckpoint.ActionResolved,
                    new List<ScenarioDirective> { ScenarioDirective.SetBattleOutcome(TurnSide.Player) },
                    requiredDefeatedUnitIds: new List<string> { "enemy-tiandang-commandant" },
                    requiredFlags: new List<string> { TiandangAlarmFlag },
                    exclusivityGroupId: "tiandang-victory"),
                new ScenarioTrigger(
                    "tiandang-boss-falls-silent",
                    ScenarioCheckpoint.ActionResolved,
                    new List<ScenarioDirective> { ScenarioDirective.SetBattleOutcome(TurnSide.Player) },
                    requiredDefeatedUnitIds: new List<string> { "enemy-tiandang-commandant" },
                    requiredFlags: new List<string> { TiandangSignalsSecuredFlag },
                    exclusivityGroupId: "tiandang-victory"),
                new ScenarioTrigger(
                    "tiandang-victory-dialogue",
                    ScenarioCheckpoint.PreBattleOutcome,
                    new List<ScenarioDirective>
                    {
                        ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                        {
                            Line("unit.liu_bei", "Liu Bei", "dialogue.tiandang.victory.1", "Tiandang's ridge is ours. Whether by silence or alarm, their camp lost the mountain tonight."),
                            Line("unit.huang_zhong", "Huang Zhong", "dialogue.tiandang.victory.2", "Good. The next strike into Hanzhong will land with their eyes still turned the wrong way."),
                        }),
                    },
                    requiresBattleEnded: true,
                    requiredWinningSide: TurnSide.Player),
                new ScenarioTrigger(
                    "tiandang-defeat-dialogue",
                    ScenarioCheckpoint.PreBattleOutcome,
                    new List<ScenarioDirective>
                    {
                        ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                        {
                            Line("speaker.narrator", "Narrator", "dialogue.tiandang.defeat.1", "Signal fires leap from ridge to ridge, and the Tiandang raid dies beneath the answering alarm."),
                        }),
                    },
                    requiresBattleEnded: true,
                    requiredWinningSide: TurnSide.Enemy),
            };

            return new BattleScenarioData(
                TiandangScenarioId,
                "Tiandang Raid",
                "scenario.tiandang_raid",
                stage,
                triggers,
                8,
                116,
                40,
                new RewardBundle(252, 4, "tiandang-falcon-badge"),
                bonusRewards: CreateTiandangBonusRewards());
        }

        public static BattleScenarioData CreateHanshui()
        {
            List<UnitSpawnData> openingSpawns = new List<UnitSpawnData>
            {
                SpawnPlayerLiuBei(new GridPosition(1, 4)),
                SpawnPlayerHuangZhong(new GridPosition(2, 5)),
                SpawnPlayerZhaoYun(new GridPosition(2, 3)),
                SpawnPlayerZhugeLiang(new GridPosition(1, 6)),
                SpawnPlayerMaChao(new GridPosition(1, 2)),
                SpawnEnemy("enemy-hanshui-shield-captain", "Shield Captain", UnitRole.Guardian, PassiveSkillType.ShieldWall, ActiveSkillType.None, 32, 9, 6, 2, 1, new GridPosition(10, 4), AiProfileType.Protector),
                SpawnEnemy("enemy-hanshui-bow-captain", "Bow Captain", UnitRole.Ranger, PassiveSkillType.LongShot, ActiveSkillType.Volley, 26, 9, 3, 3, 2, new GridPosition(11, 2), AiProfileType.Support),
                SpawnEnemy("enemy-hanshui-river-rider", "River Rider", UnitRole.Raider, PassiveSkillType.RapidMarch, ActiveSkillType.PowerStrike, 26, 10, 3, 4, 1, new GridPosition(11, 6), AiProfileType.Aggressor),
                SpawnEnemy("enemy-hanshui-guard", "River Guard", UnitRole.Guardian, PassiveSkillType.ShieldWall, ActiveSkillType.None, 28, 8, 5, 2, 1, new GridPosition(12, 4), AiProfileType.Protector),
            };

            StageDefinitionData stage = new StageDefinitionData(
                "Hanshui",
                "stage.hanshui",
                14,
                10,
                openingSpawns,
                new List<GridPosition>
                {
                    new GridPosition(5, 1),
                    new GridPosition(5, 2),
                    new GridPosition(5, 7),
                    new GridPosition(5, 8),
                    new GridPosition(6, 2),
                    new GridPosition(6, 7),
                    new GridPosition(7, 2),
                    new GridPosition(7, 7),
                    new GridPosition(8, 1),
                    new GridPosition(8, 2),
                    new GridPosition(8, 7),
                    new GridPosition(8, 8),
                },
                new List<TerrainTileData>
                {
                    new TerrainTileData(new GridPosition(4, 4), TerrainType.Fort),
                    new TerrainTileData(new GridPosition(5, 4), TerrainType.Fort),
                    new TerrainTileData(new GridPosition(4, 5), TerrainType.Fort),
                    new TerrainTileData(new GridPosition(5, 5), TerrainType.Fort),
                    new TerrainTileData(new GridPosition(9, 4), TerrainType.Fort),
                    new TerrainTileData(new GridPosition(9, 5), TerrainType.Fort),
                    new TerrainTileData(new GridPosition(3, 2), TerrainType.Forest),
                    new TerrainTileData(new GridPosition(3, 7), TerrainType.Forest),
                    new TerrainTileData(new GridPosition(10, 1), TerrainType.Hazard),
                    new TerrainTileData(new GridPosition(10, 8), TerrainType.Hazard),
                });

            ObjectiveState openingObjective = new ObjectiveState(
                "objective.hanshui.opening",
                "Hold the Han camp through the third round and keep Liu Bei alive.",
                "objective.hanshui.failure",
                "Liu Bei falls or all allies are defeated.");
            ObjectiveState finalObjective = new ObjectiveState(
                "objective.hanshui.final",
                "Counterattack and defeat the Hanshui field commander.",
                "objective.hanshui.failure",
                "Liu Bei falls or all allies are defeated.");

            List<ScenarioDirective> counterattackDirectives = new List<ScenarioDirective>
            {
                ScenarioDirective.SetFlag(HanshuiCounterattackFlag),
                ScenarioDirective.SpawnUnits(new List<UnitSpawnData>
                {
                    SpawnEnemy("enemy-hanshui-commander", "Hanshui Commander", UnitRole.Commander, PassiveSkillType.CommandAura, ActiveSkillType.PowerStrike, 35, 11, 5, 3, 1, new GridPosition(13, 4), AiProfileType.Boss),
                    SpawnEnemy("enemy-hanshui-deadeye", "Wei Deadeye", UnitRole.Ranger, PassiveSkillType.LongShot, ActiveSkillType.Volley, 24, 9, 3, 3, 2, new GridPosition(12, 1), AiProfileType.Support),
                    SpawnEnemy("enemy-hanshui-raider", "Hanshui Raider", UnitRole.Raider, PassiveSkillType.RapidMarch, ActiveSkillType.None, 25, 9, 3, 4, 1, new GridPosition(12, 7), AiProfileType.Aggressor),
                }),
                ScenarioDirective.UpdateObjective(finalObjective),
                ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                {
                    Line("unit.player_zhao_yun", "Zhao Yun", "dialogue.hanshui.mid.1", "Their advance has spent itself. Now we drive back across the river road."),
                    Line("unit.huang_zhong", "Huang Zhong", "dialogue.hanshui.mid.2", "Good. I have had enough of trading arrows from the bank."),
                    Line("unit.liu_bei", "Liu Bei", "dialogue.hanshui.mid.3", "Counterattack. Break the field commander before the river line can form again."),
                }),
            };

            List<ScenarioTrigger> triggers = new List<ScenarioTrigger>
            {
                new ScenarioTrigger(
                    "hanshui-intro",
                    ScenarioCheckpoint.BattleStart,
                    new List<ScenarioDirective>
                    {
                        ScenarioDirective.UpdateObjective(openingObjective),
                        ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                        {
                            Line("unit.player_zhuge_liang", "Zhuge Liang", "dialogue.hanshui.opening.1", "Hanshui rewards patience. Hold the camp line until their first wave commits too far."),
                            Line("unit.huang_zhong", "Huang Zhong", "dialogue.hanshui.opening.2", "Then Zhao Yun and I strike once their front foot slips."),
                            Line("unit.liu_bei", "Liu Bei", "dialogue.hanshui.opening.3", "Steady the line. We answer only when the riverbank favors us."),
                        }),
                    }),
                new ScenarioTrigger(
                    "hanshui-counterattack-round",
                    ScenarioCheckpoint.PlayerTurnStart,
                    counterattackDirectives,
                    minimumRoundNumber: 4,
                    requiredAliveUnitIds: new List<string> { "player-liu-bei" },
                    exclusivityGroupId: "hanshui-counterattack"),
                new ScenarioTrigger(
                    "hanshui-counterattack-kill",
                    ScenarioCheckpoint.ActionResolved,
                    counterattackDirectives,
                    requiredDefeatedUnitIds: new List<string>
                    {
                        "enemy-hanshui-shield-captain",
                        "enemy-hanshui-bow-captain",
                        "enemy-hanshui-river-rider",
                        "enemy-hanshui-guard",
                    },
                    requiredAliveUnitIds: new List<string> { "player-liu-bei" },
                    exclusivityGroupId: "hanshui-counterattack"),
                new ScenarioTrigger(
                    "hanshui-liu-bei-falls",
                    ScenarioCheckpoint.ActionResolved,
                    new List<ScenarioDirective> { ScenarioDirective.SetBattleOutcome(TurnSide.Enemy) },
                    requiredDefeatedUnitIds: new List<string> { "player-liu-bei" }),
                new ScenarioTrigger(
                    "hanshui-boss-falls",
                    ScenarioCheckpoint.ActionResolved,
                    new List<ScenarioDirective> { ScenarioDirective.SetBattleOutcome(TurnSide.Player) },
                    requiredDefeatedUnitIds: new List<string> { "enemy-hanshui-commander" },
                    requiredFlags: new List<string> { HanshuiCounterattackFlag }),
                new ScenarioTrigger(
                    "hanshui-victory-dialogue",
                    ScenarioCheckpoint.PreBattleOutcome,
                    new List<ScenarioDirective>
                    {
                        ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                        {
                            Line("unit.huang_zhong", "Huang Zhong", "dialogue.hanshui.victory.1", "Hanshui breaks our way. That counterstroke will echo across the whole river line."),
                            Line("unit.liu_bei", "Liu Bei", "dialogue.hanshui.victory.2", "Then we carry that momentum forward. The next ridge must fall with it."),
                        }),
                    },
                    requiresBattleEnded: true,
                    requiredWinningSide: TurnSide.Player),
                new ScenarioTrigger(
                    "hanshui-defeat-dialogue",
                    ScenarioCheckpoint.PreBattleOutcome,
                    new List<ScenarioDirective>
                    {
                        ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                        {
                            Line("speaker.narrator", "Narrator", "dialogue.hanshui.defeat.1", "The Han camp is driven from the riverbank, and the chance to reverse the line at Hanshui is lost."),
                        }),
                    },
                    requiresBattleEnded: true,
                    requiredWinningSide: TurnSide.Enemy),
            };

            return new BattleScenarioData(
                HanshuiScenarioId,
                "Hanshui",
                "scenario.hanshui",
                stage,
                triggers,
                8,
                104,
                36,
                new RewardBundle(230, 3, "hanshui-command-seal"),
                duelScenes: CreateHanshuiDuelScenes());
        }

        public static BattleScenarioData CreateDingjunMountain()
        {
            List<UnitSpawnData> openingSpawns = new List<UnitSpawnData>
            {
                SpawnPlayerLiuBei(new GridPosition(1, 6)),
                SpawnPlayerGuanYu(new GridPosition(2, 7)),
                SpawnPlayerHuangZhong(new GridPosition(1, 4)),
                SpawnPlayerZhaoYun(new GridPosition(2, 5)),
                SpawnPlayerZhugeLiang(new GridPosition(0, 6)),
                SpawnPlayerMaChao(new GridPosition(1, 8)),
                SpawnEnemy("enemy-wei_vanguard_captain", "Wei Vanguard Captain", UnitRole.Guardian, PassiveSkillType.ShieldWall, ActiveSkillType.PowerStrike, 30, 10, 6, 2, 1, new GridPosition(8, 5), AiProfileType.Protector),
                SpawnEnemy("enemy-wei_archer_captain", "Wei Archer Captain", UnitRole.Ranger, PassiveSkillType.LongShot, ActiveSkillType.Volley, 26, 9, 3, 3, 2, new GridPosition(8, 7), AiProfileType.Support),
                SpawnEnemy("enemy-wei_shieldman", "Wei Shieldman", UnitRole.Guardian, PassiveSkillType.ShieldWall, ActiveSkillType.None, 28, 8, 6, 2, 1, new GridPosition(10, 5), AiProfileType.Protector),
                SpawnEnemy("enemy-wei_skirmisher", "Wei Skirmisher", UnitRole.Raider, PassiveSkillType.RapidMarch, ActiveSkillType.None, 24, 9, 3, 4, 1, new GridPosition(10, 8), AiProfileType.Aggressor),
            };

            StageDefinitionData stage = new StageDefinitionData(
                "Dingjun Mountain",
                "stage.dingjun_mountain",
                12,
                12,
                openingSpawns,
                new List<GridPosition>
                {
                    new GridPosition(4, 1),
                    new GridPosition(4, 2),
                    new GridPosition(4, 3),
                    new GridPosition(4, 8),
                    new GridPosition(4, 9),
                    new GridPosition(4, 10),
                    new GridPosition(5, 3),
                    new GridPosition(5, 8),
                    new GridPosition(6, 3),
                    new GridPosition(6, 8),
                    new GridPosition(8, 2),
                    new GridPosition(8, 3),
                    new GridPosition(8, 8),
                    new GridPosition(8, 9),
                },
                new List<TerrainTileData>
                {
                    new TerrainTileData(new GridPosition(3, 5), TerrainType.Forest),
                    new TerrainTileData(new GridPosition(3, 6), TerrainType.Forest),
                    new TerrainTileData(new GridPosition(7, 6), TerrainType.Fort),
                    new TerrainTileData(new GridPosition(9, 6), TerrainType.Fort),
                    new TerrainTileData(new GridPosition(10, 4), TerrainType.Forest),
                    new TerrainTileData(new GridPosition(10, 8), TerrainType.Forest),
                    new TerrainTileData(new GridPosition(6, 5), TerrainType.Fort),
                    new TerrainTileData(new GridPosition(6, 6), TerrainType.Fort),
                });

            ObjectiveState openingObjective = new ObjectiveState(
                "objective.dingjun.opening",
                "Defeat the Wei Vanguard Captain and the Wei Archer Captain.",
                "objective.dingjun.failure",
                "Liu Bei falls or all allies are defeated.");
            ObjectiveState finalObjective = new ObjectiveState(
                "objective.dingjun.final",
                "Defeat Xiahou Yuan.",
                "objective.dingjun.failure",
                "Liu Bei falls or all allies are defeated.");

            List<ScenarioDirective> bossDirectives = new List<ScenarioDirective>
            {
                ScenarioDirective.SetFlag(DingjunBossArrivedFlag),
                ScenarioDirective.SpawnUnits(new List<UnitSpawnData>
                {
                    SpawnEnemy("enemy-xiahou-yuan", "Xiahou Yuan", UnitRole.Commander, PassiveSkillType.CommandAura, ActiveSkillType.PowerStrike, 36, 12, 5, 4, 1, new GridPosition(11, 6), AiProfileType.Boss),
                    SpawnEnemy("enemy-tiger_guard_captain", "Tiger Guard Captain", UnitRole.Guardian, PassiveSkillType.ShieldWall, ActiveSkillType.None, 30, 9, 6, 2, 1, new GridPosition(10, 4), AiProfileType.Protector),
                    SpawnEnemy("enemy-wei_deadeye", "Wei Deadeye", UnitRole.Ranger, PassiveSkillType.LongShot, ActiveSkillType.Volley, 24, 9, 3, 3, 2, new GridPosition(10, 8), AiProfileType.Support),
                }),
                ScenarioDirective.UpdateObjective(finalObjective),
                ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                {
                    Line("unit.enemy_xiahou_yuan", "Xiahou Yuan", "dialogue.dingjun.mid.1", "So this is the spearpoint that broke my forward camp. Come and test yourselves against me."),
                    Line("unit.huang_zhong", "Huang Zhong", "dialogue.dingjun.mid.2", "The mountain mouth is open. Xiahou Yuan has finally shown himself."),
                    Line("unit.liu_bei", "Liu Bei", "dialogue.dingjun.mid.3", "Press the advantage. Finish this here and the Hanzhong line opens before us."),
                }),
            };

            List<ScenarioTrigger> triggers = new List<ScenarioTrigger>
            {
                new ScenarioTrigger(
                    "dingjun-intro",
                    ScenarioCheckpoint.BattleStart,
                    new List<ScenarioDirective>
                    {
                        ScenarioDirective.UpdateObjective(openingObjective),
                        ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                        {
                            Line("unit.liu_bei", "Liu Bei", "dialogue.dingjun.opening.1", "Break the forward camp first. Once the pass opens, Xiahou Yuan will have to answer in person."),
                            Line("unit.player_zhao_yun", "Zhao Yun", "dialogue.dingjun.opening.2", "Their shield line is narrow but stubborn. We strike cleanly, then drive through."),
                            Line("unit.huang_zhong", "Huang Zhong", "dialogue.dingjun.opening.3", "Good. Once the pass opens, my arrows will finish what the mountain begins."),
                        }),
                    }),
                new ScenarioTrigger(
                    "dingjun-boss-arrives",
                    ScenarioCheckpoint.ActionResolved,
                    bossDirectives,
                    requiredDefeatedUnitIds: new List<string> { "enemy-wei_vanguard_captain", "enemy-wei_archer_captain" },
                    exclusivityGroupId: "dingjun-boss"),
                new ScenarioTrigger(
                    "dingjun-liu-bei-falls",
                    ScenarioCheckpoint.ActionResolved,
                    new List<ScenarioDirective> { ScenarioDirective.SetBattleOutcome(TurnSide.Enemy) },
                    requiredDefeatedUnitIds: new List<string> { "player-liu-bei" }),
                new ScenarioTrigger(
                    "dingjun-boss-falls",
                    ScenarioCheckpoint.ActionResolved,
                    new List<ScenarioDirective> { ScenarioDirective.SetBattleOutcome(TurnSide.Player) },
                    requiredDefeatedUnitIds: new List<string> { "enemy-xiahou-yuan" },
                    requiredFlags: new List<string> { DingjunBossArrivedFlag }),
                new ScenarioTrigger(
                    "dingjun-victory-dialogue",
                    ScenarioCheckpoint.PreBattleOutcome,
                    new List<ScenarioDirective>
                    {
                        ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                        {
                            Line("unit.liu_bei", "Liu Bei", "dialogue.dingjun.victory.1", "Dingjun Mountain is ours. The road into Hanzhong has changed hands today."),
                            Line("unit.huang_zhong", "Huang Zhong", "dialogue.dingjun.victory.2", "Xiahou Yuan is down. The Wei camp will not recover its footing before dawn."),
                        }),
                    },
                    requiresBattleEnded: true,
                    requiredWinningSide: TurnSide.Player),
                new ScenarioTrigger(
                    "dingjun-defeat-dialogue",
                    ScenarioCheckpoint.PreBattleOutcome,
                    new List<ScenarioDirective>
                    {
                        ScenarioDirective.QueueDialogue(new List<ScenarioDialogueLine>
                        {
                            Line("speaker.narrator", "Narrator", "dialogue.dingjun.defeat.1", "The mountain road closes again under Wei banners, and the chance at Dingjun slips away."),
                        }),
                    },
                    requiresBattleEnded: true,
                    requiredWinningSide: TurnSide.Enemy),
            };

            return new BattleScenarioData(
                DingjunScenarioId,
                "Dingjun Mountain",
                "scenario.dingjun_mountain",
                stage,
                triggers,
                9,
                118,
                40,
                new RewardBundle(260, 4, "dingjun-war-banner"),
                bonusRewards: CreateDingjunBonusRewards(),
                duelScenes: CreateDingjunDuelScenes());
        }

        private static IReadOnlyList<BonusRewardDefinition> CreateGuangzongBonusRewards()
        {
            return new List<BonusRewardDefinition>
            {
                new BonusRewardDefinition(
                    "bonus.guangzong.rally_seal",
                    "guangzong-rally-seal",
                    "bonus.guangzong.objective",
                    "第 2 回合結束前擊破外線兩名守將，且劉備、關羽、張飛、黃忠全員存活。",
                    "bonus.guangzong.summary",
                    "兩名外線守將在亂軍收束前被迅速擊破。",
                    requiredFlags: new List<string> { GuangzongRapidSealSecuredFlag },
                    excludedFlags: new List<string> { GuangzongRapidSealFailedFlag },
                    requiredAliveUnitIds: new List<string> { "player-liu-bei", "player-guan-yu", "player-zhang-fei", "player-huang-zhong" }),
            };
        }

        private static IReadOnlyList<BonusRewardDefinition> CreateChangbanBonusRewards()
        {
            return new List<BonusRewardDefinition>
            {
                new BonusRewardDefinition(
                    "bonus.changban.white_plume",
                    "changban-white-plume",
                    "bonus.changban.objective",
                    "劉備存活，並由趙雲在長坂發動一騎收束追擊主將。",
                    "bonus.changban.summary",
                    "趙雲在長坂親自截斷追軍鋒頭。",
                    requiredAliveUnitIds: new List<string> { "player-liu-bei" },
                    requiredTriggeredDuelIds: new List<string> { "duel.changban.zhao_yun" }),
            };
        }

        private static IReadOnlyList<BonusRewardDefinition> CreateJiamengBonusRewards()
        {
            return new List<BonusRewardDefinition>
            {
                new BonusRewardDefinition(
                    "bonus.jiameng.iron_girth",
                    "jiameng-iron-girth",
                    "bonus.jiameng.objective",
                    "葭萌主將登場後兩回合內，由馬超發動一騎擊潰守關主將。",
                    "bonus.jiameng.summary",
                    "馬超在葭萌關前以騎陣對騎陣壓垮守軍主將。",
                    requiredFlags: new List<string> { JiamengDuelWonFlag },
                    excludedFlags: new List<string> { JiamengDuelExpiredFlag },
                    requiredTriggeredDuelIds: new List<string> { "duel.jiameng.ma_chao" }),
            };
        }

        private static IReadOnlyList<BonusRewardDefinition> CreateBaishuiBonusRewards()
        {
            return new List<BonusRewardDefinition>
            {
                new BonusRewardDefinition(
                    "bonus.baishui.rapid_order",
                    "baishui-rapid-order",
                    "bonus.baishui.objective",
                    "下橋必須由擊破條件截斷，且趙雲在白水關發動一騎斬落守關主將。",
                    "bonus.baishui.summary",
                    "白水下橋在敵方來不及保底收束前就被反手奪下節奏。",
                    requiredFlags: new List<string> { BaishuiBridgeCutByKillFlag },
                    excludedFlags: new List<string> { BaishuiBridgeCutByTimerFlag },
                    requiredTriggeredDuelIds: new List<string> { "duel.baishui.zhao_yun" }),
            };
        }

        private static IReadOnlyList<BonusRewardDefinition> CreateTiandangBonusRewards()
        {
            return new List<BonusRewardDefinition>
            {
                new BonusRewardDefinition(
                    "bonus.tiandang.night_token",
                    "tiandang-night-token",
                    "bonus.tiandang.objective",
                    "先熄滅兩座信標、全程未觸發警報，且我方無人陣亡。",
                    "bonus.tiandang.summary",
                    "天蕩山在完全未驚動全營的情況下被夜襲拿下。",
                    requiredFlags: new List<string> { TiandangSignalsSecuredFlag },
                    excludedFlags: new List<string> { TiandangAlarmFlag },
                    requiredAliveUnitIds: new List<string>
                    {
                        "player-liu-bei",
                        "player-guan-yu",
                        "player-zhang-fei",
                        "player-huang-zhong",
                        "player-zhuge-liang",
                        "player-zhao-yun",
                        "player-ma-chao",
                    }),
            };
        }

        private static IReadOnlyList<BonusRewardDefinition> CreateDingjunBonusRewards()
        {
            return new List<BonusRewardDefinition>
            {
                new BonusRewardDefinition(
                    "bonus.dingjun.gold_spur",
                    "dingjun-gold-spur",
                    "bonus.dingjun.objective",
                    "由黃忠在定軍山發動一騎親手收束夏侯淵，且場上名將全員存活。",
                    "bonus.dingjun.summary",
                    "黃忠在定軍山前以一騎定勝負，夏侯淵就此折陣。",
                    requiredAliveUnitIds: new List<string>
                    {
                        "player-liu-bei",
                        "player-guan-yu",
                        "player-huang-zhong",
                        "player-zhao-yun",
                        "player-zhuge-liang",
                        "player-ma-chao",
                    },
                    requiredTriggeredDuelIds: new List<string> { "duel.dingjun.huang_zhong" }),
            };
        }

        private static IReadOnlyList<DuelSceneDefinition> CreateChangbanDuelScenes()
        {
            return new List<DuelSceneDefinition>
            {
                new DuelSceneDefinition(
                    "duel.changban.zhao_yun",
                    "player-zhao-yun",
                    "enemy-pursuit_commander",
                    "duel.changban.title",
                    "長坂一騎",
                    minimumRoundNumber: 3,
                    targetHpPercentAtMost: 50,
                    outcome: new DuelOutcomeDefinition(
                        defeatTarget: true,
                        applyStatusesToCaster: new List<StatusEffectDurationDefinition>
                        {
                            new StatusEffectDurationDefinition(StatusEffectType.Guarded, 2),
                            new StatusEffectDurationDefinition(StatusEffectType.Inspired, 2),
                        }))
            };
        }

        private static IReadOnlyList<DuelSceneDefinition> CreateBaishuiDuelScenes()
        {
            return new List<DuelSceneDefinition>
            {
                new DuelSceneDefinition(
                    "duel.baishui.zhao_yun",
                    "player-zhao-yun",
                    "enemy-baishui-commandant",
                    "duel.baishui.title",
                    "白水一騎",
                    requiredFlags: new List<string> { BaishuiBridgeCutFlag },
                    targetHpPercentAtMost: 50,
                    outcome: new DuelOutcomeDefinition(
                        defeatTarget: true,
                        applyStatusesToCaster: new List<StatusEffectDurationDefinition>
                        {
                            new StatusEffectDurationDefinition(StatusEffectType.Guarded, 2),
                        }))
            };
        }

        private static IReadOnlyList<DuelSceneDefinition> CreateJiamengDuelScenes()
        {
            return new List<DuelSceneDefinition>
            {
                new DuelSceneDefinition(
                    "duel.jiameng.ma_chao",
                    "player-ma-chao",
                    "enemy-jiameng-commandant",
                    "duel.jiameng.title",
                    "葭萌關一騎",
                    requiredFlags: new List<string> { JiamengBossArrivedFlag },
                    excludedFlags: new List<string> { JiamengDuelExpiredFlag },
                    targetHpPercentAtMost: 50,
                    outcome: new DuelOutcomeDefinition(
                        defeatTarget: true,
                        applyStatusesToCaster: new List<StatusEffectDurationDefinition>
                        {
                            new StatusEffectDurationDefinition(StatusEffectType.Inspired, 2),
                        },
                        applyStatusesToNearbyEnemies: new List<StatusEffectDurationDefinition>
                        {
                            new StatusEffectDurationDefinition(StatusEffectType.Intimidated, 2),
                        },
                        setFlags: new List<string> { JiamengDuelWonFlag }))
            };
        }

        private static IReadOnlyList<DuelSceneDefinition> CreateHanshuiDuelScenes()
        {
            return new List<DuelSceneDefinition>
            {
                new DuelSceneDefinition(
                    "duel.hanshui.huang_zhong",
                    "player-huang-zhong",
                    "enemy-hanshui-commander",
                    "duel.hanshui.title",
                    "漢水一騎",
                    requiredFlags: new List<string> { HanshuiCounterattackFlag },
                    targetHpPercentAtMost: 50,
                    outcome: new DuelOutcomeDefinition(
                        defeatTarget: true,
                        applyStatusesToNearbyEnemies: new List<StatusEffectDurationDefinition>
                        {
                            new StatusEffectDurationDefinition(StatusEffectType.Intimidated, 2),
                        },
                        nearbyEnemyRadius: 2))
            };
        }

        private static IReadOnlyList<DuelSceneDefinition> CreateDingjunDuelScenes()
        {
            return new List<DuelSceneDefinition>
            {
                new DuelSceneDefinition(
                    "duel.dingjun.huang_zhong",
                    "player-huang-zhong",
                    "enemy-xiahou-yuan",
                    "duel.dingjun.title",
                    "定軍山一騎",
                    requiredFlags: new List<string> { DingjunBossArrivedFlag },
                    targetHpPercentAtMost: 50,
                    outcome: new DuelOutcomeDefinition(defeatTarget: true))
            };
        }

        private static void AddCoreSquad(
            ICollection<UnitSpawnData> spawns,
            GridPosition liuBeiPosition,
            GridPosition guanYuPosition,
            GridPosition zhangFeiPosition,
            GridPosition huangZhongPosition)
        {
            spawns.Add(SpawnPlayerLiuBei(liuBeiPosition));
            spawns.Add(SpawnPlayerGuanYu(guanYuPosition));
            spawns.Add(SpawnPlayerZhangFei(zhangFeiPosition));
            spawns.Add(SpawnPlayerHuangZhong(huangZhongPosition));
        }

        private static UnitSpawnData SpawnPlayerLiuBei(GridPosition position)
        {
            return SpawnPlayer("player-liu-bei", "Liu Bei", UnitRole.Commander, PassiveSkillType.CommandAura, ActiveSkillType.RoyalAid, 30, 9, 5, 3, 1, position, 20, AiProfileType.Protector);
        }

        private static UnitSpawnData SpawnPlayerGuanYu(GridPosition position)
        {
            return SpawnPlayer("player-guan-yu", "Guan Yu", UnitRole.Guardian, PassiveSkillType.ArmorBreak, ActiveSkillType.GreenDragonSlash, 34, 12, 5, 3, 1, position, 20, AiProfileType.Protector);
        }

        private static UnitSpawnData SpawnPlayerZhangFei(GridPosition position)
        {
            return SpawnPlayer("player-zhang-fei", "Zhang Fei", UnitRole.Guardian, PassiveSkillType.Vanguard, ActiveSkillType.WarCry, 36, 11, 6, 3, 1, position, 20, AiProfileType.Aggressor);
        }

        private static UnitSpawnData SpawnPlayerHuangZhong(GridPosition position)
        {
            return SpawnPlayer("player-huang-zhong", "Huang Zhong", UnitRole.Ranger, PassiveSkillType.LongShot, ActiveSkillType.Volley, 28, 10, 3, 3, 2, position, 20, AiProfileType.Support);
        }

        private static UnitSpawnData SpawnPlayerZhugeLiang(GridPosition position)
        {
            return SpawnPlayer("player-zhuge-liang", "Zhuge Liang", UnitRole.Commander, PassiveSkillType.CommandAura, ActiveSkillType.FireStratagem, 26, 8, 3, 3, 1, position, 26, AiProfileType.Support);
        }

        private static UnitSpawnData SpawnPlayerZhaoYun(GridPosition position)
        {
            return SpawnPlayer("player-zhao-yun", "Zhao Yun", UnitRole.Scout, PassiveSkillType.RapidMarch, ActiveSkillType.DragonPierce, 31, 11, 4, 4, 1, position, 18, AiProfileType.Aggressor);
        }

        private static UnitSpawnData SpawnPlayerMaChao(GridPosition position)
        {
            return SpawnPlayer("player-ma-chao", "Ma Chao", UnitRole.Raider, PassiveSkillType.Vanguard, ActiveSkillType.WesternStampede, 33, 12, 4, 4, 1, position, 18, AiProfileType.Aggressor);
        }

        private static UnitSpawnData SpawnPlayer(
            string id,
            string displayName,
            UnitRole role,
            PassiveSkillType passiveSkill,
            ActiveSkillType activeSkill,
            int maxHp,
            int attack,
            int defense,
            int moveRange,
            int attackRange,
            GridPosition position,
            int maxMana,
            AiProfileType aiProfile)
        {
            return Spawn(
                CreateDefinition(id, displayName, UnitFaction.Player, role, passiveSkill, activeSkill, maxHp, attack, defense, moveRange, attackRange, maxMana, aiProfile),
                position);
        }

        private static UnitSpawnData SpawnEnemy(
            string id,
            string displayName,
            UnitRole role,
            PassiveSkillType passiveSkill,
            ActiveSkillType activeSkill,
            int maxHp,
            int attack,
            int defense,
            int moveRange,
            int attackRange,
            GridPosition position,
            AiProfileType aiProfile)
        {
            return Spawn(
                CreateDefinition(id, displayName, UnitFaction.Enemy, role, passiveSkill, activeSkill, maxHp, attack, defense, moveRange, attackRange, 20, aiProfile),
                position);
        }

        private static UnitSpawnData Spawn(UnitDefinitionData definition, GridPosition position)
        {
            return new UnitSpawnData(definition, position);
        }

        private static ScenarioDialogueLine Line(string speakerNameKey, string speakerFallback, string textKey, string textFallback)
        {
            return new ScenarioDialogueLine(speakerNameKey, speakerFallback, textKey, textFallback);
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
            int attackRange,
            int maxMana,
            AiProfileType aiProfile = AiProfileType.Default)
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
                attackRange,
                maxMana,
                null,
                null,
                aiProfile,
                null,
                1,
                0,
                null);
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
