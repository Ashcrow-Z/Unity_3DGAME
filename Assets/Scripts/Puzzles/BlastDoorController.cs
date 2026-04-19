using System.Collections;
using UnityEngine;

namespace SipLab
{
    public class BlastDoorController : MonoBehaviour, IInteractable
    {
        public Transform LeftDoor;
        public Transform RightDoor;
        public float SlideDistance = 1.05f;
        public float Duration = 1.5f;
        public Renderer IndicatorRenderer;
        public GameObject VictoryTriggerObject;

        Vector3 _leftClosed, _leftOpen;
        Vector3 _rightClosed, _rightOpen;
        bool _isOpen;
        bool _animating;

        void Awake()
        {
            if (LeftDoor != null)
            {
                _leftClosed = LeftDoor.localPosition;
                _leftOpen = _leftClosed + new Vector3(-SlideDistance, 0f, 0f);
            }
            if (RightDoor != null)
            {
                _rightClosed = RightDoor.localPosition;
                _rightOpen = _rightClosed + new Vector3(SlideDistance, 0f, 0f);
            }
            if (VictoryTriggerObject != null) VictoryTriggerObject.SetActive(false);
            UpdateIndicator();
        }

        void Update()
        {
            UpdateIndicator();
        }

        void UpdateIndicator()
        {
            if (IndicatorRenderer == null) return;
            Color c;
            if (_isOpen) c = new Color(0.1f, 0.9f, 0.2f);
            else if (CanOpen()) c = new Color(0.95f, 0.85f, 0.1f);
            else c = new Color(0.9f, 0.1f, 0.1f);

            var mat = IndicatorRenderer.material;
            mat.color = c;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", c * 1.5f);
        }

        bool CanOpen()
        {
            var gsm = GameStateManager.Instance;
            return gsm != null && gsm.IsPowerOn && gsm.HasKeycard;
        }

        public string GetPrompt()
        {
            if (_isOpen) return "Blast Door Open";
            var gsm = GameStateManager.Instance;
            if (gsm == null) return "";
            if (!gsm.IsPowerOn) return "Power Required";
            if (!gsm.HasKeycard) return "Security Keycard Required";
            return "Press E to Open Blast Door";
        }

        public bool IsInteractable() => !_isOpen && !_animating;

        public void Interact(PlayerInventory inventory)
        {
            if (!CanOpen() || _isOpen || _animating) return;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayKeycardSwipe();
                AudioManager.Instance.PlayBlastDoor();
            }
            StartCoroutine(OpenRoutine());
        }

        IEnumerator OpenRoutine()
        {
            _animating = true;
            float t = 0f;
            while (t < Duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / Duration);
                if (LeftDoor != null) LeftDoor.localPosition = Vector3.Lerp(_leftClosed, _leftOpen, k);
                if (RightDoor != null) RightDoor.localPosition = Vector3.Lerp(_rightClosed, _rightOpen, k);
                yield return null;
            }
            if (LeftDoor != null) LeftDoor.localPosition = _leftOpen;
            if (RightDoor != null) RightDoor.localPosition = _rightOpen;
            _isOpen = true;
            _animating = false;
            if (VictoryTriggerObject != null) VictoryTriggerObject.SetActive(true);
        }
    }
}
