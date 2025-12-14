"""
Windows 平台图像获取接口实现
基于 pywin32 (win32gui, win32ui, win32con) 实现高效屏幕截图
"""
import win32gui
import win32ui
import win32con
import numpy as np
import cv2
from typing import Tuple, Optional
from interface.图像获取接口 import 图像获取接口
from utils.日志管理 import 日志管理器

class Windows图像接口(图像获取接口):
    """
    基于 Windows API 的图像获取实现
    比 PIL/pyautogui 截图方式快 10-20 倍
    """
    
    def __init__(self):
        self.logger = 日志管理器.获取日志记录器("Windows图像接口")
        self.logger.信息("初始化 Windows 图像接口")

    def 获取屏幕区域(self, 区域: Tuple[int, int, int, int]) -> Optional[np.ndarray]:
        """
        获取指定屏幕区域的图像
        
        参数:
            区域: (x, y, width, height) 屏幕区域坐标
            
        返回:
            np.ndarray: BGR格式的图像数据 (OpenCV兼容)，失败返回 None
        """
        x, y, width, height = 区域
        
        # 参数校验
        if width <= 0 or height <= 0:
            self.logger.警告(f"无效的截图区域: {区域}")
            return None

        hwin = win32gui.GetDesktopWindow()
        
        try:
            # 获取设备上下文
            hwindc = win32gui.GetWindowDC(hwin)
            srcdc = win32ui.CreateDCFromHandle(hwindc)
            memdc = srcdc.CreateCompatibleDC()
            
            # 创建位图对象
            bmp = win32ui.CreateBitmap()
            bmp.CreateCompatibleBitmap(srcdc, width, height)
            memdc.SelectObject(bmp)
            
            # 截图 (BitBlt)
            memdc.BitBlt((0, 0), (width, height), srcdc, (x, y), win32con.SRCCOPY)
            
            # 获取位图数据
            signedIntsArray = bmp.GetBitmapBits(True)
            img = np.frombuffer(signedIntsArray, dtype='uint8')
            img.shape = (height, width, 4) # BGRA 格式
            
            # 资源释放
            srcdc.DeleteDC()
            memdc.DeleteDC()
            win32gui.ReleaseDC(hwin, hwindc)
            win32gui.DeleteObject(bmp.GetHandle())
            
            # 转换格式: BGRA -> BGR (去除 Alpha 通道，适配 OpenCV)
            return cv2.cvtColor(img, cv2.COLOR_BGRA2BGR)
            
        except Exception as e:
            self.logger.错误(f"截图失败: {e}")
            return None
