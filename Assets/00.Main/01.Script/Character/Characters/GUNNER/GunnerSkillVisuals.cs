using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectMS.CharacterSystem.Examples
{
    /// <summary>
    /// 거너 스킬 손맛 연출 — 모든 클라에서 각자 그린다(GunnerCharacter의 RPC/네트워크 상태만 읽음, 게임 진행에 영향 없음).
    /// 이펙트는 전부 인스펙터에 꽂은 프리팹(Epic Toon FX 등)을 띄운다.
    ///   - Q: 총구 섬광 → 레이저 탄이 판정 끝까지 순식간에 날아감 → 끝점 폭발, 총 반동.
    ///   - E: 총구 연기 퍼프, 총 반동.
    ///   - 궁: 기 모으는 동안 바주카로 바꿔 들고 총구에 기 모이는 이펙트(점점 커지고 떨림).
    ///     다 모이면 앞쪽 발사 화염 + 뒤쪽 후폭풍 + 화면 번쩍/흔들림, 잠깐 바주카를 든 채 크게 밀려난다.
    /// </summary>
    [RequireComponent(typeof(GunnerCharacter))]
    public sealed class GunnerSkillVisuals : MonoBehaviour
    {
        [Header("무기")]
        [Tooltip("총 스프라이트(Arm Root/R/R). 반동/떨림/바주카 교체에 쓴다.")]
        [SerializeField] private SpriteRenderer weaponRenderer;
        [Tooltip("궁 기 모을 때 바꿔 들 바주카 그림. 비우면 총 그대로 쓴다.")]
        [SerializeField] private Sprite bazookaSprite;
        [Tooltip("바주카 가로 길이 = 총 가로 길이 × 이 값.")]
        [Min(0.1f)] [SerializeField] private float bazookaSizeRatio = 1.8f;
        [Tooltip("쏘고 나서 바주카를 들고 있는 시간(초).")]
        [Min(0f)] [SerializeField] private float bazookaHoldAfterFire = 0.45f;

        [Header("Q - 피어싱 라이트")]
        [SerializeField] private SkillFxSlot[] piercingMuzzleFx;
        [Tooltip("총구에서 판정 끝까지 날려 보낼 탄(광선처럼 보이게 아주 빠르게).")]
        [SerializeField] private SkillFxSlot piercingBoltFx;
        [Min(0.01f)] [SerializeField] private float piercingBoltTravelTime = 0.07f;
        [SerializeField] private SkillFxSlot[] piercingEndFx;
        [SerializeField] private float piercingKick = 0.32f;

        [Header("E - 수류탄")]
        [SerializeField] private SkillFxSlot[] grenadeThrowFx;
        [SerializeField] private float grenadeKick = 0.22f;

        [Header("궁 - 기 모으기")]
        [Tooltip("기 모으는 동안 총구를 따라다니는 이펙트(반복 파티클 권장). 모일수록 커진다.")]
        [SerializeField] private SkillFxSlot[] chargeFx;
        [Tooltip("다 모였을 때 크기 배율(시작은 1).")]
        [Min(1f)] [SerializeField] private float chargeGrowth = 1.8f;
        [SerializeField] private AudioClip chargeClip;
        [Range(0f, 2f)] [SerializeField] private float chargeVolume = 0.9f;

        [Header("궁 - 발사")]
        [Tooltip("총구 앞으로 터지는 발사 화염.")]
        [SerializeField] private SkillFxSlot[] rocketFireFx;
        [Tooltip("바주카 뒤쪽으로 뿜는 후폭풍(바주카 길이만큼 뒤에서 터진다).")]
        [SerializeField] private SkillFxSlot[] rocketBackblastFx;
        [Tooltip("로켓이 실제로 나가는 순간의 소리.")]
        [SerializeField] private AudioClip rocketFireClip;
        [Range(0f, 2f)] [SerializeField] private float rocketFireVolume = 1f;
        [SerializeField] private float rocketKick = 0.55f;

        private GunnerCharacter gunner;

        private Sprite baseSprite;
        private Vector3 baseWeaponLocalPos;
        private Vector3 baseWeaponScale;
        private bool bazookaActive;
        private float bazookaUntil;
        private float weaponPopTime = -10f;

        private Vector2 kickOffset;
        private float trembleAmount;

        private bool wasCharging;
        private readonly List<GameObject> chargeInstances = new List<GameObject>();
        private readonly List<Vector3> chargeBaseScales = new List<Vector3>();

        private void Awake()
        {
            gunner = GetComponent<GunnerCharacter>();
            if (weaponRenderer != null)
            {
                baseSprite = weaponRenderer.sprite;
                baseWeaponLocalPos = weaponRenderer.transform.localPosition;
                baseWeaponScale = weaponRenderer.transform.localScale;
            }
        }

        private void OnDisable()
        {
            StopCharge();
            wasCharging = false;
            kickOffset = Vector2.zero;
            SetBazooka(false, false);
            weaponPopTime = -10f;
            ApplyWeaponPose();
        }

        private void LateUpdate()
        {
            bool charging = gunner != null && gunner.Object != null && gunner.Object.IsValid &&
                            gunner.IsChargingUltimate && !gunner.IsDead;

            if (charging)
                UpdateCharge();
            else if (wasCharging)
                StopCharge();
            wasCharging = charging;

            if (bazookaActive && !charging && Time.time >= bazookaUntil)
                SetBazooka(false, true);

            kickOffset = Vector2.Lerp(kickOffset, Vector2.zero, 1f - Mathf.Exp(-16f * Time.deltaTime));
            ApplyWeaponPose();
        }

        // ───────── Q ─────────

        public void PlayPiercingLight(Vector2 origin, float angle, float length)
        {
            Vector2 dir = Dir(angle);
            Vector3 end = origin + dir * length;

            SkillFxSlot.SpawnAll(piercingMuzzleFx, origin, angle);
            if (piercingBoltFx != null && piercingBoltFx.prefab != null)
                StartCoroutine(FlyBolt(origin, end, angle));
            else
                SkillFxSlot.SpawnAll(piercingEndFx, end, angle);

            Kick(dir, piercingKick);
            if (IsLocal)
                ScreenFlash.Play(new Color(1f, 0.85f, 0.7f), 0.08f, 0.02f, 0.12f);
        }

        private IEnumerator FlyBolt(Vector3 from, Vector3 to, float angle)
        {
            GameObject bolt = piercingBoltFx.Spawn(from, angle);
            for (float t = 0f; t < piercingBoltTravelTime && bolt != null; t += Time.deltaTime)
            {
                bolt.transform.position = Vector3.Lerp(from, to, t / piercingBoltTravelTime);
                yield return null;
            }
            if (bolt != null)
            {
                bolt.transform.position = to;
                EffectAutoDestroy.Schedule(bolt, 0f);
            }
            SkillFxSlot.SpawnAll(piercingEndFx, to, angle);
        }

        // ───────── E ─────────

        public void PlayGrenadeThrow(Vector2 origin, float angle)
        {
            SkillFxSlot.SpawnAll(grenadeThrowFx, origin, angle);
            Kick(Dir(angle), grenadeKick);
        }

        // ───────── 궁 ─────────

        private void UpdateCharge()
        {
            Transform muzzle = gunner.Muzzle;
            if (muzzle == null)
                return;

            if (!wasCharging)
                BeginCharge(muzzle);

            float r = gunner.UltimateChargeRatio;
            Vector3 at = muzzle.position;
            at.z = 0f;
            float growth = Mathf.Lerp(1f, chargeGrowth, r * r);
            for (int i = 0; i < chargeInstances.Count; i++)
            {
                GameObject go = chargeInstances[i];
                if (go == null)
                    continue;
                Vector2 offset = chargeFx != null && i < chargeFx.Length ? chargeFx[i].offset : Vector2.zero;
                go.transform.position = at + (Vector3)offset;
                go.transform.localScale = chargeBaseScales[i] * growth;
            }

            trembleAmount = Mathf.Lerp(0.015f, 0.07f, r);
            if (IsLocal)
                TwoPlayerCamera.Instance?.Shake(Mathf.Lerp(0.03f, 0.14f, r * r), 0.08f);
        }

        private void BeginCharge(Transform muzzle)
        {
            SetBazooka(true, true);
            SoundManager.Instance?.PlaySfx(chargeClip, chargeVolume, 0f);

            StopCharge();
            if (chargeFx == null)
                return;
            foreach (SkillFxSlot slot in chargeFx)
            {
                // 수명은 기 모으기가 끝날 때 StopCharge가 정한다(0 = 자동 삭제 안 함).
                GameObject go = slot?.Spawn(muzzle.position, 0f);
                if (go != null)
                {
                    if (slot.lifetime > 0f)
                        Destroy(go.GetComponent<EffectAutoDestroy>());
                    // 루트 크기를 키우면 파티클도 같이 커지게(UpdateCharge의 성장 연출)
                    foreach (ParticleSystem ps in go.GetComponentsInChildren<ParticleSystem>(true))
                    {
                        ParticleSystem.MainModule main = ps.main;
                        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                    }
                }
                chargeInstances.Add(go);
                chargeBaseScales.Add(go != null ? go.transform.localScale : Vector3.one);
            }
        }

        private void StopCharge()
        {
            trembleAmount = 0f;
            // 뚝 끊지 않고, 새로 뿜는 것만 멈춘 뒤 남은 파티클이 사라지면 지운다.
            foreach (GameObject go in chargeInstances)
                if (go != null)
                    EffectAutoDestroy.Schedule(go, 0f);
            chargeInstances.Clear();
            chargeBaseScales.Clear();
        }

        public void PlayRocketLaunch(Vector2 origin, float angle)
        {
            Vector2 dir = Dir(angle);

            SetBazooka(true, false);
            bazookaUntil = Time.time + bazookaHoldAfterFire;

            SkillFxSlot.SpawnAll(rocketFireFx, origin, angle);
            Vector3 back = origin - dir * WeaponLength();
            SkillFxSlot.SpawnAll(rocketBackblastFx, back, angle);

            Kick(dir, rocketKick);
            SoundManager.Instance?.PlaySfx(rocketFireClip, rocketFireVolume, 0f);

            TwoPlayerCamera cam = TwoPlayerCamera.Instance;
            if (IsLocal)
            {
                ScreenFlash.Play(new Color(1f, 0.85f, 0.6f), 0.18f, 0.03f, 0.22f);
                if (cam != null)
                {
                    cam.Shake(0.6f, 0.32f);
                    cam.PunchZoom(-dir, 0.75f, 0.06f, 0.32f);
                }
            }
            else
            {
                ScreenFlash.Play(new Color(1f, 0.85f, 0.6f), 0.08f, 0.02f, 0.18f);
                cam?.Shake(0.25f, 0.2f);
            }
        }

        // ───────── 무기 ─────────

        private void Kick(Vector2 aimDir, float amount)
        {
            kickOffset = -aimDir * amount;
        }

        private void SetBazooka(bool on, bool pop)
        {
            if (weaponRenderer == null || bazookaSprite == null || bazookaActive == on)
                return;
            bazookaActive = on;
            weaponRenderer.sprite = on ? bazookaSprite : baseSprite;
            if (pop)
                weaponPopTime = Time.time;
        }

        private void ApplyWeaponPose()
        {
            if (weaponRenderer == null)
                return;
            Transform t = weaponRenderer.transform;

            float sizeK = 1f;
            Vector3 shift = Vector3.zero;
            if (bazookaActive && baseSprite != null)
            {
                float gunW = baseSprite.bounds.size.x;
                float bazW = bazookaSprite.bounds.size.x;
                sizeK = gunW / bazW * bazookaSizeRatio;
                // 총구는 그대로 두고 뒤(어깨 쪽)로 길어지게 — 총/바주카 모두 그림의 +X(뒤집힌 상태)가 총구 쪽
                shift = new Vector3(-(gunW * baseWeaponScale.x) * (bazookaSizeRatio - 1f) * 0.5f, 0f, 0f);
            }

            float pt = (Time.time - weaponPopTime) / 0.18f;
            float pop = pt >= 0f && pt < 1f ? 1f + 0.35f * (1f - pt) * (1f - pt) : 1f;
            t.localScale = baseWeaponScale * sizeK * pop;

            Vector2 world = kickOffset;
            if (trembleAmount > 0f)
                world += Random.insideUnitCircle * trembleAmount;
            Vector3 local = t.parent != null ? t.parent.InverseTransformVector(world) : (Vector3)world;
            t.localPosition = baseWeaponLocalPos + shift + new Vector3(local.x, local.y, 0f);
        }

        private float WeaponLength()
        {
            return weaponRenderer != null ? Mathf.Max(weaponRenderer.bounds.size.x, weaponRenderer.bounds.size.y) : 1f;
        }

        private bool IsLocal => gunner != null && gunner.IsLocalPlayer;

        private static Vector2 Dir(float angle)
        {
            float rad = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }
    }
}
