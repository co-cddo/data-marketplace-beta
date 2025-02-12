namespace Cddo.Data.Marketplace.Api.Dto.Responses.EventLogs;

public class TelemetryQueryResultsTableRowSet
{
    public List<TelemetryQueryResultsTableRow> Rows { get; set; }

    public TelemetryQueryResultsTableRow GetRowWhere(Func<TelemetryQueryResultsTableRow, bool> condition)
    {
       foreach (var row in Rows)
        {
            if (condition(row))
            {
                return row;
            }
        }
        return null;
    }


}