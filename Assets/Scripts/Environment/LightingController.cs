using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SipLab
{
    public class LightingController : MonoBehaviour
    {
        public List<Light> RedLights = new List<Light>();
        public List<Light> WhiteLights = new List<Light>();
        public float TransitionDuration = 1.5f;
        public float RedIntensity = 1.2f;
        public float WhiteIntensity = 1.4f;
        public Color AmbientOff = new Color(0.05f, 0.0f, 0.0f);
        public Color AmbientOn = new Color(0.55f, 0.55f, 0.6f);

        void Start()
        {
            ApplyState(false, instant: true);
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnPowerRestored += OnPowerOn;
        }

        void OnDestroy()
        {
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnPowerRestored -= OnPowerOn;
        }

        void OnPowerOn()
        {
            StartCoroutine(Transition());
        }

        void ApplyState(bool powered, bool instant)
        {
            foreach (var l in RedLights)
                if (l != null) l.intensity = powered ? 0f : RedIntensity;
            foreach (var l in WhiteLights)
                if (l != null) l.intensity = powered ? WhiteIntensity : 0f;
            RenderSettings.ambientLight = powered ? AmbientOn : AmbientOff;
        }

        IEnumerator Transition()
        {
            float t = 0f;
            while (t < TransitionDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / TransitionDuration);
                foreach (var l in RedLights)
                    if (l != null) l.intensity = Mathf.Lerp(RedIntensity, 0f, k);
                foreach (var l in WhiteLights)
                    if (l != null) l.intensity = Mathf.Lerp(0f, WhiteIntensity, k);
                RenderSettings.ambientLight = Color.Lerp(AmbientOff, AmbientOn, k);
                yield return null;
            }
        }
    }
}
