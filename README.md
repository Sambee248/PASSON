# PASSON

PASSON 是一款围绕“传递”主题设计的第三人称 3D 空间解谜 Demo。玩家在悬空遗迹中投放影子，让自身行动传至另一条路线，并通过视角、路径和机关状态变化继续前进。

## 当前内容

- 主菜单与三个可玩的关卡。
- 网格路径移动、影子投放与跟随、预设观察角度切换。
- 旋转机关、压力板、巡逻敌人、环境陷阱和检查点。
- Windows 试玩版请在仓库的 [Releases](https://github.com/Sambee248/PASSON/releases) 页面下载。

## 运行源码

项目使用 Unity/Tuanjie 2022.3 系列编辑器（工程记录版本：2022.3.62t15）。克隆仓库后，在 Unity Hub 中通过 Add/Open 选择项目根目录，等待 Package Manager 完成依赖导入，再从 `Assets/Scenes/MainMenu.unity` 运行。

## 操作

- 鼠标左键：点击可达格移动
- Z / X：切换预设观察角度
- 按住 F：预览影子投放位置；松开 F：投放
- R：收回影子
- E：与附近机关互动
- Esc：返回主菜单

## 目录

- `Assets/`：场景、脚本、材质、字体与测试
- `Packages/`：Unity 包配置及锁定版本
- `ProjectSettings/`：项目设置

第四、第五关属于后续设计方向，当前可执行试玩版包含前三关。
