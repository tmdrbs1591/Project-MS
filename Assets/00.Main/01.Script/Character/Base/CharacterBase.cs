using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// 모든 플레이어 캐릭터가 상속받는 부모 클래스다.
///
/// [역할]
///   - PhotonView 기준으로 내 캐릭터만 입력과 물리를 처리한다.
///   - 입력, 이동, 쿨타임 처리는 Module 폴더의 일반 C# 클래스에게 맡긴다.
///   - 이동 모듈에서 발생한 점프 신호와 현재 이동 상태를 자식 캐릭터에게 넘긴다.
///   - 후배들은 이 클래스를 직접 수정하지 않고, Playable 폴더의 캐릭터 클래스에서
///     BasicAttack, SkillQ, SkillE, Dash, Ultimate, Passive 함수를 구현한다.
///
/// [필요한 것]
///   - 플레이어 프리팹에 Rigidbody2D + Collider2D + PhotonView
///   - PhotonView의 Observed Components에 이 클래스를 상속받은 캐릭터 스크립트 등록
///   - Resources 폴더 안의 플레이어 프리팹을 PhotonNetwork.Instantiate로 생성
///
/// 슬라임 통통점프 같은 캐릭터 전용 특성은 이 클래스에 넣지 않고 별도 전용 스크립트로 분리한다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(PhotonView))]
public abstract class CharacterBase : MonoBehaviour, IPunObservable
{
    [Header("Key Setting")]
    [SerializeField] private CharacterKeySetting keySetting = new CharacterKeySetting();

    [Header("Movement")]
    [SerializeField] private CharacterMovementData movementData = new CharacterMovementData();

    [Header("Cooldown")]
    [SerializeField] private CharacterCooldownData cooldownData = new CharacterCooldownData();

    [Header("Network")]
    [SerializeField] private float interpolationDelay = 0.1f;

    protected Rigidbody2D Rb { get; private set; }
    protected Collider2D Col { get; private set; }
    protected PhotonView Pv { get; private set; }
    protected CharacterMovementHandler Movement { get; private set; }

    protected bool IsMine => Pv == null || Pv.IsMine;

    private CharacterInputHandler input;
    private CharacterCooldownHandler cooldown;
    private readonly List<CharacterNetworkState> networkBuffer = new List<CharacterNetworkState>(32);
    private Vector2 syncedVelocity;

    protected virtual void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        Col = GetComponent<Collider2D>();
        Pv = GetComponent<PhotonView>();

        input = new CharacterInputHandler(keySetting);
        cooldown = new CharacterCooldownHandler(cooldownData);
        Movement = new CharacterMovementHandler(Rb, Col, transform, movementData);
        Movement.Jumped += OnCharacterJump;

        if (!IsMine)
        {
            Rb.bodyType = RigidbodyType2D.Kinematic;
            Rb.simulated = false;
            Col.enabled = false;
        }
    }

    protected virtual void OnDestroy()
    {
        if (Movement == null)
            return;

        Movement.Jumped -= OnCharacterJump;
    }

    protected virtual void Update()
    {
        if (!IsMine)
        {
            InterpolateRemoteCharacter();
            return;
        }

        CharacterInputState inputState = input.Read();
        HandleActionInput(inputState);
        cooldown.Tick(Time.deltaTime);
        Passive();
    }

    protected virtual void FixedUpdate()
    {
        if (!IsMine)
            return;

        Movement.TickFixed(input.CurrentState);
        OnCharacterVisualTick(Time.fixedDeltaTime, Movement.IsGrounded, Movement.MoveInput, Movement.CurrentVelocity);
    }

    public void SetKeySetting(CharacterKeySetting newKeySetting)
    {
        keySetting = newKeySetting;
        input.SetKeySetting(newKeySetting);
    }

    private void HandleActionInput(CharacterInputState inputState)
    {
        TryUseAction(CharacterActionType.BasicAttack, inputState.BasicAttackPressed, BasicAttack);
        TryUseAction(CharacterActionType.SkillQ, inputState.SkillQPressed, SkillQ);
        TryUseAction(CharacterActionType.SkillE, inputState.SkillEPressed, SkillE);
        TryUseAction(CharacterActionType.Dash, inputState.DashPressed, Dash);
        TryUseAction(CharacterActionType.Ultimate, inputState.UltimatePressed, Ultimate);
    }

    private void TryUseAction(CharacterActionType actionType, bool pressed, System.Action action)
    {
        if (!pressed || !cooldown.CanUse(actionType))
            return;

        cooldown.Start(actionType);
        action?.Invoke();
    }

    private void InterpolateRemoteCharacter()
    {
        if (networkBuffer.Count == 0)
            return;

        double renderTime = PhotonNetwork.Time - interpolationDelay;
        CharacterNetworkState newest = networkBuffer[networkBuffer.Count - 1];

        if (renderTime >= newest.Time)
        {
            ApplyNetworkState(newest, newest.Position);
            return;
        }

        if (renderTime <= networkBuffer[0].Time)
        {
            ApplyNetworkState(networkBuffer[0], networkBuffer[0].Position);
            return;
        }

        for (int i = 0; i < networkBuffer.Count - 1; i++)
        {
            CharacterNetworkState from = networkBuffer[i];
            CharacterNetworkState to = networkBuffer[i + 1];

            if (renderTime < from.Time || renderTime > to.Time)
                continue;

            double span = to.Time - from.Time;
            float t = span > 0.0 ? (float)((renderTime - from.Time) / span) : 0f;
            Vector3 position = Vector3.Lerp(from.Position, to.Position, t);
            syncedVelocity = Vector2.Lerp(from.Velocity, to.Velocity, t);

            CharacterNetworkState near = t < 0.5f ? from : to;
            ApplyNetworkState(near, position);

            if (i > 0)
                networkBuffer.RemoveRange(0, i);

            return;
        }
    }

    private void ApplyNetworkState(CharacterNetworkState state, Vector3 position)
    {
        transform.position = position;
        Movement.ApplyNetworkVisualState(state.FacingDirection, state.MoveInput, state.IsGrounded, syncedVelocity);
        OnCharacterVisualTick(Time.deltaTime, Movement.IsGrounded, Movement.MoveInput, Movement.CurrentVelocity);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(Rb.linearVelocity);
            stream.SendNext((short)Movement.FacingDirection);
            stream.SendNext(Movement.MoveInput);
            stream.SendNext(Movement.IsGrounded);
        }
        else
        {
            CharacterNetworkState state = new CharacterNetworkState
            {
                Time = info.SentServerTime,
                Position = (Vector3)stream.ReceiveNext(),
                Velocity = (Vector2)stream.ReceiveNext(),
                FacingDirection = (short)stream.ReceiveNext(),
                MoveInput = (float)stream.ReceiveNext(),
                IsGrounded = (bool)stream.ReceiveNext()
            };

            if (networkBuffer.Count > 0 && state.Time <= networkBuffer[networkBuffer.Count - 1].Time)
                return;

            networkBuffer.Add(state);
            if (networkBuffer.Count > 30)
                networkBuffer.RemoveAt(0);
        }
    }

    protected virtual void OnCharacterJump()
    {
    }

    protected virtual void OnCharacterVisualTick(float deltaTime, bool isGrounded, float moveInput, Vector2 velocity)
    {
    }

    protected abstract void BasicAttack();
    protected abstract void SkillQ();
    protected abstract void SkillE();
    protected abstract void Dash();
    protected abstract void Ultimate();
    protected abstract void Passive();
}
