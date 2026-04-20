using System.Collections;
using UnityEngine;

namespace SipLab
{
    /// <summary>
    /// A single cell of the binary-code server array. Bit = true means this
    /// drawer will extrude from the wall and light its green LED strip once
    /// BinaryServerArray.Reveal() walks over it. Bit = false drawers stay
    /// dormant (flush with the wall, LED dark) for the whole game.
    /// </summary>
    public class ServerDrawer : MonoBehaviour
    {
        [Header("Binary Value")]
        public bool Bit;

        [Header("Visual Refs")]
        public Transform DrawerBody;
        public Renderer LedRenderer;

        [Header("Materials (shared)")]
        public Material LedOffMaterial;
        public Material LedOnMaterial;

        [Header("Animation")]
        public float ExtendDistance = 0.14f;
        public float ExtendDuration = 0.45f;

        Vector3 _retractedLocal;
        bool _revealed;

        void Awake()
        {
            if (DrawerBody == null) DrawerBody = transform;
            _retractedLocal = DrawerBody.localPosition;
            if (LedRenderer != null && LedOffMaterial != null)
                LedRenderer.sharedMaterial = LedOffMaterial;
        }

        public void Reveal()
        {
            if (_revealed) return;
            _revealed = true;
            if (!Bit) return; // dormant: stays retracted, LED stays off

            if (LedRenderer != null && LedOnMaterial != null)
                LedRenderer.sharedMaterial = LedOnMaterial;
            StartCoroutine(CoExtend());
        }

        IEnumerator CoExtend()
        {
            Vector3 target = _retractedLocal + Vector3.forward * ExtendDistance;
            float t = 0f;
            while (t < ExtendDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / ExtendDuration));
                DrawerBody.localPosition = Vector3.Lerp(_retractedLocal, target, k);
                yield return null;
            }
            DrawerBody.localPosition = target;
        }
    }
}
