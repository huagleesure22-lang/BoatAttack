using NUnit.Framework;

namespace Tidebound.Tests
{
    public class TideQuestTests
    {
        [Test]
        public void UnownedAndRepeatedFragmentsDoNotAdvanceQuest()
        {
            var quest = new TideQuest(6);
            Assert.IsFalse(quest.Collect(0));
            Assert.IsFalse(quest.Defeat(-1));
            Assert.IsFalse(quest.Defeat(6));
            Assert.IsTrue(quest.Defeat(0));
            Assert.IsFalse(quest.Defeat(0));
            Assert.IsTrue(quest.Collect(0));
            Assert.IsFalse(quest.Collect(0));
            Assert.AreEqual(1, quest.Defeated);
            Assert.AreEqual(1, quest.Fragments);
        }

        [Test]
        public void AllKillsWithoutPickingUpEveryFragmentCannotWin()
        {
            var quest = new TideQuest(6);
            for (int id = 0; id < 6; id++) quest.Defeat(id);
            for (int id = 0; id < 5; id++) quest.Collect(id);
            Assert.IsFalse(quest.CanRepair);
            Assert.IsFalse(quest.Repair(true, true));
            Assert.IsFalse(quest.Repaired);
        }

        [Test]
        public void RepairRequiresLivePlayerAtLighthouseAndCompletesOnce()
        {
            var quest = new TideQuest(6);
            for (int id = 0; id < 6; id++) { quest.Defeat(id); quest.Collect(id); }
            Assert.IsTrue(quest.CanRepair);
            Assert.IsFalse(quest.Repair(false, true));
            Assert.IsFalse(quest.Repair(true, false));
            Assert.IsTrue(quest.Repair(true, true));
            Assert.IsTrue(quest.Repaired);
            Assert.IsFalse(quest.Repair(true, true));
        }

        [Test]
        public void NewRunStartsWithoutPreviousProgress()
        {
            var first = new TideQuest(6);
            first.Defeat(0);
            first.Collect(0);
            var second = new TideQuest(6);
            Assert.AreEqual(0, second.Defeated);
            Assert.AreEqual(0, second.Fragments);
            Assert.IsFalse(second.Repaired);
        }
    }
}
