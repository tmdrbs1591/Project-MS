using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;

/// <summary>
/// 플레이어가 IInteractable 오브젝트(포탈, 로비 건물 등) 앞에 닿으면 자기 안의
/// Key UI 캔버스([F] 상호작용)를 켜고, 그 상태에서 F 를 누르면 상호작용한다.
///
/// [Fusion 이전 메모]
///   - "내 캐릭터인지"는 NetworkObject.HasStateAuthority 로 판단한다.
///   - 로비처럼 아직 네트워크에 스폰되지 않은(NetworkObject 가 없거나 무효한) 로컬
///     캐릭터에서는 항상 내 것으로 취급해 로컬에서 동작한다.
/// </summary>
public class InteractionDetector : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("상호작용 대상에 닿았을 때 켤 Key UI 캔버스 (평소엔 꺼둠)")]
    [SerializeField] private GameObject keyUICanvas;

    private IInteractable currentInteractable;
    private NetworkObject netObject;

    // 네트워크 오브젝트가 없거나(로비 로컬) 무효하면 내 것으로 간주.
    private bool IsMine => netObject == null || !netObject.IsValid || netObject.HasStateAuthority;

    private void Awake()
    {
        netObject = GetComponent<NetworkObject>();

        if (keyUICanvas != null)
            keyUICanvas.SetActive(false);
    }

    private void Update()
    {
        if (!IsMine) return;
        if (currentInteractable == null) return;

        var kb = Keyboard.current;
        if (kb != null && kb.fKey.wasPressedThisFrame)
        {
            currentInteractable.Interact();

            if (keyUICanvas != null)
                keyUICanvas.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsMine) return;

        var interactable = other.GetComponent<IInteractable>();
        if (interactable == null) return;

        currentInteractable = interactable;
        if (keyUICanvas != null)
            keyUICanvas.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsMine) return;

        var interactable = other.GetComponent<IInteractable>();
        if (interactable == null || interactable != currentInteractable) return;

        currentInteractable = null;
        if (keyUICanvas != null)
            keyUICanvas.SetActive(false);
    }
}
