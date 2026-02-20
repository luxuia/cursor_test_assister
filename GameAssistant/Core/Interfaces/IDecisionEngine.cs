using System.Collections.Generic;
using System.Threading.Tasks;
using GameAssistant.Core.Models;

namespace GameAssistant.Core.Interfaces
{
    /// <summary>
    /// 决策分析引擎接口
    /// </summary>
    public interface IDecisionEngine
    {
        /// <summary>
        /// 分析当前状态并生成建议
        /// </summary>
        Task<List<Advice>> AnalyzeAsync(GameState gameState);

        /// <summary>
        /// 更新游戏状态
        /// </summary>
        void UpdateGameState(GameState gameState);
    }
}
