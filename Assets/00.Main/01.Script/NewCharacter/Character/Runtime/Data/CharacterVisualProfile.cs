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
