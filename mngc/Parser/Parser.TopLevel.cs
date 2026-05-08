using mngc.AST;
using mngc.Lexer;

namespace mngc.Parser;

public partial class Parser
{
    private ProgramNode ParseProgram()
    {
        var imports = new List<ImportNode>();
        while (Check(TokenType.Import))
            imports.Add(ParseImport());

        EntryPointNode? entry = null;
        var decls = new List<StmtNode>();

        while (!Check(TokenType.Eof))
        {
            if (Check(TokenType.Init))
                entry = ParseEntryPoint();
            else
                decls.Add(ParseDeclaration());
        }

        if (entry == null)
            throw new ParseException("no init entry point found", Current.Line, Current.Column);

        return new ProgramNode(imports, entry, decls);
    }

    private ImportNode ParseImport()
    {
        Expect(TokenType.Import);
        var path = new System.Text.StringBuilder();
        var wildcard = false;

        while (!Check(TokenType.RAngle) && !Check(TokenType.Eof))
        {
            if (Check(TokenType.Star)) { wildcard = true; Advance(); break; }
            if (Check(TokenType.Dot))  { path.Append('.'); Advance(); continue; }
            path.Append(Advance().Value);
        }

        Expect(TokenType.RAngle);
        return new ImportNode(path.ToString(), wildcard);
    }

    private EntryPointNode ParseEntryPoint()
    {
        Expect(TokenType.Init);
        Expect(TokenType.Void);
        var name = ExpectIdentifier();
        Expect(TokenType.LParen);
        Expect(TokenType.RParen);
        return new EntryPointNode(name, ParseBlock());
    }

    private StmtNode ParseDeclaration()
    {
        return Current.Type switch
        {
            TokenType.Func  => ParseFuncDecl(),
            TokenType.Op    => ParseOpDecl(),
            TokenType.Type  => ParseTypeDecl(),
            _               => ParseStmt(),
        };
    }
}
