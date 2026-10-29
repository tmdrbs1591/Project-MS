using ProjectMS.CharacterSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 로비의 증강 팩 선택 건물(AugmentPackBuilding)과 상호작용하면 뜨는 카드팩 보관함.
/// 왼쪽에 전체 팩 목록(AugmentPackRow), 오른쪽에 고른 팩의 상세(AugmentPackDetailUI)를 보여준다.
/// 해금 여부는 각 행이 칩("해금됨"/"잠김")으로 표시하고, 잠긴 팩도 눌러서 담긴 증강을 미리 볼 수 있다.
/// 열릴 때 첫 번째 팩을 기본 선택해서 상세가 빈 채로 보이지 않게 한다. (재화/구매는 아직 없음)
///
/// [연출]
///   - 열릴 때: 뒤의 로비가 흐려지고(배경 블러) 로비 HUD는 사라지며, 창이 페이드인하면서
///     목록은 왼쪽에서, 상세는 오른쪽에서 밀려 들어오고 팩 줄들이 위에서부터 차례로 나타난다.
///   - 닫을 때(X/뒤로 버튼, Esc): 반대로 페이드아웃하고 블러가 풀린다.
///   - 키보드: ↑/↓(W/S)로 팩을 바꾼다.
///
/// [씬 설정]
///   - panel: 평소엔 꺼져 있다가 Open() 호출 시 켜지는 패널(CanvasGroup이 없으면 자동으로 붙는다).
///   - rowContainer / rowPrefab: 팩 줄(AugmentPackRow)들이 생성될 부모와 프리팹.
///   - detailUI: 고른 팩 상세를 보여줄 패널.
///   - closeButton / extraCloseButtons: 누르면 창을 닫는 버튼들.
/// </summary>
public class AugmentPackManageUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform rowContainer;
    [SerializeField] private AugmentPackRow rowPrefab;
    [SerializeField] private AugmentPackDetailUI detailUI;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button[] extraCloseButtons;

    [Header("연출")]
    [SerializeField] private RectTransform listRoot;
    [SerializeField] private RectTransform detailRoot;
    [SerializeField] private RectTransform headerRoot;
    [Min(0.01f)] [SerializeField] private float openDuration = 0.3f;
    [Min(0.01f)] [SerializeField] private float closeDuration = 0.18f;
    [SerializeField] private float slideDistance = 60f;
    [Tooltip("창이 열려 있는 동안 뒤의 로비 화면을 흐리게 하는 배경 블러.")]
    [SerializeField] private UIBackgroundBlur backgroundBlur;
    [Tooltip("창이 열려 있는 동안 페이드아웃해서 숨길 로비 HUD(채팅, 키 안내, 플레이어 이름 등).")]
    [SerializeField] private CanvasGroup[] hideWhileOpen;

    [Header("사운드")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [SerializeField] private AudioClip selectSound;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.6f;

    private readonly List<AugmentPackRow> spawnedRows = new List<AugmentPackRow>();
    private CanvasGroup canvasGroup;
    private Coroutine animRoutine;
    private bool isOpen;
    private AugmentPackData selectedPack;
    private Vector2 listBase;
    private Vector2 detailBase;
    private Vector2 headerBase;

    private void Awake()
    {
        if (panel != null)
        {
            canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = panel.AddComponent<CanvasGroup>();
        }

        if (listRoot != null)
            listBase = listRoot.anchoredPosition;
        if (detailRoot != null)
            detailBase = detailRoot.anchoredPosition;
        if (headerRoot != null)
            headerBase = headerRoot.anchoredPosition;

        SetPanelActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
        if (extraCloseButtons != null)
        {
            foreach (Button button in extraCloseButtons)
            {
                if (button != null)
                    button.onClick.AddListener(Close);
            }
        }
    }

    private void Update()
    {
        if (!isOpen)
            return;

        Keyboard kb = Keyboard.current;
        if (kb == null)
            return;

        if (kb.escapeKey.wasPressedThisFrame)
        {
            // 카드를 크게 보고 있으면 Esc는 그것부터 닫는다.
            if (detailUI == null || !detailUI.TryCloseInspector())
                Close();
        }
        else if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)
            StepSelection(-1);
        else if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame)
            StepSelection(1);
    }

    public void Open()
    {
        if (isOpen)
            return;
        isOpen = true;

        CharacterBase.SetLobbyControlLocked(true);
        LobbyCharacterController.SetLocked(true);

        SetPanelActive(true);
        BuildRows();
        selectedPack = null;
        SelectFirstPackByDefault();

        SoundManager.Instance?.PlaySfx(openSound, sfxVolume);
        if (backgroundBlur != null)
            backgroundBlur.SetTarget(1f);

        for (int i = 0; i < spawnedRows.Count; i++)
            spawnedRows[i].PlayReveal(0.08f + i * 0.05f);

        RestartRoutine(Fade(0f, 1f, openDuration, opening: true));
    }

    public void Close()
    {
        if (!isOpen)
            return;
        isOpen = false;

        CharacterBase.SetLobbyControlLocked(false);
        LobbyCharacterController.SetLocked(false);

        SoundManager.Instance?.PlaySfx(closeSound, sfxVolume);
        if (backgroundBlur != null)
            backgroundBlur.SetTarget(0f);

        RestartRoutine(Fade(1f, 0f, closeDuration, opening: false));
    }

    private void BuildRows()
    {
        foreach (AugmentPackRow row in spawnedRows)
        {
            if (row != null)
                Destroy(row.gameObject);
        }
        spawnedRows.Clear();

        if (rowPrefab == null || rowContainer == null)
            return;

        foreach (AugmentPackData pack in AugmentPackManager.AllPacks)
        {
            AugmentPackRow row = Instantiate(rowPrefab, rowContainer);
            row.Bind(pack, OnPackDetailRequested);
            spawnedRows.Add(row);
        }
    }

    private void OnPackDetailRequested(AugmentPackData pack)
    {
        if (pack == selectedPack)
            return;

        if (selectedPack != null)
            SoundManager.Instance?.PlaySfx(selectSound, sfxVolume, 0.04f);

        selectedPack = pack;
        foreach (AugmentPackRow row in spawnedRows)
        {
            if (row != null)
                row.SetSelected(row.Pack == pack);
        }

        detailUI?.Show(pack);
    }

    /// <summary>상세 패널이 열리자마자 빈 상태로 보이지 않도록 첫 번째 팩을 기본 선택한다.</summary>
    private void SelectFirstPackByDefault()
    {
        IReadOnlyList<AugmentPackData> packs = AugmentPackManager.AllPacks;
        if (packs.Count > 0)
            OnPackDetailRequested(packs[0]);
    }

    private void StepSelection(int direction)
    {
        if (spawnedRows.Count == 0)
            return;

        int current = spawnedRows.FindIndex(r => r != null && r.Pack == selectedPack);
        int next = Mathf.Clamp(current + direction, 0, spawnedRows.Count - 1);
        if (next != current && spawnedRows[next] != null)
            OnPackDetailRequested(spawnedRows[next].Pack);
    }

    private void RestartRoutine(IEnumerator routine)
    {
        if (animRoutine != null)
            StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(routine);
    }

    /// <summary>창 전체 페이드 + 목록(왼쪽)/상세(오른쪽)/제목(위)이 각자 바깥에서 밀려 들어오거나 나간다.
    /// HUD는 창이 보이는 만큼 사라진다.</summary>
    private IEnumerator Fade(float from, float to, float duration, bool opening)
    {
        SetInteractable(false);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = opening ? 1f - Mathf.Pow(1f - t, 3f) : t * t;
            ApplyVisual(Mathf.Lerp(from, to, eased));
            yield return null;
        }

        ApplyVisual(to);
        if (opening)
            SetInteractable(true);
        else
            SetPanelActive(false);
        animRoutine = null;
    }

    private void ApplyVisual(float visible)
    {
        if (canvasGroup != null)
            canvasGroup.alpha = visible;

        float off = (1f - visible) * slideDistance;
        if (listRoot != null)
            listRoot.anchoredPosition = listBase + new Vector2(-off, 0f);
        if (detailRoot != null)
            detailRoot.anchoredPosition = detailBase + new Vector2(off, 0f);
        if (headerRoot != null)
            headerRoot.anchoredPosition = headerBase + new Vector2(0f, off * 0.5f);

        if (hideWhileOpen == null)
            return;
        foreach (CanvasGroup hud in hideWhileOpen)
        {
            if (hud == null)
                continue;
            hud.alpha = 1f - visible;
            hud.blocksRaycasts = visible < 0.5f;
        }
    }

    private void SetInteractable(bool interactable)
    {
        if (canvasGroup == null)
            return;
        canvasGroup.interactable = interactable;
        canvasGroup.blocksRaycasts = interactable;
    }

    private void SetPanelActive(bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }
}
