using UnityEngine;

/// <summary>
/// 스크린샷용 연출 씬(Showcase)에서 파티클 이펙트를 원하는 순간에 멈춰 보여 준다.
/// 편집 모드에서도 동작하므로 씬을 열기만 하면 이펙트가 그 순간 모습으로 보인다(플레이 모드에서도 멈춘 채로 유지).
/// 이펙트 프리팹의 맨 위 오브젝트에 붙이고, time으로 보여 줄 순간을 고른다.
/// </summary>
[ExecuteAlways]
public class ShowcaseFxFreeze : MonoBehaviour
{
    [Tooltip("이펙트를 이 시간(초)만큼 재생한 순간에 멈춘다.")]
    [SerializeField] private float time = 0.3f;

    [Tooltip("같은 모양이 나오도록 고정하는 난수 씨앗.")]
    [SerializeField] private uint seed = 7;

    [Tooltip("투사체처럼 날아오는 이펙트의 속도(월드 단위/초). 0이 아니면 time 동안 이 속도로 날아와 지금 위치에 도착한 것처럼 " +
             "시뮬레이션한다. 움직인 거리만큼 뿜는 꼬리 파티클(rateOverDistance)이 이렇게 해야 보인다.")]
    [SerializeField] private Vector3 travelVelocity;

    private void OnEnable() => Apply();

    // OnValidate 안에서는 파티클을 시뮬레이션할 수 없어서(경고가 난다) 한 번 미뤄서 한다.
    private void OnValidate()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += () => { if (this != null) Apply(); };
#endif
    }

    private void Start() => Apply();

    public void Apply()
    {
        ParticleSystem[] systems = GetComponentsInChildren<ParticleSystem>(true);
        uint s = seed;
        foreach (ParticleSystem ps in systems)
        {
            ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.useAutoRandomSeed = false;
            ps.randomSeed = s++;
        }

        // 다른 ParticleSystem 밑에 있지 않은 것만 시뮬레이션한다(자식은 withChildren으로 같이 돈다).
        var roots = new System.Collections.Generic.List<ParticleSystem>();
        foreach (ParticleSystem ps in systems)
            if (!HasParticleAncestor(ps.transform))
                roots.Add(ps);

        if (travelVelocity == Vector3.zero)
        {
            foreach (ParticleSystem ps in roots)
                ps.Simulate(time, true, true, true);
        }
        else
        {
            // 날아온 길을 짧은 걸음으로 따라가며 시뮬레이션하고, 마지막에 원래 자리에 둔다.
            Vector3 end = transform.position;
            const float step = 1f / 60f;
            int steps = Mathf.Max(1, Mathf.CeilToInt(time / step));
            float dt = time / steps;
            for (int i = 0; i <= steps; i++)
            {
                transform.position = end - travelVelocity * (time - i * dt);
                foreach (ParticleSystem ps in roots)
                    ps.Simulate(i == 0 ? 0f : dt, true, i == 0, true);
            }
            transform.position = end;
        }

        foreach (ParticleSystem ps in systems)
            ps.Pause(false);
    }

    private bool HasParticleAncestor(Transform t)
    {
        for (Transform p = t.parent; p != null && p != transform.parent; p = p.parent)
            if (p.GetComponent<ParticleSystem>() != null)
                return true;
        return false;
    }
}
