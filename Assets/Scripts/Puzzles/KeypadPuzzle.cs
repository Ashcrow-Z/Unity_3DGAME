using System.Collections;
using UnityEngine;

namespace SipLab
{
    public class KeypadPuzzle : MonoBehaviour
    {
        [Tooltip("Overwritten at run start from GameStateManager.AccessCode.")]
        public string TargetCode = "";
        public TextMesh DisplayText;
        public SafeDoor Safe;
        public int MaxLength = 4;

        string _input = "";
        bool _solved;
        Coroutine _flashCo;

        void Start()
        {
            var gsm = GameStateManager.Instance;
            if (gsm != null)
            {
                gsm.OnRunStarted += ApplyAccessCode;
                if (!string.IsNullOrEmpty(gsm.AccessCode) && gsm.AccessCode.Length == 4)
                    ApplyAccessCode();
            }
            UpdateDisplay();
        }

        void OnDestroy()
        {
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnRunStarted -= ApplyAccessCode;
        }

        void ApplyAccessCode()
        {
            var gsm = GameStateManager.Instance;
            if (gsm == null || string.IsNullOrEmpty(gsm.AccessCode) || gsm.AccessCode.Length != 4) return;
            TargetCode = gsm.AccessCode;
            _solved = false;
            _input = "";
            UpdateDisplay();
        }

        public bool IsActive()
        {
            return !_solved && GameStateManager.Instance != null && GameStateManager.Instance.IsPowerOn;
        }

        public void OnButtonPressed(string value)
        {
            if (!IsActive()) return;

            if (value == "C")
            {
                _input = "";
                UpdateDisplay();
                return;
            }
            if (value == "E")
            {
                CheckCode();
                return;
            }

            if (_input.Length >= MaxLength) return;
            _input += value;
            UpdateDisplay();
        }

        void CheckCode()
        {
            if (_input == TargetCode)
            {
                _solved = true;
                if (DisplayText != null)
                {
                    DisplayText.text = "OK";
                    DisplayText.color = new Color(0.2f, 1f, 0.4f);
                }
                if (AudioManager.Instance != null) AudioManager.Instance.PlayCodeSuccess();
                if (Safe != null) Safe.Open();
            }
            else
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayCodeError();
                if (_flashCo != null) StopCoroutine(_flashCo);
                _flashCo = StartCoroutine(FlashError());
            }
        }

        IEnumerator FlashError()
        {
            if (DisplayText != null)
            {
                DisplayText.text = "ERR";
                DisplayText.color = new Color(1f, 0.2f, 0.2f);
            }
            yield return new WaitForSecondsRealtime(0.6f);
            _input = "";
            UpdateDisplay();
        }

        void UpdateDisplay()
        {
            if (DisplayText == null) return;
            DisplayText.color = new Color(0.4f, 1f, 0.5f);
            DisplayText.text = _input.PadRight(MaxLength, '_');
        }
    }
}
