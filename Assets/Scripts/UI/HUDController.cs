using UnityEngine;
using UnityEngine.UI;

namespace SipLab
{
    public class HUDController : MonoBehaviour
    {
        public static HUDController Instance { get; private set; }

        public Text PromptText;
        public Text InventoryText;
        public Text TimerText;
        public Image Crosshair;
        public CanvasGroup HudCanvasGroup;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            SetPrompt("");
            UpdateInventory();
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged += OnGameStateChanged;
                GameStateManager.Instance.OnEnergyCoreObtained += UpdateInventory;
                GameStateManager.Instance.OnKeycardObtained += UpdateInventory;
                OnGameStateChanged(GameStateManager.Instance.State);
            }
        }

        void OnDestroy()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged -= OnGameStateChanged;
                GameStateManager.Instance.OnEnergyCoreObtained -= UpdateInventory;
                GameStateManager.Instance.OnKeycardObtained -= UpdateInventory;
            }
        }

        void Update()
        {
            if (TimerText != null && GameStateManager.Instance != null)
                TimerText.text = "Time: " + GameStateManager.Instance.FormattedTime;
        }

        void OnGameStateChanged(GameState s)
        {
            if (HudCanvasGroup != null)
            {
                bool show = (s == GameState.Playing);
                HudCanvasGroup.alpha = show ? 1f : 0f;
                HudCanvasGroup.blocksRaycasts = false;
                HudCanvasGroup.interactable = false;
            }
        }

        public void SetPrompt(string text)
        {
            if (PromptText != null) PromptText.text = text;
        }

        public void UpdateInventory()
        {
            if (InventoryText == null) return;
            var gsm = GameStateManager.Instance;
            string holding = "Nothing";
            if (gsm != null)
            {
                if (gsm.HasKeycard) holding = "Keycard";
                else if (gsm.HasEnergyCore) holding = "Energy Core";
            }
            InventoryText.text = "Holding: " + holding;
        }
    }
}
