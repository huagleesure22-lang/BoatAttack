using System.Collections.Generic;

namespace Tidebound
{
    // The quest owns encounter identities: duplicate death/pickup callbacks cannot grant progress.
    public sealed class TideQuest
    {
        readonly HashSet<int> defeated = new HashSet<int>();
        readonly HashSet<int> collected = new HashSet<int>();
        public int Required { get; }
        public int Defeated => defeated.Count;
        public int Fragments => collected.Count;
        public bool Repaired { get; private set; }
        public bool CanRepair => Defeated == Required && Fragments == Required && !Repaired;

        public TideQuest(int required)
        {
            if (required < 1) throw new System.ArgumentOutOfRangeException(nameof(required));
            Required = required;
        }

        public bool Defeat(int encounterId)
        {
            return encounterId >= 0 && encounterId < Required && !Repaired && defeated.Add(encounterId);
        }

        public bool Collect(int encounterId)
        {
            return defeated.Contains(encounterId) && !Repaired && collected.Add(encounterId);
        }

        public bool Repair(bool inReach, bool playerAlive)
        {
            if (!inReach || !playerAlive || !CanRepair) return false;
            Repaired = true;
            return true;
        }
    }
}
