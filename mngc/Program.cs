using System.Diagnostics;
using mngc.Driver;
using mngc.Emit;
using mngc.Lexer;
using mngc.Parser;
using mngc.TypeChecker;

if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
{
    PrintHelp();
    return 0;
}

return args[0] switch
{
    "build" => RunBuild(args[1..], run: false),
    "run"   => RunBuild(args[1..], run: true),
    _       => UnknownCommand(args[0])
};

static int RunBuild(string[] args, bool run)
{
    if (args.Length == 0)
    {
        Console.Error.WriteLine($"usage: mono {(run ? "run" : "build")} <file.mngrm> [-o <output>] [--keep-c]");
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

        var checker = new TypeChecker();
        checker.Check(ast);
        if (checker.HasErrors)
        {
            foreach (var err in checker.Errors)
                Console.Error.WriteLine(err);
            return 1;
        }

        var lifecycle = new LifecycleChecker();
        lifecycle.Check(ast);
        if (lifecycle.HasErrors)
        {
            foreach (var err in lifecycle.Errors)
                Console.Error.WriteLine(err);
            return 1;
        }

        var emitter = new CEmitter();
        var cSource = emitter.Emit(ast);

        File.WriteAllText(cPath, cSource);

        var result = GccDriver.Compile(
            cPath,
            outputPath,
            needsMath: emitter.RequiredHeaders.Contains("<math.h>")
        );

        if (!keepC) File.Delete(cPath);

        if (!result.Success)
        {
            Console.Error.WriteLine("compile error:");
            Console.Error.WriteLine(result.Errors);
            return 1;
        }

        if (!run)
        {
            Console.WriteLine($"built {inputPath} -> {outputPath}");
            return 0;
        }

        var proc = Process.Start(new ProcessStartInfo
        {
            FileName  = outputPath,
            UseShellExecute = false,
        });
        proc!.WaitForExit();
        return proc.ExitCode;
    }
    catch (ParseException ex)
    {
        Console.Error.WriteLine($"parse error: {ex.Message}");
        return 1;
    }
    catch (SemanticException ex)
    {
        foreach (var err in ex.Errors)
            Console.Error.WriteLine(err);
        return 1;
    }
    catch (FileNotFoundException)
    {
        Console.Error.WriteLine($"error: file not found: {inputPath}");
        return 1;
    }
}

static void PrintHelp()
{
    Console.WriteLine("mono — Monogram compiler v0.2.0");
    Console.WriteLine();
    Console.WriteLine("commands:");
    Console.WriteLine("  mono run   <file.mngrm>              compile and run");
    Console.WriteLine("  mono build <file.mngrm> [-o output]  compile only");
    Console.WriteLine();
    Console.WriteLine("flags:");
    Console.WriteLine("  -o <path>   output binary path");
    Console.WriteLine("  --keep-c    keep generated C file");
}

static int UnknownCommand(string cmd)
{
    Console.Error.WriteLine($"error: unknown command '{cmd}'. Run 'mono help' for usage.");
    return 1;
}
