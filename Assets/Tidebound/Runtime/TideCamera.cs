using UnityEngine;

namespace Tidebound
{
    public sealed class TideCamera : MonoBehaviour
    {
        public TidePlayer target;
        public float distance = 4.5f;
        public float sensitivity = .12f;
        float yaw, pitch = 18;
        void LateUpdate()
        {
            if (target == null) return;
            if (target.adventure.IsPlaying)
            {
                var look = TideInput.Look;
                yaw += look.x * sensitivity;
                pitch = Mathf.Clamp(pitch - look.y * sensitivity, -25, 65);
            }
            var rotation = Quaternion.Euler(pitch, yaw, 0);
            var pivot = target.transform.position + Vector3.up * 1.5f;
            var back = rotation * Vector3.back;
            float available = distance;
            foreach (var hit in Physics.SphereCastAll(pivot, .2f, back, distance, ~0, QueryTriggerInteraction.Ignore))
                if (!hit.transform.IsChildOf(target.transform)) available = Mathf.Min(available, Mathf.Max(.25f, hit.distance - .15f));
            transform.SetPositionAndRotation(pivot + back * available, rotation);
        }
    }
}
