using System.Collections.Generic;
using Fusion;
using ProjectMS.CharacterSystem;
using ProjectMS.CharacterSystem.Examples;
using UnityEngine;

/// <summary>
/// 적(내가 아닌 캐릭터)과 적이 쏜 투사체/설치물에 은은한 빨간 테두리를 둘러 피아 구분을 쉽게 한다.
///   - 원본 스프라이트 뒤에 같은 모양을 단색(Custom/Sprite/Silhouette)으로 사방에 살짝씩 비껴 깔아서 테두리를 만든다.
///     안쪽 테두리는 진하게, 바깥쪽은 옅게 한 겹 더 깔아 배경에 부드럽게 녹아든다.
///   - 원본의 그림/뒤집힘/표시 여부/투명도를 매 프레임 따라가므로 애니메이션·피격 깜빡임에도 같이 움직인다.
///   - 파티클 이펙트에는 붙이지 않는다(스프라이트만).
/// 각 클라가 자기 화면 기준(LocalPlayer가 아닌 쪽)으로 그리므로 동기화는 필요 없다.
///
/// [씬 설정]
///   - 게임 씬 아무 오브젝트에 하나 붙이고 Silhouette 머티리얼을 넣는다.
/// </summary>
public class EnemyOutline : MonoBehaviour
{
    [SerializeField] private Material silhouetteMaterial;
    [SerializeField] private Color color = new Color(1f, 0.16f, 0.16f, 1f);
    [Tooltip("캐릭터 테두리 두께(월드 단위).")]
    [SerializeField] private float characterWidth = 0.026f;
    [Tooltip("투사체 테두리 두께(월드 단위).")]
    [SerializeField] private float projectileWidth = 0.022f;
    [Tooltip("바깥쪽 은은한 번짐의 두께 배율과 진하기.")]
    [SerializeField] private float glowScale = 1.8f;
    [Range(0f, 1f)] [SerializeField] private float glowAlpha = 0.22f;
    [Range(0f, 1f)] [SerializeField] private float coreAlpha = 0.9f;
    [SerializeField] private float scanInterval = 0.15f;

    private readonly Dictionary<GameObject, Rig> rigs = new Dictionary<GameObject, Rig>();
    private readonly List<GameObject> remove = new List<GameObject>();
    private float nextScan;

    private void LateUpdate()
    {
        CharacterBase local = CharacterBase.LocalPlayer;
        if (local == null || silhouetteMaterial == null)
            return;

        if (Time.unscaledTime >= nextScan)
        {
            nextScan = Time.unscaledTime + scanInterval;
            Scan(local);
        }

        remove.Clear();
        foreach (var kv in rigs)
        {
            if (kv.Key == null) { remove.Add(kv.Key); continue; }
            kv.Value.Sync();
        }
        foreach (GameObject k in remove)
            rigs.Remove(k);
    }

    private void Scan(CharacterBase local)
    {
        PlayerRef me = local.MatchPlayer;
        foreach (CharacterBase c in CharacterBase.All)
            if (c != null && c != local)
                Ensure(c.gameObject, characterWidth);

        foreach (CharacterProjectile p in FindObjectsByType<CharacterProjectile>(FindObjectsSortMode.None))
            if (p.Object != null && p.Owner != me)
                Ensure(p.gameObject, projectileWidth);

        foreach (GunnerGrenadeProjectile p in FindObjectsByType<GunnerGrenadeProjectile>(FindObjectsSortMode.None))
            if (p.Object != null && p.Owner != me)
                Ensure(p.gameObject, projectileWidth);

        foreach (CharacterOwnedEntity e in FindObjectsByType<CharacterOwnedEntity>(FindObjectsSortMode.None))
            if (e.Object != null && local.Object != null && e.OwnerCharacterId.IsValid && e.OwnerCharacterId != local.Object.Id)
                Ensure(e.gameObject, projectileWidth);
    }

    private void Ensure(GameObject target, float width)
    {
        if (!rigs.ContainsKey(target))
            rigs[target] = new Rig(target, this, width);
    }

    // ---------------- 한 대상의 테두리 묶음 ----------------

    private sealed class Rig
    {
        private static readonly Vector2[] Dirs =
        {
            new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1),
            new Vector2(0.707f, 0.707f), new Vector2(-0.707f, 0.707f), new Vector2(0.707f, -0.707f), new Vector2(-0.707f, -0.707f),
        };

        private readonly GameObject target;
        private readonly EnemyOutline owner;
        private readonly float width;
        private readonly List<(SpriteRenderer src, SpriteRenderer[] copies)> parts = new List<(SpriteRenderer, SpriteRenderer[])>();
        private int sourceCount = -1;

        public Rig(GameObject target, EnemyOutline owner, float width)
        {
            this.target = target;
            this.owner = owner;
            this.width = width;
        }

        public void Sync()
        {
            SpriteRenderer[] sources = Sources();
            if (sources.Length != sourceCount)
                Rebuild(sources);

            int order = int.MaxValue;
            foreach (var (src, _) in parts)
                if (src != null) order = Mathf.Min(order, src.sortingOrder);
            order -= 1;

            foreach (var (src, copies) in parts)
            {
                if (src == null) continue;
                bool show = src.enabled && src.gameObject.activeInHierarchy && src.sprite != null && src.color.a > 0.85f;
                // 반투명한 몸(연습 봇 등)은 뒤에 깐 테두리가 비쳐 몸이 통째로 빨개 보이므로 그리지 않는다.
                for (int i = 0; i < copies.Length; i++)
                {
                    SpriteRenderer c = copies[i];
                    if (c == null) continue;
                    c.enabled = show;
                    if (!show) continue;
                    bool glow = i >= Dirs.Length;
                    Vector2 d = Dirs[i % Dirs.Length] * width * (glow ? owner.glowScale : 1f);
                    c.transform.localPosition = src.transform.InverseTransformVector(d);
                    c.sprite = src.sprite;
                    c.flipX = src.flipX;
                    c.flipY = src.flipY;
                    c.sortingLayerID = src.sortingLayerID;
                    c.sortingOrder = order;
                    Color col = owner.color;
                    col.a *= (glow ? owner.glowAlpha : owner.coreAlpha) * Mathf.InverseLerp(0.85f, 1f, src.color.a);
                    c.color = col;
                }
            }
        }

        private SpriteRenderer[] Sources()
        {
            var list = new List<SpriteRenderer>();
            foreach (SpriteRenderer sr in target.GetComponentsInChildren<SpriteRenderer>(true))
                if (sr.sharedMaterial != owner.silhouetteMaterial && !sr.name.StartsWith("Outline"))
                    list.Add(sr);
            return list.ToArray();
        }

        private void Rebuild(SpriteRenderer[] sources)
        {
            foreach (var (_, copies) in parts)
                foreach (SpriteRenderer c in copies)
                    if (c != null) Object.Destroy(c.gameObject);
            parts.Clear();
            sourceCount = sources.Length;
            foreach (SpriteRenderer src in sources)
            {
                var copies = new SpriteRenderer[Dirs.Length * 2];
                for (int i = 0; i < copies.Length; i++)
                {
                    var go = new GameObject("Outline");
                    go.transform.SetParent(src.transform, false);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sharedMaterial = owner.silhouetteMaterial;
                    copies[i] = sr;
                }
                parts.Add((src, copies));
            }
        }
    }
}
