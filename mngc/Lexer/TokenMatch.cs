namespace mngc.Lexer;

public class TokenMatch
{
    public bool IsMatch { get; set; }
    public TokenType TokenType { get; set; }
    public string Value { get; set; } = string.Empty;
    public string RemainingText { get; set; } = string.Empty;
}
