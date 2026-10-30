# 可视化调试验证

在 Windows 上运行，无需游戏、ONNX 模型或额外测试框架，不执行输入模拟：

```powershell
dotnet run --project tools/VisualDebugValidation -c Debug
```

覆盖共享死区几何、100%～200% DPI 坐标映射、后台发布、快照隔离、关闭会话后的迟到写入，以及真实 WPF 离屏渲染中的框和标签、空结果、尺寸变化和清理。另在屏幕外创建不激活的 WPF 窗口，检查加载、可见性变化、定时刷新和资源释放。

默认将合成预览写入验证程序输出目录下的 `artifacts/autotarget-overlay.png`，可通过第一个参数指定输出目录。此验证使用合成检测快照，不能代替游戏内窗口跟随、跨显示器 DPI 和真实推理验证。
