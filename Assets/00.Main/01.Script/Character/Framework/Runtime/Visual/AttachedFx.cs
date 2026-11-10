using UnityEngine;

namespace ProjectMS.CharacterSystem
{
    /// <summary>
    /// 켜질 때 이펙트 프리팹을 띄워 이 오브젝트를 따라다니게 한다(투사체 꼬리/글로우 등).
    /// Epic Toon FX 같은 외부 이펙트는 프리팹에 바로 넣으면 URP에서 깨지므로 FxSpawner로 변환해서 띄운다.
    /// 꺼질 때(투사체 소멸 등)는 그 자리에서 새로 뿜는 것만 멈추고 남은 파티클이 자연스럽게 사라지게 한다.
    /// (자식으로 붙이지 않는 이유: 꺼지는 도중엔 계층을 바꿀 수 없어서 떼어낼 수가 없다.)
    /// </summary>
    public sealed class AttachedFx : MonoBehaviour
    {
        [SerializeField] private SkillFxSlot[] slots;
        [Tooltip("켜면 이 오브젝트의 크기(가로/세로 비율 포함)를 이펙트에도 곱한다 — 얇고 긴 설치물에 맞춰 이펙트를 찌그러뜨릴 때.")]
        [SerializeField] private bool inheritScale;

        private GameObject[] instances;
        private Vector3[] baseScales;

        private void OnEnable()
        {
            if (slots == null)
                return;
            instances = new GameObject[slots.Length];
            baseScales = new Vector3[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                SkillFxSlot slot = slots[i];
                if (slot == null || slot.prefab == null)
                    continue;
                GameObject go = slot.Spawn(transform.position, transform.eulerAngles.z);
                if (go != null)
                {
                    Destroy(go.GetComponent<EffectAutoDestroy>());
                    baseScales[i] = go.transform.localScale;
                    if (inheritScale)
                        foreach (ParticleSystem ps in go.GetComponentsInChildren<ParticleSystem>(true))
                        {
                            ParticleSystem.MainModule main = ps.main;
                            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                        }
                }
                instances[i] = go;
            }
            LateUpdate();
        }

        private void LateUpdate()
        {
            if (instances == null)
                return;
            float angle = transform.eulerAngles.z;
            for (int i = 0; i < instances.Length; i++)
            {
                GameObject go = instances[i];
                if (go == null)
                    continue;
                SkillFxSlot slot = slots[i];
                Vector3 p = transform.position + (Vector3)slot.offset;
                p.z = 0f;
                go.transform.position = p;
                float a = angle + slot.angleOffset;
                go.transform.rotation = slot.facing switch
                {
                    SkillFxFacing.PlusX => Quaternion.Euler(0f, 0f, a),
                    SkillFxFacing.PlusZ => Quaternion.Euler(0f, 0f, a) * Quaternion.Euler(0f, 90f, 0f),
                    _ => go.transform.rotation,
                };
                if (inheritScale)
                {
                    Vector3 s = transform.lossyScale;
                    go.transform.localScale = Vector3.Scale(baseScales[i], new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), 1f));
                }
            }
        }

        private void OnDisable()
        {
            if (instances == null)
                return;
            foreach (GameObject go in instances)
                if (go != null)
                    EffectAutoDestroy.Schedule(go, 0f);
            instances = null;
        }
    }
}
