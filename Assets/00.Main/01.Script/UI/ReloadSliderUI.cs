using ProjectMS.CharacterSystem;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 재장전 진행도 슬라이더. 재장전이 시작되면(휠 수동 / 탄창 소진 자동) 슬라이더를 켜고
/// value 를 0 → 1 로 재장전 시간 동안 채운 뒤, 다 차면 잠깐 보여주고 숨긴다.
///
/// [대상 캐릭터]
///   - 캐릭터 프리팹 안(머리 위 월드 캔버스 등)에 두면 부모의 CharacterBase 를 따라간다.
///   - 화면 HUD 에 두면 내가 조작하는 캐릭터(CharacterBase.LocalPlayer)를 따라간다.
///
/// [씬 설정]
///   - slider: 진행도를 표시할 Slider (Min 0 / Max 1 로 맞춰둔다. 코드에서도 강제함).
///   - visualRoot: 재장전 중에만 켤 오브젝트. 비워두면 slider 오브젝트 자체를 켜고 끈다.
/// </summary>
public class ReloadSliderUI : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [Tooltip("재장전 중에만 켤 오브젝트. 비워두면 slider 오브젝트를 켜고 끈다.")]
    [SerializeField] private GameObject visualRoot;
    [Tooltip("다 찬 뒤 숨기기 전까지 1 상태로 보여주는 시간(초).")]
    [SerializeField] private float holdAfterComplete = 0.15f;

    private CharacterBase ownerInParents;
    private bool animating;
    private float animStartTime;
    private float animStartValue;
    private float animDuration;
    private float hideAt = -1f;

    private GameObject Root => visualRoot != null ? visualRoot : (slider != null ? slider.gameObject : null);

    private void Awake()
    {
        ownerInParents = GetComponentInParent<CharacterBase>();
        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.interactable = false;
        }
        SetVisible(false);
    }

    private void Update()
    {
        CharacterBase target = ownerInParents != null ? ownerInParents : CharacterBase.LocalPlayer;
        if (target == null || target.Object == null || slider == null)
        {
            animating = false;
            SetVisible(false);
            return;
        }

        if (target.IsReloading)
        {
            // 재장전이 새로 시작됐으면(또는 중간에 켜졌으면) 남은 시간 기준으로 로컬 애니메이션 시작.
            // 네트워크 틱(초당 32회) 단위로 값을 읽으면 계단처럼 끊겨 보여서, 시작 시점만 맞추고
            // 채우는 건 프레임마다 부드럽게 한다.
            if (!animating)
            {
                animating = true;
                animStartValue = target.ReloadProgress;
                animDuration = Mathf.Max(0.01f, target.ReloadDuration * (1f - animStartValue));
                animStartTime = Time.time;
                hideAt = -1f;
                SetVisible(true);
            }

            float t = Mathf.Clamp01((Time.time - animStartTime) / animDuration);
            slider.value = Mathf.Lerp(animStartValue, 1f, t);
            return;
        }

        if (animating)
        {
            // 재장전 끝 → 1 로 채워서 잠깐 보여준 뒤 숨긴다.
            animating = false;
            slider.value = 1f;
            hideAt = Time.time + holdAfterComplete;
        }

        if (hideAt >= 0f && Time.time >= hideAt)
        {
            hideAt = -1f;
            SetVisible(false);
        }
    }

    private void SetVisible(bool visible)
    {
        GameObject root = Root;
        if (root == null)
            return;

        // 이 스크립트가 붙은 오브젝트 자체를 끄면 Update 가 멈춰서 다시 못 켜므로, 그땐 투명도로 숨긴다.
        if (root == gameObject)
        {
            CanvasGroup group = GetComponent<CanvasGroup>();
            if (group == null)
                group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = visible ? 1f : 0f;
            return;
        }

        if (root.activeSelf != visible)
            root.SetActive(visible);
    }
}
