using mngc.AST;
using mngc.Lexer;

namespace mngc.Parser;

public partial class Parser
{
    private BlockStmt ParseBlock()
    {
        Expect(TokenType.LBrace);
        var stmts = new List<StmtNode>();
        while (!Check(TokenType.RBrace) && !Check(TokenType.Eof))
            stmts.Add(ParseStmt());
        Expect(TokenType.RBrace);
        return new BlockStmt(stmts);
    }

    private StmtNode ParseStmt()
    {
        // var decl: starts with optional mutability or a type
        if (CheckAny(TokenType.Const, TokenType.Volatile, TokenType.EConst, TokenType.XConst) || IsTypeStart())
        {
            // disambiguate: could be a type-led var decl or just an expression starting with an identifier
            // var decl has the form: [mutability] type_expr IDENTIFIER
            // peek ahead to see if after the type there is an identifier
            if (IsVarDecl())
                return ParseVarDecl();
        }

        return Current.Type switch
        {
            TokenType.If        => ParseIf(),
            TokenType.Match     => ParseMatch(),
            TokenType.For       => ParseFor(),
            TokenType.LBrace    => ParseBlock(),
            TokenType.FatArrow  => ParseReturn(),
            TokenType.Break     => ParseBreak(),
            TokenType.Continue  => ParseContinue(),
            TokenType.Rebind    => ParseRebind(),
            TokenType.Deref     => ParseDerefBind(),
            TokenType.Container => ParseContainer(),
            TokenType.Phased    => ParsePhased(),
            TokenType.Dephased  => ParseDephased(),
            _                   => ParseExprOrAssign(),
        };
    }

    // Heuristic: if we see [mutability?] typeExpr IDENTIFIER, it's a var decl
    private bool IsVarDecl()
    {
        int saved = _pos;
        try
        {
            if (CheckAny(TokenType.EConst, TokenType.XConst)) Advance();
            else
            {
                if (CheckAny(TokenType.Const, TokenType.Volatile)) Advance();
                if (Check(TokenType.Const) || Check(TokenType.Volatile)) Advance(); // const volatile
            }
            if (!IsTypeStart()) return false;
            ParseTypeExpr();
            return Check(TokenType.Identifier);
        }
        catch
        {
            return false;
        }
        finally
        {
            _pos = saved;
        }
    }

    private VarDeclNode ParseVarDecl()
    {
        var mut = Mutability.None;
        if (TryConsume(TokenType.EConst))      mut = Mutability.EConst;
        else if (TryConsume(TokenType.XConst)) mut = Mutability.XConst;
        else if (Check(TokenType.Const) && Peek().Type == TokenType.Volatile)
        {
            Advance(); Advance();
            mut = Mutability.ConstVolatile;
        }
        else if (TryConsume(TokenType.Const))    mut = Mutability.Const;
        else if (TryConsume(TokenType.Volatile)) mut = Mutability.Volatile;

        var type = ParseTypeExpr();
        var name = ExpectIdentifier();
        ExprNode? init = null;
        if (TryConsume(TokenType.Assign))
            init = ParseExpr();
        Expect(TokenType.Semicolon);
        return new VarDeclNode(mut, type, name, init);
    }

    private ReturnStmt ParseReturn()
    {
        Expect(TokenType.FatArrow);
        var value = ParseExpr();
        Expect(TokenType.Semicolon);
        return new ReturnStmt(value);
    }

    private BreakStmt ParseBreak()
    {
        Expect(TokenType.Break);
        Expect(TokenType.Semicolon);
        return new BreakStmt();
    }

    private ContinueStmt ParseContinue()
    {
        Expect(TokenType.Continue);
        Expect(TokenType.Semicolon);
        return new ContinueStmt();
    }

    private StmtNode ParseExprOrAssign()
    {
        var expr = ParseExpr();
        if (TryConsume(TokenType.Assign))
        {
            var value = ParseExpr();
            Expect(TokenType.Semicolon);
            return new AssignStmt(expr, value);
        }
        Expect(TokenType.Semicolon);
        return new ExprStmt(expr);
    }

    private IfStmt ParseIf()
    {
        Expect(TokenType.If);
        Expect(TokenType.LParen);
        var cond = ParseExpr();
        Expect(TokenType.RParen);
        var then = ParseBlock();

        var elseIfs = new List<ElseIf>();
        BlockStmt? elseBranch = null;

        while (Check(TokenType.Else))
        {
            Advance();
            if (Check(TokenType.If))
            {
                Advance();
                Expect(TokenType.LParen);
                var eic = ParseExpr();
                Expect(TokenType.RParen);
                elseIfs.Add(new ElseIf(eic, ParseBlock()));
            }
            else
            {
                elseBranch = ParseBlock();
                break;
            }
        }

        return new IfStmt(cond, then, elseIfs, elseBranch);
    }

    private MatchStmt ParseMatch()
    {
        Expect(TokenType.Match);
        Expect(TokenType.Colon);
        var subject = ExpectIdentifier();
        Expect(TokenType.LBrace);

        var arms = new List<MatchArm>();
        while (!Check(TokenType.RBrace) && !Check(TokenType.Eof))
        {
            TypeNode? pattern = null;
            if (!Check(TokenType.Identifier) || Current.Value != "_")
                pattern = ParseTypeExpr();
            else
                Advance(); // consume _

            Expect(TokenType.FatArrow);
            arms.Add(new MatchArm(pattern, ParseBlock()));
        }

        Expect(TokenType.RBrace);
        return new MatchStmt(subject, arms);
    }

    private StmtNode ParseFor()
    {
        Expect(TokenType.For);

        // for -> ...  (mapped forms)
        if (Check(TokenType.Arrow))
        {
            Advance();

            // for -> type T: x { }
            if (Check(TokenType.Type))
            {
                Advance();
                var typeName = ExpectIdentifier();
                Expect(TokenType.Colon);
                var varName = ExpectIdentifier();
                return new ForTypedStmt(typeName, varName, ParseBlock());
            }

            // for -> :val in x { }
            Expect(TokenType.Colon);
            var val = ExpectIdentifier();
            Expect(TokenType.In);
            var coll = ParseExpr();
            return new ForMapStmt(val, coll, ParseBlock());
        }

        // for :name ...  (sequential forms)
        Expect(TokenType.Colon);
        var iterName = ExpectIdentifier();

        // for :i -> expr cond; { }
        if (Check(TokenType.Arrow))
        {
            Advance();
            var expr = ParseExpr();
            var (op, condVal) = ParseCondition();
            Expect(TokenType.Semicolon);
            return new ForMappedIterStmt(iterName, expr, op, condVal, ParseBlock());
        }

        // for :val in expr [cond;] { }
        Expect(TokenType.In);
        var collection = ParseExpr();

        if (IsConditionStart())
        {
            var (op, condVal) = ParseCondition();
            Expect(TokenType.Semicolon);
            return new ForIterStmt(iterName, collection, op, condVal, ParseBlock());
        }

        return new ForEachStmt(iterName, collection, ParseBlock());
    }

    private bool IsConditionStart() =>
        CheckAny(TokenType.Gte, TokenType.Lte, TokenType.RAngle, TokenType.LAngle, TokenType.Eq, TokenType.Neq);

    private (string op, ExprNode val) ParseCondition()
    {
        if (!IsConditionStart())
            throw new ParseException("expected condition operator", Current.Line, Current.Column);
        var op = Advance().Value;
        return (op, ParseExpr());
    }

    private RebindStmt ParseRebind()
    {
        Expect(TokenType.Rebind);
        var name = ExpectIdentifier();
        Expect(TokenType.Assign);
        var value = ParseExpr();
        Expect(TokenType.Semicolon);
        return new RebindStmt(name, value);
    }

    private DerefBindStmt ParseDerefBind()
    {
        Expect(TokenType.Deref);
        // consume the literal word "bind" as an identifier
        if (Current.Value != "bind")
            throw new ParseException("expected 'bind' after 'deref'", Current.Line, Current.Column);
        Advance();
        Expect(TokenType.Colon);
        var name = ExpectIdentifier();
        Expect(TokenType.Assign);
        var source = ParseExpr();
        Expect(TokenType.Semicolon);
        return new DerefBindStmt(name, source);
    }

    private ContainerStmt ParseContainer()
    {
        Expect(TokenType.Container);
        Expect(TokenType.Colon);
        var name = ExpectIdentifier();
        return new ContainerStmt(name, ParseBlock());
    }

    private PhasedStmt ParsePhased()
    {
        Expect(TokenType.Phased);
        Expect(TokenType.Colon);
        var name = ExpectIdentifier();
        return new PhasedStmt(name, ParseBlock());
    }

    private DePhasedStmt ParseDephased()
    {
        Expect(TokenType.Dephased);
        return new DePhasedStmt(ParseBlock());
    }
}
