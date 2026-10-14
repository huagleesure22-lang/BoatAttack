using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IslandQuest2019
{
    public class QuestGame : MonoBehaviour
    {
        public QuestHero hero;
        public int totalEnemies = 6;
        public int kills;
        public bool showcase;
        public bool showHUD = true;
        public string message = "";
        float messageUntil;
        GUIStyle title, body, hint;
        Font font;
        public bool Finished { get { return kills >= totalEnemies; } }

        void Awake()
        {
            // Support the fonts present on Chinese and English Windows machines.
            string[] candidates = { "Microsoft YaHei", "SimHei", "Noto Sans CJK SC", "Arial" };
            font = Font.CreateDynamicFontFromOSFont(candidates, 20);
        }
        public void Notice(string text) { message = text; messageUntil = Time.unscaledTime + 4; }
        public void EnemyDefeated() { kills++; Notice("击败骷髅 / Skeleton defeated  " + kills + " / " + totalEnemies); }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1)) showHUD = !showHUD;
            if (Input.GetKeyDown(KeyCode.F8)) StartCoroutine(Capture());
            if (hero && hero.health <= 0 && Input.GetKeyDown(KeyCode.R)) SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        System.Collections.IEnumerator Capture()
        {
            bool before = showHUD; showHUD = false;
            yield return new WaitForEndOfFrame();
            string dir = Path.Combine(Application.dataPath, "../Screenshots"); Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "IslandQuest_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".png");
            ScreenCapture.CaptureScreenshot(path, 2);
            yield return null;
            showHUD = before; Notice("截图已保存 / Screenshot saved: Screenshots");
        }
        void Styles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { font = font, fontSize = 28, fontStyle = FontStyle.Bold };
            title.normal.textColor = new Color(.65f,.95f,1);
            body = new GUIStyle(GUI.skin.label) { font = font, fontSize = 18, wordWrap = true };
            body.normal.textColor = Color.white;
            hint = new GUIStyle(body) { alignment = TextAnchor.MiddleCenter, fontSize = 22 };
        }
        void OnGUI()
        {
            if (!showHUD) return; Styles();
            Matrix4x4 previousMatrix = GUI.matrix;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
            float width = Screen.width / scale, height = Screen.height / scale;
            GUI.Box(new Rect(18,18,showcase ? 440 : 450,showcase ? 102 : 158), "");
            GUI.Label(new Rect(34,27,430,40),showcase ? "潮岸遗物 · 三维资产展" : "潮岸遗物 · 骷髅试炼",title);
            if (showcase)
                GUI.Label(new Rect(34,72,400,42),"主人公 / 骷髅 / 三阶长剑 / 升级宝箱",body);
            else if (hero)
            {
                GUI.Label(new Rect(34,72,410,32),"生命 " + Mathf.CeilToInt(hero.health) + " / 100    武器 Lv." + hero.weaponLevel + "    伤害 " + QuestRules.Damage(hero.weaponLevel),body);
                GUI.Label(new Rect(34,105,410,55),"骷髅 " + kills + " / " + totalEnemies + "    宝箱条件：击败 2 / 4 只骷髅",body);
            }
            GUI.Box(new Rect(18,height-80,width-36,62),"");
            GUI.Label(new Rect(34,height-72,width-68,54),showcase
                ? "鼠标拖动旋转 · 滚轮缩放 · 0 总览 / 1 主角 / 2 骷髅 / 3 长剑 / 4 宝箱\nF1 隐藏文字 · F8 保存截图到工程的 Screenshots 文件夹"
                : "WASD 移动 · Shift 奔跑 · 空格跳跃 · 鼠标转镜头 · 左键攻击 · E 开箱\nEsc 释放鼠标 / 点击画面继续 · F1 隐藏界面 · F8 截图",body);
            if (Time.unscaledTime < messageUntil) GUI.Label(new Rect(200,height*.78f,width-400,48),message,hint);
            if (!showcase && hero && (hero.health <= 0 || Finished))
            {
                GUI.Box(new Rect(width*.5f-240,height*.42f-45,480,100),"");
                GUI.Label(new Rect(width*.5f-220,height*.42f-28,440,70),hero.health <= 0 ? "试炼失败 · 按 R 重试" : "试炼完成！六只骷髅已清除。",hint);
            }
            GUI.matrix = previousMatrix;
        }
        void OnDisable() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }
}
