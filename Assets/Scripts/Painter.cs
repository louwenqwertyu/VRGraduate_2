using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Painter : MonoBehaviour
{
    //[SerializeField] private Transform PenPos;
    //[SerializeField] private Transform initTransform;
    [SerializeField] private Camera UICamera;
    //[SerializeField] private Image drawRawImage;
    [SerializeField] private Slider controlSizeSlider;
    //[SerializeField] private Camera paintCamera;

    private const int MaxBrushSize = 20;

    public RawImage rawImage;
    public Material material;
    private Texture2D brushTexture; //笔刷纹理
    private Texture2D rubberTexture;//橡皮檫纹理
    public int brushSize = 5;    //笔刷大小
    private float resolutionMultiplier = 5;  //线性插值密度调节
    public Color currentColor = Color.black;
    public Material rubberMaterial;

    private Vector2 previousPos; //记录上一帧鼠标的位置  
    //private Vector2 initPos;

    //private float scaleX;
    //private float scaleY;

    //Texture2D texture;
    private RenderTexture renderTexture;

    private HashSet<Vector2Int> modifiedPixels = new HashSet<Vector2Int>();

    private bool isPen = true;

    private void Start()
    {
        renderTexture = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
        renderTexture.filterMode = FilterMode.Point;
        renderTexture.wrapMode = TextureWrapMode.Clamp;
        brushTexture = CreateBrushTexture(brushSize, currentColor);
        rubberTexture = CreateBrushTexture(brushSize, new Color(1, 1, 1, 0.1f));

        //scaleX = 512 /(float) Screen.width;
        //scaleY = 512 /(float) Screen.height;

        //texture = new Texture2D(Screen.width, Screen.height, TextureFormat.ARGB32, false);
        //texture.filterMode = FilterMode.Point;
        //texture.wrapMode = TextureWrapMode.Clamp;

        rawImage.texture = renderTexture;
    }

    Texture2D CreateBrushTexture(int size, Color color)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    //private Vector2 ChangePos(Vector2 pos)
    //{
    //    float x = pos.x - initPos.x;
    //    float y = pos.y - initPos.y;
    //    Debug.Log($"x:{x}  y:{y}");
    //    x = x * renderTexture.width;
    //    y = y * renderTexture.height;

    //    return new Vector2(x, y);
    //}

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            previousPos = MousePosToPixel(Input.mousePosition);
        }

        if (Input.GetMouseButton(0))
        {
            Vector2 pisittion= MousePosToPixel(Input.mousePosition);

            DrawLine(previousPos, pisittion);
            previousPos = pisittion;
        }
    }

    private Vector2 MousePosToPixel(Vector2 mouseScreenPos)
    {
        // 转换屏幕坐标到UI元素的本地坐标
        Vector2 localPos;
        bool insideRect = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rawImage.rectTransform,
            mouseScreenPos,
            UICamera,
            out localPos
        );

        if (!insideRect) return previousPos; // 可选：当鼠标不在UI元素内时不处理

        Rect rect = rawImage.rectTransform.rect;

        // 计算UV坐标（假设轴心点在左下角）
        float uvX = (localPos.x - rect.x) / rect.width;
        float uvY = (localPos.y - rect.y) / rect.height;

        // 转换为像素坐标
        int pixelX = Mathf.FloorToInt(uvX * renderTexture.width);
        int pixelY = Mathf.FloorToInt(uvY * renderTexture.height);

        // 确保坐标在纹理范围内
        //pixelX = Mathf.Clamp(pixelX, 0, renderTexture.width+1);
        //pixelY = Mathf.Clamp(pixelY, 0, renderTexture.height+1);

        Debug.Log($"纹理像素坐标: ({pixelX}, {pixelY})");

        return new Vector2(pixelX, pixelY);
    }

    private void DrawLine(Vector2 start, Vector2 end)
    {
        float distance = Vector2.Distance(start, end);
        int steps = Mathf.CeilToInt(distance * resolutionMultiplier);
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            int x = Mathf.FloorToInt(Mathf.Lerp(start.x, end.x, t));
            int y = Mathf.FloorToInt(Mathf.Lerp(start.y, end.y, t));
            Draw(x, y);
        }
    }

    //private void DrawBrush(int x, int y)
    //{
    //    //Rect brushRect = new Rect(x, y, brushSize, brushSize);
    //    //Graphics.SetRenderTarget(renderTexture);
    //    //GL.PushMatrix();
    //    //GL.Color(Color.black);
    //    //GL.LoadPixelMatrix(0, renderTexture.width, 0, renderTexture.height);
    //    //Graphics.DrawTexture(brushRect, brushTexture);
    //    //GL.PopMatrix();
    //    //Graphics.SetRenderTarget(null);
    //}

    void Draw(int x, int y)
    {
        // 创建一个临时RenderTexture
        RenderTexture tempTexture = RenderTexture.GetTemporary(renderTexture.width, renderTexture.height);
        // 将当前画布复制到临时RenderTexture
        Graphics.Blit(renderTexture, tempTexture);
        // 设置RenderTexture为活动状态
        RenderTexture.active = renderTexture;
        // 绘制笔刷
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, renderTexture.width, renderTexture.height, 0);
        Graphics.DrawTexture(new Rect(x - brushSize / 2, -y+renderTexture.height - brushSize / 2, brushSize, brushSize), isPen? brushTexture:rubberTexture,isPen?null:rubberMaterial);
        Debug.Log($"x:{x}  y:{y}");

        GL.PopMatrix();
        RenderTexture.active = null;

        // 释放临时RenderTexture
        RenderTexture.ReleaseTemporary(tempTexture);

        RecordModifiedPixels(x,y);
    }

    public Color[] GetCanvasPixels()
    {
        // 从RenderTexture中读取像素
        Texture2D tex = new Texture2D(renderTexture.width, renderTexture.height);
        RenderTexture.active = renderTexture;
        tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        return tex.GetPixels();
    }

    void RecordModifiedPixels(int centerX, int centerY)
    {
        for (int i = -brushSize / 2; i <= brushSize / 2; i++)
        {
            for (int j = -brushSize / 2; j <= brushSize / 2; j++)
            {
                int pixelX = centerX + i;
                int pixelY = centerY + j;

                // 确保像素位置在画布范围内
                if (pixelX >= 0 && pixelX < renderTexture.width && pixelY >= 0 && pixelY < renderTexture.height)
                {
                    Vector2Int oldValue;
                    if(modifiedPixels.TryGetValue(new Vector2Int(pixelX, pixelY),out oldValue))
                    {
                        if (!isPen)
                        {
                            modifiedPixels.Remove(oldValue);
                        }

                    }
                    else
                    {
                        if (isPen)
                        {
                            modifiedPixels.Add(new Vector2Int(pixelX, pixelY));
                        }
                    }
                    
                }
            }
        }
    }

    // 返回被修改的像素位置集合
    public HashSet<Vector2Int> GetModifiedPixelPositions()
    {
        return modifiedPixels;
    }

    //void DrawCircle(int x, int y, int radius, Color color)
    //{
    //    for (int i = -radius; i <= radius; i++)
    //    {
    //        for (int j = -radius; j <= radius; j++)
    //        {
    //            if (i * i + j * j <= radius * radius)
    //            {
    //                int pixelX = x + i;
    //                int pixelY = y + j;

    //                if (pixelX >= 0 && pixelX < texture.width && pixelY >= 0 && pixelY < texture.height)
    //                {
    //                    texture.SetPixel(pixelX, pixelY, color);
    //                }
    //            }
    //        }
    //    }
    //    texture.Apply();
    //}

    public void SetColor(Color color)
    {
        currentColor = color;
        brushTexture = CreateBrushTexture(brushSize, currentColor);
    }

    public void SetRubber()
    {
        isPen = false;
        //brushTexture = CreateBrushTexture(brushSize, Color.clear);
    }

    public void SetPen()
    {
        isPen = true;
        //brushTexture = CreateBrushTexture(brushSize, currentColor);
    }

    public void ChangeBrushSize()
    {
        brushSize =(int)(controlSizeSlider.value * MaxBrushSize);
    }

    public void ClearDraw()
    {
            RenderTexture.active = renderTexture;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = null;
            modifiedPixels.Clear();
    }
}
