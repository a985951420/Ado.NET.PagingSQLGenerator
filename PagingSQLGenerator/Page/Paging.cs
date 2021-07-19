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
    public class Paging : IDisposable
    {
        /// <summary>
        /// 日志
        /// </summary>
        public Action<string> Log { get; set; }
        /// <summary>
        /// 分页前缀
        /// </summary>
        readonly string prefix = string.Empty;
        PagingType _pageType;
        GrammarType _grammarType;
        public enum PagingType
        {
            /// <summary>
            /// 低版本12一下
            /// </summary>
            RowNumber,
            /// <summary>
            /// 12及以上版本
            /// </summary>
            OFFSET
        }
        public Paging() : this(PagingType.RowNumber)
        {
        }
        public Paging(PagingType pagingType, GrammarType grammarType = GrammarType.Original)
        {
            prefix = Str_char(5);
            _pageType = pagingType;
            _grammarType = grammarType;
        }
        /// <summary>
        /// 参数占位符
        /// </summary>
        private string _param
        {
            get
            {
                return $"@{prefix}AutoParameter_";
            }
        }
        /// <summary>
        /// rowNumber
        /// </summary>
        string _rowNumberName
        {
            get
            {
                return $"[{prefix}_RowNumber]";
            }
        }
        /// <summary>
        /// SQL参数化
        /// </summary>
        Dictionary<string, string> _Tsql
        {
            get
            {
                var dic = new Dictionary<string, string>()
                {
                    //排序SQL 正序
                    { "orderBydescending", " ORDER BY {0} DESC "},  
                    //排序SQL倒序
                    { "orderBy"," ORDER BY {0} ASC "},
                    //分页SQL
                    { "pageoffset"," OFFSET {0} ROWS FETCH NEXT {1} ROWS ONLY "},
                    //分页SQL
                    { "pagenumber"," SELECT * FROM(SELECT *, ROW_NUMBER() OVER ({0}) AS " + _rowNumberName + " FROM ({1}) paging_autoTable) AS paging_rowTable  {2}"},
                    //总条数
                    { "count"," SELECT COUNT(1) FROM ( {0}  {1} ) paging_autoTable "},
                    //参数化
                    { "parameter"," {0} {1} {2} " + _param + "{3} "},
                    //Between And 
                    { "parameterBetweenAnd"," {0} {1} Between  " + _param + "{2} AND " + _param + "{3} " },
                };
                return dic;
            }
        }

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
        /// SQL格式化处理
        /// </summary>
        static Dictionary<GrammarType, Func<string, string>> FunSqlHandler = new Dictionary<GrammarType, Func<string, string>>
        {
            {GrammarType.Original,(sql) => { return sql; }  },
            {GrammarType.UpperCase,(sql) => { return sql.ToUpper(); }  },
            {GrammarType.LowerCase,(sql) => { return sql.ToLower(); }  },
        };
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
            if (pageIndex == 0)
                pageIndex += 1;
            var startPage = (pageIndex - 1) * pageSize;
            switch (_pageType)
            {
                case PagingType.RowNumber:
                    Where(_rowNumberName, ParameterType.BetweenAnd, startPage, pageSize);
                    break;
                case PagingType.OFFSET:
                    _paging.AppendFormat(_Tsql["pageoffset"], startPage, pageSize);
                    break;
                default:
                    break;
            }
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
        /// 条件
        /// </summary>
        /// <param name="condition">条件</param>
        /// <param name="columnName">参数名称</param>
        /// <param name="parameterType">查询类型</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public void WhereIf(bool condition, string columnName, ParameterType parameterType = ParameterType.Equal, params object[] value)
        {
            if (condition)
                Where(columnName, parameterType, value);
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
            var countSql = string.Format(_Tsql["count"], _tSql, countWhereSql);
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
                if (_pageType == PagingType.OFFSET)
                {
                    if (_paging.Length <= 0)
                        throw new Exception("请添加分页！");
                }
                else
                {
                    var any = parameters.Any(s => s.Any(c => c.ColumnName == _rowNumberName));
                    if (!any)
                        throw new Exception("请添加分页！");
                }
                if (_tSql.Length <= 0)
                    throw new Exception("请添加查询！");
                var finalSql = string.Empty;
                switch (_pageType)
                {
                    case PagingType.RowNumber:
                        finalSql = string.Format(_Tsql["pagenumber"], _orderByBuilder, _tSql, whereSql, _paging);
                        break;
                    case PagingType.OFFSET:
                        finalSql = $"{_tSql}  {whereSql} {_orderByBuilder} {_paging}";
                        break;
                    default:
                        throw new Exception("SQL分页不兼容");
                }
                var handler = FunSqlHandler[_grammarType];
                return handler.Invoke(finalSql);
            }
        }
        /// <summary>
        /// 默认不是Count信号
        /// </summary>
        bool countSign = false;
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
                    if (countSign)
                    {
                        var notRowNameCount = item.Count(s => s.ColumnName != _rowNumberName);
                        if (notRowNameCount <= 0)
                            continue;
                    }
                    var parametersIndex = 0;
                    where.Append(" AND ( ");
                    foreach (var pitem in item.Where(s => s.AutoAppend))
                    {
                        if (countSign)
                        {
                            if (pitem.ColumnName == _rowNumberName)
                                continue;
                        }
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
        /// count 条件设置 Where SQL
        /// </summary>
        string countWhereSql
        {
            get
            {
                //设置Count查询去除RowNumber条件
                countSign = true;
                var sql = whereSql;
                //恢复默认添加RowNumber条件
                countSign = false;
                return sql;
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
            if (Log != null)
            {
                var sb = new StringBuilder();
                sb.Append("---------------------TSQL-Page--------------------------" + Environment.NewLine);
                sb.AppendFormat("Sql : {0}", _SQL + Environment.NewLine);
                sb.Append("Parameters：" + Environment.NewLine);
                sb.Append(string.Join(Environment.NewLine, GetParameters().Select(s => $"Key：【{s.Key}】 Value：【{s.Value}】")) + Environment.NewLine);
                sb.Append("-----------------------------------------------------------" + Environment.NewLine);
                Log.Invoke(sb.ToString());
            }
            return _SQL;
        }
        /// <summary>
        /// 带参数可执行TSQL
        /// </summary>
        /// <returns></returns>
        public string ExecuteTSql()
        {
            var temp = @"DECLARE {0} {1};SET {0} = '{2}';";
            var parameters = GetParameters();
            var sqlSb = new StringBuilder();
            foreach (var item in parameters)
            {
                var v = item.Value.GetType();
                if (!DataType.ContainsKey(v))
                    throw new Exception($"不支持类型转TSQL:【{v.Name}】");
                var valueType = DataType[v];
                if (v.Equals(typeof(string)))
                    valueType = string.Format(valueType, ((string)item.Value).Length);
                sqlSb.AppendFormat(temp, item.Key, valueType, item.Value);
            }
            sqlSb.AppendLine(_SQL);
            return sqlSb.ToString();
        }
        Dictionary<Type, string> DataType = new Dictionary<Type, string>
        {
            { typeof(bool), "Bit"},
            { typeof(char), "Char"},
            { typeof(DateTime), "DateTime"},
            { typeof(DateTimeOffset), "DateTimeOffset"},
            { typeof(decimal), "Decimal"},
            { typeof(Guid), "UniqueIdentifier"},
            { typeof(short), "SmallInt"},
            { typeof(int), "Int"},
            { typeof(long), "BigInt"},
            { typeof(string), "NVarChar({0})"},
        };
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
        /// 生成随机纯字母随机数
        /// </summary>
        /// <param name="IntStr">生成长度</param>
        /// <returns></returns>
        public string Str_char(int Length)
        {
            return Str_char(Length, false);
        }
        /// <summary>
        /// 生成随机纯字母随机数
        /// </summary>
        /// <param name="Length">生成长度</param>
        /// <param name="Sleep">是否要在生成前将当前线程阻止以避免重复</param>
        /// <returns></returns>
        public string Str_char(int Length, bool Sleep)
        {
            if (Sleep) System.Threading.Thread.Sleep(3);
            char[] Pattern = new char[] { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };
            string result = "";
            int n = Pattern.Length;
            System.Random random = new Random(~unchecked((int)DateTime.Now.Ticks));
            for (int i = 0; i < Length; i++)
            {
                int rnd = random.Next(0, n);
                result += Pattern[rnd];
            }
            return result;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                if (disposing)
                {
                    // Release managed resources
                    DataType = null;
                }

                // Release unmanaged resources

                m_disposed = true;
            }
        }

        ~Paging()
        {
            Dispose(false);
        }
        private bool m_disposed;
    }
}
