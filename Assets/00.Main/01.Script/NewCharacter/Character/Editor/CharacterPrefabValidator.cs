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
        [MenuItem("Tools/Project MS/Character/Validate Selected Character")]
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

            if (character == null)
            {
                EditorUtility.DisplayDialog("Character Validator", "CharacterBase를 상속한 컴포넌트를 찾지 못했습니다.", "확인");
                return;
            }

            List<string> problems = new List<string>();
            Require<NetworkObject>(character.gameObject, problems);
            Require<Rigidbody2D>(character.gameObject, problems);
            Require<Collider2D>(character.gameObject, problems);

            if (character.Definition == null)
                problems.Add("Character Definition이 연결되지 않았습니다.");
            if (character.GetComponentInChildren<CharacterVisualController>(true) == null)
                problems.Add("CharacterVisualController가 없습니다.");

            string message = problems.Count == 0
                ? "필수 캐릭터 구성이 정상입니다. Fusion Prefab Table 등록과 2인 테스트를 추가로 확인하세요."
                : string.Join("\n", problems);

            EditorUtility.DisplayDialog(
                "Character Validator",
                message,
                "확인");
        }

        private static void Require<T>(GameObject gameObject, ICollection<string> problems) where T : Component
        {
            if (gameObject.GetComponent<T>() == null)
                problems.Add($"{typeof(T).Name} 컴포넌트가 없습니다.");
        }
    }
}
#endif
