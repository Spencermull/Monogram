using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace monogram_lsp.Handlers;

public class CompletionHandler : CompletionHandlerBase
{
    private static readonly CompletionList _completions = BuildCompletions();

    public override Task<CompletionList> Handle(CompletionParams request, CancellationToken ct)
        => Task.FromResult(_completions);

    public override Task<CompletionItem> Handle(CompletionItem request, CancellationToken ct)
        => Task.FromResult(request);

    protected override CompletionRegistrationOptions CreateRegistrationOptions(
        CompletionCapability capability, ClientCapabilities clientCapabilities) => new()
    {
        DocumentSelector = TextDocumentHandler.Selector,
        TriggerCharacters  = new Container<string>(".", ":"),
        ResolveProvider    = false,
    };

    private static CompletionList BuildCompletions()
    {
        var items = new List<CompletionItem>();

        // Keywords
        foreach (var kw in Keywords)
            items.Add(new CompletionItem { Label = kw, Kind = CompletionItemKind.Keyword });

        // Primitive types
        foreach (var t in PrimitiveTypes)
            items.Add(new CompletionItem { Label = t, Kind = CompletionItemKind.TypeParameter });

        // Builtin types
        foreach (var t in BuiltinTypes)
            items.Add(new CompletionItem { Label = t, Kind = CompletionItemKind.Class });

        // Stdlib functions
        foreach (var (label, detail, snippet) in StdlibFunctions)
            items.Add(new CompletionItem
            {
                Label            = label,
                Kind             = CompletionItemKind.Function,
                Detail           = detail,
                InsertText       = snippet,
                InsertTextFormat = InsertTextFormat.Snippet,
            });

        // Snippets
        foreach (var (label, detail, snippet) in Snippets)
            items.Add(new CompletionItem
            {
                Label            = label,
                Kind             = CompletionItemKind.Snippet,
                Detail           = detail,
                InsertText       = snippet,
                InsertTextFormat = InsertTextFormat.Snippet,
            });

        return new CompletionList(items);
    }

    private static readonly string[] Keywords =
    [
        "if", "else", "match", "for", "in", "as",
        "const", "volatile", "transform", "true", "false",
    ];

    private static readonly string[] PrimitiveTypes =
    [
        "int", "int8", "int16", "int32", "int64",
        "uint8", "uint16", "uint32", "uint64",
        "float", "float32", "float64",
        "char", "byte", "bool", "void",
    ];

    private static readonly string[] BuiltinTypes =
    [
        "node", "lattice", "process", "slice",
    ];

    private static readonly (string label, string detail, string snippet)[] StdlibFunctions =
    [
        ("sys.stdout",           "sys.stdout(:'fmt', args)",           "sys.stdout(:$0)"),
        ("sys.stderr",           "sys.stderr(:'fmt', args)",           "sys.stderr(:$0)"),
        ("sys.exit",             "sys.exit(:code)",                    "sys.exit(:$0)"),
        ("std.mem.alloc",        "std.mem.alloc(:size) => void*",      "std.mem.alloc(:$0)"),
        ("std.mem.calloc",       "std.mem.calloc(:n, :size)",          "std.mem.calloc(:$1, :$2)"),
        ("std.mem.realloc",      "std.mem.realloc(:ptr, :size)",       "std.mem.realloc(:$1, :$2)"),
        ("std.mem.free",         "std.mem.free(:ptr)",                 "std.mem.free(:$0)"),
        ("std.str.len",          "std.str.len(:s) => int",             "std.str.len(:$0)"),
        ("std.str.copy",         "std.str.copy(:dst, :src)",           "std.str.copy(:$1, :$2)"),
        ("std.str.cat",          "std.str.cat(:dst, :src)",            "std.str.cat(:$1, :$2)"),
        ("std.str.cmp",          "std.str.cmp(:a, :b) => int",         "std.str.cmp(:$1, :$2)"),
        ("std.str.chr",          "std.str.chr(:s, :c) => char*",       "std.str.chr(:$1, :$2)"),
        ("std.str.fmt",          "std.str.fmt(:buf, :'fmt', args)",    "std.str.fmt(:$0)"),
        ("std.math.sqrt",        "std.math.sqrt(:x) => float64",       "std.math.sqrt(:$0)"),
        ("std.math.pow",         "std.math.pow(:base, :exp)",          "std.math.pow(:$1, :$2)"),
        ("std.math.abs",         "std.math.abs(:x) => float64",        "std.math.abs(:$0)"),
        ("std.math.floor",       "std.math.floor(:x) => float64",      "std.math.floor(:$0)"),
        ("std.math.ceil",        "std.math.ceil(:x) => float64",       "std.math.ceil(:$0)"),
        ("std.io.open",          "std.io.open(:'path', :'mode')",       "std.io.open(:$1, :$2)"),
        ("std.io.close",         "std.io.close(:file)",                "std.io.close(:$0)"),
        ("std.io.read",          "std.io.read(:buf, :n, :file)",       "std.io.read(:$1, :$2, :$3)"),
        ("std.io.write",         "std.io.write(:buf, :file)",          "std.io.write(:$1, :$2)"),
        ("std.io.flush",         "std.io.flush(:file)",                "std.io.flush(:$0)"),
        ("std.io.scanf",         "std.io.scanf(:'fmt', args)",         "std.io.scanf(:$0)"),
        ("node.new",             "node.new(:value) => node",           "node.new(:$0)"),
        ("node.link",            "node.link(:a, :b)",                  "node.link(:$1, :$2)"),
        ("node.get",             "node.get(:n) => void*",              "node.get(:$0)"),
        ("node.set",             "node.set(:n, :value)",               "node.set(:$1, :$2)"),
        ("node.next",            "node.next(:n) => node",              "node.next(:$0)"),
        ("node.prev",            "node.prev(:n) => node",              "node.prev(:$0)"),
        ("node.free",            "node.free(:n)",                      "node.free(:$0)"),
        ("node.transform",       "node.transform(:n, :fn) => node<,>", "node.transform(:$1, :$2)"),
        ("lattice.new",          "lattice.new(:rows, :cols)",          "lattice.new(:$1, :$2)"),
        ("lattice.new_transform","lattice.new_transform(:rows, :cols, :fn)", "lattice.new_transform(:$1, :$2, :$3)"),
        ("lattice.get",          "lattice.get(:l, :r, :c)",            "lattice.get(:$1, :$2, :$3)"),
        ("lattice.set",          "lattice.set(:l, :r, :c, :v)",        "lattice.set(:$1, :$2, :$3, :$4)"),
        ("lattice.apply",        "lattice.apply(:l, :r, :c)",          "lattice.apply(:$1, :$2, :$3)"),
        ("lattice.rows",         "lattice.rows(:l) => int",            "lattice.rows(:$0)"),
        ("lattice.cols",         "lattice.cols(:l) => int",            "lattice.cols(:$0)"),
        ("lattice.free",         "lattice.free(:l)",                   "lattice.free(:$0)"),
        ("slice.new",            "slice.new(:n) => slice<T>",          "slice.new(:$0)"),
        ("slice.get",            "slice.get(:s, :i) => uintptr_t",     "slice.get(:$1, :$2)"),
        ("slice.set",            "slice.set(:s, :i, :v)",              "slice.set(:$1, :$2, :$3)"),
        ("slice.len",            "slice.len(:s) => int",               "slice.len(:$0)"),
        ("slice.free",           "slice.free(:s)",                     "slice.free(:$0)"),
        ("process.new",          "process.new(:cap) => process",       "process.new(:$0)"),
        ("process.get",          "process.get(:p, :i) => byte",        "process.get(:$1, :$2)"),
        ("process.set",          "process.set(:p, :i, :v)",            "process.set(:$1, :$2, :$3)"),
        ("process.write",        "process.write(:p, :off, :src, :n)",  "process.write(:$1, :$2, :$3, :$4)"),
        ("process.read",         "process.read(:p, :off, :dst, :n)",   "process.read(:$1, :$2, :$3, :$4)"),
        ("process.len",          "process.len(:p) => int",             "process.len(:$0)"),
        ("process.cap",          "process.cap(:p) => int",             "process.cap(:$0)"),
        ("process.free",         "process.free(:p)",                   "process.free(:$0)"),
    ];

    private static readonly (string label, string detail, string snippet)[] Snippets =
    [
        ("func:",    "function declaration",   "func: ${1:name}(${2::param ${3:Type}}) => ${4:Type} {\n\t$0\n}"),
        ("init",     "entry point",            "init void ${1:main}() {\n\t$0\n}"),
        ("if",       "if statement",           "if (${1:cond}) {\n\t$0\n}"),
        ("if/else",  "if/else statement",      "if (${1:cond}) {\n\t$2\n} else {\n\t$0\n}"),
        ("for:in",   "sequential foreach",     "for :${1:v} in ${2:coll} {\n\t$0\n}"),
        ("for->:in", "mapped foreach",         "for -> :${1:v} in ${2:coll} {\n\t$0\n}"),
        ("match:",   "match statement",        "match: ${1:var} {\n\t${2:Type} => {\n\t\t$0\n\t}\n\t_ => {\n\t}\n}"),
        ("type",     "type declaration",       "type ${1:Name} {\n\t:${2:field} ${3:Type}\n}"),
        ("#import",  "import module",          "#import<${1:std.io}>"),
    ];
}
