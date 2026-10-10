using UnityEngine.AI;

namespace Tidebound
{
    public static class TideNavigation
    {
        // Agent 0 belongs to BoatAttack: radius 3, slope 0, climb .001.
        // The caller owns the returned agent and must RemoveSettings when done.
        public static NavMeshBuildSettings CreateWalkerSettings()
        {
            var settings = NavMesh.CreateSettings();
            settings.agentRadius = .5f;
            settings.agentHeight = 2.2f;
            settings.agentSlope = 45;
            settings.agentClimb = .35f;
            settings.overrideVoxelSize = true;
            settings.voxelSize = .15f;
            return settings;
        }
    }
}
