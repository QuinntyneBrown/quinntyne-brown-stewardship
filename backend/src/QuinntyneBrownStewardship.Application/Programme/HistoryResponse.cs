namespace QuinntyneBrownStewardship.Application.Programme;

public sealed record HistoryResponse(List<BookingResponse> Sessions, int BookedCount, int Allowance);
