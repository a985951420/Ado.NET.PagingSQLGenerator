using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PagingSQLGenerator.Page
{
    /// <summary>
    /// author： Time
    /// 分页
    /// </summary>
    public class Paging
    {
        /// <summary>
        /// 参数占位符
        /// </summary>
        private const string _param = "@AutoParameter_";
        /// <summary>
        /// SQL参数化
        /// </summary>
        Dictionary<string, string> _Tsql = new Dictionary<string, string>
            {
                { "orderBydescending", " ORDER BY {0} DESC "},  //排序SQL 正序
                { "orderBy"," ORDER BY {0} ASC "},  //排序SQL倒序
                { "page"," OFFSET {0} ROWS FETCH NEXT {1} ROWS ONLY "},//分页SQL
                { "count"," SELECT COUNT(1) FROM ( {0}  {1} ) paging_autoTable "},//总条数
                { "parameter"," {0} {1} {2} "+_param+"{3} "},//参数化
                { "parameterBetweenAnd"," {0} {1} Between  " + _param + "{2} AND " + _param + "{3} " },//Between And 
            };
        /// <summary>
        /// 排序
        /// </summary>
        private readonly StringBuilder _orderByBuilder = new StringBuilder();
        /// <summary>
        /// 分页
        /// </summary>
        private readonly StringBuilder _paging = new StringBuilder();
        /// <summary>
        /// SQL语句
        /// </summary>
        private readonly StringBuilder _tSql = new StringBuilder();
        /// <summary>
        /// 参数
        /// </summary>
        private readonly List<List<PagingParameterConfiguration>> parameters = new List<List<PagingParameterConfiguration>>();
        /// <summary>
        /// 倒序
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public void OrderByDescending(string columnName)
        {
            if (_orderByBuilder.Length > 0)
                throw new Exception("应该存在排序字段！");
            _orderByBuilder.AppendFormat(_Tsql["orderBydescending"], columnName);
        }
        /// <summary>
        /// 正序
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public void OrderBy(string columnName)
        {
            if (_orderByBuilder.Length > 0)
                throw new Exception("应该存在排序字段！");
            _orderByBuilder.AppendFormat(_Tsql["orderBy"], columnName);
        }

        /// <summary>
        /// 分页
        /// </summary>
        /// <param name="pageIndex">index</param>
        /// <param name="pageSize">Size</param>
        /// <returns></returns>
        public void Page(int pageIndex, int pageSize)
        {
            if (_paging.Length > 0)
                throw new Exception("已存在分页！");
            var startPage = (pageIndex - 1) * pageSize;
            _paging.AppendFormat(_Tsql["page"], startPage, pageSize);
        }

        /// <summary>
        /// 查询SQL
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public void Body(string sql)
        {
            if (_tSql.Length > 0)
                throw new Exception("查询已存在！");
            _tSql.Append($" {sql} ");
        }

        /// <summary>
        /// 条件
        /// </summary>
        /// <param name="columnName">参数名称</param>
        /// <param name="parameterType">查询类型</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public void Where(string columnName, ParameterType parameterType = ParameterType.Equal, params object[] value)
        {
            AddSingleParameter(columnName, value, parameterType, ParameterLinkType.AND, true);
        }
        /// <summary>
        /// 或者条件
        /// </summary>
        public void OrArea(Action<PagingOrArea> action)
        {
            var pagingParameterConfigurations = new List<PagingParameterConfiguration>();
            var area = new PagingOrArea(ref pagingParameterConfigurations);
            action(area);
            if (pagingParameterConfigurations.Any())
                parameters.Add(pagingParameterConfigurations);
        }
        /// <summary>
        /// 获取总条数SQL
        /// </summary>
        /// <returns></returns>
        public string Count()
        {
            if (_tSql.Length <= 0)
                throw new Exception("请添加查询！");
            var countSql = string.Format(_Tsql["count"], _tSql, whereSql);
            var sb = new StringBuilder();
            sb.Append("---------------------TSQL-Count--------------------------" + Environment.NewLine);
            sb.AppendFormat("Sql : {0}", countSql + Environment.NewLine);
            sb.Append("Parameters：" + Environment.NewLine);
            sb.Append(string.Join(Environment.NewLine, GetParameters().Select(s => $"Key：【{s.Key}】 Value：【{s.Value}】")) + Environment.NewLine);
            sb.Append("------------------------------------------------------------" + Environment.NewLine);
            Trace.WriteLine(sb.ToString());
            return countSql;
        }
        /// <summary>
        /// 获取参数
        /// </summary>
        /// <param name="isAll">是否获取所有 默认自动化的参数</param>
        /// <returns></returns>
        public Dictionary<string, object> GetParameters()
        {
            var param = new Dictionary<string, object>();
            var index = 0;
            foreach (var item in parameters)
            {
                var filterParameters = item.ToList();
                //Like参数不再输出
                foreach (var pitem in filterParameters)
                {
                    switch (pitem.ParameterType)
                    {
                        case ParameterType.GreaterThan:
                        case ParameterType.GreaterThanEqual:
                        case ParameterType.Lessthan:
                        case ParameterType.LessThanEqual:
                        case ParameterType.Equal:
                            param.Add(_param + index, pitem.Values[0]);
                            index++;
                            break;
                        case ParameterType.BetweenAnd:
                        case ParameterType.IN:
                            foreach (var val in pitem.Values)
                            {
                                param.Add(_param + index, val);
                                index++;
                            }
                            break;
                        case ParameterType.LIKE:
                            param.Add(_param + index, $"%{pitem.Values[0]}%");
                            index++;
                            break;
                        case ParameterType.RightLike:
                            param.Add(_param + index, $"{pitem.Values[0]}%");
                            index++;
                            break;
                        case ParameterType.LeftLike:
                            param.Add(_param + index, $"%{pitem.Values[0]}");
                            index++;
                            break;
                    }
                }
            }
            return param;
        }

        /// <summary>
        /// 分页SQL语句
        /// </summary>
        string _SQL
        {
            get
            {
                if (_orderByBuilder.Length <= 0)
                    throw new Exception("请添加排序！");
                if (_paging.Length <= 0)
                    throw new Exception("请添加分页！");
                if (_tSql.Length <= 0)
                    throw new Exception("请添加查询！");

                return $"{_tSql}  {whereSql} {_orderByBuilder} {_paging}";
            }
        }
        /// <summary>
        /// Where SQL
        /// </summary>
        string whereSql
        {
            get
            {
                var index = 0;
                var where = new StringBuilder(" Where 1=1 ");
                foreach (var item in parameters)
                {
                    var parametersIndex = 0;
                    where.Append(" AND ( ");
                    foreach (var pitem in item.Where(s => s.AutoAppend))
                    {
                        var psymbol = string.Empty;
                        if (parametersIndex != 0)
                        {
                            switch (pitem.LinkType)
                            {
                                case ParameterLinkType.AND:
                                    psymbol = " AND ";
                                    break;
                                case ParameterLinkType.OR:
                                    psymbol = " OR ";
                                    break;
                            }
                        }
                        var tsql = _Tsql["parameter"];
                        switch (pitem.ParameterType)
                        {
                            case ParameterType.Equal:
                                where.AppendFormat(tsql, psymbol, pitem.ColumnName, "=", index);
                                index++;
                                break;
                            case ParameterType.LIKE:
                            case ParameterType.RightLike:
                            case ParameterType.LeftLike:
                                where.AppendFormat(tsql, psymbol, pitem.ColumnName, "LIKE", index);
                                index++;
                                break;
                            case ParameterType.IN:
                                var parameters = new List<string>();
                                foreach (var val in pitem.Values)
                                {
                                    parameters.Add(_param + index);
                                    index++;
                                }
                                where.AppendFormat(tsql, psymbol, pitem.ColumnName, " IN ", $"({string.Join(",", parameters)})");
                                break;
                            case ParameterType.BetweenAnd:
                                where.AppendFormat(_Tsql["parameterBetweenAnd"], psymbol, pitem.ColumnName, index++, index++);
                                break;
                            case ParameterType.GreaterThan:
                                where.AppendFormat(tsql, psymbol, pitem.ColumnName, ">", index);
                                index++;
                                break;
                            case ParameterType.GreaterThanEqual:
                                where.AppendFormat(tsql, psymbol, pitem.ColumnName, ">=", index);
                                index++;
                                break;
                            case ParameterType.Lessthan:
                                where.AppendFormat(tsql, psymbol, pitem.ColumnName, "<", index);
                                index++;
                                break;
                            case ParameterType.LessThanEqual:
                                where.AppendFormat(tsql, psymbol, pitem.ColumnName, "<=", index);
                                index++;
                                break;
                        }
                        parametersIndex++;
                    }

                    where.Append(" ) ");
                }
                return where.ToString();
            }
        }
        /// <summary>
        /// 追加条件
        /// </summary>
        /// <param name="columnName">列名</param>
        /// <param name="values">值</param>
        /// <param name="parameterType">值类型</param>
        /// <param name="pagingParamType">连接类型</param>
        /// <param name="auto">是否自动追加</param>
        /// <returns></returns>
        protected void AddSingleParameter(string columnName, object[] values, ParameterType parameterType, ParameterLinkType pagingParamType, bool auto = false)
        {
            Verification(columnName);
            columnName = columnName.Trim();
            if (!new Regex(@"^\[.*\]$").IsMatch(columnName))
                columnName = "[" + columnName + "]";
            parameters.Add(new List<PagingParameterConfiguration>
                {
                    new PagingParameterConfiguration
                    {
                        ColumnName = columnName,
                        ParameterType = parameterType,
                        AutoAppend = auto,
                        LinkType = pagingParamType,
                        Values = values
                    }
                });
        }

        /// <summary>
        /// 验证参数是否存在当前名称
        /// </summary>
        /// <param name="columnName">参数名称</param>
        void Verification(string columnName)
        {
            if (parameters.Any(s => s.Any(c => c.ColumnName.ToLower() == columnName.ToLower())))
                throw new Exception("已存在参数！参数添加重复。");
        }

        /// <summary>
        /// 生成SQL语句
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("---------------------TSQL-Page--------------------------" + Environment.NewLine);
            sb.AppendFormat("Sql : {0}", _SQL + Environment.NewLine);
            sb.Append("Parameters：" + Environment.NewLine);
            sb.Append(string.Join(Environment.NewLine, GetParameters().Select(s => $"Key：【{s.Key}】 Value：【{s.Value}】")) + Environment.NewLine);
            sb.Append("-----------------------------------------------------------" + Environment.NewLine);
            Trace.WriteLine(sb.ToString());
            return _SQL;
        }

        /// <summary>
        /// 所有参数
        /// </summary>
        public class PagingParameterArea
        {
            /// <summary>
            /// 所有参数
            /// </summary>
            public List<PagingParameterConfiguration> PagingParameters { get; set; }
        }

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
    }
}
