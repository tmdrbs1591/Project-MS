using UnityEngine;

namespace ProjectMS.CharacterSystem
{
    /// <summary>
    /// 월드에 이펙트 프리팹을 띄우는 공용 도우미. 크기 배율, 옛 파티클 셰이더의 URP 변환(그리는 순서를 지키게),
    /// 그리기 순서, 다 재생된 뒤 자동 삭제를 한 번에 처리한다.
    /// Epic Toon FX 같은 외부 이펙트는 Built-in 셰이더라 그대로 두면 캐릭터/배경 순서를 무시하고 맨 앞에 그려진다.
    /// </summary>
    public static class FxSpawner
    {
        /// <summary>외부 이펙트를 변환할 때 쓰는 기본 그리기 순서(캐릭터보다 앞).</summary>
        public const int DefaultSortingOrder = 60;

        private static Transform holder;
        private static Transform Holder
        {
            get
            {
                if (holder == null)
                {
                    var go = new GameObject("[FxSpawner Holder]");
                    go.SetActive(false);
                    Object.DontDestroyOnLoad(go);
                    holder = go.transform;
                }
                return holder;
            }
        }

        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, float scale = 1f,
            float lifetime = 2f, int sortingOrder = DefaultSortingOrder, Transform parent = null)
        {
            if (prefab == null)
                return null;

            position.z = 0f;
            // 꺼진 보관함 아래에 먼저 만들어서, 켜지기 전에 딸린 소리(Epic Toon FX 프리팹의 AudioSource)를 끈다.
            // 소리는 각 스킬이 직접 고른 효과음으로만 낸다.
            GameObject go = Object.Instantiate(prefab, position, rotation, Holder);
            foreach (AudioSource audio in go.GetComponentsInChildren<AudioSource>(true))
                audio.enabled = false;
            go.transform.SetParent(parent, true);
            go.transform.SetPositionAndRotation(position, rotation);

            if (!Mathf.Approximately(scale, 1f))
            {
                go.transform.localScale *= scale;
                // 파티클 크기/속도가 오브젝트 크기를 따라가게(기본 Local이면 루트를 키워도 파티클은 그대로다)
                foreach (ParticleSystem ps in go.GetComponentsInChildren<ParticleSystem>(true))
                {
                    ParticleSystem.MainModule main = ps.main;
                    main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                }
            }

            foreach (ParticleSystemRenderer r in go.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                Material before = r.sharedMaterial;
                Material after = UrpParticleMaterial.Convert(before);
                if (after == before)
                    continue;   // 이미 URP/프로젝트 셰이더 — 원래 설정한 순서를 존중한다
                r.sharedMaterial = after;
                if (r.trailMaterial != null)
                    r.trailMaterial = UrpParticleMaterial.Convert(r.trailMaterial);
                r.sortingOrder = sortingOrder + r.sortingOrder;
            }

            if (lifetime > 0f)
                EffectAutoDestroy.Schedule(go, lifetime);
            return go;
        }

        /// <summary>파티클 시작 색에 tint를 곱한다(노란 이펙트를 빨갛게 쓰는 등).</summary>
        public static void Tint(GameObject go, Color tint)
        {
            if (go == null)
                return;
            foreach (ParticleSystem ps in go.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = ps.main;
                ParticleSystem.MinMaxGradient c = main.startColor;
                switch (c.mode)
                {
                    case ParticleSystemGradientMode.Color:
                        c.color *= tint;
                        break;
                    case ParticleSystemGradientMode.TwoColors:
                        c.colorMin *= tint;
                        c.colorMax *= tint;
                        break;
                    default:
                        c = new ParticleSystem.MinMaxGradient(tint);
                        break;
                }
                main.startColor = c;
            }
        }
    }
}
