using Jondo.Unity.Server.Handlers;
using Jondo.Unity.Server.Managers;
using Jondo.Unity.Server;
using Jondo.Unity.World.Fights;
using Jondo.Unity.World.Maps;
using Xunit;

namespace Jondo.Unity.Tests.Combat
{
    public class MonsterCastRulesTests
    {
        private const int SpellId = 42;
        private const int Origin = 288;
        private const int AlignedTarget = 317;
        private const int NonAlignedTarget = 289;

        [Fact]
        public void A_per_target_limit_blocks_only_the_target_that_spent_it()
        {
            var monster = FighterAt(Origin);
            SpellCastRules.Register(monster, SpellId, targetId: 10, minimumCastInterval: 0);

            Assert.Equal(SpellCastRules.Rejection.PerTarget,
                Check(monster, targetId: 10, maxPerTarget: 1));
            Assert.Equal(SpellCastRules.Rejection.None,
                Check(monster, targetId: 11, maxPerTarget: 1));
        }

        [Fact]
        public void A_per_turn_limit_counts_accepted_casts()
        {
            var monster = FighterAt(Origin);
            SpellCastRules.Register(monster, SpellId, targetId: 10, minimumCastInterval: 0);
            SpellCastRules.Register(monster, SpellId, targetId: 11, minimumCastInterval: 0);

            Assert.Equal(
                SpellCastRules.Rejection.PerTurn,
                SpellCastRules.Check(
                    monster, SpellId, targetId: 12,
                    sourceCell: Origin, targetCell: AlignedTarget,
                    maxCastPerTurn: 2, maxCastPerTarget: 0, castInLine: false));
        }

        [Fact]
        public void Any_existing_cooldown_is_authoritative_including_initial_cooldowns()
        {
            var monster = FighterAt(Origin);
            monster.Recarga[SpellId] = 2;

            Assert.Equal(SpellCastRules.Rejection.Cooldown, Check(monster, targetId: 10));
        }

        [Fact]
        public void A_minimum_interval_counts_down_at_the_end_of_its_owners_turn()
        {
            var monster = FighterAt(Origin);
            SpellCastRules.Register(monster, SpellId, targetId: 10, minimumCastInterval: 3);

            SpellCastRules.EndTurn(monster);
            Assert.Equal(2, monster.Recarga[SpellId]);
            Assert.Empty(monster.LanzadosEsteTurno);
            Assert.Empty(monster.LanzadosPorObjetivo);
            SpellCastRules.EndTurn(monster);
            SpellCastRules.EndTurn(monster);

            Assert.Equal(0, monster.Recarga[SpellId]);
            Assert.Equal(SpellCastRules.Rejection.None, Check(monster, targetId: 10));
        }

        [Fact]
        public void Cast_in_line_uses_isometric_axes()
        {
            Assert.True(MapGeometry.AreAligned(Origin, AlignedTarget));
            Assert.False(MapGeometry.AreAligned(Origin, NonAlignedTarget));
            var monster = FighterAt(Origin);

            Assert.Equal(SpellCastRules.Rejection.None,
                Check(monster, 10, castInLine: true, targetCell: AlignedTarget));
            Assert.Equal(SpellCastRules.Rejection.NotInLine,
                Check(monster, 10, castInLine: true, targetCell: NonAlignedTarget));
        }

        [Fact]
        public void Player_spell_limits_load_the_catalogue_cast_in_line_flag()
        {
            Assert.True(FightHandler.RequiresAlignedCast(203, level: 1));
        }

        [Fact]
        public void Off_axis_monster_moves_to_a_real_casting_cell_even_when_range_was_already_valid()
        {
            var fight = new FightInstance(1, 1);

            var path = FightHandler.PathToward(
                fight, Origin, NonAlignedTarget, steps: 1, deseada: 2,
                idealCell: cell => MapGeometry.AreAligned(cell, NonAlignedTarget)
                    && MapGeometry.Distance(cell, NonAlignedTarget) >= 1
                    && MapGeometry.Distance(cell, NonAlignedTarget) <= 3,
                preferAlignment: true,
                canEnterCell: cell => MapGeometry.IsValid(cell));

            Assert.Equal(2, path.Count);
            Assert.NotEqual(Origin, path[^1]);
            Assert.True(MapGeometry.AreAligned(path[^1], NonAlignedTarget));
        }

        [Fact]
        public void Monster_initial_cooldowns_are_loaded_from_the_real_spell_grade()
        {
            var monster = FighterAt(Origin);
            monster.SpellIds.Add(214);
            monster.SpellGrades[214] = 1;

            FightHandler.InitializeMonsterCooldowns(monster);

            Assert.Equal(1, monster.Recarga[214]);
            Assert.True(FightHandler.IsSpellOnCooldown(monster, 214));
        }

        [Fact]
        public void Inverted_catalogue_ranges_are_treated_as_a_single_safe_maximum()
        {
            var spell = DatabaseManager.GetSpellCombatData(592, 1);

            Assert.NotNull(spell);
            Assert.Equal(5, spell.MinRange);
            Assert.Equal(1, spell.MaxRange);
            Assert.Equal(5, FightHandler.EffectiveMaximumRange(spell));
        }

        private static SpellCastRules.Rejection Check(
            Fighter caster, long targetId, int maxPerTarget = 0, bool castInLine = false,
            int targetCell = AlignedTarget)
            => SpellCastRules.Check(
                caster, SpellId, targetId, caster.CellId, targetCell,
                maxCastPerTurn: 0, maxPerTarget, castInLine);

        private static Fighter FighterAt(int cell)
            => new Fighter
            {
                Id = -1,
                TeamId = 1,
                CellId = cell,
                IsMonster = true,
                MaxHP = 100,
                CurrentHP = 100,
                MaxAP = 6,
                CurrentAP = 6,
                MaxMP = 3,
                CurrentMP = 3,
            };
    }
}
