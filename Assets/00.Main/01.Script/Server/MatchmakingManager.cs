using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 매칭 UI 래퍼. 실제 네트워크 접속/매칭은 NetworkLauncher 가 담당한다. (Fusion 2)
///
/// [역할]
///   - 매칭 버튼/포탈에서 StartMatching() 을 호출하면 NetworkLauncher 로 위임한다.
///   - NetworkLauncher 의 상태 메시지를 받아 UI 텍스트로 표시한다.
///
/// [씬 설정]
///   - 로비 씬에 NetworkLauncher(빈 오브젝트)를 두고, 이 스크립트는 UI 와 함께 둔다.
///   - 정원/게임 씬 이름 설정은 NetworkLauncher 쪽에 있다.
/// </summary>
public class MatchmakingManager : MonoBehaviour
{
    [Header("UI (선택)")]
    [SerializeField] private Button matchButton;
    [SerializeField] private TMP_Text statusText;

    private void Start()
    {
        if (matchButton != null)
            matchButton.onClick.AddListener(StartMatching);

        if (NetworkLauncher.Instance != null)
            NetworkLauncher.Instance.StatusChanged += SetStatus;

        SetStatus("매칭 대기 중");
    }

    private void OnDestroy()
    {
        if (NetworkLauncher.Instance != null)
            NetworkLauncher.Instance.StatusChanged -= SetStatus;
    }

    /// <summary>포탈/버튼에서 호출.</summary>
    public void StartMatching()
    {
        if (NetworkLauncher.Instance == null)
        {
            SetStatus("NetworkLauncher 가 씬에 없습니다.");
            Debug.LogError("[Matchmaking] NetworkLauncher.Instance 가 null 입니다. 로비 씬에 NetworkLauncher 를 배치하세요.");
            return;
        }

        if (matchButton != null)
            matchButton.interactable = false;

        NetworkLauncher.Instance.StartMatchmaking();
    }

    public void CancelMatching()
    {
        if (NetworkLauncher.Instance != null)
            NetworkLauncher.Instance.CancelMatchmaking();

        if (matchButton != null)
            matchButton.interactable = true;
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}
