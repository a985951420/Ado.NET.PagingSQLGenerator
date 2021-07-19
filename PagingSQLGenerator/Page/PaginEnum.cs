namespace PagingSQLGenerator.Page
{
    /// <summary>
    /// 查询值类型
    /// </summary>
    public enum ParameterType
    {
        /// <summary>
        /// 等于
        /// </summary>
        Equal,
        /// <summary>
        /// LIKE
        /// </summary>
        LIKE,
        /// <summary>
        /// RightLike
        /// </summary>
        RightLike,
        /// <summary>
        /// LeftLike
        /// </summary>
        LeftLike,
        /// <summary>
        /// IN
        /// </summary>
        IN,
        /// <summary>
        /// 区间
        /// </summary>
        BetweenAnd,
        /// <summary>
        /// 大于
        /// </summary>
        GreaterThan,
        /// <summary>
        /// 大于等于
        /// </summary>
        GreaterThanEqual,
        /// <summary>
        /// 小于
        /// </summary>
        Lessthan,
        /// <summary>
        /// 小于等于
        /// </summary>
        LessThanEqual,
    }


    /// <summary>
    /// 条件连接
    /// </summary>
    public enum ParameterLinkType
    {
        /// <summary>
        /// 并且
        /// </summary>
        AND,
        /// <summary>
        /// 或者
        /// </summary>
        OR
    }

    /// <summary>
    /// SQL 大小写
    /// </summary>
    public enum GrammarType
    {
        /// <summary>
        /// 原始
        /// </summary>
        Original = 0,
        /// <summary>
        /// 大写
        /// </summary>
        UpperCase = 1,
        /// <summary>
        /// 小写
        /// </summary>
        LowerCase = 2
    }
}
