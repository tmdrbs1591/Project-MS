using UnityEngine;

namespace ProjectMS.CharacterSystem
{
    /// <summary>
    /// 점프/착지/피격/사망/부활처럼 대부분의 캐릭터가 공유하는 단발성 효과음 세트다.
    /// CharacterVisualProfile(스쿼시/스트레치 등 수치 튜닝)과 같은 패턴 — 에셋 하나를 여러
    /// 캐릭터 프리팹이 같이 참조하면 되고, 특정 캐릭터만 다른 소리를 쓰고 싶으면 세트를
    /// 하나 더 만들어서 그 프리팹에만 갈아끼우면 된다.
    /// </summary>
    [CreateAssetMenu(menuName = "Project MS/Character/Common Sound Set", fileName = "CharacterCommonSoundSet")]
    public sealed class CharacterCommonSoundSet : ScriptableObject
    {
        [Header("Movement")]
        [SerializeField] private AudioClip jumpClip;
        [SerializeField] private AudioClip landClip;

        [Header("Combat")]
        [SerializeField] private AudioClip damagedClip;
        [Tooltip("같은 소리가 반복돼도 기계적으로 안 들리게 1±이 값 범위에서 피치를 무작위로 살짝 바꾼다.")]
        [Range(0f, 0.5f)] [SerializeField] private float damagedPitchVariance = 0.05f;
        [SerializeField] private AudioClip deathClip;
        [SerializeField] private AudioClip revivedClip;

        public AudioClip JumpClip => jumpClip;
        public AudioClip LandClip => landClip;
        public AudioClip DamagedClip => damagedClip;
        public float DamagedPitchVariance => damagedPitchVariance;
        public AudioClip DeathClip => deathClip;
        public AudioClip RevivedClip => revivedClip;
    }
}
