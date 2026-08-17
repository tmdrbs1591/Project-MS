#if UNITY_EDITOR
using System.Collections.Generic;
using Fusion;
using UnityEditor;
using UnityEngine;
using ProjectMS.CharacterSystem;

namespace ProjectMS.CharacterSystem.Editor
{
    public static class CharacterPrefabValidator
    {
        [MenuItem("Tools/Project MS/Character/Validate Selected Character Asset")]
        private static void ValidateSelected()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("Character Validator", "Hierarchy 또는 Project에서 캐릭터를 선택하세요.", "확인");
                return;
            }

            CharacterBase character = selected.GetComponentInParent<CharacterBase>();
            if (character == null)
                character = selected.GetComponentInChildren<CharacterBase>(true);

            CharacterOwnedEntity ownedEntity = selected.GetComponentInParent<CharacterOwnedEntity>();
            if (ownedEntity == null)
                ownedEntity = selected.GetComponentInChildren<CharacterOwnedEntity>(true);

            if (character == null && ownedEntity == null)
            {
                EditorUtility.DisplayDialog(
                    "Character Validator",
                    "CharacterBase 또는 CharacterOwnedEntity 파생 컴포넌트를 찾지 못했습니다.",
                    "확인");
                return;
            }

            List<string> problems = new List<string>();
            if (character != null)
                ValidateCharacter(character, problems);
            else
                ValidateOwnedEntity(ownedEntity, problems);

            string message = problems.Count == 0
                ? "필수 공통 구성이 정상입니다. Fusion Prefab Table 등록과 2인 테스트를 추가로 확인하세요."
                : string.Join("\n", problems);

            EditorUtility.DisplayDialog(
                "Character Validator",
                message,
                "확인");
        }

        private static void ValidateCharacter(CharacterBase character, ICollection<string> problems)
        {
            Require<NetworkObject>(character.gameObject, problems);
            Require<Rigidbody2D>(character.gameObject, problems);
            Require<Collider2D>(character.gameObject, problems);
            Require<NetworkTRSP>(character.gameObject, problems);

            if (character.Definition == null)
                problems.Add("Character Definition이 연결되지 않았습니다.");
            if (character.GetComponentInChildren<CharacterVisualController>(true) == null)
                problems.Add("CharacterVisualController가 없습니다.");
        }

        private static void ValidateOwnedEntity(
            CharacterOwnedEntity ownedEntity,
            ICollection<string> problems)
        {
            Require<NetworkObject>(ownedEntity.gameObject, problems);

            if (!(ownedEntity is CharacterThrowable throwable))
                return;

            Rigidbody2D body = throwable.GetComponent<Rigidbody2D>();
            Collider2D collider = throwable.GetComponent<Collider2D>();
            Require<Rigidbody2D>(throwable.gameObject, problems);
            Require<Collider2D>(throwable.gameObject, problems);
            Require<Fusion.Addons.Physics.NetworkRigidbody2D>(throwable.gameObject, problems);

            if (body != null && body.bodyType != RigidbodyType2D.Dynamic)
                problems.Add("투척체 Rigidbody2D는 Dynamic이어야 합니다.");
            if (collider != null && collider.isTrigger)
                problems.Add("투척체 Collider2D는 Trigger가 아니어야 합니다.");
            if (throwable.GetComponents<NetworkTRSP>().Length != 1)
                problems.Add("투척체 루트의 위치 동기화 컴포넌트는 NetworkRigidbody2D 하나만 있어야 합니다.");
            if (!throwable.HasValidFuseConfiguration)
                problems.Add("OnGroundContact 퓨즈에는 Ground Layer가 필요합니다.");
        }

        private static void Require<T>(GameObject gameObject, ICollection<string> problems) where T : Component
        {
            if (gameObject.GetComponent<T>() == null)
                problems.Add($"{typeof(T).Name} 컴포넌트가 없습니다.");
        }
    }
}
#endif
