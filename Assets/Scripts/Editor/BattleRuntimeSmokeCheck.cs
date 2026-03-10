#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using PhalanxChronicle.Battle;
using PhalanxChronicle.Battle.Grid;
using PhalanxChronicle.Battle.Units;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PhalanxChronicle.Editor
{
    public static class BattleRuntimeSmokeCheck
    {
        private const string ScenePath = "Assets/Scenes/Battle.unity";
        private const string ResultPath = "/tmp/phalanx-chronicle-unity-logs/runtime-smoke-result.txt";

        [MenuItem("Phalanx Chronicle/Run Runtime Smoke Check")]
        public static void RunFromMenu()
        {
            RunInternal(exitOnComplete: false);
        }

        public static void Run()
        {
            RunInternal(exitOnComplete: true);
        }

        private static void RunInternal(bool exitOnComplete)
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                if (!exitOnComplete)
                {
                    EditorApplication.delayCall += RunFromMenu;
                }

                return;
            }

            string originalScenePath = EditorSceneManager.GetActiveScene().path;
            try
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

                GameManager gameManager = UnityEngine.Object.FindObjectOfType<GameManager>();
                if (gameManager == null)
                {
                    GameObject gameManagerObject = new GameObject("GameManager");
                    gameManager = gameManagerObject.AddComponent<GameManager>();
                }

                InvokeLifecycle(gameManager, "Awake");

                BattleManager battleManager = gameManager.GetComponentInChildren<BattleManager>();
                if (battleManager == null)
                {
                    throw new InvalidOperationException("BattleManager was not created by GameManager.");
                }

                InvokeLifecycle(battleManager, "Awake");
                InvokeLifecycle(battleManager, "Start");

                GridManager gridManager = UnityEngine.Object.FindObjectOfType<GridManager>();
                Canvas canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
                Unit[] units = UnityEngine.Object.FindObjectsOfType<Unit>();
                string[] rootNames = EditorSceneManager.GetActiveScene()
                    .GetRootGameObjects()
                    .Select(root => root.name)
                    .OrderBy(name => name)
                    .ToArray();

                if (gridManager == null)
                {
                    throw new InvalidOperationException("GridManager was not created.");
                }

                if (canvas == null)
                {
                    throw new InvalidOperationException("Battle canvas was not created.");
                }

                if (units.Length == 0)
                {
                    throw new InvalidOperationException("No runtime units were created.");
                }

                Debug.Log(
                    $"[BattleRuntimeSmokeCheck] PASS roots=[{string.Join(", ", rootNames)}] " +
                    $"units={units.Length} canvas={canvas.name} grid={gridManager.name}");
                WriteResult(
                    $"PASS roots=[{string.Join(", ", rootNames)}] " +
                    $"units={units.Length} canvas={canvas.name} grid={gridManager.name}");

                if (exitOnComplete)
                {
                    EditorApplication.Exit(0);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError("[BattleRuntimeSmokeCheck] FAIL\n" + exception);
                WriteResult("FAIL\n" + exception);
                if (exitOnComplete)
                {
                    EditorApplication.Exit(1);
                }
            }
            finally
            {
                if (!exitOnComplete)
                {
                    string sceneToRestore = string.IsNullOrEmpty(originalScenePath) ? ScenePath : originalScenePath;
                    EditorSceneManager.OpenScene(sceneToRestore, OpenSceneMode.Single);
                }
            }
        }

        private static void InvokeLifecycle(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new MissingMethodException(target.GetType().Name, methodName);
            }

            method.Invoke(target, null);
        }

        private static void WriteResult(string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ResultPath) ?? "/tmp");
            File.WriteAllText(ResultPath, content);
        }
    }
}
#endif
