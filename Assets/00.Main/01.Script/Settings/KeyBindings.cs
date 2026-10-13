using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>사용자가 바꿀 수 있는 키 동작. 순서를 바꾸면 저장된 설정이 어긋나므로 뒤에만 추가한다.</summary>
public enum BindableAction
{
    MoveLeft,
    MoveRight,
    Jump,
    JumpAlt,
    SkillQ,
    SkillE,
    Dash,
    Ultimate,
    Interact,
}

/// <summary>
/// 키 설정(리바인딩). 전투 캐릭터, 로비 캐릭터, 상호작용이 모두 여기서 키를 읽는다.
/// PlayerPrefs에 저장되며, 한 키는 한 동작에만 묶인다(이미 쓰던 키를 고르면 두 동작의 키를 맞바꾼다).
/// 마우스(기본공격/재장전)와 Esc는 바꿀 수 없다.
/// </summary>
public static class KeyBindings
{
    private const string PrefsPrefix = "KeyBinding_";

    private static readonly Key[] Defaults =
    {
        Key.A, Key.D, Key.Space, Key.W, Key.Q, Key.E, Key.LeftShift, Key.R, Key.F,
    };

    private static readonly string[] ActionNames =
    {
        "왼쪽 이동", "오른쪽 이동", "점프", "점프(보조)", "스킬 Q", "스킬 E", "대시", "궁극기", "상호작용",
    };

    private static Key[] current;

    /// <summary>키가 바뀔 때마다(초기화 포함) 호출된다. 키 표시 텍스트 갱신용.</summary>
    public static event Action Changed;

    public static int ActionCount => Defaults.Length;

    public static Key Get(BindableAction action)
    {
        EnsureLoaded();
        int index = (int)action;
        return index >= 0 && index < current.Length ? current[index] : Key.None;
    }

    public static bool IsHeld(BindableAction action)
    {
        Keyboard keyboard = Keyboard.current;
        Key key = Get(action);
        return keyboard != null && key != Key.None && keyboard[key].isPressed;
    }

    public static bool WasPressed(BindableAction action)
    {
        Keyboard keyboard = Keyboard.current;
        Key key = Get(action);
        return keyboard != null && key != Key.None && keyboard[key].wasPressedThisFrame;
    }

    /// <summary>action에 key를 묶는다. 다른 동작이 이미 쓰던 키면 그 동작에는 action의 이전 키를 준다.</summary>
    public static void Set(BindableAction action, Key key)
    {
        EnsureLoaded();
        if (!IsBindable(key))
            return;

        int index = (int)action;
        Key previous = current[index];
        for (int i = 0; i < current.Length; i++)
        {
            if (i != index && current[i] == key)
            {
                current[i] = previous;
                Save(i);
            }
        }

        current[index] = key;
        Save(index);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    public static void ResetToDefaults()
    {
        EnsureLoaded();
        for (int i = 0; i < current.Length; i++)
        {
            current[i] = Defaults[i];
            PlayerPrefs.DeleteKey(PrefsPrefix + (BindableAction)i);
        }
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    /// <summary>바꿀 수 있는 키인지. Esc(메뉴/취소 전용)와 시스템 키는 막는다.</summary>
    public static bool IsBindable(Key key)
    {
        switch (key)
        {
            case Key.None:
            case Key.Escape:
            case Key.LeftWindows:
            case Key.RightWindows:
            case Key.ContextMenu:
            case Key.PrintScreen:
                return false;
            default:
                return true;
        }
    }

    public static string GetActionName(BindableAction action)
    {
        int index = (int)action;
        return index >= 0 && index < ActionNames.Length ? ActionNames[index] : action.ToString();
    }

    /// <summary>action에 묶인 키를 화면 표시용 짧은 이름으로(예: LeftShift → "SHIFT").</summary>
    public static string GetKeyLabel(BindableAction action) => GetKeyLabel(Get(action));

    public static string GetKeyLabel(Key key)
    {
        switch (key)
        {
            case Key.None: return "-";
            case Key.Space: return "SPACE";
            case Key.LeftShift:
            case Key.RightShift: return "SHIFT";
            case Key.LeftCtrl:
            case Key.RightCtrl: return "CTRL";
            case Key.LeftAlt:
            case Key.RightAlt: return "ALT";
            case Key.Tab: return "TAB";
            case Key.Enter: return "ENTER";
            case Key.Backspace: return "BACK";
            case Key.CapsLock: return "CAPS";
            case Key.UpArrow: return "↑";
            case Key.DownArrow: return "↓";
            case Key.LeftArrow: return "←";
            case Key.RightArrow: return "→";
        }

        string name = key.ToString();
        if (name.StartsWith("Digit"))
            return name.Substring(5);
        if (name.StartsWith("Numpad"))
            return "NUM" + name.Substring(6);
        return name.ToUpperInvariant();
    }

    private static void EnsureLoaded()
    {
        if (current != null)
            return;

        current = new Key[Defaults.Length];
        for (int i = 0; i < current.Length; i++)
        {
            int saved = PlayerPrefs.GetInt(PrefsPrefix + (BindableAction)i, (int)Defaults[i]);
            current[i] = Enum.IsDefined(typeof(Key), saved) ? (Key)saved : Defaults[i];
        }
    }

    private static void Save(int index)
    {
        PlayerPrefs.SetInt(PrefsPrefix + (BindableAction)index, (int)current[index]);
    }
}
