using ProjectMS.CharacterSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 좌/우 상단 코너에 표시되는 플레이어 정보 HUD(HP/이름/스코어/캐릭터 이미지).
/// 다른 인게임 UI(WorldHealthBarUI, MatchResultUI)와 같은 이유로 이벤트 대신
/// 매 프레임 폴링 방식을 쓴다: 원격 동기화 타이밍 문제를 피하기 위함.
///
/// [씬 설정]
///   - 게임 씬의 Screen Space Canvas 하위 좌/우 코너에 각각 하나씩 배치하고
///     isLeftSide 를 좌/우에 맞게 설정한다.
///   - hpFillImage: Filled Image (Horizontal). 캐릭터가 아직 스폰되지 않았으면 0으로 표시된다.
///   - nameText: 임시로 "P1"(왼쪽)/"P2"(오른쪽)만 표시한다. 실제 닉네임 연동은 추후.
///   - scoreSlots: MatchManager.WinsRequired(=2)만큼(3판 2선승, 2승 찍으면 매치 종료라 그 이상은
///     필요 없음 — 예전 "5판 3선승" 시절엔 3칸이 맞았으나 지금은 아님). 승수만큼 앞에서부터
///     scoreFilledColor, 나머지는 scoreEmptyColor.
///   - characterImage: 캐릭터 초상화 표시용 빈 슬롯. 캐릭터 선택/포트레이트 시스템이 아직 없어
///     지금은 자동으로 채우지 않는다(필요 시 인스펙터에서 수동으로 스프라이트를 지정).
/// </summary>
public class PlayerCornerHUD : MonoBehaviour
{
    [SerializeField] private bool isLeftSide = true;

    [Header("HP")]
    [SerializeField] private Image hpFillImage;
    [Tooltip("HP가 깎이면 잠깐 머물렀다가 천천히 따라 줄어드는 흰 잔상 바(hpFillImage 뒤, Filled Horizontal). 선택 사항.")]
    [SerializeField] private Image hpTrailImage;
    [Min(0f)] [SerializeField] private float trailDelay = 0.35f;
    [Min(0.1f)] [SerializeField] private float trailSpeed = 0.9f;

    [Header("캐릭터 이름")]
    [Tooltip("지금 이 코너 캐릭터의 이름(CharacterDefinition.DisplayName)을 보여줄 글자.")]
    [SerializeField] private TMP_Text characterNameText;

    [Header("이름 (임시)")]
    [SerializeField] private TMP_Text nameText;

    [Header("스코어 (3판 2선승 = WinsRequired 2, 2칸)")]
    [SerializeField] private Image[] scoreSlots;
    [SerializeField] private Color scoreFilledColor = Color.white;
    [SerializeField] private Color scoreEmptyColor = new Color(1f, 1f, 1f, 0.25f);

    [Header("캐릭터 이미지 (빈 슬롯, 추후 연동)")]
    [SerializeField] private Image characterImage;

    private CharacterBase character;
    private float trailFill = 1f;
    private float lastFill = 1f;
    private float lastHitTime = -10f;

    private void Awake()
    {
        if (nameText != null)
            nameText.text = isLeftSide ? "P1" : "P2";
    }

    private void LateUpdate()
    {
        UpdateCharacterRef();
        UpdateHp();
        UpdateScore();
    }

    private void UpdateCharacterRef()
    {
        character = null;

        MatchManager match = MatchManager.Instance;
        if (match == null)
            return;

        foreach (CharacterBase candidate in CharacterBase.All)
        {
            if (candidate.Object == null)
                continue;

            if (match.IsPlayer1(candidate.MatchPlayer) == isLeftSide)
            {
                character = candidate;
                return;
            }
        }
    }

    private void UpdateHp()
    {
        if (characterNameText != null)
        {
            string name = character != null && character.Definition != null ? character.Definition.DisplayName : string.Empty;
            if (characterNameText.text != name)
                characterNameText.text = name;
        }

        if (hpFillImage == null)
            return;

        if (character == null)
        {
            hpFillImage.fillAmount = 0f;
            if (hpTrailImage != null)
                hpTrailImage.fillAmount = 0f;
            trailFill = lastFill = 0f;
            return;
        }

        float max = character.MaxHealth;
        float fill = max > 0f ? character.CurrentHealth / max : 0f;
        hpFillImage.fillAmount = fill;

        if (hpTrailImage == null)
            return;

        // 깎이면 잔상은 잠깐 그대로 있다가 천천히 따라 내려오고, 회복은 바로 따라간다.
        if (fill < lastFill - 0.0001f)
            lastHitTime = Time.unscaledTime;
        lastFill = fill;
        if (fill > trailFill)
            trailFill = fill;
        else if (Time.unscaledTime - lastHitTime > trailDelay)
            trailFill = Mathf.MoveTowards(trailFill, fill, trailSpeed * Time.unscaledDeltaTime);
        hpTrailImage.fillAmount = trailFill;
    }

    private void UpdateScore()
    {
        if (scoreSlots == null || scoreSlots.Length == 0)
            return;

        MatchManager match = MatchManager.Instance;
        int wins = match == null ? 0 : (isLeftSide ? match.Player1Wins : match.Player2Wins);

        for (int i = 0; i < scoreSlots.Length; i++)
        {
            if (scoreSlots[i] != null)
                scoreSlots[i].color = i < wins ? scoreFilledColor : scoreEmptyColor;
        }
    }
}
