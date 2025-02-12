namespace Cddo.Data.Marketplace.Api.Dto.Requests
{
    public class ManageDepartmentRequest
    {
        public string? SearchTerm { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int PageNumber { get; set; } = 1;

    }
}
