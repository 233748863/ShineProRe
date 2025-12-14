using System;
using System.Runtime.InteropServices;
using ShineProCS.Core.Interfaces;

namespace ShineProCS.Infrastructure
{
    /// <summary>
    /// Win32 按键接口实现
    /// 使用 Windows API 模拟按键操作（类似 Python 版本的 幽灵盒子按键接口）
    /// </summary>
    public class Win32KeyboardInterface : IKeyboardInterface
    {
        // ===== Win32 API 声明 =====
        // 这些是 Windows 系统提供的底层函数，用于模拟键盘操作
        // [DllImport] 表示从 user32.dll 导入函数（类似 Python 的 ctypes）
        
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        // 按键事件标志
        private const uint KEYEVENTF_KEYDOWN = 0x0000;  // 按下按键
        private const uint KEYEVENTF_KEYUP = 0x0002;    // 释放按键

        /// <summary>
        /// 按下指定的按键
        /// </summary>
        public bool PressKey(int keyCode)
        {
            try
            {
                // 调用 Windows API 模拟按键按下
                // (byte)keyCode 将整数转换为字节类型
                keybd_event((byte)keyCode, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                return true;
            }
            catch (Exception ex)
            {
                // 如果出错，打印错误信息并返回 false
                Console.WriteLine($"按键按下失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 释放指定的按键
        /// </summary>
        public bool ReleaseKey(int keyCode)
        {
            try
            {
                // 调用 Windows API 模拟按键释放
                keybd_event((byte)keyCode, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"按键释放失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 按下并释放按键（完整的按键操作）
        /// 这是最常用的方法，相当于用户按一下键盘
        /// </summary>
        public bool PressAndRelease(int keyCode)
        {
            try
            {
                // 先按下
                keybd_event((byte)keyCode, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                
                // 短暂延迟（50 毫秒），模拟真实按键
                System.Threading.Thread.Sleep(50);
                
                // 再释放
                keybd_event((byte)keyCode, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"按键操作失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 释放所有按键
        /// 注意：这个实现比较简单，只是一个占位符
        /// 实际使用中可能需要跟踪哪些键被按下了
        /// </summary>
        public bool ReleaseAllKeys()
        {
            // 这里可以实现释放所有常用按键的逻辑
            // 暂时返回 true
            return true;
        }
    }
}
