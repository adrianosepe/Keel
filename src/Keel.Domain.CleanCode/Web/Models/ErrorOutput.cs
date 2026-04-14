namespace Keel.Domain.CleanCode.Web.Models;

public record ErrorOutput(ErrorDetailOutput Error);
public record ErrorDetailOutput(string? Flag, string Message);