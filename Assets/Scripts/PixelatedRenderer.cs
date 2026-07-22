using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
public class PixelatedRenderer : MonoBehaviour
{
    public int targetHeight = 240;

    Camera cam;
    RenderTexture lowResRT;
    Canvas canvas;

    void OnEnable()
    {
        cam = GetComponent<Camera>();
        CreateRenderTexture();
        CreateDisplayCanvas();
    }

    void CreateRenderTexture()
    {
        int width = Mathf.Max(4, Mathf.RoundToInt(targetHeight * cam.aspect));
        lowResRT = new RenderTexture(width, targetHeight, 16);
        lowResRT.filterMode = FilterMode.Point;
        cam.targetTexture = lowResRT;
    }

    void CreateDisplayCanvas()
    {
        GameObject canvasGo = new GameObject("PixelationDisplayCanvas");
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGo.AddComponent<CanvasScaler>();

        GameObject imageGo = new GameObject("PixelatedOutput");
        imageGo.transform.SetParent(canvasGo.transform, false);
        RawImage rawImage = imageGo.AddComponent<RawImage>();
        rawImage.texture = lowResRT;

        RectTransform rt = rawImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void OnDisable()
    {
        if (cam != null) cam.targetTexture = null;
        if (lowResRT != null)
        {
            lowResRT.Release();
            Destroy(lowResRT);
        }
        if (canvas != null) Destroy(canvas.gameObject);
    }
}
