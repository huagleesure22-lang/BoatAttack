# 海岛场景 · Unity 2019

基于 Unity 官方 BoatAttack `release/2019.3` 分支，源提交 `d5698516c65793971c1064cedd4cd75f19a70dda`。工程要求 **Unity 2019.3.0f6**，URP **7.2.1**。这是从官方 2019 工程单独制作的场景，不是把 2020 工程的版本号改成 2019。

## 打开

1. 解压后，在 Unity Hub 中添加包含 `Assets`、`Packages`、`ProjectSettings` 的目录。
2. 选择 **Unity 2019.3.0f6**，等待依赖包下载与资源导入。包配置保持官方 2019 分支原值；首次打开需要联网。
3. 打开 `Assets/scenes/island_clean_2019.unity`，或顶部菜单 **Island2019 → Open Clean Island**。
4. Scene 视图用于浏览、编辑海岛；Play 使用原静态海岛相机预览。此交付包含海岛、房屋、海水、光照，不含新增人物、战斗或灯塔任务。

如果同学安装的是其他 2019 小版本，请先确认完整版本号。这里的准确目标是 2019.3.0f6，尚未验证其他编辑器版本。

## 修改

- 新场景引用 `Assets/Island2019/Island_WithoutBoats.prefab`，从该副本直接移除 `Logic` 与 `Dynamic Objects` 实例，不会生成赛艇，也不会显示帆板等动态海上道具。
- 它引用 `Environment_WithoutFloatingProps.prefab`，从该环境副本直接移除 `Props_Floating` 实例，包括浮标、皮划艇与其他海面漂浮道具。
- 保留岛屿地形、房屋、植被、海水、天空、光照、环境声音和原静态相机。
- 原官方场景和预制体保持原样。新场景是唯一启用的构建场景，旧赛车场景保留但未启用。
- 清除新场景引用的旧遮挡剔除数据，避免移除后的道具继续影响可见性。

## 下载与压缩

普通双击根目录的 **Prepare-Island2019.cmd**。v2 使用 GitHub Desktop 的 Git，先复制 `D:\github` 已有的 Git 数据，随后通过 HTTP/1.1 只获取 `island-clean-2019.3` 的浅层快照，避免重复下载整条历史；复用原目录的 LFS 缓存，并以两个并行连接下载缺失的 2019 资源，随后检查资源完整性。它不修改原来的 2020 项目，不需要管理员权限，也不需要在准备阶段安装 Unity 2019。

新目录固定为 `D:\BoatAttack-Island2019-Prepared`（没有 D 盘时使用当前用户 Downloads）。网络失败后，成功完成的 Git 对象及 LFS 文件会留在该目录。切换到可用网络后再次运行同一个 v2 工具即可继续；进行中的 Git 传输不保证按字节续传。工具仅复用带有自身标记的目录，并在已经检出的项目有本地修改时停止；不会对原项目 reset、clean 或强制覆盖。后续运行使用正常增量 fetch 和 fast-forward 更新，以保留浅仓库的已知提交关系。每次成功打包使用新的随机 ZIP 名称，不覆盖旧压缩包。

HTTP/1.1 和较小下载量不能保证修复本机的网络重置。若仍报 `Connection was reset`，需要检查本机网络或代理；可以换手机热点后再运行，不要关闭 TLS 验证。此前 v1 随机目录下载未完成时，v2 会创建自己的独立目录，不会擅自删除失败目录。

完成后，会在新工程旁生成一个 ZIP，包含 `Assets`、`Packages`、`ProjectSettings`、本说明和原 LICENSE。这个 ZIP 可直接发给同学。它不包含 Library、Temp、Git 数据或 Unity 编辑器；解压后的首次依赖包导入仍需网络。如果云端 GitHub ZIP 下载只包含 LFS 指针，请使用此工具或 GitHub Desktop 克隆分支，而不要把指针当作真实模型、贴图。

看到 **DONE** 仅表示文件下载、LFS 完整性检查和压缩完成，不等于 Unity 已编译或试玩通过。若显示 ERROR，请保留窗口并截图，不要把不完整的项目发出。

## 验证状态

已验证：官方 2019 版本和原依赖配置保留；152 个 LFS 文件完整性校验通过；新场景与预制体的 YAML、对象 ID、父子引用和资源 GUID 已检查；新场景依赖链不包含 Logic、Dynamic Objects 或 Props_Floating；原始资源没有改动。

云端未安装并激活 Unity 2019 编辑器，尚未完成该版本的整体导入、编译、海水渲染或 Play Mode 验证。请在同学本机的 Unity 2019.3.0f6 中验证；发生错误时私下提供新的 Editor.log，不要公开日志中的登录参数。
