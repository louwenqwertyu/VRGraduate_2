using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.Networking;
using System.Text;
using System.Security.Cryptography;
using System.Security.Policy;
//using System.Security.Policy;
//using static System.Net.WebRequestMethods;
//using UnityEditor.PackageManager.Requests;

namespace MTT
{
    public class VuforiaTargetBody
    {
        public string name;
        public float width;
        public string image; //JPG 或 PNG 格式的 Base64 编码二进制图像文件
        public bool active_flag;
    }
    public class LocalPipeline : MonoBehaviour
    {
        //vuforia server访问密钥
        public string accessKey;
        public string secretKey;

        public string url = "https://vws.vuforia.com/targets";

        public void Run(string srcImg)
        {
            //将srcImg转化为能够上传的格式
            Texture2D targetImg = PathToData(srcImg);

            //保存和生成视频
            //Coroutine saveImage = StartCoroutine(_Run(srcImg));
            //StopCoroutine(saveImage);

            //将srcImg上传到服务器
            Coroutine upLoadToVuforia = StartCoroutine(UpLoadPictureToVuforia(targetImg, srcImg));

        }

        private IEnumerator _Run(string srcImg)
        {

            //PC端persistentDataPath = C:/Users/用户名/AppData/LocalLow/DefaultCompany/Painter
            string scanRoot = Path.Combine(Application.persistentDataPath, "Scans_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss"));

            Directory.CreateDirectory(scanRoot);



            string dstImg = Path.Combine(scanRoot, "input.png"); //保存输入图片
            string outDir = Path.Combine(scanRoot, "output"); //大模型处理后导出视频
            Directory.CreateDirectory(outDir);

            System.IO.File.Copy(srcImg, dstImg, true);
            //FindObjectOfType<PythonRunner>().RunScript(dstImg, outDir);
            yield break;
        }

        //将路径转为可发送vuforia的数据
        public Texture2D PathToData(string path)
        {
            //检查path是否为空
            if (path == null)
            {
                Debug.LogError("Error:文件路径是空的！");
                return null;
            }


            byte[] fileData = System.IO.File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2); // 创建一个Texture2D对象，大小会自动调整
            if (tex.LoadImage(fileData)) // 加载图片数据
            {
                Debug.Log("生成texture2D成功!");
                return tex;
            }
            else
            {
                Debug.LogError("图片格式不支持或加载失败");
                return null;
            }
        }

        public IEnumerator UpLoadPictureToVuforia(Texture2D tex, string srcPath)
        {
            Uri uri = new Uri(url);
            byte[] imageBytes = File.ReadAllBytes(srcPath);

            var reqBody = new VuforiaTargetBody
            {
                name = Path.GetFileNameWithoutExtension(srcPath),
                width = 10.0f,
                image = Convert.ToBase64String(imageBytes, Base64FormattingOptions.None),
                active_flag = true
            };
            string jsonBody = JsonUtility.ToJson(reqBody);
            
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

            string dateStr = DateTime.UtcNow.ToString("r"); //GMT格式时间
            string contentMD5 = GetMD5Base64(bodyRaw);
            string contentType = "application/json";
            string requestPath = uri.AbsolutePath;
            //string signature = ComputeSignature("POST", requestPath, contentType, dateStr, bodyRaw, secretKey);

            //计算signatrue
            var stringToSign = $"POST\n{contentMD5}\n{contentType}\n{dateStr}\n{requestPath}";
            string signature = ComputeHmacBase64(secretKey, stringToSign);


            UnityWebRequest www = new UnityWebRequest(url, "POST");
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Date", dateStr);
            www.SetRequestHeader("Content-MD5", contentMD5);
            www.SetRequestHeader("Authorization", $"VWS {accessKey}:{signature}");
            www.SetRequestHeader("Content-Type", contentType);


            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("上传成功：" + www.downloadHandler.text);
            }
            else
            {
                Debug.LogError("上传失败：" + www.error + " 详情：" + www.downloadHandler.text);

            }

        }

        public string GetMD5Base64(byte[] data)
        {
            using (var md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(data);
                return System.Convert.ToBase64String(hash);
            }
        }

        //错误的获取signature函数，留下供以后研究为何出错
        //private string GetSignature(string stringToSign, string secretKey)
        //{
        //    var encoding = new ASCIIEncoding();
        //    byte[] keyBytes = encoding.GetBytes(secretKey);
        //    byte[] stringBytes = encoding.GetBytes(stringToSign);
        //    using (HMACSHA1 hmac = new HMACSHA1(keyBytes))
        //    {
        //        byte[] hashBytes = hmac.ComputeHash(stringBytes);
        //        return Convert.ToBase64String(hashBytes);
        //    }
        //}

        public string ComputeSignature(
        string httpVerb,
        string requestPath,       // 仅路径，如 "/targets" 或 "/targets/{id}"（如有查询，按实际拼上）
        string contentType,       // 无则传空字符串 ""
        string dateRfc1123,       // 必须与请求头 Date 一致，如 DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture)
        byte[]? body,             // GET/DELETE 传 null 或空
        string serverSecretKey)   // Vuforia Server Secret Key
        {
            var contentMd5 = GetMD5Base64(body);
            //stringToSign的请求结构
            //HTTP - Verb + "\n" + --HTTP - Verb为HTTP方法，上传使用POST
            //Content - MD5 + "\n" + --MD5值
            //Content - Type + "\n" + --请求内容的类型
            //Date + "\n" + --日期
            //Request - Path";  --请求路径，为/targets

            var stringToSign = $"{httpVerb}\n{contentMd5}\n{contentType}\n{dateRfc1123}\n{requestPath}";
            using var hmac = new HMACSHA1(Encoding.ASCII.GetBytes(serverSecretKey));
            var sigBytes = hmac.ComputeHash(Encoding.ASCII.GetBytes(stringToSign));
            return Convert.ToBase64String(sigBytes);
        }


        public static string ComputeHmacBase64(string key, string data)
        {
            using (var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(key)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                return Convert.ToBase64String(hash);
            }
        }
        
    }
}
