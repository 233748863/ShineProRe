"""
目标选择器
负责执行目标选择逻辑，目前主要支持通过游戏快捷键选择最低血量队友
"""
from typing import Optional
from interface.按键操作接口 import 按键操作接口
from utils.日志管理 import 日志管理器

class 目标选择器:
    """
    目标选择器
    """
    def __init__(self, 按键接口: 按键操作接口, 选中最低血量键值: int):
        self.按键接口 = 按键接口
        self.选中最低血量键值 = 选中最低血量键值
        self.日志 = 日志管理器.获取日志记录器("目标选择器")

    def 选择最低血量队友(self) -> bool:
        """
        执行选择最低血量队友的操作
        """
        if self.选中最低血量键值 <= 0:
            self.日志.警告("未配置'选中最低血量队友'快捷键")
            return False
            
        self.日志.调试(f"执行目标选择: 按下键值 {self.选中最低血量键值}")
        return self.按键接口.按下并释放(self.选中最低血量键值)
