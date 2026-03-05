# 使用提示
1.aipainting的性能问题

  原先的参数很吃性能，降低生成质量的办法是找到Scripts/Aipainting/aipainting.py,打开并找到以下代码：
  
  STROKES_PER_BLOCK =3  
  
  REPEAT_CANVAS_HEIGHT = 3 
  
  REPEAT_CANVAS_WIDTH = 3
  
  第一个代表每个绘画格子的笔画术，第二和第三个代表生成出的视频帧图片的长和宽。
  
  降低这三个参数的值，可以显著减轻算力使用。
  
  fix版本将这三个值从10，10，10调整为3，3，3


# 需要完成的其它工作

# 目前需要实现的功能
  1.feat:用户能够上传图片

  2.feat：用户上传的图片能够被AR识别
    解决方案：
    a.vuforia
    b.AR foundation

  3.feat:用户上传的图片自动进行AI加工

  4.fix:修正程序bug
  
    a.图片识别后在屏幕中出现其它图片遮挡

  5.feat:生成的视频可导出

  6.feat:用户可调节生成视频的参数

  other.remake：替代vuforia的更优框架

# 如何合作？
初始化：完成github的基础部署后，从Main分支pull下来源文件，这将作为以后你的本地仓库。

创建工作分支：不要在main分支上直接推送代码，而是创建新分支。一个新分支完成一项功能，完成该功能后便对主分支发起merge合并请求

合并和建立新工作：merge成功后，新分支便可删除。随后以main分支为基础创建一个新的分支，这样可以同步工作进度，然后进行新的工作

