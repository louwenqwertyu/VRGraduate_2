using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MTT;

public class UploadButton : MonoBehaviour
{
    [SerializeField]LocalPipeline pipeline;
    private void Start()
    {
        pipeline = GetComponent<LocalPipeline>();
    }
    public void OnBtnClick()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
       NativeGallery.GetImageFromGallery((path)=>
       {
           if (!string.IsNullOrEmpty(path) ) pipeline.Run(path);
       });
#else
        string path = UnityEditor.EditorUtility.OpenFilePanel("choose picture:", "", "png,jpg");
        if (!string.IsNullOrEmpty(path) ) pipeline.Run(path);
#endif

    }
}
