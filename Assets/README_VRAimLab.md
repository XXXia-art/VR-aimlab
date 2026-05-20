# VR Aim Lab - 使用说明

## 项目简介
这是一个基于 Unity 2022.3 + OpenXR 的 VR 第一人称瞄准训练游戏，灵感来源于 Aim Lab。

## 核心功能
- **5×5 网格目标区域**：正前方 25 个点位随机生成青蓝色小球
- **3 目标同场**：始终保持 3 个小球在场上
- **激光瞄准系统**：绿色（未命中）/ 红色（瞄准到目标）
- **实时数据面板**：分数、命中数、射击数、命中率、训练时长
- **对象池管理**：高效的目标小球生成与回收
- **粒子击中特效**：击中时的视觉反馈

## 快速开始

### 1. 打开场景
打开 `Assets/Scenes/SampleScene.unity`，场景中已有一个 `GameSystem` 对象。

### 2. 运行游戏
直接点击 Play，GameBootstrap 会自动构建：
- XR 追踪（HMD + 右手控制器）
- 训练房间（灰色墙壁 + 棋盘格地板）
- 5×5 目标网格与 GridManager
- 右手枪械与激光瞄准线
- 世界空间数据 UI 面板

### 3. 可选：编辑器中持久化场景
选中 `GameSystem` 对象，在 Inspector 中右键点击 `GameBootstrap` 组件：
- 选择 **VRAimLab/Build Full Scene** 可在编辑器中生成所有对象并保存
- 选择 **VRAimLab/Cleanup Runtime Objects** 可清除生成的对象

## 输入控制
- **VR 模式**：右手控制器扳机键（Trigger）射击
- **编辑器调试**：按住鼠标左键也可射击（用于无头显测试）

## 脚本说明

| 脚本 | 功能 |
|------|------|
| `GameBootstrap.cs` | 场景初始化器，运行时/编辑器构建所有游戏对象 |
| `GridManager.cs` | 5×5 网格管理，目标随机生成与对象池 |
| `Target.cs` | 小球目标逻辑，击中判定与缩放动画 |
| `VRGun.cs` | 激光瞄准与物理射线射击系统 |
| `ScoreManager.cs` | 分数、命中率、训练时长统计 |
| `HitEffectPool.cs` | 击中粒子特效对象池 |
| `XRCameraTracker.cs` | HMD 头部追踪（基于旧版 XR Input） |
| `XRControllerTracker.cs` | 手柄位置/旋转追踪 |
| `UIFaceCamera.cs` | 世界空间 UI 面板始终面向玩家 |

## 自定义配置

选中 `GameSystem` 对象，在 Inspector 中调整 `GameBootstrap` 的参数：

- **Room**：房间尺寸、网格距离
- **Visual**：墙壁/地板/小球/激光颜色
- **Target**：小球大小、同时存在数量
- **Grid**：网格行列数、间距、高度
- **UI**：数据面板距离和高度
- **Target Layer Index**：目标层索引（默认 0，可创建专用 Target 层后修改）

## 技术规格
- **Unity 版本**：2022.3.62f3c1 (LTS)
- **XR 后端**：OpenXR
- **交互工具包**：XR Interaction Toolkit 2.6.5（用于设备追踪）
- **渲染管线**：Built-in Render Pipeline
- **目标帧率**：72Hz/90Hz VR

## 注意事项
1. 首次在 Unity 中打开时，可能需要在 **Project Settings > XR Plug-in Management** 中确认 OpenXR Loader 已启用
2. 如果需要使用专用 Target 层：
   - 打开 **Edit > Project Settings > Tags and Layers**
   - 在 User Layer 中创建一个名为 `Target` 的层（例如 Layer 6）
   - 将 `GameSystem` 上的 `Target Layer Index` 改为对应的层编号
3. 场景中默认保留 `Directional Light` 提供基础照明
