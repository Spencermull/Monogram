using mngc.AST;
using mngc.Lexer;

namespace mngc.Parser;

public partial class Parser
{
    private ExprNode ParseExpr() => ParseTernary();

    private ExprNode ParseTernary()
    {
        var expr = ParseLogic();
        if (!TryConsume(TokenType.Question)) return expr;
        var then = ParseExpr();
        Expect(TokenType.Colon);
        var els = ParseExpr();
        return new TernaryExpr(expr, then, els);
    }

    private ExprNode ParseLogic()
    {
        var left = ParseCompare();
        while (CheckAny(TokenType.And, TokenType.Or))
        {
            var op = Advance().Value;
            left = new BinaryExpr(left, op, ParseCompare());
        }
        return left;
    }

    private ExprNode ParseCompare()
    {
        var left = ParseShift();
        while (CheckAny(TokenType.Eq, TokenType.Neq, TokenType.Gte, TokenType.Lte, TokenType.RAngle, TokenType.LAngle))
        {
            var op = Advance().Value;
            left = new BinaryExpr(left, op, ParseShift());
        }
        return left;
    }

    private ExprNode ParseShift()
    {
        var left = ParseAdd();
        while (CheckAny(TokenType.Shl, TokenType.Shr))
        {
            var op = Advance().Value;
            left = new BinaryExpr(left, op, ParseAdd());
        }
        return left;
    }

    private ExprNode ParseAdd()
    {
        var left = ParseMul();
        while (CheckAny(TokenType.Plus, TokenType.Minus))
        {
            var op = Advance().Value;
            left = new BinaryExpr(left, op, ParseMul());
        }
        return left;
    }

    private ExprNode ParseMul()
    {
        var left = ParseUnary();
        while (CheckAny(TokenType.Star, TokenType.Slash, TokenType.Percent))
        {
            var op = Advance().Value;
            left = new BinaryExpr(left, op, ParseUnary());
        }
        return left;
    }

    private ExprNode ParseUnary()
    {
        if (CheckAny(TokenType.At, TokenType.Tilde, TokenType.Bang, TokenType.Minus))
        {
            var op = Advance().Value;
            return new UnaryExpr(op, ParseUnary());
        }
        return ParsePipeline();
    }

    private ExprNode ParsePipeline()
    {
        var left = ParsePostfix();
        while (Check(TokenType.Arrow))
        {
            Advance();
            left = new PipelineExpr(left, ParsePostfix());
        }
        return left;
    }

    private ExprNode ParsePostfix()
    {
        var expr = ParsePrimary();

        while (true)
        {
            if (Check(TokenType.Dot))
            {
                Advance();
                if (Check(TokenType.Transform))
                {
                    Advance();
                    expr = new TransformChainExpr(expr);
                }
                else
                {
                    expr = new MemberAccessExpr(expr, ExpectIdentifier());
                }
            }
            else if (Check(TokenType.LParen))
            {
                Advance();
                var args = new List<Arg>();
                if (!Check(TokenType.RParen))
                {
                    args.Add(ParseArg());
                    while (TryConsume(TokenType.Comma))
                        args.Add(ParseArg());
                }
                Expect(TokenType.RParen);
                expr = new CallExpr(expr, args);
            }
            else if (Check(TokenType.LBracket))
            {
                Advance();
                var index = ParseExpr();
                Expect(TokenType.RBracket);
                expr = new IndexExpr(expr, index);
            }
            else if (Check(TokenType.As))
            {
                Advance();
                var targetType = ParseTypeExpr();
                expr = new CastExpr(targetType, expr);
            }
            else break;
        }

        return expr;
    }

    private ExprNode ParsePrimary()
    {
        // :name binding sigil
        if (Check(TokenType.Colon))
        {
            Advance();
            return new BindingExpr(ExpectIdentifier());
        }

        if (Check(TokenType.LParen))
        {
            Advance();
            var inner = ParseExpr();
            Expect(TokenType.RParen);
            return new GroupExpr(inner);
        }

        if (Check(TokenType.Identifier))
            return new IdentifierExpr(Advance().Value);

        return ParseLiteral();
    }

    private ExprNode ParseLiteral()
    {
        return Current.Type switch
        {
            TokenType.IntLit    => new LiteralExpr(Advance().Value, LiteralKind.Int),
            TokenType.HexLit    => new LiteralExpr(Advance().Value, LiteralKind.Hex),
            TokenType.FloatLit  => new LiteralExpr(Advance().Value, LiteralKind.Float),
            TokenType.CharLit   => new LiteralExpr(Advance().Value, LiteralKind.Char),
            TokenType.StringLit => new LiteralExpr(Advance().Value, LiteralKind.String),
            TokenType.True      => new LiteralExpr(Advance().Value, LiteralKind.Bool),
            TokenType.False     => new LiteralExpr(Advance().Value, LiteralKind.Bool),
            _ => throw new ParseException(
                    $"unexpected token '{Current.Value}' in expression",
                    Current.Line, Current.Column)
        };
    }

    private Arg ParseArg()
    {
        if (Check(TokenType.Colon))
        {
            Advance();
            return new Arg(true, ParseExpr());
        }
        return new Arg(false, ParseExpr());
    }
}
