using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 맵 슬롯머신. 맵 이미지들이 두루마리처럼 세로로 빈틈없이 이어져 위로 굴러가다가,
/// 점점 느려지면서 결과 맵에 딱 멈추고 한 번 커졌다 돌아온다.
///
/// 이미지의 원래 비율을 유지하고, 처음 배치한 영역 안에서 결과 맵 하나에 창 크기를 정확히 맞춘다.
/// 멈췄을 때 위아래의 다른 맵이 보이거나 결과 맵이 잘리지 않는다.
///
/// [쓰는 법]
///   화면에 배치해둔 UI 오브젝트(슬롯 창)에 붙이고 Map Sprites만 채우면 끝.
///   그 오브젝트의 크기를 최대 크기로 사용하고, 마스크·띠·칸은 실행할 때 자동으로 만들어진다.
///   - 스크립트를 다른 곳에 두려면 Slot Window에 창 오브젝트를 넣는다.
///   - 창 바깥에 별도 마스크와 테두리가 있으면 Slot Frame / Slot Border에 넣어 함께 맞춘다.
///   - 돌리기: Spin() / Spin(결과인덱스). Spin On Enable을 켜면 켜질 때 자동으로 돈다.
///   - 등장 전에 크기를 맞추려면 Prepare()를 호출하고, 반환된 인덱스로 Spin(결과인덱스)을 호출한다.
///   - Use Map Manager(기본 켬): 맵 매니저에 등록된 맵들의 대표 이미지(MapDefinition.Thumbnail)를
///     쓰고, 실제로 깔린 맵에서 멈춘다. 대표 이미지가 하나라도 비어 있으면 Map Sprites로 무작위로 돈다.
/// </summary>
public class MapSlotMachine : MonoBehaviour
{
    [Tooltip("돌릴 맵 이미지들. 배열 순서 = 결과 인덱스.")]
    [SerializeField] private Sprite[] mapSprites;

    [Tooltip("이미 화면에 배치해둔 슬롯 창(RectTransform). 비워두면 이 오브젝트 자신을 쓴다.")]
    [SerializeField] private RectTransform slotWindow;

    [Tooltip("슬롯 창을 감싸는 마스크. 지정하면 창과 함께 크기를 맞춘다.")]
    [SerializeField] private RectTransform slotFrame;
    [Tooltip("슬롯 테두리. 처음 설정한 창과의 크기 차이를 유지하며 함께 맞춘다.")]
    [SerializeField] private RectTransform slotBorder;

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

    [Header("맵 이름 (선택)")]
    [Tooltip("도는 동안 지금 창 가운데를 지나는 맵 이름이 따라 바뀌고, 멈추면 결과 이름이 '띵' 하고 박힌다.")]
    [SerializeField] private TMPro.TMP_Text nameLabel;
    [Tooltip("맵 매니저를 안 쓸 때의 이름들(배열 순서 = 결과 인덱스).")]
    [SerializeField] private string[] mapNames;
    [SerializeField] private Color nameRollingColor = new Color(0.75f, 0.8f, 0.95f, 1f);
    [SerializeField] private Color nameDecidedColor = new Color(1f, 0.86f, 0.4f, 1f);

    [SerializeField] private bool spinOnEnable = true;
    [Tooltip("켜면 맵 매니저의 맵 대표 이미지를 쓰고, 실제로 시작하는 맵에서 멈춘다.")]
    [SerializeField] private bool useMapManager = true;

    [Tooltip("멈췄을 때 결과 인덱스를 알려준다.")]
    public UnityEvent<int> onResult;

    private readonly List<Image> cells = new List<Image>();
    // 각 칸의 중심 위치(띠 기준). 이미지마다 비율이 달라 높이가 제각각이라 하나씩 기억해둔다.
    private readonly List<float> cellCenters = new List<float>();

    private RectTransform viewport;
    private RectTransform reel;
    private Image previewImage;
    private Image flash;
    private Coroutine routine;
    private Vector2 initialWindowSize;
    private Vector2 frameSizeOffset;
    private Vector2 borderSizeOffset;

    public int Result { get; private set; } = -1;
    public bool IsSpinning => routine != null;

    private void OnEnable()
    {
        if (spinOnEnable)
            Spin();
    }

    private void OnDisable() => routine = null;

    /// <summary>실제 맵(맵 매니저 연동 시) 또는 랜덤한 맵으로 돌린다.</summary>
    public void Spin()
    {
        int resultIndex = ResolveResultIndex();
        if (resultIndex >= 0)
            Spin(resultIndex);
    }

    /// <summary>회전은 시작하지 않고 결과 맵에 맞춰 창과 등장용 이미지를 준비한다. 실패하면 -1을 반환한다.</summary>
    public int Prepare()
    {
        int resultIndex = ResolveResultIndex();
        if (resultIndex < 0 || !PrepareViewport(resultIndex))
            return -1;

        // 창과 같은 비율의 이미지를 보여줘 등장 중에도 양옆에 빈 공간이 생기지 않게 한다.
        // 이전 라운드의 띠와 플래시가 새 등장용 이미지를 가리지 않도록 숨긴다.
        reel.gameObject.SetActive(false);
        if (flash != null)
            flash.gameObject.SetActive(false);
        if (previewImage != null)
        {
            previewImage.sprite = mapSprites[resultIndex];
            previewImage.type = Image.Type.Simple;
            previewImage.preserveAspect = true;
        }

        return resultIndex;
    }

    private int ResolveResultIndex()
    {
        MapManager maps = useMapManager ? MapManager.Instance : null;
        Sprite[] thumbnails = maps != null ? maps.GetMapThumbnails() : null;
        if (thumbnails != null)
        {
            // 이미지와 이름을 같은 순서(맵 매니저)에서 가져와야 굴러가는 이름이 그림과 맞는다.
            // 연습장처럼 실제 맵이 안 정해졌으면 아무 맵에서나 멈춘다.
            mapSprites = thumbnails;
            mapNames = maps.GetMapNames();
            return maps.ActiveMapIndex >= 0
                ? Mathf.Clamp(maps.ActiveMapIndex, 0, mapSprites.Length - 1)
                : Random.Range(0, mapSprites.Length);
        }

        return mapSprites != null && mapSprites.Length > 0 ? Random.Range(0, mapSprites.Length) : -1;
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
        // 로딩 화면이 떠 있는 동안 돌면 아무도 못 본다 — 로딩이 걷힌 뒤에 돌기 시작한다.
        while (SceneTransition.Instance != null && SceneTransition.Instance.IsBusy)
            yield return null;

        if (!Build(resultIndex))
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
            // 지금 창 가운데를 지나는 칸의 맵 이름.
            SetName(NearestCell(-(startY + travelled)) % mapSprites.Length, false);

            yield return null;
        }

        reel.anchoredPosition = new Vector2(0f, -cellCenters[targetCell]);

        // 정지 후에는 이웃 칸을 숨겨 경계의 소수점 오차로 다른 맵이 비치는 것도 막는다.
        for (int i = 0; i < cells.Count; i++)
            cells[i].gameObject.SetActive(i == targetCell);

        Result = resultIndex;
        if (stopClip != null)
            SoundManager.Instance?.PlaySfx(stopClip);
        SetName(resultIndex, true);
        if (nameLabel != null)
            StartCoroutine(NamePunch());

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

    /// <summary>맵을 굴리기 전에도 마스크 창과 테두리를 결과 맵 크기에 맞출 수 있게 준비한다.</summary>
    private bool PrepareViewport(int resultIndex)
    {
        if (viewport == null)
        {
            viewport = slotWindow != null ? slotWindow : transform as RectTransform;
            if (viewport == null)
            {
                Debug.LogWarning($"[{nameof(MapSlotMachine)}] Slot Window에 화면에 배치한 UI 오브젝트를 넣어주세요.", this);
                return false;
            }

            previewImage = viewport.GetComponent<Image>();

            // 창 밖으로 나간 칸이 안 보이게.
            if (viewport.GetComponent<RectMask2D>() == null && viewport.GetComponent<Mask>() == null)
                viewport.gameObject.AddComponent<RectMask2D>();

            GameObject reelGo = new GameObject("Reel", typeof(RectTransform));
            reel = (RectTransform)reelGo.transform;
            reel.SetParent(viewport, false);
            reel.anchorMin = reel.anchorMax = reel.pivot = new Vector2(0.5f, 0.5f);
        }

        if (initialWindowSize.x <= 0f || initialWindowSize.y <= 0f)
        {
            initialWindowSize = viewport.rect.size;
            if (initialWindowSize.x <= 0f || initialWindowSize.y <= 0f)
            {
                Debug.LogWarning($"[{nameof(MapSlotMachine)}] 슬롯 창의 크기가 0입니다. Width와 Height를 지정해주세요.", this);
                return false;
            }

            frameSizeOffset = slotFrame != null ? slotFrame.rect.size - initialWindowSize : Vector2.zero;
            borderSizeOffset = slotBorder != null ? slotBorder.rect.size - initialWindowSize : Vector2.zero;
        }

        FitWindow(mapSprites[resultIndex]);
        return true;
    }

    /// <summary>창 안에 "loops 바퀴 × 맵 개수" 만큼 칸을 빈틈없이 이어붙인 띠를 만든다.
    /// 칸 높이는 이미지 원래 비율로 정하므로 늘어나지 않는다.</summary>
    private bool Build(int resultIndex)
    {
        if (!PrepareViewport(resultIndex))
            return false;

        float width = viewport.rect.width;

        foreach (Image cell in cells)
        {
            if (cell != null)
            {
                cell.gameObject.SetActive(false);
                Destroy(cell.gameObject);
            }
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

        reel.gameObject.SetActive(true);
        return true;
    }

    private void FitWindow(Sprite sprite)
    {
        // 매번 최초 크기를 기준으로 계산해 라운드가 바뀔 때 창이 계속 작아지지 않게 한다.
        float height = HeightFor(sprite, initialWindowSize.x);
        float scale = Mathf.Min(1f, initialWindowSize.y / height);
        float width = initialWindowSize.x * scale;
        Vector2 size = new Vector2(width, HeightFor(sprite, width));

        // 부모 마스크부터 조정한 뒤, 늘이기 앵커를 사용하는 자식 창도 같은 크기로 맞춘다.
        if (slotFrame != null)
            SetSize(slotFrame, size + frameSizeOffset);
        SetSize(viewport, size);
        if (slotBorder != null)
            SetSize(slotBorder, size + borderSizeOffset);
    }

    private static void SetSize(RectTransform target, Vector2 size)
    {
        target.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
        target.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
    }

    private int NearestCell(float centerY)
    {
        int best = 0;
        float bestDist = float.MaxValue;
        for (int i = 0; i < cellCenters.Count; i++)
        {
            float d = Mathf.Abs(cellCenters[i] - centerY);
            if (d < bestDist)
            {
                bestDist = d;
                best = i;
            }
        }
        return best;
    }

    private void SetName(int index, bool decided)
    {
        if (nameLabel == null)
            return;
        string text = mapNames != null && index >= 0 && index < mapNames.Length ? mapNames[index] : string.Empty;
        if (nameLabel.text != text)
            nameLabel.text = text;
        nameLabel.color = decided ? nameDecidedColor : nameRollingColor;
    }

    /// <summary>결정된 이름이 크게 튀었다가 자리 잡으며 하얗게 번쩍인다.</summary>
    private IEnumerator NamePunch()
    {
        RectTransform rt = nameLabel.rectTransform;
        Vector3 baseScale = Vector3.one;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.35f)
        {
            float k = 1f + Mathf.Sin(t * Mathf.PI) * (1f - t * 0.5f) * 0.45f;
            rt.localScale = baseScale * k;
            nameLabel.color = Color.Lerp(Color.white, nameDecidedColor, t);
            yield return null;
        }
        rt.localScale = baseScale;
        nameLabel.color = nameDecidedColor;
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
