# MilkVio ACR for PromeRotation API12 (TC)

MilkVio 全职业 ACR，基于 PromeRotation API12 繁中服。

## 支持职业

| 职能 | 职业 |
|---|---|
| Tank | PLD, WAR, DRK, GNB |
| Healer | WHM, SCH, AST, SGE |
| Melee DPS | MNK, DRG, NIN, SAM, RPR, VPR |
| Ranged DPS | MCH, DNC |
| Caster DPS | SMN, RDM, PCT |

## 开发 / 编译

### 依赖

本项目通过 NuGet 引用编译期 SDK：

```xml
<PackageReference Include="PromeRotation.SDK.API12" Version="0.1.0-preview.2" />
```

**不再需要**手动下载 Dalamud DLL 或提交 `lib/` 目录下的编译引用 DLL。`dotnet restore` 会自动恢复所有编译引用。

### 本地编译

```powershell
dotnet restore MilkVio\MilkVio.csproj
dotnet build MilkVio\MilkVio.csproj -c Release
```

如需直接输出到本地 XIVLauncherTC ACR 目录，可通过 `-p:OutputPath=` 覆盖：

```powershell
dotnet build MilkVio\MilkVio.csproj -c Release -p:OutputPath="C:\Users\<你的用户名>\AppData\Roaming\XIVLauncherTC\pluginConfigs\PromeRotation\ACR\MilkVio"
```

或者创建 gitignored 的 `Directory.Build.props.user` 文件设置本地路径。

### 更新 SDK 版本

1. 修改 `MilkVio\MilkVio.csproj` 中的 `PromeRotation.SDK.API12` 版本号
2. **先提交 SDK 版本变更**（不要与发布 Tag 混在同一个提交里）
3. 确认 NuGet 能成功恢复后，再创建发布 Tag

不推荐使用浮动版本（`*` / `0.1.0-preview.*`）。始终固定为具体版本号。

## 自动发布

推送以 `v` 开头的版本 Tag 即可触发 GitHub Actions 自动构建发布：

```powershell
git tag v1.5.2.0
git push origin v1.5.2.0
```

也可以在 GitHub Actions 页面手动触发（`workflow_dispatch`），输入版本号。

发布流程：
1. 检出对应提交
2. 安装 .NET 9
3. NuGet 恢复（自动获取 `PromeRotation.SDK.API12`）
4. 自动读取 `PromeRotation.dll` 程序集版本作为 `referencePromeVersion`
5. 同步 `GlobalVersion.cs` 中的版本号
6. 编译 ACR
7. 打包为 `latest.zip`
8. 计算 SHA256
9. 生成 `repo.json`
10. 创建 GitHub Release

## repo.json 使用

将以下链接填入 PromeRotation 远程 ACR 配置界面：

```
https://github.com/MilkVio/MirukuACR/releases/latest/download/repo.json
```
