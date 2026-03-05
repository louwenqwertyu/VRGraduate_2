目前所有进度位于场景 Scenes/TestScene

# 主要gameObject介绍：

**Canvas**：上传图片的按钮和代码

**CloudImageTarget1**和**Cloud Recognition**为云识别功能物件，实现云识别功能。

**ARCamera**是vuforia基本相机

# 配置：

在CloudImageTarget1的Inspector界面，将image Target Behaviour组件下的Type改为CloudReco，这是云识别模式。

其余代码已经配置完毕，但需要使用自己的云数据库，以下是创建流程：

1.  注册一个vuforia海外账号，国区账号不能创建云识别数据库（哪怕是香港也不行）。
2.  在MyAccount ->Target Manager这里，选择Generate Database，创建数据库。数据库类型选择Cloud。
3.  进入新创建的数据库，点开Database Access Keys，得到四个密钥数据，分为服务器密钥对和客户端密钥对
4.  将客户端密钥对粘贴到Cloud Recognition的Inspector窗口中的AccessKey和SercetKey

完成上述步骤后，你在云数据库中创建的新的target后，只要target的status为active，就能够在unity中识别到这个图片。但是，由于识别需要联网，因此会存在延迟或识别不到的问题，需要多次尝试。目前在物件上挂载了一个白色正方体，当识别成功时屏幕会显示该立方体。

# 有关上传图片的功能：

主要代码挂载在Canvas下的Button中。

分为LocalPipeline和UploadButton两个代码，前者处理上传、保存、处理图片的逻辑，后者处理用户点击按钮时的事件。

上传图片的重要参数为服务器密钥对，如果要尝试在unity端将图片上传到云数据库，务必将自己的服务器accesskey和服务器secretkey复制到LocalPipeline下的accesskey和secretkey中，这样它才能访问到对应的云数据库。（另外，服务器secretKey不应泄露）

url是vuforia的网址。

上传图片，需要VWS的认证头Authentication和记录了请求内容的JSON文件，由于VWS是vuforia独一家的玩意，有一套自己的认证头生成方式，参见https://developer.vuforia.com/library/vuforia-engine/web-api/vuforia-web-api-authentication/和https://developer.vuforia.com/library/vuforia-engine/web-api/cloud-targets-web-services-api/，前者是身份认证的官方说明文档，后者是使用云识别各项功能所需的JSON文件的说明。

目前，在LocalPipeline里面已经配置了生成认证头、JSON文件和发送网络请求的方法UpLoadPictureToVuforia，但无法成功上传图片。