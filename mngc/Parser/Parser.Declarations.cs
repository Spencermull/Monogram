using mngc.AST;
using mngc.Lexer;

namespace mngc.Parser;

public partial class Parser
{
    private FuncDeclNode ParseFuncDecl()
    {
        Expect(TokenType.Func);
        Expect(TokenType.Colon);
        var name = ExpectIdentifier();
        var generics = ParseGenericParams();

        Expect(TokenType.LParen);
        var parms = new List<ParamNode>();
        if (!Check(TokenType.RParen))
        {
            parms.Add(ParseParam());
            while (TryConsume(TokenType.Comma))
                parms.Add(ParseParam());
        }
        Expect(TokenType.RParen);

        ReturnSig? ret = null;
        if (Check(TokenType.FatArrow))
        {
            Advance();
            ret = new ReturnSig(ParseTypeExpr(), false);
        }
        else if (Check(TokenType.Arrow))
        {
            Advance();
            ret = new ReturnSig(ParseTypeExpr(), true);
        }

        return new FuncDeclNode(name, generics, parms, ret, ParseBlock());
    }

    private ParamNode ParseParam()
    {
        Expect(TokenType.Colon);
        var name = ExpectIdentifier();
        var type = ParseTypeExpr();
        return new ParamNode(name, type);
    }

    private OpDeclNode ParseOpDecl()
    {
        Expect(TokenType.Op);
        Expect(TokenType.Colon);
        var name = ExpectIdentifier();

        Expect(TokenType.LParen);
        var parms = new List<string>();
        parms.Add(ExpectIdentifier());
        while (TryConsume(TokenType.Comma))
            parms.Add(ExpectIdentifier());
        Expect(TokenType.RParen);

        Expect(TokenType.FatArrow);
        var returnType = ExpectIdentifier();

        return new OpDeclNode(name, parms, returnType, ParseBlock());
    }

    private TypeDeclNode ParseTypeDecl()
    {
        Expect(TokenType.Type);
        var name = ExpectIdentifier();
        var generics = ParseGenericParams();

        TypeBodyNode? body = null;

        if (Check(TokenType.LBrace))
        {
            Advance();
            var fields = new List<FieldNode>();
            if (!Check(TokenType.RBrace))
            {
                fields.Add(ParseField());
                while (TryConsume(TokenType.Comma))
                    fields.Add(ParseField());
            }
            Expect(TokenType.RBrace);
            body = new StructTypeBody(fields);
        }
        else if (Check(TokenType.LParen))
        {
            Advance();
            var from = ParseTypeExpr();
            Expect(TokenType.Arrow);
            var to = ParseTypeExpr();
            Expect(TokenType.RParen);
            body = new TransformTypeBody(from, to);
        }
        else if (Check(TokenType.LBracket))
        {
            Advance();
            Expect(TokenType.RBracket);
            body = new CollectionTypeBody();
        }

        return new TypeDeclNode(name, generics, body);
    }

    private FieldNode ParseField()
    {
        Expect(TokenType.Colon);
        var name = ExpectIdentifier();
        var type = ParseTypeExpr();
        return new FieldNode(name, type);
    }
}
