using ProjectMS.CharacterSystem;
using TMPro;
using UnityEngine;

/// <summary>
/// 연습장 탈출구. 내 캐릭터가 가까이 오면 안내 문구("F 나가기")를 띄우고, 상호작용 키를 누르면 로비로 돌아간다.
/// 연습 맵 프리팹 안에 두며, 시작할 때 서서히 나타난다.
/// </summary>
public class PracticeExitPortal : MonoBehaviour
{
    [SerializeField] private float interactRadius = 1.6f;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private string promptFormat = "{0}  나가기";
    [SerializeField] private float fadeInDuration = 0.8f;

    private SpriteRenderer[] renderers;
    private float[] baseAlpha;
    private float startedAt;
    private bool leaving;

    private void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        baseAlpha = new float[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            baseAlpha[i] = renderers[i].color.a;
        startedAt = Time.time;
        SetPromptVisible(false);
    }

    private void Update()
    {
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

        if (promptText != null)
            promptText.text = string.Format(promptFormat, KeyBindings.GetKeyLabel(BindableAction.Interact));

        if (KeyBindings.WasPressed(BindableAction.Interact) && NetworkLauncher.Instance != null)
        {
            leaving = true;
            SetPromptVisible(false);
            NetworkLauncher.Instance.ReturnToLobby();
        }
    }

    private void SetPromptVisible(bool visible)
    {
        if (promptText != null && promptText.gameObject.activeSelf != visible)
            promptText.gameObject.SetActive(visible);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
