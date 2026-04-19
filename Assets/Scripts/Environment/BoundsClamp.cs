using UnityEngine;

namespace SipLab
{
    public class BoundsClamp : MonoBehaviour
    {
        public Vector3 Min = new Vector3(-4.7f, -1f, -4.7f);
        public Vector3 Max = new Vector3(4.7f, 10f, 4.7f);

        void LateUpdate()
        {
            Vector3 p = transform.position;
            p.x = Mathf.Clamp(p.x, Min.x, Max.x);
            p.z = Mathf.Clamp(p.z, Min.z, Max.z);
            if (p.y < Min.y) p.y = Min.y;
            transform.position = p;
        }
    }
}
