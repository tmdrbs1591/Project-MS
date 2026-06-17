using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

/// <summary>
/// 플레이어가 포탈 앞에 닿으면 자기 안의 Key UI 캔버스([F] 매칭)를 켜고,
/// 그 상태에서 F 를 누르면 포탈과 상호작용(매칭 시작)한다.
///
/// [필요한 것]
///   - 플레이어에 Rigidbody2D + Collider2D (이미 있음 → SlimeCharacterController)
///   - 플레이어 자식에 "Key UI Canvas" (예: [F] 매칭 텍스트). 평소엔 꺼진 채로 두고,
///     이 스크립트의 Key UI Canvas 칸에 그 오브젝트를 연결한다.
///   - 포탈 오브젝트에는 Portal 스크립트 + Is Trigger 콜라이더
///
/// 트리거 감지는 플레이어 쪽에서 받는다(플레이어에 Rigidbody2D 가 있으므로).
/// </summary>
public class PortalInteractor : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("포탈에 닿았을 때 켤 Key UI 캔버스 (플레이어 자식의 [F] 매칭 UI). 평소엔 꺼둠")]
    [SerializeField] private GameObject keyUICanvas;

    private Portal currentPortal;     // 현재 범위 안에 있는 포탈
    private PhotonView pv;            // 멀티에서 내 캐릭터만 조작하도록

    private bool IsMine => pv == null || pv.IsMine;

    private void Awake()
    {
        pv = GetComponent<PhotonView>();

        // 시작할 땐 Key UI 를 꺼둔다
        if (keyUICanvas != null)
            keyUICanvas.SetActive(false);
    }

    private void Update()
    {
        if (!IsMine) return;
        if (currentPortal == null) return; // 범위 밖이면 무시

        var kb = Keyboard.current;
        if (kb != null && kb.fKey.wasPressedThisFrame)
        {
            currentPortal.Interact();

            // 매칭을 시작했으니 Key UI 는 닫아둔다 (상태 표시는 MatchmakingManager 가 담당)
            if (keyUICanvas != null)
                keyUICanvas.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsMine) return;

        var portal = other.GetComponent<Portal>();
        if (portal == null) return;

        currentPortal = portal;
        if (keyUICanvas != null)
            keyUICanvas.SetActive(true); // [F] 매칭 UI 켜기
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsMine) return;

        var portal = other.GetComponent<Portal>();
        if (portal == null || portal != currentPortal) return;

        currentPortal = null;
        if (keyUICanvas != null)
            keyUICanvas.SetActive(false); // 범위 벗어나면 UI 끄기
    }
}
