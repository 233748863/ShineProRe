using System;
using System.Collections.Generic;
using OpenCvSharp;
using ShineProCS.Core.Interfaces;
using ShineProCS.Models;

namespace ShineProCS.Core.Services
{
    /// <summary>
    /// 技能状态检测器
    /// 负责更复杂的技能状态检测，如图标识别、冷却时间精确跟踪等
    /// 
    /// 【高级检测说明】
    /// 1. 模板匹配：识别技能图标是否亮起
    /// 2. 颜色识别：检测技能是否处于可释放状态
    /// 3. 文本识别（OCR）：识别剩余冷却秒数（可选）
    /// </summary>
    public class SkillStateDetector
    {
        private readonly IImageInterface _imageInterface;
        private readonly ConfigManager _config;
        private readonly StateMonitor _stateMonitor;

        /// <summary>
        /// 构造函数
        /// </summary>
        public SkillStateDetector(IImageInterface imageInterface, ConfigManager config, StateMonitor stateMonitor)
        {
            _imageInterface = imageInterface;
            _config = config;
            _stateMonitor = stateMonitor;
        }

        /// <summary>
        /// 并行更新技能状态（由引擎调用）
        /// </summary>
        public void UpdateSkillStateVisually(SkillRuntimeState skill, Mat currentFrame)
        {
            try
            {
                var region = skill.Config.IconRegion;
                if (region == null || region.Length < 4 || region[2] <= 0 || region[3] <= 0)
                {
                    skill.IsVisuallyReady = true;
                    return;
                }

                // 从当前帧裁剪出技能图标区域
                using var iconMat = new Mat(currentFrame, new Rect(region[0], region[1], region[2], region[3]));
                
                // 1. 模板匹配
                if (!string.IsNullOrEmpty(skill.Config.TemplatePath) && System.IO.File.Exists(skill.Config.TemplatePath))
                {
                    using var template = Cv2.ImRead(skill.Config.TemplatePath);
                    if (template != null && !template.Empty())
                    {
                        skill.IsVisuallyReady = CheckIconByTemplate(iconMat, template, skill.Config.SimilarityThreshold);
                        return;
                    }
                }

                // 2. 亮度检测
                skill.IsVisuallyReady = IsIconBright(iconMat);
            }
            catch
            {
                skill.IsVisuallyReady = false;
            }
        }

        /// <summary>
        /// 检测特定技能的实时状态
        /// </summary>
        public bool IsSkillReady(SkillRuntimeState skill)
        {
            // 逻辑保底检查
            if (!skill.IsAvailable && !_config.AppSettings.EnableSmartMode) return false;

            // 视觉识别检查 (现在由引擎并行更新)
            if (_config.AppSettings.EnableSmartMode)
            {
                return skill.IsVisuallyReady;
            }

            return true;
        }

        /// <summary>
        /// 检查技能的 Buff/Debuff 前置条件
        /// </summary>
        public bool CheckSpecificBuff(string buffName)
        {
            if (string.IsNullOrEmpty(buffName)) return true;

            // 1. 优先在全局 Buff 库中查找
            foreach (var buff in _config.AppSettings.GlobalBuffs)
            {
                if (buff.Name == buffName)
                {
                    return CheckBuffVisually(buff);
                }
            }

            // 2. 在技能私有 Buff 配置中查找 (兼容旧版)
            foreach (var skill in _config.Skills)
            {
                foreach (var buff in skill.BuffRequirements)
                {
                    if (buff.Name == buffName)
                    {
                        return CheckBuffVisually(buff);
                    }
                }
            }

            // 3. 在当前游戏状态的字符串列表中查找 (兼容性)
            var gameState = _stateMonitor.DetectGameState();
            return gameState.ActiveBuffs.Contains(buffName);
        }

        private bool CheckBuffConditions(SkillConfig config, GameState state)
        {
            // 1. 检查可视化 Buff 要求 (核心)
            foreach (var buff in config.BuffRequirements)
            {
                bool isPresent = CheckBuffVisually(buff);
                
                // 如果是必须拥有的 Buff，但没检测到 -> 失败
                if (buff.IsRequired && !isPresent) return false;
                
                // 如果是不能拥有的 Buff (Debuff)，但检测到了 -> 失败
                if (!buff.IsRequired && isPresent) return false;
            }

            // 2. 检查旧版字符串 Buff 要求 (兼容性)
            if (!string.IsNullOrEmpty(config.RequiredBuffs))
            {
                var required = config.RequiredBuffs.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                 .Select(b => b.Trim());
                foreach (var buff in required)
                {
                    if (!state.ActiveBuffs.Contains(buff)) return false;
                }
            }

            if (!string.IsNullOrEmpty(config.ExcludedBuffs))
            {
                var excluded = config.ExcludedBuffs.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                 .Select(b => b.Trim());
                foreach (var buff in excluded)
                {
                    if (state.ActiveBuffs.Contains(buff)) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 视觉检测特定 Buff 是否存在
        /// </summary>
        private bool CheckBuffVisually(BuffConfig buff)
        {
            try
            {
                var region = buff.IconRegion;
                if (region == null || region.Length < 4 || region[2] <= 0 || region[3] <= 0) return false;

                using var iconMat = _imageInterface.GetScreenRegion(region[0], region[1], region[2], region[3]);
                if (iconMat == null || iconMat.Empty()) return false;

                if (!string.IsNullOrEmpty(buff.TemplatePath) && System.IO.File.Exists(buff.TemplatePath))
                {
                    // 使用模板匹配
                    using var template = Cv2.ImRead(buff.TemplatePath);
                    if (template != null && !template.Empty())
                    {
                        return CheckIconByTemplate(iconMat, template, buff.SimilarityThreshold);
                    }
                    return false;
                }
                else
                {
                    // 回退到亮度检测
                    return IsIconBright(iconMat);
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 通过图像识别检查技能是否可用
        /// </summary>
        private bool CheckSkillVisually(SkillRuntimeState skill)
        {
            var region = skill.Config.IconRegion;
            if (region == null || region.Length < 4 || region[2] <= 0 || region[3] <= 0)
            {
                return true; // 未配置区域则跳过视觉检查
            }

            // 1. 截取技能图标区域
            using var iconMat = _imageInterface.GetScreenRegion(region[0], region[1], region[2], region[3]);
            if (iconMat == null || iconMat.Empty()) return true;

            // 2. 如果配置了模板路径，进行模板匹配
            if (!string.IsNullOrEmpty(skill.Config.TemplatePath) && System.IO.File.Exists(skill.Config.TemplatePath))
            {
                using var template = Cv2.ImRead(skill.Config.TemplatePath);
                if (template != null && !template.Empty())
                {
                    return CheckIconByTemplate(iconMat, template, skill.Config.SimilarityThreshold);
                }
            }

            // 3. 如果没有模板，可以进行简单的亮度/颜色分析（示例：检测图标是否变灰）
            // 变灰通常意味着饱和度降低或亮度降低
            return IsIconBright(iconMat);
        }

        /// <summary>
        /// 识别技能图标（模板匹配）
        /// </summary>
        private bool CheckIconByTemplate(Mat icon, Mat template, double threshold)
        {
            using var result = new Mat();
            Cv2.MatchTemplate(icon, template, result, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out _);
            return maxVal >= threshold;
        }

        /// <summary>
        /// 简单分析图标亮度（用于判断是否可用）
        /// </summary>
        private bool IsIconBright(Mat icon)
        {
            using var hsv = new Mat();
            Cv2.CvtColor(icon, hsv, ColorConversionCodes.BGR2HSV);
            var mean = Cv2.Mean(hsv);
            // V 通道（亮度）平均值大于 100 认为亮起（需根据实际游戏调整）
            return mean.Val2 > 100;
        }
    }
}
