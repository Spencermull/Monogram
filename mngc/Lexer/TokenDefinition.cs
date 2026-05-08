using System.Text.RegularExpressions;

namespace mngc.Lexer;

public class TokenDefinition
{
    private readonly Regex _regex;
    private readonly TokenType _tokenType;

    public TokenDefinition(TokenType tokenType, string pattern)
    {
        _regex = new Regex(pattern, RegexOptions.Compiled);
        _tokenType = tokenType;
    }

    public TokenMatch Match(string text)
    {
        var match = _regex.Match(text);
        if (!match.Success)
            return new TokenMatch { IsMatch = false };

        return new TokenMatch
        {
            IsMatch = true,
            TokenType = _tokenType,
            Value = match.Value,
            RemainingText = text[match.Length..]
        };
    }
}
