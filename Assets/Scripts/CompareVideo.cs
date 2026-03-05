using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class CompareVideo : MonoBehaviour
{
    public Color targetColor = Color.red;

    public RawImage rawImage;
    private Texture2D currentFrame;
    private Texture2D prevFrame;
    private Texture2D resultTexture;

    public HashSet<Vector2Int> GetVideoChangePixels(VideoPlayer source)
    {
        currentFrame = new Texture2D((int)source.targetTexture.width, (int)source.targetTexture.height);
        RenderTexture.active = (RenderTexture)source.targetTexture;
        currentFrame.ReadPixels(new Rect(0, 0, source.targetTexture.width, source.targetTexture.height), 0, 0);
        currentFrame.Apply();

        HashSet<Vector2Int> changePixels = new HashSet<Vector2Int>();

        if (prevFrame != null)
        {
            changePixels = CompareFrames();
        }

        prevFrame = currentFrame;

        //赋值完才能画
        rawImage.texture = resultTexture;

        return changePixels;
    }

    private HashSet<Vector2Int> CompareFrames()
    {
        Color32[] prevPixels = prevFrame.GetPixels32();
        Color32[] currPixels = currentFrame.GetPixels32();
        Color32[] newPixels = currPixels;
        HashSet<Vector2Int> changedPixels = new HashSet<Vector2Int>();
        resultTexture = new Texture2D(prevFrame.width, prevFrame.height);

        for (int i = 0; i < prevPixels.Length; i++)
        {
            if (!ColorEquals(prevPixels[i], currPixels[i]))
            {
                newPixels[i] = targetColor;
                int x = i % prevFrame.width;
                int y = i / prevFrame.width;
                changedPixels.Add(new Vector2Int(x, y));
            }
        }
        resultTexture.SetPixels32(newPixels);
        resultTexture.Apply();

        Debug.Log($"变化像素数: {changedPixels.Count}");
        return changedPixels;
    }

    bool ColorEquals(Color32 a, Color32 b, byte threshold = 5)
    {
        return Mathf.Abs(a.r - b.r) < threshold &&
               Mathf.Abs(a.g - b.g) < threshold &&
               Mathf.Abs(a.b - b.b) < threshold;
    }

    private void OnDestroy()
    {
        if (prevFrame != null) Destroy(prevFrame);
        if (currentFrame != null) Destroy(currentFrame);
        if (resultTexture != null) Destroy(resultTexture);
    }
}
