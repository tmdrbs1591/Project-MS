using ProjectMS.CharacterSystem;
using UnityEngine;

public class ZipperESkill : CharacterDeployable
{
    private void OnTriggerEnter2D(Collider2D collider)
    {
        CharacterProjectile projectile = collider.GetComponentInParent<CharacterProjectile>();
        if (projectile == null) return;

        projectile.CompleteManually();
    }
}
