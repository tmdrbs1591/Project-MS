using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 맵 슬롯머신. 맵 이미지들이 두루마리처럼 세로로 빈틈없이 이어져 위로 굴러가다가,
/// 점점 느려지면서 결과 맵에 딱 멈추고 한 번 커졌다 돌아온다.
///
/// 이미지는 절대 늘리지 않는다. 창 너비에 맞춰 원래 비율대로 칸 높이를 정하고, 창이 그보다
/// 크면 그만큼 칸이 더 보일 뿐이라 위아래로 계속 이어져 보인다.
///
/// [쓰는 법]
///   화면에 배치해둔 UI 오브젝트(슬롯 창)에 붙이고 Map Sprites만 채우면 끝.
///   그 오브젝트의 위치·크기가 곧 창이고, 마스크·띠·칸은 실행할 때 자동으로 만들어진다.
///   - 스크립트를 다른 곳에 두려면 Slot Window에 창 오브젝트를 넣는다.
///   - 돌리기: Spin() / Spin(결과인덱스). Spin On Enable을 켜면 켜질 때 자동으로 돈다.
/// </summary>
public class MapSlotMachine : MonoBehaviour
{
    [Tooltip("돌릴 맵 이미지들. 배열 순서 = 결과 인덱스.")]
    [SerializeField] private Sprite[] mapSprites;

    [Tooltip("이미 화면에 배치해둔 슬롯 창(RectTransform). 비워두면 이 오브젝트 자신을 쓴다.")]
    [SerializeField] private RectTransform slotWindow;

    [Header("회전")]
    [Tooltip("돌아가는 시간(초). 처음엔 빠르고 끝으로 갈수록 느려진다.")]
    [Min(0.1f)] [SerializeField] private float duration = 2.5f;
    [Tooltip("멈추기 전까지 맵 목록을 몇 바퀴 굴릴지.")]
    [Min(2)] [SerializeField] private int loops = 5;

    [Header("결정 연출 (플래시)")]
    [Tooltip("멈추는 순간 슬롯 창 안에서만 터지는 플래시 색.")]
    [SerializeField] private Color flashColor = Color.white;
    [Tooltip("플래시가 최대로 밝아지는 데 걸리는 시간(초). 0이면 즉시 번쩍.")]
    [Min(0f)] [SerializeField] private float flashInDuration;
    [Tooltip("플래시가 사라지는 데 걸리는 시간(초).")]
    [Min(0.01f)] [SerializeField] private float flashOutDuration = 0.4f;
    [Range(0f, 1f)] [SerializeField] private float flashAlpha = 1f;

    [Header("소리 (선택)")]
    [SerializeField] private AudioClip tickClip;
    [Range(0f, 1f)] [SerializeField] private float tickVolume = 0.5f;
    [SerializeField] private AudioClip stopClip;

    [SerializeField] private bool spinOnEnable = true;

    [Tooltip("멈췄을 때 결과 인덱스를 알려준다.")]
    public UnityEvent<int> onResult;

    private readonly List<Image> cells = new List<Image>();
    // 각 칸의 중심 위치(띠 기준). 이미지마다 비율이 달라 높이가 제각각이라 하나씩 기억해둔다.
    private readonly List<float> cellCenters = new List<float>();

    private RectTransform viewport;
    private RectTransform reel;
    private Image flash;
    private Coroutine routine;

    public int Result { get; private set; } = -1;
    public bool IsSpinning => routine != null;

    private void OnEnable()
    {
        if (spinOnEnable)
            Spin();
    }

    private void OnDisable() => routine = null;

    /// <summary>랜덤한 맵으로 돌린다.</summary>
    public void Spin()
    {
        if (mapSprites != null && mapSprites.Length > 0)
            Spin(Random.Range(0, mapSprites.Length));
    }

    /// <summary>결과를 정해놓고 돌린다.</summary>
    public void Spin(int resultIndex)
    {
        if (mapSprites == null || mapSprites.Length == 0)
        {
            Debug.LogWarning($"[{nameof(MapSlotMachine)}] Map Sprites가 비어 있습니다.", this);
            return;
        }

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(SpinRoutine(Mathf.Clamp(resultIndex, 0, mapSprites.Length - 1)));
    }

    private IEnumerator SpinRoutine(int resultIndex)
    {
        if (!Build())
        {
            routine = null;
            yield break;
        }

        int targetCell = (loops - 1) * mapSprites.Length + resultIndex;
        float startY = -cellCenters[0];
        float distance = cellCenters[0] - cellCenters[targetCell];
        float tickStep = distance / Mathf.Max(1, targetCell); // 칸 하나당 평균 이동 거리(딸깍 간격용)

        int lastTick = 0;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float travelled = distance * EaseOutQuart(Mathf.Clamp01(elapsed / duration));
            reel.anchoredPosition = new Vector2(0f, startY + travelled);

            // 한 칸 지날 때마다 딸깍. 느려지면 소리 간격도 저절로 벌어진다.
            int tick = Mathf.FloorToInt(travelled / tickStep);
            if (tick != lastTick)
            {
                lastTick = tick;
                if (tickClip != null)
                    SoundManager.Instance?.PlaySfx(tickClip, tickVolume);
            }

            yield return null;
        }

        reel.anchoredPosition = new Vector2(0f, startY + distance);

        Result = resultIndex;
        if (stopClip != null)
            SoundManager.Instance?.PlaySfx(stopClip);

        yield return Flash();

        routine = null;
        onResult?.Invoke(resultIndex);
    }

    /// <summary>결정된 순간 슬롯 창 안에서만 하얗게 번쩍인다. 플래시 이미지는 창의 자식이라
    /// 마스크에 잘려서 창 밖으로는 새어나가지 않는다.</summary>
    private IEnumerator Flash()
    {
        EnsureFlashImage();
        flash.gameObject.SetActive(true);
        flash.transform.SetAsLastSibling(); // 맵 칸들 위에 덮이게

        // 번쩍 — 밝아지기
        float elapsed = 0f;
        while (elapsed < flashInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetFlashAlpha(flashAlpha * Mathf.Clamp01(elapsed / flashInDuration));
            yield return null;
        }

        SetFlashAlpha(flashAlpha);

        // 사그라들기
        elapsed = 0f;
        while (elapsed < flashOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / flashOutDuration);
            SetFlashAlpha(flashAlpha * (1f - t * t)); // 처음엔 천천히, 끝에서 빠르게 사라진다
            yield return null;
        }

        SetFlashAlpha(0f);
        flash.gameObject.SetActive(false);
    }

    private void EnsureFlashImage()
    {
        if (flash != null)
            return;

        GameObject go = new GameObject("Flash", typeof(RectTransform), typeof(Image));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(viewport, false);
        // 창 전체를 덮게 늘린다.
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        flash = go.GetComponent<Image>();
        flash.raycastTarget = false;
        SetFlashAlpha(0f);
    }

    private void SetFlashAlpha(float alpha)
    {
        if (flash != null)
            flash.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);
    }

    /// <summary>창 안에 "loops 바퀴 × 맵 개수" 만큼 칸을 빈틈없이 이어붙인 띠를 만든다.
    /// 칸 높이는 이미지 원래 비율로 정하므로 늘어나지 않는다.</summary>
    private bool Build()
    {
        if (viewport == null)
        {
            viewport = slotWindow != null ? slotWindow : transform as RectTransform;
            if (viewport == null)
            {
                Debug.LogWarning($"[{nameof(MapSlotMachine)}] Slot Window에 화면에 배치한 UI 오브젝트를 넣어주세요.", this);
                return false;
            }

            // 창 밖으로 나간 칸이 안 보이게.
            if (viewport.GetComponent<RectMask2D>() == null && viewport.GetComponent<Mask>() == null)
                viewport.gameObject.AddComponent<RectMask2D>();

            GameObject reelGo = new GameObject("Reel", typeof(RectTransform));
            reel = (RectTransform)reelGo.transform;
            reel.SetParent(viewport, false);
            reel.anchorMin = reel.anchorMax = reel.pivot = new Vector2(0.5f, 0.5f);
        }

        float width = viewport.rect.width;
        if (width <= 0f)
        {
            Debug.LogWarning($"[{nameof(MapSlotMachine)}] 슬롯 창의 너비가 0입니다. Width를 지정해주세요.", this);
            return false;
        }

        foreach (Image cell in cells)
        {
            if (cell != null)
                Destroy(cell.gameObject);
        }
        cells.Clear();
        cellCenters.Clear();

        int total = loops * mapSprites.Length;

        // 먼저 칸 높이들을 재서 띠 전체 높이를 구한다(이미지마다 비율이 다를 수 있다).
        float[] heights = new float[total];
        float stripHeight = 0f;
        for (int i = 0; i < total; i++)
        {
            heights[i] = HeightFor(mapSprites[i % mapSprites.Length], width);
            stripHeight += heights[i];
        }

        reel.sizeDelta = new Vector2(width, stripHeight);

        // 0번을 맨 위에 두고 아래로 빈틈없이 쌓는다.
        float cursor = stripHeight * 0.5f;
        for (int i = 0; i < total; i++)
        {
            float center = cursor - heights[i] * 0.5f;
            cursor -= heights[i];
            cellCenters.Add(center);

            GameObject go = new GameObject($"Cell {i}", typeof(RectTransform), typeof(Image));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(reel, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width, heights[i]);
            rt.anchoredPosition = new Vector2(0f, center);

            Image image = go.GetComponent<Image>();
            image.sprite = mapSprites[i % mapSprites.Length];
            cells.Add(image);
        }

        return true;
    }

    /// <summary>창 너비에 맞췄을 때의 이미지 원래 높이.</summary>
    private static float HeightFor(Sprite sprite, float width)
    {
        if (sprite == null || sprite.rect.width <= 0f)
            return width;

        return width * (sprite.rect.height / sprite.rect.width);
    }

    private static float EaseOutQuart(float t)
    {
        float x = 1f - t;
        return 1f - x * x * x * x;
    }
}
