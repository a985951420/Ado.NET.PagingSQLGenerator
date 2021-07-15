using PagingSQLGenerator.Page;
using System;

namespace PagingSQLGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            var page = new Paging(Paging.PagingType.OFFSET);
            var value = 1M;
            page.Body("SELECT * FROM ORDERS");
            page.Where("IsLocked", ParameterType.Equal, value);
            page.OrArea((parameter) =>
            {
                parameter.Where("OrderNumber", ParameterType.LeftLike, ParameterLinkType.OR, "B");
            });
            page.OrderByDescending("Id");
            page.Page(1, 10);
            var sql = page.ToString();
            var esql = page.ExecuteSql();
            var parameters = page.GetParameters();
            var count = page.Count();

            Console.WriteLine("分页： " + sql);
            Console.WriteLine("总数： " + count);
            Console.Read();
        }
    }
}
