using UnityEngine;

namespace ProjectMS.CharacterSystem
{
    /// <summary>노드, 지뢰, 장판, 포탑처럼 캐릭터가 필드에 설치하는 오브젝트의 확장 기반.</summary>
    public class CharacterDeployable : CharacterOwnedEntity
    {
        [SerializeField] private bool activateOnSpawn = true;

        protected override void OnOwnedEntitySpawnedAuthority()
        {
            SetOwnedEntityActive(activateOnSpawn);
        }

        protected void CompleteDeployment()
        {
            SetOwnedEntityActive(true);
        }
    }
}
