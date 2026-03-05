using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// 一直展示画布
/// </summary>
public class CanvasControl : MonoBehaviour
{
    private Vedio _video;
    public Slider slider;
    [SerializeField] private SliderEvent sliderEvent;
    [SerializeField] private Image videoIconImage;
    [SerializeField] private GameObject drawPanel;
    [SerializeField] private GameObject videoControlPanel;
    [SerializeField] private CompareVideo compareVideo;
    [SerializeField] private Painter painter;
    [SerializeField] private GameObject FinishWinPanel;
    [SerializeField] private GameObject FinishLosePanel;
    [SerializeField] private GameObject ButtonPanel;

    private bool isPause=true;
    private GameObject ARCamera;
    private VideoPlayer videoPlayer;

    private bool isOnDraw = false;

    HashSet<Vector2Int> needToDrawPixels = new HashSet<Vector2Int>();

    private void Start()
    {
        ARCamera = Camera.main.gameObject;
    }

    public void TargetFound(Vedio video)
    {
        _video = video;
        sliderEvent.toPlayVideo = video;
        isPause = false;
        videoPlayer = video.GetComponent<VideoPlayer>();

        videoControlPanel.gameObject.SetActive(true);
    }

    public void TargetLost()
    {
        videoControlPanel.gameObject.SetActive(false);
    }

    public void VideoNext()
    {
        _video.SetVedioNext();
        needToDrawPixels = compareVideo.GetVideoChangePixels(videoPlayer);
        if (isOnDraw)
        {
            painter.ClearDraw();
        }

    }

    public void VideoLast()
    {
        _video.SetVedioLast();
    }

    public void OnClickPauseButton()
    {
        if (isPause)
        {
            VideoResum();
        }
        else
        {
            VideoPause();
        }
    }

    public void VideoPause()
    {
        videoPlayer.Pause();
        isPause = true;
    }

    public void VideoResum()
    {
        videoPlayer.Play();
        isPause = false;
    }

    public void OnClickDrawButton()
    {
        if (!isOnDraw)
        {
            drawPanel.SetActive(true);
            VideoPause();
            //compareVideo.GetVideoChangePixels(videoPlayer);
            VideoNext();
            needToDrawPixels = compareVideo.GetVideoChangePixels(videoPlayer);
            ARCamera.SetActive(false);
            ButtonPanel.SetActive(true);

            isOnDraw = true;
        }
    }

    public void ReturnARCamera()
    {
        drawPanel.SetActive(false);
        VideoResum();
        ARCamera.SetActive(true);
        isOnDraw = false;
        ButtonPanel.SetActive(false);
    }

    public void OnClickFinishButton()
    {
        HashSet<Vector2Int> drawPixels= painter.GetModifiedPixelPositions();

        int overlapCount = needToDrawPixels.Count(drawPixels.Contains);

        float percent = overlapCount / (float)needToDrawPixels.Count;

        float compareCount = drawPixels.Count / (float) needToDrawPixels.Count;

        if (percent > 0.8f&&compareCount<1.2f&&compareCount>0.8f)
        {
            FinishWinPanel.gameObject.SetActive(true);
            Invoke("CloseFinishWinPanel", 1f);
        }
        else
        {
            FinishLosePanel.gameObject.SetActive(true);
            Invoke("CloseFinishLosePanel", 1f);
        }
        Debug.Log($"percent:{percent}");
    }

    public void CloseFinishWinPanel()
    {
        FinishWinPanel.gameObject.SetActive(false);
        VideoNext();
    }

    public void CloseFinishLosePanel()
    {
        FinishLosePanel.gameObject.SetActive(false);
    }
}
