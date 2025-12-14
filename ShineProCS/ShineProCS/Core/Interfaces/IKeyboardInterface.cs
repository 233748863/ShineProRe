using System.Threading.Tasks;

namespace ShineProCS.Core.Interfaces
{
    /// <summary>
    /// 按键操作接口
    /// 定义了按键模拟的基本操作（类似 Python 版本的 按键操作接口）
    /// </summary>
    public interface IKeyboardInterface
    {
        /// <summary>
        /// 按下指定的按键
        /// </summary>
        /// <param name="keyCode">按键代码（如 81 代表 Q 键）</param>
        /// <returns>是否成功</returns>
        bool PressKey(int keyCode);

        /// <summary>
        /// 释放指定的按键
        /// </summary>
        /// <param name="keyCode">按键代码</param>
        /// <returns>是否成功</returns>
        bool ReleaseKey(int keyCode);

        /// <summary>
        /// 按下并释放按键（完整的按键操作）
        /// </summary>
        /// <param name="keyCode">按键代码</param>
        /// <returns>是否成功</returns>
        bool PressAndRelease(int keyCode);

        /// <summary>
        /// 释放所有当前按下的按键
        /// </summary>
        /// <returns>是否成功</returns>
        bool ReleaseAllKeys();
    }
}
