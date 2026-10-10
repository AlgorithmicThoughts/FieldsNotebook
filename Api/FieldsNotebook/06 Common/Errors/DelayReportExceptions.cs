namespace Common.Errors;

public sealed class DelayReportValidationException(string message) : Exception(message);
public sealed class DelayReportForbiddenException(string message) : Exception(message);
