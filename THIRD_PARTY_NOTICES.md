# Third-party Resources / 第三方资源说明

项目使用第三方模型、动画、材质、特效及 Unity 包。游戏系统与资源来源分别展示；这些素材不作为原创美术成果声明。

## 仓库边界

公开作品集排除以下第三方资源目录及其 `.meta`。完整私有工程保留资源原有文件。现有场景、预制体和材质依赖这些资源，因此也不包含在公开源码导出中。

README 中的 `docs/media` 仅包含 6 张经挑选的游戏渲染截图，不包含可提取或导入 Unity 的原始模型、纹理、材质或资源包。截图中的第三方原型美术仍归原作者所有，展示截图不等于将这些素材作为原创成果或重新授权分发。

| 本地目录 | 识别到的内容 | 当前处理 |
| --- | --- | --- |
| `Assets/PolygonPrototype` | Synty Studios 的 Polygon Prototype 资源 | 排除公开源文件 |
| `Assets/MountainLandscape` | Mountain 场景与模型资源 | 排除公开源文件；确切商品与授权待核对 |
| `Assets/Fire` | Free Fire VFX URP | 排除公开源文件 |
| `Assets/Fireball` | Procedural fire | 排除公开源文件 |
| `Assets/Water` | Simple Water Shader | 排除公开源文件 |
| `Assets/Grass` | Point-Grass-Renderer | 排除公开源文件；确切许可待核对 |
| `Assets/Tables` | Table 模型及材质 | 排除公开源文件；确切来源待核对 |
| `Assets/Animations` | FBX 动画与 Animator 资源 | 排除公开源文件；确切来源待核对 |
| `Assets/TextMesh Pro` | TextMesh Pro 导入资源、字体与示例 | 排除公开源文件；私有工程保留附带声明 |

免费获得素材也不等于可以公开再分发原始素材。Unity Asset Store 的标准许可允许在满足条款的产品中嵌入资源，原始资源再分发需另行核对；参见 [官方 EULA](https://unity.com/legal/as-terms)。本清单用于记录来源和发布边界，不替代具体资源的许可文本。

## Unity 包与生成代码

`Packages/manifest.json` 与 `Packages/packages-lock.json` 记录项目依赖，不包含包的源文件副本。Unity Editor 及包由使用者自行安装，分别受对应许可约束。

`Assets/PlayerInputActions.cs` 是 Input System 根据项目输入配置生成的代码，并非手写框架实现；在公开仓库中保留其自动生成声明。

## 当前授权状态

当前不为整个仓库附加 MIT 或其他统一开源许可，以免将第三方或来源未追溯的代码一并授权。公开代码用于作品集展示，第三方资源仍按其原许可使用。待补齐来源记录后，可以单独决定自有代码的开源许可。

发布 Windows Build 前，需要从原始购买/下载记录核对资源名称、作者、许可和任何 Restricted Asset 条件。私有保存不会自动赋予分享资源或增加协作者的权利。
