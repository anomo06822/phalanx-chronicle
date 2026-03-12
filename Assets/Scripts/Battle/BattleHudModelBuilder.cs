using System;
using System.Collections.Generic;
using System.Linq;
using PhalanxChronicle.Core;
using PhalanxChronicle.Localization;
using PhalanxChronicle.Presentation;
using PhalanxChronicle.UI;
using UnityEngine;

namespace PhalanxChronicle.Battle
{
    public sealed class BattleHudModelBuilder
    {
        private readonly BattleHudModelSupport support;
        private readonly BattleForecastModelBuilder forecastModelBuilder;
        private readonly BattleSelectionModelBuilder selectionModelBuilder;
        private readonly BattleRosterModelBuilder rosterModelBuilder;
        private readonly BattleActionMenuModelBuilder actionMenuModelBuilder;
        private readonly BattleResultModelBuilder resultModelBuilder;

        public BattleHudModelBuilder()
        {
            support = new BattleHudModelSupport();
            forecastModelBuilder = new BattleForecastModelBuilder(support);
            selectionModelBuilder = new BattleSelectionModelBuilder(support);
            rosterModelBuilder = new BattleRosterModelBuilder(support);
            actionMenuModelBuilder = new BattleActionMenuModelBuilder(support);
            resultModelBuilder = new BattleResultModelBuilder(support);
        }

        public BattleOverviewModel BuildOverviewModel(
            BattleSimulation simulation,
            BattleScenarioData scenarioData,
            ScenarioDirector scenarioDirector,
            string currentTurnText,
            string currentInstructionText)
        {
            if (simulation == null || simulation.Context == null)
            {
                return new BattleOverviewModel();
            }

            ObjectiveState objective = scenarioDirector != null ? scenarioDirector.CurrentObjective : null;
            BattleContext context = simulation.Context;
            int playerAlive = context.GetUnits(UnitFaction.Player).Count;
            int enemyAlive = context.GetUnits(UnitFaction.Enemy).Count;
            int playerTotal = context.Units.Count(unit => unit.Faction == UnitFaction.Player);
            int enemyTotal = context.Units.Count(unit => unit.Faction == UnitFaction.Enemy);
            int readyUnits = context.Units.Count(unit => unit.Faction == UnitFaction.Player && unit.IsAlive && !unit.HasActed);
            int skillReadyUnits = context.Units.Count(unit =>
                unit.Faction == UnitFaction.Player &&
                unit.IsAlive &&
                !unit.HasActed &&
                unit.ActiveSkill != ActiveSkillType.None &&
                unit.HasEnoughMana(ActiveSkillRules.GetManaCost(unit)) &&
                simulation.GetSkillTargets(unit.Id).Count > 0);
            string localizedStageName = LocalizationService.Text(context.StageNameKey, context.StageName);
            string variantSuffix = scenarioData != null && !string.IsNullOrWhiteSpace(scenarioData.ScenarioVariantTag)
                ? " [" + support.FormatScenarioVariantTag(scenarioData.ScenarioVariantTag) + "]"
                : string.Empty;
            string primaryObjective = objective != null
                ? LocalizationService.Text(objective.PrimaryObjectiveKey, objective.PrimaryObjectiveFallback)
                : "-";
            string failureObjective = objective != null
                ? LocalizationService.Text(objective.FailureConditionKey, objective.FailureConditionFallback)
                : "-";

            return new BattleOverviewModel
            {
                HeaderEyebrow = context.CurrentTurnSide == TurnSide.Player
                    ? LocalizationService.Text("ui.turn.player.short", "玩家回合")
                    : LocalizationService.Text("ui.turn.enemy.short", "敵軍回合"),
                StageLabel = LocalizationService.Format("ui.stage", "Stage: {0}", localizedStageName + variantSuffix),
                SeedLabel = context.IsRandomMap
                    ? LocalizationService.Format("ui.seed.value", "Seed: {0}", context.MapSeed)
                    : LocalizationService.Text("ui.seed.fixed", "Seed: Fixed"),
                PhaseLabel = string.IsNullOrEmpty(currentTurnText)
                    ? LocalizationService.Text(
                        context.CurrentTurnSide == TurnSide.Player ? "ui.turn.player" : "ui.turn.enemy",
                        context.CurrentTurnSide == TurnSide.Player ? "Turn: Player Phase" : "Turn: Enemy Phase")
                    : currentTurnText,
                TurnLabel = LocalizationService.Format("ui.turn.count", "Turn {0}", context.TurnNumber),
                PlayerAliveLabel = LocalizationService.Format("ui.overview.player_force", "Allies {0}/{1}", playerAlive, playerTotal),
                EnemyAliveLabel = LocalizationService.Format("ui.overview.enemy_force", "Enemies {0}/{1}", enemyAlive, enemyTotal),
                ReadyLabel = LocalizationService.Format("ui.overview.ready_units", "Ready units {0}", readyUnits),
                SkillReadyLabel = LocalizationService.Format("ui.overview.skill_ready", "Skills ready {0}", skillReadyUnits),
                ObjectivePrimary = LocalizationService.Format("ui.objective.primary", "Primary: {0}", primaryObjective),
                ObjectiveFailure = LocalizationService.Format("ui.objective.failure", "Fail: {0}", failureObjective),
                InstructionText = currentInstructionText,
                SecondaryInstructionText = skillReadyUnits > 0
                    ? LocalizationService.Text("ui.overview.secondary_instruction_skill", "本回合至少有一名友軍能用技能打開節奏。")
                    : LocalizationService.Text("ui.overview.secondary_instruction_form", "如果沒有穩定擊殺線，先保陣形與站位。"),
                HeaderFacts = new List<HudFactModel>
                {
                    new HudFactModel
                    {
                        Label = LocalizationService.Text("ui.turn.count.short", "回合"),
                        Value = context.TurnNumber.ToString(),
                        AccentColor = BattleUiTheme.AccentGold,
                    },
                    new HudFactModel
                    {
                        Label = LocalizationService.Text("ui.overview.player_force.short", "友軍"),
                        Value = LocalizationService.Format("ui.overview.force_value", "{0}/{1}", playerAlive, playerTotal),
                        AccentColor = BattleUiTheme.AccentBlue,
                    },
                    new HudFactModel
                    {
                        Label = LocalizationService.Text("ui.overview.enemy_force.short", "敵軍"),
                        Value = LocalizationService.Format("ui.overview.force_value", "{0}/{1}", enemyAlive, enemyTotal),
                        AccentColor = BattleUiTheme.AccentRed,
                    },
                    new HudFactModel
                    {
                        Label = LocalizationService.Text("ui.overview.ready_units.short", "可動"),
                        Value = readyUnits.ToString(),
                        AccentColor = BattleUiTheme.AccentGreen,
                    },
                    new HudFactModel
                    {
                        Label = LocalizationService.Text("ui.overview.skill_ready.short", "技能就緒"),
                        Value = skillReadyUnits.ToString(),
                        AccentColor = BattleUiTheme.AccentTeal,
                    },
                },
                ObjectiveFacts = new List<HudFactModel>
                {
                    new HudFactModel
                    {
                        Label = LocalizationService.Text("ui.objective.header", "目標"),
                        Value = primaryObjective,
                        AccentColor = BattleUiTheme.AccentGold,
                    },
                    new HudFactModel
                    {
                        Label = LocalizationService.Text("ui.objective.failure_short", "失敗"),
                        Value = failureObjective,
                        AccentColor = BattleUiTheme.AccentRed,
                    },
                },
            };
        }

        public BattleSelectedUnitModel BuildSelectedUnitModel(BattleSimulation simulation, UnitRuntimeState selected, BattleThreatProjection threatProjection)
        {
            return selectionModelBuilder.BuildSelectedUnitModel(simulation, selected, threatProjection);
        }

        public IReadOnlyList<BattleRosterEntryModel> BuildRosterEntries(BattleSimulation simulation, UnitFaction faction, string selectedUnitId, BattleThreatProjection selectedProjection)
        {
            return rosterModelBuilder.BuildRosterEntries(simulation, faction, selectedUnitId, selectedProjection);
        }

        public BattleActionMenuModel BuildActionMenuModel(BattleSimulation simulation, UnitRuntimeState selected, bool moved)
        {
            return actionMenuModelBuilder.BuildActionMenuModel(simulation, selected, moved);
        }

        public BattleForecastModel BuildNeutralForecastModel(BattleOverviewModel overview, IReadOnlyList<string> feedEntries)
        {
            return forecastModelBuilder.BuildNeutralForecastModel(overview, feedEntries);
        }

        public BattleForecastModel BuildIntentForecastModel(BattleSimulation simulation, BattleIntentPreview preview)
        {
            return forecastModelBuilder.BuildIntentForecastModel(simulation, preview);
        }

        public BattleForecastModel BuildCombatResultForecastModel(BattleSimulation simulation, CombatResult combatResult)
        {
            return forecastModelBuilder.BuildCombatResultForecastModel(simulation, combatResult);
        }

        public BattleForecastModel BuildSkillResultForecastModel(BattleSimulation simulation, SkillResult skillResult)
        {
            return forecastModelBuilder.BuildSkillResultForecastModel(simulation, skillResult);
        }

        public string BuildCombatLog(BattleSimulation simulation, CombatResult combatResult)
        {
            return resultModelBuilder.BuildCombatLog(simulation, combatResult);
        }

        public string BuildSkillLog(BattleSimulation simulation, SkillResult skillResult)
        {
            return resultModelBuilder.BuildSkillLog(simulation, skillResult);
        }

        public BattleResultModel BuildBattleResultModel(
            BattleSimulation simulation,
            BattleScenarioData scenarioData,
            ScenarioDirector scenarioDirector,
            string title,
            IReadOnlyList<string> rewardLines,
            IReadOnlyList<string> unitLines)
        {
            return resultModelBuilder.BuildBattleResultModel(simulation, scenarioData, scenarioDirector, title, rewardLines, unitLines);
        }
    }

    public sealed class BattleSelectionModelBuilder
    {
        private readonly BattleHudModelSupport support;

        public BattleSelectionModelBuilder(BattleHudModelSupport support = null)
        {
            this.support = support ?? new BattleHudModelSupport();
        }

        public BattleSelectedUnitModel BuildSelectedUnitModel(BattleSimulation simulation, UnitRuntimeState selected, BattleThreatProjection threatProjection)
        {
            if (simulation == null || selected == null)
            {
                return new BattleSelectedUnitModel();
            }

            RoleLoadoutProfile loadoutProfile = RoleLoadoutCatalog.GetProfile(selected.Role);
            ItemDefinition weapon = ItemCatalog.Get(selected.EquipmentLoadout.WeaponId);
            ItemDefinition armor = ItemCatalog.Get(selected.EquipmentLoadout.ArmorId);
            ItemDefinition mount = ItemCatalog.Get(selected.EquipmentLoadout.MountId);
            TerrainType terrainType = simulation.Context.GetTerrainAt(selected.Position);
            int attackValue = selected.Attack +
                              PassiveSkillRules.GetPersonalAttackBonus(selected) +
                              PassiveSkillRules.GetAttackBonus(simulation.Context, selected) +
                              SupportRules.GetAttackBonus(simulation.Context, selected, selected.Position) +
                              StatusEffectRules.GetAttackModifier(selected);
            int defenseValue = selected.Defense +
                               PassiveSkillRules.GetDefenseBonus(selected) +
                               SupportRules.GetDefenseBonus(simulation.Context, selected) +
                               TerrainRules.GetDefenseBonus(terrainType) +
                               StatusEffectRules.GetDefenseModifier(selected);
            int moveRange = support.GetCurrentMoveRange(simulation.Context, selected);
            int attackRange = PassiveSkillRules.GetAttackRange(selected);
            string skillStatusText = support.BuildSkillAvailabilityLabel(simulation, selected);
            string terrainName = support.GetTerrainDisplayName(terrainType);
            string terrainEffectSummary = support.BuildTerrainEffectSummary(simulation.Context, selected, terrainType);
            string mountSummary = mount != null
                ? LocalizationService.Format("ui.label.mount_value", "Mount: {0}", LocalizationService.Text(mount.NameKey, mount.NameFallback))
                : string.Empty;
            string mountDeltaLabel = mount != null && mount.MoveBonus > 0
                ? LocalizationService.Format("camp.item.move", "MOVE +{0}", mount.MoveBonus)
                : string.Empty;
            string projectedRisk = support.BuildThreatRiskLabel(threatProjection, selected.CurrentHp);
            string threatSummaryText = support.BuildThreatSummary(threatProjection, selected.CurrentHp);
            string threatDetailText = support.BuildThreatDetailLine(simulation, threatProjection);
            string statusSummary = support.BuildStatusSummary(selected);
            string weaponName = weapon != null
                ? LocalizationService.Text(weapon.NameKey, weapon.NameFallback)
                : LocalizationService.Text(loadoutProfile.WeaponNameKey, loadoutProfile.WeaponNameFallback);
            string weaponDescription = weapon != null
                ? LocalizationService.Text(weapon.DescriptionKey, weapon.DescriptionFallback)
                : LocalizationService.Text(loadoutProfile.WeaponDescriptionKey, loadoutProfile.WeaponDescriptionFallback);
            string equipmentSummary = string.Join(
                "\n",
                new[]
                {
                    LocalizationService.Format("ui.label.weapon_value", "Weapon: {0}", weaponName),
                    support.BuildEquipmentSummary(armor, mount),
                }.Where(line => !string.IsNullOrWhiteSpace(line)));

            List<HudChipModel> identityChips = new List<HudChipModel>
            {
                support.CreateStateChip(
                    selected.HasActed
                        ? LocalizationService.Text("ui.status.done", "DONE")
                        : LocalizationService.Text("ui.status.ready", "READY"),
                    !selected.HasActed,
                    false),
                support.CreateInfoChip(terrainName),
            };
            if (!string.IsNullOrWhiteSpace(projectedRisk))
            {
                identityChips.Add(support.BuildThreatChip(threatProjection, selected.CurrentHp));
            }

            List<HudFactModel> vitalFacts = new List<HudFactModel>
            {
                new HudFactModel
                {
                    Label = LocalizationService.Text("ui.label.hp_short", "生命"),
                    Value = LocalizationService.Format("ui.value.current_max", "{0}/{1}", selected.CurrentHp, selected.MaxHp),
                    AccentColor = new Color(0.39f, 0.81f, 0.42f, 1f),
                },
                new HudFactModel
                {
                    Label = LocalizationService.Text("ui.label.mana_short", "士氣"),
                    Value = LocalizationService.Format("ui.value.current_max", "{0}/{1}", selected.CurrentMana, selected.MaxMana),
                    AccentColor = new Color(0.38f, 0.78f, 0.95f, 1f),
                },
            };
            List<HudFactModel> combatFacts = new List<HudFactModel>
            {
                new HudFactModel
                {
                    Label = LocalizationService.Text("camp.item.attack", "攻擊"),
                    Value = attackValue.ToString(),
                    AccentColor = BattleUiTheme.AccentGold,
                },
                new HudFactModel
                {
                    Label = LocalizationService.Text("camp.item.defense", "防禦"),
                    Value = defenseValue.ToString(),
                    AccentColor = BattleUiTheme.AccentBlue,
                },
                new HudFactModel
                {
                    Label = LocalizationService.Text("camp.item.move", "移動"),
                    Value = moveRange.ToString(),
                    AccentColor = BattleUiTheme.AccentGreen,
                },
                new HudFactModel
                {
                    Label = LocalizationService.Text("ui.label.range_short", "射程"),
                    Value = attackRange.ToString(),
                    AccentColor = BattleUiTheme.AccentTeal,
                },
            };

            List<HudChipModel> primaryChips = new List<HudChipModel>
            {
                identityChips[0],
                support.CreateStateChip(
                    selected.CanUseSkill && !selected.HasActed && selected.ActiveSkill != ActiveSkillType.None
                        ? (support.IsSkillReady(simulation, selected)
                            ? LocalizationService.Text("ui.roster.skill_ready", "SKILL READY")
                            : skillStatusText)
                        : skillStatusText,
                    support.IsSkillReady(simulation, selected),
                    false),
            };
            if (!string.IsNullOrWhiteSpace(mountDeltaLabel))
            {
                primaryChips.Add(support.CreateInfoChip(mountDeltaLabel));
            }

            if (!string.IsNullOrWhiteSpace(statusSummary) &&
                !string.Equals(statusSummary, LocalizationService.Text("ui.status.none", "None"), StringComparison.OrdinalIgnoreCase))
            {
                primaryChips.Add(support.CreateWarningChip(statusSummary));
            }

            List<string> detailLines = new List<string>();
            if (!string.IsNullOrWhiteSpace(terrainEffectSummary))
            {
                detailLines.Add(LocalizationService.Format("ui.selected.detail.terrain", "Terrain: {0}", terrainEffectSummary));
            }

            detailLines.Add(LocalizationService.Format("ui.selected.detail.weapon", "{0}: {1}", weaponName, weaponDescription));
            if (!string.IsNullOrWhiteSpace(equipmentSummary))
            {
                detailLines.Add(equipmentSummary);
            }

            detailLines.Add(LocalizationService.Format(
                "ui.selected.detail.passive",
                "Passive | {0}: {1}",
                LocalizationService.Text(selected.PassiveSkillNameKey, selected.PassiveSkill.ToString()),
                LocalizationService.Text(selected.PassiveSkillDescriptionKey, selected.PassiveSkill.ToString())));
            detailLines.Add(LocalizationService.Format(
                "ui.selected.detail.active",
                "Active | {0}: {1}",
                support.BuildMasteryTaggedSkillName(LocalizationService.Text(selected.ActiveSkillNameKey, selected.ActiveSkill.ToString()), selected),
                support.BuildSkillPanelDescription(simulation, selected)));
            if (!string.IsNullOrWhiteSpace(threatDetailText))
            {
                detailLines.Add(threatDetailText);
            }

            HudChipModel threatChip = support.BuildThreatChip(threatProjection, selected.CurrentHp);
            string threatLine = string.Join(
                "\n",
                new[] { threatSummaryText, threatDetailText }
                    .Where(line => !string.IsNullOrWhiteSpace(line)));

            return new BattleSelectedUnitModel
            {
                HasSelection = true,
                UnitId = selected.Id,
                DisplayName = support.GetUnitDisplayName(simulation, selected.Id),
                Role = selected.Role,
                RoleLabel = LocalizationService.Format(
                    "ui.selected.role_level",
                    "{0}  Lv {1}",
                    LocalizationService.Text(UnitClassCatalog.Get(selected.ClassId).DisplayNameKey, LocalizationService.Text(selected.RoleNameKey, selected.Role.ToString())),
                    selected.Level),
                PositionLabel = support.BuildRosterPositionLabel(simulation.Context, selected),
                TerrainName = terrainName,
                TerrainEffectSummary = terrainEffectSummary,
                Faction = selected.Faction,
                CurrentHp = selected.CurrentHp,
                MaxHp = selected.MaxHp,
                CurrentMana = selected.CurrentMana,
                MaxMana = selected.MaxMana,
                Level = selected.Level,
                CurrentExp = selected.CurrentExp,
                NextLevelExp = selected.NextLevelExp,
                Attack = attackValue,
                Defense = defenseValue,
                MoveRange = moveRange,
                AttackRange = attackRange,
                WeaponTypeLabel = LocalizationService.Text(loadoutProfile.WeaponTypeKey, loadoutProfile.WeaponTypeFallback),
                WeaponName = weaponName,
                WeaponDescription = weaponDescription,
                ArmorSummary = support.BuildEquipmentSummary(armor, mount),
                WeaponAccentColor = loadoutProfile.AccentColor,
                PassiveName = LocalizationService.Text(selected.PassiveSkillNameKey, selected.PassiveSkill.ToString()),
                PassiveDescription = LocalizationService.Text(selected.PassiveSkillDescriptionKey, selected.PassiveSkill.ToString()),
                ActiveName = support.BuildMasteryTaggedSkillName(LocalizationService.Text(selected.ActiveSkillNameKey, selected.ActiveSkill.ToString()), selected),
                ActiveDescription = support.BuildSkillPanelDescription(simulation, selected),
                CooldownLabel = skillStatusText,
                StatusSummary = LocalizationService.Format("ui.label.status_value", "Status: {0}", statusSummary),
                ActionSummary = LocalizationService.Format(
                    "ui.label.action_state",
                    "Action state: {0}",
                    selected.HasActed
                        ? LocalizationService.Text("ui.status.done", "DONE")
                        : LocalizationService.Text("ui.status.ready", "READY")),
                ThreatSummary = threatSummaryText,
                ThreatDetail = threatDetailText,
                ProjectedRiskLabel = projectedRisk,
                EquipmentSummary = equipmentSummary,
                MountSummary = mountSummary,
                MountDeltaLabel = mountDeltaLabel,
                ThreatProjection = threatProjection,
                IdentityChips = identityChips,
                VitalFacts = vitalFacts,
                CombatFacts = combatFacts,
                StatusPills = primaryChips,
                IdentityFacts = new List<HudFactModel>
                {
                    new HudFactModel
                    {
                        Label = LocalizationService.Text("ui.selected.identity.class", "Class"),
                        Value = LocalizationService.Text(selected.RoleNameKey, selected.Role.ToString()),
                        AccentColor = BattleUiTheme.AccentGold,
                    },
                    new HudFactModel
                    {
                        Label = LocalizationService.Text("ui.selected.identity.position", "Position"),
                        Value = support.BuildRosterPositionLabel(simulation.Context, selected),
                        AccentColor = BattleUiTheme.AccentSlate,
                    },
                },
                PrimaryFacts = combatFacts,
                PrimaryChips = primaryChips,
                ThreatChip = threatChip,
                ThreatLine = threatLine,
                DetailHeader = LocalizationService.Text("ui.selected.details_header", "武裝與技能"),
                DetailLines = detailLines.Where(line => !string.IsNullOrWhiteSpace(line)).ToList(),
            };
        }
    }

    public sealed class BattleRosterModelBuilder
    {
        private readonly BattleHudModelSupport support;

        public BattleRosterModelBuilder(BattleHudModelSupport support = null)
        {
            this.support = support ?? new BattleHudModelSupport();
        }

        public IReadOnlyList<BattleRosterEntryModel> BuildRosterEntries(BattleSimulation simulation, UnitFaction faction, string selectedUnitId, BattleThreatProjection selectedProjection)
        {
            if (simulation == null)
            {
                return new List<BattleRosterEntryModel>();
            }

            HashSet<string> threateningIds = faction == UnitFaction.Enemy && selectedProjection != null
                ? new HashSet<string>(selectedProjection.ThreateningEnemyIds)
                : new HashSet<string>();

            IEnumerable<BattleRosterEntryModel> models = simulation.Context.Units
                .Where(unit => unit.Faction == faction)
                .Select(unit =>
                {
                    BattleThreatProjection unitProjection = unit.IsAlive
                        ? BattleThreatAnalyzer.AnalyzeProjected(simulation.Context, unit, unit.Position)
                        : new BattleThreatProjection(0, 0, new List<string>());
                    bool lowHp = unit.MaxHp > 0 && (float)unit.CurrentHp / unit.MaxHp <= 0.35f;
                    bool skillReady = support.IsSkillReady(simulation, unit);
                    bool exposed = unitProjection.IsLethalRisk(unit.CurrentHp) || unitProjection.ThreateningEnemyCount >= 2;
                    bool threateningSelection = threateningIds.Contains(unit.Id);
                    IReadOnlyList<BattleRosterTag> tags = support.BuildRosterTags(unit, lowHp, exposed, skillReady, threateningSelection);
                    BattleRosterTag? primaryTag = tags.Count > 0 ? tags[0] : (BattleRosterTag?)null;
                    BattleRosterTag? secondaryTag = tags.Count > 1 ? tags[1] : null;
                    return new BattleRosterEntryModel
                    {
                        UnitId = unit.Id,
                        DisplayName = support.GetUnitDisplayName(simulation, unit.Id),
                        RoleShortLabel = support.GetRoleShortLabel(unit.Role),
                        PositionLabel = support.BuildRosterPositionLabel(simulation.Context, unit),
                        StatusLabel = primaryTag.HasValue ? support.GetRosterTagLabel(primaryTag.Value) : string.Empty,
                        SkillLabel = secondaryTag.HasValue ? support.GetRosterTagLabel(secondaryTag.Value) : string.Empty,
                        Faction = unit.Faction,
                        CurrentHp = unit.CurrentHp,
                        MaxHp = unit.MaxHp,
                        IsAlive = unit.IsAlive,
                        HasActed = unit.HasActed,
                        CanUseSkill = unit.CanUseSkill,
                        IsSelected = unit.Id == selectedUnitId,
                        IsThreateningSelection = threateningSelection,
                        IsLowHp = lowHp,
                        IsSkillReady = skillReady,
                        IsExposed = exposed,
                        Tags = tags,
                        PrimaryTag = primaryTag,
                        SecondaryTag = secondaryTag,
                    };
                });

            return faction == UnitFaction.Player
                ? models
                    .OrderBy(model => GetPlayerSortRank(model))
                    .ThenBy(model => model.PositionLabel, StringComparer.Ordinal)
                    .ThenBy(model => model.DisplayName, StringComparer.Ordinal)
                    .ToList()
                : models
                    .OrderBy(model => GetEnemySortRank(model))
                    .ThenBy(model => model.PositionLabel, StringComparer.Ordinal)
                    .ThenBy(model => model.DisplayName, StringComparer.Ordinal)
                    .ToList();
        }

        private static int GetPlayerSortRank(BattleRosterEntryModel model)
        {
            if (model.IsSelected)
            {
                return 0;
            }

            if (model.IsExposed)
            {
                return 1;
            }

            if (model.IsAlive && !model.HasActed)
            {
                return 2;
            }

            if (model.IsAlive)
            {
                return 3;
            }

            return 4;
        }

        private static int GetEnemySortRank(BattleRosterEntryModel model)
        {
            if (model.IsThreateningSelection)
            {
                return 0;
            }

            if (model.IsExposed)
            {
                return 1;
            }

            if (model.IsAlive)
            {
                return 2;
            }

            return 3;
        }
    }

    public sealed class BattleActionMenuModelBuilder
    {
        private readonly BattleHudModelSupport support;

        public BattleActionMenuModelBuilder(BattleHudModelSupport support = null)
        {
            this.support = support ?? new BattleHudModelSupport();
        }

        public BattleActionMenuModel BuildActionMenuModel(BattleSimulation simulation, UnitRuntimeState selected, bool moved)
        {
            if (simulation == null || selected == null)
            {
                return new BattleActionMenuModel();
            }

            BattleIntentPreview bestAttackPreview = support.SelectBestIntentPreview(
                simulation.GetAttackableTargets(selected.Id)
                    .Select(target => simulation.PreviewAttackIntent(selected.Id, target.Id)));
            BattleIntentPreview bestSkillPreview = support.SelectBestIntentPreview(
                simulation.GetSkillTargets(selected.Id)
                    .Select(target => simulation.PreviewSkillIntent(selected.Id, target.Id)));
            BattleThreatProjection currentThreat = BattleThreatAnalyzer.AnalyzeProjected(simulation.Context, selected, selected.Position);

            List<BattleActionDescriptor> actions = new List<BattleActionDescriptor>
            {
                BuildAttackDescriptor(selected, bestAttackPreview, currentThreat),
                BuildSkillDescriptor(simulation, selected, bestSkillPreview, currentThreat),
                BuildWaitDescriptor(selected, currentThreat),
                BuildBackDescriptor(moved),
            };

            return new BattleActionMenuModel
            {
                Mode = moved ? BattleActionMenuMode.AfterMove : BattleActionMenuMode.HoldPosition,
                ModeLabel = LocalizationService.Text(
                    moved ? "ui.action_menu.mode.moved" : "ui.action_menu.mode.hold",
                    moved ? "移動後指令" : "原地指令"),
                ContextHint = actions.Any(action => action.Priority == BattleActionDescriptorPriority.Primary && action.IsEnabled)
                    ? LocalizationService.Text("ui.action_menu.context.ready", "先看上方戰術摘要，再決定最乾淨的一步。")
                    : LocalizationService.Text("ui.action_menu.context.setup", "目前沒有有效目標，先待命或撤回移動調整站位。"),
                Actions = actions,
            };
        }

        private BattleActionDescriptor BuildAttackDescriptor(UnitRuntimeState selected, BattleIntentPreview preview, BattleThreatProjection fallbackThreat)
        {
            BattleThreatProjection threat = preview != null ? preview.ThreatAfterAction : fallbackThreat;
            string rangeText = LocalizationService.Format("ui.label.range_value", "射程 {0}", PassiveSkillRules.GetAttackRange(selected));
            string areaText = LocalizationService.Text("ui.skill.impact.single_enemy", "單體");
            string outcome = preview == null
                ? LocalizationService.Text("ui.action_menu.attack.unavailable", "目前無可攻擊目標")
                : preview.LethalTargetIds.Count > 0
                    ? LocalizationService.Text("ui.action_menu.attack.outcome_ko", "可直接擊破目標")
                    : LocalizationService.Format("ui.action_menu.attack.outcome_damage", "預計造成 {0} 點傷害", preview.PredictedDamage);
            if (preview != null && preview.PredictedStatuses.Count > 0)
            {
                outcome = string.Join(
                    " ",
                    outcome,
                    LocalizationService.Format("ui.label.status_value", "狀態：{0}", string.Join(", ", preview.PredictedStatuses.Select(status => support.GetStatusDisplayName(status.Type)))));
            }

            return new BattleActionDescriptor
            {
                Type = BattleActionDescriptorType.Attack,
                Label = LocalizationService.Text("ui.button.attack", "攻擊"),
                IsEnabled = preview != null && preview.CanCommit,
                Reason = preview != null && preview.CanCommit
                    ? LocalizationService.Text("ui.action_menu.attack.ready", "目標在攻擊範圍內")
                    : LocalizationService.Text("ui.action_menu.attack.unavailable", "目前無可攻擊目標"),
                Range = rangeText,
                Area = areaText,
                BestDamage = preview != null ? preview.PredictedDamage : 0,
                PredictedStatuses = preview != null
                    ? preview.PredictedStatuses.Select(status => support.GetStatusDisplayName(status.Type)).ToList()
                    : new List<string>(),
                ThreatAfterAction = support.BuildThreatSummary(threat, selected.CurrentHp),
                Lethal = preview != null && preview.LethalTargetIds.Count > 0,
                MetricChips = BuildMetricChips(rangeText, areaText, 0, preview != null ? preview.PredictedDamage : 0, preview != null ? preview.PredictedHealing : 0, preview != null ? preview.PredictedStatuses.Count : 0, preview != null && preview.LethalTargetIds.Count > 0),
                OutcomeLine = outcome,
                RiskChip = support.BuildThreatChip(threat, selected.CurrentHp),
                Priority = BattleActionDescriptorPriority.Primary,
            };
        }

        private BattleActionDescriptor BuildSkillDescriptor(BattleSimulation simulation, UnitRuntimeState selected, BattleIntentPreview preview, BattleThreatProjection fallbackThreat)
        {
            BattleThreatProjection threat = preview != null ? preview.ThreatAfterAction : fallbackThreat;
            int manaCost = selected.ActiveSkill == ActiveSkillType.None ? 0 : ActiveSkillRules.GetManaCost(selected.ActiveSkill);
            string rangeText = selected.ActiveSkill == ActiveSkillType.None ? string.Empty : LocalizationService.Format("ui.label.range_value", "射程 {0}", ActiveSkillRules.GetRange(selected));
            string areaText = selected.ActiveSkill == ActiveSkillType.None ? string.Empty : support.BuildSkillAreaLabel(selected.ActiveSkill);
            string outcome = preview == null
                ? support.BuildSkillAvailabilityLabel(simulation, selected)
                : support.BuildSkillOutcomeSummary(preview);
            if (preview != null && preview.PredictedStatuses.Count > 0)
            {
                outcome = string.Join(
                    " | ",
                    new[]
                    {
                        outcome,
                        LocalizationService.Format("ui.label.status_value", "狀態：{0}", string.Join(", ", preview.PredictedStatuses.Select(status => support.GetStatusDisplayName(status.Type)))),
                    }.Where(line => !string.IsNullOrWhiteSpace(line)));
            }

            return new BattleActionDescriptor
            {
                Type = BattleActionDescriptorType.Skill,
                Label = LocalizationService.Text(selected.ActiveSkillNameKey, selected.ActiveSkill.ToString()),
                IsEnabled = preview != null && preview.CanCommit,
                Reason = support.BuildSkillAvailabilityLabel(simulation, selected),
                ManaCost = manaCost,
                Range = rangeText,
                Area = areaText,
                BestDamage = preview != null ? preview.PredictedDamage : 0,
                PredictedStatuses = preview != null
                    ? preview.PredictedStatuses.Select(status => support.GetStatusDisplayName(status.Type)).ToList()
                    : new List<string>(),
                ThreatAfterAction = support.BuildThreatSummary(threat, selected.CurrentHp),
                Lethal = preview != null && preview.LethalTargetIds.Count > 0,
                MetricChips = BuildMetricChips(rangeText, areaText, manaCost, preview != null ? preview.PredictedDamage : 0, preview != null ? preview.PredictedHealing : 0, preview != null ? preview.PredictedStatuses.Count : 0, preview != null && preview.LethalTargetIds.Count > 0),
                OutcomeLine = outcome,
                RiskChip = support.BuildThreatChip(threat, selected.CurrentHp),
                Priority = BattleActionDescriptorPriority.Primary,
            };
        }

        private BattleActionDescriptor BuildWaitDescriptor(UnitRuntimeState selected, BattleThreatProjection threat)
        {
            return new BattleActionDescriptor
            {
                Type = BattleActionDescriptorType.Wait,
                Label = LocalizationService.Text("ui.button.wait", "待命"),
                IsEnabled = true,
                Reason = LocalizationService.Text("ui.action_menu.wait.detail", "結束此單位本回合行動"),
                ThreatAfterAction = support.BuildThreatSummary(threat, selected.CurrentHp),
                MetricChips = new List<HudChipModel>
                {
                    support.CreateInfoChip(LocalizationService.Text("ui.action_menu.wait.hold", "保持站位")),
                },
                OutcomeLine = LocalizationService.Text("ui.action_menu.wait.outcome", "結束此單位回合並維持目前站位"),
                RiskChip = support.BuildThreatChip(threat, selected.CurrentHp),
                Priority = BattleActionDescriptorPriority.Secondary,
            };
        }

        private BattleActionDescriptor BuildBackDescriptor(bool moved)
        {
            return new BattleActionDescriptor
            {
                Type = BattleActionDescriptorType.Back,
                Label = moved
                    ? LocalizationService.Text("ui.button.undo_move", "返回移動")
                    : LocalizationService.Text("ui.button.back", "返回"),
                IsEnabled = moved,
                Reason = moved
                    ? LocalizationService.Text("ui.action_menu.back.ready", "回到原本所在格")
                    : LocalizationService.Text("ui.action_menu.back.unavailable", "目前沒有可撤回的移動"),
                MetricChips = moved
                    ? new List<HudChipModel> { support.CreateInfoChip(LocalizationService.Text("ui.action_menu.back.metric", "撤回本次移動")) }
                    : new List<HudChipModel>(),
                OutcomeLine = moved
                    ? LocalizationService.Text("ui.action_menu.back.outcome", "回到原本位置，重新選擇移動路線")
                    : LocalizationService.Text("ui.action_menu.back.unavailable", "目前沒有可撤回的移動"),
                Priority = BattleActionDescriptorPriority.Secondary,
            };
        }

        private IReadOnlyList<HudChipModel> BuildMetricChips(string rangeText, string areaText, int manaCost, int damage, int healing, int statusCount, bool lethal)
        {
            List<HudChipModel> chips = new List<HudChipModel>();
            if (manaCost > 0)
            {
                chips.Add(support.CreateInfoChip(LocalizationService.Format("ui.skill.mana_cost", "消耗 {0} 士氣", manaCost)));
            }

            if (!string.IsNullOrWhiteSpace(rangeText))
            {
                chips.Add(support.CreateInfoChip(rangeText));
            }

            if (!string.IsNullOrWhiteSpace(areaText))
            {
                chips.Add(support.CreateInfoChip(areaText));
            }

            if (damage > 0)
            {
                chips.Add(support.CreateInfoChip(LocalizationService.Format("ui.attack.best_damage", "Best damage {0}", damage)));
            }
            else if (healing > 0)
            {
                chips.Add(support.CreateInfoChip(LocalizationService.Format("ui.skill.detail.heal", "+{0} HP", healing)));
            }

            if (statusCount > 0)
            {
                chips.Add(support.CreateInfoChip(LocalizationService.Format("ui.skill.detail.status_count", "{0} status effects", statusCount)));
            }

            if (lethal)
            {
                chips.Add(support.CreateWarningChip(LocalizationService.Text("ui.forecast.attack.ko_short", "KO")));
            }

            return chips;
        }
    }

    public sealed class BattleForecastModelBuilder
    {
        private readonly BattleHudModelSupport support;

        public BattleForecastModelBuilder(BattleHudModelSupport support = null)
        {
            this.support = support ?? new BattleHudModelSupport();
        }

        public BattleForecastModel BuildNeutralForecastModel(BattleOverviewModel overview, IReadOnlyList<string> feedEntries)
        {
            string latestFeed = feedEntries != null && feedEntries.Count > 0
                ? feedEntries[0]
                : LocalizationService.Text("ui.feed.empty", "目前還沒有新的戰場紀錄。");
            string objective = overview != null ? overview.ObjectivePrimary : string.Empty;
            string instruction = overview != null ? overview.InstructionText : string.Empty;
            BattleForecastModel model = new BattleForecastModel
            {
                Mode = BattleForecastMode.Neutral,
                Header = !string.IsNullOrWhiteSpace(overview != null ? overview.StageLabel : string.Empty)
                    ? overview.StageLabel
                    : LocalizationService.Text("ui.panel.forecast", "戰術摘要"),
                Title = !string.IsNullOrWhiteSpace(overview != null ? overview.PhaseLabel : string.Empty)
                    ? overview.PhaseLabel
                    : LocalizationService.Text("ui.forecast.neutral.title", "等待指令"),
                OutcomeFacts = new List<HudFactModel>
                {
                    new HudFactModel
                    {
                        Label = LocalizationService.Text("ui.turn.count.short", "回合"),
                        Value = overview != null ? overview.TurnLabel : string.Empty,
                        AccentColor = BattleUiTheme.AccentGold,
                    },
                    new HudFactModel
                    {
                        Label = LocalizationService.Text("ui.overview.ready_units.short", "可動"),
                        Value = overview != null ? overview.ReadyLabel : string.Empty,
                        AccentColor = BattleUiTheme.AccentGreen,
                    },
                }.Where(fact => !string.IsNullOrWhiteSpace(fact.Value)).Take(3).ToList(),
                PrimaryLine = !string.IsNullOrWhiteSpace(objective) ? objective : latestFeed,
                SecondaryLines = new[]
                {
                    latestFeed,
                    instruction,
                }.Where(line => !string.IsNullOrWhiteSpace(line)).Distinct().Take(2).ToList(),
                AccentColor = new Color(0.78f, 0.62f, 0.28f, 1f),
            };

            return FinalizeModel(model);
        }

        public BattleForecastModel BuildIntentForecastModel(BattleSimulation simulation, BattleIntentPreview preview)
        {
            if (simulation == null || preview == null)
            {
                return new BattleForecastModel();
            }

            UnitRuntimeState actor = simulation.Context.GetUnit(preview.ActorUnitId);
            string actorName = support.GetUnitDisplayName(simulation, preview.ActorUnitId);
            string targetName = string.IsNullOrWhiteSpace(preview.PrimaryTargetId) ? string.Empty : support.GetUnitDisplayName(simulation, preview.PrimaryTargetId);
            switch (preview.ActionKind)
            {
                case BattleIntentActionKind.Move:
                    return FinalizeModel(BuildMovePreviewModel(simulation, preview, actor, actorName));
                case BattleIntentActionKind.Attack:
                case BattleIntentActionKind.QuickAttack:
                    return FinalizeModel(BuildAttackPreviewModel(simulation, preview, actor, actorName, targetName));
                case BattleIntentActionKind.Skill:
                    return FinalizeModel(BuildSkillPreviewModel(simulation, preview, actor, targetName));
                default:
                    return new BattleForecastModel();
            }
        }

        public BattleForecastModel BuildCombatResultForecastModel(BattleSimulation simulation, CombatResult combatResult)
        {
            if (simulation == null || combatResult == null)
            {
                return new BattleForecastModel();
            }

            BattleForecastModel model = new BattleForecastModel
            {
                Mode = BattleForecastMode.ResultConfirm,
                Header = LocalizationService.Text("ui.forecast.result.header", "Battle Result"),
                Title = LocalizationService.Format("ui.combat.banner", "{0} strikes {1}", support.GetUnitDisplayName(simulation, combatResult.AttackerUnitId), support.GetUnitDisplayName(simulation, combatResult.DefenderUnitId)),
                OutcomeFacts = new List<HudFactModel>
                {
                    new HudFactModel
                    {
                        Label = LocalizationService.Text("ui.forecast.fact.damage", "傷害"),
                        Value = combatResult.Damage.ToString(),
                        AccentColor = BattleUiTheme.AccentRed,
                    },
                    new HudFactModel
                    {
                        Label = LocalizationService.Text("ui.forecast.fact.outcome", "結果"),
                        Value = combatResult.DefenderDied
                            ? LocalizationService.Text("ui.forecast.attack.ko_short", "KO")
                            : LocalizationService.Format("ui.forecast.result.remaining", "{0} HP", combatResult.DefenderRemainingHp),
                        AccentColor = combatResult.DefenderDied ? BattleUiTheme.AccentGold : BattleUiTheme.AccentBlue,
                    },
                    new HudFactModel
                    {
                        Label = LocalizationService.Text("ui.exp.short", "EXP"),
                        Value = combatResult.AttackerExpGained > 0 ? "+" + combatResult.AttackerExpGained : string.Empty,
                        AccentColor = BattleUiTheme.AccentGreen,
                    },
                }.Where(fact => !string.IsNullOrWhiteSpace(fact.Value)).ToList(),
                PrimaryLine = support.BuildCombatLog(simulation, combatResult),
                SecondaryLines = combatResult.AttackerExpGained > 0
                    ? new[] { support.FormatExpGainText(combatResult.AttackerExpGained) }
                    : Array.Empty<string>(),
                AccentColor = new Color(0.96f, 0.42f, 0.26f, 1f),
            };

            return FinalizeModel(model);
        }

        public BattleForecastModel BuildSkillResultForecastModel(BattleSimulation simulation, SkillResult skillResult)
        {
            if (simulation == null || skillResult == null)
            {
                return new BattleForecastModel();
            }

            int totalHealing = skillResult.Effects.Where(effect => effect.IsHealing).Sum(effect => effect.Amount);
            int totalDamage = skillResult.Effects.Where(effect => !effect.IsHealing).Sum(effect => effect.Amount);
            int totalStatuses = skillResult.Effects.Sum(effect => effect.AppliedStatuses.Count(status => status.WasApplied));
            BattleForecastModel model = new BattleForecastModel
            {
                Mode = BattleForecastMode.ResultConfirm,
                Header = LocalizationService.Text("ui.forecast.result.header", "Battle Result"),
                Title = LocalizationService.Format(
                    "ui.skill.banner",
                    "{0} uses {1}",
                    support.GetUnitDisplayName(simulation, skillResult.CasterUnitId),
                    support.GetSkillDisplayName(simulation, skillResult.CasterUnitId, skillResult.SkillType)),
                OutcomeFacts = new List<HudFactModel>
                {
                    new HudFactModel
                    {
                        Label = LocalizationService.Text("ui.forecast.fact.damage", "傷害"),
                        Value = totalDamage > 0 ? totalDamage.ToString() : string.Empty,
                        AccentColor = BattleUiTheme.AccentRed,
                    },
                    new HudFactModel
                    {
                        Label = LocalizationService.Text("ui.forecast.fact.heal", "治療"),
                        Value = totalHealing > 0 ? totalHealing.ToString() : string.Empty,
                        AccentColor = BattleUiTheme.AccentGreen,
                    },
                    new HudFactModel
                    {
                        Label = LocalizationService.Text("ui.forecast.fact.status", "狀態"),
                        Value = totalStatuses > 0 ? totalStatuses.ToString() : string.Empty,
                        AccentColor = BattleUiTheme.AccentTeal,
                    },
                }.Where(fact => !string.IsNullOrWhiteSpace(fact.Value)).ToList(),
                PrimaryLine = support.BuildSkillLog(simulation, skillResult),
                SecondaryLines = skillResult.Effects
                    .Select(effect => support.BuildEffectLine(simulation, effect))
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Take(2)
                    .ToList(),
                AccentColor = RoleLoadoutCatalog.GetSkillAccent(skillResult.SkillType),
            };

            return FinalizeModel(model);
        }

        private BattleForecastModel BuildMovePreviewModel(BattleSimulation simulation, BattleIntentPreview preview, UnitRuntimeState actor, string actorName)
        {
            BattleThreatProjection threat = preview.ThreatAfterAction;
            List<HudFactModel> facts = new List<HudFactModel>
            {
                new HudFactModel
                {
                    Label = LocalizationService.Text("ui.forecast.fact.move", "移動"),
                    Value = preview.MoveCost.ToString(),
                    AccentColor = BattleUiTheme.AccentGold,
                },
                new HudFactModel
                {
                    Label = LocalizationService.Text("ui.forecast.fact.threats", "威脅"),
                    Value = threat != null ? threat.ThreateningEnemyCount.ToString() : "0",
                    AccentColor = BattleUiTheme.AccentRed,
                },
                new HudFactModel
                {
                    Label = LocalizationService.Text("ui.forecast.fact.incoming", "承傷"),
                    Value = threat != null ? threat.MaxProjectedDamage.ToString() : "0",
                    AccentColor = BattleUiTheme.AccentSlate,
                },
            };

            return new BattleForecastModel
            {
                Mode = BattleForecastMode.MovePreview,
                Header = LocalizationService.Text("ui.forecast.move.header", "Move Preview"),
                Title = LocalizationService.Format("ui.forecast.move.title", "{0} -> ({1}, {2})", actorName, preview.Destination.X, preview.Destination.Y),
                OutcomeFacts = facts,
                PrimaryLine = actor != null
                    ? support.BuildTerrainEffectSummary(simulation.Context, actor, simulation.Context.GetTerrainAt(preview.Destination))
                    : string.Empty,
                SecondaryLines = new[]
                {
                    preview.Path.Count > 1 ? "Path: " + string.Join(" -> ", preview.Path.Select(position => $"({position.X},{position.Y})")) : string.Empty,
                    support.BuildThreatDetailLine(simulation, threat),
                }.Where(line => !string.IsNullOrWhiteSpace(line)).Take(2).ToList(),
                RiskChip = support.BuildThreatChip(threat, actor != null ? actor.CurrentHp : 0),
                CommitChip = threat != null && !threat.IsExposed
                    ? support.CreatePositiveChip(LocalizationService.Text("ui.forecast.move.safe_setup", "Low-risk setup"))
                    : support.CreateWarningChip(LocalizationService.Text("ui.forecast.move.attack_ready", "Check threat before commit")),
                AccentColor = new Color(0.9f, 0.78f, 0.36f, 1f),
            };
        }

        private BattleForecastModel BuildAttackPreviewModel(BattleSimulation simulation, BattleIntentPreview preview, UnitRuntimeState actor, string actorName, string targetName)
        {
            BattleIntentEffectPreview attackEffect = preview.Effects.FirstOrDefault();
            List<HudFactModel> facts = new List<HudFactModel>
            {
                new HudFactModel
                {
                    Label = LocalizationService.Text("ui.forecast.fact.damage", "傷害"),
                    Value = (attackEffect != null ? attackEffect.PredictedDamage : preview.PredictedDamage).ToString(),
                    AccentColor = BattleUiTheme.AccentRed,
                },
                new HudFactModel
                {
                    Label = LocalizationService.Text("ui.forecast.fact.outcome", "結果"),
                    Value = attackEffect != null && attackEffect.Lethal
                        ? LocalizationService.Text("ui.forecast.attack.ko_short", "KO")
                        : LocalizationService.Format("ui.forecast.attack.remaining_short", "{0} HP", attackEffect != null ? attackEffect.RemainingHp : 0),
                    AccentColor = attackEffect != null && attackEffect.Lethal ? BattleUiTheme.AccentGold : BattleUiTheme.AccentBlue,
                },
                new HudFactModel
                {
                    Label = LocalizationService.Text("ui.forecast.fact.status", "狀態"),
                    Value = preview.PredictedStatuses.Count > 0 ? preview.PredictedStatuses.Count.ToString() : string.Empty,
                    AccentColor = BattleUiTheme.AccentTeal,
                },
            };

            return new BattleForecastModel
            {
                Mode = BattleForecastMode.ActionPreview,
                Header = preview.ActionKind == BattleIntentActionKind.QuickAttack
                    ? LocalizationService.Text("ui.forecast.quick_attack.header", "Quick Attack Preview")
                    : LocalizationService.Text("ui.forecast.attack.header", "Attack Forecast"),
                Title = LocalizationService.Format("ui.forecast.attack.title", "{0} -> {1}", actorName, targetName),
                OutcomeFacts = facts.Where(fact => !string.IsNullOrWhiteSpace(fact.Value)).ToList(),
                PrimaryLine = string.Join(" | ", new[] { preview.RangeLabel, preview.AreaLabel }.Where(text => !string.IsNullOrWhiteSpace(text))),
                SecondaryLines = new[]
                {
                    preview.PredictedStatuses.Count > 0
                        ? LocalizationService.Format("ui.label.status_value", "Status: {0}", string.Join(", ", preview.PredictedStatuses.Select(status => support.FormatStatusApplication(status))))
                        : string.Empty,
                    support.BuildThreatDetailLine(simulation, preview.ThreatAfterAction),
                }.Where(line => !string.IsNullOrWhiteSpace(line)).Take(2).ToList(),
                RiskChip = support.BuildThreatChip(preview.ThreatAfterAction, actor != null ? actor.CurrentHp : 0),
                CommitChip = attackEffect != null && attackEffect.Lethal
                    ? support.CreatePositiveChip(LocalizationService.Text("ui.forecast.attack.recommendation_ko", "Secure the KO"))
                    : support.CreateWarningChip(LocalizationService.Text("ui.forecast.attack.recommendation_setup", "Damage is guaranteed, exposure remains")),
                AccentColor = actor != null && actor.Faction == UnitFaction.Player ? new Color(0.28f, 0.58f, 0.98f, 1f) : new Color(0.92f, 0.36f, 0.28f, 1f),
            };
        }

        private BattleForecastModel BuildSkillPreviewModel(BattleSimulation simulation, BattleIntentPreview preview, UnitRuntimeState actor, string targetName)
        {
            List<HudFactModel> facts = new List<HudFactModel>
            {
                new HudFactModel
                {
                    Label = LocalizationService.Text("ui.forecast.fact.damage", "傷害"),
                    Value = preview.PredictedDamage > 0 ? preview.PredictedDamage.ToString() : string.Empty,
                    AccentColor = BattleUiTheme.AccentRed,
                },
                new HudFactModel
                {
                    Label = LocalizationService.Text("ui.forecast.fact.heal", "治療"),
                    Value = preview.PredictedHealing > 0 ? preview.PredictedHealing.ToString() : string.Empty,
                    AccentColor = BattleUiTheme.AccentGreen,
                },
                new HudFactModel
                {
                    Label = LocalizationService.Text("ui.skill.mana_short", "MP"),
                    Value = preview.ManaCost > 0 ? preview.ManaCost.ToString() : string.Empty,
                    AccentColor = BattleUiTheme.AccentTeal,
                },
            };

            return new BattleForecastModel
            {
                Mode = BattleForecastMode.ActionPreview,
                Header = LocalizationService.Text("ui.forecast.skill.header", "Skill Forecast"),
                Title = LocalizationService.Format("ui.forecast.skill.title", "{0} -> {1}", support.GetSkillDisplayName(simulation, preview.ActorUnitId, actor != null ? actor.ActiveSkill : ActiveSkillType.None), targetName),
                OutcomeFacts = facts.Where(fact => !string.IsNullOrWhiteSpace(fact.Value)).ToList(),
                PrimaryLine = string.Join(" | ", new[] { preview.RangeLabel, preview.AreaLabel, preview.AffectedTargetIds.Count > 1 ? LocalizationService.Format("ui.skill.detail.multi", "{0} targets hit", preview.AffectedTargetIds.Count) : string.Empty }.Where(text => !string.IsNullOrWhiteSpace(text))),
                SecondaryLines = preview.Effects
                    .Select(effect => support.BuildEffectLine(simulation, effect))
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Take(2)
                    .ToList(),
                RiskChip = support.BuildThreatChip(preview.ThreatAfterAction, actor != null ? actor.CurrentHp : 0),
                CommitChip = preview.LethalTargetIds.Count > 0
                    ? support.CreatePositiveChip(LocalizationService.Text("ui.forecast.skill.recommendation_multi", "This swing is immediate"))
                    : support.CreateWarningChip(LocalizationService.Text("ui.forecast.skill.recommendation_single", "Use this only if the package beats the exposure")),
                AccentColor = actor != null ? RoleLoadoutCatalog.GetSkillAccent(actor.ActiveSkill) : BattleUiTheme.AccentTeal,
            };
        }

        private static BattleForecastModel FinalizeModel(BattleForecastModel model)
        {
            if (model == null)
            {
                return new BattleForecastModel();
            }

            model.OutcomeSummary = model.OutcomeFacts != null && model.OutcomeFacts.Count > 0
                ? string.Join(" | ", model.OutcomeFacts.Select(fact => string.IsNullOrWhiteSpace(fact.Label) ? fact.Value : fact.Label + " " + fact.Value))
                : string.Empty;
            model.PrimaryEffect = model.PrimaryLine;
            model.SecondaryEffects = model.SecondaryLines ?? Array.Empty<string>();
            model.RiskSummary = model.RiskChip != null ? model.RiskChip.Text : string.Empty;
            model.CommitRecommendation = model.CommitChip != null ? model.CommitChip.Text : string.Empty;
            return model;
        }
    }

    public sealed class BattleResultModelBuilder
    {
        private readonly BattleHudModelSupport support;

        public BattleResultModelBuilder(BattleHudModelSupport support = null)
        {
            this.support = support ?? new BattleHudModelSupport();
        }

        public BattleResultModel BuildBattleResultModel(
            BattleSimulation simulation,
            BattleScenarioData scenarioData,
            ScenarioDirector scenarioDirector,
            string title,
            IReadOnlyList<string> rewardLines,
            IReadOnlyList<string> unitLines)
        {
            if (simulation == null || scenarioData == null || simulation.Context == null || !simulation.Context.BattleEnded)
            {
                return new BattleResultModel { Title = title };
            }

            int survivingPlayers = simulation.Context.GetUnits(UnitFaction.Player, false).Count(unit => unit.IsAlive);
            int totalPlayers = simulation.Context.GetUnits(UnitFaction.Player, false).Count;
            string objectiveText = scenarioDirector != null && scenarioDirector.CurrentObjective != null
                ? LocalizationService.Text(scenarioDirector.CurrentObjective.PrimaryObjectiveKey, scenarioDirector.CurrentObjective.PrimaryObjectiveFallback)
                : LocalizationService.Text("ui.objective.none", "Objective complete.");

            return new BattleResultModel
            {
                Title = title,
                Summary = string.Join(
                    "\n",
                    LocalizationService.Format("ui.result.summary.turns", "Rounds fought: {0}", simulation.Context.RoundNumber),
                    LocalizationService.Format("ui.result.summary.survivors", "Allied survivors: {0}/{1}", survivingPlayers, totalPlayers),
                    LocalizationService.Format("ui.result.summary.objective", "Objective: {0}", objectiveText)),
                RewardLines = rewardLines ?? new List<string>(),
                UnitLines = unitLines ?? new List<string>(),
            };
        }

        public string BuildCombatLog(BattleSimulation simulation, CombatResult combatResult)
        {
            string attackerName = support.GetUnitDisplayName(simulation, combatResult.AttackerUnitId);
            string defenderName = support.GetUnitDisplayName(simulation, combatResult.DefenderUnitId);
            return combatResult.DefenderDied
                ? LocalizationService.Format("ui.combat.log.ko", "{0} defeated {1}.", attackerName, defenderName)
                : LocalizationService.Format("ui.combat.log.damage", "{0} dealt {1} damage to {2}.", attackerName, combatResult.Damage, defenderName);
        }

        public string BuildSkillLog(BattleSimulation simulation, SkillResult skillResult)
        {
            return LocalizationService.Format(
                "ui.skill.log.use",
                "{0} used {1}.",
                support.GetUnitDisplayName(simulation, skillResult.CasterUnitId),
                support.GetSkillDisplayName(simulation, skillResult.CasterUnitId, skillResult.SkillType));
        }
    }

    public sealed class BattleHudModelSupport
    {
        public HudChipModel CreateInfoChip(string text)
        {
            return new HudChipModel
            {
                Text = text ?? string.Empty,
                BackgroundColor = BattleUiTheme.ChipInfo,
                TextColor = BattleUiTheme.TextPrimary,
            };
        }

        public HudChipModel CreatePositiveChip(string text)
        {
            return new HudChipModel
            {
                Text = text ?? string.Empty,
                BackgroundColor = BattleUiTheme.ChipPositive,
                TextColor = BattleUiTheme.TextPrimary,
            };
        }

        public HudChipModel CreateWarningChip(string text)
        {
            return new HudChipModel
            {
                Text = text ?? string.Empty,
                BackgroundColor = BattleUiTheme.ChipWarning,
                TextColor = BattleUiTheme.TextPrimary,
            };
        }

        public HudChipModel CreateStateChip(string text, bool positive, bool warning)
        {
            return new HudChipModel
            {
                Text = text ?? string.Empty,
                BackgroundColor = BattleUiTheme.GetChipColor(positive, warning),
                TextColor = BattleUiTheme.TextPrimary,
            };
        }

        public HudChipModel BuildThreatChip(BattleThreatProjection threatProjection, int currentHp)
        {
            string riskLabel = BuildThreatRiskLabel(threatProjection, currentHp);
            if (string.Equals(riskLabel, LocalizationService.Text("ui.threat.safe", "SAFE"), StringComparison.Ordinal))
            {
                return CreatePositiveChip(riskLabel);
            }

            return CreateWarningChip(riskLabel);
        }

        public BattleIntentPreview SelectBestIntentPreview(IEnumerable<BattleIntentPreview> previews)
        {
            return previews?
                .Where(preview => preview != null && preview.CanCommit)
                .OrderByDescending(preview => preview.LethalTargetIds.Count)
                .ThenByDescending(preview => preview.PredictedDamage + preview.PredictedHealing)
                .ThenBy(preview => preview.ThreatAfterAction != null ? preview.ThreatAfterAction.MaxProjectedDamage : int.MaxValue)
                .FirstOrDefault();
        }

        public bool IsSkillReady(BattleSimulation simulation, UnitRuntimeState unit)
        {
            return simulation != null &&
                   unit != null &&
                   unit.CanUseSkill &&
                   !unit.HasActed &&
                   unit.HasEnoughMana(ActiveSkillRules.GetManaCost(unit.ActiveSkill)) &&
                   simulation.GetSkillTargets(unit.Id).Count > 0;
        }

        public string BuildThreatSummary(BattleThreatProjection threatProjection, int currentHp)
        {
            if (threatProjection == null || !threatProjection.IsExposed)
            {
                return LocalizationService.Text("ui.threat.none", "No immediate enemy threat.");
            }

            string baseSummary = LocalizationService.Format(
                "ui.threat.summary",
                "Threats {0} | Max incoming {1}",
                threatProjection.ThreateningEnemyCount,
                threatProjection.MaxProjectedDamage);
            if (threatProjection.IsLethalRisk(currentHp))
            {
                return baseSummary + " | " + LocalizationService.Text("ui.threat.lethal", "Lethal risk");
            }

            return baseSummary;
        }

        public string BuildThreatRiskLabel(BattleThreatProjection threatProjection, int currentHp)
        {
            if (threatProjection == null || !threatProjection.IsExposed)
            {
                return LocalizationService.Text("ui.threat.safe", "SAFE");
            }

            return threatProjection.IsLethalRisk(currentHp)
                ? LocalizationService.Text("ui.threat.lethal_short", "LETHAL")
                : LocalizationService.Text("ui.threat.exposed_short", "EXPOSED");
        }

        public string BuildThreatDetailLine(BattleSimulation simulation, BattleThreatProjection threatProjection)
        {
            if (simulation == null || threatProjection == null || threatProjection.ThreateningEnemyIds.Count == 0)
            {
                return string.Empty;
            }

            return LocalizationService.Format(
                "ui.threat.detail",
                "Threatened by: {0}",
                string.Join(", ", threatProjection.ThreateningEnemyIds.Select(unitId => GetUnitDisplayName(simulation, unitId))));
        }

        public string BuildSkillAvailabilityLabel(BattleSimulation simulation, UnitRuntimeState unit)
        {
            if (unit == null || unit.ActiveSkill == ActiveSkillType.None)
            {
                return LocalizationService.Text("ui.action_menu.skill.none", "No active skill");
            }

            bool hasTargets = simulation != null && simulation.GetSkillTargets(unit.Id).Count > 0;
            if (unit.CurrentMana < ActiveSkillRules.GetManaCost(unit.ActiveSkill))
            {
                return LocalizationService.Format("ui.action_menu.skill.no_mana", "Not enough mana ({0}/{1})", unit.CurrentMana, unit.MaxMana);
            }

            return hasTargets
                ? LocalizationService.Text("ui.action_menu.skill.ready", "Skill ready")
                : LocalizationService.Text("ui.action_menu.skill.unavailable", "No valid target");
        }

        public string BuildSkillPanelDescription(BattleSimulation simulation, UnitRuntimeState unit)
        {
            string description = LocalizationService.Text(unit.ActiveSkillDescriptionKey, unit.ActiveSkill.ToString());
            string masteryFooter = BuildSkillMasteryFooter(unit);
            string availability = BuildSkillAvailabilityLabel(simulation, unit);
            return string.Join("\n", new[] { availability, description, masteryFooter }.Where(part => !string.IsNullOrWhiteSpace(part)));
        }

        public string BuildSkillMasteryFooter(UnitRuntimeState caster)
        {
            string deltaKey = GetSkillMasteryDeltaKey(caster.ActiveSkill);
            string deltaText = LocalizationService.Text(deltaKey, string.Empty);
            return ActiveSkillRules.IsMastered(caster)
                ? LocalizationService.Format("ui.mastery.active", "{0} active", BuildMasteryTag())
                : string.IsNullOrWhiteSpace(deltaText)
                    ? string.Empty
                    : LocalizationService.Format("ui.mastery.future", "Lv10: {0}", deltaText);
        }

        public string BuildMasteryTaggedSkillName(string skillName, UnitRuntimeState unit)
        {
            return ActiveSkillRules.IsMastered(unit)
                ? skillName + "  " + BuildMasteryTag()
                : skillName;
        }

        public string BuildSkillAreaLabel(ActiveSkillType skillType)
        {
            switch (skillType)
            {
                case ActiveSkillType.RoyalAid:
                    return LocalizationService.Text("ui.skill.impact.single_ally", "1 ally");
                case ActiveSkillType.ImperialAid:
                case ActiveSkillType.GuardOrder:
                    return LocalizationService.Text("ui.skill.impact.ally_adjacent", "1 ally + adjacent");
                case ActiveSkillType.PowerStrike:
                case ActiveSkillType.PinningShot:
                case ActiveSkillType.DragonPierce:
                    return LocalizationService.Text("ui.skill.impact.single_enemy", "1 foe");
                case ActiveSkillType.Volley:
                case ActiveSkillType.SkyVolley:
                case ActiveSkillType.FireStratagem:
                case ActiveSkillType.EightTrigramInferno:
                    return LocalizationService.Text("ui.skill.impact.enemy_adjacent", "1 foe + adjacent");
                case ActiveSkillType.GreenDragonSlash:
                case ActiveSkillType.AzureDragonSlash:
                case ActiveSkillType.WesternStampede:
                    return LocalizationService.Text("ui.skill.impact.line", "up to 2 foes");
                case ActiveSkillType.WarCry:
                case ActiveSkillType.LionWarCry:
                    return LocalizationService.Text("ui.skill.impact.nearby", "nearby foes");
                default:
                    return string.Empty;
            }
        }

        public string BuildSkillOutcomeSummary(BattleIntentPreview preview)
        {
            if (preview == null)
            {
                return string.Empty;
            }

            List<string> parts = new List<string>();
            if (preview.PredictedHealing > 0)
            {
                parts.Add(LocalizationService.Format("ui.skill.detail.heal", "+{0} HP", preview.PredictedHealing));
            }

            if (preview.PredictedDamage > 0)
            {
                parts.Add(LocalizationService.Format("ui.skill.detail.damage", "-{0} HP", preview.PredictedDamage));
            }

            if (preview.AffectedTargetIds.Count > 1)
            {
                parts.Add(LocalizationService.Format("ui.skill.detail.multi", "{0} targets hit", preview.AffectedTargetIds.Count));
            }

            if (preview.PredictedStatuses.Count > 0)
            {
                parts.Add(LocalizationService.Format("ui.skill.detail.status_count", "{0} status effects", preview.PredictedStatuses.Count));
            }

            return parts.Count == 0 ? string.Empty : string.Join(" | ", parts);
        }

        public string BuildSkillResultSummary(SkillResult skillResult)
        {
            if (skillResult == null || skillResult.Effects == null || skillResult.Effects.Count == 0)
            {
                return string.Empty;
            }

            int totalHealing = skillResult.Effects.Where(effect => effect.IsHealing).Sum(effect => effect.Amount);
            int totalDamage = skillResult.Effects.Where(effect => !effect.IsHealing).Sum(effect => effect.Amount);
            int totalStatuses = skillResult.Effects.Sum(effect => effect.AppliedStatuses.Count(status => status.WasApplied));
            List<string> parts = new List<string>();
            if (totalHealing > 0)
            {
                parts.Add(LocalizationService.Format("ui.skill.detail.heal", "+{0} HP", totalHealing));
            }

            if (totalDamage > 0)
            {
                parts.Add(LocalizationService.Format("ui.skill.detail.damage", "-{0} HP", totalDamage));
            }

            if (skillResult.Effects.Count > 1)
            {
                parts.Add(LocalizationService.Format("ui.skill.detail.multi", "{0} targets hit", skillResult.Effects.Count));
            }

            if (totalStatuses > 0)
            {
                parts.Add(LocalizationService.Format("ui.skill.detail.status_count", "{0} status effects", totalStatuses));
            }

            return parts.Count == 0 ? string.Empty : string.Join(" | ", parts);
        }

        public string BuildEffectLine(BattleSimulation simulation, BattleIntentEffectPreview effect)
        {
            List<string> parts = new List<string>();
            if (effect.PredictedHealing > 0)
            {
                parts.Add(LocalizationService.Format("ui.skill.detail.heal", "+{0} HP", effect.PredictedHealing));
            }
            else if (effect.PredictedDamage > 0)
            {
                parts.Add(effect.Lethal
                    ? LocalizationService.Format("ui.skill.effect.ko_line", "-{0} HP | KO", effect.PredictedDamage)
                    : LocalizationService.Format("ui.skill.effect.damage_line", "-{0} HP | {1} HP left", effect.PredictedDamage, effect.RemainingHp));
            }

            string statusText = BuildStatusSummaryText(effect.PredictedStatuses);
            if (!string.IsNullOrWhiteSpace(statusText))
            {
                parts.Add(statusText);
            }

            return LocalizationService.Format(
                "ui.forecast.skill.effect_line",
                "{0}: {1}",
                GetUnitDisplayName(simulation, effect.UnitId),
                parts.Count == 0 ? LocalizationService.Text("ui.forecast.skill.none", "No forecast available.") : string.Join(" | ", parts));
        }

        public string BuildEffectLine(BattleSimulation simulation, SkillEffectResult effect)
        {
            List<string> parts = new List<string>();
            if (effect.Amount > 0)
            {
                parts.Add(effect.IsHealing
                    ? LocalizationService.Format("ui.skill.detail.heal", "+{0} HP", effect.Amount)
                    : effect.UnitDied
                        ? LocalizationService.Format("ui.skill.effect.ko_line", "-{0} HP | KO", effect.Amount)
                        : LocalizationService.Format("ui.skill.effect.damage_line", "-{0} HP | {1} HP left", effect.Amount, effect.RemainingHp));
            }

            string statusText = BuildStatusSummaryText(effect.AppliedStatuses.Where(status => status.WasApplied).ToList());
            if (!string.IsNullOrWhiteSpace(statusText))
            {
                parts.Add(statusText);
            }

            return LocalizationService.Format(
                "ui.forecast.skill.effect_line",
                "{0}: {1}",
                GetUnitDisplayName(simulation, effect.UnitId),
                parts.Count == 0 ? LocalizationService.Text("ui.forecast.skill.none", "No forecast available.") : string.Join(" | ", parts));
        }

        public string BuildStatusSummaryText(IReadOnlyList<SkillStatusApplication> statuses)
        {
            if (statuses == null || statuses.Count == 0)
            {
                return string.Empty;
            }

            return string.Join(", ", statuses.Select(FormatStatusApplication));
        }

        public string FormatStatusApplication(SkillStatusApplication status)
        {
            return LocalizationService.Format("ui.status.duration", "{0} ({1}T)", GetStatusDisplayName(status.Type), status.Duration);
        }

        public string BuildTerrainEffectSummary(BattleContext context, UnitRuntimeState unit, TerrainType terrainType)
        {
            List<string> parts = new List<string>();
            if (terrainType == TerrainType.Forest || terrainType == TerrainType.Hazard)
            {
                parts.Add(LocalizationService.Format("ui.terrain.effect.move_cost", "Move cost {0}", TerrainRules.GetMoveCost(unit, terrainType)));
            }

            int defenseBonus = TerrainRules.GetDefenseBonus(terrainType);
            if (defenseBonus > 0)
            {
                parts.Add(LocalizationService.Format("ui.terrain.effect.defense_bonus", "Defense +{0}", defenseBonus));
            }

            int healing = TerrainRules.GetEndTurnHealing(terrainType) + EquipmentEffectRules.GetFortHealingBonus(unit);
            if (healing > 0)
            {
                parts.Add(LocalizationService.Format("ui.terrain.effect.heal", "End turn heal {0}", healing));
            }

            int damage = EquipmentEffectRules.IgnoresHazardTick(unit) ? 0 : TerrainRules.GetEndTurnDamage(terrainType);
            if (damage > 0)
            {
                parts.Add(LocalizationService.Format("ui.terrain.effect.damage", "End turn damage {0}", damage));
            }
            else if (terrainType == TerrainType.Hazard && EquipmentEffectRules.IgnoresHazardTick(unit))
            {
                parts.Add(LocalizationService.Text("ui.terrain.effect.ignore_hazard", "Hazard damage ignored"));
            }

            return parts.Count == 0
                ? LocalizationService.Text("ui.terrain.effect.none", "No special effect")
                : string.Join(" | ", parts);
        }

        public string BuildEquipmentSummary(ItemDefinition armor, ItemDefinition mount)
        {
            List<string> sections = new List<string>();
            if (armor != null)
            {
                List<string> parts = new List<string>
                {
                    LocalizationService.Format("ui.label.armor_value", "Armor: {0}", LocalizationService.Text(armor.NameKey, armor.NameFallback)),
                };
                if (armor.DefenseBonus != 0)
                {
                    parts.Add(LocalizationService.Format("camp.item.defense", "DEF +{0}", armor.DefenseBonus));
                }

                if (armor.HpBonus != 0)
                {
                    parts.Add(LocalizationService.Format("camp.item.hp", "HP +{0}", armor.HpBonus));
                }

                sections.Add(string.Join("  ", parts));
            }

            if (mount != null)
            {
                List<string> parts = new List<string>
                {
                    LocalizationService.Format("ui.label.mount_value", "Mount: {0}", LocalizationService.Text(mount.NameKey, mount.NameFallback)),
                };
                if (mount.MoveBonus != 0)
                {
                    parts.Add(LocalizationService.Format("camp.item.move", "MOVE +{0}", mount.MoveBonus));
                }

                sections.Add(string.Join("  ", parts));
            }

            return sections.Count == 0 ? string.Empty : string.Join("\n", sections);
        }

        public string GetTerrainDisplayName(TerrainType terrainType)
        {
            return LocalizationService.Text(TerrainRules.GetNameKey(terrainType), terrainType.ToString());
        }

        public string GetUnitDisplayName(BattleSimulation simulation, string unitId)
        {
            UnitRuntimeState unit = simulation != null ? simulation.Context.GetUnit(unitId) : null;
            return unit != null
                ? LocalizationService.Text(unit.DisplayNameKey, unit.DisplayName)
                : unitId;
        }

        public string GetSkillDisplayName(BattleSimulation simulation, string unitId, ActiveSkillType skillType)
        {
            UnitRuntimeState unit = simulation != null ? simulation.Context.GetUnit(unitId) : null;
            return unit != null && unit.ActiveSkill == skillType
                ? LocalizationService.Text(unit.ActiveSkillNameKey, skillType.ToString())
                : skillType.ToString();
        }

        public int GetCurrentMoveRange(BattleContext context, UnitRuntimeState unit)
        {
            return unit == null ? 0 : PassiveSkillRules.GetMoveRange(unit) + EquipmentEffectRules.GetMoveBonus(context, unit);
        }

        public string BuildRosterPositionLabel(BattleContext context, UnitRuntimeState unit)
        {
            if (unit == null)
            {
                return LocalizationService.Text("ui.position.compact", "(0,0)");
            }

            ItemDefinition mount = ItemCatalog.Get(unit.EquipmentLoadout.MountId);
            int moveRange = GetCurrentMoveRange(context, unit);
            return mount != null && mount.MoveBonus > 0
                ? LocalizationService.Format("ui.position.compact_move_mount", "({0},{1}) | M {2} | {3}", unit.Position.X, unit.Position.Y, moveRange, LocalizationService.Text(mount.NameKey, mount.NameFallback))
                : LocalizationService.Format("ui.position.compact_move", "({0},{1}) | M {2}", unit.Position.X, unit.Position.Y, moveRange);
        }

        public string BuildStatusSummary(UnitRuntimeState unit)
        {
            if (unit == null || unit.StatusEffects.Count == 0)
            {
                return LocalizationService.Text("ui.status.none", "None");
            }

            return string.Join(", ", unit.StatusEffects.Select(effect => GetStatusDisplayName(effect.Type)));
        }

        public IReadOnlyList<BattleRosterTag> BuildRosterTags(UnitRuntimeState unit, bool lowHp, bool exposed, bool skillReady, bool threateningSelection)
        {
            List<BattleRosterTag> tags = new List<BattleRosterTag>();
            if (exposed)
            {
                tags.Add(BattleRosterTag.Exposed);
            }

            if (threateningSelection)
            {
                tags.Add(BattleRosterTag.Threatening);
            }

            if (skillReady)
            {
                tags.Add(BattleRosterTag.SkillReady);
            }

            if (lowHp)
            {
                tags.Add(BattleRosterTag.LowHp);
            }

            if (unit != null && unit.IsAlive)
            {
                tags.Add(unit.HasActed ? BattleRosterTag.Done : BattleRosterTag.Ready);
            }

            return tags
                .Distinct()
                .OrderBy(GetRosterTagPriority)
                .Take(2)
                .ToList();
        }

        public string GetRosterTagLabel(BattleRosterTag tag)
        {
            switch (tag)
            {
                case BattleRosterTag.Ready:
                    return LocalizationService.Text("ui.roster.ready", "READY");
                case BattleRosterTag.Done:
                    return LocalizationService.Text("ui.roster.done", "DONE");
                case BattleRosterTag.SkillReady:
                    return LocalizationService.Text("ui.roster.skill_ready", "SKILL READY");
                case BattleRosterTag.Threatening:
                    return LocalizationService.Text("ui.roster.threatening", "THREATENING");
                case BattleRosterTag.LowHp:
                    return LocalizationService.Text("ui.roster.low_hp", "LOW HP");
                case BattleRosterTag.Exposed:
                    return LocalizationService.Text("ui.threat.exposed_short", "EXPOSED");
                default:
                    return tag.ToString();
            }
        }

        public string FormatScenarioVariantTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag) || string.Equals(tag, "Normal", StringComparison.Ordinal))
            {
                return LocalizationService.Text("campaign.variant.normal", "Normal");
            }

            const string replayPrefix = "Replay ";
            if (tag.StartsWith(replayPrefix, StringComparison.Ordinal))
            {
                return LocalizationService.Format("campaign.variant.replay", "Replay {0}", tag.Substring(replayPrefix.Length));
            }

            return tag;
        }

        public string FormatExpGainText(int experience)
        {
            return LocalizationService.Format("ui.exp.gain", "EXP +{0}", experience);
        }

        public string GetRoleShortLabel(UnitRole role)
        {
            switch (role)
            {
                case UnitRole.Commander:
                    return LocalizationService.Text("ui.role.short.commander", "CMD");
                case UnitRole.Guardian:
                    return LocalizationService.Text("ui.role.short.guardian", "GDN");
                case UnitRole.Ranger:
                    return LocalizationService.Text("ui.role.short.ranger", "RNG");
                case UnitRole.Scout:
                    return LocalizationService.Text("ui.role.short.scout", "SCT");
                case UnitRole.Raider:
                    return LocalizationService.Text("ui.role.short.raider", "RDR");
                default:
                    return LocalizationService.Text("ui.role.short.unknown", "UNIT");
            }
        }

        public string GetStatusDisplayName(StatusEffectType statusEffectType)
        {
            switch (statusEffectType)
            {
                case StatusEffectType.Inspired:
                    return LocalizationService.Text("status.inspired.name", "Inspired");
                case StatusEffectType.ShatteredArmor:
                    return LocalizationService.Text("status.shattered_armor.name", "Shattered Armor");
                case StatusEffectType.Intimidated:
                    return LocalizationService.Text("status.intimidated.name", "Intimidated");
                case StatusEffectType.Bleeding:
                    return LocalizationService.Text("status.bleeding.name", "Bleeding");
                case StatusEffectType.Rooted:
                    return LocalizationService.Text("status.rooted.name", "Rooted");
                case StatusEffectType.Guarded:
                    return LocalizationService.Text("status.guarded.name", "Guarded");
                case StatusEffectType.Taunted:
                    return LocalizationService.Text("status.taunted.name", "Taunted");
                default:
                    return statusEffectType.ToString();
            }
        }

        public string BuildCombatLog(BattleSimulation simulation, CombatResult combatResult)
        {
            string attackerName = GetUnitDisplayName(simulation, combatResult.AttackerUnitId);
            string defenderName = GetUnitDisplayName(simulation, combatResult.DefenderUnitId);
            return combatResult.DefenderDied
                ? LocalizationService.Format("ui.combat.log.ko", "{0} defeated {1}.", attackerName, defenderName)
                : LocalizationService.Format("ui.combat.log.damage", "{0} dealt {1} damage to {2}.", attackerName, combatResult.Damage, defenderName);
        }

        public string BuildSkillLog(BattleSimulation simulation, SkillResult skillResult)
        {
            return LocalizationService.Format(
                "ui.skill.log.use",
                "{0} used {1}.",
                GetUnitDisplayName(simulation, skillResult.CasterUnitId),
                GetSkillDisplayName(simulation, skillResult.CasterUnitId, skillResult.SkillType));
        }

        private static int GetRosterTagPriority(BattleRosterTag tag)
        {
            switch (tag)
            {
                case BattleRosterTag.Exposed:
                    return 0;
                case BattleRosterTag.Threatening:
                    return 1;
                case BattleRosterTag.SkillReady:
                    return 2;
                case BattleRosterTag.LowHp:
                    return 3;
                case BattleRosterTag.Ready:
                    return 4;
                case BattleRosterTag.Done:
                    return 5;
                default:
                    return 6;
            }
        }

        private static string GetSkillMasteryDeltaKey(ActiveSkillType skillType)
        {
            switch (skillType)
            {
                case ActiveSkillType.RoyalAid:
                    return "ui.mastery.delta.royal_aid";
                case ActiveSkillType.ImperialAid:
                    return "ui.mastery.delta.imperial_aid";
                case ActiveSkillType.GuardOrder:
                    return "ui.mastery.delta.guard_order";
                case ActiveSkillType.PowerStrike:
                    return "ui.mastery.delta.power_strike";
                case ActiveSkillType.DragonPierce:
                    return "ui.mastery.delta.dragon_pierce";
                case ActiveSkillType.Volley:
                    return "ui.mastery.delta.volley";
                case ActiveSkillType.SkyVolley:
                    return "ui.mastery.delta.sky_volley";
                case ActiveSkillType.GreenDragonSlash:
                    return "ui.mastery.delta.green_dragon_slash";
                case ActiveSkillType.AzureDragonSlash:
                    return "ui.mastery.delta.azure_dragon_slash";
                case ActiveSkillType.WesternStampede:
                    return "ui.mastery.delta.western_stampede";
                case ActiveSkillType.WarCry:
                    return "ui.mastery.delta.war_cry";
                case ActiveSkillType.LionWarCry:
                    return "ui.mastery.delta.lion_war_cry";
                case ActiveSkillType.PinningShot:
                    return "ui.mastery.delta.pinning_shot";
                case ActiveSkillType.FireStratagem:
                    return "ui.mastery.delta.fire_stratagem";
                case ActiveSkillType.EightTrigramInferno:
                    return "ui.mastery.delta.eight_trigram_inferno";
                default:
                    return string.Empty;
            }
        }

        private static string BuildMasteryTag()
        {
            return LocalizationService.Text("ui.mastery.tag", "Lv10 Mastery");
        }
    }
}
