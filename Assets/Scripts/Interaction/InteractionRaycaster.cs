using UnityEngine;

namespace SipLab
{
    public class InteractionRaycaster : MonoBehaviour
    {
        public Camera Cam;
        public PlayerInventory Inventory;
        public float Range = 3.0f;
        public LayerMask InteractableMask;

        IInteractable _current;

        void Reset()
        {
            Cam = GetComponent<Camera>();
        }

        void Update()
        {
            if (GameStateManager.Instance == null || GameStateManager.Instance.State != GameState.Playing)
            {
                if (HUDController.Instance != null) HUDController.Instance.SetPrompt("");
                _current = null;
                return;
            }
            if (Cam == null) return;

            Ray ray = new Ray(Cam.transform.position, Cam.transform.forward);
            RaycastHit hit;
            _current = null;

            if (Physics.Raycast(ray, out hit, Range, InteractableMask, QueryTriggerInteraction.Collide))
            {
                var interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null && interactable.IsInteractable())
                {
                    _current = interactable;
                    if (HUDController.Instance != null)
                        HUDController.Instance.SetPrompt(interactable.GetPrompt());
                }
            }

            if (_current == null && HUDController.Instance != null)
                HUDController.Instance.SetPrompt("");

            if (_current != null && (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0)))
            {
                _current.Interact(Inventory);
            }
        }
    }
}
