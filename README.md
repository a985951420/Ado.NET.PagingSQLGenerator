```csharp  
 Ado.NET.PagingSQLGenerator

 var page = new Paging(Paging.PagingType.OFFSET);
 page.Body("SELECT Id,A,B,C FROM A");  
 page.Where("A", Paging.ParameterType.Equal, "big");  

 page.OrArea((parameter) =>  
 {  
    parameter.Where("B", Paging.ParameterType.LeftLike, Paging.ParameterLinkType.OR, "A");  
    parameter.Where("B", Paging.ParameterType.LeftLike, Paging.ParameterLinkType.OR, "B");  
 });  
 
 page.OrderByDescending("Id");  
 page.Page(1, 10);  
 var sql = page.ToString();  
 var parameters = page.GetParameters();  
 var count = page.Count();  
 
 Console.WriteLine("分页： " + sql);  
 Console.WriteLine("总数： " + count);  
 Console.Read();  
```
