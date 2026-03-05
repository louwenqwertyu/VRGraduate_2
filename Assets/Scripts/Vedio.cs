using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class Vedio : MonoBehaviour
{
    private VideoClip videoClip;         // 视频的文件 参数

    public Slider videoTimeSlider;      // 视频的时间 Slider

    public long OnePaintTime;//一笔所用时间

    //定义参数获取VideoPlayer组件和RawImage组件

    internal VideoPlayer videoPlayer;

    private RawImage rawImage;

    private bool isPlay;

    // Use this for initialization

    void Start()

    {
        string a;

        //获取场景中对应的组件

        videoPlayer = this.GetComponent<VideoPlayer>();

        rawImage = this.GetComponent<RawImage>();

        clipHour = (int)videoPlayer.clip.length / 3600;

        clipMinute = (int)(videoPlayer.clip.length - clipHour * 3600) / 60;

        clipSecond = (int)(videoPlayer.clip.length - clipHour * 3600 - clipMinute * 60);

    }

    public void VideoPlay()
    {
        videoPlayer.time = 0;
        videoPlayer.Play();
        isPlay = true;
    }

    // Update is called once per frame

    void Update()

    {

        //如果videoPlayer没有对应的视频texture，则返回

        //if (videoPlayer.texture == null)

        //{

        //    return;

        //}

        ////把VideoPlayerd的视频渲染到UGUI的RawImage

        //rawImage.texture = videoPlayer.texture;

        if (isPlay)
        {
            ShowVideoTime();
        }

    }

    /// <summary>

    /// 显示当前视频的时间

    /// </summary>

    private void ShowVideoTime()

    {

        // 当前的视频播放时间

        currentHour = (int)videoPlayer.time / 3600;

        currentMinute = (int)(videoPlayer.time - currentHour * 3600) / 60;

        currentSecond = (int)(videoPlayer.time - currentHour * 3600 - currentMinute * 60);

        // 把当前视频播放的时间显示在 Text 上


        // 把当前视频播放的时间比例赋值到 Slider 上

        videoTimeSlider.value = (float)(videoPlayer.time / videoPlayer.clip.length);

    }

    /// <summary>

    /// 当前的 Slider 比例值转换为当前的视频播放时间

    /// </summary>

    private void SetVideoTimeValueChange()
    {

        videoPlayer.time = videoTimeSlider.value * videoPlayer.clip.length;

    }

    // 当前视频的总时间值和当前播放时间值的参数

    private int currentHour;

    private int currentMinute;

    private int currentSecond;

    private int clipHour;

    private int clipMinute;

    private int clipSecond;


    public void SetVedioNext()
    {
        videoPlayer.frame += OnePaintTime;
    }

    public void SetVedioLast()
    {
        videoPlayer.frame -= OnePaintTime;
    }
}
