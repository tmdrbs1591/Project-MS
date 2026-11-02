using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 전체를 잠깐 한 색으로 번쩍이게 하는 공용 오버레이(섬광탄에 눈부심 등). 처음 쓸 때 스스로 만들어진다.
/// </summary>
public class ScreenFlash : MonoBehaviour
{
    private static ScreenFlash instance;
    private Image image;
    private Coroutine routine;

    public static void Play(Color color, float alpha, float hold, float fade)
    {
        if (instance == null)
        {
            var go = new GameObject("Screen Flash");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<ScreenFlash>();
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 810;
            instance.image = new GameObject("Flash", typeof(RectTransform)).AddComponent<Image>();
            instance.image.transform.SetParent(go.transform, false);
            RectTransform rt = instance.image.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            instance.image.raycastTarget = false;
            instance.image.color = Color.clear;
        }
        if (instance.routine != null)
            instance.StopCoroutine(instance.routine);
        instance.routine = instance.StartCoroutine(instance.Run(color, alpha, hold, fade));
    }

    private IEnumerator Run(Color c, float alpha, float hold, float fade)
    {
        image.color = new Color(c.r, c.g, c.b, alpha);
        for (float t = 0f; t < hold; t += Time.unscaledDeltaTime)
            yield return null;
        for (float t = 0f; t < fade; t += Time.unscaledDeltaTime)
        {
            float k = 1f - t / fade;
            image.color = new Color(c.r, c.g, c.b, alpha * k * k);
            yield return null;
        }
        image.color = Color.clear;
        routine = null;
    }
}
