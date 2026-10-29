using UnityEngine;

/// <summary>
/// 켜질 때마다 "띠용" 하고 튀어나온다(머리 위 [F] 안내 등): 0에서 크게 넘쳤다가 출렁이며 자리 잡고,
/// 그동안 가로/세로가 번갈아 눌렸다 늘어난다. keyTarget을 넣으면 그 칸이 한 박자 늦게 한 번 더 통 튄다.
/// 대상 RectTransform의 크기/위치만 건드린다.
/// </summary>
public class UIPopIn : MonoBehaviour
{
    [SerializeField] private RectTransform target;
    [Tooltip("한 박자 늦게 따로 통 튈 칸(예: 키캡). 비워도 된다.")]
    [SerializeField] private RectTransform keyTarget;
    [Min(0.05f)] [SerializeField] private float duration = 0.6f;
    [SerializeField] private float rise = 22f;
    [SerializeField] private float keyDelay = 0.14f;

    private Vector2 basePosition;
    private Vector3 baseScale;
    private Vector3 keyBaseScale;
    private float shownAt;
    private bool captured;

    private void Awake() => Capture();

    private void Capture()
    {
        if (captured || target == null)
            return;
        captured = true;
        basePosition = target.anchoredPosition;
        baseScale = target.localScale;
        if (keyTarget != null)
            keyBaseScale = keyTarget.localScale;
    }

    private void OnEnable()
    {
        Capture();
        shownAt = Time.unscaledTime;
        Apply(0f);
    }

    private void Update() => Apply(Time.unscaledTime - shownAt);

    private void Apply(float elapsed)
    {
        if (target == null)
            return;

        float t = Mathf.Clamp01(elapsed / duration);
        // 감쇠 진동: 0 → 약 1.25까지 넘쳤다가 출렁이며 1로.
        float s = t >= 1f ? 1f : 1f - Mathf.Exp(-7f * t) * Mathf.Cos(16f * t);
        float squash = t >= 1f ? 0f : 0.18f * Mathf.Exp(-6f * t) * Mathf.Sin(16f * t);
        target.localScale = new Vector3(baseScale.x * s * (1f + squash), baseScale.y * s * (1f - squash), baseScale.z);
        target.anchoredPosition = basePosition + Vector2.down * (rise * (1f - s));

        if (keyTarget == null)
            return;
        float k = Mathf.Clamp01((elapsed - keyDelay) / 0.35f);
        float bump = k <= 0f || k >= 1f ? 0f : Mathf.Sin(k * Mathf.PI) * Mathf.Exp(-3f * k) * 0.35f;
        keyTarget.localScale = keyBaseScale * (1f + bump);
    }
}
