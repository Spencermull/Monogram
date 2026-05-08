using System.Text.RegularExpressions;

namespace mngc.Lexer;

public class Lexer
{
    private static readonly Regex _whitespace = new(@"^\s+", RegexOptions.Compiled);
    private static readonly Regex _comment    = new(@"^//[^\n]*", RegexOptions.Compiled);
    private static readonly Regex _invalid    = new(@"^\S+", RegexOptions.Compiled);

    private static readonly List<TokenDefinition> _definitions = new()
    {
        // Multi-char operators — longer patterns before any shorter prefix
        new(TokenType.Arrow,     @"^->"),
        new(TokenType.FatArrow,  @"^=>"),
        new(TokenType.And,       @"^&&"),
        new(TokenType.Or,        @"^\|\|"),
        new(TokenType.Eq,        @"^=="),
        new(TokenType.Neq,       @"^!="),
        new(TokenType.Lte,       @"^<="),
        new(TokenType.Gte,       @"^>="),
        new(TokenType.Shl,       @"^<<"),
        new(TokenType.Shr,       @"^>>"),

        // Import directive
        new(TokenType.Import,    @"^#import<"),

        // Keywords — longer/more-specific before shorter
        new(TokenType.Volatile,  @"^volatile\b"),
        new(TokenType.Const,     @"^const\b"),
        new(TokenType.Transform, @"^transform\b"),
        new(TokenType.Init,      @"^init\b"),
        new(TokenType.Func,      @"^func:"),
        new(TokenType.Type,      @"^type\b"),
        new(TokenType.Match,     @"^match\b"),
        new(TokenType.Else,      @"^else\b"),
        new(TokenType.For,       @"^for\b"),
        new(TokenType.If,        @"^if\b"),
        new(TokenType.In,        @"^in\b"),
        new(TokenType.As,        @"^as\b"),
        new(TokenType.Op,        @"^op:"),
        new(TokenType.Void,      @"^void\b"),
        new(TokenType.True,      @"^true\b"),
        new(TokenType.False,     @"^false\b"),

        // Primitive types — sized variants before unsized
        new(TokenType.Int8,      @"^int8\b"),
        new(TokenType.Int16,     @"^int16\b"),
        new(TokenType.Int32,     @"^int32\b"),
        new(TokenType.Int64,     @"^int64\b"),
        new(TokenType.Uint8,     @"^uint8\b"),
        new(TokenType.Uint16,    @"^uint16\b"),
        new(TokenType.Uint32,    @"^uint32\b"),
        new(TokenType.Uint64,    @"^uint64\b"),
        new(TokenType.Int,       @"^int\b"),
        new(TokenType.Float32,   @"^float32\b"),
        new(TokenType.Float64,   @"^float64\b"),
        new(TokenType.Float,     @"^float\b"),
        new(TokenType.Char,      @"^char\b"),
        new(TokenType.Byte,      @"^byte\b"),
        new(TokenType.Bool,      @"^bool\b"),

        // Identifier (after all keywords so keywords are not swallowed)
        new(TokenType.Identifier, @"^[a-zA-Z_][a-zA-Z0-9_]*"),

        // Literals — more specific before more general
        new(TokenType.HexLit,    @"^0x[0-9A-Fa-f]+"),
        new(TokenType.FloatLit,  @"^[0-9]+\.[0-9]+"),
        new(TokenType.IntLit,    @"^[0-9]+"),
        new(TokenType.CharLit,   @"^'[^']'"),
        new(TokenType.StringLit, @"^'[^']*'"),

        // Single-char symbols
        new(TokenType.LParen,    @"^\("),
        new(TokenType.RParen,    @"^\)"),
        new(TokenType.LBrace,    @"^\{"),
        new(TokenType.RBrace,    @"^\}"),
        new(TokenType.LBracket,  @"^\["),
        new(TokenType.RBracket,  @"^\]"),
        new(TokenType.LAngle,    @"^<"),
        new(TokenType.RAngle,    @"^>"),
        new(TokenType.Comma,     @"^,"),
        new(TokenType.Semicolon, @"^;"),
        new(TokenType.Dot,       @"^\."),
        new(TokenType.Colon,     @"^:"),
        new(TokenType.Assign,    @"^="),
        new(TokenType.Bang,      @"^!"),
        new(TokenType.At,        @"^@"),
        new(TokenType.Tilde,     @"^~"),
        new(TokenType.Question,  @"^\?"),
        new(TokenType.Plus,      @"^\+"),
        new(TokenType.Minus,     @"^-"),
        new(TokenType.Star,      @"^\*"),
        new(TokenType.Slash,     @"^/"),
        new(TokenType.Percent,   @"^%"),
    };

    public List<Token> Tokenize(string source)
    {
        var tokens = new List<Token>();
        string remaining = source;
        int line = 1, col = 1;

        while (remaining.Length > 0)
        {
            var ws = _whitespace.Match(remaining);
            if (ws.Success)
            {
                (line, col) = Advance(line, col, ws.Value);
                remaining = remaining[ws.Length..];
                continue;
            }

            var cmt = _comment.Match(remaining);
            if (cmt.Success)
            {
                (line, col) = Advance(line, col, cmt.Value);
                remaining = remaining[cmt.Length..];
                continue;
            }

            var match = FindMatch(remaining);
            if (match.IsMatch)
            {
                tokens.Add(new Token(match.TokenType, match.Value, line, col));
                (line, col) = Advance(line, col, match.Value);
                remaining = match.RemainingText;
            }
            else
            {
                var bad = _invalid.Match(remaining);
                var value = bad.Success ? bad.Value : remaining[..1];
                tokens.Add(new Token(TokenType.Invalid, value, line, col));
                (line, col) = Advance(line, col, value);
                remaining = remaining[value.Length..];
            }
        }

        tokens.Add(new Token(TokenType.Eof, "", line, col));
        return tokens;
    }

    private static TokenMatch FindMatch(string text)
    {
        foreach (var def in _definitions)
        {
            var match = def.Match(text);
            if (match.IsMatch) return match;
        }
        return new TokenMatch { IsMatch = false };
    }

    private static (int line, int col) Advance(int line, int col, string text)
    {
        foreach (char c in text)
        {
            if (c == '\n') { line++; col = 1; }
            else col++;
        }
        return (line, col);
    }
}
