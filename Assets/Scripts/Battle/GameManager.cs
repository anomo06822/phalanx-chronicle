using PhalanxChronicle.Data;
using PhalanxChronicle.Localization;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PhalanxChronicle.Battle
{
    public sealed class GameManager : MonoBehaviour
    {
        private static GameManager instance;

        [SerializeField] private BattleScenarioDefinition scenarioDefinition;
        [SerializeField] private GameLocale initialLocale = GameLocale.TraditionalChinese;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureExists()
        {
            if (FindObjectOfType<GameManager>() != null)
            {
                return;
            }

            GameObject gameManagerObject = new GameObject("GameManager");
            gameManagerObject.AddComponent<GameManager>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            LocalizationService.SetLocale(initialLocale);
            EnsureCamera();
            EnsureEventSystem();
            EnsureBattleManager();
        }

        private void EnsureCamera()
        {
            if (Camera.main != null)
            {
                return;
            }

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera cameraComponent = cameraObject.AddComponent<Camera>();
            cameraComponent.orthographic = true;
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private void EnsureBattleManager()
        {
            if (GetComponentInChildren<BattleManager>() != null)
            {
                return;
            }

            GameObject battleManagerObject = new GameObject("BattleManager");
            battleManagerObject.transform.SetParent(transform, false);
            BattleManager battleManager = battleManagerObject.AddComponent<BattleManager>();
            battleManager.Initialize(scenarioDefinition != null ? scenarioDefinition : BattleScenarioDefinition.CreateDefault());
        }
    }
}
