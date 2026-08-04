using UnityEngine;

namespace ProjectMS.CharacterSystem
{
    internal sealed class CounterFlipSolver
    {
        private readonly Transform target;
        private readonly Vector3 baseLocalScale;

        public CounterFlipSolver(Transform target)
        {
            this.target = target;
            baseLocalScale = target != null ? target.localScale : Vector3.one;
        }

        public void Tick()
        {
            if (target == null || target.parent == null)
                return;

            float parentSign = Mathf.Sign(target.parent.lossyScale.x);
            Vector3 scale = baseLocalScale;
            scale.x = Mathf.Abs(baseLocalScale.x) * parentSign;
            target.localScale = scale;
        }
    }
}
