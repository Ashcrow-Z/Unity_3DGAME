using System.Collections;
using UnityEngine;

namespace SipLab
{
    public class KeypadButton : MonoBehaviour, IInteractable
    {
        public KeypadPuzzle Puzzle;
        public string Value = "0";
        public Renderer ButtonRenderer;

        Vector3 _restPos;
        bool _animating;

        void Awake()
        {
            _restPos = transform.localPosition;
        }

        public string GetPrompt()
        {
            if (Puzzle == null || !Puzzle.IsActive())
                return "Keypad Offline";
            if (Value == "C") return "Press E to Clear";
            if (Value == "E") return "Press E to Confirm";
            return "Press E to Press " + Value;
        }

        public bool IsInteractable()
        {
            return Puzzle != null && Puzzle.IsActive();
        }

        public void Interact(PlayerInventory inventory)
        {
            if (!IsInteractable()) return;
            if (!_animating) StartCoroutine(PressAnim());
            if (AudioManager.Instance != null) AudioManager.Instance.PlayKeypadBeep(Value);
            Puzzle.OnButtonPressed(Value);
        }

        IEnumerator PressAnim()
        {
            _animating = true;
            Vector3 down = _restPos + new Vector3(0f, 0f, 0.025f);
            float t = 0f;
            while (t < 0.08f) { t += Time.unscaledDeltaTime; transform.localPosition = Vector3.Lerp(_restPos, down, t / 0.08f); yield return null; }
            t = 0f;
            while (t < 0.08f) { t += Time.unscaledDeltaTime; transform.localPosition = Vector3.Lerp(down, _restPos, t / 0.08f); yield return null; }
            transform.localPosition = _restPos;
            _animating = false;
        }
    }
}
