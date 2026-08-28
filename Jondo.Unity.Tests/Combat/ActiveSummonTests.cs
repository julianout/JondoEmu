using Jondo.Unity.Server.Handlers;
using Jondo.Unity.Server.Managers;
using Jondo.Unity.World.Combat;
using Jondo.Unity.World.Fights;
using Xunit;

namespace Jondo.Unity.Tests.Combat
{
    public class ActiveSummonTests
    {
        [Theory]
        [InlineData(EffectSupport.Summon)]
        [InlineData(EffectSupport.ControllableSummon)]
        public void Summon_effects_keep_the_template_and_their_own_grade(int effectId)
        {
            var fight = new FightInstance(1, 1);
            var caster = AliveFighter(10);
            fight.AddPlayer(caster);
            var effect = new SpellEffect
            {
                EffectId = effectId,
                EffectUid = 1,
                DiceNum = 8076,
                DiceSide = 2,
                Triggers = EffectEngine.AlLanzar,
            };

            var outcome = Assert.Single(EffectEngine.ResolveEffects(
                fight, caster, 100, 5, caster, EffectEngine.AlLanzar, 1,
                new[] { effect }, aimedCell: 100));

            Assert.Equal(8076, outcome.Invoca);
            Assert.Equal(2, outcome.SummonGrade);
        }

        [Fact]
        public void Dragoune_uses_the_requested_monster_grade_and_active_spell_list()
        {
            var fight = new FightInstance(1, 1);
            var owner = AliveFighter(10);
            owner.Level = 200;
            fight.AddPlayer(owner);

            var dragoune = FightHandler.CreateSummonedFighter(
                fight, owner, template: 8076, grade: 2, cell: 200);

            Assert.NotNull(dragoune);
            Assert.Equal(1, dragoune.GradeIndex);
            Assert.Equal(4, dragoune.MaxMP);
            Assert.Equal(50, dragoune.Intelligence);
            Assert.Equal(50, dragoune.FireDamage);
            Assert.Equal(new[] { 31166, 31168 }, dragoune.SpellIds);
            Assert.All(dragoune.SpellIds, spell => Assert.Equal(2, dragoune.SpellGrades[spell]));
            Assert.True(dragoune.JuegaTurno);
            Assert.True(FightHandler.ShouldUseMonsterAi(dragoune));
        }

        [Fact]
        public void Active_summons_play_after_their_owner_and_passive_summons_skip_ai()
        {
            var fight = new FightInstance(1, 1);
            var owner = AliveFighter(10);
            owner.Initiative = 100;
            var enemy = AliveFighter(-1, monster: true);
            enemy.Initiative = 50;
            fight.AddPlayer(owner);
            fight.AddMonster(enemy);

            var active = AliveFighter(-2, monster: true);
            active.JuegaTurno = true;
            active.SpellIds.Add(31166);
            var passive = AliveFighter(-3, monster: true);
            passive.JuegaTurno = false;
            fight.Invocar(active, owner);
            fight.Invocar(passive, owner);

            Assert.Equal(new long[] { owner.Id, active.Id, enemy.Id },
                         fight.TurnOrder.Select(fighter => fighter.Id));
            Assert.True(FightHandler.ShouldUseMonsterAi(active));
            Assert.False(FightHandler.ShouldUseMonsterAi(passive));
            Assert.True(FightHandler.ShouldUseMonsterAi(enemy));
        }

        private static Fighter AliveFighter(long id, bool monster = false)
            => new Fighter
            {
                Id = id,
                IsMonster = monster,
                MaxHP = 100,
                CurrentHP = 100,
            };
    }
}
