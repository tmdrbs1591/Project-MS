using System.Collections;
using ProjectMS.CharacterSystem;
using TMPro;
using UnityEngine;

/// <summary>
/// 전투 진입 시 표시되는 "VS" 연출: 좌(Player1)/우(Player2) 캐릭터 이름을 채우고,
/// 연출이 나오는 동안 양쪽 캐릭터 조작을 전부 막는다. 일정 시간 뒤 플래시 오브젝트를 켜면서
/// 그동안 재생되던 파티클을 끄고, 플래시가 터진 뒤 추가로 잠깐 더 기다렸다가 조작을 풀어준다.
///
/// [흐름]
///   OnEnable → 즉시 전체 조작 잠금 + 이름 세팅 시작
///   → flashDelay(기본 5초) 대기 → flashObject 켜짐 + loopingParticle 꺼짐
///   → controlUnlockDelay(기본 2초) 대기 → 전체 조작 잠금 해제
///
/// [트리거]
///   - 이 스크립트가 붙은 오브젝트(=VS 연출 루트)가 SetActive(true) 되는 순간(OnEnable)에
///     시작된다. BattleStartVfxController가 켜는 Vfx_UI_BattleStart_Vs 같은 오브젝트에
///     붙이면 된다.
///   - 각 클라가 자기 화면에서 독립적으로 재생한다(동기화 불필요 — BattleStartVfxController/
///     RoundFinishController와 동일 패턴). 조작 잠금(CharacterBase.SetGameplayLocked)은
///     StateAuthority가 있는 캐릭터에만 실제로 적용되므로, 각 클라는 결과적으로 자기 캐릭터만
///     잠그게 된다(다른 클라도 자기 화면에서 동일하게 재생되어 상대도 똑같이 잠김).
///
/// [씬 설정]
///   - player1Text / player2Text: 좌(Player1) / 우(Player2) 캐릭터 이름을 표시할 텍스트.
///     캐릭터 이름은 CharacterDefinition.displayName(스파크/체이서/거너 등)에서 그대로 가져온다.
///   - flashObject: 대기 시간이 끝나면 켜지는 플래시 연출 오브젝트(처음엔 꺼진 상태로 둬도 된다).
///   - loopingParticle: 대기하는 동안 재생 중인 파티클. 플래시가 켜지는 순간 같이 꺼진다.
/// </summary>
public class VsIntroUI : MonoBehaviour
{
    [Header("VS 텍스트")]
    [SerializeField] private TMP_Text player1Text;
    [SerializeField] private TMP_Text player2Text;

    [Header("플래시 연출")]
    [Tooltip("연출이 시작되고 플래시가 터지기까지 대기 시간(초).")]
    [SerializeField] private float flashDelay = 5f;
    [SerializeField] private GameObject flashObject;
    [Tooltip("대기하는 동안 재생 중인 파티클. 플래시가 켜지는 순간 같이 꺼진다.")]
    [SerializeField] private ParticleSystem loopingParticle;

    [Header("조작 잠금")]
    [Tooltip("플래시가 터진 뒤 조작이 풀리기까지 추가 대기 시간(초).")]
    [SerializeField] private float controlUnlockDelay = 2f;

    private Coroutine introRoutine;

    private void OnEnable()
    {
        if (flashObject != null)
            flashObject.SetActive(false);

        SetAllCharactersLocked(true);

        if (introRoutine != null)
            StopCoroutine(introRoutine);
        introRoutine = StartCoroutine(IntroSequence());
    }

    private void OnDisable()
    {
        if (introRoutine != null)
        {
            StopCoroutine(introRoutine);
            introRoutine = null;
        }

        // 연출 도중에 오브젝트가 꺼져도 조작 불능 상태로 남지 않도록 방어적으로 풀어준다.
        SetAllCharactersLocked(false);
    }

    private void Update()
    {
        // 캐릭터 스폰/동기화 타이밍이 화면마다 다를 수 있어 매 프레임 폴링한다
        // (PlayerCornerHUD와 동일한 이유).
        UpdateNames();
    }

    private void UpdateNames()
    {
        MatchManager match = MatchManager.Instance;

        foreach (CharacterBase character in CharacterBase.All)
        {
            if (character == null || character.Object == null)
                continue;

            bool isP1 = match != null && match.IsPlayer1(character.Object.InputAuthority);
            string displayName = character.Definition != null ? character.Definition.DisplayName : string.Empty;

            if (isP1)
            {
                if (player1Text != null)
                    player1Text.text = displayName;
            }
            else
            {
                if (player2Text != null)
                    player2Text.text = displayName;
            }
        }
    }

    private static void SetAllCharactersLocked(bool locked)
    {
        foreach (CharacterBase character in CharacterBase.All)
        {
            character?.SetGameplayLocked(locked);
        }
    }

    private IEnumerator IntroSequence()
    {
        yield return new WaitForSeconds(flashDelay);

        if (flashObject != null)
            flashObject.SetActive(true);

        if (loopingParticle != null)
            loopingParticle.gameObject.SetActive(false);

        yield return new WaitForSeconds(controlUnlockDelay);

        SetAllCharactersLocked(false);
        introRoutine = null;
    }
}
