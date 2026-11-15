using UnityEngine;

/// <summary>
/// 설정의 스킬 UI 타입(GameSettings.HudLayout)에 따라 HUD를 바꿔 켠다.
///   - A(0): 기존 — 위쪽 양 코너의 스킬 HUD(P1/P2)
///   - B(1): 내 스킬만 화면 아래 가운데(LocalCooldownHUD)
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
