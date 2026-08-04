using UnityEngine;

namespace ProjectMS.CharacterSystem
{
    internal sealed class AimVisualSolver
    {
        private readonly Transform target;
        private readonly AimVisualMode mode;
        private readonly float rotationOffset;
        private readonly Vector3 baseLocalScale;

        public AimVisualSolver(Transform target, AimVisualMode mode, float rotationOffset)
        {
            this.target = target;
            this.mode = mode;
            this.rotationOffset = rotationOffset;
            baseLocalScale = target != null ? target.localScale : Vector3.one;
        }

        public void Apply(int direction, float angle)
        {
            if (target == null || mode == AimVisualMode.None)
                return;

            int sign = direction >= 0 ? 1 : -1;
            target.localScale = new Vector3(
                Mathf.Abs(baseLocalScale.x) * sign,
                baseLocalScale.y,
                baseLocalScale.z);

            if (mode == AimVisualMode.FlipOnly)
                return;

            float localAngle = sign < 0 ? angle - 180f : angle;
            target.localRotation = Quaternion.Euler(0f, 0f, localAngle + rotationOffset);
        }
    }
}
