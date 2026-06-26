# The SIP Lab Lockdown

一个用 Unity 2022.3 + Built-in Render Pipeline 实现的第一人称 3D 密室逃脱小游戏。

整个游戏的几何体、材质、音效、UI 全部由代码生成 —— **零外部美术资源 / 音频文件**。

---

## 玩法概述

你被锁在一间警报红光下的实验室里，必须按顺序破解 3 个谜题逃出去：

1. **恢复电力** — 推开角落里的箱子，找到能量核心，插入东墙的发电机。房间灯光从红色警报变为正常白光。
2. **破解二进制密码** — 通电瞬间，南墙嵌入的 4×4 服务器抽屉阵列启动：二进制位为 `1` 的抽屉弹出并亮起绿色工作灯，为 `0` 的保持缩入黑暗。结合东侧桌上《设备维护手册》给出的权重图例（`[■ □ □ □] = 8` / `4` / `2` / `1`），每行累加得到一位十进制数字，推导出完整 4 位密码，在西墙 3D 键盘输入并按 `E` 确认。保险柜旋转开启，取出门禁卡。
3. **逃出实验室** — 用门禁卡刷开北墙的防爆门，穿过门外触发胜利。

整个过程会显示用时，越快越好。

---

## 控制

| 输入 | 行为 |
|------|------|
| `WASD` | 移动 |
| `Mouse` | 视角 |
| `E` 或 鼠标左键 | 与准星指向的物体交互 |
| `Esc` 或 `Space` | 暂停 / 继续 |

主菜单和暂停菜单都有 **Settings** 按钮，可独立调节：
- **Interaction Volume** — 拾取、键盘、密码对错、刷卡、开门、电源、胜利等事件音
- **Action Volume** — 走路脚步声、箱子碰撞 / 掉落声

音量设置通过 `PlayerPrefs` 持久化，重启游戏会保留。

---

## 技术栈

- **Unity**: `2022.3.62f3`
- **渲染管线**: Built-in Render Pipeline（未引入 URP/HDRP）
- **物理**: Unity 内置 PhysX，玩家用 Rigidbody + CapsuleCollider
- **UI**: Unity UGUI（Screen Space Overlay）
- **音频**: 100% 程序化合成（`AudioClip.Create + SetData`），无任何 .wav/.mp3 资源
- **几何 / 材质**: 全部 `GameObject.CreatePrimitive` + 代码创建的 Standard Shader 材质
- **场景构建**: 一次性编辑器脚本 `Assets/Editor/SceneBuilder.cs`，通过 MCP 工具或菜单按钮执行 `BuildAll()` 即可重建整个场景

---

## 快速开始

1. 用 Unity Hub 添加本项目，引擎版本 **2022.3.62f3**（其它 2022.3 LTS 子版本通常也兼容）。
2. 打开后 Unity 会自动恢复包依赖（首次较慢）。
3. 直接打开 `Assets/Scenes/MainScene.unity`，按 ▶︎ Play 即可游戏。

如果场景丢失或损坏，可以在 Unity 内通过 MCP 调用 `SceneBuilder.BuildAll()`，或在编辑器里手动：
1. 创建一个新的空场景 `Assets/Scenes/MainScene.unity`
2. 在 C# Console / 编辑器脚本里调用 `SceneBuilder.BuildAll()`

`SceneBuilder` 会清空当前场景，**程序化重建**所有内容（几何、材质、玩家、灯光、3 个谜题、UI、音频管理器），无需任何手工拖拽。

---

## 项目结构

```text
Assets/
├── Editor/
│   └── SceneBuilder.cs            # 一键重建整个场景的编辑器脚本
├── Materials/                     # 由 SceneBuilder 自动创建的 Standard 材质
├── Scenes/
│   └── MainScene.unity            # 唯一游戏场景
├── Scripts/
│   ├── Audio/
│   │   ├── AudioManager.cs        # 单例 + 程序化合成 12 个 SFX + 双音量通道
│   │   ├── FootstepEmitter.cs     # 玩家移动脚步触发
│   │   └── CrateCollisionSound.cs # 箱子碰撞按冲量计算音量/音高
│   ├── Environment/
│   │   ├── BoundsClamp.cs         # 限制物体不被推出房间
│   │   └── LightingController.cs  # 红光 → 白光的平滑过渡
│   ├── Interaction/
│   │   ├── IInteractable.cs       # 交互接口
│   │   └── InteractionRaycaster.cs# 3m 射线 + LayerMask + E/LMB
│   ├── Managers/
│   │   └── GameStateManager.cs    # 单例 + 状态机 (Start/Playing/Paused/Victory)
│   ├── Player/
│   │   ├── FirstPersonController.cs # WASD + 鼠标视角，Rigidbody 驱动
│   │   └── PlayerInventory.cs       # 能量核心 / 门禁卡的持有标志
│   ├── Puzzles/
│   │   ├── EnergyCore.cs            # Puzzle 1 — 能量核心拾取
│   │   ├── GeneratorTerminal.cs     # Puzzle 1 — 发电机插入核心
│   │   ├── KeypadButton.cs          # Puzzle 2 — 单个 3D 按钮
│   │   ├── KeypadPuzzle.cs          # Puzzle 2 — 4 位密码校验
│   │   ├── SafeDoor.cs              # Puzzle 2 — 保险柜开门动画
│   │   ├── KeycardPickup.cs         # Puzzle 2 — 钥匙卡拾取
│   │   ├── BlastDoorController.cs   # Puzzle 3 — 防爆门双开
│   │   └── VictoryTrigger.cs        # Puzzle 3 — 走过触发胜利
│   └── UI/
│       ├── HUDController.cs         # 准星 / 提示 / 库存 / 计时
│       ├── MenuManager.cs           # Start/Pause/Victory/Settings 切换
│       └── SettingsPanel.cs         # 两滑块音量设置
└── UI/
    └── CrosshairRing.png            # 程序化生成的准星贴图
```

---

## 核心架构

### 状态机

```text
[Start] --Start按钮--> [Playing] <--Esc/Space--> [Paused]
                          |
                          +-- VictoryTrigger --> [Victory]
```

- **Start / Paused / Victory**: `Time.timeScale = 0`，光标解锁可见
- **Playing**: `Time.timeScale = 1`，光标锁定隐藏

切换通过 `GameStateManager.SetState()` 完成，并广播 `OnStateChanged` 事件，HUD / 菜单 / 玩家控制器各自订阅。

### 交互

`InteractionRaycaster` 每帧从相机发出 3m 射线，仅命中 `Interactable` Layer：

```csharp
public interface IInteractable
{
    string GetPrompt();
    bool IsInteractable();
    void Interact(PlayerInventory inventory);
}
```

只要把任何 GameObject 的 layer 设为 `Interactable` 并挂一个实现了 `IInteractable` 的组件，玩家走近瞄准就会显示提示，按 `E` 触发逻辑。

### 音频系统

`AudioManager` 是单例，启动时一次性合成 12 段音效：

| 名称 | 用途 | 合成手法 |
|------|------|----------|
| `Pickup` / `KeycardPickup` | 拾取物品 | 双音正弦 blip |
| `PowerOn` | 通电恢复 | 频率扫频 + 谐波 + 包络 |
| `KeypadBeep` | 键盘按键 | 方波，按数字音高变化 |
| `Success` | 密码正确 | C-E-G 升调三和弦 |
| `Error` | 密码错误 | 低频锯齿"嗡嗡" |
| `KeycardSwipe` | 刷门禁卡 | 滤波白噪声 burst |
| `DoorOpen` / `SafeOpen` | 开门 | 低频隆隆 + 噪声扫频 |
| `Footstep` | 走路 | 低通噪声 + 指数衰减 |
| `CrateThud` | 箱子碰撞 | 低频共振 + 噪声，按冲量调音量 |
| `Victory` | 通关胜利 | C-G-C 琶音 + 持续 C 大调和弦 + 颤音 |

两个独立音量通道 (`InteractionVolume` / `ActionVolume`) 通过 `PlayerPrefs` 持久化，`SettingsPanel` UI 实时调节。

### 程序化场景生成

`SceneBuilder.cs` 是整个项目的灵魂 —— 一个静态类，调用 `BuildAll()` 后会：

1. 清空当前场景所有根物体
2. 在 `Assets/Materials/` 创建 / 复用所有 Standard 材质
3. 搭建房间几何（地板、天花、4 面墙、防爆门口、门外胜利区）
4. 配置灯光（4 红角灯 + 1 白主光 + 1 白补光）
5. 创建玩家（Rigidbody + Capsule + 相机 + 交互射线 + 脚步组件）
6. 构建 3 个谜题区域所有物件 + 组件 + 引用关系
7. 构建 UI（HUD + Start/Pause/Victory/Settings 4 个 Canvas + 持久化按钮监听）
8. 把 `MainScene.unity` 加入 Build Settings
9. 标记 dirty 并保存

整个项目可以在任何时刻通过 `BuildAll()` **从零完整重建**，无需手工配置。

---

## 关键工程取舍

- **零外部资源**：所有 Material / Sprite / AudioClip 都用代码创建，避免课程作业中常见的"资源丢失"问题
- **持久化按钮监听**：UI 按钮使用 `UnityEventTools.AddPersistentListener` 而非 `onClick.AddListener`，确保保存到 `.unity` 文件后重新打开仍可点击
- **Rigidbody 玩家**：用 Rigidbody 而非 CharacterController，能与可推动的箱子产生符合物理直觉的交互
- **TextMesh 而非 TMP**：3D 键盘 / 提示板用 Built-in `TextMesh`，避免引入额外包依赖
- **3D 键盘 E 键确认**：输入 4 位密码后 **不会自动校验**，必须按 `E` 键，符合实体键盘的交互直觉

---

## 验收要点

- ✅ 房间 10×4×10、玩家初始 (0, 1.1, -4)、出口在北墙中心
- ✅ 红光初始氛围 / 通电后白光
- ✅ WASD + 鼠标 360° + 中心准星 + 3m raycast + LayerMask + E/LMB
- ✅ 5 个带 Rigidbody 的箱子 + 隐藏的 Energy Core + Generator Terminal
- ✅ 南墙 4×4 二进制服务器抽屉阵列 + 维护手册权重图例 + 12 按钮 3D 键盘 + 仅通电后激活 + Safe 旋转开门 + Keycard
- ✅ BlastDoor 同时检查通电 + 钥匙卡，Lerp 滑开，穿过触发胜利
- ✅ HUD 三件套（准星 / 提示 / 库存 / 计时） + Start / Pause / Victory / Settings 菜单
- ✅ 鼠标在 UI 时解锁可见，玩法时锁定隐藏
- ✅ 玩家不穿墙（Continuous 碰撞检测 + 0.2m 厚墙）
- ✅ 完整音效系统 + 双通道音量调节 + 持久化

---

## License

仅用于学习实践，无商业用途。
