using System.Collections;
using UnityEngine;

namespace SipLab
{
    public class SafeDoor : MonoBehaviour
    {
        public Transform LeftDoor;
        public Transform RightDoor;
        public Transform DoorPivot;
        public GameObject KeycardObject;
        public float OpenAngle = -110f;
        public float Duration = 1.0f;
        public float PopDistance = 0.18f;
        public float SlideDistance = 0.55f;
        public float PopPortion = 0.3f;

        bool _opened;

        void Awake()
        {
            EnsureHollowBody();
            EnsureSplitDoors();
        }

        void EnsureHollowBody()
        {
            Transform safeRoot = DoorPivot != null && DoorPivot.parent != null
                ? DoorPivot.parent
                : transform.parent;
            if (safeRoot == null) return;

            if (safeRoot.Find("SafeBackPanel") != null) return;

            var bodyRenderer = GetComponent<Renderer>();
            var bodyCollider = GetComponent<Collider>();
            Material bodyMaterial = bodyRenderer != null ? bodyRenderer.sharedMaterial : null;

            // The old scene used a single solid cube for SafeBody. It looked
            // fine while closed, but after the new sliding doors opened the
            // interior was still visibly blocked. Hide only the mesh/collider
            // on this object so this SafeDoor component keeps running.
            if (bodyRenderer != null) bodyRenderer.enabled = false;
            if (bodyCollider != null) bodyCollider.enabled = false;

            CreateBodyPart("SafeBackPanel", safeRoot, new Vector3(0f, 0f, -0.29f), new Vector3(0.86f, 0.86f, 0.04f), bodyMaterial);
            CreateBodyPart("SafeLeftWall", safeRoot, new Vector3(-0.48f, 0f, 0f), new Vector3(0.08f, 1.0f, 0.6f), bodyMaterial);
            CreateBodyPart("SafeRightWall", safeRoot, new Vector3(0.48f, 0f, 0f), new Vector3(0.08f, 1.0f, 0.6f), bodyMaterial);
            CreateBodyPart("SafeTopWall", safeRoot, new Vector3(0f, 0.48f, 0f), new Vector3(1.0f, 0.08f, 0.6f), bodyMaterial);
            CreateBodyPart("SafeBottomWall", safeRoot, new Vector3(0f, -0.48f, 0f), new Vector3(1.0f, 0.08f, 0.6f), bodyMaterial);
        }

        void EnsureSplitDoors()
        {
            if (LeftDoor != null && RightDoor != null) return;
            if (DoorPivot == null || DoorPivot.parent == null) return;

            var safeRoot = DoorPivot.parent;
            var legacyRenderer = DoorPivot.GetComponentInChildren<Renderer>();
            Material doorMaterial = legacyRenderer != null ? legacyRenderer.sharedMaterial : null;

            LeftDoor = CreateDoorHalf("LeftDoorPanel", safeRoot, new Vector3(-0.225f, 0f, 0.32f), doorMaterial).transform;
            RightDoor = CreateDoorHalf("RightDoorPanel", safeRoot, new Vector3(0.225f, 0f, 0.32f), doorMaterial).transform;

            // Hide the old one-piece hinged door in scenes that were built
            // before the split-door design existed.
            DoorPivot.gameObject.SetActive(false);
        }

        GameObject CreateDoorHalf(string name, Transform parent, Vector3 localPosition, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = new Vector3(0.44f, 0.9f, 0.04f);
            if (material != null)
                go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        GameObject CreateBodyPart(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            if (material != null)
                go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        public void Open()
        {
            if (_opened) return;
            _opened = true;
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySafeDoor();
            StartCoroutine(OpenRoutine());
        }

        IEnumerator OpenRoutine()
        {
            if (LeftDoor != null && RightDoor != null)
            {
                yield return OpenSplitDoorRoutine();
            }
            else if (DoorPivot != null)
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

        IEnumerator OpenSplitDoorRoutine()
        {
            Vector3 leftStart = LeftDoor.localPosition;
            Vector3 rightStart = RightDoor.localPosition;

            // The safe's local +Z is its outward/front direction. Because the
            // safe is mounted on the east wall, that maps to world -X (west),
            // so the doors pop out into the room before sliding aside.
            Vector3 pop = Vector3.forward * PopDistance;
            Vector3 leftPopped = leftStart + pop;
            Vector3 rightPopped = rightStart + pop;
            Vector3 leftOpen = leftPopped + Vector3.left * SlideDistance;
            Vector3 rightOpen = rightPopped + Vector3.right * SlideDistance;

            float popTime = Mathf.Max(0.05f, Duration * PopPortion);
            float slideTime = Mathf.Max(0.05f, Duration - popTime);

            float t = 0f;
            while (t < popTime)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.SmoothStep(0f, 1f, t / popTime);
                LeftDoor.localPosition = Vector3.Lerp(leftStart, leftPopped, u);
                RightDoor.localPosition = Vector3.Lerp(rightStart, rightPopped, u);
                yield return null;
            }

            t = 0f;
            while (t < slideTime)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.SmoothStep(0f, 1f, t / slideTime);
                LeftDoor.localPosition = Vector3.Lerp(leftPopped, leftOpen, u);
                RightDoor.localPosition = Vector3.Lerp(rightPopped, rightOpen, u);
                yield return null;
            }

            LeftDoor.localPosition = leftOpen;
            RightDoor.localPosition = rightOpen;
        }
    }
}
