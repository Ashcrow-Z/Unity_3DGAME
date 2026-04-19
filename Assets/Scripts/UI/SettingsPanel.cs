using UnityEngine;
using UnityEngine.UI;

namespace SipLab
{
    /// <summary>
    /// Drives the two volume sliders. Wires its own listeners at runtime
    /// so a single panel can be re-used by both the start and pause menus.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        public Slider InteractionSlider;
        public Slider ActionSlider;
        public Text InteractionLabel;
        public Text ActionLabel;

        void OnEnable()
        {
            if (AudioManager.Instance == null) return;

            if (InteractionSlider != null)
            {
                InteractionSlider.SetValueWithoutNotify(AudioManager.Instance.InteractionVolume);
                InteractionSlider.onValueChanged.RemoveListener(OnInteractionChanged);
                InteractionSlider.onValueChanged.AddListener(OnInteractionChanged);
            }
            if (ActionSlider != null)
            {
                ActionSlider.SetValueWithoutNotify(AudioManager.Instance.ActionVolume);
                ActionSlider.onValueChanged.RemoveListener(OnActionChanged);
                ActionSlider.onValueChanged.AddListener(OnActionChanged);
            }
            UpdateLabels();
        }

        void OnInteractionChanged(float v)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetInteractionVolume(v);
            UpdateLabels();
        }

        void OnActionChanged(float v)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetActionVolume(v);
            UpdateLabels();
        }

        void UpdateLabels()
        {
            if (AudioManager.Instance == null) return;
            if (InteractionLabel != null)
                InteractionLabel.text = "Interaction Volume: " + Mathf.RoundToInt(AudioManager.Instance.InteractionVolume * 100f) + "%";
            if (ActionLabel != null)
                ActionLabel.text = "Action Volume: " + Mathf.RoundToInt(AudioManager.Instance.ActionVolume * 100f) + "%";
        }
    }
}
