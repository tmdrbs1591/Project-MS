using ProjectMS.CharacterSystem;
using TMPro;
using UnityEngine;

/// <summary>
/// 연습장 탈출구. 내 캐릭터가 가까이 오면 로비처럼 캐릭터 머리 위에 [F] 나가기 안내를 띄우고,
/// 상호작용 키를 누르면 로비로 돌아간다. 연습 맵 프리팹 안에 두며, 시작할 때 서서히 나타난다.
///   - keyPromptPrefab: 로비 캐릭터의 Key UI Canvas와 같은 모양(Box 밑에 "Key Cap/Key" 글자, "Label" 글자).
///     여기서 하나 만들어 두고 내 캐릭터를 따라다니게 위치만 옮긴다.
/// </summary>
public class PracticeExitPortal : MonoBehaviour
{
    [SerializeField] private float interactRadius = 1.6f;
    [SerializeField] private GameObject keyPromptPrefab;
    [SerializeField] private string promptLabel = "나가기";
    [Tooltip("키 안내를 캐릭터 위치에서 얼마나 옮길지(로비 캐릭터와의 키 차이 보정).")]
    [SerializeField] private Vector2 promptOffset = Vector2.zero;
    [SerializeField] private float fadeInDuration = 0.8f;

    private SpriteRenderer[] renderers;
    private float[] baseAlpha;
    private float startedAt;
    private bool leaving;
    private GameObject keyPrompt;
    private TMP_Text keyText;

    private void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        baseAlpha = new float[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            baseAlpha[i] = renderers[i].color.a;
        startedAt = Time.time;

        if (keyPromptPrefab != null)
        {
            keyPrompt = Instantiate(keyPromptPrefab);
            keyPrompt.name = "Practice Exit Key Prompt";
            Transform key = keyPrompt.transform.Find("Box/Key Cap/Key");
            Transform label = keyPrompt.transform.Find("Box/Label");
            keyText = key != null ? key.GetComponent<TMP_Text>() : null;
            if (label != null && label.TryGetComponent(out TMP_Text labelText))
                labelText.text = promptLabel;
            keyPrompt.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (keyPrompt != null)
            Destroy(keyPrompt);
    }

    private void Update()
    {
        // 로딩 화면이 떠 있는 동안엔 등장 페이드를 시작하지 않는다.
        if (SceneTransition.Instance != null && SceneTransition.Instance.IsBusy)
            startedAt = Time.time;

        float fade = fadeInDuration > 0f ? Mathf.Clamp01((Time.time - startedAt) / fadeInDuration) : 1f;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;
            Color c = renderers[i].color;
            c.a = baseAlpha[i] * fade;
            renderers[i].color = c;
        }

        CharacterBase local = CharacterBase.LocalPlayer;
        bool near = local != null && !leaving &&
                    Vector2.Distance(local.transform.position, transform.position) <= interactRadius;
        SetPromptVisible(near);
        if (!near)
            return;

        UpdatePrompt(local);

        if (KeyBindings.WasPressed(BindableAction.Interact) && NetworkLauncher.Instance != null)
        {
            leaving = true;
            SetPromptVisible(false);
            NetworkLauncher.Instance.ReturnToLobby();
        }
    }

    private void SetPromptVisible(bool visible)
    {
        if (keyPrompt != null && keyPrompt.activeSelf != visible)
            keyPrompt.SetActive(visible);
    }

    private void UpdatePrompt(CharacterBase local)
    {
        if (keyText != null)
            keyText.text = KeyBindings.GetKeyLabel(BindableAction.Interact);

        // 튀어나오는 연출은 프리팹의 UIPopIn이 한다. 여기선 캐릭터를 따라다니기만 한다.
        keyPrompt.transform.position = local.transform.position + (Vector3)promptOffset;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
