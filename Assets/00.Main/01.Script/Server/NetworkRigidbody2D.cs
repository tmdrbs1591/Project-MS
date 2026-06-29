using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

/// <summary>
/// 네트워크로 동기화되는 2D 물리 객체(박스 등).
///
/// 동작 방식 — "소유권 이전(Ownership Transfer)"
///   - 박스의 소유자(IsMine)인 클라이언트만 실제 물리(Dynamic)로 시뮬레이션하고,
///     위치/회전/속도를 다른 모두에게 보낸다.
///   - 소유자가 아닌 클라이언트는 박스를 Kinematic 으로 두고, 받은 스냅샷을
///     '과거 시점'으로 보간해 매끄럽게 따라 그린다(플레이어와 같은 방식 → 안 뚫고 안 끊김).
///   - 플레이어가 박스를 밀면(충돌하면) 그 플레이어가 소유권을 가져와서,
///     자기 컴퓨터에서 즉시 밀고 결과를 동기화한다. → 미는 사람은 지연 0.
///
/// [필요한 설정]
///   - 박스에 Rigidbody2D + Collider2D + PhotonView + 이 스크립트
///   - PhotonView 의 Observed Components 에 이 스크립트를 등록
///   - PhotonView 의 Ownership Transfer 를 "Takeover" 로 설정 (충돌 시 즉시 소유권 이전)
///   - 박스는 씬에 그냥 배치해도 됨(씬 PhotonView 는 처음엔 마스터 소유).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PhotonView))]
public class NetworkRigidbody2D : MonoBehaviourPun, IPunObservable, IPunOwnershipCallbacks
{
    [Header("동기화")]
    [Tooltip("소유자가 아닐 때 '과거 시점'으로 보간하는 시간(초). 플레이어와 같은 0.1 권장")]
    [SerializeField] private float interpolationDelay = 0.1f;

    [Header("소유권")]
    [Tooltip("활성화 시 충돌한 플레이어에게 소유권 이전. 양쪽 동시 충돌 시 핑퐁 문제 발생으로 기본 비활성화.")]
    [SerializeField] private bool takeOwnershipOnTouch = false;

    private Rigidbody2D rb;

    // 받은 상태를 시간순으로 모아 두 스냅샷 사이를 보간한다.
    private struct State
    {
        public double time;
        public Vector2 pos;
        public float rot;
    }
    private readonly System.Collections.Generic.List<State> buffer =
        new System.Collections.Generic.List<State>(32);

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // 끊김 방지의 핵심: 물리 위치를 화면 주사율에 맞춰 부드럽게 보간시킨다.
        // (이게 꺼져 있으면 비소유자 박스가 FixedUpdate 50Hz 로만 갱신돼 뚝뚝 끊겨 보인다)
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        ApplyAuthority();
    }

    // 소유권이 방금 나에게서 떠난 직후, 곧바로 도로 뺏어오지 않도록 하는 쿨다운.
    private float reclaimBlockedUntil;

    // 보간으로 추정한 박스 속도 (소유권 넘겨받을 때 momentum 을 이어받는 용도)
    private Vector2 estimatedVelocity;

    // 소유권 콜백을 받으려면 콜백 타겟으로 등록해야 한다.
    private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    /// <summary>소유자면 직접 물리(Dynamic), 아니면 네트워크 추종(Kinematic).</summary>
    private void ApplyAuthority()
    {
        bool mine = photonView.IsMine;
        rb.bodyType = mine ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;

        if (mine)
        {
            // 소유권을 넘겨받는 순간 박스가 '뚝 멈췄다 다시 출발'하지 않도록,
            // 직전까지 보간으로 추정한 속도를 그대로 이어받아 momentum 을 유지한다.
            if (buffer.Count > 0)
                rb.linearVelocity = estimatedVelocity;

            buffer.Clear(); // 내가 주인이 됐으니 보간 버퍼는 비운다
        }
    }

    // ---- IPunOwnershipCallbacks ----

    // 다른 클라가 이 박스의 소유권을 요청했을 때 (모든 클라에서 호출됨)
    public void OnOwnershipRequest(PhotonView targetView, Player requestingPlayer)
    {
        if (targetView != photonView) return;

        // 내가 현재 주인이면 요청한 플레이어에게 넘겨준다.
        // (Ownership Transfer = "Request" 모드에서도 동작하게. "Takeover" 면 PUN 이 자동 처리)
        if (targetView.Owner == PhotonNetwork.LocalPlayer)
            targetView.TransferOwnership(requestingPlayer);
    }

    // 소유권이 실제로 바뀌면(누가 가져가면) 권한 상태를 다시 적용
    public void OnOwnershipTransfered(PhotonView targetView, Player previousOwner)
    {
        if (targetView != photonView) return;

        // 방금 내 소유권을 누가 가져갔다면, 잠깐은 도로 뺏지 않는다 (핑퐁 방지)
        if (previousOwner == PhotonNetwork.LocalPlayer)
            reclaimBlockedUntil = Time.time + 0.5f;

        ApplyAuthority();
    }

    // 소유권 요청이 실패했을 때 (거의 동시에 다른 사람이 가져간 경우 등)
    public void OnOwnershipTransferFailed(PhotonView targetView, Player senderOfFailedRequest)
    {
    }

    private void FixedUpdate()
    {
        if (photonView.IsMine) return; // 주인은 물리엔진이 알아서 시뮬레이션
        Interpolate();
    }

    /// <summary>비소유자: 과거 시점을 두 스냅샷 사이로 보간해 Kinematic 으로 이동.</summary>
    private void Interpolate()
    {
        if (buffer.Count == 0) return;

        double renderTime = PhotonNetwork.Time - interpolationDelay;

        State newest = buffer[buffer.Count - 1];
        if (renderTime >= newest.time) { Apply(newest); return; }
        if (renderTime <= buffer[0].time) { Apply(buffer[0]); return; }

        for (int i = 0; i < buffer.Count - 1; i++)
        {
            State a = buffer[i];
            State b = buffer[i + 1];
            if (renderTime < a.time || renderTime > b.time) continue;

            double span = b.time - a.time;
            float t = span > 0.0 ? (float)((renderTime - a.time) / span) : 0f;

            rb.MovePosition(Vector2.Lerp(a.pos, b.pos, t));
            rb.MoveRotation(Mathf.LerpAngle(a.rot, b.rot, t));

            if (i > 0) buffer.RemoveRange(0, i);
            return;
        }
    }

    private void Apply(State s)
    {
        rb.MovePosition(s.pos);
        rb.MoveRotation(s.rot);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 주인 → 위치/회전 전송 (속도는 안 보내도 보간으로 충분히 매끄럽다)
            stream.SendNext(rb.position);
            stream.SendNext(rb.rotation);
        }
        else
        {
            State s;
            s.time = info.SentServerTime;
            s.pos = (Vector2)stream.ReceiveNext();
            s.rot = (float)stream.ReceiveNext();

            // 늦게 도착한 오래된 패킷은 버린다
            if (buffer.Count > 0 && s.time <= buffer[buffer.Count - 1].time)
                return;

            // 마지막 두 스냅샷으로 속도를 추정해 둔다 (소유권 넘겨받을 때 momentum 유지용)
            if (buffer.Count > 0)
            {
                State prev = buffer[buffer.Count - 1];
                double dt = s.time - prev.time;
                if (dt > 0.0)
                    estimatedVelocity = (s.pos - prev.pos) / (float)dt;
            }

            buffer.Add(s);
            if (buffer.Count > 30)
                buffer.RemoveAt(0);
        }
    }

    /// <summary>
    /// 구조물에 힘을 가한다. MasterClient면 직접 적용, 아니면 MasterClient에 RPC 요청.
    /// PushableStructure/BreakableStructure에서 비방장 플레이어의 밀기에 사용한다.
    /// </summary>
    public void AddNetworkForce(Vector2 force)
    {
        if (PhotonNetwork.IsMasterClient)
            rb.AddForce(force, ForceMode2D.Impulse);
        else
            photonView.RPC(nameof(AddForceRPC), RpcTarget.MasterClient, force);
    }

    [PunRPC]
    private void AddForceRPC(Vector2 force)
    {
        rb.AddForce(force, ForceMode2D.Impulse);
    }

    // 플레이어가 박스를 "능동적으로 밀 때"만 그 플레이어가 소유권을 가져온다.
    private void OnCollisionEnter2D(Collision2D collision) => TryClaim(collision);
    private void OnCollisionStay2D(Collision2D collision) => TryClaim(collision);

    private void TryClaim(Collision2D collision)
    {
        if (!takeOwnershipOnTouch) return;
        if (photonView.IsMine) return;            // 이미 내 거면 그대로 시뮬
        if (Time.time < reclaimBlockedUntil) return; // 방금 뺏긴 직후엔 자제 (핑퐁 방지)

        // 나를 친 상대가 '내가 조종하는 플레이어'인지 확인
        PhotonView otherView = collision.collider.GetComponentInParent<PhotonView>();
        if (otherView == null || !otherView.IsMine) return;

        // 핵심: 상대 플레이어가 '실제로 움직이고 있을 때'만 소유권을 가져온다.
        // (가만히 선 플레이어에게 박스가 굴러와 부딪힌 경우엔 소유권을 안 뺏어 핑퐁을 막음)
        Rigidbody2D otherRb = collision.collider.attachedRigidbody;
        if (otherRb != null && otherRb.linearVelocity.sqrMagnitude < 0.25f) return;

        photonView.RequestOwnership();
    }
}
