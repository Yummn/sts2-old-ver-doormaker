# 旧版门扉 / Old Ver. Doormaker

<!-- MOD-DOWNLOAD-ZH-START -->
## 中文下载与安装（先看这里）

- **旧版门扉**：请进入 [发布下载 / Releases](https://github.com/Yummn/sts2-old-ver-doormaker/releases)，展开 **Assets**，选择对应平台和游戏版本的中文安装包。
- **不要下载** Code → Download ZIP 或 Source code (zip/tar.gz)：那是源码，不含可直接安装的编译产物。
- 手动上传的源码压缩包已撤下，今后只发布安装包、说明和验证资料；项目代码仍保留在仓库。

### 本轮对应安装包

- [手机 v0.113.0【未通过，显示ERROR模型，不建议启用】](https://github.com/Yummn/sts2-old-ver-doormaker/releases/download/mobile-v113-pc-v111-20261117/OldVerDoormaker-1.1.0-Android-v113.0.zip)
- [电脑 v0.111.0（尚未实机回归）](https://github.com/Yummn/sts2-old-ver-doormaker/releases/download/mobile-v113-pc-v111-20261117/OldVerDoormaker-1.1.0-PC-v111.0.zip)

以上只是本轮构建目标，其他游戏版本请查看相应历史发布；不能混装手机和电脑版。旧文档中的版本号、源码附件指引如与本节冲突，以本节及对应Release说明为准。

### 电脑版安装

1. 退出游戏，解压**电脑版安装包**。
2. 将整个 `旧版门扉` 文件夹放到游戏根目录的 `mods` 文件夹中；没有 `mods` 就新建。
3. 重启游戏，在模组列表中启用。不要只把ZIP或源码目录放在游戏根目录。

```text
Slay the Spire 2/mods/旧版门扉/OldVerDoormaker.dll
Slay the Spire 2/mods/旧版门扉/OldVerDoormaker.json
```

手机版请使用对应游戏版本的启动器导入安装包，不要拿电脑版代替；保留原有存档和模组配置。

GitHub 自动生成的 Source code 链接不能单独删除，因此请认准上面的中文安装包链接。
<!-- MOD-DOWNLOAD-ZH-END -->

> 项目分类：兼容维护项目 / 《杀戮尖塔 2》Mod / 旧版 Boss 恢复 / 可直接使用

将早期版本的“门扉 / 造门者”Boss 战恢复到《杀戮尖塔 2》，并针对后续游戏版本重新做兼容处理。

## 下载与安装

1. 打开 [Releases](https://github.com/Yummn/sts2-old-ver-doormaker/releases)。
2. 下载与自己游戏平台和版本一致的 ZIP。
3. 启动器可直接导入完整 ZIP；手动安装时，将 ZIP 内的模组文件夹完整复制到游戏 `mods/` 目录。

不同版本的 DLL 不可混用。

## 兼容版本

- Android v0.103.2：使用文件名含“手机-v103”的历史包；
- PC v0.107.1：使用文件名含“电脑-v107.1”的 v1.1.0 包；
- Android v0.110.1：使用文件名含“手机-v110.1”的 v1.1.0 包。

## v1.1.0

v0.110.1 已移除核心 `DoormakerBoss` 遭遇模型，因此新版不能只重新编译旧 DLL。v1.1.0 将旧 Boss 遭遇改为模组内自包含模型，并完成：

- 重新加入荣耀角色的 Boss 发现顺序与候选池；
- 适配受保护模型 API；
- 适配 `PlayerChoiceContext` 能力指令；
- 适配阶段力量变化与备用站位；
- 将缺失的历史记录图标回退到现有门扉资源，避免资源加载错误。

REDMI K80 Pro / Android v0.110.1 已完成实机启动验证：4/4 Harmony 补丁成功，无补丁失败与接口/资源错误。

## 来源与署名

- 原模组：`Old Ver. Doormaker`
- 原作者：`noname`
- v0.103.2 修复与 v0.110.1 兼容改造：Yummn / Codex 协作维护

本仓库保留原作者署名。v0.110.1 版本依据原模组行为进行兼容重建，不声称拥有游戏原始美术、音频或文本资源。
