using mngc.Lexer;

if (args.Length == 0)
{
    Console.Error.WriteLine("usage: mngc <file.mngrm>");
    return 1;
}

var source = File.ReadAllText(args[0]);
var lexer = new Lexer();
var tokens = lexer.Tokenize(source);

foreach (var token in tokens)
    Console.WriteLine(token);

return 0;
