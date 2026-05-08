namespace mngc.Emit;

public record StdlibEntry(string Header, Func<List<string>, string> Emit);

public static class StdlibMap
{
    public static readonly Dictionary<string, StdlibEntry> Entries = new()
    {
        // sys — console / process
        ["sys.stdout"]      = new("<stdio.h>",   args => $"printf({Join(args)})"),
        ["sys.stderr"]      = new("<stdio.h>",   args => $"fprintf(stderr, {Join(args)})"),
        ["sys.exit"]        = new("<stdlib.h>",  args => $"exit({args.ElementAtOrDefault(0) ?? "0"})"),

        // std.mem — heap
        ["std.mem.alloc"]   = new("<stdlib.h>",  args => $"malloc({args[0]})"),
        ["std.mem.calloc"]  = new("<stdlib.h>",  args => $"calloc({args[0]}, {args[1]})"),
        ["std.mem.realloc"] = new("<stdlib.h>",  args => $"realloc({args[0]}, {args[1]})"),
        ["std.mem.free"]    = new("<stdlib.h>",  args => $"free({args[0]})"),

        // std.str — string ops
        ["std.str.len"]     = new("<string.h>",  args => $"strlen({args[0]})"),
        ["std.str.copy"]    = new("<string.h>",  args => $"strcpy({args[0]}, {args[1]})"),
        ["std.str.cat"]     = new("<string.h>",  args => $"strcat({args[0]}, {args[1]})"),
        ["std.str.cmp"]     = new("<string.h>",  args => $"strcmp({args[0]}, {args[1]})"),
        ["std.str.chr"]     = new("<string.h>",  args => $"strchr({args[0]}, {args[1]})"),
        ["std.str.fmt"]     = new("<stdio.h>",   args => $"sprintf({Join(args)})"),

        // std.math
        ["std.math.sqrt"]   = new("<math.h>",    args => $"sqrt({args[0]})"),
        ["std.math.pow"]    = new("<math.h>",     args => $"pow({args[0]}, {args[1]})"),
        ["std.math.abs"]    = new("<math.h>",    args => $"fabs({args[0]})"),
        ["std.math.floor"]  = new("<math.h>",    args => $"floor({args[0]})"),
        ["std.math.ceil"]   = new("<math.h>",    args => $"ceil({args[0]})"),
        ["std.math.sin"]    = new("<math.h>",    args => $"sin({args[0]})"),
        ["std.math.cos"]    = new("<math.h>",    args => $"cos({args[0]})"),
        ["std.math.tan"]    = new("<math.h>",    args => $"tan({args[0]})"),
        ["std.math.log"]    = new("<math.h>",    args => $"log({args[0]})"),

        // std.io — file I/O
        ["std.io.open"]     = new("<stdio.h>",   args => $"fopen({args[0]}, {args[1]})"),
        ["std.io.close"]    = new("<stdio.h>",   args => $"fclose({args[0]})"),
        ["std.io.read"]     = new("<stdio.h>",   args => $"fgets({args[0]}, {args[1]}, {args[2]})"),
        ["std.io.write"]    = new("<stdio.h>",   args => $"fputs({args[0]}, {args[1]})"),
        ["std.io.flush"]    = new("<stdio.h>",   args => $"fflush({args[0]})"),
        ["std.io.scanf"]    = new("<stdio.h>",   args => $"scanf({Join(args)})"),
    };

    private static string Join(List<string> args) => string.Join(", ", args);
}
