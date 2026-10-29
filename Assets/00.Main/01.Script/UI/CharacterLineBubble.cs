using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 캐릭터 선택 화면에서 캐릭터를 골랐을 때 그 카드 위에 뜨는 말풍선(선택 대사).
/// 톡 튀어나오며 글자가 한 자씩 타이핑되고, 잠깐 머물렀다가 사라진다.
///
/// [씬 설정]
///   - 카드들과 같은 부모(Card Row) 아래에 말풍선 오브젝트를 두고 이 스크립트를 붙인다.
///   - 대사는 CharacterDefinition의 selectLine에서 온다(CharacterSelectUI가 Show를 부른다).
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class CharacterLineBubble : MonoBehaviour
{
    [SerializeField] private TMP_Text lineText;
    [Tooltip("카드 위치 기준으로 말풍선을 띄울 높이.")]
    [SerializeField] private float heightAboveCard = 215f;
    [Min(1f)] [SerializeField] private float charactersPerSecond = 28f;
    [Min(0f)] [SerializeField] private float holdDuration = 2.2f;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Coroutine routine;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = (RectTransform)transform;
        canvasGroup.alpha = 0f;
    }

    private void OnDisable()
    {
        routine = null;
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    public void Show(string line, RectTransform card)
    {
        if (string.IsNullOrEmpty(line) || card == null || !isActiveAndEnabled)
            return;

        rectTransform.anchoredPosition = card.anchoredPosition + new Vector2(0f, heightAboveCard);
        if (routine != null)
            StopCoroutine(routine);
        routine = StartCoroutine(ShowRoutine(line));
    }

    private IEnumerator ShowRoutine(string line)
    {
        lineText.text = line;
        lineText.maxVisibleCharacters = 0;

        // 톡 튀어나오기
        float elapsed = 0f;
        const float popDuration = 0.2f;
        while (elapsed < popDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / popDuration);
            float s = Mathf.LerpUnclamped(0.6f, 1f, EaseOutBack(t));
            rectTransform.localScale = new Vector3(s, s, 1f);
            canvasGroup.alpha = t;
            lineText.maxVisibleCharacters = Mathf.FloorToInt(elapsed * charactersPerSecond);
            yield return null;
        }

        // 한 자씩 타이핑
        int total = line.Length;
        float typed = lineText.maxVisibleCharacters;
        while (typed < total)
        {
            typed += Time.unscaledDeltaTime * charactersPerSecond;
            lineText.maxVisibleCharacters = Mathf.FloorToInt(typed);
            yield return null;
        }
        lineText.maxVisibleCharacters = total;

        float wait = 0f;
        while (wait < holdDuration)
        {
            wait += Time.unscaledDeltaTime;
            yield return null;
        }

        // 위로 살짝 떠오르며 사라짐
        Vector2 start = rectTransform.anchoredPosition;
        elapsed = 0f;
        const float fadeDuration = 0.3f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            canvasGroup.alpha = 1f - t;
            rectTransform.anchoredPosition = start + new Vector2(0f, t * 12f);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        routine = null;
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float x = t - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }
}
