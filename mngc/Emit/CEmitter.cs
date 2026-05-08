using mngc.AST;
using mngc.Stdlib;

namespace mngc.Emit;

public partial class CEmitter
{
    private readonly System.Text.StringBuilder _sb = new();
    private readonly HashSet<string> _requiredHeaders = new()
    {
        "<stdint.h>", "<stdbool.h>", "<stddef.h>"
    };
    public IReadOnlySet<string> RequiredHeaders => _requiredHeaders;
    private int _indent = 0;
    private const string Tab = "    ";

    // --- Output helpers ---

    private void Write(string text) => _sb.Append(text);

    private void Line(string text = "")
    {
        for (int i = 0; i < _indent; i++) _sb.Append(Tab);
        _sb.AppendLine(text);
    }

    private void Push() => _indent++;
    private void Pop()  => _indent--;

    // --- Entry point ---

    public string Emit(ProgramNode program)
    {
        // Seed headers from explicit imports
        foreach (var imp in program.Imports)
        {
            var h = ResolveImport(imp.ModulePath, imp.Wildcard);
            if (h != null) _requiredHeaders.Add(h);
            else _sb.AppendLine($"/* unresolved import: {imp.ModulePath}{(imp.Wildcard ? ".*" : "")} */");
        }

        // Types first so forward decls and main can reference them
        foreach (var decl in program.Declarations.OfType<TypeDeclNode>())
            EmitTypeDecl(decl);
        if (program.Declarations.Any(d => d is TypeDeclNode))
            _sb.AppendLine();

        EmitForwardDeclarations(program.Declarations);
        EmitEntryPoint(program.EntryPoint);
        foreach (var decl in program.Declarations.Where(d => d is not TypeDeclNode))
            EmitStmt(decl);

        // Prepend headers then body.
        // _requiredHeaders entries starting with '<' or '"' are system/local includes.
        // All other entries are inline module names (node, lattice, process).
        var headers = new System.Text.StringBuilder();
        foreach (var h in _requiredHeaders.Where(h => h.StartsWith('<') || h.StartsWith('"')).OrderBy(x => x))
            headers.AppendLine($"#include {h}");
        headers.AppendLine();
        foreach (var module in _requiredHeaders.Where(h => !h.StartsWith('<') && !h.StartsWith('"')).OrderBy(x => x))
        {
            if (StdlibHeaders.All.TryGetValue(module, out var content))
            {
                headers.AppendLine(content);
                headers.AppendLine();
            }
        }

        return headers.ToString() + _sb.ToString();
    }

    private static string? ResolveImport(string path, bool wildcard) => path switch
    {
        "std.io"            => "<stdio.h>",
        "std" when wildcard => "<stdio.h>",
        "std.mem"           => "<stdlib.h>",
        "std.str"           => "<string.h>",
        "std.math"          => "<math.h>",
        "node"              => "node",      // inline module
        "lattice"           => "lattice",   // inline module
        "process"           => "process",   // inline module
        _                   => null,
    };

    // Emit forward declarations for all functions so call order doesn't matter
    private void EmitForwardDeclarations(List<StmtNode> decls)
    {
        foreach (var decl in decls)
        {
            if (decl is FuncDeclNode f)
            {
                var ret   = f.Return != null ? EmitTypeExpr(f.Return.Type) : "void";
                var parms = string.Join(", ", f.Params.Select(p => $"{EmitTypeExpr(p.Type)} {p.Name}"));
                Line($"{ret} {f.Name}({parms});");
            }
            else if (decl is OpDeclNode o)
            {
                var parms = string.Join(", ", o.OpParams.Select(p => $"void* {p}"));
                Line($"{o.ReturnType} {o.Name}({parms});");
            }
        }
        if (decls.Any(d => d is FuncDeclNode or OpDeclNode))
            _sb.AppendLine();
    }
}
