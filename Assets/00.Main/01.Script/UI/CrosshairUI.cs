using ProjectMS.CharacterSystem;
using ProjectMS.CharacterSystem.Examples;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 전투 중 마우스 커서 대신 보이는 조준점. 재장전 중에는 조준점 둘레의 링이 재장전 진행도만큼 차오르고,
/// 탄이 바닥나면 조준점이 emptyColor로 깜빡인다.
///
/// [표시 규칙]
///   - 체이서 데드아이(저격 모드) 중에는 조준경(ChaserScopeUI)이 대신 보이므로 조준점을 숨긴다
///     (시스템 커서도 계속 숨긴다).
///   - 전투 중(MatchPhase.Fighting)이고 내 캐릭터가 있고 조작이 잠겨 있지 않을 때만 조준점을 보이고
///     시스템 커서를 숨긴다. 그 밖(증강 선택, 결과 화면, 설정 창 등)에는 시스템 커서로 돌아간다.
///
/// [씬 설정]
///   - 게임 씬 Canvas 맨 아래(가장 위에 그려지게)에 빈 오브젝트를 만들고 이 스크립트를 붙인다.
///   - crosshair: 조준점 루트(RectTransform). 마우스 위치로 옮겨진다. 숨김은 CanvasGroup으로 한다.
///     자식 Graphic(선, 점 등)은 reloadRing을 빼고 전부 색이 같이 바뀐다.
///   - reloadRing: Filled(Radial360) Image. crosshair의 자식으로 두면 같이 움직인다. 비워두면 생략.
/// </summary>
public class CrosshairUI : MonoBehaviour
{
    [SerializeField] private RectTransform crosshair;
    [SerializeField] private Image reloadRing;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color emptyColor = new Color(1f, 0.3f, 0.3f, 1f);
    [Min(0f)] [SerializeField] private float blinkSpeed = 10f;

    private Canvas canvas;
    private CanvasGroup group;
    private bool visible = true;
    private Graphic[] parts;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        if (crosshair != null)
        {
            group = crosshair.GetComponent<CanvasGroup>();
            if (group == null)
                group = crosshair.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            var list = new System.Collections.Generic.List<Graphic>(crosshair.GetComponentsInChildren<Graphic>(true));
            list.Remove(reloadRing);
            parts = list.ToArray();
            foreach (Graphic part in parts)
                part.raycastTarget = false;
        }
        SetVisible(false);
    }

    private void OnDisable()
    {
        Cursor.visible = true;
    }

    private void LateUpdate()
    {
        CharacterBase local = CharacterBase.LocalPlayer;
        MatchManager match = MatchManager.Instance;
        Mouse mouse = Mouse.current;

        bool show = crosshair != null && mouse != null && local != null && local.Object != null &&
                    match != null && match.Phase == MatchPhase.Fighting && !CharacterBase.IsLobbyControlLocked;
        SetVisible(show);
        if (!show)
            return;

        MoveToMouse(mouse.position.ReadValue());

        // 데드아이 중엔 조준경이 조준점 역할을 한다.
        bool sniping = local is ChaserCharacter chaser && chaser.IsSniping;
        if (group != null)
            group.alpha = sniping ? 0f : 1f;
        if (sniping)
            return;

        bool reloading = local.IsReloading;
        if (reloadRing != null)
        {
            reloadRing.enabled = reloading;
            if (reloading)
                reloadRing.fillAmount = local.ReloadProgress;
        }

        if (parts != null)
        {
            bool empty = !reloading && local.TryGetAmmo(out int current, out _) && current <= 0;
            float blink = 0.5f + 0.5f * Mathf.Sin(Time.time * blinkSpeed);
            Color color = empty ? Color.Lerp(normalColor, emptyColor, blink) : normalColor;
            foreach (Graphic part in parts)
                part.color = color;
        }
    }

    private void MoveToMouse(Vector2 screenPosition)
    {
        RectTransform parent = crosshair.parent as RectTransform;
        if (parent == null)
            return;

        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPosition, cam, out Vector2 local))
            crosshair.localPosition = local;
    }

    private void SetVisible(bool value)
    {
        if (visible == value)
            return;

        visible = value;
        if (group != null)
            group.alpha = value ? 1f : 0f;
        Cursor.visible = !value;
    }
}
