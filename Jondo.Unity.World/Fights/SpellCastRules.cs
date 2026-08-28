using Jondo.Unity.World.Maps;

namespace Jondo.Unity.World.Fights
{
    /// <summary>Stateful spell restrictions shared by live combatants.</summary>
    public static class SpellCastRules
    {
        public enum Rejection
        {
            None,
            Cooldown,
            PerTurn,
            PerTarget,
            NotInLine,
        }

        public static Rejection Check(
            Fighter caster, int spellId, long targetId, int sourceCell, int targetCell,
            int maxCastPerTurn, int maxCastPerTarget, bool castInLine)
        {
            // Recarga also carries InitialCooldown and effect-1045 cooldowns, independently of a
            // spell's MinCastInterval, so its current value is always authoritative.
            if (caster.Recarga.TryGetValue(spellId, out int cooldown) && cooldown > 0)
                return Rejection.Cooldown;

            caster.LanzadosEsteTurno.TryGetValue(spellId, out int thisTurn);
            if (maxCastPerTurn > 0 && thisTurn >= maxCastPerTurn)
                return Rejection.PerTurn;

            caster.LanzadosPorObjetivo.TryGetValue((spellId, targetId), out int onTarget);
            if (targetId != 0 && maxCastPerTarget > 0 && onTarget >= maxCastPerTarget)
                return Rejection.PerTarget;

            if (castInLine && !MapGeometry.AreAligned(sourceCell, targetCell))
                return Rejection.NotInLine;

            return Rejection.None;
        }

        public static void Register(Fighter caster, int spellId, long targetId,
                                    int minimumCastInterval)
        {
            caster.LanzadosEsteTurno.TryGetValue(spellId, out int thisTurn);
            caster.LanzadosEsteTurno[spellId] = thisTurn + 1;

            if (targetId != 0)
            {
                caster.LanzadosPorObjetivo.TryGetValue((spellId, targetId), out int onTarget);
                caster.LanzadosPorObjetivo[(spellId, targetId)] = onTarget + 1;
            }

            if (minimumCastInterval > 0)
                caster.Recarga[spellId] = minimumCastInterval;
        }

        public static void EndTurn(Fighter caster)
        {
            AdvanceCooldowns(caster);
            ClearTurnCounters(caster);
        }

        public static void AdvanceCooldowns(Fighter caster)
        {
            foreach (int spellId in caster.Recarga.Keys.ToArray())
            {
                if (caster.Recarga[spellId] > 0) caster.Recarga[spellId]--;
            }
        }

        public static void ClearTurnCounters(Fighter caster)
        {
            caster.LanzadosEsteTurno.Clear();
            caster.LanzadosPorObjetivo.Clear();
        }
    }
}
