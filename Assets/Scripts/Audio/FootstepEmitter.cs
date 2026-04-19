using UnityEngine;

namespace SipLab
{
    /// <summary>
    /// Plays a footstep through AudioManager every <see cref="StepInterval"/>
    /// seconds while the owning Rigidbody is moving on the ground.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class FootstepEmitter : MonoBehaviour
    {
        public float StepInterval = 0.42f;
        public float MinSpeed = 0.6f;

        Rigidbody _rb;
        float _timer;

        void Awake() { _rb = GetComponent<Rigidbody>(); }

        void Update()
        {
            if (GameStateManager.Instance == null || GameStateManager.Instance.State != GameState.Playing)
            {
                _timer = StepInterval * 0.5f;
                return;
            }

            Vector3 v = _rb.velocity; v.y = 0f;
            float speed = v.magnitude;
            if (speed < MinSpeed)
            {
                _timer = StepInterval * 0.5f;
                return;
            }

            _timer -= Time.unscaledDeltaTime;
            if (_timer <= 0f)
            {
                _timer = StepInterval;
                if (AudioManager.Instance != null) AudioManager.Instance.PlayFootstep();
            }
        }
    }
}
