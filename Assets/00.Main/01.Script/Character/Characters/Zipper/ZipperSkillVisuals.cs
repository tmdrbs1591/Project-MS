using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace ProjectMS.CharacterSystem.Examples
{
    /// <summary>
    /// 지퍼 스킬 연출 — 모든 클라에서 각자 그린다(ZipperCharacter의 RPC만 받음, 게임 진행에 영향 없음).
    /// 이펙트는 전부 인스펙터에 꽂은 프리팹(Epic Toon FX 등)을 띄운다.
    ///   - 기본기: 휘두를 때 손에서 참격 섬광.
    ///   - Q(다이브): 출발점 폭발 + 돌진하는 동안 몸을 따라오는 궤적 + 도착점 폭발.
    ///   - 궁(위상 돌파): 착지 지점에 마법진 표시 → 출발점에서 사라지고 도착점에서 터지며 화면이 흔들린다.
    ///   - 패시브(차원 표식): 표식이 걸린 적 머리 위에 표식, 기본기로 터뜨리면 폭발.
    /// </summary>
    [RequireComponent(typeof(ZipperCharacter))]
    public sealed class ZipperSkillVisuals : MonoBehaviour
    {
        [Header("기본기 - 슬래시")]
        [SerializeField] private SkillFxSlot[] slashCastFx;

        [Header("Q - 다이브")]
        [SerializeField] private SkillFxSlot[] diveStartFx;
        [Tooltip("돌진하는 동안 지퍼를 따라다니는 이펙트.")]
        [SerializeField] private SkillFxSlot[] diveTrailFx;
        [SerializeField] private SkillFxSlot[] diveEndFx;
        [Tooltip("다이브 효과음. (Q는 충전 방식이라 일반 액션 효과음 경로를 안 타서 여기서 낸다.)")]
        [SerializeField] private AudioClip diveClip;
        [Range(0f, 1f)] [SerializeField] private float diveVolume = 0.9f;

        [Header("궁 - 위상 돌파")]
        [Tooltip("시전하자마자 착지 지점에 뜨는 표시(이동 대기 시간 동안 유지).")]
        [SerializeField] private SkillFxSlot[] phaseMarkerFx;
        [Tooltip("사라지는 출발 지점 이펙트.")]
        [SerializeField] private SkillFxSlot[] phaseDepartFx;
        [Tooltip("나타나며 피해를 주는 도착 지점 이펙트.")]
        [SerializeField] private SkillFxSlot[] phaseArriveFx;
        [SerializeField] private float phaseArriveShake = 0.55f;

        [Header("패시브 - 차원 표식")]
        [Tooltip("표식이 걸린 적을 따라다니는 이펙트(offset = 대상 기준 위치).")]
        [SerializeField] private SkillFxSlot markFx;
        [SerializeField] private SkillFxSlot[] markConsumeFx;

        private ZipperCharacter zipper;
        private readonly List<GameObject> diveTrails = new List<GameObject>();
        private float diveEndAt = -1f;

        private sealed class Mark
        {
            public CharacterBase Target;
            public GameObject Instance;
            public float ExpireAt;
        }

        private readonly List<Mark> marks = new List<Mark>();
        private readonly List<GameObject> phaseMarkers = new List<GameObject>();

        private void Awake() => zipper = GetComponent<ZipperCharacter>();

        private void OnDisable()
        {
            StopDiveTrail(false);
            foreach (Mark m in marks)
                if (m.Instance != null)
                    Destroy(m.Instance);
            marks.Clear();
            ClearPhaseMarkers();
        }

        private void LateUpdate()
        {
            Vector3 at = transform.position;
            at.z = 0f;

            if (diveTrails.Count > 0)
            {
                foreach (GameObject go in diveTrails)
                    if (go != null)
                        go.transform.position = at;
                if (Time.time >= diveEndAt)
                    StopDiveTrail(true);
            }

            for (int i = marks.Count - 1; i >= 0; i--)
            {
                Mark m = marks[i];
                bool gone = m.Target == null || m.Target.Object == null || !m.Target.Object.IsValid || m.Target.IsDead;
                if (gone || Time.time >= m.ExpireAt)
                {
                    if (m.Instance != null)
                        EffectAutoDestroy.Schedule(m.Instance, 0f);
                    marks.RemoveAt(i);
                    continue;
                }
                if (m.Instance != null)
                    m.Instance.transform.position = m.Target.transform.position + (Vector3)markFx.offset;
            }
        }

        // ───────── 기본기 ─────────

        public void PlaySlash(Vector2 origin, float angle)
        {
            SkillFxSlot.SpawnAll(slashCastFx, origin, angle);
        }

        // ───────── Q ─────────

        public void PlayDiveStart(Vector2 origin, float angle, float duration)
        {
            SkillFxSlot.SpawnAll(diveStartFx, origin, angle);
            SoundManager.Instance?.PlaySfx(diveClip, diveVolume, 0.05f);
            StopDiveTrail(false);
            if (diveTrailFx != null)
                foreach (SkillFxSlot slot in diveTrailFx)
                {
                    GameObject go = slot?.Spawn(transform.position, angle);
                    if (go == null)
                        continue;
                    Destroy(go.GetComponent<EffectAutoDestroy>());
                    diveTrails.Add(go);
                }
            diveEndAt = Time.time + Mathf.Max(0.05f, duration);
            if (zipper != null && zipper.IsLocalPlayer && TwoPlayerCamera.Instance != null)
            {
                float rad = angle * Mathf.Deg2Rad;
                TwoPlayerCamera.Instance.PunchZoom(new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)), 0.4f, 0.03f, 0.22f);
            }
        }

        private void StopDiveTrail(bool playEnd)
        {
            foreach (GameObject go in diveTrails)
                if (go != null)
                    EffectAutoDestroy.Schedule(go, 0f);
            diveTrails.Clear();
            if (playEnd)
                SkillFxSlot.SpawnAll(diveEndFx, transform.position, 0f);
            diveEndAt = -1f;
        }

        // ───────── 궁 ─────────

        public void PlayPhaseMarker(Vector2 target, float delay)
        {
            ClearPhaseMarkers();
            if (phaseMarkerFx == null)
                return;
            foreach (SkillFxSlot slot in phaseMarkerFx)
            {
                GameObject go = slot?.Spawn(target, 0f);
                if (go == null)
                    continue;
                // 이동 대기 시간 동안만 보이고, 도착하는 순간 사라지기 시작한다.
                EffectAutoDestroy.Schedule(go, delay);
                phaseMarkers.Add(go);
            }
        }

        public void PlayPhaseArrive(Vector2 from, Vector2 to)
        {
            ClearPhaseMarkers();
            SkillFxSlot.SpawnAll(phaseDepartFx, from, 0f);
            SkillFxSlot.SpawnAll(phaseArriveFx, to, 0f);

            TwoPlayerCamera.Instance?.Shake(phaseArriveShake, 0.3f);
            if (zipper != null && zipper.IsLocalPlayer)
                ScreenFlash.Play(new Color(0.85f, 0.8f, 1f), 0.14f, 0.02f, 0.2f);
        }

        private void ClearPhaseMarkers()
        {
            foreach (GameObject go in phaseMarkers)
                if (go != null)
                    EffectAutoDestroy.Schedule(go, 0f);
            phaseMarkers.Clear();
        }

        // ───────── 패시브 ─────────

        public void PlayMarkApplied(NetworkId targetId, float duration)
        {
            CharacterBase target = Resolve(targetId);
            if (target == null)
                return;

            foreach (Mark m in marks)
            {
                if (m.Target != target)
                    continue;
                m.ExpireAt = Time.time + duration;   // 갱신
                return;
            }

            GameObject go = markFx != null ? markFx.Spawn(target.transform.position, 0f) : null;
            if (go != null)
                Destroy(go.GetComponent<EffectAutoDestroy>());
            marks.Add(new Mark { Target = target, Instance = go, ExpireAt = Time.time + duration });
        }

        public void PlayMarkConsumed(NetworkId targetId)
        {
            CharacterBase target = Resolve(targetId);
            for (int i = marks.Count - 1; i >= 0; i--)
            {
                if (marks[i].Target != target)
                    continue;
                if (marks[i].Instance != null)
                    Destroy(marks[i].Instance);
                marks.RemoveAt(i);
            }
            if (target != null)
                SkillFxSlot.SpawnAll(markConsumeFx, target.transform.position + Vector3.up * 0.3f, 0f);
            if (zipper != null && zipper.IsLocalPlayer)
                TwoPlayerCamera.Instance?.Shake(0.25f, 0.15f);
        }

        private CharacterBase Resolve(NetworkId id)
        {
            if (zipper == null || zipper.Runner == null || !id.IsValid)
                return null;
            return zipper.Runner.TryFindObject(id, out NetworkObject obj) ? obj.GetComponent<CharacterBase>() : null;
        }
    }
}
