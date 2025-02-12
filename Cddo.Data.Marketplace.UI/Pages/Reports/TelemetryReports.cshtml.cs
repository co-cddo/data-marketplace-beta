using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Cddo.Data.Marketplace.UI.Model;
using Cddo.Data.Marketplace.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Azure;

namespace Cddo.Data.Marketplace.UI.Pages.Reports
{
    public class TelemetryReportsModel : PageModel
    {
        public IUserRoleService UserRoleService { get; }
        public IManageOrganisationsService ManageOrganisationsService { get; }
        public TelemetryReportsModel(IUserRoleService userRoleService, IManageOrganisationsService manageOrganisationsService) 
        {
            UserRoleService = userRoleService;
            ManageOrganisationsService = manageOrganisationsService;
        }
        
        private readonly string _directoryPath = "Pages/Reports/ReportTemplates/Telemetry/";
        public List<ReportTemplate> PagedItems { get; set; } = new List<ReportTemplate>();
        public string? SearchTerm { get; set; }
        public int CurrentPage { get; set; }
        public int Count { get; set; }
        public int PageSize { get; set; } = 10; // Number of items per page
        public int TotalPages =1;
        public async Task OnGet(string searchReport = "", int pageNumber = 1, int pageSize = 10)
        {
            //Get a list of all telemetry files
            List<ReportTemplate> reports = new List<ReportTemplate>();

            try
            {
                string[] files = Directory.GetFiles(_directoryPath);
                foreach (string file in files)
                {
                    var jsonHandler = new ReadWriteJson<ReportTemplate>(file);
                    var report = await jsonHandler.ReadJsonAsync(file);
                    if(!report.IsPredefined)
                    {
                        var userProfile = await UserRoleService.GetUserByIdAsync(report.UserId.ToString());
                        report.Owner = userProfile!.User!.UserName;
                    }
                    else
                    {
                        var organisation = await ManageOrganisationsService.GetOrganisationAsync((int)report.OrganisationId);
                        report.Owner = organisation!.OrganisationName;
                    }
                    
                    reports.Add(report);
                }
            }
            catch (Exception)
            {

                throw;
            }

            if (!string.IsNullOrEmpty(searchReport))
            {
                reports = reports.Where(x => x.ReportName.ToLower().Contains(searchReport.ToLower()) 
                || x.ReportType.ToString().ToLower().Contains(searchReport.ToLower()) 
                || x.Owner.ToLower().Contains(searchReport.ToLower())).ToList();
            }

            SearchTerm = searchReport;
            PageSize = pageSize;
            TotalPages = (int)Math.Ceiling(reports!.Count / (double)PageSize);
            Count = reports.Count;
            CurrentPage = pageNumber;

            PagedItems = reports
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();
        }
    }
}
