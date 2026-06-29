using Photon.Pun;

/// <summary>
/// 맵에 배치되는 모든 구조물이 상속받는 부모 클래스다.
///
/// [역할]
///   - PushableStructure, StaticStructure, BreakableStructure의 공통 식별자 역할을 한다.
///   - 네트워크 동기화가 필요한 자식 클래스는 PhotonView를 오브젝트에 추가하고 photonView를 활용한다.
///
/// [자식 클래스별 필요 컴포넌트]
///   - PushableStructure : Collider2D + Rigidbody2D + PhotonView + NetworkRigidbody2D
///   - StaticStructure   : Collider2D (PhotonView 불필요)
///   - BreakableStructure: Collider2D + PhotonView
/// </summary>

namespace Map
{
    public abstract class StructureBase : MonoBehaviourPun { }
}
