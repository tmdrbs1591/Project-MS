using System.Collections;
using System.Text.RegularExpressions;
using ProjectMS.CharacterSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터 선택 패널(CharacterSelectUI) 아래쪽의 설명 영역. Show(definition) 한 번으로 이름/역할/소개/
/// 대표 이미지/스킬 탭을 전부 채운다. 내용은 전부 CharacterDefinition의 "Character Info" 항목에서 온다.
///
/// 왼쪽 스킬 탭(CharacterSkillInfoRow)을 누르면 오른쪽 상세 영역에 그 스킬의 이름, 종류 뱃지
/// (패시브/일반 스킬/궁극기), 키, 쿨타임, 설명이 뜬다. 설명 속 숫자(30%, 5초 등)는 자동으로 강조색이 된다.
/// 캐릭터를 바꾸거나 탭을 바꿀 때 내용이 살짝 밀려 들어오며 페이드인한다.
///
/// [씬 설정]
///   - CharacterSelectUI 패널 안에 설명 영역 오브젝트를 만들고 이 스크립트를 붙인다.
///   - 스킬 탭마다 CharacterSkillInfoRow를 붙여 skillRows에 넣는다
///     (Q/E/대시/궁은 actionType으로, 패시브는 isPassive를 켜서 구분).
///   - CharacterSelectUI의 infoPanel 필드에 이 컴포넌트를 연결한다.
/// </summary>
public class CharacterInfoPanel : MonoBehaviour
{
    private static readonly Regex NumberPattern = new Regex(@"\d+(\.\d+)?\s*(%|초|s\b|m\b)?");

    [Header("캐릭터")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text roleText;
    [SerializeField] private TMP_Text descriptionText;
    [Tooltip("대표 이미지. CharacterDefinition의 Portrait가 비어 있으면 궁극기 아이콘을 대신 쓴다.")]
    [SerializeField] private Image portraitImage;
    [Tooltip("캐릭터가 바뀔 때 살짝 밀려 들어오는 연출을 줄 묶음(이름/역할/소개/대표 이미지).")]
    [SerializeField] private CanvasGroup headerGroup;

    [Header("능력치 막대")]
    [Tooltip("공격력/체력/기동성/스킬 회전 순서. 값은 CharacterDefinition 수치에서 계산하고, 선택 화면에 있는\n" +
        "캐릭터들끼리 비교한 상대값으로 막대를 채운다(SetStatRange).")]
    [SerializeField] private StatBar[] statBars;
    [Min(0.1f)] [SerializeField] private float statFillSpeed = 6f;

    [Header("스킬 탭")]
    [SerializeField] private CharacterSkillInfoRow[] skillRows;

    [Header("스킬 상세")]
    [SerializeField] private CanvasGroup detailGroup;
    [SerializeField] private TMP_Text skillNameText;
    [SerializeField] private Image typeBadge;
    [SerializeField] private TMP_Text typeBadgeText;
    [SerializeField] private GameObject keyChip;
    [SerializeField] private TMP_Text keyChipText;
    [SerializeField] private GameObject cooldownGroup;
    [SerializeField] private TMP_Text cooldownText;
    [SerializeField] private TMP_Text skillDescriptionText;

    [Header("스킬 미리보기")]
    [Tooltip("상세 영역 오른쪽의 큰 스킬 아이콘 묶음. 스킬을 바꾸면 튕기듯 새로 나타나고 평소엔 둥실 떠 있는다.")]
    [SerializeField] private RectTransform skillPreview;
    [SerializeField] private Image skillPreviewIcon;
    [Tooltip("아이콘이 없는 스킬(패시브 등)일 때 대신 보여줄 큰 글자.")]
    [SerializeField] private TMP_Text skillPreviewLetter;
    [Tooltip("아이콘 뒤에서 천천히 도는 빛. 스킬 종류 색(뱃지 색)으로 물든다.")]
    [SerializeField] private Image skillPreviewGlow;

    [Header("색")]
    [SerializeField] private Color passiveBadgeColor = new Color(0.55f, 0.36f, 1f, 1f);
    [SerializeField] private Color skillBadgeColor = new Color(0.12f, 0.52f, 1f, 1f);
    [SerializeField] private Color ultimateBadgeColor = new Color(1f, 0.55f, 0.12f, 1f);
    [Tooltip("설명 속 숫자 강조색.")]
    [SerializeField] private Color highlightColor = new Color(1f, 0.83f, 0.3f, 1f);

    [Header("연출")]
    [Min(0.01f)] [SerializeField] private float contentFadeDuration = 0.22f;
    [SerializeField] private float contentSlideDistance = 14f;
    [Min(0.01f)] [SerializeField] private float introDuration = 0.35f;
    [SerializeField] private float introSlideDistance = 50f;

    public enum StatKind
    {
        Attack,
        Health,
        Mobility,
        SkillHaste,
    }

    [System.Serializable]
    private sealed class StatBar
    {
        public StatKind kind;
        [Tooltip("Image Type = Filled(Horizontal)인 막대 이미지.")]
        public Image fill;
        [Tooltip("S/A/B/C 등급 글자(선택 사항).")]
        public TMP_Text grade;
        [HideInInspector] public float target;
        [HideInInspector] public float min;
        [HideInInspector] public float max;
    }

    private CharacterDefinition current;
    private float previewPopTime = -10f;
    private CharacterSkillInfoRow selectedRow;
    private Coroutine headerRoutine;
    private Coroutine detailRoutine;
    private Coroutine introRoutine;
    private RectTransform rectTransform;
    private Vector2 basePosition;
    private Vector2 headerBase;
    private Vector2 detailBase;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        if (rectTransform != null)
            basePosition = rectTransform.anchoredPosition;
        if (headerGroup != null)
            headerBase = ((RectTransform)headerGroup.transform).anchoredPosition;
        if (detailGroup != null)
            detailBase = ((RectTransform)detailGroup.transform).anchoredPosition;

        if (skillRows == null)
            return;

        foreach (CharacterSkillInfoRow row in skillRows)
        {
            if (row != null)
                row.Clicked += SelectSkill;
        }
    }

    private void OnDisable()
    {
        headerRoutine = null;
        detailRoutine = null;
        introRoutine = null;
        if (rectTransform != null)
            rectTransform.anchoredPosition = basePosition;
        ResetGroup(headerGroup, headerBase);
        ResetGroup(detailGroup, detailBase);
    }

    private void Update()
    {
        UpdateSkillPreview();

        if (statBars == null)
            return;

        float k = 1f - Mathf.Exp(-statFillSpeed * Time.unscaledDeltaTime);
        foreach (StatBar bar in statBars)
        {
            if (bar != null && bar.fill != null)
                bar.fill.fillAmount = Mathf.Lerp(bar.fill.fillAmount, bar.target, k);
        }
    }

    /// <summary>선택 화면에 나오는 캐릭터들의 능력치 범위를 잡는다. 막대는 이 범위 안에서의 상대값으로 찬다.</summary>
    public void SetStatRange(System.Collections.Generic.IEnumerable<CharacterDefinition> definitions)
    {
        if (statBars == null)
            return;

        foreach (StatBar bar in statBars)
        {
            if (bar == null)
                continue;
            bar.min = float.MaxValue;
            bar.max = float.MinValue;
            foreach (CharacterDefinition d in definitions)
            {
                if (d == null)
                    continue;
                float v = StatValue(d, bar.kind);
                bar.min = Mathf.Min(bar.min, v);
                bar.max = Mathf.Max(bar.max, v);
            }
        }
    }

    /// <summary>능력치 원본 값. 공격력은 초당 피해(평타 + Q + E), 기동성은 이동속도 + 대시 빈도,
    /// 스킬 회전은 Q/E 평균 쿨타임의 역수다.</summary>
    public static float StatValue(CharacterDefinition d, StatKind kind)
    {
        switch (kind)
        {
            case StatKind.Attack:
                return d.GetDamage(CharacterActionType.BasicAttack) / Mathf.Max(d.GetCooldown(CharacterActionType.BasicAttack), 0.05f)
                    + d.GetDamage(CharacterActionType.SkillQ) / Mathf.Max(d.GetCooldown(CharacterActionType.SkillQ), 0.5f)
                    + d.GetDamage(CharacterActionType.SkillE) / Mathf.Max(d.GetCooldown(CharacterActionType.SkillE), 0.5f);
            case StatKind.Health:
                return d.MaxHealth;
            case StatKind.Mobility:
                return d.MoveSpeed + d.DefaultDashPower / Mathf.Max(d.GetCooldown(CharacterActionType.Dash), 0.5f);
            case StatKind.SkillHaste:
                return 1f / Mathf.Max((d.GetCooldown(CharacterActionType.SkillQ) + d.GetCooldown(CharacterActionType.SkillE)) * 0.5f, 0.1f);
            default:
                return 0f;
        }
    }

    private void ShowStats(CharacterDefinition definition, bool restart)
    {
        if (statBars == null)
            return;

        foreach (StatBar bar in statBars)
        {
            if (bar == null)
                continue;

            float t = 0.65f;
            if (definition != null && bar.max > bar.min)
                t = Mathf.InverseLerp(bar.min, bar.max, StatValue(definition, bar.kind));
            // 제일 낮은 캐릭터도 막대가 텅 비어 보이지 않게 30%부터 시작한다.
            bar.target = definition != null ? Mathf.Lerp(0.3f, 1f, t) : 0f;

            // 캐릭터가 바뀌면 막대를 비웠다가 다시 차오르게 해서 변화가 눈에 보이게 한다.
            if (restart && bar.fill != null)
                bar.fill.fillAmount = 0f;

            if (bar.grade != null)
                bar.grade.text = definition == null ? string.Empty : bar.target >= 0.9f ? "S" : bar.target >= 0.7f ? "A" : bar.target >= 0.5f ? "B" : "C";
        }
    }

    public void Show(CharacterDefinition definition)
    {
        bool changed = definition != current;
        current = definition;
        ShowStats(definition, changed);

        SetText(nameText, definition != null ? definition.DisplayName : null);
        SetText(roleText, definition != null ? definition.Role : null);
        SetText(descriptionText, definition != null ? definition.Description : null);

        if (portraitImage != null)
        {
            Sprite portrait = null;
            if (definition != null)
                portrait = definition.Portrait != null ? definition.Portrait : definition.GetIcon(CharacterActionType.Ultimate);
            portraitImage.sprite = portrait;
            portraitImage.enabled = portrait != null;
        }

        CharacterSkillInfoRow firstVisible = null;
        if (skillRows != null)
        {
            foreach (CharacterSkillInfoRow row in skillRows)
            {
                if (row == null)
                    continue;
                row.Show(definition);
                if (firstVisible == null && row.HasContent)
                    firstVisible = row;
            }
        }

        // 같은 캐릭터를 다시 보여줄 땐(호버 미리보기 왕복 등) 보고 있던 탭을 유지한다.
        CharacterSkillInfoRow target = !changed && selectedRow != null && selectedRow.HasContent ? selectedRow : firstVisible;
        ApplySkill(target, animate: changed);

        if (changed)
            PlayContent(headerGroup, headerBase, ref headerRoutine);
    }

    /// <summary>패널이 열릴 때 아래에서 밀려 올라오며 나타난다.</summary>
    public void PlayIntro(float delay)
    {
        if (!isActiveAndEnabled || rectTransform == null)
            return;

        if (introRoutine != null)
            StopCoroutine(introRoutine);
        introRoutine = StartCoroutine(IntroRoutine(delay));
    }

    /// <summary>키보드 조작용: 보이는 스킬 탭 중 다음 것으로 넘긴다(끝이면 처음으로).</summary>
    public void SelectNextSkill()
    {
        if (skillRows == null || skillRows.Length == 0)
            return;

        int start = System.Array.IndexOf(skillRows, selectedRow);
        for (int i = 1; i <= skillRows.Length; i++)
        {
            CharacterSkillInfoRow row = skillRows[(start + i + skillRows.Length) % skillRows.Length];
            if (row != null && row.HasContent)
            {
                row.PlayClickSound();
                SelectSkill(row);
                return;
            }
        }
    }

    /// <summary>키보드 조작용: 패시브/Q/E/궁 탭을 바로 고른다.</summary>
    public void SelectSkill(bool passive, CharacterActionType action)
    {
        if (skillRows == null)
            return;

        foreach (CharacterSkillInfoRow row in skillRows)
        {
            if (row != null && row.HasContent && row.IsPassive == passive && (passive || row.ActionType == action))
            {
                if (row != selectedRow)
                    row.PlayClickSound();
                SelectSkill(row);
                return;
            }
        }
    }

    private void SelectSkill(CharacterSkillInfoRow row)
    {
        if (row == selectedRow)
            return;
        ApplySkill(row, animate: true);
    }

    private void ApplySkill(CharacterSkillInfoRow row, bool animate)
    {
        selectedRow = row;

        if (skillRows != null)
        {
            foreach (CharacterSkillInfoRow r in skillRows)
            {
                if (r != null)
                    r.SetSelected(r == row);
            }
        }

        if (row == null)
        {
            SetText(skillNameText, null);
            SetText(skillDescriptionText, null);
            SetActive(typeBadge != null ? typeBadge.gameObject : null, false);
            SetActive(keyChip, false);
            SetActive(cooldownGroup, false);
            SetActive(skillPreview != null ? skillPreview.gameObject : null, false);
            return;
        }

        SetText(skillNameText, row.Info.name);
        SetText(skillDescriptionText, Highlight(row.Info.description));

        bool isUltimate = !row.IsPassive && row.ActionType == CharacterActionType.Ultimate;
        SetActive(typeBadge != null ? typeBadge.gameObject : null, true);
        if (typeBadge != null)
            typeBadge.color = row.IsPassive ? passiveBadgeColor : isUltimate ? ultimateBadgeColor : skillBadgeColor;
        SetText(typeBadgeText, row.IsPassive ? "패시브" : isUltimate ? "궁극기" : "일반 스킬");

        string key = row.IsPassive ? null : KeyLabel(row.ActionType);
        SetActive(keyChip, !string.IsNullOrEmpty(key));
        SetText(keyChipText, key);

        string cooldown = row.IsPassive ? null : CooldownLabel(row.ActionType);
        SetActive(cooldownGroup, !string.IsNullOrEmpty(cooldown));
        SetText(cooldownText, cooldown);

        ShowSkillPreview(row, typeBadge != null ? typeBadge.color : skillBadgeColor);

        if (animate)
            PlayContent(detailGroup, detailBase, ref detailRoutine);
    }

    // ── 스킬 미리보기 ─────────────────────────────────────────────────────────

    private void ShowSkillPreview(CharacterSkillInfoRow row, Color tint)
    {
        if (skillPreview == null)
            return;

        skillPreview.gameObject.SetActive(true);

        Sprite icon = row.Icon;
        if (skillPreviewIcon != null)
        {
            skillPreviewIcon.sprite = icon;
            skillPreviewIcon.enabled = icon != null;
        }
        if (skillPreviewLetter != null)
        {
            skillPreviewLetter.gameObject.SetActive(icon == null);
            skillPreviewLetter.text = row.IsPassive ? "P" : KeyLabel(row.ActionType);
        }
        if (skillPreviewGlow != null)
        {
            tint.a = skillPreviewGlow.color.a;
            skillPreviewGlow.color = tint;
        }

        previewPopTime = Time.unscaledTime;
    }

    /// <summary>바뀐 직후엔 작게 기울어진 채로 튕기듯 커지며 나타나고, 그 뒤로는 둥실 떠 있으며 뒤의 빛이 천천히 돈다.</summary>
    private void UpdateSkillPreview()
    {
        if (skillPreview == null || !skillPreview.gameObject.activeInHierarchy)
            return;

        float now = Time.unscaledTime;
        float pop = Mathf.Clamp01((now - previewPopTime) / 0.35f);
        const float c1 = 1.9f;
        float x = pop - 1f;
        float popScale = 1f + (c1 + 1f) * x * x * x + c1 * x * x;
        float scale = Mathf.LerpUnclamped(0.55f, 1f, popScale);
        float tilt = Mathf.Lerp(-18f, 0f, popScale);

        skillPreview.localScale = new Vector3(scale, scale, 1f);
        skillPreview.localRotation = Quaternion.Euler(0f, 0f, tilt + Mathf.Sin(now * 1.6f) * 2.5f);
        if (skillPreviewIcon != null)
            skillPreviewIcon.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Sin(now * 2.2f) * 4f);
        if (skillPreviewGlow != null)
            skillPreviewGlow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, now * 25f);
    }

    private string CooldownLabel(CharacterActionType action)
    {
        if (current == null)
            return null;

        if (action == CharacterActionType.Ultimate && current.UltimateUsesGauge)
            return "게이지 충전";

        float cooldown = current.GetCooldown(action);
        if (cooldown <= 0f)
            return null;

        return "쿨타임 " + cooldown.ToString("0.#") + "s";
    }

    private static string KeyLabel(CharacterActionType action)
    {
        switch (action)
        {
            case CharacterActionType.SkillQ: return KeyBindings.GetKeyLabel(BindableAction.SkillQ);
            case CharacterActionType.SkillE: return KeyBindings.GetKeyLabel(BindableAction.SkillE);
            case CharacterActionType.Dash: return KeyBindings.GetKeyLabel(BindableAction.Dash);
            case CharacterActionType.Ultimate: return KeyBindings.GetKeyLabel(BindableAction.Ultimate);
            default: return null;
        }
    }

    private string Highlight(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        string hex = ColorUtility.ToHtmlStringRGB(highlightColor);
        return NumberPattern.Replace(text, m => "<color=#" + hex + ">" + m.Value + "</color>");
    }

    // ── 연출 ─────────────────────────────────────────────────────────────────

    private void PlayContent(CanvasGroup group, Vector2 basePos, ref Coroutine routine)
    {
        if (group == null || !isActiveAndEnabled)
            return;

        if (routine != null)
            StopCoroutine(routine);
        routine = StartCoroutine(ContentRoutine(group, basePos));
    }

    /// <summary>내용이 오른쪽에서 살짝 밀려 들어오며 페이드인한다.</summary>
    private IEnumerator ContentRoutine(CanvasGroup group, Vector2 basePos)
    {
        RectTransform rt = (RectTransform)group.transform;
        float elapsed = 0f;
        while (elapsed < contentFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOutCubic(Mathf.Clamp01(elapsed / contentFadeDuration));
            group.alpha = t;
            rt.anchoredPosition = basePos + new Vector2((1f - t) * contentSlideDistance, 0f);
            yield return null;
        }

        ResetGroup(group, basePos);
    }

    private IEnumerator IntroRoutine(float delay)
    {
        CanvasGroup group = GetComponent<CanvasGroup>();
        if (group != null)
            group.alpha = 0f;
        rectTransform.anchoredPosition = basePosition - new Vector2(0f, introSlideDistance);

        float wait = 0f;
        while (wait < delay)
        {
            wait += Time.unscaledDeltaTime;
            yield return null;
        }

        float elapsed = 0f;
        while (elapsed < introDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOutCubic(Mathf.Clamp01(elapsed / introDuration));
            if (group != null)
                group.alpha = t;
            rectTransform.anchoredPosition = basePosition - new Vector2(0f, (1f - t) * introSlideDistance);
            yield return null;
        }

        if (group != null)
            group.alpha = 1f;
        rectTransform.anchoredPosition = basePosition;
        introRoutine = null;
    }

    private static void ResetGroup(CanvasGroup group, Vector2 basePos)
    {
        if (group == null)
            return;
        group.alpha = 1f;
        ((RectTransform)group.transform).anchoredPosition = basePos;
    }

    private static float EaseOutCubic(float t)
    {
        float x = 1f - t;
        return 1f - x * x * x;
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value ?? string.Empty;
    }

    private static void SetActive(GameObject go, bool active)
    {
        if (go != null)
            go.SetActive(active);
    }
}
