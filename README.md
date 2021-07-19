```csharp  
            //默认
            var page1 = new Paging();
            //使用
            var page = new Paging(Paging.PagingType.OFFSET, GrammarType.UpperCase)
            {
                Log = (message) =>
                {
                    Trace.WriteLine(message);
                },
            };
            var orderNumber = string.Empty;
            bool? bl = null;
            page.Body(@"SELECT * FROM dbo.table1 a
                                  JOIN dbo.table2 b ON a.Id=b.OrderId");
            page.WhereIf(bl != null, "IsLocked", ParameterType.Equal, bl);
            page.OrArea((parameter) =>
            {
                parameter.Whereif(!string.IsNullOrEmpty(orderNumber), "OrderNumber", ParameterType.LeftLike, ParameterLinkType.OR, orderNumber);
            });
            page.OrderByDescending("a.Id");
            page.Page(1, 10);
            var sql = page.ToString();
            var esql = page.ExecuteTSql();
            var parameters = page.GetParameters();
            var count = page.Count();

            Console.WriteLine("分页： " + sql);
            Console.WriteLine("总数： " + count);
            page1.Dispose();
            Console.Read();
```
