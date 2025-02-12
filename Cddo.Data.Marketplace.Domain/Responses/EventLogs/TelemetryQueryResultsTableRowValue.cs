using System.Diagnostics;

namespace Cddo.Data.Marketplace.Api.Dto.Responses.EventLogs;

[DebuggerDisplay("{ValueName}: '{Value}' (Type={ValueType})")]
public class TelemetryQueryResultsTableRowValue
{
    public string ValueName { get; set; }

    public TelemetryQueryResultsTableValueType ValueType { get; set; }

    public object Value { get; set; }
}