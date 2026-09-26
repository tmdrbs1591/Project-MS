using Fusion;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectMS.CharacterSystem.Examples
{
    public partial class SparkCharacter : CharacterBase
    {
        // GameObject 프리팹 참조는 RPC로 직렬화해서 보낼 수 없어서, 어떤 프리팹을 쓸지는 enum으로만 보내고
        // 각 클라가 자기 Spark 인스펙터에 연결된 프리팹을 로컬에서 그대로 사용한다(양쪽 클라에 프리팹이
        // 동일하게 세팅돼 있어야 한다).
        private enum RangeEffectKind : byte
        {
            Overload,
            TeslaField
        }

        private enum SparkSoundKind : byte
        {
            OverloadExplosion,
            TeslaCast,
            TeslaExplosion
        }

        [Header("Sounds - Pitch")]
        [Tooltip("E/R 사운드를 낼 때마다 피치를 1±이 값 범위에서 랜덤으로 살짝 바꾼다(같은 소리가 반복돼도 덜 기계적으로 들리게).")]
        [Range(0f, 0.5f)][SerializeField] private float skillSoundPitchVariance = 0.08f;

        // E/R 전용 사운드. 모든 클라에서 들리도록 RPC 로 브로드캐스트한다(클립은 각 클라 인스펙터 값 사용).
        private void PlaySparkSound(SparkSoundKind kind)
        {
            if (Object != null && Object.HasStateAuthority && !Runner.IsResimulation)
                Rpc_PlaySparkSound(kind);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void Rpc_PlaySparkSound(SparkSoundKind kind)
        {
            switch (kind)
            {
                case SparkSoundKind.OverloadExplosion:
                    SoundManager.Instance?.PlaySfx(overloadExplosionClip, overloadExplosionVolume, skillSoundPitchVariance);
                    break;
                case SparkSoundKind.TeslaCast:
                    SoundManager.Instance?.PlaySfx(teslaCastClip, teslaCastVolume, skillSoundPitchVariance);
                    break;
                case SparkSoundKind.TeslaExplosion:
                    SoundManager.Instance?.PlaySfx(teslaExplosionClip, teslaExplosionVolume, skillSoundPitchVariance);
                    break;
            }
        }

        // effectKind에 해당하는 프리팹을 모든 클라에서 생성하고, 스킬의 실제 반경(radius)에 맞춰 크기를 맞추는
        // 범위 이펙트 메서드. RPC로 브로드캐스트하므로 상대(관전) 클라에서도 보인다.
        private void PlayRangeEffect(RangeEffectKind effectKind, Vector3 position, float radius, float duration = 1.0f)
        {
            if (Object != null && Object.HasStateAuthority)
                Rpc_PlayRangeEffect(effectKind, position, radius, duration);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void Rpc_PlayRangeEffect(RangeEffectKind effectKind, Vector3 position, float radius, float duration)
        {
            GameObject effectPrefab = effectKind switch
            {
                RangeEffectKind.Overload => overloadEffectPrefab,
                RangeEffectKind.TeslaField => teslaEffectPrefab,
                _ => null
            };

            if (effectPrefab == null) return;

            GameObject instance = Instantiate(effectPrefab, position, Quaternion.identity);

            if (instance.TryGetComponent<ParticleSystem>(out var ps))
            {
                // 파티클 프리팹이면 Shape Radius를 직접 range 반경에 맞춤
                var shape = ps.shape;
                shape.radius = radius;
                ps.Play();
            }
            else
            {
                // 스프라이트 기반 프리팹이면 원본(스케일 1) 가로 폭 대비 스케일을 계산해서 지름(radius*2)에 맞춤
                SpriteRenderer sr = instance.GetComponentInChildren<SpriteRenderer>();
                float baseDiameter = (sr != null && sr.sprite != null && sr.sprite.bounds.size.x > 0f)
                    ? sr.sprite.bounds.size.x
                    : 1f;
                instance.transform.localScale = Vector3.one * ((radius * 2f) / baseDiameter);
            }

            // duration 뒤에 뚝 끊지 않고, 남은 파티클이 다 재생된 뒤에 지운다.
            EffectAutoDestroy.Schedule(instance, duration);
        }
    }
}
