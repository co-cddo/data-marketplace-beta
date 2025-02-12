namespace Cddo.Data.Marketplace.Logic.Exceptions;

public class CddoFlurlException : Exception
{
    public required int? StatusCode { get; init; }

    public required string? FlurlResponseText { get; init; }

    public required string ExceptionText { get; init; }

    public override string Message => "CddoFlurlException thrown: " + ToString();

    public override string ToString()
    {
        return $"{nameof(StatusCode)}: '{StatusCode}', " +
               $"{nameof(FlurlResponseText)}: '{FlurlResponseText}', " +
               $"{nameof(ExceptionText)}: '{ExceptionText}'";
    }
}