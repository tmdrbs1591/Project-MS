using System.Collections.Generic;
using ProjectMS.CharacterSystem;
using TMPro;
using UnityEngine;

/// <summary>
/// 타격감 연출 모음: 데미지 숫자, 히트스톱, 공격자 쪽 명중 피드백(명중음 + 약한 화면 흔들림).
/// CharacterBase.HitRendered(각 화면이 피격을 렌더할 때 발생)를 받아 로컬에서만 연출한다 —
/// 네트워크 값/물리/Time.timeScale은 전혀 건드리지 않으므로 두 클라이언트의 게임 진행에 영향이 없다.
///
/// [표시 규칙]
///   - 데미지 숫자: 내가 준 피해 = 노란색(크게), 내가 받은 피해 = 빨간색, 도트/낙사 = 작고 연하게.
///   - 히트스톱: 도트/낙사가 아닌 "타격"에만, 맞은 쪽과 때린 쪽 캐릭터의 그림을 잠깐 멈춘다.
///   - 명중 피드백: 내가 때려서 맞혔을 때만 명중음과 약한 화면 흔들림(설정의 흔들림 배율 적용).
///
/// [씬 설정]
///   - 게임 씬에 빈 오브젝트를 만들고 이 스크립트를 붙인다(씬에 하나).
///   - font를 연결하고, 필요하면 hitConfirmClip을 연결한다(비워두면 소리 없이 동작).
/// </summary>
public class HitFeedbackController : MonoBehaviour
{
    [Header("데미지 숫자")]
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private Color dealtColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color takenColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private Color minorColor = new Color(1f, 1f, 1f, 0.75f);
    [Min(0.1f)] [SerializeField] private float numberSize = 4.5f;
    [Tooltip("도트/낙사 피해 숫자 크기 배율.")]
    [Range(0.2f, 1f)] [SerializeField] private float minorSizeScale = 0.6f;
    [Min(0.1f)] [SerializeField] private float numberLifetime = 0.7f;
    [SerializeField] private float riseDistance = 1.2f;
    [Tooltip("캐릭터 위치에서 숫자가 처음 뜨는 높이.")]
    [SerializeField] private float spawnHeight = 1.4f;
    [Tooltip("여러 숫자가 겹치지 않게 좌우로 흩뿌리는 폭.")]
    [SerializeField] private float horizontalJitter = 0.4f;
    [SerializeField] private string sortingLayerName = "UI";
    [SerializeField] private int sortingOrder = 200;

    [Header("히트스톱 (그림만 멈춤)")]
    [Min(0f)] [SerializeField] private float hitStopDuration = 0.05f;
    [Tooltip("피해량이 클수록 추가로 늘어나는 최대 시간.")]
    [Min(0f)] [SerializeField] private float hitStopBonusMax = 0.05f;
    [Tooltip("이 피해량에서 보너스가 최대가 된다.")]
    [Min(1f)] [SerializeField] private float hitStopBonusDamage = 30f;

    [Header("명중 이펙트 (투사체가 아닌 타격)")]
    [Tooltip("범위/즉발 공격처럼 투사체가 아닌 타격에 맞은 캐릭터 위치에 띄울 이펙트. " +
             "투사체는 CharacterProjectile.hitVfxPrefab이 따로 띄우므로 여기서는 제외한다.")]
    [SerializeField] private GameObject impactVfxPrefab;
    [Min(0.1f)] [SerializeField] private float impactVfxLifetime = 1.5f;
    [SerializeField] private float impactVfxHeight = 0.6f;

    [Header("명중 피드백 (내가 맞혔을 때만)")]
    [SerializeField] private AudioClip hitConfirmClip;
    [Range(0f, 1f)] [SerializeField] private float hitConfirmVolume = 0.7f;
    [Min(0f)] [SerializeField] private float hitConfirmShake = 0.05f;
    [Min(0f)] [SerializeField] private float hitConfirmShakeDuration = 0.08f;

    private sealed class Popup
    {
        public TextMeshPro Text;
        public Vector3 Start;
        public float Elapsed;
        public float BaseSize;
        public Color Color;
    }

    private readonly List<Popup> active = new List<Popup>();
    private readonly Stack<TextMeshPro> pool = new Stack<TextMeshPro>();

    private void OnEnable() => CharacterBase.HitRendered += OnHitRendered;

    private void OnDisable()
    {
        CharacterBase.HitRendered -= OnHitRendered;
        foreach (Popup popup in active)
            Release(popup.Text);
        active.Clear();
    }

    private void OnHitRendered(CharacterBase victim, CharacterHitRenderInfo hit)
    {
        if (victim == null)
            return;

        CharacterBase local = CharacterBase.LocalPlayer;
        bool dealtByMe = local != null && hit.Attacker != default && hit.Attacker == local.DamageOwner && victim != local;
        bool takenByMe = victim == local;

        SpawnNumber(victim.transform.position, hit, dealtByMe, takenByMe);

        if (hit.IsNonImpact)
            return;

        if (impactVfxPrefab != null && hit.Source != CharacterDamageSource.Projectile)
        {
            Vector3 at = victim.transform.position + Vector3.up * impactVfxHeight;
            Destroy(Instantiate(impactVfxPrefab, at, Quaternion.identity), impactVfxLifetime);
        }

        float stop = hitStopDuration + hitStopBonusMax * Mathf.Clamp01(hit.Amount / hitStopBonusDamage);
        victim.PlayHitStop(stop);
        CharacterBase attacker = FindAttacker(hit.Attacker);
        if (attacker != null && attacker != victim)
            attacker.PlayHitStop(stop);

        if (dealtByMe)
        {
            SoundManager.Instance?.PlaySfx(hitConfirmClip, hitConfirmVolume, 0.05f);
            if (hitConfirmShake > 0f)
                TwoPlayerCamera.Instance?.Shake(hitConfirmShake, hitConfirmShakeDuration);
        }
    }

    private static CharacterBase FindAttacker(Fusion.PlayerRef attacker)
    {
        if (attacker == default)
            return null;

        foreach (CharacterBase character in CharacterBase.All)
        {
            if (character != null && character.Object != null && character.DamageOwner == attacker)
                return character;
        }

        return null;
    }

    private void SpawnNumber(Vector3 position, CharacterHitRenderInfo hit, bool dealtByMe, bool takenByMe)
    {
        int value = Mathf.Max(1, Mathf.RoundToInt(hit.Amount));
        Color color = hit.IsNonImpact ? minorColor : dealtByMe ? dealtColor : takenByMe ? takenColor : Color.white;
        float size = numberSize * (hit.IsNonImpact ? minorSizeScale : dealtByMe ? 1.15f : 1f);

        TextMeshPro text = Acquire();
        text.text = value.ToString();
        text.color = color;
        text.fontSize = size;

        Vector3 start = position + new Vector3(Random.Range(-horizontalJitter, horizontalJitter), spawnHeight, 0f);
        text.transform.position = start;
        text.transform.localScale = Vector3.one;
        active.Add(new Popup { Text = text, Start = start, Elapsed = 0f, BaseSize = size, Color = color });
    }

    private void LateUpdate()
    {
        float dt = Time.unscaledDeltaTime;
        for (int i = active.Count - 1; i >= 0; i--)
        {
            Popup popup = active[i];
            popup.Elapsed += dt;
            float t = popup.Elapsed / numberLifetime;
            if (t >= 1f)
            {
                Release(popup.Text);
                active.RemoveAt(i);
                continue;
            }

            // 처음엔 살짝 커졌다가(팝) 위로 떠오르며 사라진다.
            float pop = t < 0.15f ? Mathf.Lerp(1.4f, 1f, t / 0.15f) : 1f;
            popup.Text.fontSize = popup.BaseSize * pop;
            popup.Text.transform.position = popup.Start + Vector3.up * (riseDistance * (1f - (1f - t) * (1f - t)));
            Color c = popup.Color;
            c.a *= t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
            popup.Text.color = c;
        }
    }

    private TextMeshPro Acquire()
    {
        TextMeshPro text = pool.Count > 0 ? pool.Pop() : CreateText();
        text.gameObject.SetActive(true);
        return text;
    }

    private void Release(TextMeshPro text)
    {
        if (text == null)
            return;
        text.gameObject.SetActive(false);
        pool.Push(text);
    }

    private TextMeshPro CreateText()
    {
        GameObject go = new GameObject("DamageNumber");
        go.transform.SetParent(transform, false);
        TextMeshPro text = go.AddComponent<TextMeshPro>();
        if (font != null)
            text.font = font;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.outlineWidth = 0.25f;
        text.outlineColor = new Color32(0, 0, 0, 200);
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        text.sortingLayerID = SortingLayer.NameToID(sortingLayerName);
        text.sortingOrder = sortingOrder;
        return text;
    }
}
