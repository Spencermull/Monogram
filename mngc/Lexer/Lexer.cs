namespace mngc.Lexer;

public class Lexer
{
    private readonly string _source;
    private int _pos;
    private int _line;
    private int _col;

    public Lexer(string source)
    {
        _source = source;
        _pos = 0;
        _line = 1;
        _col = 1;
    }

    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();

        // TODO: walk _source, emit tokens into the list
        // Tip: peek with _source[_pos], advance by incrementing _pos
        // Track _line/_col for error messages

        tokens.Add(new Token(TokenType.Eof, "", _line, _col));
        return tokens;
    }
}
