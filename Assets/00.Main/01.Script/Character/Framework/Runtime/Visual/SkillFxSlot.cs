using System;
using UnityEngine;

namespace ProjectMS.CharacterSystem
{
    /// <summary>이펙트 프리팹이 어느 축을 "앞"으로 쓰는지. 조준 방향으로 돌릴 때 기준이 된다.</summary>
    public enum SkillFxFacing
    {
        /// <summary>회전 없이 그대로(원형 이펙트).</summary>
        None,
        /// <summary>프리팹의 +X가 앞(프로젝트 2D 이펙트).</summary>
        PlusX,
        /// <summary>프리팹의 +Z가 앞(Epic Toon FX 같은 3D 이펙트 — 머즐 플래시 등).</summary>
        PlusZ,
    }

    /// <summary>스킬 연출에서 쓰는 이펙트 프리팹 한 칸. 같은 순간에 여러 칸을 겹쳐 쓴다.</summary>
    [Serializable]
    public sealed class SkillFxSlot
    {
        public GameObject prefab;
        [Min(0.01f)] public float scale = 1f;
        public SkillFxFacing facing = SkillFxFacing.PlusZ;
        [Tooltip("조준 방향에 더할 각도(180이면 뒤쪽 — 바주카 후폭풍 등).")]
        public float angleOffset;
        [Tooltip("월드 좌표로 띄울 위치 보정.")]
        public Vector2 offset;
        [Tooltip("흰색이면 원래 색 그대로.")]
        public Color tint = Color.white;
        [Min(0f)] public float lifetime = 2f;

        public GameObject Spawn(Vector3 position, float aimAngle, Transform parent = null)
        {
            if (prefab == null)
                return null;

            float angle = aimAngle + angleOffset;
            Quaternion rotation = facing switch
            {
                SkillFxFacing.PlusX => Quaternion.Euler(0f, 0f, angle),
                SkillFxFacing.PlusZ => Quaternion.Euler(0f, 0f, angle) * Quaternion.Euler(0f, 90f, 0f),
                _ => Quaternion.identity,
            };

            GameObject go = FxSpawner.Spawn(prefab, position + (Vector3)offset, rotation, scale, lifetime,
                FxSpawner.DefaultSortingOrder, parent);
            if (go != null && tint != Color.white)
                FxSpawner.Tint(go, tint);
            return go;
        }

        public static void SpawnAll(SkillFxSlot[] slots, Vector3 position, float aimAngle)
        {
            if (slots == null)
                return;
            foreach (SkillFxSlot slot in slots)
                slot?.Spawn(position, aimAngle);
        }
    }
}
