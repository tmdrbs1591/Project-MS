using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Play()를 부르면 자식 줄들이 위에서부터 차례로 "촤라라락" 옆에서 밀려 들어오며 나타난다(설정 창 줄 목록 등).
/// 레이아웃 그룹 안의 자식이어도 되도록, 시작할 때의 위치를 기억해 두고 그 위치 기준으로만 잠깐 밀었다가 정확히 되돌린다.
/// </summary>
public class UIStaggerReveal : MonoBehaviour
{
    [SerializeField] private float stagger = 0.028f;
    [SerializeField] private float duration = 0.26f;
    [SerializeField] private Vector2 offset = new Vector2(28f, 0f);

    private readonly List<RectTransform> items = new List<RectTransform>();
    private readonly List<CanvasGroup> groups = new List<CanvasGroup>();
    private readonly List<Vector2> bases = new List<Vector2>();
    private float startedAt = -1f;

    public void Play()
    {
        Finish();
        items.Clear();
        groups.Clear();
        bases.Clear();
        // 창을 처음 여는 순간엔 레이아웃이 아직 안 잡혀 있을 수 있다 → 먼저 확정한 뒤 위치를 기억한다.
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
        foreach (Transform child in transform)
        {
            if (!child.gameObject.activeSelf)
                continue;
            var rt = (RectTransform)child;
            if (!child.TryGetComponent(out CanvasGroup group))
                group = child.gameObject.AddComponent<CanvasGroup>();
            items.Add(rt);
            groups.Add(group);
            bases.Add(rt.anchoredPosition);
        }
        startedAt = Time.unscaledTime;
        Apply(0f);
    }

    private void OnDisable() => Finish();

    private void LateUpdate()
    {
        if (startedAt < 0f)
            return;
        float elapsed = Time.unscaledTime - startedAt;
        Apply(elapsed);
        if (elapsed >= duration + stagger * items.Count)
            Finish();
    }

    private void Apply(float elapsed)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null)
                continue;
            float t = Mathf.Clamp01((elapsed - i * stagger) / duration);
            float e = 1f - Mathf.Pow(1f - t, 3f);
            items[i].anchoredPosition = bases[i] + offset * (1f - e);
            groups[i].alpha = e;
        }
    }

    private void Finish()
    {
        if (startedAt < 0f)
            return;
        startedAt = -1f;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null)
                continue;
            items[i].anchoredPosition = bases[i];
            groups[i].alpha = 1f;
        }
        LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
    }
}
