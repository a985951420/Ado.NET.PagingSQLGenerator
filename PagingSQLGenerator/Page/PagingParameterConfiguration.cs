namespace PagingSQLGenerator.Page
{
    /// <summary>
    /// 分页参数配置
    /// </summary>
    public class PagingParameterConfiguration
    {
        /// <summary>
        /// 参数名
        /// </summary>
        public string ColumnName { get; set; }
        /// <summary>
        /// 值
        /// </summary>
        public object[] Values { get; set; }
        /// <summary>
        /// 是否自动追加
        /// </summary>
        public bool AutoAppend { get; set; }
        /// <summary>
        /// 连接类型
        /// </summary>
        public ParameterLinkType LinkType { get; set; }
        /// <summary>
        /// 值类型
        /// </summary>
        public ParameterType ParameterType { get; set; }
    }
}
