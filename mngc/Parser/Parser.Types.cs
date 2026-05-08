using mngc.AST;
using mngc.Lexer;

namespace mngc.Parser;

public partial class Parser
{
    private static readonly HashSet<TokenType> _primitiveTypes = new()
    {
        TokenType.Int8,  TokenType.Int16,  TokenType.Int32,  TokenType.Int64,
        TokenType.Uint8, TokenType.Uint16, TokenType.Uint32, TokenType.Uint64,
        TokenType.Int,
        TokenType.Float32, TokenType.Float64, TokenType.Float,
        TokenType.Char, TokenType.Byte, TokenType.Bool, TokenType.Void,
    };

    private bool IsPrimitiveType() => _primitiveTypes.Contains(Current.Type);

    private bool IsTypeStart() =>
        IsPrimitiveType() ||
        Check(TokenType.Identifier) ||
        Check(TokenType.LParen);   // transform type ( T -> T )

    private TypeNode ParseTypeExpr()
    {
        TypeNode type;

        if (Check(TokenType.LParen))
        {
            // ( type_expr -> type_expr )
            Advance();
            var from = ParseTypeExpr();
            Expect(TokenType.Arrow);
            var to = ParseTypeExpr();
            Expect(TokenType.RParen);
            type = new TransformTypeNode(from, to);
        }
        else if (IsPrimitiveType())
        {
            type = new PrimitiveTypeNode(Advance().Value);
        }
        else
        {
            var name = ExpectIdentifier();
            var generics = new List<TypeNode>();

            if (Check(TokenType.LAngle))
            {
                Advance();
                generics.Add(ParseTypeArg());
                while (TryConsume(TokenType.Comma))
                    generics.Add(ParseTypeArg());
                Expect(TokenType.RAngle);
            }

            type = new NamedTypeNode(name, generics);
        }

        // Postfix [] makes an array type
        while (Check(TokenType.LBracket) && Peek().Type == TokenType.RBracket)
        {
            Advance(); Advance();
            type = new ArrayTypeNode(type);
        }

        return type;
    }

    // A generic type arg may itself be a transform: T -> U
    private TypeNode ParseTypeArg()
    {
        var t = ParseTypeExpr();
        if (TryConsume(TokenType.Arrow))
        {
            var to = ParseTypeExpr();
            return new TransformTypeNode(t, to);
        }
        return t;
    }

    private List<string> ParseGenericParams()
    {
        var names = new List<string>();
        if (!Check(TokenType.LAngle)) return names;
        Advance();
        names.Add(ExpectIdentifier());
        while (TryConsume(TokenType.Comma))
            names.Add(ExpectIdentifier());
        Expect(TokenType.RAngle);
        return names;
    }
}
