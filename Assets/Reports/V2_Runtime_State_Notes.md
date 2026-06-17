# V2 运行态逻辑与边界说明 (Runtime State Notes)

## V2.2 自动往返虚拟运动边界说明

V2.2 自动往返中，SQ 元件画布 ON/OFF 表示手动触发状态；虚拟限位触发由 MotionRuntimeState 管理，不写回 SQ 的 IsClosed。

因此在自动往返换向时，SQ 本体可能仍显示 OFF，但电机运行态中会显示虚拟左/右限位触发。若用户手动同时触发左右 SQ，系统按异常限位状态处理，电机停止，避免抖动。
