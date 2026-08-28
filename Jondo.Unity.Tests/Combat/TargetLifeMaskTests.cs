using Jondo.Unity.Server.Managers;
using Jondo.Unity.World.Combat;
using Jondo.Unity.World.Fights;
using Xunit;

namespace Jondo.Unity.Tests.Combat
{
    public class TargetLifeMaskTests
    {
        [Theory]
        [InlineData("a,V100,v95", 100, false)]
        [InlineData("a,V100,v95", 99, true)]
        [InlineData("a,V100,v95", 95, true)]
        [InlineData("a,V100,v95", 94, false)]
        [InlineData("a,V95,v90", 95, false)]
        [InlineData("a,V95,v90", 94, true)]
        [InlineData("a,V95,v90", 90, true)]
        [InlineData("a,V95,v90", 89, false)]
        [InlineData("a,V20,v0", 20, false)]
        [InlineData("a,V20,v0", 1, true)]
        public void Staircase_life_bands_use_V_as_ceiling_and_v_as_floor(
            string mask, int currentHealth, bool expectedToMatch)
        {
            var fight = new FightInstance(1, 1);
            var caster = Fighter(1, 100);
            var target = Fighter(2, currentHealth);
            fight.AddPlayer(caster);
            fight.AddPlayer(target);
            var effect = new SpellEffect
            {
                EffectId = EffectSupport.AddState,
                EffectUid = 1,
                DiceNum = 321,
                Triggers = EffectEngine.AlLanzar,
                TargetMask = mask,
            };

            var outcomes = EffectEngine.ResolveEffects(
                fight, caster, 100, 1, target, EffectEngine.AlLanzar, 1,
                new[] { effect });

            Assert.Equal(expectedToMatch, outcomes.Count == 1);
            Assert.Equal(expectedToMatch, target.Buffs.TieneEstado(321));
        }

        private static Fighter Fighter(long id, int currentHealth)
            => new Fighter
            {
                Id = id,
                CellId = (int)id + 100,
                CurrentHP = currentHealth,
                MaxHP = 100,
            };
    }
}
