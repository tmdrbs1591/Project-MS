using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjectMS.CharacterSystem
{
    /// <summary>로컬 플레이어의 마우스 위치에 조준선을 표시하고 커서 표시 상태를 복구한다.</summary>
    public sealed class CharacterAimCursorPresenter : IDisposable
    {
        private readonly GameObject canvasObject;
        private readonly RectTransform crosshairRoot;
        private bool cursorOverrideActive;
        private bool cursorVisibleBeforeOverride;

        public bool IsCrosshairVisible => crosshairRoot != null && crosshairRoot.gameObject.activeSelf;
        public Vector2 ScreenOffset { get; private set; }

        public CharacterAimCursorPresenter()
        {
            canvasObject = new GameObject(
                "Character Aim Cursor",
                typeof(RectTransform),
                typeof(Canvas));
            canvasObject.hideFlags = HideFlags.DontSave;

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            GameObject root = new GameObject("Crosshair", typeof(RectTransform));
            root.hideFlags = HideFlags.DontSave;
            crosshairRoot = root.GetComponent<RectTransform>();
            crosshairRoot.SetParent(canvasObject.transform, false);
            crosshairRoot.anchorMin = new Vector2(0.5f, 0.5f);
            crosshairRoot.anchorMax = new Vector2(0.5f, 0.5f);
            crosshairRoot.pivot = new Vector2(0.5f, 0.5f);
            crosshairRoot.sizeDelta = new Vector2(28f, 28f);

            CreateLine("Horizontal", new Vector2(24f, 2f));
            CreateLine("Vertical", new Vector2(2f, 24f));
            root.SetActive(false);
        }

        public void Apply(
            Vector2 screenOffset,
            bool hasCrosshairOverride,
            bool crosshairVisible,
            bool hasCursorOverride,
            bool systemCursorVisible)
        {
            ScreenOffset = screenOffset;
            if (crosshairRoot != null)
            {
                crosshairRoot.gameObject.SetActive(hasCrosshairOverride && crosshairVisible);
                UpdatePosition();
            }

            ApplySystemCursor(hasCursorOverride, systemCursorVisible);
        }

        public void Dispose()
        {
            RestoreSystemCursor();
            if (canvasObject == null)
                return;

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(canvasObject);
            else
                UnityEngine.Object.DestroyImmediate(canvasObject);
        }

        private void CreateLine(string name, Vector2 size)
        {
            GameObject lineObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            lineObject.hideFlags = HideFlags.DontSave;
            RectTransform line = lineObject.GetComponent<RectTransform>();
            line.SetParent(crosshairRoot, false);
            line.anchorMin = new Vector2(0.5f, 0.5f);
            line.anchorMax = new Vector2(0.5f, 0.5f);
            line.pivot = new Vector2(0.5f, 0.5f);
            line.anchoredPosition = Vector2.zero;
            line.sizeDelta = size;

            Image image = lineObject.GetComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = false;
        }

        private void UpdatePosition()
        {
            Vector2 mousePosition;
            if (Mouse.current != null)
                mousePosition = Mouse.current.position.ReadValue();
            else
                mousePosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            crosshairRoot.anchoredPosition = mousePosition -
                                               new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) +
                                               ScreenOffset;
        }

        private void ApplySystemCursor(bool hasOverride, bool visible)
        {
            if (hasOverride)
            {
                if (!cursorOverrideActive)
                {
                    cursorVisibleBeforeOverride = Cursor.visible;
                    cursorOverrideActive = true;
                }

                Cursor.visible = visible;
                return;
            }

            RestoreSystemCursor();
        }

        private void RestoreSystemCursor()
        {
            if (!cursorOverrideActive)
                return;

            Cursor.visible = cursorVisibleBeforeOverride;
            cursorOverrideActive = false;
        }
    }
}
