using UnityEditor;
using UnityEditor.SceneManagement;

namespace BoatAttack.Island2019.Editor
{
    public static class OpenCleanIsland
    {
        [MenuItem("Island2019/Open Clean Island")]
        public static void Open()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene("Assets/scenes/island_clean_2019.unity");
        }
    }
}
