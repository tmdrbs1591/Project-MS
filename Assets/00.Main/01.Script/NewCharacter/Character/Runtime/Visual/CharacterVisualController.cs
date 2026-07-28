using System;
using UnityEngine;

namespace ProjectMS.CharacterSystem
{
    [Serializable]
    public sealed class CharacterJiggleTarget
    {
        [SerializeField] private Transform target;
        [SerializeField] private float bodyFollow = 1.5f;

        public Transform Target => target;
        public float BodyFollow => bodyFollow;
    }

    [Serializable]
    public sealed class CharacterActionEffect
    {
        [SerializeField] private CharacterActionType action;
        [SerializeField] private GameObject prefab;
        [Min(0f)] [SerializeField] private float lifetime = 2f;

        public CharacterActionType Action => action;
        public GameObject Prefab => prefab;
        public float Lifetime => lifetime;
    }

    [DisallowMultipleComponent]
    public sealed class CharacterVisualController : MonoBehaviour
    {
        [Header("Required References")]
        [SerializeField] private Transform body;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private CharacterVisualProfile profile;

        [Header("Optional Aim Visual")]
        [SerializeField] private AimVisualMode aimVisualMode = AimVisualMode.FlipOnly;
        [SerializeField] private Transform aimVisualRoot;
        [SerializeField] private float aimRotationOffset;

        [Header("Optional Effects")]
        [SerializeField] private ParticleSystem dustEffect;
        [SerializeField] private Animator animator;
        [SerializeField] private CharacterActionEffect[] actionEffects = Array.Empty<CharacterActionEffect>();

        [Header("Optional Jiggle Targets")]
        [SerializeField] private CharacterJiggleTarget[] jiggleTargets = Array.Empty<CharacterJiggleTarget>();

        [Header("Optional Counter Flip Targets")]
        [SerializeField] private Transform[] counterFlipTargets = Array.Empty<Transform>();

        private SquashStretchSolver squashStretch;
        private AimVisualSolver aimVisual;
        private JiggleSolver[] jiggles;
        private CounterFlipSolver[] counterFlips;
        private Color baseColor = Color.white;
        private float hitFlashTimer;

        private void Awake()
        {
            if (body == null)
                body = transform;
            if (bodyRenderer == null)
                bodyRenderer = body.GetComponentInChildren<SpriteRenderer>();

            if (profile == null)
            {
                Debug.LogError($"[{nameof(CharacterVisualController)}] Visual Profile이 필요합니다.", this);
                enabled = false;
                return;
            }

            if (bodyRenderer != null)
                baseColor = bodyRenderer.color;

            squashStretch = new SquashStretchSolver(body, bodyRenderer, profile);
            aimVisual = new AimVisualSolver(aimVisualRoot, aimVisualMode, aimRotationOffset);

            jiggles = new JiggleSolver[jiggleTargets.Length];
            for (int i = 0; i < jiggleTargets.Length; i++)
            {
                CharacterJiggleTarget entry = jiggleTargets[i];
                jiggles[i] = new JiggleSolver(entry.Target, body, profile, entry.BodyFollow);
            }

            counterFlips = new CounterFlipSolver[counterFlipTargets.Length];
            for (int i = 0; i < counterFlipTargets.Length; i++)
                counterFlips[i] = new CounterFlipSolver(counterFlipTargets[i]);

            if (dustEffect != null)
            {
                ParticleSystem.EmissionModule emission = dustEffect.emission;
                emission.enabled = false;
                if (!dustEffect.isPlaying)
                    dustEffect.Play();
            }
        }

        public void ApplyState(CharacterVisualState state)
        {
            if (!enabled || profile == null)
                return;

            squashStretch.Tick(state);
            aimVisual.Apply(state.AimDirection, state.AimAngle);
            UpdateDust(state.Grounded && !state.Dead && Mathf.Abs(state.MoveInput) > 0.01f);
            UpdateHitFlash(state.DeltaTime);
        }

        public void PlayJump()
        {
            squashStretch?.PlayJump();
            SetAnimatorTrigger("Jump");
        }

        public void PlayLanded()
        {
            SetAnimatorTrigger("Land");
        }

        public void PlayDamaged()
        {
            squashStretch?.PlayHit();
            hitFlashTimer = profile != null ? profile.HitFlashDuration : 0f;
            if (bodyRenderer != null && profile != null)
                bodyRenderer.color = profile.HitFlashColor;
            SetAnimatorTrigger("Damaged");
        }

        public void PlayDead()
        {
            SetAnimatorTrigger("Dead");
        }

        public void PlayRevived()
        {
            SetAnimatorTrigger("Revived");
        }

        public void PlayAction(CharacterActionType action)
        {
            if (action != CharacterActionType.None)
                SetAnimatorTrigger(action.ToString());
        }

        public void SpawnActionEffect(CharacterActionType action, Vector2 position, float angle)
        {
            foreach (CharacterActionEffect effect in actionEffects)
            {
                if (effect.Action != action || effect.Prefab == null)
                    continue;

                GameObject instance = Instantiate(
                    effect.Prefab,
                    position,
                    Quaternion.Euler(0f, 0f, angle));

                if (effect.Lifetime > 0f)
                    Destroy(instance, effect.Lifetime);
                return;
            }
        }

        private void LateUpdate()
        {
            if (jiggles != null)
            {
                foreach (JiggleSolver jiggle in jiggles)
                    jiggle?.Tick(Time.deltaTime);
            }

            if (counterFlips != null)
            {
                foreach (CounterFlipSolver counterFlip in counterFlips)
                    counterFlip?.Tick();
            }
        }

        private void UpdateDust(bool enabledState)
        {
            if (dustEffect == null)
                return;

            ParticleSystem.EmissionModule emission = dustEffect.emission;
            if (emission.enabled != enabledState)
                emission.enabled = enabledState;
        }

        private void UpdateHitFlash(float deltaTime)
        {
            if (hitFlashTimer <= 0f || bodyRenderer == null)
                return;

            hitFlashTimer -= deltaTime;
            if (hitFlashTimer <= 0f)
                bodyRenderer.color = baseColor;
        }

        private void SetAnimatorTrigger(string triggerName)
        {
            if (animator != null && !string.IsNullOrEmpty(triggerName))
                animator.SetTrigger(triggerName);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (body == null)
                body = transform;
            if (bodyRenderer == null && body != null)
                bodyRenderer = body.GetComponentInChildren<SpriteRenderer>();
        }
#endif
    }
}
