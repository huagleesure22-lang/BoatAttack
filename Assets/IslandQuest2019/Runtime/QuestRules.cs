namespace IslandQuest2019
{
    // Pure rules: independent of rendering and input, also used in validation.
    public static class QuestRules
    {
        public static int Damage(int level) { return level <= 1 ? 18 : level == 2 ? 30 : 48; }
        public static int Upgrade(int level) { return level < 3 ? level + 1 : 3; }
        public static bool CanOpen(int kills, int required, bool opened) { return !opened && kills >= required; }
        public static bool CanSimulate(bool started,bool focused,bool controlling,float health)
        { return started && focused && controlling && health>0; }
    }
}
