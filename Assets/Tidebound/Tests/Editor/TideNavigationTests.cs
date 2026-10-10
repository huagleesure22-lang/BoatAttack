using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;

namespace Tidebound.Tests
{
    public sealed class TideNavigationTests
    {
        [Test]
        public void WalkingNavigationProvidesACompletePathAcrossASlope()
        {
            var settings = TideNavigation.CreateWalkerSettings();
            NavMeshData data = null;
            NavMeshDataInstance instance = default;
            try
            {
                // A ten-degree slope exposed the original boat-agent bug:
                // agent 0's zero-degree slope allowance produces no walking path.
                var sources = new List<NavMeshBuildSource>
                {
                    new NavMeshBuildSource
                    {
                        shape = NavMeshBuildSourceShape.Box,
                        size = new Vector3(50, 1, 50),
                        transform = Matrix4x4.TRS(new Vector3(0, 8, 0), Quaternion.Euler(0, 0, 10), Vector3.one),
                        area = 0
                    }
                };
                data = NavMeshBuilder.BuildNavMeshData(settings, sources,
                    new Bounds(new Vector3(0, 8, 0), new Vector3(60, 30, 60)), Vector3.zero, Quaternion.identity);
                Assert.IsNotNull(data);
                instance = NavMesh.AddNavMeshData(data);
                var filter = new NavMeshQueryFilter { agentTypeID = settings.agentTypeID, areaMask = NavMesh.AllAreas };
                float slope = Mathf.Tan(10 * Mathf.Deg2Rad);
                float top = 8 + .5f / Mathf.Cos(10 * Mathf.Deg2Rad);
                Assert.IsTrue(NavMesh.SamplePosition(new Vector3(-10, top - slope * 10, -5), out var start, 1, filter));
                Assert.IsTrue(NavMesh.SamplePosition(new Vector3(10, top + slope * 10, 5), out var end, 1, filter));
                var path = new NavMeshPath();
                Assert.IsTrue(NavMesh.CalculatePath(start.position, end.position, filter, path));
                Assert.AreEqual(NavMeshPathStatus.PathComplete, path.status);
            }
            finally
            {
                if (instance.valid) instance.Remove();
                if (data != null) Object.DestroyImmediate(data);
                NavMesh.RemoveSettings(settings.agentTypeID);
            }
        }
    }
}
