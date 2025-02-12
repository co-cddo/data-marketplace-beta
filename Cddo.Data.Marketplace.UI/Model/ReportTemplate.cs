using System.Text.Json;

namespace Cddo.Data.Marketplace.UI.Model
{
    public class ReportTemplate
    {
        public Guid? TemplateId { get; set; }
        public required string KqlQuery { get; set; }
        public ReportType? ReportType { get; set; }
        public required string ReportName { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? UserId { get; set; }
        public int? OrganisationId { get; set; }
        public List<int>? SharedWithOrganisationIds { get; set; }
        public List<int>? SharedWithUsersIds { get; set; }
        public bool IsPredefined { get; set; } = false;
        public string? Owner { get; set; }
    }

    public class ReportTemplates
    {
        public ReportTemplates()
        {
            PredefinedReports = GetAllTemplates();
        }
        public List<ReportTemplate>? PredefinedReports { get; set; }

        private List<ReportTemplate> GetAllTemplates()
        {
           string mateData = $@"
[
    {{
      ""TemplateId"": ""02F4F56E-5CC8-417A-9E5F-60524FAF8AD9"",
      ""KqlQuery"": ""AppEvents| extend UserName = tostring(Properties.UserName), EventName = Name| where UserName != 'No User' and UserName != ''| summarize Count = count() by UserName, EventName| order by Count desc"",
      ""ReportType"": 1,
      ""ReportName"": ""User event activity summary"",
      ""CreatedOn"": ""2024-10-16T12:34:56.789Z""
    }},
    {{
      ""TemplateId"": ""1B7CDA7E-5CA0-48C9-8CD5-FA4E0F878892"",
      ""KqlQuery"": ""AppEvents| extend UserName = tostring(Properties.UserName), EventName = Name, PageName = tostring(Properties.PageName)| where UserName != 'No User' and UserName != ''| where Name == 'UserPageNavigation' | summarize Count = count() by UserName, EventName, PageName | order by Count desc"",
      ""ReportType"": 1,
      ""ReportName"": ""User page navigation events"",
      ""CreatedOn"": ""2024-10-15T12:34:56.789Z""
    }},
    {{
      ""TemplateId"": ""F084424B-5B2C-4057-BA9F-E3F80CCC9B56"",
      ""KqlQuery"": ""AppEvents| extend UserName = coalesce(tostring(Properties.UserName), 'No user'), UserId = coalesce(tostring(Properties.UserId), 'No id')| where UserName != 'No User' or UserId == '-1'| where Name == 'UserFailedLoginAttempt' | summarize Count = count() by UserName, UserId | order by Count desc"",
      ""ReportType"": 1,
      ""ReportName"": ""Failed login attempt"",
      ""CreatedOn"": ""2024-10-15T12:34:56.789Z""
    }},
    {{
      ""TemplateId"": ""818E7FB6-8BF0-45A5-BF69-F0568D3E3E87"",
      ""KqlQuery"": ""AppEvents| extend UserName = coalesce(tostring(Properties.UserName), 'No user'), UserId = coalesce(tostring(Properties.UserId), 'No id'), UserEmail = coalesce(tostring(Properties.UserEmail), 'No email') | where Name == 'UserLogin' | summarize Count = count() by UserName, UserId, UserEmail | order by Count desc"",
      ""ReportType"": 1,
      ""ReportName"": ""User login events"",
      ""CreatedOn"": ""2024-10-15T12:34:56.789Z""
    }}
]";

            List<ReportTemplate> predefinedReports = JsonSerializer.Deserialize<List<ReportTemplate>>(mateData);

            return predefinedReports;
        }
    }

    public enum ReportType
    {
        None = 0,
        Telemetry,
        Metadata,
        Users,
        Datasharerequests
    }
}
