using UnityEngine;

namespace SipLab
{
    public class KeycardPickup : MonoBehaviour, IInteractable
    {
        public string GetPrompt() => "Press E to Pick Up Security Keycard";
        public bool IsInteractable() => gameObject.activeInHierarchy;

        public void Interact(PlayerInventory inventory)
        {
            inventory.GiveKeycard();
            if (AudioManager.Instance != null) AudioManager.Instance.PlayPickupKeycard();
            gameObject.SetActive(false);
        }
    }
}
