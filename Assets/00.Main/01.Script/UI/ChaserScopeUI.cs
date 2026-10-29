using ProjectMS.CharacterSystem;
using ProjectMS.CharacterSystem.Examples;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 체이서 궁극기(데드아이 저격 모드) 조준경.
///   - 궁극기를 쓰면 ultCanvas(체이서 R 캔버스)를 켜고, 저격 모드가 끝나면 끈다.
///   - 그동안 scope 이미지가 마우스를 따라다니고, 한 발 쏠 때마다 살짝 커지면서 빨갛게 번쩍였다가 돌아온다.
///   - 그 체이서가 내가 조작하는 캐릭터일 때만 동작한다(상대 체이서의 캔버스는 내 화면에 안 켜짐).
///
/// [연결]
///   - 체이서 프리팹 안(루트 등)에 붙이고, ultCanvas / scope 를 인스펙터에서 연결한다.
///   - 이 스크립트를 ultCanvas 안에 붙여도 되지만, 그 경우엔 오브젝트를 끄지 않고 Canvas 컴포넌트만 켜고 끈다
///     (오브젝트를 끄면 이 스크립트도 멈추기 때문).
///   - 평소(적을 겨누지 않음)엔 aimColor(빨강), shotColor 가 쏠 때 번쩍이는 색이다.
///   - 락온: 지금 쏘면 적이 맞는 위치에 조준경이 있으면 lockColor(보라)로 바뀌고 두근거린다.
///     처음 락온될 때 lockClip을 재생한다(판정 반경은 ChaserCharacter의 Dead Eye Lock Radius).
/// </summary>
public class ChaserScopeUI : MonoBehaviour
{
    [Tooltip("궁극기(저격 모드) 동안 켤 캔버스 (체이서 R 캔버스)")]
    [SerializeField] private GameObject ultCanvas;
    [Tooltip("마우스를 따라다닐 조준경 이미지")]
    [SerializeField] private Image scope;
    [Tooltip("비워두면 부모에서 체이서를 찾는다.")]
    [SerializeField] private ChaserCharacter chaser;

    [Header("발사 반동 연출")]
    [Tooltip("쏠 때 순간적으로 커지는 배율")]
    [SerializeField] private float shotScale = 1.2f;
    [SerializeField] private Color shotColor = Color.red;
    [Tooltip("커지고 빨개졌다가 원래대로 돌아오는 시간(초)")]
    [SerializeField] private float shotRecoverDuration = 0.2f;
    [Tooltip("적을 겨누지 않았을 때(쏴도 안 맞는 위치) 조준경 색.")]
    [SerializeField] private Color aimColor = new Color(1f, 0.25f, 0.25f, 1f);
    [Tooltip("적을 겨눴을 때(지금 쏘면 맞는 위치) 조준경 색.")]
    [SerializeField] private Color lockColor = new Color(0.72f, 0.35f, 1f, 1f);
    [Min(0f)] [SerializeField] private float lockPulse = 0.12f;
    [SerializeField] private AudioClip lockClip;

    private RectTransform scopeRect;
    private Color normalColor;
    private bool wasLocked;
    private Vector3 normalScale;
    private bool toggleCanvasComponentsOnly;
    private bool visible = true;

    private int lastShotCount;
    private float shotTime = -1f;

    private void Awake()
    {
        if (chaser == null)
            chaser = GetComponentInParent<ChaserCharacter>();

        if (scope != null)
        {
            scopeRect = scope.rectTransform;
            normalColor = aimColor;
            normalScale = scopeRect.localScale;
            scope.raycastTarget = false; // 클릭을 가로채지 않게
        }

        toggleCanvasComponentsOnly = ultCanvas != null && transform.IsChildOf(ultCanvas.transform);
        SetVisible(false);
    }

    private void Update()
    {
        bool show = chaser != null && chaser.Object != null && chaser.IsLocalPlayer && chaser.IsSniping && !chaser.IsDead;

        if (show != visible)
        {
            SetVisible(show);
            if (show)
            {
                // 저격 모드 진입: 발사 카운트 기준점을 맞추고 조준경 모양 초기화.
                lastShotCount = chaser.SnipeShotCount;
                ResetShotVisual();
            }
        }

        if (!show || scopeRect == null)
            return;

        FollowMouse();

        if (chaser.SnipeShotCount != lastShotCount)
        {
            lastShotCount = chaser.SnipeShotCount;
            shotTime = Time.unscaledTime;
        }

        if (shotTime >= 0f)
            UpdateShotVisual();
        else
            UpdateLockVisual();
    }

    private void UpdateLockVisual()
    {
        bool locked = false;
        Camera worldCamera = Camera.main;
        Mouse mouse = Mouse.current;
        if (worldCamera != null && mouse != null)
        {
            Vector2 screen = mouse.position.ReadValue();
            Vector3 world = worldCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -worldCamera.transform.position.z));
            locked = chaser.IsAimOnEnemy(world);
        }

        if (locked && !wasLocked)
            SoundManager.Instance?.PlaySfx(lockClip);
        wasLocked = locked;

        float pulse = locked ? 1f + lockPulse * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 18f)) : 1f;
        scopeRect.localScale = normalScale * pulse;
        scope.color = locked ? lockColor : normalColor;
    }

    private void SetVisible(bool value)
    {
        visible = value;
        if (ultCanvas == null)
            return;

        if (toggleCanvasComponentsOnly)
        {
            foreach (Canvas canvas in ultCanvas.GetComponentsInChildren<Canvas>(true))
                canvas.enabled = value;
        }
        else if (ultCanvas.activeSelf != value)
        {
            ultCanvas.SetActive(value);
        }
    }

    private void FollowMouse()
    {
        Mouse mouse = Mouse.current;
        RectTransform parentRect = scopeRect.parent as RectTransform;
        if (mouse == null || parentRect == null)
            return;

        Canvas canvas = scope.canvas;
        Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, mouse.position.ReadValue(), uiCamera, out Vector2 local))
            scopeRect.localPosition = local;
    }

    // 쏜 순간 shotScale/shotColor 로 튀었다가 shotRecoverDuration 동안 부드럽게 원래대로.
    private void UpdateShotVisual()
    {
        if (shotTime < 0f)
            return;

        float t = Mathf.Clamp01((Time.unscaledTime - shotTime) / Mathf.Max(0.01f, shotRecoverDuration));
        float eased = 1f - (1f - t) * (1f - t); // 처음엔 빠르게, 끝에선 천천히 돌아온다
        scopeRect.localScale = Vector3.Lerp(normalScale * shotScale, normalScale, eased);
        scope.color = Color.Lerp(shotColor, normalColor, eased);

        if (t >= 1f)
            shotTime = -1f;
    }

    private void ResetShotVisual()
    {
        shotTime = -1f;
        wasLocked = false;
        if (scopeRect == null)
            return;
        scopeRect.localScale = normalScale;
        scope.color = normalColor;
    }
}
