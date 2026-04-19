using UnityEngine;

namespace SipLab
{
    public class PlayerInventory : MonoBehaviour
    {
        public bool HasEnergyCore => GameStateManager.Instance != null && GameStateManager.Instance.HasEnergyCore;
        public bool HasKeycard => GameStateManager.Instance != null && GameStateManager.Instance.HasKeycard;

        public void GiveEnergyCore() => GameStateManager.Instance?.PickupEnergyCore();
        public void GiveKeycard() => GameStateManager.Instance?.PickupKeycard();
    }
}
