namespace Cddo.Data.Marketplace.Api.Dto.Responses.EventLogs;

public class TelemetryQueryResultsData
{
    public int TotalNumberOfResults => RowData.Rows.Count;

    public TelemetryQueryResultsTableColumnSet ColumnData { get; set; }

    public TelemetryQueryResultsTableRowSet RowData { get; set; }
}