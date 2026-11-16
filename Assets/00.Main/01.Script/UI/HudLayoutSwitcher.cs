using UnityEngine;

/// <summary>
/// 설정의 스킬 UI 타입(GameSettings.HudLayout)에 따라 HUD를 바꿔 켠다.
///   - A(0): 기존 — 위쪽 양 코너의 스킬 HUD(P1/P2)
///   - B(1): 내 스킬만 화면 아래 가운데(LocalCooldownHUD). 위쪽 스킬 HUD가 빠진 자리는
///           shiftInB(증강 기록 등)를 위로 당겨 메운다.
/// 설정 창에서 바꾸면 바로 반영된다.
///
/// [씬 설정]
///   - Canvas 등 항상 켜져 있는 오브젝트에 붙이고, 타입별로 켤 오브젝트를 연결한다.
/// </summary>
public class HudLayoutSwitcher : MonoBehaviour
{
    [Tooltip("A 타입에서 켤 것(위쪽 양 코너 스킬 HUD).")]
    [SerializeField] private GameObject[] layoutA;
    [Tooltip("B 타입에서 켤 것(아래 가운데 내 스킬 HUD).")]
    [SerializeField] private GameObject[] layoutB;
    [Tooltip("B 타입에서 옮길 것(위쪽 스킬 HUD 아래에 있던 증강 기록 등). 스킬 HUD가 빠진 빈 자리를 메운다.")]
    [SerializeField] private RectTransform[] shiftInB;
    [Tooltip("B 타입에서 shiftInB의 같은 순서 항목에 더할 위치(anchoredPosition 기준). 좌/우 HUD는 방향이 반대라 따로 둔다.")]
    [SerializeField] private Vector2[] shiftOffsetsB;

    private Vector2[] shiftOriginals;

    private void OnEnable()
    {
        GameSettings.HudLayoutChanged += Apply;
        Apply();
    }

    private void OnDisable()
    {
        GameSettings.HudLayoutChanged -= Apply;
    }

    private void Apply()
    {
        bool b = GameSettings.HudLayout == 1;
        SetActive(layoutA, !b);
        SetActive(layoutB, b);

        if (shiftInB == null)
            return;
        if (shiftOriginals == null || shiftOriginals.Length != shiftInB.Length)
        {
            shiftOriginals = new Vector2[shiftInB.Length];
            for (int i = 0; i < shiftInB.Length; i++)
                if (shiftInB[i] != null)
                    shiftOriginals[i] = shiftInB[i].anchoredPosition;
        }
        for (int i = 0; i < shiftInB.Length; i++)
        {
            if (shiftInB[i] == null)
                continue;
            Vector2 offset = b && shiftOffsetsB != null && i < shiftOffsetsB.Length ? shiftOffsetsB[i] : Vector2.zero;
            shiftInB[i].anchoredPosition = shiftOriginals[i] + offset;
        }
    }

    private static void SetActive(GameObject[] targets, bool active)
    {
        if (targets == null)
            return;
        foreach (GameObject go in targets)
            if (go != null && go.activeSelf != active)
                go.SetActive(active);
    }
}
