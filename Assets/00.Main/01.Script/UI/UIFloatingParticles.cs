using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 위를 반짝이 입자들이 천천히 떠오르며 깜빡이는 UI 배경 연출.
/// 파티클 시스템 없이 Image 몇 개만 돌려써서 Screen Space - Overlay 캔버스에서도 그대로 보인다.
///
/// [씬 설정]
///   - 입자가 떠다닐 영역(RectTransform, 보통 화면 전체)에 붙인다. 입자는 이 영역 안에서만 생긴다.
///   - 캐릭터 선택 화면에서는 카드 뒤쪽에 둬서 카드/패널을 가리지 않게 한다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UIFloatingParticles : MonoBehaviour
{
    [SerializeField] private Sprite sprite;
    [Min(1)] [SerializeField] private int count = 28;
    [SerializeField] private Vector2 sizeRange = new Vector2(10f, 28f);
    [SerializeField] private Vector2 riseSpeedRange = new Vector2(18f, 55f);
    [SerializeField] private Color color = new Color(0.75f, 0.9f, 1f, 0.7f);

    private struct Particle
    {
        public RectTransform rect;
        public Image image;
        public float speed;
        public float sway;
        public float phase;
        public float twinkle;
    }

    private Particle[] particles;
    private RectTransform area;

    private void Awake()
    {
        area = (RectTransform)transform;
        particles = new Particle[count];
        for (int i = 0; i < count; i++)
        {
            GameObject go = new GameObject("Particle", typeof(RectTransform));
            go.layer = gameObject.layer;
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(transform, false);

            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;

            particles[i] = new Particle { rect = rt, image = img };
            Respawn(ref particles[i], randomHeight: true);
        }
    }

    private void Update()
    {
        Rect r = area.rect;
        float dt = Time.unscaledDeltaTime;
        float time = Time.unscaledTime;

        for (int i = 0; i < particles.Length; i++)
        {
            ref Particle p = ref particles[i];
            Vector2 pos = p.rect.anchoredPosition;
            pos.y += p.speed * dt;
            pos.x += Mathf.Sin(time * 0.8f + p.phase) * p.sway * dt;
            p.rect.anchoredPosition = pos;

            // 위로 갈수록 서서히 사라지고, 반짝반짝 깜빡인다.
            float heightT = Mathf.InverseLerp(r.yMin, r.yMax, pos.y);
            float fade = Mathf.Clamp01(heightT * 6f) * Mathf.Clamp01((1f - heightT) * 3f);
            float blink = 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(time * p.twinkle + p.phase));
            Color c = color;
            c.a *= fade * blink;
            p.image.color = c;

            if (pos.y > r.yMax)
                Respawn(ref p, randomHeight: false);
        }
    }

    private void Respawn(ref Particle p, bool randomHeight)
    {
        Rect r = area.rect;
        float size = Random.Range(sizeRange.x, sizeRange.y);
        p.rect.sizeDelta = new Vector2(size, size);
        p.rect.anchoredPosition = new Vector2(
            Random.Range(r.xMin, r.xMax),
            randomHeight ? Random.Range(r.yMin, r.yMax) : r.yMin - size);
        p.speed = Random.Range(riseSpeedRange.x, riseSpeedRange.y);
        p.sway = Random.Range(8f, 30f);
        p.phase = Random.Range(0f, Mathf.PI * 2f);
        p.twinkle = Random.Range(1.5f, 4f);
    }
}
