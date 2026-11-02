using UnityEngine;

/// <summary>
/// 오브젝트가 켜질 때(이펙트/설치물이 생성될 때) 효과음을 한 번 낸다.
/// 폭발·피격 이펙트 프리팹이나 설치물 프리팹 루트에 붙이면, 그 이펙트가 생길 때마다 소리가 같이 난다.
/// </summary>
public class PlaySoundOnSpawn : MonoBehaviour
{
    [SerializeField] private AudioClip clip;
    [Range(0f, 1f)] [SerializeField] private float volume = 0.9f;
    [Range(0f, 0.5f)] [SerializeField] private float pitchVariance = 0.06f;

    private void OnEnable()
    {
        if (clip != null)
            SoundManager.Instance?.PlaySfx(clip, volume, pitchVariance);
    }
}
