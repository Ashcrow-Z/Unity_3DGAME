using System.Collections;
using UnityEngine;

namespace SipLab
{
    public class SafeDoor : MonoBehaviour
    {
        public Transform DoorPivot;
        public GameObject KeycardObject;
        public float OpenAngle = 110f;
        public float Duration = 1.0f;

        bool _opened;

        public void Open()
        {
            if (_opened) return;
            _opened = true;
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySafeDoor();
            StartCoroutine(OpenRoutine());
        }

        IEnumerator OpenRoutine()
        {
            if (DoorPivot != null)
            {
                Quaternion start = DoorPivot.localRotation;
                Quaternion end = start * Quaternion.Euler(0f, OpenAngle, 0f);
                float t = 0f;
                while (t < Duration)
                {
                    t += Time.unscaledDeltaTime;
                    DoorPivot.localRotation = Quaternion.Slerp(start, end, Mathf.SmoothStep(0f, 1f, t / Duration));
                    yield return null;
                }
                DoorPivot.localRotation = end;
            }
            if (KeycardObject != null) KeycardObject.SetActive(true);
        }
    }
}
