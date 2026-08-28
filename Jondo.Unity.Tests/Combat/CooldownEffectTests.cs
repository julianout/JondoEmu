using Jondo.Unity.Server.Handlers;
using Jondo.Unity.Server.Managers;
using Jondo.Unity.World.Combat;
using Jondo.Unity.World.Fights;
using Xunit;

namespace Jondo.Unity.Tests.Combat
{
    public class CooldownEffectTests
    {
        [Theory]
        [InlineData(0, 99, 0)]
        [InlineData(3, 99, 3)]
        public void Effect_1045_uses_Value_only_and_keeps_zero_as_a_reset(
            int value, int diceSide, int expectedRounds)
        {
            var fight = new FightInstance(1, 1);
            var caster = Fighter(1);
            fight.AddPlayer(caster);
            var effect = new SpellEffect
            {
                EffectId = EffectSupport.SetCooldown,
                EffectUid = 1,
                DiceNum = 4312,
                DiceSide = diceSide,
                Value = value,
                Triggers = EffectEngine.AlLanzar,
            };

            var outcome = Assert.Single(EffectEngine.ResolveEffects(
                fight, caster, 4317, 1, caster, EffectEngine.AlLanzar, 1,
                new[] { effect }));

            Assert.Equal(4312, outcome.CooldownSpell);
            Assert.Equal(expectedRounds, outcome.CooldownRounds);
        }

        [Fact]
        public void A_zero_round_outcome_reactivates_a_spell()
        {
            var fighter = Fighter(1);
            fighter.Recarga[4312] = 4;
            var outcome = new Outcome
            {
                Sobre = fighter,
                Efecto = new SpellEffect { EffectId = EffectSupport.SetCooldown },
                CooldownSpell = 4312,
                CooldownRounds = 0,
            };

            Assert.True(FightHandler.ApplyCooldownOutcome(outcome));
            Assert.Equal(0, fighter.Recarga[4312]);
            Assert.False(FightHandler.IsSpellOnCooldown(fighter, 4312));
        }

        [Fact]
        public void Spell_4317_keeps_its_zero_round_reactivation_entries()
        {
            var fight = new FightInstance(1, 1);
            var caster = Fighter(1);
            var target = Fighter(2);
            fight.AddPlayer(caster);
            fight.AddPlayer(target);

            var resets = EffectEngine.Resolver(
                    fight, caster, 4317, 1, target, EffectEngine.AlLanzar, 1)
                .Where(outcome => outcome.CooldownSpell > 0)
                .Select(outcome => (outcome.CooldownSpell, outcome.CooldownRounds))
                .ToArray();

            Assert.Contains((4312, 0), resets);
            Assert.Contains((4313, 0), resets);
            Assert.All(resets, reset => Assert.Equal(0, reset.CooldownRounds));
        }

        [Fact]
        public void Monster_cooldowns_stay_internal_and_are_not_listed_in_default_jxc()
        {
            var monster = Fighter(-1);
            monster.IsMonster = true;
            monster.Recarga[31166] = 2;

            Assert.True(FightHandler.IsSpellOnCooldown(monster, 31166));
            Assert.Empty(FightHandler.CooldownsForPacket(monster));
        }

        private static Fighter Fighter(long id)
            => new Fighter { Id = id, CurrentHP = 100, MaxHP = 100 };
    }
}
