using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 마우스를 올리면 짧은 소리를 낸다. 버튼이 비활성(interactable 꺼짐)이면 울리지 않는다.
/// 코드에서 AddComponent 후 clip/volume을 넣어 써도 된다.
/// </summary>
public class UIHoverSound : MonoBehaviour, IPointerEnterHandler
{
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 0.35f;

    private Selectable selectable;

    private void Awake() => selectable = GetComponent<Selectable>();

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (selectable != null && !selectable.IsInteractable())
            return;
        SoundManager.Instance?.PlaySfx(clip, volume, 0.05f);
    }
}
