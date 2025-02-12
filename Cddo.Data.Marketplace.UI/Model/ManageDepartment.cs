using Cddo.Data.Marketplace.Api.Dto.ManageUser;

namespace Cddo.Data.Marketplace.UI.Model
{
    public class ManageDepartment
    {
        public string? SearchTerm { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int PageNumber { get; set; } = 1;

        public List<Department>? Departments { get; set; }
    }
}
