using UnityEngine;

namespace SipLab
{
    public class VictoryTrigger : MonoBehaviour
    {
        public string PlayerTag = "Player";

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(PlayerTag) && other.GetComponentInParent<FirstPersonController>() == null) return;
            if (GameStateManager.Instance != null) GameStateManager.Instance.TriggerVictory();
        }
    }
}
