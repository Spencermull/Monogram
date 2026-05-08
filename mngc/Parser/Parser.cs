using mngc.AST;
using mngc.Lexer;

namespace mngc.Parser;

public partial class Parser
{
    private readonly List<Token> _tokens;
    private int _pos;

    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
        _pos = 0;
    }

    // --- Cursor helpers ---

    private Token Current => _tokens[_pos];
    private Token Peek(int offset = 1) => _tokens[Math.Min(_pos + offset, _tokens.Count - 1)];

    private bool Check(TokenType type) => Current.Type == type;
    private bool CheckAny(params TokenType[] types) => Array.Exists(types, t => t == Current.Type);

    private Token Advance()
    {
        var t = Current;
        if (_pos < _tokens.Count - 1) _pos++;
        return t;
    }

    private bool TryConsume(TokenType type)
    {
        if (!Check(type)) return false;
        Advance();
        return true;
    }

    private Token Expect(TokenType type)
    {
        if (!Check(type))
            throw new ParseException(
                $"expected {type} but got {Current.Type} '{Current.Value}'",
                Current.Line, Current.Column);
        return Advance();
    }

    private string ExpectIdentifier() => Expect(TokenType.Identifier).Value;

    // --- Entry point ---

    public ProgramNode Parse() => ParseProgram();
}
