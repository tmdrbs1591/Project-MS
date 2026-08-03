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
///   - scoreSlots: 3판 2선승이라 3칸. 승수만큼 앞에서부터 scoreFilledColor, 나머지는 scoreEmptyColor.
///   - characterImage: 캐릭터 초상화 표시용 빈 슬롯. 캐릭터 선택/포트레이트 시스템이 아직 없어
///     지금은 자동으로 채우지 않는다(필요 시 인스펙터에서 수동으로 스프라이트를 지정).
/// </summary>
public class PlayerCornerHUD : MonoBehaviour
{
    [SerializeField] private bool isLeftSide = true;

    [Header("HP")]
    [SerializeField] private Image hpFillImage;

    [Header("이름 (임시)")]
    [SerializeField] private TMP_Text nameText;

    [Header("스코어 (3판 2선승, 3칸)")]
    [SerializeField] private Image[] scoreSlots;
    [SerializeField] private Color scoreFilledColor = Color.white;
    [SerializeField] private Color scoreEmptyColor = new Color(1f, 1f, 1f, 0.25f);

    [Header("캐릭터 이미지 (빈 슬롯, 추후 연동)")]
    [SerializeField] private Image characterImage;

    private CharacterBase character;

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

            if (match.IsPlayer1(candidate.Object.InputAuthority) == isLeftSide)
            {
                character = candidate;
                return;
            }
        }
    }

    private void UpdateHp()
    {
        if (hpFillImage == null)
            return;

        if (character == null)
        {
            hpFillImage.fillAmount = 0f;
            return;
        }

        float max = character.MaxHealth;
        hpFillImage.fillAmount = max > 0f ? character.CurrentHealth / max : 0f;
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
