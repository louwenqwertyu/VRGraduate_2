using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// ¸úËæÍ¼Æ¬³öÏÖ»­²¼
/// </summary>
public class ImageTargetControl : MonoBehaviour
{
    [SerializeField] private Vedio video;
    [SerializeField] private VideoPlayer videoPlayer;

    private DefaultObserverEventHandler defaultObserverEventHandler;
    private GameObject UICanvas;
    private CanvasControl canvasControl;
    void Start()
    {
        videoPlayer.Stop();

        defaultObserverEventHandler = GetComponent<DefaultObserverEventHandler>();
        defaultObserverEventHandler.OnTargetFound.AddListener(OnTargetFound);
        defaultObserverEventHandler.OnTargetLost.AddListener(OnTargetLost);

        UICanvas = GameObject.FindGameObjectWithTag("Canvas");
        canvasControl = UICanvas.GetComponent<CanvasControl>();
    }

    private void OnTargetFound()
    {
        //videoPlayer.gameObject.SetActive(true);
        video.videoTimeSlider = canvasControl.slider;
        canvasControl.TargetFound(video);
        video.VideoPlay();
    }

    private void OnTargetLost()
    {
        //videoPlayer.gameObject.SetActive(false);
        canvasControl.TargetLost();
    }
}
