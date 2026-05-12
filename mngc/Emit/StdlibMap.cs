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

        // node — linked / graph node
        ["node.new"]        = new("node", args => $"node_new({args[0]})"),
        ["node.link"]       = new("node", args => $"node_link({args[0]}, {args[1]})"),
        ["node.get"]        = new("node", args => $"node_get({args[0]})"),
        ["node.set"]        = new("node", args => $"node_set({args[0]}, {args[1]})"),
        ["node.next"]       = new("node", args => $"node_next({args[0]})"),
        ["node.prev"]       = new("node", args => $"node_prev({args[0]})"),
        ["node.free"]       = new("node", args => $"node_free({args[0]})"),
        ["node.transform"]  = new("node", args => $"node_transform({args[0]}, {args[1]})"),

        // lattice — 2D data grid
        ["lattice.new"]           = new("lattice", args => $"lattice_new({args[0]}, {args[1]})"),
        ["lattice.new_transform"] = new("lattice", args => $"lattice_new_transform({args[0]}, {args[1]}, {args[2]})"),
        ["lattice.get"]           = new("lattice", args => $"lattice_get({args[0]}, {args[1]}, {args[2]})"),
        ["lattice.set"]           = new("lattice", args => $"lattice_set({args[0]}, {args[1]}, {args[2]}, {args[3]})"),
        ["lattice.apply"]         = new("lattice", args => $"lattice_apply({args[0]}, {args[1]}, {args[2]})"),
        ["lattice.rows"]          = new("lattice", args => $"lattice_rows({args[0]})"),
        ["lattice.cols"]          = new("lattice", args => $"lattice_cols({args[0]})"),
        ["lattice.free"]          = new("lattice", args => $"lattice_free({args[0]})"),

        // slice — length-tracked array
        ["slice.new"]   = new("slice", args => $"slice_new({args[0]})"),
        ["slice.get"]   = new("slice", args => $"slice_get({args[0]}, {args[1]})"),
        ["slice.set"]   = new("slice", args => $"slice_set({args[0]}, {args[1]}, {args[2]})"),
        ["slice.len"]   = new("slice", args => $"slice_len({args[0]})"),
        ["slice.free"]  = new("slice", args => $"slice_free({args[0]})"),

        // std.time
        ["std.time.now"]    = new("std.time", args => "time(NULL)"),
        ["std.time.clock"]  = new("std.time", args => "clock()"),
        ["std.time.diff"]   = new("std.time", args => $"difftime({args[0]}, {args[1]})"),
        ["std.time.sleep"]  = new("std.time", args => $"mg_sleep_ms({args[0]})"),

        // std.env
        ["std.env.get"]     = new("<stdlib.h>", args => $"getenv({args[0]})"),

        // std.sync
        ["std.sync.mutex"]       = new("std.sync", args => "mg_mutex_new()"),
        ["std.sync.lock"]        = new("std.sync", args => $"mg_mutex_lock({args[0]})"),
        ["std.sync.unlock"]      = new("std.sync", args => $"mg_mutex_unlock({args[0]})"),
        ["std.sync.mutex_free"]  = new("std.sync", args => $"mg_mutex_free({args[0]})"),

        // std.fs
        ["std.fs.rename"]   = new("std.fs", args => $"rename({args[0]}, {args[1]})"),
        ["std.fs.remove"]   = new("std.fs", args => $"remove({args[0]})"),
        ["std.fs.exists"]   = new("std.fs", args => $"mg_file_exists({args[0]})"),

        // std.proc
        ["std.proc.spawn"]  = new("std.proc", args => $"system({args[0]})"),
        ["std.proc.pid"]    = new("std.proc", args => "mg_proc_pid()"),

        // std.delta
        ["std.delta.d2"]    = new("std.delta", args => $"mg_delta_2d({args[0]}, {args[1]}, {args[2]}, {args[3]})"),
        ["std.delta.d3"]    = new("std.delta", args => $"mg_delta_3d({args[0]}, {args[1]}, {args[2]}, {args[3]}, {args[4]}, {args[5]})"),
        ["std.delta.mag"]   = new("std.delta", args => $"mg_delta_mag({args[0]})"),
        ["std.delta.dx"]    = new("std.delta", args => $"mg_delta_dx({args[0]})"),
        ["std.delta.dy"]    = new("std.delta", args => $"mg_delta_dy({args[0]})"),
        ["std.delta.dz"]    = new("std.delta", args => $"mg_delta_dz({args[0]})"),

        // process.thread — thread spawn (used by container/phased/dephased)
        ["process.thread"]  = new("mono.phase", args => $"mg_thread_spawn({args[0]})"),
        ["process.join"]    = new("mono.phase", args => $"mg_thread_join({args[0]})"),

        // process — byte buffer pool
        ["process.new"]     = new("process", args => $"process_new({args[0]})"),
        ["process.get"]     = new("process", args => $"process_get({args[0]}, {args[1]})"),
        ["process.set"]     = new("process", args => $"process_set({args[0]}, {args[1]}, {args[2]})"),
        ["process.write"]   = new("process", args => $"process_write({args[0]}, {args[1]}, {args[2]}, {args[3]})"),
        ["process.read"]    = new("process", args => $"process_read({args[0]}, {args[1]}, {args[2]}, {args[3]})"),
        ["process.len"]     = new("process", args => $"process_len({args[0]})"),
        ["process.cap"]     = new("process", args => $"process_cap({args[0]})"),
        ["process.free"]    = new("process", args => $"process_free({args[0]})"),
    };

    private static string Join(List<string> args) => string.Join(", ", args);
}
