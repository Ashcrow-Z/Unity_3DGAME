using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SipLab
{
    public class MenuManager : MonoBehaviour
    {
        public static MenuManager Instance { get; private set; }

        public Canvas StartCanvas;
        public Canvas PauseCanvas;
        public Canvas VictoryCanvas;
        public Canvas SettingsCanvas;
        public Text VictoryTimeText;

        GameState _stateBeforeSettings = GameState.Start;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged += OnStateChanged;
                GameStateManager.Instance.SetState(GameState.Start);
                OnStateChanged(GameState.Start);
            }
        }

        void OnDestroy()
        {
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnStateChanged -= OnStateChanged;
        }

        void Update()
        {
            if (GameStateManager.Instance == null) return;
            var s = GameStateManager.Instance.State;
            if (s == GameState.Playing || s == GameState.Paused)
            {
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space))
                    GameStateManager.Instance.TogglePause();
            }
        }

        void OnStateChanged(GameState s)
        {
            // close settings whenever state changes externally
            if (SettingsCanvas != null) SettingsCanvas.gameObject.SetActive(false);
            if (StartCanvas != null) StartCanvas.gameObject.SetActive(s == GameState.Start);
            if (PauseCanvas != null) PauseCanvas.gameObject.SetActive(s == GameState.Paused);
            if (VictoryCanvas != null)
            {
                bool v = (s == GameState.Victory);
                VictoryCanvas.gameObject.SetActive(v);
                if (v && VictoryTimeText != null)
                    VictoryTimeText.text = "Time to Escape: " + GameStateManager.Instance.FormattedTime;
            }
        }

        public void OnStartClicked()
        {
            if (GameStateManager.Instance != null) GameStateManager.Instance.StartGame();
        }

        public void OnResumeClicked()
        {
            if (GameStateManager.Instance != null) GameStateManager.Instance.SetState(GameState.Playing);
        }

        public void OnRestartClicked()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void OnSettingsClicked()
        {
            if (GameStateManager.Instance == null) return;
            _stateBeforeSettings = GameStateManager.Instance.State;
            if (StartCanvas != null) StartCanvas.gameObject.SetActive(false);
            if (PauseCanvas != null) PauseCanvas.gameObject.SetActive(false);
            if (SettingsCanvas != null) SettingsCanvas.gameObject.SetActive(true);
        }

        public void OnSettingsBackClicked()
        {
            if (SettingsCanvas != null) SettingsCanvas.gameObject.SetActive(false);
            // restore whichever menu was visible before (Start or Paused)
            if (_stateBeforeSettings == GameState.Paused)
            {
                if (PauseCanvas != null) PauseCanvas.gameObject.SetActive(true);
            }
            else
            {
                if (StartCanvas != null) StartCanvas.gameObject.SetActive(true);
            }
        }
    }
}
