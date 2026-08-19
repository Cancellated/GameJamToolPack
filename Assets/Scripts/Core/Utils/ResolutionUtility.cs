using System.Collections.Generic;
using UnityEngine;

namespace MyGame.Utils
{
    /// <summary>
    /// 分辨率列表的单一事实来源。
    /// UI 下拉框与 GameSettings.ApplyToGame 必须使用同一份去重列表，
    /// 避免 ResolutionIndex 因 Screen.resolutions 顺序/重复项导致错位。
    /// </summary>
    public static class ResolutionUtility
    {
        private static Resolution[] s_cachedResolutions;

        /// <summary>
        /// 按宽高去重后的分辨率列表（不包含刷新率差异）。
        /// </summary>
        public static Resolution[] GetUniqueResolutions()
        {
            if (s_cachedResolutions != null && s_cachedResolutions.Length > 0)
            {
                return s_cachedResolutions;
            }

            Resolution[] rawResolutions = Screen.resolutions;
            if (rawResolutions == null || rawResolutions.Length == 0)
            {
                s_cachedResolutions = new[]
                {
                    new Resolution
                    {
                        width = Screen.width > 0 ? Screen.width : 1920,
                        height = Screen.height > 0 ? Screen.height : 1080,
                        refreshRateRatio = new RefreshRate { numerator = 60, denominator = 1 },
                    },
                };
                return s_cachedResolutions;
            }

            List<Resolution> unique = new();
            HashSet<string> keys = new();

            foreach (Resolution resolution in rawResolutions)
            {
                string key = $"{resolution.width}x{resolution.height}";
                if (keys.Add(key))
                {
                    unique.Add(resolution);
                }
            }

            s_cachedResolutions = unique.ToArray();
            return s_cachedResolutions;
        }

        /// <summary>
        /// 尝试按索引获取分辨率；索引越界时返回 false。
        /// </summary>
        public static bool TryGetResolution(int index, out Resolution resolution)
        {
            Resolution[] resolutions = GetUniqueResolutions();
            if (index >= 0 && index < resolutions.Length)
            {
                resolution = resolutions[index];
                return true;
            }

            resolution = default;
            return false;
        }

        /// <summary>
        /// 按当前屏幕宽高匹配索引；匹配失败返回 0。
        /// </summary>
        public static int GetCurrentResolutionIndex()
        {
            Resolution current = Screen.currentResolution;
            Resolution[] resolutions = GetUniqueResolutions();

            for (int i = 0; i < resolutions.Length; i++)
            {
                if (resolutions[i].width == current.width && resolutions[i].height == current.height)
                {
                    return i;
                }
            }

            return 0;
        }

        /// <summary>
        /// 将分辨率索引钳制到合法范围。
        /// </summary>
        public static int ClampResolutionIndex(int index)
        {
            Resolution[] resolutions = GetUniqueResolutions();
            return resolutions.Length > 0 ? Mathf.Clamp(index, 0, resolutions.Length - 1) : 0;
        }
    }
}
