using ProjectMS.CharacterSystem;
using UnityEngine;

/// <summary>
/// 연습 봇의 입력. AI 대신 붙어서 PracticeSession.DummyMode에 따라 서있기 / 좌우 이동 / 점프 반복만 한다.
/// 공격은 하지 않는다. 캐릭터 종류와 상관없이 붙일 수 있다(PlayerSpawner.SpawnBot에서 연습장일 때 붙인다).
/// </summary>
public class PracticeDummyInput : MonoBehaviour, ICharacterInputSource
{
    [SerializeField] private float walkRange = 3f;
    [SerializeField] private float jumpInterval = 1.1f;
    [Tooltip("팝업 궁 에러창에 갇혔을 때 연타 간격(초).")]
    [SerializeField] private float breakPressInterval = 0.3f;
    private float nextBreakPressAt;

    private CharacterBase self;
    private Vector2 home;
    private float direction = 1f;
    private float clock;
    private float nextJumpAt;
    private float lastJumpAt = -10f;

    public static PracticeDummyInput AttachTo(CharacterBase character)
    {
        PracticeDummyInput input = character.gameObject.AddComponent<PracticeDummyInput>();
        input.self = character;
        input.home = character.transform.position;
        character.SetExternalInputSource(input);
        return input;
    }

    private void OnDestroy()
    {
        if (self != null)
            self.SetExternalInputSource(null);
    }

    public CharacterInputSnapshot BuildInput(CharacterBase character, float deltaTime)
    {
        clock += deltaTime;
        CharacterInputSnapshot input = default;

        // 내 캐릭터 쪽을 바라보게 조준만 맞춰둔다.
        CharacterBase player = CharacterBase.LocalPlayer;
        Vector2 position = character.transform.position;
        Vector2 aimTarget = player != null ? (Vector2)player.transform.position : position + Vector2.right;
        input.AimWorldPosition = aimTarget;

        if (character.IsDead)
            return input;

        // 팝업 궁(에러창)은 연타로만 풀린다 — 연습 봇도 천천히 눌러서 빠져나온다(약 3초).
        if (ProjectMS.CharacterSystem.Examples.PopupErrorPopupDeployable.IsTrapped(character))
        {
            if (clock >= nextBreakPressAt)
            {
                input.JumpPressed = true;
                nextBreakPressAt = clock + breakPressInterval;
            }
            return input;
        }

        switch (PracticeSession.DummyMode)
        {
            case PracticeDummyMode.Walk:
                if (position.x > home.x + walkRange)
                    direction = -1f;
                else if (position.x < home.x - walkRange)
                    direction = 1f;
                input.MoveDirection = direction;
                break;

            case PracticeDummyMode.Jump:
                if (clock >= nextJumpAt)
                {
                    input.JumpPressed = true;
                    lastJumpAt = clock;
                    nextJumpAt = clock + jumpInterval;
                }
                input.JumpHeld = clock < lastJumpAt + 0.25f;
                break;
        }

        return input;
    }
}
