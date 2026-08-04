using UnityEngine;

namespace ProjectMS.CharacterSystem
{
    internal sealed class JiggleSolver
    {
        private readonly Transform target;
        private readonly Transform parent;
        private readonly Transform body;
        private readonly CharacterVisualProfile profile;
        private readonly float bodyFollow;
        private readonly Vector3 baseLocalPosition;
        private readonly Quaternion baseLocalRotation;
        private readonly Vector3 bodyBaseLocalPosition;

        private Vector3 currentWorldPosition;
        private Vector3 velocity;

        public JiggleSolver(
            Transform target,
            Transform body,
            CharacterVisualProfile profile,
            float bodyFollow)
        {
            this.target = target;
            this.body = body;
            this.profile = profile;
            this.bodyFollow = bodyFollow;
            parent = target != null ? target.parent : null;
            baseLocalPosition = target != null ? target.localPosition : Vector3.zero;
            baseLocalRotation = target != null ? target.localRotation : Quaternion.identity;
            bodyBaseLocalPosition = body != null ? body.localPosition : Vector3.zero;
            currentWorldPosition = target != null ? target.position : Vector3.zero;
        }

        public void Tick(float deltaTime)
        {
            if (target == null || parent == null || deltaTime <= 0f)
                return;

            deltaTime = Mathf.Min(deltaTime, 0.0333f);
            Vector3 targetLocal = baseLocalPosition;
            if (body != null)
                targetLocal += (body.localPosition - bodyBaseLocalPosition) * bodyFollow;

            Vector3 anchor = parent.TransformPoint(targetLocal);
            Vector3 force = (anchor - currentWorldPosition) * profile.JiggleStiffness
                - velocity * profile.JiggleDamping;

            velocity += force * deltaTime;
            currentWorldPosition += velocity * deltaTime;

            Vector3 offset = currentWorldPosition - anchor;
            if (offset.magnitude > profile.JiggleMaxOffset)
            {
                offset = offset.normalized * profile.JiggleMaxOffset;
                currentWorldPosition = anchor + offset;
            }

            target.position = currentWorldPosition;
            Vector3 localOffset = parent.InverseTransformVector(offset);
            float angle = -localOffset.x * profile.JiggleRotation;
            target.localRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, angle);
        }
    }
}
