namespace QuinntyneBrownStewardship.Application.Common;

public sealed class ProgrammeException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
