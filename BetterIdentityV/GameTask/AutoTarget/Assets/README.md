# YOLO 目标跟随模型

将 YOLO11 目标检测 ONNX 权重放在此目录，默认文件名为 `target.onnx`。
构建时 ONNX 文件会复制到程序输出目录，也可以在触发器设置中填写模型的绝对路径。

调用链移植自 BetterGI：`BivOnnxFactory → BivYoloPredictor → YoloSharp 6.0.3`。
执行提供程序沿用 BetterGI 的 `DML → CPU` 分支：关闭内存模式优化，使用顺序执行模式，并保留 CPU 回退。
ONNX Runtime 使用与 BetterGI 相同的 DirectML / Managed 1.21.0 依赖，排除 YoloSharp 间接引入的 CPU 原生运行库资源。
模型的预处理、检测与后处理使用 YoloSharp 原有实现；权重需使用其支持的 YOLO11 检测导出格式，并包含类别名称等模型元数据。

触发器默认关闭。开启后，只在游戏处于前台时对原始完整截图进行检测，按检测结果原顺序取第一个对象。
以目标框中心相对画面中心的方向发送鼠标相对移动，进入圆形 deadzone 后停止移动；每帧重新选择第一个对象，不跨帧锁定目标。

- `DeadzoneRadius`：中心死区半径，原始截图像素，默认 30。
- `MouseMoveDistance`：每次相对鼠标移动距离，默认 2。
- `InferenceDevice`：推理设备，默认 `GpuDirectMl`，也可以选择 `Cpu`。
- `GpuDevice`：DirectML 的 DXGI 设备编号，默认 1，对应当前开发机的 RTX 4060；其他机器需按实际显卡编号设置。

推理设备与显卡编号可在触发器设置中修改并自动保存，工作线程会在下一轮重建预测器。
中心死区和移动距离的修改不触发模型重载。

模型缺失或加载失败时会记录错误并等待重试，不中断其他触发器。
