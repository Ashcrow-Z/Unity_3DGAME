using UnityEngine;

namespace SipLab
{
    public class EnergyCore : MonoBehaviour, IInteractable
    {
        public string GetPrompt() => "Press E to Pick Up Energy Core";
        public bool IsInteractable() => gameObject.activeInHierarchy;

        public void Interact(PlayerInventory inventory)
        {
            inventory.GiveEnergyCore();
            if (AudioManager.Instance != null) AudioManager.Instance.PlayPickupCore();
            gameObject.SetActive(false);
        }
    }
}
