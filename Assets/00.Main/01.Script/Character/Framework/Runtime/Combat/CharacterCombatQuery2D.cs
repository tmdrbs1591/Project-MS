using System.Collections.Generic;
using UnityEngine;

namespace ProjectMS.CharacterSystem
{
    public static class CharacterCombatQuery2D
    {
        public static List<CharacterBase> Circle(
            CharacterBase owner,
            Vector2 center,
            float radius,
            LayerMask targetLayer)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(center, Mathf.Max(0f, radius), targetLayer);
            return CollectUnique(owner, hits);
        }

        public static List<CharacterBase> Box(
            CharacterBase owner,
            Vector2 center,
            Vector2 size,
            float angle,
            LayerMask targetLayer)
        {
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, angle, targetLayer);
            return CollectUnique(owner, hits);
        }

        public static List<CharacterBase> Line(
            CharacterBase owner,
            Vector2 origin,
            Vector2 direction,
            float distance,
            float width,
            LayerMask targetLayer)
        {
            Vector2 normalized = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            RaycastHit2D[] hits = Physics2D.CircleCastAll(
                origin,
                Mathf.Max(0f, width * 0.5f),
                normalized,
                Mathf.Max(0f, distance),
                targetLayer);

            Collider2D[] colliders = new Collider2D[hits.Length];
            for (int i = 0; i < hits.Length; i++)
                colliders[i] = hits[i].collider;
            return CollectUnique(owner, colliders);
        }

        public static List<CharacterBase> Arc(
            CharacterBase owner,
            Vector2 origin,
            Vector2 forward,
            float radius,
            float angle,
            LayerMask targetLayer)
        {
            Vector2 normalizedForward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector2.right;
            List<CharacterBase> candidates = Circle(owner, origin, radius, targetLayer);

            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                Vector2 toTarget = (Vector2)candidates[i].transform.position - origin;
                if (toTarget.sqrMagnitude < 0.0001f)
                    continue;
                if (Vector2.Angle(normalizedForward, toTarget) > angle * 0.5f)
                    candidates.RemoveAt(i);
            }

            return candidates;
        }

        private static List<CharacterBase> CollectUnique(CharacterBase owner, Collider2D[] colliders)
        {
            List<CharacterBase> result = new List<CharacterBase>();
            HashSet<CharacterBase> seen = new HashSet<CharacterBase>();

            foreach (Collider2D hit in colliders)
            {
                if (hit == null)
                    continue;

                CharacterBase target = hit.GetComponentInParent<CharacterBase>();
                if (target == null || target == owner || !seen.Add(target))
                    continue;

                result.Add(target);
            }

            return result;
        }
    }
}
