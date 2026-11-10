using ProjectMS.CharacterSystem;
using UnityEngine;

public class ZipperESkill : CharacterDeployable
{
    [Tooltip("투사체를 빨아들일 때 그 자리에서 터지는 이펙트.")]
    [SerializeField] private SkillFxSlot[] absorbFx;

    private void OnTriggerEnter2D(Collider2D collider)
    {
        CharacterProjectile projectile = collider.GetComponentInParent<CharacterProjectile>();
        if (projectile == null) return;

        // 트리거는 모든 클라에서 불리므로 이펙트는 각자 그린다(실제 소멸은 투사체 권한자 쪽에서만 일어난다).
        SkillFxSlot.SpawnAll(absorbFx, projectile.transform.position, 0f);
        projectile.CompleteManually();
    }
}
