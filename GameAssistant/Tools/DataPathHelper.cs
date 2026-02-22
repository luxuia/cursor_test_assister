using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace GameAssistant.Tools
{
    /// <summary>
    /// 解析 Data 目录路径，兼容运行目录、输出目录、IDE 启动等场景。
    /// </summary>
    public static class DataPathHelper
    {
        /// <summary>
        /// 返回候选的 Data 目录路径（按优先级），用于加载物品/技能/英雄等 JSON。
        /// </summary>
        public static IEnumerable<string> GetCandidateDataDirectories()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            // 1) 程序集/exe 所在目录下的 Data（发布后双击运行）
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            if (!string.IsNullOrEmpty(baseDir))
            {
                var dataDir = Path.Combine(baseDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), "Data");
                if (seen.Add(dataDir)) yield return dataDir;
            }
            // 2) 当前工作目录下的 Data（IDE 或 dotnet run 时常见）
            var curDir = Directory.GetCurrentDirectory();
            if (!string.IsNullOrEmpty(curDir))
            {
                var curData = Path.Combine(curDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), "Data");
                if (seen.Add(curData)) yield return curData;
            }
            // 3) 入口程序集所在目录下的 Data
            string? entryData = null;
            try
            {
                var entryPath = Assembly.GetEntryAssembly()?.Location;
                if (!string.IsNullOrEmpty(entryPath))
                {
                    var entryDir = Path.GetDirectoryName(entryPath);
                    if (!string.IsNullOrEmpty(entryDir))
                        entryData = Path.Combine(entryDir, "Data");
                }
            }
            catch { /* 忽略 */ }
            if (entryData != null && seen.Add(entryData))
                yield return entryData;
        }
    }
}
