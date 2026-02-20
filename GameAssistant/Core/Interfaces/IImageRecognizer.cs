using System.Drawing;
using System.Threading.Tasks;
using GameAssistant.Core.Models;

namespace GameAssistant.Core.Interfaces
{
    /// <summary>
    /// 图像识别接口
    /// </summary>
    public interface IImageRecognizer
    {
        /// <summary>
        /// 识别英雄阵容
        /// </summary>
        Task<HeroRosterResult> RecognizeHeroRosterAsync(Bitmap frame);

        /// <summary>
        /// 识别小地图站位
        /// </summary>
        Task<MinimapResult> RecognizeMinimapAsync(Bitmap frame);

        /// <summary>
        /// 识别装备信息
        /// </summary>
        Task<EquipmentResult> RecognizeEquipmentAsync(Bitmap frame);

        /// <summary>
        /// 识别血量和技能状态
        /// </summary>
        Task<StatusResult> RecognizeStatusAsync(Bitmap frame);
    }
}
