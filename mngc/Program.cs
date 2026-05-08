using mngc.Lexer;
using mngc.Parser;

if (args.Length == 0)
{
    Console.Error.WriteLine("usage: mngc <file.mngrm>");
    return 1;
}

try
{
    var source = File.ReadAllText(args[0]);
    var tokens = new Lexer().Tokenize(source);
    var ast    = new Parser(tokens).Parse();
    Console.WriteLine($"Parsed OK — {ast.Declarations.Count} declaration(s)");
}
catch (ParseException ex)
{
    Console.Error.WriteLine($"parse error: {ex.Message}");
    return 1;
}

return 0;
