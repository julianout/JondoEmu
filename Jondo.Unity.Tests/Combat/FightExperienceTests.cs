using Jondo.Unity.World.Fights;
using Xunit;

namespace Jondo.Unity.Tests.Combat
{
    public class FightExperienceTests
    {
        [Fact]
        public void Balanced_solo_fight_awards_the_monsters_base_experience()
        {
            var result = Calculate(Player(100), Monster(100, 1_000));

            Assert.Equal(1_000, result.Total);
            Assert.Equal(1m, result.LevelBalance);
            Assert.Equal(1m, result.RecipientShare);
        }

        [Theory]
        [InlineData(100, 2_000)]
        [InlineData(500, 6_000)]
        [InlineData(5_000, 51_000)]
        public void Each_wisdom_point_adds_one_percent_experience(int wisdom, long expected)
        {
            var result = Calculate(Player(100), Monster(100, 1_000), wisdom);

            Assert.Equal(expected, result.Total);
        }

        [Fact]
        public void Challenge_bonus_is_applied_after_wisdom()
        {
            var result = Calculate(Player(100), Monster(100, 1_000), wisdom: 100, challenge: 50);

            Assert.Equal(3_000, result.Total);
        }

        [Fact]
        public void Fighting_a_much_higher_level_group_reduces_experience()
        {
            var result = Calculate(Player(50), Monster(100, 1_000));

            Assert.Equal(600, result.Total);
            Assert.Equal(0.6m, result.LevelBalance);
        }

        [Fact]
        public void Overlevelled_players_receive_both_level_penalties()
        {
            var result = Calculate(Player(200), Monster(50, 1_000));

            Assert.Equal(156, result.Total);
            Assert.Equal(0.25m, result.LevelBalance);
            Assert.Equal(0.625m, result.HighestMonsterBalance);
            Assert.Equal(1m, result.RecipientShare);
        }

        [Fact]
        public void Highest_monster_penalty_is_applied_to_the_group_before_individual_shares()
        {
            var recipient = Player(150, id: 1);
            var ally = Player(150, id: 2);

            var result = FightExperience.Calculate(
                new[] { recipient, ally },
                new[] { Monster(100, 1_000) },
                recipient,
                wisdom: 0,
                challengeBonusPercent: 0);

            Assert.Equal(152, result.Total);
            Assert.Equal(0.5m, result.RecipientShare);
            Assert.Equal(250m / 300m, result.HighestMonsterBalance);
        }

        [Fact]
        public void Two_significant_players_receive_the_group_coefficient_and_a_level_share()
        {
            var recipient = Player(100, id: 1);
            var ally = Player(100, id: 2);

            var result = FightExperience.Calculate(
                new[] { recipient, ally },
                new[] { Monster(200, 1_000) },
                recipient,
                wisdom: 0,
                challengeBonusPercent: 0);

            Assert.Equal(550, result.Total);
            Assert.Equal(2, result.SignificantWinnerCount);
            Assert.Equal(1.1m, result.GroupCoefficient);
            Assert.Equal(0.5m, result.RecipientShare);
        }

        [Fact]
        public void Players_below_one_third_of_the_highest_level_do_not_raise_the_group_coefficient()
        {
            var recipient = Player(200, id: 1);
            var lowLevelAlly = Player(60, id: 2);

            var result = FightExperience.Calculate(
                new[] { recipient, lowLevelAlly },
                new[] { Monster(260, 1_000) },
                recipient,
                wisdom: 0,
                challengeBonusPercent: 0);

            Assert.Equal(769, result.Total);
            Assert.Equal(1, result.SignificantWinnerCount);
            Assert.Equal(1m, result.GroupCoefficient);
        }

        [Fact]
        public void Summons_do_not_change_level_totals_group_size_or_base_experience()
        {
            var recipient = Player(100, id: 1);
            var playerSummon = Player(200, id: -10);
            playerSummon.Invocador = recipient.Id;
            var monster = Monster(100, 1_000, id: -1);
            var monsterSummon = Monster(200, 50_000, id: -2);
            monsterSummon.Invocador = monster.Id;

            var result = FightExperience.Calculate(
                new[] { recipient, playerSummon },
                new[] { monster, monsterSummon },
                recipient,
                wisdom: 0,
                challengeBonusPercent: 0);

            Assert.Equal(1_000, result.Total);
            Assert.Equal(100, result.WinnerLevelTotal);
            Assert.Equal(100, result.LoserLevelTotal);
            Assert.Equal(1, result.SignificantWinnerCount);
        }

        private static FightExperienceResult Calculate(
            Fighter player,
            Fighter monster,
            int wisdom = 0,
            int challenge = 0)
            => FightExperience.Calculate(
                new[] { player },
                new[] { monster },
                player,
                wisdom,
                challenge);

        private static Fighter Player(int level, long id = 1)
            => new Fighter { Id = id, Level = level };

        private static Fighter Monster(int level, int experience, long id = -1)
            => new Fighter { Id = id, Level = level, XpReward = experience, IsMonster = true };
    }
}
