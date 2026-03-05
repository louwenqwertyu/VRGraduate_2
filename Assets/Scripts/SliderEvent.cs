using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SliderEvent : MonoBehaviour, IDragHandler
{

        [SerializeField]

        public Vedio toPlayVideo;        // 视频播放的脚本

        /// <summary>

        /// 给 Slider 添加 拖拽事件

        /// </summary>

        /// <param name="eventData"></param>

        public void OnDrag(PointerEventData eventData)

        {

            SetVideoTimeValueChange();

        }

        /// <summary>

        /// 当前的 Slider 比例值转换为当前的视频播放时间

        /// </summary>

        private void SetVideoTimeValueChange()

        {

            toPlayVideo.videoPlayer.time = toPlayVideo.videoTimeSlider.value * toPlayVideo.videoPlayer.clip.length;

        }
 }
