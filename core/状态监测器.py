"""
状态监测器
负责监测当前目标的HP和Buff/Debuff状态
"""
import cv2
import numpy as np
from typing import Dict, List, Tuple, Any, Optional
from interface.图像获取接口 import 图像获取接口
from utils.日志管理 import 日志管理器
from utils.颜色判断工具 import 判断颜色是否在范围

class 状态监测器:
    """
    状态监测器
    基于视觉识别监测目标状态
    """
    def __init__(self, 图像接口: 图像获取接口):
        self.图像接口 = 图像接口
        self.日志 = 日志管理器.获取日志记录器("状态监测器")
        self.Buff模板缓存 = {}
        self.Debuff模板缓存 = {}
        
    def 获取目标HP百分比(self, 血条区域: Tuple[int, int, int, int], 颜色阈值: Dict[str, Any]) -> float:
        """
        获取当前目标的HP百分比
        基于HSV颜色识别，计算血条区域内特定颜色像素的占比
        
        参数:
            血条区域: (x1, y1, x2, y2)
            颜色阈值: {
                "lower": [h, s, v],  # HSV下限
                "upper": [h, s, v]   # HSV上限
            }
        """
        if not 血条区域 or not 颜色阈值:
            return 1.0 # 默认满血，避免误判
            
        截图 = self.图像接口.获取屏幕区域(血条区域)
        if 截图 is None or 截图.size == 0:
            return 1.0
            
        # 转换到HSV空间
        hsv图像 = cv2.cvtColor(截图, cv2.COLOR_BGR2HSV)
        
        # 提取颜色掩膜
        lower = np.array(颜色阈值.get("lower", [0, 0, 0]))
        upper = np.array(颜色阈值.get("upper", [180, 255, 255]))
        mask = cv2.inRange(hsv图像, lower, upper)
        
        # 计算非零像素点
        # 假设血条是水平的，我们计算每一列是否存在红色像素，然后统计存在的列数占总宽度的比例
        # 或者简单点，直接计算所有红色像素占总像素的比例（假设血条高度充满区域）
        # 更精确的方法：统计mask中非零像素的总数，除以(宽*高)
        # 但考虑到血条可能有背景色干扰，通常计算"最长连续红色区域的宽度"或者"红色像素总数/单行最大红色像素数"
        
        # 简单实现：计算红色像素占比
        红色像素数 = cv2.countNonZero(mask)
        总像素数 = 截图.shape[0] * 截图.shape[1]
        
        if 总像素数 == 0:
            return 1.0
            
        # 这里的比例可能需要根据实际UI进行校准，因为血条可能不会填满整个矩形区域
        # 假设配置的区域就是血条的有效显示区域
        比例 = 红色像素数 / 总像素数
        
        return 比例

    def 检测Buff状态(self, Buff区域: Tuple[int, int, int, int], Buff名称列表: List[str], 模板路径字典: Dict[str, str]) -> List[str]:
        """
        检测目标身上存在的Buff
        """
        return self._检测图标(Buff区域, Buff名称列表, 模板路径字典, "Buff")

    def 检测Debuff状态(self, Debuff区域: Tuple[int, int, int, int], Debuff名称列表: List[str], 模板路径字典: Dict[str, str]) -> List[str]:
        """
        检测目标身上存在的Debuff
        """
        return self._检测图标(Debuff区域, Debuff名称列表, 模板路径字典, "Debuff")

    def _检测图标(self, 区域: Tuple[int, int, int, int], 名称列表: List[str], 模板路径字典: Dict[str, str], 类型: str) -> List[str]:
        """
        通用的图标检测逻辑
        """
        已发现列表 = []
        if not 区域:
            return 已发现列表
            
        截图 = self.图像接口.获取屏幕区域(区域)
        if 截图 is None or 截图.size == 0:
            return 已发现列表
            
        for 名称 in 名称列表:
            模板路径 = 模板路径字典.get(名称)
            if not 模板路径:
                continue
                
            # 缓存模板
            缓存Key = f"{类型}_{名称}"
            if 缓存Key not in self.Buff模板缓存: # 统称缓存
                模板 = cv2.imread(模板路径)
                if 模板 is None:
                    self.日志.警告(f"无法加载{类型}模板: {名称} -> {模板路径}")
                    continue
                self.Buff模板缓存[缓存Key] = 模板
            
            模板 = self.Buff模板缓存[缓存Key]
            
            # 模板匹配
            try:
                res = cv2.matchTemplate(截图, 模板, cv2.TM_CCOEFF_NORMED)
                min_val, max_val, min_loc, max_loc = cv2.minMaxLoc(res)
                
                if max_val > 0.8: # 阈值可配置
                    已发现列表.append(名称)
            except Exception as e:
                self.日志.错误(f"检测{类型}异常: {名称} - {e}")
                
        return 已发现列表
