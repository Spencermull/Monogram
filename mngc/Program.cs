using mngc.Driver;
using mngc.Emit;
using mngc.Lexer;
using mngc.Parser;

if (args.Length == 0)
{
    Console.Error.WriteLine("usage: mngc <file.mngrm> [-o <output>] [--keep-c]");
    return 1;
}

var inputPath  = args[0];
var outputPath = GccDriver.DefaultOutputPath(inputPath);
var keepC      = false;

for (int i = 1; i < args.Length; i++)
{
    if (args[i] == "-o" && i + 1 < args.Length) outputPath = args[++i];
    else if (args[i] == "--keep-c") keepC = true;
}

var cPath = Path.ChangeExtension(inputPath, ".c");

try
{
    var source  = File.ReadAllText(inputPath);
    var tokens  = new Lexer().Tokenize(source);
    var ast     = new Parser(tokens).Parse();
    var emitter = new CEmitter();
    var cSource = emitter.Emit(ast);

    File.WriteAllText(cPath, cSource);

    var result = GccDriver.Compile(
        cPath,
        outputPath,
        needsMath: emitter.RequiredHeaders.Contains("<math.h>")
    );

    if (!result.Success)
    {
        Console.Error.WriteLine("compile error:");
        Console.Error.WriteLine(result.Errors);
        return 1;
    }

    Console.WriteLine($"compiled {inputPath} -> {outputPath}");

    if (!keepC) File.Delete(cPath);
}
catch (ParseException ex)
{
    Console.Error.WriteLine($"parse error: {ex.Message}");
    return 1;
}

return 0;
