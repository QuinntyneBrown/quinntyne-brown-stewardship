namespace QuinntyneBrownStewardship.Performance;

public sealed record Measurement(string Operation, int Requests, int Concurrency, double P95Milliseconds, int BudgetMilliseconds, bool Passed);
