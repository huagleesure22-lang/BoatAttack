using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Tidebound
{
    public sealed class TideAdventure : MonoBehaviour
    {
        public const int EncounterCount = 6;
        enum Phase { Loading, Intro, Playing, Won, Lost, Error }
        Phase phase;
        bool paused;
        string error, message;
        float messageUntil;
        Transform lighthouse;
        Light beacon;
        NavMeshSurface surface;
        Font font;
        GUIStyle titleStyle, textStyle;
        readonly List<Material> materials = new List<Material>();
        public readonly List<TideEnemy> Enemies = new List<TideEnemy>();
        public TideQuest Quest { get; private set; }
        public TidePlayer Player { get; private set; }
        public Material WaterMaterial { get; private set; }
        public bool IsPlaying => phase == Phase.Playing && !paused;

        IEnumerator Start()
        {
            Time.timeScale = 1;
            SetCursor(false);
            Quest = new TideQuest(EncounterCount);
            // Let the loading label render before the synchronous navigation bake.
            yield return null;
            PrepareWorld();
            if (phase == Phase.Error) yield break;
            var points = FindEncounterPoints();
            if (points.Count < EncounterCount + 2)
            {
                Fail("未找到足够的连通陆地区域。请检查岛屿碰撞体与导航烘焙日志。");
                yield break;
            }
            var navy = Own(new Color(.045f, .09f, .24f));
            var white = Own(new Color(.9f, .94f, 1));
            WaterMaterial = Own(new Color(.15f, .75f, 1), true);
            var teal = Own(new Color(.08f, .6f, .7f));
            var gold = Own(new Color(1, .7f, .25f), true);
            CreatePlayer(points[0], navy, white);
            CreateLighthouse(points[points.Count - 1], white, navy, gold);
            for (int i = 0; i < EncounterCount; i++) CreateEnemy(i, points[i + 1], teal, navy);
            phase = Phase.Intro;
        }

        Material Own(Color color, bool glow = false)
        {
            var material = TideVisuals.Material(color, glow);
            materials.Add(material);
            return material;
        }

        void PrepareWorld()
        {
            // These prefab branches are already serialized inactive in tidebound_Island.unity.
            // Removing them here leaves the island, buildings, lighting and water intact.
            foreach (var t in GetComponentsInScene<Transform>())
            {
                if (t.name == "Logic" || t.name == "Dynamic Objects" || t.name == "Props_Floating")
                {
                    if (t.gameObject.activeInHierarchy)
                    {
                        Fail("赛车场景分支仍启用。请用 Tidebound 菜单重新创建冒险场景。");
                        return;
                    }
                    Destroy(t.gameObject);
                }
            }
            foreach (var camera in GetComponentsInScene<Camera>()) camera.gameObject.SetActive(false);
            // Terrain already has TerrainColliders. BoatAttack's decorative architecture does not.
            foreach (var filter in GetComponentsInScene<MeshFilter>())
            {
                if (!filter.gameObject.activeInHierarchy || filter.sharedMesh == null || filter.GetComponent<Collider>() != null) continue;
                string path = filter.name.ToLowerInvariant();
                var parent = filter.transform.parent;
                while (parent != null) { path += "/" + parent.name.ToLowerInvariant(); parent = parent.parent; }
                if (path.Contains("water") || path.Contains("sky") || path.Contains("cloud") ||
                    path.Contains("bush") || path.Contains("tree") || path.Contains("palm") ||
                    path.Contains("seaweed") || path.Contains("light") || path.Contains("fx")) continue;
                filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
            }
            Physics.SyncTransforms();
            surface = gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Volume;
            surface.center = new Vector3(0, 50.8f, 0);
            surface.size = new Vector3(360, 100, 360);
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.overrideVoxelSize = true;
            surface.voxelSize = .25f;
            surface.BuildNavMesh();
            if (surface.navMeshData == null) Fail("岛屿导航数据创建失败。");
        }

        List<T> GetComponentsInScene<T>() where T : Component
        {
            var result = new List<T>();
            foreach (var root in gameObject.scene.GetRootGameObjects()) result.AddRange(root.GetComponentsInChildren<T>(true));
            return result;
        }

        List<Vector3> FindEncounterPoints()
        {
            var land = new List<Vector3>();
            for (int x = -128; x <= 128; x += 8)
                for (int z = -128; z <= 128; z += 8)
                {
                    if (!Physics.Raycast(new Vector3(x, 100, z), Vector3.down, out var floor, 99, ~0, QueryTriggerInteraction.Ignore)) continue;
                    if (floor.point.y < 1 || floor.point.y > 35 || floor.normal.y < .85f) continue;
                    if (NavMesh.SamplePosition(floor.point, out var hit, 1, NavMesh.AllAreas) && hit.position.y >= .8f)
                        land.Add(hit.position);
                }
            var result = new List<Vector3>();
            if (land.Count == 0) return result;
            // Prefer the original beach, then select reachable, separated sites on the same island.
            var beach = new Vector3(-63, 3, -79);
            land.Sort((a, b) => (a - beach).sqrMagnitude.CompareTo((b - beach).sqrMagnitude));
            var spawn = land[0];
            result.Add(spawn);
            var path = new NavMeshPath();
            var reachable = new List<Vector3>();
            foreach (var point in land)
            {
                if (Vector3.Distance(spawn, point) < 18 || Vector3.Distance(spawn, point) > 100) continue;
                if (!NavMesh.CalculatePath(spawn, point, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete) continue;
                // Do not place encounters on paths that cross the submerged seabed.
                bool aboveWater = true;
                foreach (var corner in path.corners) if (corner.y < .8f) aboveWater = false;
                if (aboveWater) reachable.Add(point);
            }
            reachable.Sort((a, b) => (a - spawn).sqrMagnitude.CompareTo((b - spawn).sqrMagnitude));
            foreach (var point in reachable)
            {
                bool separated = true;
                foreach (var chosen in result) if (Vector3.Distance(point, chosen) < 13) separated = false;
                if (separated) result.Add(point);
                if (result.Count == EncounterCount + 1) break;
            }
            // The beacon is a distinct, reachable destination beyond the last encounter.
            for (int i = reachable.Count - 1; i >= 0; i--)
            {
                bool separated = true;
                foreach (var chosen in result) if (Vector3.Distance(reachable[i], chosen) < 9) separated = false;
                if (separated) { result.Add(reachable[i]); break; }
            }
            return result;
        }

        void CreatePlayer(Vector3 point, Material navy, Material white)
        {
            var go = new GameObject("Furina reference - prototype traveler");
            go.transform.position = point + Vector3.up * .15f;
            var controller = go.AddComponent<CharacterController>();
            controller.height = 2.2f;
            controller.center = Vector3.up * 1.1f;
            controller.radius = .32f;
            controller.stepOffset = .35f;
            controller.slopeLimit = 45;
            Player = go.AddComponent<TidePlayer>();
            Player.adventure = this;
            Player.visual = TideVisuals.Avatar(go.transform, navy, white, WaterMaterial);
            Player.SetSpawn(go.transform.position);
            var camera = new GameObject("Tidebound third-person camera");
            camera.tag = "MainCamera";
            var lens = camera.AddComponent<Camera>();
            lens.fieldOfView = 65;
            lens.nearClipPlane = .08f;
            lens.farClipPlane = 1000;
            camera.AddComponent<AudioListener>();
            var view = camera.AddComponent<TideCamera>();
            view.target = Player;
            Player.view = view;
        }

        void CreateEnemy(int id, Vector3 point, Material teal, Material navy)
        {
            var go = new GameObject("Tidal Echo " + (id + 1));
            go.transform.position = point;
            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.height = 1.8f;
            capsule.center = Vector3.up * .9f;
            capsule.radius = .5f;
            var agent = go.AddComponent<NavMeshAgent>();
            agent.speed = 3;
            agent.angularSpeed = 240;
            agent.acceleration = 12;
            agent.radius = .5f;
            agent.height = 1.8f;
            agent.stoppingDistance = 1.65f;
            var enemy = go.AddComponent<TideEnemy>();
            enemy.encounterId = id;
            enemy.adventure = this;
            enemy.body = TideVisuals.Part(go.transform, PrimitiveType.Sphere, "Tidal shell", Vector3.up * .85f, new Vector3(1.1f, 1.25f, 1.1f), teal).transform;
            enemy.core = enemy.body.GetComponent<Renderer>();
            foreach (var side in new[] { -1f, 1f })
                TideVisuals.Part(enemy.body, PrimitiveType.Sphere, "Eye", new Vector3(side * .18f, .1f, .45f), Vector3.one * .15f, navy);
            Enemies.Add(enemy);
        }

        void CreateLighthouse(Vector3 point, Material white, Material navy, Material gold)
        {
            lighthouse = new GameObject("The Last Lantern").transform;
            lighthouse.position = point;
            var tower = TideVisuals.Part(lighthouse, PrimitiveType.Cylinder, "Lighthouse", new Vector3(0, 4, 0), new Vector3(2.5f, 4, 2.5f), white);
            var wall = tower.AddComponent<CapsuleCollider>();
            wall.height = 2;
            wall.radius = .5f;
            var obstacle = tower.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Capsule;
            obstacle.radius = .5f;
            obstacle.height = 2;
            obstacle.carving = true;
            TideVisuals.Part(lighthouse, PrimitiveType.Cylinder, "Lantern roof", new Vector3(0, 9.5f, 0), new Vector3(3.2f, .15f, 3.2f), navy);
            var lamp = TideVisuals.Part(lighthouse, PrimitiveType.Sphere, "Dormant lamp", new Vector3(0, 8.6f, 0), Vector3.one * 1.25f, gold);
            lamp.SetActive(false);
            beacon = lighthouse.gameObject.AddComponent<Light>();
            beacon.type = LightType.Point;
            beacon.color = new Color(.55f, .85f, 1);
            beacon.range = 90;
            beacon.intensity = 0;
        }

        public void DropFragment(int id, Vector3 point)
        {
            var go = new GameObject("Lantern fragment " + (id + 1));
            go.transform.position = point + Vector3.up;
            var jewel = TideVisuals.Part(go.transform, PrimitiveType.Cube, "Fragment", Vector3.zero, Vector3.one * .4f, WaterMaterial);
            jewel.transform.localRotation = Quaternion.Euler(45, 0, 45);
            var fragment = go.AddComponent<TideFragment>();
            fragment.Adventure = this;
            fragment.EncounterId = id;
        }

        void Update()
        {
            if (TideInput.Restart && (phase == Phase.Lost || phase == Phase.Won)) Restart();
            if (phase != Phase.Playing) return;
            if (TideInput.Pause) { paused = !paused; Time.timeScale = paused ? 0 : 1; SetCursor(!paused); }
            if (!IsPlaying || lighthouse == null) return;
            if (TideInput.Interact)
            {
                bool inReach = Vector3.Distance(Player.transform.position, lighthouse.position) < 4.5f;
                if (Quest.Repair(inReach, Player.Health > 0))
                {
                    lighthouse.Find("Dormant lamp").gameObject.SetActive(true);
                    beacon.intensity = 8;
                    phase = Phase.Won;
                    SetCursor(false);
                }
                else if (inReach) Notify("灯芯仍不完整：清理全部潮汐怪并拾取 6 枚碎片。", 3);
            }
        }

        void Begin() { phase = Phase.Playing; SetCursor(true); }
        public void Lose() { phase = Phase.Lost; SetCursor(false); }
        public void Notify(string text, float seconds) { message = text; messageUntil = Time.time + seconds; }
        void Fail(string reason) { error = reason; phase = Phase.Error; Debug.LogError("Tidebound: " + reason); SetCursor(false); }
        void Restart() { Time.timeScale = 1; SceneManager.LoadScene(gameObject.scene.name); }
        void SetCursor(bool capture) { Cursor.lockState = capture ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !capture; }

        void OnGUI()
        {
            if (titleStyle == null)
            {
                font = Font.CreateDynamicFontFromOSFont(new[] { "Noto Sans CJK SC", "Microsoft YaHei", "PingFang SC", "Arial" }, 20);
                titleStyle = new GUIStyle(GUI.skin.label) { font = font, fontSize = 30, wordWrap = true };
                textStyle = new GUIStyle(GUI.skin.label) { font = font, fontSize = 19, wordWrap = true, richText = true };
            }
            var oldMatrix = GUI.matrix;
            var oldColor = GUI.color;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * scale) / 2, (Screen.height - 720 * scale) / 2), Quaternion.identity, Vector3.one * scale);
            if (phase == Phase.Loading || phase == Phase.Error)
            {
                Panel(new Rect(280, 230, 720, 200));
                GUI.Label(new Rect(310, 250, 660, 160), phase == Phase.Error ? error : "正在准备海岛步行区域……", textStyle);
            }
            else if (phase == Phase.Intro || phase == Phase.Won || phase == Phase.Lost || paused)
            {
                Panel(new Rect(300, 145, 680, 430));
                string heading = phase == Phase.Intro ? "潮幕 · 最后一盏灯" : phase == Phase.Won ? "灯塔重燃" : phase == Phase.Lost ? "潮声未尽" : "暂停";
                GUI.Label(new Rect(335, 180, 610, 60), heading, titleStyle);
                string story = phase == Phase.Intro ? "风暴过后，海岛的灯塔失去了光。六只潮汐怪带走了灯芯碎片，回家的航路渐渐消失。\n\n你是一位以芙宁娜为形象参考的水幕旅者。夺回碎片，在灯塔前按 E，让海上的归人再次看见岸。\n\nWASD 移动 · 鼠标转视角 · 空格跳跃\nShift 奔跑 · 左键水弹 · Q 潮幕震波 · Esc 暂停" :
                    phase == Phase.Won ? "你将六枚碎片嵌回灯芯。蓝色光束穿过雾，远处传来归人的钟声。\n\n“海潮会带走故事，也会将它们送回来。”\n\n原创任务《最后一盏灯》完成。按 R 再次挑战。" :
                    phase == Phase.Lost ? "灯芯还在岸上等你。留意潮汐怪的红色蓄力提示，用跳跃和走位避开攻击。\n\n按 R 重新开始。" : "WASD 移动 · 空格跳跃 · Shift 奔跑\n左键：水弹，18 伤害\nQ：潮幕震波，30 伤害，6 米范围，6 秒冷却\nE：在灯塔附近修复灯芯";
                GUI.Label(new Rect(335, 250, 610, 235), story, textStyle);
                if (GUI.Button(new Rect(510, 505, 260, 44), phase == Phase.Intro ? "启程 / Begin" : paused ? "继续 / Resume" : "重新挑战 / Restart"))
                {
                    if (phase == Phase.Intro) Begin();
                    else if (paused) { paused = false; Time.timeScale = 1; SetCursor(true); }
                    else Restart();
                }
            }
            else
            {
                Panel(new Rect(24, 22, 390, 160));
                GUI.Label(new Rect(44, 34, 360, 35), "潮幕 · 最后一盏灯", textStyle);
                GUI.Label(new Rect(44, 71, 360, 100), $"生命 {Player.Health}/100\n潮汐怪 {Quest.Defeated}/6   灯芯碎片 {Quest.Fragments}/6\nQ 潮幕震波：{(Player.WaveRemaining > 0 ? Player.WaveRemaining.ToString("F1") + " 秒" : "就绪")}", textStyle);
                GUI.Label(new Rect(625, 345, 40, 40), "+", titleStyle);
                float distance = Vector3.Distance(Player.transform.position, lighthouse.position);
                var screen = Player.view.GetComponent<Camera>().WorldToScreenPoint(lighthouse.position + Vector3.up * 10);
                if (screen.z > 0)
                {
                    float px = (screen.x - (Screen.width - 1280 * scale) / 2) / scale;
                    float py = (Screen.height - screen.y - (Screen.height - 720 * scale) / 2) / scale;
                    GUI.Label(new Rect(Mathf.Clamp(px - 100, 420, 1050), Mathf.Clamp(py, 25, 560), 220, 60), $"灯塔 · {distance:F0} m", textStyle);
                }
                Panel(new Rect(24, 615, 1232, 82));
                string hint = distance < 4.5f ? (Quest.CanRepair ? "按 E 修复灯塔" : "灯塔需要全部 6 枚灯芯碎片") : "左键 水弹   Q 震波   空格 跳跃   Shift 奔跑   Esc 暂停";
                if (Time.time < messageUntil) hint = message;
                GUI.Label(new Rect(44, 632, 1180, 54), hint, textStyle);
            }
            GUI.color = oldColor;
            GUI.matrix = oldMatrix;
        }

        static void Panel(Rect rect)
        {
            var old = GUI.color;
            GUI.color = new Color(.035f, .075f, .15f, .96f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }

        void OnDestroy()
        {
            Time.timeScale = 1;
            SetCursor(false);
            foreach (var material in materials) if (material != null) Destroy(material);
            if (font != null) Destroy(font);
        }
    }
}
