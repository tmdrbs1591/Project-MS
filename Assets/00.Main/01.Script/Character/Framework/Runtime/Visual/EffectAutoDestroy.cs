using UnityEngine;

namespace ProjectMS.CharacterSystem
{
    /// <summary>
    /// 이펙트 인스턴스를 "끝까지 재생한 뒤" 지운다.
    /// Destroy(instance, 시간) 로 지우면 파티클이 아직 날아가는 중에 뚝 끊기는 문제가 있어서,
    ///   - activeDuration 이 지나면 반복(loop) 파티클은 새로 뿜는 것만 멈추고,
    ///   - 이미 나온 파티클이 전부 사라지면 그때 오브젝트를 지운다.
    /// 파티클이 없는 이펙트(스프라이트 등)는 기존처럼 activeDuration 뒤에 바로 지운다.
    /// </summary>
    public sealed class EffectAutoDestroy : MonoBehaviour
    {
        // 파티클이 무한히 살아있는 설정이어도 오브젝트가 영원히 남지 않도록 하는 안전장치.
        private const float MaxExtraLifetime = 10f;

        private float activeDuration;
        private float elapsed;
        private bool stopped;
        private ParticleSystem[] systems;

        /// <summary>instance 를 activeDuration 동안 재생한 뒤, 남은 파티클이 다 사라지면 지운다.</summary>
        public static void Schedule(GameObject instance, float activeDuration)
        {
            if (instance == null)
                return;

            EffectAutoDestroy auto = instance.GetComponent<EffectAutoDestroy>();
            if (auto == null)
                auto = instance.AddComponent<EffectAutoDestroy>();
            auto.activeDuration = Mathf.Max(0f, activeDuration);
            auto.elapsed = 0f;
            auto.stopped = false;
        }

        private void Awake()
        {
            systems = GetComponentsInChildren<ParticleSystem>(true);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;

            if (!stopped)
            {
                if (elapsed < activeDuration)
                    return;

                stopped = true;
                if (systems.Length == 0)
                {
                    Destroy(gameObject);
                    return;
                }

                foreach (ParticleSystem ps in systems)
                {
                    if (ps != null && ps.main.loop)
                        ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                }
            }

            if (!AnyParticleAlive() || elapsed > activeDuration + MaxExtraLifetime)
                Destroy(gameObject);
        }

        private bool AnyParticleAlive()
        {
            foreach (ParticleSystem ps in systems)
            {
                if (ps != null && ps.IsAlive(false))
                    return true;
            }
            return false;
        }
    }
}
