using ProjectMS.CharacterSystem;

namespace ProjectMS.CharacterSystem.Examples
{
    /// <summary>
    /// 신규 캐릭터 시작 템플릿.
    /// 필요한 스킬과 패시브 훅만 override하고 Fusion API는 직접 사용하지 않는다.
    /// </summary>
    public class ZipperCharacter : CharacterBase
    {
        protected override bool OnBasicAttack(CharacterActionContext context)
        {
            // 예: FindEnemiesInArc 또는 SpawnProjectile을 사용해 평타를 구현한다.
            return false;
        }

        protected override bool OnSkillQ(CharacterActionContext context)
        {
            return false;
        }

        protected override bool OnSkillE(CharacterActionContext context)
        {
            return false;
        }

        protected override bool OnUltimate(CharacterActionContext context)
        {
            return false;
        }

        protected override void OnPassiveTick(float deltaTime)
        {
            // 매 네트워크 Simulation 틱에 필요한 패시브만 구현한다.
        }

        protected override float ModifyOutgoingDamage(
            CharacterBase target,
            float damage,
            CharacterDamageSource source)
        {
            // 예: 뒤에서 맞힌 공격만 데미지를 높인다.
            return damage;
        }

        protected override void OnProjectileDespawned(
            CharacterProjectile projectile,
            ProjectileDespawnReason reason,
            CharacterBase hitTarget)
        {
            // 예: 명중, 벽 충돌, 시간 종료, 수동 종료에 맞는 효과를 추가한다.
        }

        protected override void OnResetCharacter()
        {
            // 조준 모드, 변신, 연속기처럼 캐릭터 스크립트가 가진 로컬 상태를 초기화한다.
        }

        protected override void OnCharacterDespawned()
        {
            // 외부 이벤트 구독이나 캐릭터별 런타임 참조가 있다면 여기서 해제한다.
        }
    }
}
