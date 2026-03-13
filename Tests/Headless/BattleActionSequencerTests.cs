using System.Collections.Generic;
using PhalanxChronicle.Battle.Effects;
using PhalanxChronicle.Core;
using Xunit;

namespace PhalanxChronicle.Headless.Tests
{
    public sealed class BattleActionSequencerTests
    {
        [Fact]
        public void EnemyLowSignalCombat_UsesCompressedPresentationPolicy()
        {
            BattleActionSequencer sequencer = new BattleActionSequencer();
            CombatResult combatResult = new CombatResult("enemy-1", "player-1", 2, 18, false, 0, 0);
            UnitActionResult actionResult = new UnitActionResult("enemy-1", new GridPosition(2, 1), new GridPosition(2, 1), combatResult, null);

            Assert.Equal(BattlePresentationImpactTier.LowSignal, sequencer.GetImpactTier(combatResult));
            Assert.False(sequencer.ShouldShowCombatForecast(TurnSide.Enemy, combatResult));
            Assert.Equal(0.02f, sequencer.GetPostCombatHold(TurnSide.Enemy, combatResult));
            Assert.Equal(0.1f, sequencer.GetPostActionHold(actionResult));
        }

        [Fact]
        public void CriticalEnemySkill_KeepsReadableForecastAndHold()
        {
            BattleActionSequencer sequencer = new BattleActionSequencer();
            SkillResult skillResult = new SkillResult(
                "enemy-archer",
                ActiveSkillType.PinningShot,
                "player-1",
                new List<SkillEffectResult>
                {
                    new SkillEffectResult(
                        "player-1",
                        6,
                        12,
                        false,
                        false,
                        new List<SkillStatusApplication>
                        {
                            new SkillStatusApplication(StatusEffectType.Rooted, 1, true),
                        }),
                },
                0,
                0);

            Assert.Equal(BattlePresentationImpactTier.Critical, sequencer.GetImpactTier(skillResult));
            Assert.True(sequencer.ShouldShowSkillForecast(TurnSide.Enemy, skillResult));
            Assert.True(sequencer.ShouldShowSkillBark(TurnSide.Enemy, skillResult));
            Assert.True(sequencer.GetPostSkillHold(TurnSide.Enemy, skillResult) > BattlePresentationProfile.EnemyFastResolve.PostSkillHold);
        }

        [Fact]
        public void EnemyLowSignalSkill_RemovesInterEffectDelay()
        {
            BattleActionSequencer sequencer = new BattleActionSequencer();
            SkillResult skillResult = new SkillResult(
                "enemy-buffer",
                ActiveSkillType.WarCry,
                "player-1",
                new List<SkillEffectResult>
                {
                    new SkillEffectResult("player-1", 0, 20, false, false),
                },
                0,
                0);

            Assert.Equal(BattlePresentationImpactTier.LowSignal, sequencer.GetImpactTier(skillResult));
            Assert.Equal(0f, sequencer.GetInterEffectDelay(TurnSide.Enemy, skillResult));
            Assert.False(sequencer.ShouldShowSkillForecast(TurnSide.Enemy, skillResult));
        }

        [Fact]
        public void EnemyLowSignalMultiTargetSkill_BatchesImpact()
        {
            BattleActionSequencer sequencer = new BattleActionSequencer();
            SkillResult skillResult = new SkillResult(
                "enemy-archer",
                ActiveSkillType.Volley,
                "enemy-archer",
                new List<SkillEffectResult>
                {
                    new SkillEffectResult("player-1", 0, 14, false, false),
                    new SkillEffectResult("player-2", 0, 14, false, false),
                    new SkillEffectResult("player-3", 0, 14, false, false),
                },
                0,
                0);

            Assert.Equal(BattlePresentationImpactTier.LowSignal, sequencer.GetImpactTier(skillResult));
            Assert.True(sequencer.ShouldBatchSkillResult(TurnSide.Enemy, skillResult));
            Assert.Equal(0f, sequencer.GetInterEffectDelay(TurnSide.Enemy, skillResult));
        }

        [Fact]
        public void PlayerMultiTargetSkill_DoesNotBatchByDefault()
        {
            BattleActionSequencer sequencer = new BattleActionSequencer();
            SkillResult skillResult = new SkillResult(
                "player-archer",
                ActiveSkillType.Volley,
                "player-archer",
                new List<SkillEffectResult>
                {
                    new SkillEffectResult("enemy-1", 4, 14, false, false),
                    new SkillEffectResult("enemy-2", 4, 14, false, false),
                },
                0,
                0);

            Assert.False(sequencer.ShouldBatchSkillResult(TurnSide.Player, skillResult));
        }
    }
}
