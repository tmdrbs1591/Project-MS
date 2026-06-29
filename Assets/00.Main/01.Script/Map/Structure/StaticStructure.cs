using Map;

/// <summary>
/// 어떤 상호작용에도 반응하지 않는 정적 구조물이다.
///
/// [필요한 것]
///   - Collider2D
///   - Rigidbody2D가 필요하다면 Body Type을 Static으로 설정
///   - 네트워크 동기화 불필요 (NetworkObject 없이 양쪽 씬에 동일하게 배치되면 됨)
/// </summary>
public class StaticStructure : StructureBase { }
