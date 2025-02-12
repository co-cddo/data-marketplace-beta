using System.Linq;

namespace Cddo.Data.Marketplace.UI.Model
{
    public static class ReportsTelemetryPagination
    {

        public static string GetPaginationTemplate(string kqlQuery, int pageNumber, int pageSize, List<string> selectedColumnNames)
        {
            var joinSelected = string.Join(",", selectedColumnNames);

            return $@"let userCounts = 
    {kqlQuery};
let totalCount = 
    userCounts 
    | summarize TotalCount = count();
let paginatedCounts = 
    userCounts
    | serialize
    | extend RowNum = row_number()
    | where RowNum > (({pageNumber} - 1) * {pageSize}) and RowNum <= ({pageNumber} * {pageSize})
    | project {joinSelected}, Count;
paginatedCounts
| union (
    totalCount 
    | project {selectedColumnNames[0]} = 'Total', Count = TotalCount
)";
        }
    }
}
