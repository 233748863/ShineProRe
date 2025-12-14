# ShineProCS - C# WPF 技能循环引擎

这是 ShinePro 技能循环引擎的 C# WPF 重写版本，采用现代化的 MVVM 架构和 Fluent Design 界面。

## ✨ 特性

- 🎯 **智能技能释放** - 基于优先级和冷却时间的自动技能循环
- 🎨 **现代化 UI** - 使用 HandyControl 的 Fluent Design 风格界面
- 📊 **实时监控** - 执行次数、响应时间等统计信息
- 🔔 **系统托盘** - 最小化到托盘，后台运行
- ⚙️ **配置管理** - JSON 配置文件，灵活自定义
- 🏗️ **MVVM 架构** - 清晰的代码结构，易于维护和扩展

## 🚀 快速开始

### 环境要求
- Windows 10/11
- .NET 8.0 SDK

### 运行程序
```bash
cd ShineProCS
dotnet run --project ShineProCS
```

### 编译项目
```bash
dotnet build
```

### 发布单文件
```bash
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:PublishTrimmed=true
```

发布后的文件位于：`ShineProCS/bin/Release/net8.0-windows/win-x64/publish/`

## 📁 项目结构

```
ShineProCS/
├── Core/                       # 核心逻辑
│   ├── Engine/                 # 技能循环引擎
│   ├── Interfaces/             # 接口定义
│   └── Services/               # 服务（配置管理等）
├── ViewModels/                 # MVVM 视图模型
├── Models/                     # 数据模型
├── Infrastructure/             # 基础设施（接口实现）
│   ├── Win32KeyboardInterface.cs
│   └── OpenCvImageInterface.cs
├── Resources/                  # 资源文件
└── config/                     # 配置文件
    ├── appsettings.json        # 应用配置
    └── skills.json             # 技能配置
```

## ⚙️ 配置说明

### 应用配置 (appsettings.json)
```json
{
  "DetectionRegion": [100, 200, 300, 400],  // 检测区域 [x, y, width, height]
  "ManaBarRegion": [500, 600, 200, 20],     // 蓝条区域
  "EnableSmartMode": true,                   // 启用智能模式
  "LoopInterval": 1000,                      // 循环间隔（毫秒）
  "LogLevel": 1                              // 日志级别
}
```

### 技能配置 (skills.json)
```json
[
  {
    "Name": "技能1 - Q键",
    "KeyCode": 81,          // 按键码（Q=81）
    "Cooldown": 5.0,        // 冷却时间（秒）
    "Priority": 1,          // 优先级（数字越大优先级越高）
    "Enabled": true         // 是否启用
  }
]
```

## 🎮 使用说明

1. **启动引擎** - 点击"启动"按钮开始技能循环
2. **暂停/恢复** - 点击"暂停"按钮暂停或恢复
3. **停止引擎** - 点击"停止"按钮停止运行
4. **系统托盘** - 关闭窗口会最小化到托盘，双击托盘图标恢复窗口
5. **退出程序** - 右键托盘图标选择"退出程序"

## 🛠️ 技术栈

- **.NET 8.0** - 最新的 .NET 平台
- **WPF** - Windows Presentation Foundation
- **HandyControl** - 现代化 UI 控件库
- **CommunityToolkit.Mvvm** - MVVM 工具包
- **OpenCvSharp4** - OpenCV 图像处理库
- **Hardcodet.NotifyIcon.Wpf** - 系统托盘支持

## 📝 代码特色

所有关键代码都添加了**详细的中文注释**，包括：
- C# 语法解释
- MVVM 模式说明
- 依赖注入概念
- XAML 数据绑定
- LINQ 查询语法

非常适合 C# 初学者学习和理解！

## 🔧 开发计划

- [x] 项目初始化
- [x] MVVM 架构设计
- [x] 配置管理系统
- [x] 技能循环引擎
- [x] Win32 按键模拟
- [x] OpenCV 图像接口
- [x] HandyControl 主题
- [x] 系统托盘功能
- [ ] 状态监测器（HP 检测、Buff 识别）
- [ ] 目标选择器
- [ ] 性能优化

## 📊 性能对比

| 指标 | Python 版本 | C# 版本 |
|------|------------|---------|
| 启动速度 | 1-2 秒 | <0.5 秒 |
| 内存占用 | 50-100MB | 20-40MB |
| 打包体积 | 50-150MB | 10-30MB |
| 性能 | 中等 | 优秀 |

## 📄 许可证

本项目仅供学习和研究使用。

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

## 📮 联系方式

如有问题或建议，请通过 Issue 联系。
