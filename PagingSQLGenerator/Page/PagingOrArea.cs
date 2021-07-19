using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PagingSQLGenerator.Page
{
    /// <summary>
    /// 分页Or区域
    /// </summary>
    public class PagingOrArea
    {
        List<PagingParameterConfiguration> _pagingParameterConfigurations;
        /// <summary>
        /// PagingOrArea
        /// </summary>
        /// <param name="pagingParameterConfigurations"></param>
        public PagingOrArea(ref List<PagingParameterConfiguration> pagingParameterConfigurations)
        {
            _pagingParameterConfigurations = pagingParameterConfigurations;
        }

        /// <summary>
        /// 强行添加条件 并不自动追加
        /// </summary>
        /// <param name="columnName">列名</param>
        /// <param name="parameterType">值类型</param>
        /// <param name="pagingParamType">连接类型</param>
        /// <param name="values">值</param>
        /// <returns></returns>
        public void Where(string columnName, ParameterType parameterType, ParameterLinkType pagingParamType, params object[] values)
        {
            columnName = columnName.Trim();
            if (!new Regex(@"^\[.*\]$").IsMatch(columnName))
                columnName = "[" + columnName + "]";
            _pagingParameterConfigurations.Add(new PagingParameterConfiguration
            {
                ColumnName = columnName,
                ParameterType = parameterType,
                AutoAppend = true,
                LinkType = pagingParamType,
                Values = values
            });
        }
        /// <summary>
        /// 强行添加条件 并不自动追加
        /// </summary>
        /// <param name="columnName">条件</param>
        /// <param name="columnName">列名</param>
        /// <param name="parameterType">值类型</param>
        /// <param name="pagingParamType">连接类型</param>
        /// <param name="values">值</param>
        /// <returns></returns>
        public void Whereif(bool condition, string columnName, ParameterType parameterType, ParameterLinkType pagingParamType, params object[] values)
        {
            if (condition)
                Where(columnName, parameterType, pagingParamType, values);
        }
    }
}
