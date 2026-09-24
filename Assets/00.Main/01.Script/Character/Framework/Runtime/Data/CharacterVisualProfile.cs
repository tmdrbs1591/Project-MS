using UnityEngine;

namespace ProjectMS.CharacterSystem
{
    [CreateAssetMenu(menuName = "Project MS/Character/Visual Profile", fileName = "CharacterVisualProfile")]
    public sealed class CharacterVisualProfile : ScriptableObject
    {
        [Header("Squash And Stretch")]
        [Min(0f)] [SerializeField] private float stiffness = 320f;
        [Min(0f)] [SerializeField] private float damping = 14f;
        [SerializeField] private float jumpStretch = 0.35f;
        [Tooltip("오토홉(이동 중 자동으로 통통 튀는 연출)이 쓰는 스쿼시 세기 배율. 1이면 진짜 점프와 똑같이 늘어난다.\n" +
            "오토홉은 0.1초마다 반복되므로 점프와 같은 세기로 늘리면 걷는 내내 고무처럼 늘어나 보인다.")]
        [Range(0f, 1f)] [SerializeField] private float autoHopStretchScale = 0.3f;
        [SerializeField] private float landSquash = 0.45f;
        [SerializeField] private float airStretchFactor = 0.03f;
        [SerializeField] private float horizontalCompensation = 0.6f;
        [SerializeField] private bool anchorBodyToBottom = true;

        [Header("Idle")]
        [SerializeField] private float idleWobbleAmount = 0.04f;
        [Min(0f)] [SerializeField] private float idleWobbleSpeed = 6f;

        [Header("Hit")]
        [SerializeField] private Color hitFlashColor = new Color(1f, 0.4f, 0.4f, 1f);
        [Min(0f)] [SerializeField] private float hitFlashDuration = 0.12f;
        [SerializeField] private float hitRecoilStretch = 0.25f;

        [Header("Jiggle")]
        [Min(0f)] [SerializeField] private float jiggleStiffness = 180f;
        [Min(0f)] [SerializeField] private float jiggleDamping = 13f;
        [Min(0f)] [SerializeField] private float jiggleMaxOffset = 0.25f;
        [SerializeField] private float jiggleRotation = 25f;

        public float Stiffness => stiffness;
        public float Damping => damping;
        public float JumpStretch => jumpStretch;
        public float AutoHopStretchScale => autoHopStretchScale;
        public float LandSquash => landSquash;
        public float AirStretchFactor => airStretchFactor;
        public float HorizontalCompensation => horizontalCompensation;
        public bool AnchorBodyToBottom => anchorBodyToBottom;
        public float IdleWobbleAmount => idleWobbleAmount;
        public float IdleWobbleSpeed => idleWobbleSpeed;
        public Color HitFlashColor => hitFlashColor;
        public float HitFlashDuration => hitFlashDuration;
        public float HitRecoilStretch => hitRecoilStretch;
        public float JiggleStiffness => jiggleStiffness;
        public float JiggleDamping => jiggleDamping;
        public float JiggleMaxOffset => jiggleMaxOffset;
        public float JiggleRotation => jiggleRotation;
    }
}
