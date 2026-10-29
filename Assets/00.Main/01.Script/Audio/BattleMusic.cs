using ProjectMS.CharacterSystem;
using UnityEngine;

/// <summary>
/// 게임 씬 배경음. 상황에 맞춰 곡을 부드럽게 바꿔 튼다.
///   - 연습장: 평소엔 전투곡, 매칭을 돌리는 동안엔 매칭곡.
///   - 대전 시작 연출(VS) 동안: 짧은 VS 곡(vsMusic). 비어 있으면 음악을 끄고 연출 소리만.
///   - "FIGHT!"가 박혀 조작이 풀리는 순간부터: 전투곡.
///   - 씬을 떠나면(로비로 돌아가면) 끈다.
///
/// [씬 설정]
///   - 게임 씬 아무 오브젝트에 붙이고 battleMusic / matchingMusic을 넣는다.
/// </summary>
public class BattleMusic : MonoBehaviour
{
    [SerializeField] private AudioClip battleMusic;
    [SerializeField] private AudioClip matchingMusic;
    [Tooltip("대전 시작 연출(VS) 동안 깔리는 짧은 곡. 비우면 연출 동안 음악을 끈다.")]
    [SerializeField] private AudioClip vsMusic;
    [Min(0f)] [SerializeField] private float crossfade = 0.8f;

    private enum Track { None, Battle, Matching, Silent }

    private Track current = Track.None;

    private void Update()
    {
        if (SoundManager.Instance == null)
            return;
        if (SceneTransition.Instance != null && SceneTransition.Instance.IsBusy)
            return;

        Track wanted = Decide();
        if (wanted == Track.None || wanted == current)
            return;

        Track previous = current;
        current = wanted;
        AudioClip clip = wanted == Track.Battle ? battleMusic : wanted == Track.Matching ? matchingMusic : vsMusic;
        // 연출이 시작되고 끝날 땐 빠르게 넘기고(연출 템포를 안 끊게), 곡끼리는 부드럽게 넘긴다.
        bool quick = wanted == Track.Silent || previous == Track.Silent;
        SoundManager.Instance.CrossfadeMusic(clip, wanted == Track.Silent ? 0.25f : (quick ? 0.3f : crossfade));
    }

    private static Track Decide()
    {
        if (CharacterBase.IntroLocked)
            return Track.Silent;

        if (PracticeSession.IsActive)
        {
            NetworkLauncher launcher = NetworkLauncher.Instance;
            return launcher != null && launcher.IsMatchingInPractice ? Track.Matching : Track.Battle;
        }

        MatchManager match = MatchManager.Instance;
        if (match != null && match.Phase == MatchPhase.Fighting)
            return Track.Battle;
        return Track.None;
    }

    private void OnDestroy()
    {
        if (current != Track.None)
            SoundManager.Instance?.StopMusic();
    }
}
