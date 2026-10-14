using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace IslandQuest2019.Portfolio
{
    // A standalone asset studio. No enemies, player health or win condition are involved.
    public class PortfolioShowcase : MonoBehaviour
    {
        public Camera studioCamera;
        public Transform[] assets;
        public GameObject[] stands;
        public PortfolioRig[] rigs;
        public int view;
        public bool animate, showRig, showInterface=true;
        public string lastCapture;
        float yaw=16,pitch=12,distance=8;
        Vector3 focus=new Vector3(0,1.15f,0);
        bool capturing;
        GUIStyle title,body,button;
        static readonly string[] Names={"资产全景","主角 · 布料与金属","骷髅 · 骨质与结构","武器与升级宝箱","骨骼与蒙皮"};
        public static readonly string[] Files={"01_Collection","02_Hero","03_Skeleton","04_Weapons_Chest","05_Rig_Skinning"};
        void Start(){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;SetView(0);}
        public void SetView(int index)
        {
            view=Mathf.Clamp(index,0,4);showRig=view==4;animate=false;
            focus=new[]{new Vector3(0,1.15f,0),new Vector3(-2,1.13f,0),new Vector3(0,1.02f,0),new Vector3(2.05f,.8f,.5f),new Vector3(-1,1.06f,0)}[view];
            distance=new[]{8.6f,3.9f,3.65f,4.65f,5.6f}[view];yaw=view==4 ? 0 : view==0 ? 12 : 18;pitch=view==3 ? 19 : 10;
            for(int i=0;i<assets.Length;i++)
            {
                bool visible=view==0 || (view==1 && i==0) || (view==2 && i==1) || (view==3 && i>=2) || (view==4 && i<2);
                assets[i].gameObject.SetActive(visible);
                if(stands!=null && i<stands.Length && stands[i])stands[i].SetActive(visible);
            }
            ApplyPose(0);PlaceCamera();
        }
        void Update()
        {
            if(capturing)return;
            if(QuestInput.Pressed(KeyCode.Alpha0))SetView(0);
            if(QuestInput.Pressed(KeyCode.Alpha1))SetView(1);
            if(QuestInput.Pressed(KeyCode.Alpha2))SetView(2);
            if(QuestInput.Pressed(KeyCode.Alpha3))SetView(3);
            if(QuestInput.Pressed(KeyCode.Alpha4))SetView(4);
            if(QuestInput.Pressed(KeyCode.Space))animate=!animate;
            if(QuestInput.Pressed(KeyCode.F1))showInterface=!showInterface;
            if(QuestInput.Pressed(KeyCode.F8))StartCoroutine(Capture(false));
            // Right mouse avoids orbiting while clicking the toolbar.
            if(QuestInput.MouseHeld(1)){yaw+=QuestInput.Look.x*2;pitch=Mathf.Clamp(pitch-QuestInput.Look.y*2,-10,65);}
            distance=Mathf.Clamp(distance-QuestInput.Scroll*3,2.4f,13);
            PlaceCamera();ApplyPose(animate ? Time.time : 0);
        }
        public void PlaceCamera()
        {
            studioCamera.transform.position=focus+Quaternion.Euler(-pitch,yaw,0)*Vector3.forward*distance;
            studioCamera.transform.LookAt(focus);
        }
        void ApplyPose(float time)
        {
            foreach(PortfolioRig rig in rigs) if(rig && rig.gameObject.activeInHierarchy){rig.Pose(time);rig.DrawBones(showRig,studioCamera);}
        }
        IEnumerator Capture(bool all)
        {
            capturing=true;bool ui=showInterface;int previous=view;bool previousAnim=animate;
            showInterface=false;animate=false;
            string folder=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Screenshots/Portfolio_"+DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(folder);
            int start=all ? 0 : view,end=all ? 4 : view;
            for(int i=start;i<=end;i++)
            {
                if(all)SetView(i);ApplyPose(0);
                yield return null;yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(folder,Files[i]+".png"),2);
                yield return null;yield return new WaitForEndOfFrame();yield return null;
            }
            if(all)SetView(previous);animate=previousAnim;showInterface=ui;capturing=false;lastCapture=folder;
            Debug.Log("作业成品截图已请求保存到："+folder+"。请打开文件夹检查 PNG 已写入。 ");
        }
        void OnGUI()
        {
            if(!showInterface || capturing)return;
            if(title==null)
            {
                Font font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","SimHei","Noto Sans CJK SC","Arial"},22);
                title=new GUIStyle(GUI.skin.label){font=font,fontSize=24,fontStyle=FontStyle.Bold};title.normal.textColor=new Color(.78f,.96f,1);
                body=new GUIStyle(GUI.skin.label){font=font,fontSize=16,wordWrap=true};body.normal.textColor=new Color(.85f,.91f,.94f);
                button=new GUIStyle(GUI.skin.button){font=font,fontSize=15};
            }
            float scale=Mathf.Max(.65f,Mathf.Min(Screen.width/1280f,Screen.height/800f));Matrix4x4 old=GUI.matrix;GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
            float width=Screen.width/scale,height=Screen.height/scale;
            GUI.Box(new Rect(18,18,500,85),GUIContent.none);GUI.Label(new Rect(34,26,470,34),"潮岸遗物 / 三维数字资产",title);
            GUI.Label(new Rect(34,64,470,30),Names[view]+"   ·   UV 贴图 / 光照 / 骨骼蒙皮",body);
            for(int i=0;i<5;i++)if(GUI.Button(new Rect(20+i*185,height-115,180,35),i+"  "+Names[i],button))SetView(i);
            if(GUI.Button(new Rect(20,height-72,180,35),animate?"暂停骨骼演示":"播放骨骼演示",button))animate=!animate;
            if(GUI.Button(new Rect(210,height-72,210,35),"保存全部 5 张成品图",button))StartCoroutine(Capture(true));
            GUI.Label(new Rect(440,height-72,width-450,48),"右键拖动：旋转   滚轮：缩放   F1：隐藏界面   F8：保存当前图\n截图自动隐藏界面；文件在工程 Screenshots 文件夹。",body);
            if(!string.IsNullOrEmpty(lastCapture))GUI.Label(new Rect(20,110,width-40,45),"截图文件夹："+lastCapture,body);
            GUI.matrix=old;
        }
    }
}
