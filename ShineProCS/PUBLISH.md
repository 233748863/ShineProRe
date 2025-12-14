# 打包发布指南

## 发布配置

### 方法 1: 单文件发布（推荐）
生成一个独立的可执行文件，包含所有依赖。

```bash
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:PublishTrimmed=true
```

**参数说明**:
- `-c Release`: 使用 Release 配置（优化性能）
- `-r win-x64`: 目标平台为 Windows x64
- `--self-contained`: 包含 .NET 运行时（用户无需安装 .NET）
- `-p:PublishSingleFile=true`: 打包为单个文件
- `-p:PublishTrimmed=true`: 裁剪未使用的代码（减小体积）

**输出位置**: `ShineProCS/bin/Release/net8.0-windows/win-x64/publish/ShineProCS.exe`

### 方法 2: 依赖框架发布
生成较小的文件，但需要用户安装 .NET 8.0 运行时。

```bash
dotnet publish -c Release -r win-x64 --no-self-contained
```

**输出位置**: `ShineProCS/bin/Release/net8.0-windows/win-x64/publish/`

### 方法 3: ReadyToRun 优化
提升启动速度，但会增加文件大小。

```bash
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:PublishReadyToRun=true
```

## 发布后的文件结构

### 单文件发布
```
publish/
├── ShineProCS.exe          # 主程序（约 60-80MB）
└── config/                 # 配置文件（需要手动复制）
    ├── appsettings.json
    └── skills.json
```

### 依赖框架发布
```
publish/
├── ShineProCS.exe          # 主程序（约 200KB）
├── ShineProCS.dll
├── *.dll                   # 依赖库
└── config/                 # 配置文件
```

## 配置文件处理

发布后需要手动复制配置文件到发布目录：

```bash
# Windows PowerShell
Copy-Item -Path "config" -Destination "ShineProCS/bin/Release/net8.0-windows/win-x64/publish/" -Recurse
```

或者在项目文件中配置自动复制：

```xml
<ItemGroup>
  <None Update="config\**\*">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    <CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>
  </None>
</ItemGroup>
```

## 优化建议

### 1. 减小文件大小
在 `.csproj` 文件中添加：

```xml
<PropertyGroup>
  <PublishTrimmed>true</PublishTrimmed>
  <TrimMode>link</TrimMode>
  <EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
</PropertyGroup>
```

### 2. 提升启动速度
```xml
<PropertyGroup>
  <PublishReadyToRun>true</PublishReadyToRun>
  <PublishReadyToRunShowWarnings>true</PublishReadyToRunShowWarnings>
</PropertyGroup>
```

### 3. 包含调试符号（用于调试）
```bash
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:DebugType=embedded
```

## 发布检查清单

- [ ] 编译无错误无警告
- [ ] 所有功能测试通过
- [ ] 配置文件正确
- [ ] 图标文件存在（如果有）
- [ ] README 文档完整
- [ ] 版本号更新

## 分发建议

### 创建安装包
可以使用以下工具创建安装程序：
- **Inno Setup** - 免费，功能强大
- **WiX Toolset** - 创建 MSI 安装包
- **Advanced Installer** - 商业软件，界面友好

### 压缩分发
```bash
# 使用 7-Zip 压缩
7z a ShineProCS_v1.0.0.zip ShineProCS/bin/Release/net8.0-windows/win-x64/publish/*
```

## 版本管理

建议在 `.csproj` 文件中设置版本信息：

```xml
<PropertyGroup>
  <Version>1.0.0</Version>
  <AssemblyVersion>1.0.0.0</AssemblyVersion>
  <FileVersion>1.0.0.0</FileVersion>
  <Copyright>Copyright © 2025</Copyright>
  <Company>ShinePro</Company>
  <Product>ShinePro 技能循环引擎</Product>
</PropertyGroup>
```

## 常见问题

### Q: 发布后程序无法启动？
A: 检查是否缺少配置文件或依赖库。

### Q: 文件太大怎么办？
A: 使用 `PublishTrimmed` 和 `TrimMode=link` 裁剪未使用的代码。

### Q: 需要管理员权限吗？
A: 通常不需要，除非使用了需要管理员权限的功能。

### Q: 如何更新程序？
A: 直接替换可执行文件即可，配置文件会保留。
