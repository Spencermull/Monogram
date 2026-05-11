namespace mngc.TypeChecker;

public record SemanticError(string Message)
{
    public override string ToString() => $"type error: {Message}";
}

public class SemanticException(IReadOnlyList<SemanticError> errors)
    : Exception($"{errors.Count} semantic error(s)")
{
    public IReadOnlyList<SemanticError> Errors { get; } = errors;
}
