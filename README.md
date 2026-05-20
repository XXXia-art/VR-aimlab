# VR Aim Lab - 第一人称瞄准训练游戏

> Unity + OpenXR 开发的 VR 射击训练项目，灵感来源于 Aim Lab。

---

## 📋 项目简介

- **引擎**：Unity 2022.3.62f3c1 (LTS)
- **XR 后端**：OpenXR
- **目标平台**：PC VR（SteamVR / Oculus Link 等）
- **核心玩法**：在 5×5 网格中射击随机刷新的青蓝色小球目标

### 功能特性
- ✅ 5×5 网格随机目标生成（始终保持 3 个小球在场上）
- ✅ 激光瞄准 + 屏幕准星（编辑器调试模式）
- ✅ 实时分数/命中率/训练时长统计面板
- ✅ 对象池管理（高效的目标生成与回收）
- ✅ 棋盘格训练房风格（支持 VR 和编辑器调试）

---

## 🚀 快速开始

### 1. 环境要求

| 工具 | 版本要求 |
|------|---------|
| Unity Hub | 最新版 |
| Unity Editor | **2022.3.62f3c1 LTS**（必须一致） |
| Git | 任意版本 |

> ⚠️ **非常重要**：全队必须使用**完全相同**的 Unity 版本，否则场景文件和 meta 文件会产生大量冲突。

### 2. Fork 本仓库

**不要直接克隆主仓库！** 请先 Fork 到自己的 GitHub / Gitee 账号下：

1. 打开项目主页：`https://github.com/XXXia-art/VR-aimlab`
2. 点击右上角的 **Fork** 按钮
3. 选择你的个人仓库，完成 Fork

### 3. 克隆你 Fork 的仓库

```bash
# 注意：这里的 YOUR_NAME 换成你的 GitHub 用户名
git clone https://github.com/YOUR_NAME/VR-aimlab.git
```

### 4. 添加上游仓库（主仓库）

```bash
cd VR-aimlab
git remote add upstream https://github.com/XXXia-art/VR-aimlab.git
```

### 5. 用 Unity 打开项目

1. 打开 **Unity Hub**
2. 点击 **"打开项目"** → 选择克隆下来的 `VR-aimlab` 文件夹
3. Unity 会自动读取 `Packages/manifest.json` 下载所有依赖包（首次打开可能需要几分钟）
4. 打开 `Assets/Scenes/SampleScene.unity`

---

## 🎮 运行游戏

### 编辑器调试模式（无 VR 头显）

1. 点击顶部 **▶ Play** 按钮
2. **点击 Game 视图** 锁定鼠标光标
3. **移动鼠标**：转动视角 / 瞄准
4. **鼠标左键**：射击（屏幕中心准星变红=已瞄准目标）
5. **Esc**：解锁鼠标

### VR 模式（连接头显后）

1. 确保 OpenXR Loader 已启用：`Project Settings > XR Plug-in Management`
2. 戴上头显，右手控制器扳机键射击
3. 激光线：绿色=未命中，红色=瞄准到目标

---

## 🤝 协作开发流程（Fork + PR）

本项目采用 **Fork + Pull Request** 的方式协作。所有成员**禁止直接推送代码到主仓库**。

### 第一步：同步上游代码（每次开发前必须做）

```bash
# 拉取主仓库最新代码到本地
git pull upstream main
```

### 第二步：在 main 分支上开发

直接在 `main` 分支上修改代码，然后提交：

```bash
# 修改代码后，添加文件
git add Assets/Scripts/你的脚本.cs

# 提交（写清楚做了什么）
git commit -m "feat: 新增手枪射击音效"
# 或
git commit -m "fix: 修复小球偶尔穿墙的问题"
```

### 第三步：推送到你的 Fork

```bash
git push origin main
```

### 第四步：发起 Pull Request

1. 打开 **你的 Fork** 仓库页面（`https://github.com/YOUR_NAME/VR-aimlab`）
2. 点击 **"Contribute"** → **"Open pull request"**
3. 确认源分支是你的 `main`，目标分支是主仓库的 `main`
4. 填写 PR 标题和描述（**说清楚改了什么**）
5. 点击 **"Create pull request"**

### 第五步：等待审核合并

- 项目管理员会审查你的 PR
- 如有问题会留言要求修改
- 审核通过后由管理员合并到主仓库

---

## ⚠️ 协作规范

### ✅ 必须提交的文件

- `Assets/` 下的所有内容（**包括 `.meta` 文件！**）
- `Packages/manifest.json` 和 `packages-lock.json`
- `ProjectSettings/` 下的所有配置
- `README.md`、`.gitignore`

### ❌ 禁止提交的文件

以下文件夹已被 `.gitignore` 自动排除，**不要手动强制添加**：

| 文件夹 | 说明 |
|--------|------|
| `Library/` | 自动生成的库缓存（通常几个 GB） |
| `Temp/` | 临时文件 |
| `Logs/` | 编辑器日志 |
| `UserSettings/` | 个人窗口布局偏好 |
| `obj/` | 编译中间文件 |

> 💡 `.meta` 文件**必须**随资产一起提交！它们保存了资源的唯一 GUID，丢失会导致场景引用全部断开（变成"Missing"）。

### 提交信息规范

| 前缀 | 用途 |
|------|------|
| `feat:` | 新增功能 |
| `fix:` | 修复问题 |
| `docs:` | 文档修改 |
| `style:` | 代码格式调整（不影响功能） |
| `refactor:` | 重构 |

### 场景协作建议

Unity 场景文件是 YAML 文本，**多人同时修改同一场景很容易冲突**。

建议：
- 每个人负责不同模块的预制体（Prefab），少直接改场景
- 需要改场景时，提前在群里说一声，避免同时提交
- 如果冲突了，优先保留最新的修改，丢失的部分手动重做

---

## 📁 项目结构

```
Assets/
├── Scenes/
│   └── SampleScene.unity          # 主场景
├── Scripts/
│   ├── GameBootstrap.cs           # 场景初始化器
│   ├── GridManager.cs             # 5×5 网格管理
│   ├── Target.cs                  # 小球目标逻辑
│   ├── VRGun.cs                   # 射击与激光瞄准
│   ├── ScoreManager.cs            # 分数统计
│   └── ...                        # 其他辅助脚本
├── XR/                            # OpenXR 配置
├── XRI/                           # XR Interaction Toolkit 配置
└── TextMesh Pro/                  # 字体资源
```

---

## 🐛 常见问题

### Q：打开项目后 Game 视图一片黑？
A：在 Project 窗口中双击 `Assets/Scenes/SampleScene.unity` 加载场景。

### Q：文字显示为粉色方块或空白？
A：菜单栏 `Window > TextMeshPro > Import TMP Essential Resources`，导入字体资源。

### Q：提示编译错误无法进入 Play 模式？
A：按 `Ctrl + R` 刷新资源，等待 Unity 重新编译脚本。

### Q：修改了脚本但运行时没变化？
A：确保保存了脚本文件（Ctrl + S），Unity 检测到改动后会自动编译。

---

## 📌 版本记录

| 日期 | 版本 | 说明 |
|------|------|------|
| 2024-05 | v0.1 | 初始版本，基础射击训练功能 |

---

## 👥 开发者

- 主仓库地址：https://github.com/XXXia-art/VR-aimlab
- 协作方式：Fork + Pull Request
