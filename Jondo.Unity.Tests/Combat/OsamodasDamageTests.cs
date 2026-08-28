using Jondo.Unity.Server.Handlers;
using Jondo.Unity.Server.Managers;
using Jondo.Unity.World.Combat;
using Jondo.Unity.World.Fights;
using Xunit;

namespace Jondo.Unity.Tests.Combat
{
    public class OsamodasDamageTests
    {
        [Theory]
        [InlineData(EffectSupport.CasterCurrentHealthDamage, 150)]
        [InlineData(EffectSupport.CasterMissingHealthDamage, 100)]
        public void Caster_health_damage_uses_the_catalogue_percentage_exactly(
            int effectId, int expectedBase)
        {
            var caster = Fighter(1, 600, 1000);

            Assert.Equal(expectedBase,
                EffectEngine.CasterHealthDamageBase(caster, effectId, percent: 25));
        }

        [Fact]
        public void Caster_health_damage_ignores_offensive_bonuses_but_keeps_resistance()
        {
            var fight = new FightInstance(1, 1);
            var caster = Fighter(1, 800, 1000);
            caster.Strength = 5000;
            caster.Power = 5000;
            caster.FlatDamage = 5000;
            var target = Fighter(2, 1000, 1000);
            target.NeutralResPct = 25;

            int baseDamage = EffectEngine.CasterHealthDamageBase(
                caster, EffectSupport.CasterCurrentHealthDamage, percent: 20);
            int damage = FightHandler.CalculateEffectDamage(
                fight, caster, target, EffectSupport.CasterCurrentHealthDamage,
                ElementType.Neutral, baseDamage, critical: false);

            Assert.Equal(160, baseDamage);
            Assert.Equal(120, damage);
        }

        [Fact]
        public void Best_element_damage_uses_combat_buffs_when_selecting_its_element()
        {
            var fight = new FightInstance(1, 1);
            var caster = Fighter(1, 1000, 1000);
            caster.Strength = 100;
            caster.Intelligence = 50;
            caster.Chance = 75;
            caster.Agility = 80;
            caster.Buffs.Poner(new Buff
            {
                EffectId = 1,
                EffectUid = 1,
                Caracteristica = 15,
                Cuanto = 100,
                Quien = caster.Id,
                EmpiezaEnRonda = 0,
                CaducaEnRonda = -1,
            }, fight.SiguienteEmbrujo);
            var effect = new SpellEffect
            {
                EffectId = EffectSupport.BestElementDamage,
                Element = -1,
            };

            Assert.Equal(2, EffectEngine.DamageElementFor(caster, effect, round: 0));
        }

        [Theory]
        [InlineData(EffectSupport.CasterCurrentHealthDamage)]
        [InlineData(EffectSupport.CasterMissingHealthDamage)]
        [InlineData(EffectSupport.BestElementDamage)]
        public void Osamodas_special_damage_is_routed_to_the_damage_pipeline(int effectId)
            => Assert.True(EffectEngine.EsDeDano(effectId));

        [Fact]
        public void A_non_immediate_health_damage_trigger_is_not_applied_during_the_cast()
        {
            var fight = new FightInstance(1, 1);
            var caster = Fighter(1, 1000, 1000);
            var target = Fighter(-1, 1000, 1000);
            target.IsMonster = true;
            fight.AddPlayer(caster);
            fight.AddMonster(target);

            // Spell 1043 grade 1 carries effect 89 only on trigger D, never on cast trigger I.
            Assert.Empty(EffectEngine.Golpes(fight, caster, 1043, 1, target));
        }

        private static Fighter Fighter(long id, int currentHealth, int maximumHealth)
            => new Fighter
            {
                Id = id,
                CurrentHP = currentHealth,
                MaxHP = maximumHealth,
            };
    }
}
