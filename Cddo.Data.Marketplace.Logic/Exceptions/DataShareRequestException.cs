namespace Cddo.Data.Marketplace.Logic.Exceptions
{
    public class DataShareRequestException : Exception
    {
        public required int? DsrStatusCode { get; init; }

        public required string? DsrResponseText { get; init; }

        public required string DsrExceptionText { get; init; }

        public override string Message => "DataShareRequestException thrown: " + ToString();

        public override string ToString()
        {
            return $"{nameof(DsrStatusCode)}: '{DsrStatusCode}', " +
                   $"{nameof(DsrResponseText)}: '{DsrResponseText}', " +
                   $"{nameof(DsrExceptionText)}: '{DsrExceptionText}'";
        }
    }
}
