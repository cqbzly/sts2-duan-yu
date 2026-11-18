# Steam 创意工坊工作区与上传

本项目使用 Mega Crit 官方 [sts2-mod-uploader](https://github.com/megacrit/sts2-mod-uploader) 上传《杀戮尖塔2》模组。默认创建 **private** 测试条目；上传使用本机 Steam 客户端的当前登录账号，与 GitHub 的 `cqbzly` 账号独立。

## 准备工作区

在项目根目录运行：

```powershell
.\scripts\prepare-workshop.ps1 -Sts2Path 'D:\steam\steamapps\common\Slay the Spire 2'
```

已有当前版本的 Release 安装包时可传入 `-SkipBuild`。默认封面是 `assets/workshop-preview.png`，也可以通过 `-PreviewPath` 指定。封面必须是小于1,000,000字节的有效PNG。

脚本根据模组清单自动填入版本、最低游戏版本与 BaseLib 版本，并生成：

```text
dist/workshop/
  content/
    CQBZLY_DUAN_YU.dll
    DuanYu.json
  workshop.json
  image.png
  mod_id.txt             上传成功后由官方工具创建
```

只有`content`目录会作为模组载荷上传；清单与DLL直接放在该目录。当前游戏的`ModManager.ReadModsInDirRecursive`也支持嵌套目录，但本工作区使用更直接的结构。发布脚本拒绝`content`中的多余文件或目录，避免混入游戏程序集、开发工具、日志或存档。

脚本保留已有`mod_id.txt`，不会清空工坊工作区。首次成功条目ID同时记录在`workshop/published-item.json`；重新生成工作区时可恢复该ID，避免意外重复创建。只有条目所有者可以更新；其他作者若要发布自己的分支，应使用自己的条目记录。`dist`不纳入源码仓库。

## 上传与更新

确认 Steam 客户端已启动并登录目标账号，然后运行：

```powershell
.\.tools\mod-uploader\ModUploader.exe upload -w "$PWD\dist\workshop"
```

首次没有`mod_id.txt`时会创建条目；以后同一命令更新该条目。需要恢复已有条目时，可使用官方`-i <工坊数字ID>`参数，必须先核对它属于本项目和当前Steam账号。不要因一次网络错误就删除已有ID重建条目。

官方工具成功时输出`Successfully uploaded`并保存工坊ID。若创建条目后、上传全部完成前失败，工具可能还没有写入`mod_id.txt`；此时从日志中保存已创建的ID，核对条目后用`-i`续传，避免创建重复条目。日志位于运行目录的`mod-uploader.log`，其中可能包含Steam昵称等本地信息，不应直接提交到源码仓库。

`workshop.json`显式声明工坊依赖`3737335127`（BaseLib），与模组清单中`BaseLib >= 3.4.8`配套。Compatibility Helper由BaseLib工坊包提供。`minBranch`和`maxBranch`均设置为`public-beta`；官方模板提示该API可能有异常行为，因此上传后仍需在条目页面核对支持分支。项目只声明已验证的游戏版本，未验证正式分支和其他模组组合。

官方工具如报告仍需接受 Steam Workshop 协议，须由账号持有人处理该账号步骤；上传退出码0不等于条目已经可以公开显示。私有条目上传后还需要订阅回装与加载验证，再决定是否改变可见性。

## 已下载工具的来源

- 官方发行页：[v0.2.0](https://github.com/megacrit/sts2-mod-uploader/releases/tag/v0.2.0)。
- Windows x64包：`ModUploader-win-x64.zip`，6,011,955字节。
- SHA256：`2b55c19cc5932235ca9dbd663ca07d04e5d5a0018402303199f9b4ec8ca06578`，与官方GitHub release asset的`digest`一致。
- 下载位置：`.tools/mod-uploader/ModUploader.exe`；该目录被Git忽略，发行包不附带上传器。
- `--help`和`upload --help`已验证。发行标签为v0.2.0，但二进制`--version`显示`1.0.0+4c4db1262cdce6a1c4550a494f2e1088dbeda346`。
- `ModUploader.exe`没有Authenticode签名；随包的`steam_api64.dll`具有有效Valve签名。哈希匹配证明下载内容与官方发行资源一致，不把无签名EXE描述为已签名软件。

本文件描述准备与验证方法，不单凭工具下载、脚本成功或工作区存在宣称已经上传。实际条目链接与上传结果应以当次成功输出为准。
