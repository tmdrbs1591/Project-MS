using System.Collections.Generic;
using ProjectMS.CharacterSystem.Examples;
using UnityEngine;

namespace ProjectMS.CharacterSystem.AI
{
    public enum BotDifficulty
    {
        Easy,
        Normal,
        Hard
    }

    /// <summary>
    /// AI 봇 공용 두뇌. 키보드/마우스 대신 CharacterInputSnapshot 을 만들어 캐릭터에 공급한다
    /// (ICharacterInputSource). 캐릭터별 플레이 스타일은 상속 클래스의 Think() 에서 정한다.
    ///
    /// [공용으로 처리하는 것]
    ///   - 상대 찾기, 반응 속도(상대 위치를 reactionTime 만큼 늦게 인지)
    ///   - 안전한 이동(낭떠러지 앞에서 멈춤, 벽/막힘 앞에서 점프)
    ///   - 날아오는 적 투사체/수류탄 회피
    ///   - 조준 헬퍼(리드샷 예측, 조준 오차, 포물선 투척 각도)
    ///   - 증강 선택(라운드 사이에 잠깐 고민하는 척하다가 랜덤으로 고름)
    ///
    /// 봇 캐릭터를 스폰한 클라(StateAuthority)에서만 붙고, CharacterBase.FixedUpdateNetwork 가
    /// 매 틱 BuildInput() 을 호출한다.
    /// </summary>
    public abstract class CharacterBotBrain : MonoBehaviour, ICharacterInputSource
    {
        /// <summary>캐릭터 종류에 맞는 두뇌를 붙인다. 아직 AI 가 없는 캐릭터면 null.</summary>
        public static CharacterBotBrain AttachTo(CharacterBase character, BotDifficulty difficulty)
        {
            CharacterBotBrain brain = character switch
            {
                GunnerCharacter _ => character.gameObject.AddComponent<GunnerBotBrain>(),
                _ => null
            };

            if (brain == null)
            {
                Debug.LogWarning($"[Bot] {character.name} 는 아직 AI 가 없습니다.");
                return null;
            }

            brain.Initialize(character, difficulty);
            return brain;
        }

        // ---- 난이도별 수치 ----
        protected float ReactionTime { get; private set; }
        protected float AimErrorDegrees { get; private set; }
        protected float DodgeChance { get; private set; }
        /// <summary>기본공격 사이에 추가로 쉬는 시간 범위(사람처럼 연타 간격이 들쭉날쭉하게).</summary>
        protected Vector2 ShotDelayRange { get; private set; }
        /// <summary>한 번에 몰아 쏘는 발 수 범위(x~y). 다 쏘면 BurstPauseRange 만큼 쉰다 —
        /// 쉬지 않고 계속 맞히면 상대가 피격 경직에 계속 묶여서 "멈춘 것처럼" 느껴진다.</summary>
        protected Vector2Int BurstSizeRange { get; private set; }
        protected Vector2 BurstPauseRange { get; private set; }
        /// <summary>스킬을 쓸 수 있는 상황이 와도 바로 안 쓰고 망설이는 확률(매 판단마다).</summary>
        protected float SkillHesitation { get; private set; }

        protected CharacterBase Self { get; private set; }
        protected CharacterBase Target { get; private set; }
        protected float Clock { get; private set; }
        /// <summary>이번 전투(잠금이 풀린 뒤) 경과 시간. 라운드 시작 직후 궁극기 남발 방지 등에 쓴다.</summary>
        protected float FightTime => Clock - wakeUpAt;
        protected Collider2D SelfCollider { get; private set; }

        private struct TargetSample
        {
            public float Time;
            public Vector2 Position;
            public Vector2 Velocity;
        }

        private readonly List<TargetSample> targetHistory = new List<TargetSample>();
        private Collider2D targetCollider;

        private float augmentPickAt = -1f;
        private float wakeUpAt;

        private float jumpHoldUntil;
        private bool jumpQueued;

        private float stuckTimer;
        private float lastX;

        // 투사체 회피용: 투사체별 직전 위치로 속도를 추정한다.
        private readonly Dictionary<Component, Vector3> projectileLastPositions = new Dictionary<Component, Vector3>(); // xy = 위치, z = 기록 시각
        private readonly HashSet<Component> judgedProjectiles = new HashSet<Component>();
        private readonly List<Component> staleKeys = new List<Component>();
        private CharacterProjectile[] cachedProjectiles = new CharacterProjectile[0];
        private GunnerGrenadeProjectile[] cachedGrenades = new GunnerGrenadeProjectile[0];
        private float nextHazardScanTime;

        protected virtual void Initialize(CharacterBase self, BotDifficulty difficulty)
        {
            Self = self;
            SelfCollider = self.GetComponent<Collider2D>();
            ApplyDifficulty(difficulty);
            self.SetExternalInputSource(this);
        }

        protected virtual void OnDestroy()
        {
            if (Self != null)
                Self.SetExternalInputSource(null);
        }

        private void ApplyDifficulty(BotDifficulty difficulty)
        {
            switch (difficulty)
            {
                case BotDifficulty.Easy:
                    ReactionTime = 0.35f;
                    AimErrorDegrees = 9f;
                    DodgeChance = 0.2f;
                    ShotDelayRange = new Vector2(0.35f, 0.7f);
                    BurstSizeRange = new Vector2Int(1, 2);
                    BurstPauseRange = new Vector2(1f, 1.8f);
                    SkillHesitation = 0.6f;
                    break;
                case BotDifficulty.Hard:
                    ReactionTime = 0.12f;
                    AimErrorDegrees = 2f;
                    DodgeChance = 0.7f;
                    ShotDelayRange = new Vector2(0.08f, 0.2f);
                    BurstSizeRange = new Vector2Int(3, 4);
                    BurstPauseRange = new Vector2(0.4f, 0.8f);
                    SkillHesitation = 0.1f;
                    break;
                default:
                    ReactionTime = 0.22f;
                    AimErrorDegrees = 5f;
                    DodgeChance = 0.45f;
                    ShotDelayRange = new Vector2(0.18f, 0.35f);
                    BurstSizeRange = new Vector2Int(2, 3);
                    BurstPauseRange = new Vector2(0.7f, 1.3f);
                    SkillHesitation = 0.3f;
                    break;
            }
        }

        public CharacterInputSnapshot BuildInput(CharacterBase self, float deltaTime)
        {
            Clock += deltaTime;
            TickAugmentPicks();

            CharacterInputSnapshot input = default;
            input.AimWorldPosition = SelfCenter + new Vector2(Self.FacingDirection, 0f);

            RefreshTarget();
            if (Self.IsDead || Target == null)
            {
                targetHistory.Clear();
                return input;
            }

            // VS 연출/라운드 전환 등으로 나나 상대가 잠겨 있으면 가만히 있는다. 잠금이 풀려도
            // 바로 튀어나가지 않고 사람처럼 잠깐(반응 시간 + 0.4~0.8초) 뒤에 움직이기 시작한다.
            if (Self.IsControlLockedByGame || Target.IsControlLockedByGame)
            {
                wakeUpAt = Clock + ReactionTime + Random.Range(0.4f, 0.8f);
                targetHistory.Clear();
                return input;
            }
            if (Clock < wakeUpAt)
                return input;

            RecordTargetSample();
            ScanHazards();

            Think(ref input, deltaTime);
            ApplyJump(ref input);
            TrackStuck(input.MoveDirection, deltaTime);
            return input;
        }

        /// <summary>캐릭터별 판단. input.MoveDirection / AimWorldPosition / 각종 Pressed 를 채운다.</summary>
        protected abstract void Think(ref CharacterInputSnapshot input, float deltaTime);

        // ================= 인지 =================

        private void RefreshTarget()
        {
            if (Target != null && Target.Object != null && !Target.IsDead)
                return;

            Target = CharacterBase.All.Find(c =>
                c != null && c != Self && c.Object != null && !c.IsDead && c.DamageTeamId != Self.DamageTeamId);
            targetCollider = Target != null ? Target.GetComponent<Collider2D>() : null;
            targetHistory.Clear();
        }

        private void RecordTargetSample()
        {
            targetHistory.Add(new TargetSample
            {
                Time = Clock,
                Position = ActualTargetCenter,
                Velocity = Target.Velocity
            });

            // 반응 시간보다 조금 더 오래된 샘플까지만 남긴다.
            while (targetHistory.Count > 2 && targetHistory[1].Time < Clock - ReactionTime)
                targetHistory.RemoveAt(0);
        }

        private TargetSample PerceivedSample
        {
            get
            {
                if (targetHistory.Count == 0)
                    return new TargetSample { Position = ActualTargetCenter, Velocity = Target.Velocity };
                return targetHistory[0];
            }
        }

        protected Vector2 SelfCenter => SelfCollider != null ? (Vector2)SelfCollider.bounds.center : (Vector2)Self.transform.position;
        protected float SelfFeetY => SelfCollider != null ? SelfCollider.bounds.min.y : Self.transform.position.y;
        protected float SelfHalfWidth => SelfCollider != null ? SelfCollider.bounds.extents.x : 0.4f;

        private Vector2 ActualTargetCenter => targetCollider != null ? (Vector2)targetCollider.bounds.center : (Vector2)Target.transform.position;

        /// <summary>반응 시간만큼 늦게 본 상대 위치(현재 위치를 바로 알면 사람보다 너무 잘 맞힌다).</summary>
        protected Vector2 PerceivedTargetPosition => PerceivedSample.Position;
        protected Vector2 PerceivedTargetVelocity => PerceivedSample.Velocity;

        protected LayerMask GroundMask => Self.Definition != null ? Self.Definition.GroundLayer : (LayerMask)Physics2D.DefaultRaycastLayers;

        protected bool HasLineOfSight(Vector2 from, Vector2 to)
        {
            return !Physics2D.Linecast(from, to, GroundMask);
        }

        protected bool IsReady(CharacterActionType action)
        {
            return Self.Cooldowns != null && Self.Cooldowns.CanUse(action);
        }

        protected bool Roll(float chance) => Random.value < chance;

        // ================= 이동 =================

        /// <summary>그 방향으로 걸어가도 안전한지(발밑 앞쪽에 땅이 있는지).</summary>
        protected bool IsGroundAhead(float direction, float lookAhead = 0.9f, float maxDrop = 3.5f)
        {
            if (Mathf.Approximately(direction, 0f))
                return true;

            Vector2 origin = new Vector2(SelfCenter.x + Mathf.Sign(direction) * (SelfHalfWidth + lookAhead), SelfFeetY + 0.2f);
            return Physics2D.Raycast(origin, Vector2.down, maxDrop, GroundMask);
        }

        protected bool IsWallAhead(float direction, float distance = 0.5f)
        {
            if (Mathf.Approximately(direction, 0f))
                return false;

            Vector2 origin = new Vector2(SelfCenter.x, SelfFeetY + 0.35f);
            return Physics2D.Raycast(origin, new Vector2(Mathf.Sign(direction), 0f), SelfHalfWidth + distance, GroundMask);
        }

        /// <summary>원하는 이동 방향을 받아, 낭떠러지면 멈추고 벽이면 점프를 예약한 뒤 실제 이동값을 돌려준다.</summary>
        protected float SafeMove(float desired)
        {
            if (Mathf.Approximately(desired, 0f))
                return 0f;

            float dir = Mathf.Sign(desired);
            if (!IsGroundAhead(dir))
                return 0f;

            if (Self.IsGrounded && IsWallAhead(dir))
                RequestJump(0.25f);

            return dir;
        }

        /// <summary>점프 요청. holdSeconds 동안 점프키를 누르고 있는 것으로 처리(길게 누르면 더 높이).</summary>
        protected void RequestJump(float holdSeconds = 0.2f)
        {
            if (!Self.IsGrounded || jumpHoldUntil > Clock)
                return;

            jumpQueued = true;
            jumpHoldUntil = Clock + holdSeconds;
        }

        private void ApplyJump(ref CharacterInputSnapshot input)
        {
            if (jumpQueued)
            {
                input.JumpPressed = true;
                jumpQueued = false;
            }
            input.JumpHeld = input.JumpHeld || Clock < jumpHoldUntil;
        }

        // 가려는데 제자리면(턱에 걸림 등) 점프로 빠져나온다.
        private void TrackStuck(float move, float deltaTime)
        {
            float x = Self.transform.position.x;
            if (!Mathf.Approximately(move, 0f) && Self.IsGrounded && Mathf.Abs(x - lastX) < 0.002f)
            {
                stuckTimer += deltaTime;
                if (stuckTimer > 0.4f)
                {
                    RequestJump(0.3f);
                    stuckTimer = 0f;
                }
            }
            else
            {
                stuckTimer = 0f;
            }
            lastX = x;
        }

        // ================= 회피 =================

        private void ScanHazards()
        {
            if (Clock < nextHazardScanTime)
                return;

            nextHazardScanTime = Clock + 0.1f;
            cachedProjectiles = FindObjectsByType<CharacterProjectile>(FindObjectsSortMode.None);
            cachedGrenades = FindObjectsByType<GunnerGrenadeProjectile>(FindObjectsSortMode.None);
        }

        /// <summary>곧 맞을 것 같은 적 투사체가 있는지. 투사체마다 한 번만 "피할지"를 굴린다
        /// (난이도의 DodgeChance). 피하기로 한 투사체가 있으면 true.</summary>
        protected bool ShouldDodgeProjectile()
        {
            bool dodge = false;
            Vector2 me = SelfCenter;

            foreach (CharacterProjectile projectile in cachedProjectiles)
            {
                if (projectile == null || projectile.Object == null || projectile.Owner == Self.DamageOwner)
                    continue;

                Vector2 pos = projectile.transform.position;
                bool seenBefore = projectileLastPositions.TryGetValue(projectile, out Vector3 last);
                projectileLastPositions[projectile] = new Vector3(pos.x, pos.y, Clock);
                float elapsed = Clock - last.z;
                if (!seenBefore || elapsed <= 0.0001f)
                    continue;

                Vector2 velocity = (pos - (Vector2)last) / elapsed;
                if (Mathf.Abs(velocity.x) < 1f || judgedProjectiles.Contains(projectile))
                    continue;

                float timeToReach = (me.x - pos.x) / velocity.x;
                if (timeToReach <= 0f || timeToReach > 0.4f)
                    continue;

                float yAtArrival = pos.y + velocity.y * timeToReach;
                if (Mathf.Abs(yAtArrival - me.y) > 0.9f)
                    continue;

                judgedProjectiles.Add(projectile);
                if (Roll(DodgeChance))
                    dodge = true;
            }

            PruneProjectileMemory();
            return dodge;
        }

        private void PruneProjectileMemory()
        {
            staleKeys.Clear();
            foreach (Component key in projectileLastPositions.Keys)
            {
                if (key == null)
                    staleKeys.Add(key);
            }
            foreach (Component key in staleKeys)
            {
                projectileLastPositions.Remove(key);
                judgedProjectiles.Remove(key);
            }
        }

        /// <summary>근처에 적 수류탄이 있으면 그 반대 방향(-1/1), 없으면 0.</summary>
        protected float EnemyGrenadeEscapeDirection(float dangerRadius = 2.8f)
        {
            Vector2 me = SelfCenter;
            foreach (GunnerGrenadeProjectile grenade in cachedGrenades)
            {
                if (grenade == null || grenade.Object == null || grenade.Owner == Self.DamageOwner)
                    continue;

                Vector2 pos = grenade.transform.position;
                if ((pos - me).sqrMagnitude < dangerRadius * dangerRadius)
                    return me.x >= pos.x ? 1f : -1f;
            }
            return 0f;
        }

        // ================= 조준 =================

        /// <summary>등속 투사체로 움직이는 대상을 맞히기 위한 예측 조준점(몇 번 반복해서 근사).</summary>
        protected static Vector2 PredictIntercept(Vector2 shooter, Vector2 targetPos, Vector2 targetVel, float projectileSpeed, float maxLeadTime = 1.2f)
        {
            if (projectileSpeed <= 0.01f)
                return targetPos;

            Vector2 predicted = targetPos;
            for (int i = 0; i < 3; i++)
            {
                float t = Mathf.Min(Vector2.Distance(shooter, predicted) / projectileSpeed, maxLeadTime);
                predicted = targetPos + targetVel * t;
            }
            return predicted;
        }

        /// <summary>조준점을 사수 기준으로 랜덤하게 살짝 틀어준다(난이도의 조준 오차).</summary>
        protected Vector2 ApplyAimError(Vector2 shooter, Vector2 aimPoint, float multiplier = 1f)
        {
            float error = (Random.value + Random.value - 1f) * AimErrorDegrees * multiplier;
            Vector2 offset = aimPoint - shooter;
            float rad = error * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return shooter + new Vector2(offset.x * cos - offset.y * sin, offset.x * sin + offset.y * cos);
        }

        /// <summary>포물선 투척: speed 로 던져서 target 에 닿는 낮은 궤도 방향을 구한다. 사거리 밖이면 false.</summary>
        protected static bool SolveBallisticDirection(Vector2 from, Vector2 to, float speed, float gravity, out Vector2 direction, out float flightTime)
        {
            direction = Vector2.zero;
            flightTime = 0f;

            float dx = to.x - from.x;
            float dy = to.y - from.y;
            float absDx = Mathf.Abs(dx);
            if (absDx < 0.05f || gravity <= 0.001f)
                return false;

            float v2 = speed * speed;
            float discriminant = v2 * v2 - gravity * (gravity * absDx * absDx + 2f * dy * v2);
            if (discriminant < 0f)
                return false;

            float angle = Mathf.Atan2(v2 - Mathf.Sqrt(discriminant), gravity * absDx);
            direction = new Vector2(Mathf.Sign(dx) * Mathf.Cos(angle), Mathf.Sin(angle));
            flightTime = absDx / Mathf.Max(speed * Mathf.Cos(angle), 0.01f);
            return true;
        }

        // ================= 증강 =================

        // 라운드 사이 증강 선택: 1.5~3초 고민하는 척하다가, 아직 최대 스택이 아닌 것 중 랜덤으로 고른다.
        private void TickAugmentPicks()
        {
            if (Self.AugmentPicksRemaining <= 0)
            {
                augmentPickAt = -1f;
                return;
            }

            if (augmentPickAt < 0f)
            {
                augmentPickAt = Clock + Random.Range(1.5f, 3f);
                return;
            }

            if (Clock < augmentPickAt)
                return;

            List<AugmentPoolEntry> candidates = Self.GetAugmentPool().FindAll(entry =>
                entry.Data != null && Self.GetAugmentStack(entry.Data.type) < Mathf.Max(1, entry.Data.maxStack));

            if (candidates.Count == 0)
            {
                Self.AutoFinishAugmentPicks();
                return;
            }

            Self.GrantAugment(candidates[Random.Range(0, candidates.Count)].Data.type);
            augmentPickAt = Clock + Random.Range(0.4f, 1f);
        }
    }
}
