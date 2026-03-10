#if UNITY_EDITOR
using PhalanxChronicle.Battle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PhalanxChronicle.Editor
{
    public static class BattleSceneBuilder
    {
        [MenuItem("Phalanx Chronicle/Create Battle Scene")]
        public static void CreateBattleScene()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject gameManagerObject = new GameObject("GameManager");
            gameManagerObject.AddComponent<GameManager>();

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/Scenes/Battle.unity");
            Selection.activeGameObject = gameManagerObject;
        }
    }
}
#endif
