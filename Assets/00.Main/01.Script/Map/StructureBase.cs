using UnityEngine;

/// <summary>
/// 정적 구조물의 공통 부모(식별자). 네트워크가 필요 없는 구조물용이다.
///
/// [Fusion 이전 메모]
///   - 네트워크 동기화가 필요한 구조물(PushableStructure / BreakableStructure)은
///     단일 상속 제약 때문에 이 클래스를 상속하지 않고 NetworkBehaviour 를 직접 상속한다.
///   - StaticStructure 처럼 동기화가 필요 없는 구조물만 이 클래스를 쓴다.
/// </summary>
namespace Map
{
    public abstract class StructureBase : MonoBehaviour { }
}
