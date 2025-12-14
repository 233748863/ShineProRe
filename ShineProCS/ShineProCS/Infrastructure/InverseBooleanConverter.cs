using System;
using System.Globalization;
using System.Windows.Data;

namespace ShineProCS
{
    /// <summary>
    /// 布尔值反转转换器
    /// 用于在 XAML 中反转布尔值（true -> false, false -> true）
    /// 
    /// 【转换器说明】
    /// 在 WPF 中，转换器用于在绑定时转换数据类型或值
    /// 例如：IsRunning = true 时，我们希望"启动"按钮禁用
    /// 所以需要将 true 转换为 false
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        /// <summary>
        /// 正向转换（从源到目标）
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;  // 反转布尔值
            }
            return false;
        }

        /// <summary>
        /// 反向转换（从目标到源）
        /// 这里不需要实现，因为我们只做单向绑定
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
