using UnityEngine;

namespace SipLab
{
    /// <summary>
    /// Plays a thud through AudioManager whenever the crate collides hard
    /// enough. Cooldown prevents one collision frame from spamming N hits.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CrateCollisionSound : MonoBehaviour
    {
        public float MinImpulse = 1.0f;
        public float MaxImpulse = 12f;
        public float Cooldown = 0.08f;

        float _nextAllowed;

        void OnCollisionEnter(Collision c)
        {
            // Suppress during pre-game (Start menu): boxes settle under gravity
            // when the scene loads, and we don't want those thuds to be audible.
            if (GameStateManager.Instance == null || GameStateManager.Instance.State != GameState.Playing) return;

            if (Time.unscaledTime < _nextAllowed) return;
            float imp = c.impulse.magnitude;
            if (imp < MinImpulse) return;
            float k = Mathf.InverseLerp(MinImpulse, MaxImpulse, imp);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayCrateImpact(k);
            _nextAllowed = Time.unscaledTime + Cooldown;
        }
    }
}
