using mngc.Emit;
using mngc.Lexer;
using mngc.Parser;

if (args.Length == 0)
{
    Console.Error.WriteLine("usage: mngc <file.mngrm> [output.c]");
    return 1;
}

try
{
    var source   = File.ReadAllText(args[0]);
    var tokens   = new Lexer().Tokenize(source);
    var ast      = new Parser(tokens).Parse();
    var cSource  = new CEmitter().Emit(ast);

    var outPath  = args.Length > 1 ? args[1] : Path.ChangeExtension(args[0], ".c");
    File.WriteAllText(outPath, cSource);
    Console.WriteLine($"emitted {outPath}");
}
catch (ParseException ex)
{
    Console.Error.WriteLine($"parse error: {ex.Message}");
    return 1;
}

return 0;
