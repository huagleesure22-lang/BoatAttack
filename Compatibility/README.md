# 复用现有 2020 工程的 2019 适配补丁

下载本目录的 **BoatAttack-2020-to-2019.3.zip**，完整解压后普通运行 `Convert-2020-to-2019.cmd`。该小补丁复用本机现有工程，准备过程不运行 Git，也不下载整套海岛资源。

默认源目录为 `D:\github-Unity2020-28206-10141`；源工程须为已下载完整资源的 Unity 2020.3.23f1 BoatAttack。工具复制模型、贴图、材质与环境预制体到新目录，再应用官方 Unity 2019.3 的兼容代码、Shader Graph、URP 7.2.1、水系统、地形与配置。原工程不改动。

新版格式的地形、烘焙数据与脚本不能通过只改 ProjectVersion 可靠降级。新副本换用 2019 原生地形，清除不兼容的旧烘焙场景，主要布局沿用 2020 静态海岛，但地形细节和光照可能不同。目标仅为去掉船和浮标的海岛，不含人物与战斗逻辑。

目标编辑器为 **Unity 2019.3.0f6**；打开 `Assets/scenes/island_clean_2019.unity` 或菜单 **Island2019 → Open Clean Island**。Gameplay Ingredients 2019.3.0 源代码及许可证嵌入工程，避免另行 Git 下载；Unity 注册表依赖首次导入仍可能需要网络。

本补丁以普通 Git 文件保存，浏览器 **Download raw file** 下载的就是 ZIP 二进制，不是 LFS 指针。已检查补丁 CRC、文件哈希、复制后版本/依赖、场景对象引用和不兼容二进制格式的排除。Windows CMD 与 Unity 2019 完整导入、编译、材质渲染仍需在本机验证，不能把生成 ZIP 当成已验证可以运行。

## 原创数字资产与第三人称战斗附加包

已有可打开的 2019.3.0f6 海岛工程后，下载 **IslandQuest2019-Assets-and-Combat.zip**，全部解压并运行 `Install-IslandQuest2019.cmd`。默认安装到 `D:\BoatAttack-2019-From2020-28614-30466`，仅增加新资产文件夹。运行编辑器菜单 **Island2019 → 1 - Build Assets and Gallery** 生成主人公、骷髅、宝箱与三阶长剑的真实网格、材质和预制体；菜单 **2 - Build Island Combat Scene** 另存石台战斗场景。

展示场景数字键 0–4 切换资产取景，F8 保存截图。战斗场景支持第三人称移动、跳跃、转镜头、近战、骷髅追击和宝箱升级。随包包含原始 `.blend` 建模文件、建模脚本及 Blender 渲染总览；预览图不是 Unity 运行截图。规则流程和可用 Unity 托管 API 编译通过；Unity 2019 原生导入及 Play 尚需本机验证。详细使用方法见包内 README.txt 和 `Assets/IslandQuest2019/README.md`。

已装旧版附加包、遇到 InvalidOperationException（UnityEngine.Input / Input System）时，下载 **IslandQuest2019-Input-Camera-Fix.zip**。完整解压运行 `Fix-IslandQuest2019.cmd`；它备份旧文件后更新脚本，保留模型和场景。重开 Unity 等待输入兼容编译符号同步及编译结束，再用菜单 2 重新生成战斗场景。修复同时覆盖展示快捷键、开箱与 R 重试；开始前和暂停时冻结敌人，战斗台避开树冠及建筑，缩小展示标签。当前完整附加包也已包含修复。
