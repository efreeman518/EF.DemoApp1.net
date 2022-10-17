using System.Collections.Generic;

namespace Application.Contracts.Model;

public class PagedResponse<T>
{
    public int PageSize { get; set; }
    public int PageIndex { get; set; }
    public int Total { get; set; }
    public List<T> Data { get; set; } = new List<T>();
}
