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
        /// 检测特定技能的实时状态
        /// </summary>
        /// <param name="skill">技能运行时状态</param>
        /// <returns>是否真正可用（基于图像识别和前置条件）</returns>
        public bool IsSkillReady(SkillRuntimeState skill)
        {
            try
            {
                // 1. 逻辑保底检查：如果逻辑冷却还没到，且视觉识别未启用，则直接返回不可用
                // 注意：如果用户要求完全靠视觉，这里可以放宽
                if (!skill.IsAvailable && !_config.AppSettings.EnableSmartMode) return false;

                // 2. 游戏状态检查（HP/MP/目标/Buff）
                var gameState = _stateMonitor.DetectGameState();
                
                // HP/MP 检查
                if (gameState.HpPercentage < skill.Config.MinHp) return false;
                if (gameState.MpPercentage < skill.Config.MinMp) return false;
                
                // 目标检查
                if (skill.Config.RequireTarget && !gameState.HasTarget) return false;

                // Buff 检查
                if (!CheckBuffConditions(skill.Config, gameState)) return false;

                // 3. 视觉识别检查
                if (_config.AppSettings.EnableSmartMode)
                {
                    return CheckSkillVisually(skill);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SkillStateDetector] 检测技能 {skill.Config.Name} 状态失败: {ex.Message}");
                return skill.IsAvailable;
            }
        }

        /// <summary>
        /// 检查技能的 Buff/Debuff 前置条件
        /// </summary>
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
