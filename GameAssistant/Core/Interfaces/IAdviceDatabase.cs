using System.Collections.Generic;
using System.Threading.Tasks;
using GameAssistant.Core.Models;

namespace GameAssistant.Core.Interfaces
{
    /// <summary>
    /// 建议数据库接口
    /// </summary>
    public interface IAdviceDatabase
    {
        /// <summary>
        /// 初始化数据库
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// 查询匹配的建议规则
        /// </summary>
        Task<List<AdviceRule>> QueryRulesAsync(AdviceCondition condition);

        /// <summary>
        /// 添加建议规则
        /// </summary>
        Task AddRuleAsync(AdviceRule rule);

        /// <summary>
        /// 删除建议规则
        /// </summary>
        Task DeleteRuleAsync(int ruleId);

        /// <summary>
        /// 获取所有规则
        /// </summary>
        Task<List<AdviceRule>> GetAllRulesAsync();

        /// <summary>
        /// 导入规则列表（替换当前所有规则）
        /// </summary>
        Task ImportRulesAsync(List<AdviceRule> rules);
    }
}
