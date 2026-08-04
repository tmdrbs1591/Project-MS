/// <summary>
/// 트리거 범위 안에서 F 키로 상호작용할 수 있는 대상.
/// InteractionDetector가 트리거 콜라이더로 감지해 Interact()를 호출한다.
/// </summary>
public interface IInteractable
{
    void Interact();
}
