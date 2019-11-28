# Uni Studio

## 快速开始

1. [下载 actipro-wpf-controls](https://unirpa.coding.net/p/UniStudio/attachment) 依赖库压缩包文件并解压安装
2. Clone 项目代码到本地，通过 Visual Studio 打开解决方案
3. 根据依赖库本地安装位置，调整 `StudioClient` 项目的 `ActiproSoftware` 相关“引用项”路径位置
4. 设置 `StudioClient` 项目为启动项目
5. 开始调试……



## 仓库目录

```powershell
├─CustomActivities		# 统一管理自定义 Activity （每个自定义 Activity 置于 一个文件夹内）
│  └─SendMail			# 自定义 SendMail Activity（示例）
└─StudioClient			# 客户端程序，主要包含用户界面及交互逻辑
    ├─Config
    ├─ExpressionEditor	# 表达式编辑器，用于自定义 Designer 智能提示
    ├─Model				# Model 类
    ├─Resources			# 资源目录
    │  ├─Images			# 图片资源
    │  ├─NuGet			# NuGet 工具及 install 目录
    │  └─Template		# 用于存放各类工作流模板
    ├─Utils				# 公共工具类
    └─Views				# 用户界面及交互逻辑
```

