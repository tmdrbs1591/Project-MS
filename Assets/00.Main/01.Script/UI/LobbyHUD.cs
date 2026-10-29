using ProjectMS.CharacterSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로비 화면 HUD 연출: 오른쪽 위 프로필 카드(지금 고른 캐릭터 그림/이름)와, 로비에 들어올 때
/// 화면 가장자리 UI들이 살며시 밀려 들어오는 등장 연출.
///
/// [씬 설정]
///   - 로비 Canvas 아래 아무 오브젝트에 붙인다.
///   - portraitImage / characterNameText: 비워두면 해당 표시는 건너뛴다.
///   - slideIns: 등장 때 밀려 들어올 묶음들(각각 CanvasGroup이 있으면 알파도 같이 올린다).
/// </summary>
public class LobbyHUD : MonoBehaviour
{
    [System.Serializable]
    private struct SlideIn
    {
        public RectTransform target;
        [Tooltip("어느 쪽에서 들어오는지(픽셀). 예: 왼쪽 위 묶음이면 (-60, 0).")]
        public Vector2 from;
        public float delay;
    }

    [Header("프로필 카드")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text characterNameText;
    [Tooltip("캐릭터가 바뀌면 톡 튀는 칸(프로필 그림 테두리 등).")]
    [SerializeField] private RectTransform punchTarget;

    [Header("등장 연출")]
    [SerializeField] private SlideIn[] slideIns;
    [Min(0.05f)] [SerializeField] private float slideDuration = 0.5f;

    private CharacterDefinition shownDefinition;
    private float punch;
    private float startTime;
    private float introLength;
    private bool introDone;
    private Vector2[] basePositions;
    private CanvasGroup[] groups;

    private void Awake()
    {
        basePositions = new Vector2[slideIns.Length];
        groups = new CanvasGroup[slideIns.Length];
        for (int i = 0; i < slideIns.Length; i++)
        {
            if (slideIns[i].target == null)
                continue;
            basePositions[i] = slideIns[i].target.anchoredPosition;
            groups[i] = slideIns[i].target.GetComponent<CanvasGroup>();
            introLength = Mathf.Max(introLength, slideIns[i].delay + slideDuration);
        }
        startTime = -1f;
        ApplySlide(0f);
    }

    private void Update()
    {
        // 로딩 화면이 걷힌 뒤에 들어오게 한다.
        bool loading = SceneTransition.Instance != null && SceneTransition.Instance.IsBusy;
        if (startTime < 0f && !loading)
            startTime = Time.unscaledTime;
        // 다 들어온 뒤로는 손대지 않는다(창이 열릴 때 다른 UI가 이 묶음들을 숨기기 때문).
        if (!introDone)
        {
            float elapsed = startTime < 0f ? 0f : Time.unscaledTime - startTime;
            ApplySlide(elapsed);
            introDone = startTime >= 0f && elapsed >= introLength;
        }

        UpdateProfile();
    }

    private void ApplySlide(float elapsed)
    {
        for (int i = 0; i < slideIns.Length; i++)
        {
            SlideIn s = slideIns[i];
            if (s.target == null)
                continue;
            float t = Mathf.Clamp01((elapsed - s.delay) / slideDuration);
            float e = 1f - Mathf.Pow(1f - t, 3f);
            s.target.anchoredPosition = basePositions[i] + s.from * (1f - e);
            if (groups[i] != null)
                groups[i].alpha = e;
        }
    }

    private void UpdateProfile()
    {
        LobbyCharacterChange change = LobbyCharacterChange.Instance;
        CharacterDefinition definition = change != null ? change.CurrentDefinition : null;
        if (definition != shownDefinition)
        {
            if (shownDefinition != null)
                punch = 1f;
            shownDefinition = definition;
            if (characterNameText != null)
                characterNameText.text = definition != null ? definition.DisplayName : string.Empty;
            if (portraitImage != null)
            {
                Sprite sprite = definition == null ? null : definition.Portrait != null ? definition.Portrait : definition.CharacterMainSprite;
                portraitImage.sprite = sprite;
                portraitImage.enabled = sprite != null;
                portraitImage.preserveAspect = true;
            }
        }

        if (punchTarget != null)
        {
            punch = Mathf.MoveTowards(punch, 0f, Time.unscaledDeltaTime * 4f);
            float s = 1f + Mathf.Sin(punch * Mathf.PI) * 0.12f;
            punchTarget.localScale = new Vector3(s, s, 1f);
        }
    }
}
