using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace IslandQuest2019.Editor
{
    // Unity 2019 preview packages use two booleans for backend selection.
    // Derive our own compile symbol from that setting, without switching input backends.
    [InitializeOnLoad]
    public static class QuestInputConfig
    {
        const string Symbol="ISLANDQUEST_NEW_INPUT";
        static QuestInputConfig(){EditorApplication.delayCall+=Synchronize;}
        [MenuItem("Island2019/5 - Refresh Input Compatibility")]
        public static void Synchronize()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating)
            {EditorApplication.delayCall+=Synchronize;return;}
            PlayerSettings[] settings=Resources.FindObjectsOfTypeAll<PlayerSettings>();
            if(settings.Length==0) return;
            SerializedObject so=new SerializedObject(settings[0]);
            SerializedProperty modern=so.FindProperty("activeInputHandler");
            SerializedProperty preview=so.FindProperty("enableNativePlatformBackendsForNewInputSystem");
            if(modern==null && preview==null) return;
            bool useNew=modern!=null ? modern.intValue!=0 : preview.boolValue;
            BuildTargetGroup group=EditorUserBuildSettings.selectedBuildTargetGroup;
            if(group==BuildTargetGroup.Unknown) group=BuildTargetGroup.Standalone;
            string current=PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            List<string> defines=new List<string>();
            foreach(string part in current.Split(';')) if(!string.IsNullOrEmpty(part) && part!=Symbol) defines.Add(part);
            if(useNew) defines.Add(Symbol);
            string next=string.Join(";",defines.ToArray());
            if(current==next) return;
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group,next);
            Debug.Log("IslandQuest2019: input compatibility updated; wait for compilation to finish.");
        }
    }
}
