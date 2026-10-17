using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 마우스 진입/이탈만 콜백으로 넘겨주는 경량 컴포넌트.
/// EventTrigger는 드래그/스크롤 이벤트까지 전부 받아서 부모 ScrollRect로 전달하지 않기 때문에
/// (스크롤 목록 안의 버튼에 붙이면 슬라이드가 안 됨), 호버만 필요할 때는 이걸 쓴다.
/// </summary>
public class UIHoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public event Action Entered;
    public event Action Exited;

    public void OnPointerEnter(PointerEventData eventData) => Entered?.Invoke();
    public void OnPointerExit(PointerEventData eventData) => Exited?.Invoke();
}
