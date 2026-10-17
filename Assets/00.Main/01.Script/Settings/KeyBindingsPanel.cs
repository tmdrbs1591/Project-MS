using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

/// <summary>
/// 키 설정 창. 동작마다 한 줄(이름 + 키 버튼)을 만들고, 버튼을 누른 뒤 원하는 키를 누르면 바뀐다.
///   - 이미 다른 동작이 쓰던 키를 고르면 두 동작의 키가 맞바뀐다(KeyBindings.Set).
///   - 키 입력 대기 중에 Esc를 누르면 취소, 대기 중이 아닐 때 Esc를 누르면 창을 닫는다.
///     (SettingsPanel은 이 창이 열려 있는 동안 Esc를 이 창에 양보한다.)
///
/// [씬 설정]
///   - 설정 창 위에 겹치는 패널에 붙인다. 숨김은 CanvasGroup으로 하므로 켜둔 채로 둔다.
///   - rowContainer: 세로 레이아웃 컨테이너. rowTemplate(꺼둔 한 줄)을 동작 수만큼 복제해 채운다.
///     rowTemplate 안에는 이름용 TMP_Text("Name")와 Button("Key", 자식에 TMP_Text)이 있어야 한다.
///   - 설정 창의 "키 설정" 버튼 OnClick에 Open()을 연결한다(SettingsPanel의 keyBindingsButton으로도 가능).
/// </summary>
public class KeyBindingsPanel : MonoBehaviour
{
    private class Row
    {
        public BindableAction action;
        public TMP_Text keyText;
        public Image keyImage;
    }

    [SerializeField] private RectTransform rowContainer;
    [SerializeField] private GameObject rowTemplate;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Color listeningColor = new Color(1f, 0.8f, 0.3f, 1f);

    public static KeyBindingsPanel Instance { get; private set; }
    /// <summary>열려 있으면 Esc를 이 창이 쓴다(SettingsPanel이 확인).</summary>
    public static bool IsOpen => Instance != null && Instance.isOpen;

    private readonly List<Row> rows = new List<Row>();
    private CanvasGroup canvasGroup;
    private bool isOpen;
    private Row listening;
    private Color normalKeyColor = Color.white;

    private void Awake()
    {
        Instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (resetButton != null)
            resetButton.onClick.AddListener(KeyBindings.ResetToDefaults);
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        BuildRows();
        SetVisible(false);
    }

    private void OnEnable() => KeyBindings.Changed += RefreshRows;
    private void OnDisable() => KeyBindings.Changed -= RefreshRows;

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Open()
    {
        isOpen = true;
        listening = null;
        RefreshRows();
        SetVisible(true);
    }

    public void Close()
    {
        isOpen = false;
        StopListening();
        SetVisible(false);
    }

    private void Update()
    {
        if (!isOpen)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (listening == null)
        {
            if (keyboard.escapeKey.wasPressedThisFrame)
                Close();
            return;
        }

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            StopListening();
            return;
        }

        foreach (KeyControl control in keyboard.allKeys)
        {
            if (control == null || !control.wasPressedThisFrame || !KeyBindings.IsBindable(control.keyCode))
                continue;

            BindableAction action = listening.action;
            StopListening();
            KeyBindings.Set(action, control.keyCode);
            return;
        }
    }

    private void BuildRows()
    {
        if (rowContainer == null || rowTemplate == null)
            return;

        rowTemplate.SetActive(false);
        for (int i = 0; i < KeyBindings.ActionCount; i++)
        {
            BindableAction action = (BindableAction)i;
            GameObject go = Instantiate(rowTemplate, rowContainer);
            go.name = action.ToString();
            go.SetActive(true);

            Transform nameTransform = go.transform.Find("Name");
            if (nameTransform != null && nameTransform.TryGetComponent(out TMP_Text nameText))
                nameText.text = KeyBindings.GetActionName(action);

            Button keyButton = go.GetComponentInChildren<Button>(true);
            var row = new Row
            {
                action = action,
                keyText = keyButton != null ? keyButton.GetComponentInChildren<TMP_Text>(true) : null,
                keyImage = keyButton != null ? keyButton.targetGraphic as Image : null,
            };
            if (row.keyImage != null)
                normalKeyColor = row.keyImage.color;
            if (keyButton != null)
                keyButton.onClick.AddListener(() => StartListening(row));
            rows.Add(row);
        }
        RefreshRows();
    }

    private void StartListening(Row row)
    {
        StopListening();
        listening = row;
        // 버튼이 선택된 채로 남으면 Space/Enter가 버튼을 다시 눌러 입력 대기가 반복되므로 선택을 푼다.
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
        if (row.keyText != null)
            row.keyText.text = "키 입력...";
        if (row.keyImage != null)
            row.keyImage.color = listeningColor;
    }

    private void StopListening()
    {
        if (listening == null)
            return;
        if (listening.keyImage != null)
            listening.keyImage.color = normalKeyColor;
        listening = null;
        RefreshRows();
    }

    private void RefreshRows()
    {
        foreach (Row row in rows)
        {
            if (row == listening || row.keyText == null)
                continue;
            row.keyText.text = KeyBindings.GetKeyLabel(row.action);
        }
    }

    private void SetVisible(bool visible)
    {
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }
}
