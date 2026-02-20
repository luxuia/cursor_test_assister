using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GameAssistant.Tools
{
    /// <summary>
    /// 物品图标下载工具（扩展HeroIconDownloader）
    /// </summary>
    public static class ItemIconDownloader
    {
        /// <summary>
        /// 从Liquipedia下载DOTA 2物品图标
        /// </summary>
        public static async Task DownloadDota2ItemIconsAsync(
            List<ItemInfo> items, 
            string downloadDirectory = "Templates/Equipment",
            IProgress<string>? progress = null)
        {
            var downloader = new HeroIconDownloader(downloadDirectory);
            await downloader.DownloadDota2ItemIconsAsync(items, progress);
        }
    }
}
