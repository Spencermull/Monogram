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
            if (!ResolveImport(imp.ModulePath, imp.Wildcard))
                _sb.AppendLine($"/* unresolved import: {imp.ModulePath}{(imp.Wildcard ? ".*" : "")} */");
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

    private bool ResolveImport(string path, bool wildcard)
    {
        switch (path)
        {
            case "std.io":   _requiredHeaders.Add("<stdio.h>");  return true;
            case "std.mem":  _requiredHeaders.Add("<stdlib.h>"); return true;
            case "std.str":  _requiredHeaders.Add("<string.h>"); return true;
            case "std.math": _requiredHeaders.Add("<math.h>");   return true;
            case "std" when wildcard:
                _requiredHeaders.Add("<stdio.h>");
                _requiredHeaders.Add("<stdlib.h>");
                _requiredHeaders.Add("<string.h>");
                _requiredHeaders.Add("<math.h>");
                return true;
            case "node":    _requiredHeaders.Add("node");    return true;
            case "lattice": _requiredHeaders.Add("lattice"); return true;
            case "process": _requiredHeaders.Add("process"); return true;
            case "slice":   _requiredHeaders.Add("slice");   return true;
            default: return false;
        }
    }

    // Emit forward declarations for all functions so call order doesn't matter
    private void EmitForwardDeclarations(List<StmtNode> decls)
    {
        foreach (var decl in decls)
        {
            if (decl is FuncDeclNode f)
            {
                if (f.GenericParams.Count > 0)
                    throw new NotSupportedException("generic functions are not yet supported by the emitter");
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
