using System;
using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;

namespace ProjectMS.CharacterSystem.Examples
{
    public class PopupErrorPopupDeployable : CharacterDeployable
    {
        [SerializeField] private Vector2 popupOffset;
        [Min(0.1f)][SerializeField] private float popupTabLeashRadius = 0.5f;

        [Header("Break Progress")]
        [Tooltip("연타 진행도(예: 3 / 10)를 그릴 텍스트. 걸린 사람은 화면 가운데에 크게 보므로, 나머지 사람에게만 보인다.")]
        [SerializeField] private TMP_Text breakProgressText;

        [Header("World Windows")]
        [Tooltip("대상에 붙는 메인 에러창 그림. 이걸 복사해서 주변에 창을 여러 개 더 띄운다.")]
        [SerializeField] private SpriteRenderer windowRenderer;
        [Min(0)] [SerializeField] private int extraWindowCount = 4;
        [Tooltip("추가 창이 메인 창 기준으로 흩어지는 범위(월드 단위, ±).")]
        [SerializeField] private Vector2 extraWindowSpread = new Vector2(0.9f, 0.6f);
        [SerializeField] private Vector2 extraWindowScaleRange = new Vector2(0.5f, 0.8f);
        [Min(0.01f)] [SerializeField] private float windowPopInterval = 0.07f;
        [SerializeField] private AudioClip windowPopClip;
        [Range(0f, 1f)] [SerializeField] private float windowPopVolume = 0.3f;
        [Tooltip("연타 한 번 들어갈 때마다 창 위치에서 터지는 이펙트.")]
        [SerializeField] private SkillFxSlot[] breakPressFx;
        [Tooltip("팝업이 사라질 때(깨지거나 시간이 다 돼서) 터지는 이펙트.")]
        [SerializeField] private SkillFxSlot[] breakFx;

        [Header("Screen Overlay (걸린 사람 화면)")]
        [SerializeField] private PopupErrorOverlaySettings screenOverlay = new PopupErrorOverlaySettings();

        private sealed class WorldWindow
        {
            public Transform Transform;
            public SpriteRenderer Renderer;
            public Vector3 BaseLocalPosition;
            public Vector3 BaseScale;
            public float BaseRotation;
            public float AppearAt;
            public float CloseAt = -1f;
            public bool IsClone;
            public Vector3 Jitter;
            public float NextJitter;
        }

        private static readonly List<PopupErrorPopupDeployable> active = new List<PopupErrorPopupDeployable>();

        /// <summary>이 캐릭터가 지금 에러창에 갇혀 있는지(권한자 쪽에서만 정확 — AI 봇이 연타할지 정할 때 쓴다).</summary>
        public static bool IsTrapped(CharacterBase character)
        {
            if (character == null)
                return false;
            foreach (PopupErrorPopupDeployable d in active)
                if (d != null && d.target == character && !d.IsDestroying)
                    return true;
            return false;
        }

        private readonly List<WorldWindow> worldWindows = new List<WorldWindow>();
        private PopupErrorScreenOverlay overlay;
        private bool overlayShown;
        private float progressPunchTime = -10f;
        private Vector3 progressTextBaseScale = Vector3.one;

        // target/damage 관련 값은 스폰한 쪽(시전자, 권한자)에서만 채워진다 — 원격 클라이언트는
        // 이 필드를 못 쓰므로, 화면 표시에 필요한 값은 [Networked]로 따로 복제한다.
        [Networked] private PlayerRef NetTargetPlayer { get; set; }
        [Networked] private int NetBreakProgress { get; set; }
        [Networked] private int NetBreakRequired { get; set; }

        private CharacterBase target;

        private float damageDuration;
        private float totalDamage;
        private int damageTimes;

        private CharacterTimerHandler timers;

        private int currentDealedCount = 0;
        private int lastRenderedProgress = -1;

        private void Awake()
        {
            timers = new CharacterTimerHandler();
            if (breakProgressText != null)
                progressTextBaseScale = breakProgressText.transform.localScale;
        }

        public override void Spawned()
        {
            base.Spawned();
            if (!active.Contains(this))
                active.Add(this);
            lastRenderedProgress = -1;
            overlayShown = false;
            BuildWorldWindows();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            active.Remove(this);
            target = null;
            if (overlay != null)
                overlay.End(false);
            overlay = null;
            ClearWorldWindows();
        }

        protected override void OnOwnedEntityDestroyedRendered(OwnedEntityDestroyReason reason)
        {
            base.OnOwnedEntityDestroyedRendered(reason);
            bool cleared = NetBreakRequired > 0 && NetBreakProgress >= NetBreakRequired;
            Vector3 at = windowRenderer != null ? windowRenderer.transform.position : transform.position;
            SkillFxSlot.SpawnAll(breakFx, at, 0f);
            if (overlay != null)
                overlay.End(cleared);
            overlay = null;
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            if (Object == null || !Object.IsValid || !Object.HasStateAuthority)
                return;
            
            if (target != null)
            {
                // idealPosition = 타겟에 팝업창이 붙어있을 때의 위치 (타겟 현재 위치 + 오프셋) --> 의도하는 위치
                // 팝업창은 자기 위치(transform.postion)를 그대로 유지하다가
                // idealPosition 에서 leash 범위보다 멀어졌을 때만 다시 타겟에 위치로 따라잡음.

                Vector3 idealPosition = target.transform.position + (Vector3)popupOffset;

                if (Vector2.Distance(transform.position, idealPosition) > popupTabLeashRadius)
                    transform.position = idealPosition;
            }
            timers.Tick(Runner.DeltaTime);
        }

        public override void Render()
        {
            base.Render();
            RenderBreakProgress();
            RenderScreenOverlay();
            AnimateWorldWindows();
        }

        /// <summary>시간 제한은 없다 — 대상이 연타로 깰 때까지(또는 PopupCharacter가 정리할 때까지) 남는다.
        /// _damageDuration은 도트 데미지를 나눠 넣는 시간일 뿐이다.</summary>
        public void Initialize(CharacterBase _target, float _damageDuration, float _totalDamage, int _damageTimes, int _breakRequired)
        {
            // PopupCharacter에서 target에 행동 불가 상태 추가

            target = _target;
            damageDuration = _damageDuration;
            totalDamage = _totalDamage;
            damageTimes = _damageTimes;

            if (!Object.HasStateAuthority)
                return;

            // 팝업을 깨야 하는 당사자의 화면에서만 진행도를 보여주기 위해 대상 플레이어를 복제해둔다.
            NetTargetPlayer = target != null && target.Object != null
                ? target.Object.InputAuthority
                : PlayerRef.None;
            NetBreakRequired = Mathf.Max(0, _breakRequired);
            NetBreakProgress = 0;

            SetContinuousDamage();

            if (LifetimeMode != OwnedEntityLifetimeMode.Manual)
                Debug.LogError($"[PopupErrorPopupDeployable] 팝업의 \'시스템 에러 팝업 등장!\'(궁극기)의 오류 팝업창은 연타로만 깨지므로 Lifetime Mode가 Manual이여야 합니다! (현재 : {LifetimeMode})", this);
        }

        /// <summary>시전자가 센 연타 횟수를 반영한다(권한자만 기록). 표시는 대상 본인 화면에서만 된다.</summary>
        public void SetBreakProgress(int progress)
        {
            if (Object == null || !Object.HasStateAuthority)
                return;

            NetBreakProgress = Mathf.Clamp(progress, 0, NetBreakRequired);
        }

        private bool IsTargetLocal => Runner != null &&
                                      NetTargetPlayer != PlayerRef.None &&
                                      NetTargetPlayer == Runner.LocalPlayer;

        private void RenderBreakProgress()
        {
            int progress = NetBreakProgress;
            if (progress != lastRenderedProgress)
            {
                // 처음 값(0)이 아니라 실제로 한 번 눌린 경우에만 팡 터뜨린다.
                if (lastRenderedProgress >= 0 && progress > lastRenderedProgress)
                    OnBreakPressRendered();
                lastRenderedProgress = progress;
                if (breakProgressText != null)
                    breakProgressText.text = progress + "<size=65%><alpha=#AA> / " + NetBreakRequired + "</size>";
            }

            if (breakProgressText == null)
                return;

            // 걸린 사람은 화면 가운데에 큰 숫자를 보므로, 머리 위 숫자는 다른 사람(시전자/관전)에게만 보인다.
            bool visible = NetTargetPlayer != PlayerRef.None && NetBreakRequired > 0 && !IsTargetLocal;
            if (breakProgressText.enabled != visible)
                breakProgressText.enabled = visible;
            if (!visible)
                return;

            float k = Mathf.Clamp01((Time.time - progressPunchTime) / 0.2f);
            float pulse = 1f + 0.04f * Mathf.Sin(Time.time * 10f);
            breakProgressText.transform.localScale = progressTextBaseScale * pulse * Mathf.Lerp(1.4f, 1f, 1f - (1f - k) * (1f - k));
        }

        private void OnBreakPressRendered()
        {
            progressPunchTime = Time.time;
            Vector3 at = windowRenderer != null ? windowRenderer.transform.position : transform.position;
            SkillFxSlot.SpawnAll(breakPressFx, at, 0f);

            // 연타가 들어갈수록 주변 창이 하나씩 닫힌다(마지막엔 메인 창만 남는다).
            int clones = 0;
            foreach (WorldWindow w in worldWindows) if (w.IsClone) clones++;
            int keep = NetBreakRequired > 0
                ? Mathf.CeilToInt(clones * (1f - NetBreakProgress / (float)NetBreakRequired))
                : clones;
            int open = 0;
            for (int i = 0; i < worldWindows.Count; i++)
                if (worldWindows[i].IsClone && worldWindows[i].CloseAt < 0f) open++;
            for (int i = worldWindows.Count - 1; i >= 0 && open > keep; i--)
            {
                WorldWindow w = worldWindows[i];
                if (!w.IsClone || w.CloseAt >= 0f)
                    continue;
                w.CloseAt = Time.time;
                open--;
            }
        }

        private void RenderScreenOverlay()
        {
            if (!overlayShown && IsTargetLocal && NetBreakRequired > 0 && !IsDestroying)
            {
                overlayShown = true;
                overlay = PopupErrorScreenOverlay.Show(screenOverlay, NetBreakRequired);
            }
            if (overlay != null)
                overlay.SetNetProgress(NetBreakProgress);
        }

        // ───────── 대상에 붙는 에러창 묶음 ─────────

        private void BuildWorldWindows()
        {
            ClearWorldWindows();
            if (windowRenderer == null)
                return;

            float now = Time.time;
            Transform main = windowRenderer.transform;
            worldWindows.Add(new WorldWindow
            {
                Transform = main,
                Renderer = windowRenderer,
                BaseLocalPosition = main.localPosition,
                BaseScale = main.localScale,
                BaseRotation = main.localEulerAngles.z,
                AppearAt = now,
            });

            // 메인 창 뒤로 창이 줄줄이 더 튀어나온다(서로 다른 크기/각도, 뒤에 그려지게).
            for (int i = 0; i < extraWindowCount; i++)
            {
                GameObject clone = Instantiate(windowRenderer.gameObject, main.parent);
                clone.name = "Popup (Extra)";
                var sr = clone.GetComponent<SpriteRenderer>();
                sr.sortingOrder = windowRenderer.sortingOrder - 1 - i;
                Vector3 offset = new Vector3(
                    UnityEngine.Random.Range(-extraWindowSpread.x, extraWindowSpread.x),
                    UnityEngine.Random.Range(-extraWindowSpread.y, extraWindowSpread.y), 0f);
                worldWindows.Add(new WorldWindow
                {
                    Transform = clone.transform,
                    Renderer = sr,
                    BaseLocalPosition = main.localPosition + offset,
                    BaseScale = main.localScale * UnityEngine.Random.Range(extraWindowScaleRange.x, extraWindowScaleRange.y),
                    BaseRotation = UnityEngine.Random.Range(-12f, 12f),
                    AppearAt = now + windowPopInterval * (i + 1),
                    IsClone = true,
                });
            }
            AnimateWorldWindows();
        }

        private void ClearWorldWindows()
        {
            foreach (WorldWindow w in worldWindows)
            {
                if (w.IsClone)
                {
                    if (w.Transform != null)
                        Destroy(w.Transform.gameObject);
                }
                else if (w.Transform != null)
                {
                    w.Transform.localPosition = w.BaseLocalPosition;
                    w.Transform.localScale = w.BaseScale;
                    w.Transform.localEulerAngles = new Vector3(0f, 0f, w.BaseRotation);
                }
            }
            worldWindows.Clear();
        }

        private void AnimateWorldWindows()
        {
            float now = Time.time;
            float punch = Mathf.Clamp01((now - progressPunchTime) / 0.15f);
            for (int i = 0; i < worldWindows.Count; i++)
            {
                WorldWindow w = worldWindows[i];
                if (w.Transform == null)
                    continue;

                bool hidden = now < w.AppearAt;
                if (w.Renderer.enabled == hidden)
                {
                    w.Renderer.enabled = !hidden;
                    if (!hidden && w.IsClone)
                        SoundManager.Instance?.PlaySfx(windowPopClip, windowPopVolume, 0.15f);
                }
                if (hidden)
                    continue;

                float k = Mathf.Clamp01((now - w.AppearAt) / 0.16f);
                float scale = OutBack(k, 2.4f);
                if (w.CloseAt >= 0f)
                {
                    float c = Mathf.Clamp01((now - w.CloseAt) / 0.14f);
                    scale *= 1f - c * c;
                    if (c >= 1f)
                    {
                        w.Renderer.enabled = false;
                        continue;
                    }
                }
                if (!w.IsClone)
                    scale *= Mathf.Lerp(1.18f, 1f, punch);

                // 가끔 툭툭 위치가 튀어서 고장난 화면처럼 보이게
                if (now >= w.NextJitter)
                {
                    w.NextJitter = now + UnityEngine.Random.Range(0.08f, 0.35f);
                    w.Jitter = UnityEngine.Random.value < 0.35f ? (Vector3)(UnityEngine.Random.insideUnitCircle * 0.06f) : Vector3.zero;
                }

                w.Transform.localPosition = w.BaseLocalPosition + w.Jitter;
                w.Transform.localScale = w.BaseScale * scale;
                w.Transform.localEulerAngles = new Vector3(0f, 0f, w.BaseRotation);
            }
        }

        private static float OutBack(float t, float s)
        {
            t -= 1f;
            return t * t * ((s + 1f) * t + s) + 1f;
        }

        private void DealContinuosDamage()
        {
            // 파괴 요청(연타로 깨짐 등) 직후 스폰 해제까지 1틱이 남아 있어, 그 사이에 예약된 도트딜이
            // 한 번 더 들어가지 않도록 막는다.
            if (IsDestroying)
                return;

            DealDamage(target, totalDamage / damageTimes, CharacterDamageSource.Periodic);
            currentDealedCount++;
        }

        private void SetContinuousDamage()
        {
            if (!Object.HasStateAuthority)
                return;

            if (currentDealedCount >= damageTimes)
                return;

            timers.Schedule(damageDuration / damageTimes, () =>
            {
                if (IsDestroying)
                    return;

                DealContinuosDamage();
                SetContinuousDamage();
            });
        }

    }
}
