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
        // A single-char mode like 'r' is parsed as CharLit and emits as 'r' (int) in C.
        // fopen requires a string — convert 'x' → "x" at emit time.
        ["std.io.open"]     = new("<stdio.h>",   args => {
            var mode = args[1];
            if (mode.Length == 3 && mode[0] == '\'' && mode[2] == '\'')
                mode = $"\"{mode[1]}\"";
            return $"fopen({args[0]}, {mode})";
        }),
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

        // mono.sync — transmutex (adaptive spinlock → mutex)
        ["mono.sync.transmutex"] = new("mono.sync", args => "mg_transmutex_new()"),
        ["mono.sync.acquire"]    = new("mono.sync", args => $"mg_transmutex_acquire({args[0]})"),
        ["mono.sync.release"]    = new("mono.sync", args => $"mg_transmutex_release({args[0]})"),
        ["mono.sync.free"]       = new("mono.sync", args => $"mg_transmutex_free({args[0]})"),

        // mono.pipe — sink, bucket, coagulate
        ["mono.pipe.sink"]      = new("mono.pipe", args => $"mg_sink_new({args[0]})"),
        ["mono.pipe.write"]     = new("mono.pipe", args => $"mg_sink_write({args[0]}, {args[1]})"),
        ["mono.pipe.sink_free"] = new("mono.pipe", args => $"mg_sink_free({args[0]})"),
        ["mono.pipe.bucket"]    = new("mono.pipe", args => $"mg_bucket_new({args[0]})"),
        ["mono.pipe.fill"]      = new("mono.pipe", args => $"mg_bucket_fill({args[0]}, {args[1]})"),
        ["mono.pipe.drain"]     = new("mono.pipe", args => $"mg_bucket_drain({args[0]})"),
        ["mono.pipe.bucket_free"]= new("mono.pipe", args => $"mg_bucket_free({args[0]})"),
        ["mono.pipe.coagulate"] = new("mono.pipe", args => $"mg_coagulate({args[0]}, {args[1]}, {args[2]}, {args[3]}, {args[4]})"),

        // mono.pool — aliasing-free memory pool
        ["mono.pool.new"]   = new("mono.pool", args => $"mg_pool_new({args[0]})"),
        ["mono.pool.alloc"] = new("mono.pool", args => $"mg_pool_alloc({args[0]}, {args[1]})"),
        ["mono.pool.reset"] = new("mono.pool", args => $"mg_pool_reset({args[0]})"),
        ["mono.pool.free"]  = new("mono.pool", args => $"mg_pool_free({args[0]})"),

        // mono.linear — linear execution chains
        ["mono.linear.new"]  = new("mono.linear", args => "mg_linear_new()"),
        ["mono.linear.bind"] = new("mono.linear", args => $"mg_linear_bind({args[0]}, {args[1]})"),
        ["mono.linear.run"]  = new("mono.linear", args => $"mg_linear_run({args[0]}, {args[1]})"),
        ["mono.linear.free"] = new("mono.linear", args => $"mg_linear_free({args[0]})"),

        // mono.graph — graph matrices and adjacency structures
        ["mono.graph.new"]      = new("mono.graph", args => $"mg_graph_new({args[0]})"),
        ["mono.graph.add"]      = new("mono.graph", args => $"mg_graph_add({args[0]}, {args[1]})"),
        ["mono.graph.link"]     = new("mono.graph", args => $"mg_graph_link({args[0]}, {args[1]}, {args[2]})"),
        ["mono.graph.unlink"]   = new("mono.graph", args => $"mg_graph_unlink({args[0]}, {args[1]}, {args[2]})"),
        ["mono.graph.has_edge"] = new("mono.graph", args => $"mg_graph_has_edge({args[0]}, {args[1]}, {args[2]})"),
        ["mono.graph.node"]     = new("mono.graph", args => $"mg_graph_node({args[0]}, {args[1]})"),
        ["mono.graph.count"]    = new("mono.graph", args => $"mg_graph_count({args[0]})"),
        ["mono.graph.free"]     = new("mono.graph", args => $"mg_graph_free({args[0]})"),

        // mono.inspect — live structure inspection
        ["mono.inspect.dump"]  = new("mono.inspect", args => $"mg_inspect_dump({args[0]}, {args[1]})"),
        ["mono.inspect.addr"]  = new("mono.inspect", args => $"mg_inspect_addr({args[0]})"),
        ["mono.inspect.name"]  = new("mono.inspect", args => $"mg_inspect_name({args[0]}, {args[1]})"),
        ["mono.inspect.int"]   = new("mono.inspect", args => $"mg_inspect_int({args[0]}, {args[1]})"),
        ["mono.inspect.float"] = new("mono.inspect", args => $"mg_inspect_float({args[0]}, {args[1]})"),

        // mono.glob — pattern matching on memory regions
        ["mono.glob.match"] = new("mono.glob", args => $"mg_glob_match({args[0]}, {args[1]})"),
        ["mono.glob.scan"]  = new("mono.glob", args => $"mg_blob_scan({args[0]}, {args[1]}, {args[2]}, {args[3]})"),

        // mono.utils — utility belt
        ["mono.utils.swap"]      = new("mono.utils", args => $"mg_swap({args[0]}, {args[1]}, {args[2]})"),
        ["mono.utils.ispow2"]    = new("mono.utils", args => $"mg_ispow2({args[0]})"),
        ["mono.utils.next_pow2"] = new("mono.utils", args => $"mg_next_pow2({args[0]})"),

        // mono.polymorph — runtime type dispatch
        ["mono.polymorph.new"]  = new("mono.polymorph", args => $"mg_poly_new({args[0]}, {args[1]})"),
        ["mono.polymorph.bind"] = new("mono.polymorph", args => $"mg_poly_bind({args[0]}, {args[1]})"),
        ["mono.polymorph.call"] = new("mono.polymorph", args => $"mg_poly_call({args[0]}, {args[1]})"),
        ["mono.polymorph.is"]   = new("mono.polymorph", args => $"mg_poly_is({args[0]}, {args[1]})"),
        ["mono.polymorph.free"] = new("mono.polymorph", args => $"mg_poly_free({args[0]})"),

        // mono.podlib — portable datasets
        ["mono.podlib.create"] = new("mono.podlib", args => $"mg_pod_create({args[0]})"),
        ["mono.podlib.attach"] = new("mono.podlib", args => $"mg_pod_attach({args[0]}, {args[1]})"),
        ["mono.podlib.run"]    = new("mono.podlib", args => $"mg_pod_run({args[0]})"),
        ["mono.podlib.export"] = new("mono.podlib", args => $"mg_pod_export({args[0]}, {args[1]})"),
        ["mono.podlib.import"] = new("mono.podlib", args => $"mg_pod_import({args[0]})"),
        ["mono.podlib.free"]   = new("mono.podlib", args => $"mg_pod_free({args[0]})"),

        // mono.utdctrl — universal thread control
        ["mono.utdctrl.init"]      = new("mono.utdctrl", args => "mg_utdctrl_init()"),
        ["mono.utdctrl.spawn"]     = new("mono.utdctrl", args => $"mg_utdctrl_spawn({args[0]}, {args[1]})"),
        ["mono.utdctrl.telemetry"] = new("mono.utdctrl", args => $"mg_utdctrl_telemetry({args[0]}, {args[1]})"),
        ["mono.utdctrl.monitor"]   = new("mono.utdctrl", args => $"mg_utdctrl_monitor({args[0]})"),
        ["mono.utdctrl.shutdown"]  = new("mono.utdctrl", args => $"mg_utdctrl_shutdown({args[0]})"),

        // mtx.argus — logging and diagnostics
        ["mtx.argus.new"]   = new("mtx.argus", args => $"mg_argus_new({args[0]})"),
        ["mtx.argus.log"]   = new("mtx.argus", args => $"mg_argus_log({args[0]}, {args[1]}, {args[2]})"),
        ["mtx.argus.debug"] = new("mtx.argus", args => $"mg_argus_debug({args[0]}, {args[1]})"),
        ["mtx.argus.info"]  = new("mtx.argus", args => $"mg_argus_info({args[0]}, {args[1]})"),
        ["mtx.argus.warn"]  = new("mtx.argus", args => $"mg_argus_warn({args[0]}, {args[1]})"),
        ["mtx.argus.error"] = new("mtx.argus", args => $"mg_argus_error({args[0]}, {args[1]})"),
        ["mtx.argus.fatal"] = new("mtx.argus", args => $"mg_argus_fatal({args[0]}, {args[1]})"),
        ["mtx.argus.free"]  = new("mtx.argus", args => $"mg_argus_free({args[0]})"),

        // mtx.benchmark — profiling and timing
        ["mtx.benchmark.new"]    = new("mtx.benchmark", args => $"mg_bench_new({args[0]})"),
        ["mtx.benchmark.start"]  = new("mtx.benchmark", args => $"mg_bench_start({args[0]})"),
        ["mtx.benchmark.stop"]   = new("mtx.benchmark", args => $"mg_bench_stop({args[0]})"),
        ["mtx.benchmark.report"] = new("mtx.benchmark", args => $"mg_bench_report({args[0]})"),
        ["mtx.benchmark.ms"]     = new("mtx.benchmark", args => $"mg_bench_ms({args[0]})"),
        ["mtx.benchmark.run"]    = new("mtx.benchmark", args => $"mg_bench_run({args[0]}, {args[1]}, {args[2]})"),
        ["mtx.benchmark.free"]   = new("mtx.benchmark", args => $"mg_bench_free({args[0]})"),

        // mtx.encode — encoding and decoding
        ["mtx.encode.hex"]       = new("mtx.encode", args => $"mg_hex_encode({args[0]}, {args[1]})"),
        ["mtx.encode.unhex"]     = new("mtx.encode", args => $"mg_hex_decode({args[0]}, {args[1]})"),
        ["mtx.encode.base64"]    = new("mtx.encode", args => $"mg_base64_encode({args[0]}, {args[1]})"),
        ["mtx.encode.utf8_valid"]= new("mtx.encode", args => $"mg_utf8_valid({args[0]})"),
        ["mtx.encode.ascii_only"]= new("mtx.encode", args => $"mg_ascii_only({args[0]})"),

        // mtx.hash — checksums and hashing
        ["mtx.hash.fnv1a"] = new("mtx.hash", args => $"mg_fnv1a({args[0]}, {args[1]})"),
        ["mtx.hash.crc32"] = new("mtx.hash", args => $"mg_crc32({args[0]}, {args[1]})"),
        ["mtx.hash.djb2"]  = new("mtx.hash", args => $"mg_djb2({args[0]})"),

        // mtx.compress — RLE compression
        ["mtx.compress.encode"] = new("mtx.compress", args => $"mg_rle_encode({args[0]}, {args[1]}, {args[2]})"),
        ["mtx.compress.decode"] = new("mtx.compress", args => $"mg_rle_decode({args[0]}, {args[1]}, {args[2]})"),

        // mtx.kiln — build and transform pipeline
        ["mtx.kiln.new"]   = new("mtx.kiln", args => "mg_kiln_new()"),
        ["mtx.kiln.stage"] = new("mtx.kiln", args => $"mg_kiln_stage({args[0]}, {args[1]}, {args[2]})"),
        ["mtx.kiln.run"]   = new("mtx.kiln", args => $"mg_kiln_run({args[0]}, {args[1]})"),
        ["mtx.kiln.free"]  = new("mtx.kiln", args => $"mg_kiln_free({args[0]})"),

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
