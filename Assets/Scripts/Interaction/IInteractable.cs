namespace SipLab
{
    public interface IInteractable
    {
        string GetPrompt();
        bool IsInteractable();
        void Interact(PlayerInventory inventory);
    }
}
