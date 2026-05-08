using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace monogram_lsp.Handlers;

public class HoverHandler : HoverHandlerBase
{
    private readonly DocumentStore _store;

    public HoverHandler(DocumentStore store) { _store = store; }

    public override Task<Hover?> Handle(HoverParams request, CancellationToken ct)
    {
        var text = _store.Get(request.TextDocument.Uri);
        if (text == null) return Task.FromResult<Hover?>(null);

        var word = ExtractQualifiedWord(text, request.Position);
        if (word == null) return Task.FromResult<Hover?>(null);

        if (!_docs.TryGetValue(word, out var doc))
            return Task.FromResult<Hover?>(null);

        return Task.FromResult<Hover?>(new Hover
        {
            Contents = new MarkedStringsOrMarkupContent(new MarkupContent
            {
                Kind  = MarkupKind.Markdown,
                Value = doc,
            }),
        });
    }

    protected override HoverRegistrationOptions CreateRegistrationOptions(
        HoverCapability capability, ClientCapabilities clientCapabilities) => new()
    {
        DocumentSelector = TextDocumentHandler.Selector,
    };

    // Extract the word under the cursor, including dots for qualified names like sys.stdout
    private static string? ExtractQualifiedWord(string text, Position pos)
    {
        var lines = text.Split('\n');
        if (pos.Line >= lines.Length) return null;
        var line = lines[(int)pos.Line];
        var col  = (int)pos.Character;
        if (col >= line.Length) return null;

        int start = col;
        while (start > 0 && (char.IsLetterOrDigit(line[start - 1]) || line[start - 1] == '_' || line[start - 1] == '.'))
            start--;
        int end = col;
        while (end < line.Length && (char.IsLetterOrDigit(line[end]) || line[end] == '_' || line[end] == '.'))
            end++;

        return end > start ? line[start..end] : null;
    }

    private static readonly Dictionary<string, string> _docs = new()
    {
        // sys
        ["sys.stdout"]     = "**sys.stdout**(:'fmt', args)\n\nWrite formatted text to stdout. Format string uses C `printf` format specifiers.",
        ["sys.stderr"]     = "**sys.stderr**(:'fmt', args)\n\nWrite formatted text to stderr.",
        ["sys.exit"]       = "**sys.exit**(:code)\n\nTerminate the process with the given exit code.",

        // std.mem
        ["std.mem.alloc"]  = "**std.mem.alloc**(:size) `=> void*`\n\nAllocate `size` bytes on the heap. Returns a pointer.",
        ["std.mem.calloc"] = "**std.mem.calloc**(:n, :size) `=> void*`\n\nAllocate `n` elements of `size` bytes each, zero-initialized.",
        ["std.mem.realloc"]= "**std.mem.realloc**(:ptr, :size) `=> void*`\n\nResize an existing allocation.",
        ["std.mem.free"]   = "**std.mem.free**(:ptr)\n\nFree a heap allocation.",

        // std.str
        ["std.str.len"]    = "**std.str.len**(:s) `=> int`\n\nLength of a null-terminated string.",
        ["std.str.copy"]   = "**std.str.copy**(:dst, :src)\n\nCopy string `src` into `dst`.",
        ["std.str.cat"]    = "**std.str.cat**(:dst, :src)\n\nAppend `src` to `dst`.",
        ["std.str.cmp"]    = "**std.str.cmp**(:a, :b) `=> int`\n\nCompare two strings. Returns 0 if equal.",
        ["std.str.chr"]    = "**std.str.chr**(:s, :c) `=> char*`\n\nFind first occurrence of character `c` in string `s`.",
        ["std.str.fmt"]    = "**std.str.fmt**(:buf, :'fmt', args)\n\nFormat into a buffer (sprintf).",

        // std.math
        ["std.math.sqrt"]  = "**std.math.sqrt**(:x) `=> float64`\n\nSquare root.",
        ["std.math.pow"]   = "**std.math.pow**(:base, :exp) `=> float64`\n\nRaise `base` to the power `exp`.",
        ["std.math.abs"]   = "**std.math.abs**(:x) `=> float64`\n\nAbsolute value.",
        ["std.math.floor"] = "**std.math.floor**(:x) `=> float64`\n\nFloor (round down).",
        ["std.math.ceil"]  = "**std.math.ceil**(:x) `=> float64`\n\nCeiling (round up).",
        ["std.math.sin"]   = "**std.math.sin**(:x) `=> float64`\n\nSine of `x` (radians).",
        ["std.math.cos"]   = "**std.math.cos**(:x) `=> float64`\n\nCosine of `x` (radians).",
        ["std.math.tan"]   = "**std.math.tan**(:x) `=> float64`\n\nTangent of `x` (radians).",
        ["std.math.log"]   = "**std.math.log**(:x) `=> float64`\n\nNatural logarithm.",

        // std.io
        ["std.io.open"]    = "**std.io.open**(:'path', :'mode') `=> file`\n\nOpen a file. Mode: `'r'`, `'w'`, `'a'`, `'rb'`, etc.",
        ["std.io.close"]   = "**std.io.close**(:file)\n\nClose a file handle.",
        ["std.io.read"]    = "**std.io.read**(:buf, :n, :file)\n\nRead up to `n` bytes from `file` into `buf`.",
        ["std.io.write"]   = "**std.io.write**(:buf, :file)\n\nWrite string `buf` to `file`.",
        ["std.io.flush"]   = "**std.io.flush**(:file)\n\nFlush the file buffer.",
        ["std.io.scanf"]   = "**std.io.scanf**(:'fmt', args)\n\nRead formatted input from stdin.",

        // node
        ["node.new"]       = "**node.new**(:value) `=> node`\n\nCreate a new graph node holding `value`.",
        ["node.link"]      = "**node.link**(:a, :b)\n\nLink node `a` → `b` (sets `a.next` and `b.prev`).",
        ["node.get"]       = "**node.get**(:n) `=> void*`\n\nGet the value stored in node `n`.",
        ["node.set"]       = "**node.set**(:n, :value)\n\nSet the value stored in node `n`.",
        ["node.next"]      = "**node.next**(:n) `=> node`\n\nGet the next node in the chain.",
        ["node.prev"]      = "**node.prev**(:n) `=> node`\n\nGet the previous node in the chain.",
        ["node.free"]      = "**node.free**(:n)\n\nFree node `n` (does not free the stored value).",
        ["node.transform"] = "**node.transform**(:n, :fn) `=> node<,>`\n\nApply transform function `fn` to node `n`, returning a transform node.",

        // lattice
        ["lattice.new"]           = "**lattice.new**(:rows, :cols) `=> lattice`\n\nCreate a `rows × cols` 2D grid.",
        ["lattice.new_transform"] = "**lattice.new_transform**(:rows, :cols, :fn) `=> lattice`\n\nCreate a lattice with a transform function applied on `lattice.apply`.",
        ["lattice.get"]           = "**lattice.get**(:l, :r, :c) `=> void*`\n\nGet the value at row `r`, column `c`.",
        ["lattice.set"]           = "**lattice.set**(:l, :r, :c, :v)\n\nSet the value at row `r`, column `c`.",
        ["lattice.apply"]         = "**lattice.apply**(:l, :r, :c) `=> void*`\n\nApply the lattice transform function at cell `(r, c)`.",
        ["lattice.rows"]          = "**lattice.rows**(:l) `=> int`\n\nNumber of rows.",
        ["lattice.cols"]          = "**lattice.cols**(:l) `=> int`\n\nNumber of columns.",
        ["lattice.free"]          = "**lattice.free**(:l)\n\nFree the lattice and its data buffer.",

        // slice
        ["slice.new"]      = "**slice.new**(:n) `=> slice<T>`\n\nCreate a slice of `n` elements. Elements are `uintptr_t` — holds primitives and pointers.",
        ["slice.get"]      = "**slice.get**(:s, :i) `=> uintptr_t`\n\nGet element at index `i`.",
        ["slice.set"]      = "**slice.set**(:s, :i, :v)\n\nSet element at index `i` to `v`.",
        ["slice.len"]      = "**slice.len**(:s) `=> int`\n\nNumber of elements.",
        ["slice.free"]     = "**slice.free**(:s)\n\nFree the slice and its data buffer.",

        // process
        ["process.new"]    = "**process.new**(:cap) `=> process`\n\nCreate a byte buffer of capacity `cap`.",
        ["process.get"]    = "**process.get**(:p, :i) `=> byte`\n\nGet byte at index `i`.",
        ["process.set"]    = "**process.set**(:p, :i, :v)\n\nSet byte at index `i`.",
        ["process.write"]  = "**process.write**(:p, :off, :src, :n)\n\nWrite `n` bytes from `src` into the buffer at offset `off`.",
        ["process.read"]   = "**process.read**(:p, :off, :dst, :n)\n\nRead `n` bytes from the buffer at offset `off` into `dst`.",
        ["process.len"]    = "**process.len**(:p) `=> int`\n\nNumber of bytes written.",
        ["process.cap"]    = "**process.cap**(:p) `=> int`\n\nTotal buffer capacity.",
        ["process.free"]   = "**process.free**(:p)\n\nFree the process buffer.",
    };
}
