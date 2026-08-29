using System;
using System.Collections.Generic;
using System.Linq;

namespace Jondo.Unity.World.Fights
{
    /// <summary>Detailed result of the combat experience calculation.</summary>
    public readonly record struct FightExperienceResult(
        long BaseExperience,
        long WinnerLevelTotal,
        long LoserLevelTotal,
        int SignificantWinnerCount,
        decimal GroupCoefficient,
        decimal LevelBalance,
        decimal HighestMonsterBalance,
        decimal RecipientShare,
        int Wisdom,
        int ChallengeBonusPercent,
        long Total);

    /// <summary>
    /// Calculates combat experience using the level balance, group, individual share, wisdom and
    /// challenge multipliers used by Dofus.
    /// </summary>
    public static class FightExperience
    {
        private static readonly decimal[] GroupCoefficients =
        {
            0m, 1m, 1.1m, 1.5m, 2.3m, 3.1m, 3.6m, 4.2m, 4.7m,
        };

        public static FightExperienceResult Calculate(
            IEnumerable<Fighter> winners,
            IEnumerable<Fighter> losers,
            Fighter recipient,
            int wisdom,
            int challengeBonusPercent)
        {
            ArgumentNullException.ThrowIfNull(winners);
            ArgumentNullException.ThrowIfNull(losers);
            ArgumentNullException.ThrowIfNull(recipient);

            var eligibleWinners = winners.Where(fighter => !fighter.EsInvocado).ToList();
            var eligibleLosers = losers.Where(fighter => !fighter.EsInvocado).ToList();
            wisdom = Math.Max(0, wisdom);
            challengeBonusPercent = Math.Max(0, challengeBonusPercent);

            long baseExperience = eligibleLosers.Sum(fighter => Math.Max(0L, fighter.XpReward));
            long winnerLevelTotal = eligibleWinners.Sum(fighter => Math.Max(1L, fighter.Level));
            long loserLevelTotal = eligibleLosers.Sum(fighter => Math.Max(1L, fighter.Level));

            if (baseExperience == 0 || winnerLevelTotal == 0 || loserLevelTotal == 0 ||
                recipient.EsInvocado || !eligibleWinners.Any(fighter => fighter.Id == recipient.Id))
            {
                return new FightExperienceResult(
                    baseExperience, winnerLevelTotal, loserLevelTotal, 0, 0m, 0m, 0m, 0m,
                    wisdom, challengeBonusPercent, 0);
            }

            int highestWinnerLevel = eligibleWinners.Max(fighter => Math.Max(1, fighter.Level));
            int highestLoserLevel = eligibleLosers.Max(fighter => Math.Max(1, fighter.Level));
            int significantWinnerCount = eligibleWinners.Count(
                fighter => Math.Max(1L, fighter.Level) * 3 >= highestWinnerLevel);
            decimal groupCoefficient = GroupCoefficients[Math.Min(significantWinnerCount, 8)];

            decimal levelBalance = 1m;
            if (winnerLevelTotal - 5 > loserLevelTotal)
            {
                levelBalance = (decimal)loserLevelTotal / winnerLevelTotal;
            }
            else if (winnerLevelTotal + 10 < loserLevelTotal)
            {
                levelBalance = (decimal)(winnerLevelTotal + 10) / loserLevelTotal;
            }

            long highestMonsterLevelCap = (long)Math.Floor(highestLoserLevel * 2.5m);
            decimal highestMonsterBalance = winnerLevelTotal > highestMonsterLevelCap
                ? (decimal)highestMonsterLevelCap / winnerLevelTotal
                : 1m;
            decimal recipientShare = (decimal)Math.Max(1L, recipient.Level) / winnerLevelTotal;

            decimal groupExperience = Math.Floor(
                baseExperience * groupCoefficient * levelBalance * highestMonsterBalance);
            decimal personalExperience = Math.Floor(groupExperience * recipientShare);
            decimal withWisdom = Math.Floor(personalExperience * (100m + wisdom) / 100m);
            decimal withChallenges = Math.Floor(
                withWisdom * (100m + challengeBonusPercent) / 100m);

            long total = withChallenges >= long.MaxValue ? long.MaxValue : (long)withChallenges;
            return new FightExperienceResult(
                baseExperience,
                winnerLevelTotal,
                loserLevelTotal,
                significantWinnerCount,
                groupCoefficient,
                levelBalance,
                highestMonsterBalance,
                recipientShare,
                wisdom,
                challengeBonusPercent,
                total);
        }
    }
}
