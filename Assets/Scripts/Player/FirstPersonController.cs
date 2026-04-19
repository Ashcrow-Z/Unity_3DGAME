using UnityEngine;

namespace SipLab
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class FirstPersonController : MonoBehaviour
    {
        [Header("Movement")]
        public float MoveSpeed = 4.0f;
        public float MouseSensitivity = 2.0f;
        public float MaxPitch = 80f;

        [Header("References")]
        public Transform CameraPivot;

        Rigidbody _rb;
        float _pitch;
        float _yaw;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.freezeRotation = true;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            _yaw = transform.eulerAngles.y;
            _pitch = 0f;
        }

        void Update()
        {
            if (GameStateManager.Instance == null) return;
            if (GameStateManager.Instance.State != GameState.Playing) return;

            float mx = Input.GetAxis("Mouse X") * MouseSensitivity;
            float my = Input.GetAxis("Mouse Y") * MouseSensitivity;

            _yaw += mx;
            _pitch -= my;
            _pitch = Mathf.Clamp(_pitch, -MaxPitch, MaxPitch);

            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            if (CameraPivot != null)
                CameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        void FixedUpdate()
        {
            if (GameStateManager.Instance == null) return;
            if (GameStateManager.Instance.State != GameState.Playing)
            {
                _rb.velocity = new Vector3(0f, _rb.velocity.y, 0f);
                return;
            }

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            Vector3 dir = (transform.right * h + transform.forward * v);
            if (dir.sqrMagnitude > 1f) dir.Normalize();

            Vector3 targetVel = dir * MoveSpeed;
            Vector3 vel = _rb.velocity;
            vel.x = targetVel.x;
            vel.z = targetVel.z;
            _rb.velocity = vel;
        }
    }
}
