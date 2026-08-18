#if UNITY_EDITOR || DEVELOPMENT_BUILD
using ProjectMS.CharacterSystem;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 증강 테스트용 치트. 에디터/개발 빌드에서만 존재한다(전처리기로 막아둬서 릴리즈 빌드엔
/// 이 스크립트 자체가 포함되지 않는다).
///
/// [씬 설정]
///   필요 없음 — 게임이 시작되면 RuntimeInitializeOnLoadMethod로 자동 생성된다.
///
/// [키 배정]
///   숫자 1~9, 0 → AUG_001~010 (표 순서 그대로)
///   -(마이너스) → AUG_011 유리 대포
///   =(이퀄)     → AUG_012 버서커
///   `(백틱)     → AUG_013 반사
///   Backspace   → 보유 증강 전부 초기화
///
/// 게임플레이가 쓰는 키(WASD/스페이스/Q/E/Shift/R)와 안 겹치는 숫자·기호 줄만 써서 평소
/// 조작과 충돌하지 않는다. 로컬 캐릭터(CharacterBase.LocalPlayer)에게만 적용된다 — 즉,
/// 내가 조작하는 캐릭터에만 지급되고 상대에게는 아무 영향 없다.
/// </summary>
public class AugmentCheatController : MonoBehaviour
{
    private static readonly Key[] NumberKeys =
    {
        Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
        Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9, Key.Digit0
    };

    // NumberKeys와 같은 순서 — AUG_001 ~ AUG_010.
    private static readonly AugmentType[] NumberKeyAugments =
    {
        AugmentType.LargeAmmoPouch,
        AugmentType.BulletproofVest,
        AugmentType.SwiftBoots,
        AugmentType.OverchargedMagazine,
        AugmentType.RapidReload,
        AugmentType.ForkedMagic,
        AugmentType.BouncingMagic,
        AugmentType.DashBooster,
        AugmentType.TurboCharge,
        AugmentType.ExplosiveMagic
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindObjectOfType<AugmentCheatController>() != null)
            return;

        GameObject host = new GameObject("AugmentCheatController (Debug)");
        host.AddComponent<AugmentCheatController>();
        DontDestroyOnLoad(host);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        CharacterBase local = CharacterBase.LocalPlayer;
        if (local == null)
            return;

        for (int i = 0; i < NumberKeys.Length; i++)
        {
            if (keyboard[NumberKeys[i]].wasPressedThisFrame)
            {
                Grant(local, NumberKeyAugments[i]);
                return;
            }
        }

        if (keyboard[Key.Minus].wasPressedThisFrame)
            Grant(local, AugmentType.GlassCannon);
        else if (keyboard[Key.Equals].wasPressedThisFrame)
            Grant(local, AugmentType.Berserker);
        else if (keyboard[Key.Backquote].wasPressedThisFrame)
            Grant(local, AugmentType.Reflect);
        else if (keyboard[Key.Backspace].wasPressedThisFrame)
        {
            local.DebugClearAugments();
            Debug.Log("[AugmentCheat] 보유 증강 전부 초기화");
        }
    }

    private static void Grant(CharacterBase character, AugmentType type)
    {
        character.DebugGrantAugment(type);
        Debug.Log($"[AugmentCheat] {type} 지급 시도 → 현재 {character.GetAugmentStack(type)}스택");
    }
}
#endif
