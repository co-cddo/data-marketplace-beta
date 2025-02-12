using Cddo.Data.Marketplace.Api.Dto.ManageUser;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Cddo.Data.Marketplace.Api.Dto.ManageUser.OrganisationDetail;

namespace Cddo.Data.Marketplace.Api.Dto.Responses.ManageUser
{
    public class ManageDepartmentsResponse
    {
        public List<Department>? Departments { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; }
        public int TotalCount { get; set; } = 0;        
        public int PageNumber { get; set; }
        public string? SearchTerm { get; set; }
        public int? TotalPages
        {
            get { return (int)Math.Ceiling((double)TotalCount / PageSize); }
        }
    }
}
