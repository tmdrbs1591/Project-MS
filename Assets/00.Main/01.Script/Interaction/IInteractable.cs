/// <summary>
/// 트리거 범위 안에서 F 키로 상호작용할 수 있는 대상.
/// InteractionDetector가 트리거 콜라이더로 감지해 Interact()를 호출한다.
/// </summary>
public interface IInteractable
{
    /// <summary>[F] 옆에 표시할 동작 이름(예: 포탈 "입장"). InteractionDetector가 범위에 닿는 순간
    /// 이 값을 Key UI의 텍스트에 채운다.</summary>
    string InteractionPrompt { get; }

    void Interact();
}
