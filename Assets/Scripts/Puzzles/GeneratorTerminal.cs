using UnityEngine;

namespace SipLab
{
    public class GeneratorTerminal : MonoBehaviour, IInteractable
    {
        public Renderer IndicatorRenderer;
        public Color OfflineColor = new Color(0.6f, 0.05f, 0.05f);
        public Color OnlineColor = new Color(0.1f, 0.9f, 0.2f);

        bool _powered;

        void Start()
        {
            UpdateIndicator();
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnPowerRestored += UpdateIndicator;
        }

        void OnDestroy()
        {
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnPowerRestored -= UpdateIndicator;
        }

        void UpdateIndicator()
        {
            _powered = GameStateManager.Instance != null && GameStateManager.Instance.IsPowerOn;
            if (IndicatorRenderer == null) return;
            var mat = IndicatorRenderer.material;
            Color c = _powered ? OnlineColor : OfflineColor;
            mat.color = c;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", c * 1.5f);
        }

        public string GetPrompt()
        {
            if (_powered) return "Generator Online";
            if (GameStateManager.Instance != null && GameStateManager.Instance.HasEnergyCore)
                return "Press E to Insert Energy Core";
            return "Generator Offline - Find Energy Core";
        }

        public bool IsInteractable() => true;

        public void Interact(PlayerInventory inventory)
        {
            if (_powered) return;
            if (!inventory.HasEnergyCore) return;
            GameStateManager.Instance.RestorePower();
            UpdateIndicator();
            if (AudioManager.Instance != null) AudioManager.Instance.PlayPowerOn();
        }
    }
}
