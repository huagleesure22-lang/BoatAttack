using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace IslandQuest2019.Editor
{
    [Serializable] public class MaterialData { public string name; public float[] color; public float metallic,roughness; }
    [Serializable] public class ObjectData { public string name,material; public int parent; public float[] position,rotation,vertices; public int[] triangles; }
    [Serializable] public class ModelData { public string name; public MaterialData[] materials; public ObjectData[] objects; }

    public static class QuestAssetBuilder
    {
        const string Root="Assets/IslandQuest2019";
        const string Generated=Root+"/Generated";
        const string GalleryPath="Assets/scenes/quest_assets_2019.unity";
        const string AdventurePath="Assets/scenes/quest_adventure_2019.unity";
        static Dictionary<string,Material> materials=new Dictionary<string,Material>();

        [MenuItem("Island2019/1 - Build Assets and Gallery")]
        public static void BuildGallery()
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try { ImportAll(); Gallery(); } catch(Exception e){Debug.LogException(e);EditorUtility.DisplayDialog("制作失败",e.Message,"OK");}
        }
        [MenuItem("Island2019/2 - Build Island Combat Scene")]
        public static void BuildAdventure()
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try {ImportAll(); Adventure();} catch(Exception e){Debug.LogException(e);EditorUtility.DisplayDialog("制作失败",e.Message,"OK");}
        }
        [MenuItem("Island2019/3 - Open Assets Gallery")]
        public static void OpenGallery(){Open(GalleryPath);}
        [MenuItem("Island2019/4 - Open Island Combat")]
        public static void OpenAdventure(){Open(AdventurePath);}
        static void Open(string path)
        {
            if(!File.Exists(path)){EditorUtility.DisplayDialog("先生成场景","请先点击菜单 1 或 2，生成对应场景。","OK");return;}
            if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(path);
        }
        static Vector3 V(float[] p){return new Vector3(p[0],p[1],p[2]);}
        static T SaveAsset<T>(T asset,string path) where T:UnityEngine.Object
        {
            T existing=AssetDatabase.LoadAssetAtPath<T>(path);
            if(existing){EditorUtility.CopySerialized(asset,existing);UnityEngine.Object.DestroyImmediate(asset);return existing;}
            AssetDatabase.CreateAsset(asset,path);return asset;
        }
        static void ImportAll()
        {
            Directory.CreateDirectory(Generated+"/Meshes");Directory.CreateDirectory(Generated+"/Materials");Directory.CreateDirectory(Generated+"/Prefabs");
            AssetDatabase.Refresh();materials.Clear();
            foreach(string name in new[]{"Hero","Skeleton","Chest","Sword1","Sword2","Sword3"}) Import(name);
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        }
        static void Import(string name)
        {
            string path=Root+"/Models/"+name+".json";
            ModelData data=JsonUtility.FromJson<ModelData>(File.ReadAllText(path));
            foreach(MaterialData m in data.materials)
            {
                if(materials.ContainsKey(m.name)) continue;
                Shader shader=Shader.Find("Universal Render Pipeline/Lit");if(!shader) shader=Shader.Find("Standard");
                Material mat=new Material(shader){name=m.name};Color color=new Color(m.color[0],m.color[1],m.color[2],1);
                if(mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor",color);
                if(mat.HasProperty("_Color")) mat.SetColor("_Color",color);
                if(mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic",m.metallic);
                if(mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness",1-m.roughness);
                if(mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness",1-m.roughness);
                if(m.name=="Teal"){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*.35f);}
                // Thin modeled hat and runes are intentionally visible from both sides.
                if(mat.HasProperty("_Cull")) mat.SetFloat("_Cull",0);
                materials[m.name]=SaveAsset(mat,Generated+"/Materials/"+m.name+".mat");
            }
            List<GameObject> nodes=new List<GameObject>();
            for(int i=0;i<data.objects.Length;i++)
            {
                ObjectData o=data.objects[i];GameObject go=new GameObject(o.name);nodes.Add(go);
                if(o.parent>=0) go.transform.SetParent(nodes[o.parent].transform,false);
                go.transform.localPosition=V(o.position);go.transform.localRotation=new Quaternion(o.rotation[0],o.rotation[1],o.rotation[2],o.rotation[3]);
                if(o.vertices.Length==0) continue;
                Vector3[] verts=new Vector3[o.vertices.Length/3];
                for(int vi=0;vi<verts.Length;vi++) verts[vi]=new Vector3(o.vertices[vi*3],o.vertices[vi*3+1],o.vertices[vi*3+2]);
                Mesh mesh=new Mesh{name=name+"_"+i};mesh.vertices=verts;mesh.triangles=o.triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
                mesh=SaveAsset(mesh,Generated+"/Meshes/"+name+"_"+i+".asset");
                go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=materials[o.material];
            }
            PrefabUtility.SaveAsPrefabAsset(nodes[0],Generated+"/Prefabs/"+name+".prefab");UnityEngine.Object.DestroyImmediate(nodes[0]);
        }
        static GameObject Spawn(string name,Vector3 position,Transform parent=null)
        {
            GameObject prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Generated+"/Prefabs/"+name+".prefab");
            GameObject go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if(parent) go.transform.SetParent(parent,false);go.transform.position=position;return go;
        }
        static Transform Find(Transform root,string name)
        {foreach(Transform t in root.GetComponentsInChildren<Transform>(true)) if(t.name==name) return t;return null;}
        static GameObject Block(string name,Vector3 position,Vector3 scale,Material material,Transform parent=null)
        {
            GameObject go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.position=position;go.transform.localScale=scale;
            if(parent) go.transform.SetParent(parent,true);go.GetComponent<Renderer>().sharedMaterial=material;return go;
        }
        static Camera CameraAt(Vector3 position,Vector3 lookAt)
        {
            GameObject go=new GameObject("Quest Camera");go.tag="MainCamera";Camera camera=go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();camera.fieldOfView=45;camera.nearClipPlane=.1f;camera.farClipPlane=1800;
            camera.clearFlags=CameraClearFlags.Skybox;camera.targetDisplay=0;go.transform.position=position;go.transform.LookAt(lookAt);return camera;
        }
        static void LightAt(string name,Vector3 rotation,Color color,float intensity)
        {
            GameObject go=new GameObject(name);Light l=go.AddComponent<Light>();l.type=LightType.Directional;l.color=color;l.intensity=intensity;
            l.shadows=LightShadows.Soft;go.transform.eulerAngles=rotation;
        }
        static void Label(string text,Vector3 position,float size)
        {
            GameObject go=new GameObject(text);go.transform.position=position;go.transform.rotation=Quaternion.Euler(0,180,0);
            TextMesh tm=go.AddComponent<TextMesh>();tm.text=text;tm.characterSize=size;tm.fontSize=80;tm.anchor=TextAnchor.MiddleCenter;tm.color=new Color(.75f,.91f,.97f);
            // Size by measured world width; long labels must not cover the models.
            Renderer renderer=go.GetComponent<Renderer>();
            float maxWidth=text=="TIDEWARD RELICS" ? 4.2f : text=="UPGRADE CHEST" ? 1.5f : 1.2f;
            if(renderer && renderer.bounds.size.x>maxWidth) go.transform.localScale*=maxWidth/renderer.bounds.size.x;
        }
        static void Gallery()
        {
            BackupScene(GalleryPath);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.3f,.37f,.45f);
            LightAt("Key Light",new Vector3(40,-30,0),new Color(.8f,.91f,1),1.1f);
            LightAt("Warm Rim",new Vector3(30,150,0),new Color(1,.8f,.5f),.7f);
            Block("Display Stage",new Vector3(0,-.15f,0),new Vector3(7,.25f,3.6f),materials["Stone"]);
            Spawn("Hero",new Vector3(-2,0,0));Spawn("Skeleton",new Vector3(0,0,0));
            Spawn("Chest",new Vector3(2.15f,.55f,0));Block("Chest Pedestal",new Vector3(2.15f,.25f,0),new Vector3(1.5f,.55f,1.1f),materials["Navy"]);
            for(int i=0;i<3;i++)
            {Spawn("Sword"+(i+1),new Vector3(1.5f+i*.6f,.16f,1.2f));Block("Sword Stand",new Vector3(1.5f+i*.6f,.09f,1.2f),new Vector3(.36f,.16f,.3f),materials["Gold"]);}
            Label("TIDEWARD RELICS",new Vector3(0,2.8f,-.15f),.04f);
            Label("HERO",new Vector3(-2,.15f,1.15f),.065f);Label("SKELETON",new Vector3(0,.15f,1.15f),.065f);
            Label("UPGRADE CHEST",new Vector3(2.1f,1.65f,0),.055f);
            Camera cam=CameraAt(new Vector3(3,2.8f,8),new Vector3(0,1,0));cam.backgroundColor=new Color(.035f,.065f,.09f);cam.clearFlags=CameraClearFlags.SolidColor;
            cam.gameObject.AddComponent<GalleryCamera>();new GameObject("Gallery Controls").AddComponent<QuestGame>().showcase=true;
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),GalleryPath);RegisterScenes();Selection.activeGameObject=cam.gameObject;
            EditorUtility.DisplayDialog("资产展示场景已生成","点击 Play 浏览成品。拖动鼠标旋转，滚轮缩放，F8 截图。\n预制体保存在 Assets/IslandQuest2019/Generated/Prefabs。","OK");
        }
        static Vector3 IslandAnchor()
        {
            // Prefer a dry coastal patch; the terrace makes play independent of the old boat NavMesh.
            Terrain[] terrains=Terrain.activeTerrains;Vector3 best=new Vector3(0,12,0);float bestScore=float.MaxValue;
            foreach(Terrain terrain in terrains)
            {
                TerrainData data=terrain.terrainData;Vector3 origin=terrain.transform.position;
                for(float x=24;x<data.size.x-24;x+=16)
                for(float z=24;z<data.size.z-24;z+=16)
                {
                    float low=float.MaxValue,high=float.MinValue;
                    for(int sx=-1;sx<=1;sx++) for(int sz=-1;sz<=1;sz++)
                    {
                        float h=terrain.SampleHeight(origin+new Vector3(x+sx*14,0,z+sz*12))+origin.y;
                        low=Mathf.Min(low,h);high=Mathf.Max(high,h);
                    }
                    if(low<3 || high>32) continue;
                    float score=Mathf.Abs(high-9)+(high-low)*3;
                    if(score<bestScore){bestScore=score;best=origin+new Vector3(x,0,z);best.y=high+1.5f;}
                }
            }
            return best;
        }
        static Vector3 ClearTerraceAnchor()
        {
            Vector3 p=IslandAnchor();float top=p.y;
            // Terrain samples include every part of the terrace and the initial camera path.
            foreach(Terrain terrain in Terrain.activeTerrains)
            {
                TerrainData data=terrain.terrainData;Vector3 origin=terrain.transform.position;
                for(float dx=-20;dx<=20;dx+=2) for(float dz=-20;dz<=20;dz+=2)
                {
                    Vector3 sample=p+new Vector3(dx,0,dz);
                    if(sample.x>=origin.x && sample.z>=origin.z && sample.x<=origin.x+data.size.x && sample.z<=origin.z+data.size.z)
                        top=Mathf.Max(top,terrain.SampleHeight(sample)+origin.y);
                }
                foreach(TreeInstance tree in data.treeInstances)
                {
                    Vector3 point=origin+Vector3.Scale(tree.position,data.size);
                    TreePrototype[] prototypes=data.treePrototypes;
                    if(tree.prototypeIndex<0 || tree.prototypeIndex>=prototypes.Length) continue;
                    GameObject prefab=prototypes[tree.prototypeIndex].prefab;
                    if(!prefab) continue;
                    float height=0,radius=0;
                    foreach(Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
                    {height=Mathf.Max(height,renderer.bounds.max.y);radius=Mathf.Max(radius,renderer.bounds.extents.x,renderer.bounds.extents.z);}
                    if(Mathf.Abs(point.x-p.x)<=20+radius*tree.widthScale && Mathf.Abs(point.z-p.z)<=20+radius*tree.widthScale)
                        top=Mathf.Max(top,point.y+Mathf.Max(height,12)*tree.heightScale);
                }
            }
            foreach(MeshRenderer renderer in UnityEngine.Object.FindObjectsOfType<MeshRenderer>())
            {
                string name=renderer.name.ToLowerInvariant();
                string layer=LayerMask.LayerToName(renderer.gameObject.layer);
                if(layer=="3DSkybox" || layer=="Clouds" || name.Contains("water") || name.Contains("sky") || name.Contains("cloud") || name.Contains("foam")) continue;
                Bounds bounds=renderer.bounds;
                if(bounds.min.x<=p.x+20 && bounds.max.x>=p.x-20 && bounds.min.z<=p.z+20 && bounds.max.z>=p.z-20)
                    top=Mathf.Max(top,bounds.max.y);
            }
            p.y=top+3;return p;
        }
        static void BackupScene(string path)
        {
            if(!File.Exists(path)) return;
            string backup=Root+"/SceneBackups/"+Path.GetFileNameWithoutExtension(path)+"_"+DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")+".unity";
            Directory.CreateDirectory(Root+"/SceneBackups");AssetDatabase.Refresh();AssetDatabase.CopyAsset(path,backup);
        }
        static QuestPose AddPose(GameObject actor,Transform visual)
        {
            QuestPose pose=actor.AddComponent<QuestPose>();pose.leftArm=Find(visual,"LeftArm");pose.rightArm=Find(visual,"RightArm");
            pose.leftLeg=Find(visual,"LeftLeg");pose.rightLeg=Find(visual,"RightLeg");return pose;
        }
        static CharacterController Controller(GameObject go,float height,float radius)
        {
            CharacterController c=go.AddComponent<CharacterController>();c.height=height;c.radius=radius;c.center=Vector3.up*(height*.5f);
            c.stepOffset=.3f;c.slopeLimit=45;c.skinWidth=.04f;return c;
        }
        static void Adventure()
        {
            const string clean="Assets/scenes/island_clean_2019.unity";
            if(!File.Exists(clean)) throw new FileNotFoundException("先打开/安装去掉船和浮标的 2019 海岛工程。",clean);
            BackupScene(AdventurePath);
            EditorSceneManager.OpenScene(clean);EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),AdventurePath);
            foreach(Camera c in UnityEngine.Object.FindObjectsOfType<Camera>()) c.enabled=false;
            foreach(AudioListener a in UnityEngine.Object.FindObjectsOfType<AudioListener>()) a.enabled=false;
            Vector3 p=ClearTerraceAnchor();GameObject arena=new GameObject("Original Skeleton Trial Terrace");
            Block("Stone Trial Terrace",p-Vector3.up*.5f,new Vector3(28,1,24),materials["Stone"],arena.transform);
            for(int x=-12;x<=12;x+=4) for(int z=-10;z<=10;z+=4)
                Block("Terrace Tile",p+new Vector3(x,.015f,z),new Vector3(3.94f,.035f,3.94f),materials[((x+z)%8)==0 ? "Navy" : "Stone"],arena.transform);
            // Low parapets keep combat on the terrace; the whole island remains in view.
            Block("North Parapet",p+new Vector3(0,.55f,11.7f),new Vector3(28,1.1f,.35f),materials["Navy"],arena.transform);
            Block("South Parapet",p+new Vector3(0,.55f,-11.7f),new Vector3(28,1.1f,.35f),materials["Navy"],arena.transform);
            Block("West Parapet",p+new Vector3(-13.7f,.55f,0),new Vector3(.35f,1.1f,24),materials["Navy"],arena.transform);
            Block("East Parapet",p+new Vector3(13.7f,.55f,0),new Vector3(.35f,1.1f,24),materials["Navy"],arena.transform);
            QuestGame game=new GameObject("Skeleton Trial").AddComponent<QuestGame>();
            GameObject player=new GameObject("Player - Tideward Swordbearer");player.transform.position=p+new Vector3(0,.12f,-7);
            player.transform.SetParent(arena.transform,true);
            Controller(player,2,.32f);GameObject visual=Spawn("Hero",player.transform.position,player.transform);AddPose(player,visual.transform);
            QuestHero hero=player.AddComponent<QuestHero>();hero.visual=visual.transform;hero.game=game;game.hero=hero;
            hero.weaponMount=Find(visual.transform,"WeaponMount");Transform held=Find(visual.transform,"HeldSword");if(held) held.gameObject.SetActive(false);
            hero.swords=new GameObject[3];
            for(int i=0;i<3;i++)
            {hero.swords[i]=Spawn("Sword"+(i+1),Vector3.zero,hero.weaponMount);hero.swords[i].transform.localPosition=Vector3.zero;hero.swords[i].transform.localRotation=Quaternion.identity;hero.swords[i].SetActive(i==0);}
            Camera cam=CameraAt(player.transform.position+new Vector3(0,3,-5),player.transform.position+Vector3.up);
            QuestCamera follow=cam.gameObject.AddComponent<QuestCamera>();follow.target=player.transform;follow.yaw=0;hero.view=follow;
            Vector3[] positions={new Vector3(-6,0,-1),new Vector3(6,0,0),new Vector3(-8,0,6),new Vector3(1,0,5),new Vector3(8,0,7),new Vector3(-1,0,10)};
            for(int i=0;i<positions.Length;i++)
            {
                GameObject enemy=new GameObject("Skeleton Guard "+(i+1));enemy.transform.position=p+positions[i]+Vector3.up*.12f;
                enemy.transform.SetParent(arena.transform,true);
                Controller(enemy,1.95f,.28f);GameObject model=Spawn("Skeleton",enemy.transform.position,enemy.transform);AddPose(enemy,model.transform);
                QuestSkeleton ai=enemy.AddComponent<QuestSkeleton>();ai.hero=hero;ai.game=game;ai.health=i<2 ? 54 : i<4 ? 76 : 100;
            }
            for(int i=0;i<2;i++)
            {
                GameObject chest=Spawn("Chest",p+new Vector3(i==0 ? -10 : 10,.08f,3));
                chest.transform.SetParent(arena.transform,true);
                BoxCollider b=chest.AddComponent<BoxCollider>();b.size=new Vector3(1.2f,.9f,.8f);b.center=Vector3.up*.45f;
                QuestChest upgrade=chest.AddComponent<QuestChest>();upgrade.lid=Find(chest.transform,"LidPivot");upgrade.game=game;upgrade.requiredKills=(i+1)*2;
            }
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight=new Color(.42f,.46f,.52f);
            LightAt("Trial Fill Light",new Vector3(35,-25,0),new Color(.85f,.92f,1),.65f);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),AdventurePath);RegisterScenes();Selection.activeGameObject=player;
            EditorUtility.DisplayDialog("第三人称战斗场景已生成","点击 Play，再点击游戏画面开始。WASD 移动，鼠标转镜头，左键挥剑，E 开宝箱。\n先击败 2 / 4 只骷髅，分别升级武器。\n原静态海岛场景保留，旧战斗场景有备份。","OK");
        }
        static void RegisterScenes()
        {
            List<EditorBuildSettingsScene> scenes=new List<EditorBuildSettingsScene>();
            foreach(string path in new[]{AdventurePath,GalleryPath,"Assets/scenes/island_clean_2019.unity"})
                if(File.Exists(path)) scenes.Add(new EditorBuildSettingsScene(path,true));
            EditorBuildSettings.scenes=scenes.ToArray();
        }
    }
}
