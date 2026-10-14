using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

namespace IslandQuest2019.Portfolio.Editor
{
    [Serializable] public class StudioMaterial { public string name,texture;public float[] color;public float metallic,roughness; }
    [Serializable] public class StudioBone {public string name;public int parent;public float[] head,tail;}
    [Serializable] public class StudioModel
    {
        public string name;public StudioMaterial[] materials;public StudioBone[] bones;
        public float[] vertices,uv,weight0;public int[] triangles,triangleMaterials,bone0,bone1;
    }
    public static class PortfolioBuilder
    {
        const string Root="Assets/IslandQuest2019/Portfolio";
        const string Output=Root+"/Generated";
        const string ScenePath="Assets/scenes/quest_portfolio_2019.unity";
        static Dictionary<string,Material> materials;
        static Vector3 V(float[] p){return new Vector3(p[0],p[1],p[2]);}
        static T Save<T>(T obj,string path) where T:UnityEngine.Object
        {
            T old=AssetDatabase.LoadAssetAtPath<T>(path);
            if(old){EditorUtility.CopySerialized(obj,old);UnityEngine.Object.DestroyImmediate(obj);return old;}
            AssetDatabase.CreateAsset(obj,path);return obj;
        }
        [MenuItem("Island2019/6 - Build Assignment Studio")]
        public static void Build()
        {
            if(Application.isPlaying){EditorUtility.DisplayDialog("请先停止运行","点击蓝色三角按钮停止 Play，再生成展示场景。","OK");return;}
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            try
            {
                Directory.CreateDirectory(Output+"/Meshes");Directory.CreateDirectory(Output+"/Materials");Directory.CreateDirectory(Output+"/Prefabs");AssetDatabase.Refresh();
                ImportTextures();materials=new Dictionary<string,Material>();
                foreach(string name in new[]{"Hero","Skeleton","Chest","Sword1","Sword2","Sword3"})Import(name);
                if(File.Exists(ScenePath))
                {
                    Directory.CreateDirectory(Root+"/SceneBackups");AssetDatabase.Refresh();
                    if(!AssetDatabase.CopyAsset(ScenePath,Root+"/SceneBackups/portfolio_"+DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")+".unity"))throw new IOException("无法备份旧展示场景，已停止。");
                }
                Studio();AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("作业展示场景已生成","点击 Play。下方 0–4 切换全景与特写，点击“保存全部 5 张成品图”。\n骨骼演示按钮可预览实际蒙皮动作。截图不需要打怪。\n菜单 7 也可以直接导出 2200×1600 成品图。","OK");
            }
            catch(Exception e){Debug.LogException(e);EditorUtility.DisplayDialog("制作未完成",e.Message,"OK");}
        }
        static void ImportTextures()
        {
            foreach(string path in Directory.GetFiles(Root+"/Textures","*.png"))
            {
                TextureImporter importer=AssetImporter.GetAtPath(path) as TextureImporter;if(!importer)continue;
                bool normal=path.Contains("_Normal");importer.textureType=normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture=!normal;importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;
                importer.mipmapEnabled=true;importer.anisoLevel=4;importer.maxTextureSize=512;importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }
        static Material GetMaterial(StudioMaterial m)
        {
            if(materials.ContainsKey(m.name))return materials[m.name];
            Shader shader=Shader.Find("Universal Render Pipeline/Lit");if(!shader)shader=Shader.Find("Standard");
            if(!shader)throw new Exception("没有找到 URP Lit 或 Standard 材质着色器。");
            Material mat=new Material(shader){name=m.name};Color color=new Color(m.color[0],m.color[1],m.color[2],1);
            if(mat.HasProperty("_BaseColor"))mat.SetColor("_BaseColor",color);if(mat.HasProperty("_Color"))mat.SetColor("_Color",color);
            mat.SetFloat("_Metallic",m.metallic);
            if(mat.HasProperty("_Smoothness"))mat.SetFloat("_Smoothness",1-m.roughness);
            if(mat.HasProperty("_Glossiness"))mat.SetFloat("_Glossiness",1-m.roughness);
            if(!string.IsNullOrEmpty(m.texture))
            {
                Texture2D tex=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+m.texture+"_Color.png");
                if(mat.HasProperty("_BaseMap"))mat.SetTexture("_BaseMap",tex);if(mat.HasProperty("_MainTex"))mat.SetTexture("_MainTex",tex);
                mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+m.texture+"_Normal.png"));
                mat.SetFloat("_BumpScale",.32f);mat.EnableKeyword("_NORMALMAP");
            }
            if(m.name=="Teal"){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*.35f);}
            if(mat.HasProperty("_Cull"))mat.SetFloat("_Cull",0);
            materials[m.name]=Save(mat,Output+"/Materials/"+m.name+".mat");return materials[m.name];
        }
        static void Import(string name)
        {
            StudioModel model=JsonUtility.FromJson<StudioModel>(File.ReadAllText(Root+"/Models/"+name+".json"));
            GameObject root=new GameObject(name+" - Weighted Asset");
            try
            {
                Material[] mats=new Material[model.materials.Length];for(int i=0;i<mats.Length;i++)mats[i]=GetMaterial(model.materials[i]);
                Transform[] bones=new Transform[model.bones.Length];Matrix4x4[] bind=new Matrix4x4[bones.Length];Vector3[] tails=new Vector3[bones.Length];
                for(int i=0;i<bones.Length;i++)
                {
                    StudioBone b=model.bones[i];Transform t=new GameObject(b.name).transform;t.SetParent(b.parent<0?root.transform:bones[b.parent],false);
                    t.position=V(b.head);bones[i]=t;bind[i]=t.worldToLocalMatrix*root.transform.localToWorldMatrix;tails[i]=V(b.tail)-V(b.head);
                }
                Vector3[] verts=new Vector3[model.vertices.Length/3];Vector2[] uv=new Vector2[verts.Length];BoneWeight[] weights=new BoneWeight[verts.Length];
                for(int i=0;i<verts.Length;i++)
                {
                    verts[i]=new Vector3(model.vertices[i*3],model.vertices[i*3+1],model.vertices[i*3+2]);uv[i]=new Vector2(model.uv[i*2],model.uv[i*2+1]);
                    weights[i]=new BoneWeight{boneIndex0=model.bone0[i],boneIndex1=model.bone1[i],weight0=model.weight0[i],weight1=1-model.weight0[i]};
                }
                Mesh mesh=new Mesh{name=name+"_UV_WeightedSkin"};mesh.vertices=verts;mesh.uv=uv;mesh.boneWeights=weights;mesh.bindposes=bind;mesh.subMeshCount=mats.Length;
                for(int mi=0;mi<mats.Length;mi++)
                {
                    List<int> tris=new List<int>();for(int t=0;t<model.triangleMaterials.Length;t++)if(model.triangleMaterials[t]==mi)
                    {tris.Add(model.triangles[t*3]);tris.Add(model.triangles[t*3+1]);tris.Add(model.triangles[t*3+2]);}
                    mesh.SetTriangles(tris,mi);
                }
                mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();mesh=Save(mesh,Output+"/Meshes/"+name+".asset");
                SkinnedMeshRenderer skin=root.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=mesh;skin.sharedMaterials=mats;skin.bones=bones;skin.rootBone=bones[0];
                skin.quality=SkinQuality.Bone2;skin.updateWhenOffscreen=true;skin.localBounds=new Bounds(new Vector3(0,1,0),new Vector3(4,4,4));
                PortfolioRig rig=root.AddComponent<PortfolioRig>();rig.bones=bones;rig.tails=tails;rig.assetName=name;
                ValidateSkin(skin,rig,name);
                PrefabUtility.SaveAsPrefabAsset(root,Output+"/Prefabs/"+name+".prefab");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        static void ValidateSkin(SkinnedMeshRenderer skin,PortfolioRig rig,string name)
        {
            if(name.StartsWith("Sword"))return;
            Mesh rest=new Mesh(),posed=new Mesh();
            try
            {
                rig.Pose(0);skin.BakeMesh(rest);rig.Pose(.7f);skin.BakeMesh(posed);
                Vector3[] a=rest.vertices,b=posed.vertices;int moved=0;
                if(a.Length!=b.Length)throw new Exception(name+" 蒙皮顶点数量不一致。");
                for(int i=0;i<a.Length;i++)if((a[i]-b[i]).sqrMagnitude>.00000001f)moved++;
                if(moved==0)throw new Exception(name+" 骨骼未能驱动网格，已停止生成。");
                Debug.Log("Portfolio skin check: "+name+" / "+rig.bones.Length+" bones / "+moved+" vertices moved.");
            }
            finally{rig.Pose(0);UnityEngine.Object.DestroyImmediate(rest);UnityEngine.Object.DestroyImmediate(posed);}
        }
        static GameObject Spawn(string name,Vector3 position)
        {
            GameObject go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Output+"/Prefabs/"+name+".prefab"));go.transform.position=position;return go;
        }
        static GameObject Box(string name,Vector3 position,Vector3 scale,Material material)
        {
            GameObject go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.position=position;go.transform.localScale=scale;
            go.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }
        static void Light(string name,Vector3 rotation,Color color,float intensity,bool shadows)
        {
            Light l=new GameObject(name).AddComponent<Light>();l.type=LightType.Directional;l.color=color;l.intensity=intensity;l.transform.eulerAngles=rotation;
            l.shadows=shadows ? LightShadows.Soft : LightShadows.None;l.shadowBias=.06f;l.shadowNormalBias=.35f;l.shadowStrength=.65f;
        }
        static void Reflection()
        {
            // A small authored studio environment for metals, independent of the island skybox.
            const int n=64;Cubemap cube=new Cubemap(n,TextureFormat.RGBA32,true);
            for(int f=0;f<6;f++)
            {
                Color[] pixels=new Color[n*n];
                for(int y=0;y<n;y++)for(int x=0;x<n;x++)
                {
                    float u=(x+.5f)/n*2-1,v=(y+.5f)/n*2-1;Vector3 d;
                    switch(f){case 0:d=new Vector3(1,-v,-u);break;case 1:d=new Vector3(-1,-v,u);break;case 2:d=new Vector3(u,1,v);break;case 3:d=new Vector3(u,-1,-v);break;case 4:d=new Vector3(u,-v,1);break;default:d=new Vector3(-u,-v,-1);break;}
                    d.Normalize();Color c=Color.Lerp(new Color(.12f,.17f,.22f),new Color(.6f,.73f,.84f),d.y*.5f+.5f);
                    float soft=Mathf.Pow(Mathf.Max(0,Vector3.Dot(d,new Vector3(-.6f,.6f,.5f).normalized)),24);
                    pixels[y*n+x]=Color.Lerp(c,new Color(1,.95f,.85f),soft);
                }
                cube.SetPixels(pixels,(CubemapFace)f);
            }
            cube.Apply(true,false);RenderSettings.defaultReflectionMode=DefaultReflectionMode.Custom;RenderSettings.customReflection=Save(cube,Output+"/StudioReflection.cubemap");RenderSettings.reflectionIntensity=1;
        }
        static void Studio()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.fog=false;RenderSettings.skybox=null;RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.65f,.74f,.83f);RenderSettings.ambientEquatorColor=new Color(.42f,.49f,.55f);RenderSettings.ambientGroundColor=new Color(.22f,.27f,.31f);RenderSettings.ambientIntensity=1;
            Reflection();Light("Soft Key",new Vector3(38,150,0),new Color(.88f,.95f,1),1.35f,true);
            Light("Warm Fill",new Vector3(24,215,0),new Color(1,.85f,.69f),.65f,false);
            Light("Cyan Rim",new Vector3(30,-20,0),new Color(.58f,.94f,1),1,false);
            Box("Studio Floor",new Vector3(0,-.27f,0),new Vector3(100,.2f,100),materials["Stone"]);
            GameObject[] stands=new GameObject[6];
            stands[0]=Box("Hero Plinth",new Vector3(-2,-.07f,0),new Vector3(1.4f,.14f,1.25f),materials["Navy"]);
            stands[1]=Box("Skeleton Plinth",new Vector3(0,-.07f,0),new Vector3(1.4f,.14f,1.25f),materials["Navy"]);
            stands[2]=Box("Chest Plinth",new Vector3(2.1f,.055f,0),new Vector3(1.65f,.39f,1.25f),materials["Navy"]);
            GameObject[] models={Spawn("Hero",new Vector3(-2,0,0)),Spawn("Skeleton",Vector3.zero),Spawn("Chest",new Vector3(2.1f,.25f,0)),Spawn("Sword1",new Vector3(1.5f,.08f,1.15f)),Spawn("Sword2",new Vector3(2.1f,.08f,1.15f)),Spawn("Sword3",new Vector3(2.7f,.08f,1.15f))};
            for(int i=0;i<3;i++)stands[i+3]=Box("Sword Plinth",new Vector3(1.5f+i*.6f,-.03f,1.15f),new Vector3(.42f,.16f,.45f),materials["Gold"]);
            Camera cam=new GameObject("Assignment Studio Camera").AddComponent<Camera>();cam.tag="MainCamera";cam.gameObject.AddComponent<AudioListener>();cam.fieldOfView=42;cam.nearClipPlane=.05f;cam.farClipPlane=200;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.08f,.13f,.18f);cam.allowHDR=true;cam.allowMSAA=true;cam.targetDisplay=0;
            PortfolioShowcase show=new GameObject("Assignment Capture Controls").AddComponent<PortfolioShowcase>();show.studioCamera=cam;show.stands=stands;show.assets=new Transform[models.Length];show.rigs=new PortfolioRig[models.Length];
            Material rigMat=new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));rigMat.color=new Color(.15f,.95f,.85f);if(rigMat.HasProperty("_BaseColor"))rigMat.SetColor("_BaseColor",rigMat.color);
            rigMat=Save(rigMat,Output+"/Materials/RigOverlay.mat");
            for(int i=0;i<models.Length;i++)
            {
                show.assets[i]=models[i].transform;PortfolioRig rig=models[i].GetComponent<PortfolioRig>();show.rigs[i]=rig;
                if(i>1)continue;rig.boneMarkers=new Transform[rig.bones.Length];
                for(int b=0;b<rig.bones.Length;b++)
                {
                    GameObject mark=GameObject.CreatePrimitive(PrimitiveType.Cylinder);mark.name="Bone display - "+rig.bones[b].name;UnityEngine.Object.DestroyImmediate(mark.GetComponent<Collider>());mark.GetComponent<Renderer>().sharedMaterial=rigMat;
                    mark.transform.SetParent(models[i].transform,false);mark.SetActive(false);rig.boneMarkers[b]=mark.transform;
                }
            }
            show.SetView(0);EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),ScenePath);Selection.activeGameObject=show.gameObject;
        }
        [MenuItem("Island2019/7 - Export Assignment PNGs")]
        public static void Export()
        {
            if(Application.isPlaying){EditorUtility.DisplayDialog("使用游戏内截图按钮","Play 时请点击下方“保存全部 5 张成品图”。","OK");return;}
            PortfolioShowcase show=UnityEngine.Object.FindObjectOfType<PortfolioShowcase>();
            if(!show){EditorUtility.DisplayDialog("请先生成展示场景","先选择菜单 6 - Build Assignment Studio。","OK");return;}
            int oldView=show.view;Camera camera=show.studioCamera;RenderTexture old=camera.targetTexture,active=RenderTexture.active;
            string folder=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Screenshots/Assignment_"+DateTime.Now.ToString("yyyyMMdd_HHmmss"));Directory.CreateDirectory(folder);
            RenderTexture rt=new RenderTexture(2200,1600,24,RenderTextureFormat.ARGB32);rt.antiAliasing=4;rt.Create();Texture2D image=new Texture2D(2200,1600,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=rt;
                for(int i=0;i<5;i++)
                {
                    EditorUtility.DisplayProgressBar("导出成品图",PortfolioShowcase.Files[i],i/5f);show.SetView(i);camera.Render();RenderTexture.active=rt;
                    image.ReadPixels(new Rect(0,0,2200,1600),0,0);image.Apply();
                    Color32[] pixels=image.GetPixels32();int low=255,high=0;
                    for(int p=0;p<pixels.Length;p+=97){int value=(pixels[p].r+pixels[p].g+pixels[p].b)/3;low=Mathf.Min(low,value);high=Mathf.Max(high,value);}
                    if(high-low<12)throw new Exception("截图为空白或没有有效画面，请使用 Play 中的截图按钮。");
                    File.WriteAllBytes(Path.Combine(folder,PortfolioShowcase.Files[i]+".png"),image.EncodeToPNG());
                }
                EditorUtility.RevealInFinder(folder);EditorUtility.DisplayDialog("已导出 5 张 PNG","请检查图片清晰、模型完整，再提交。\n"+folder,"OK");
            }
            catch(Exception e){Debug.LogException(e);EditorUtility.DisplayDialog("导出未完成","请改用 Play 后下方的截图按钮。\n"+e.Message,"OK");}
            finally{camera.targetTexture=old;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);show.SetView(oldView);EditorUtility.ClearProgressBar();}
        }
    }
}
