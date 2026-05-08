using mngc.AST;

namespace mngc.Emit;

public partial class CEmitter
{
    private readonly System.Text.StringBuilder _sb = new();
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
        EmitPreamble(program.Imports);
        _sb.AppendLine();
        EmitForwardDeclarations(program.Declarations);
        EmitEntryPoint(program.EntryPoint);
        foreach (var decl in program.Declarations)
            EmitStmt(decl);
        return _sb.ToString();
    }

    private void EmitPreamble(List<ImportNode> imports)
    {
        // Always needed for fixed-width types and bool
        Line("#include <stdint.h>");
        Line("#include <stdbool.h>");
        Line("#include <stddef.h>");

        foreach (var imp in imports)
        {
            var header = ResolveImport(imp.ModulePath, imp.Wildcard);
            if (header != null)
                Line($"#include {header}");
            else
                Line($"/* unresolved import: {imp.ModulePath}{(imp.Wildcard ? ".*" : "")} */");
        }
    }

    private static string? ResolveImport(string path, bool wildcard) => path switch
    {
        "std.io"     or "std" when wildcard => "<stdio.h>",
        "std.mem"                           => "<stdlib.h>",
        "std.str"                           => "<string.h>",
        "std.math"                          => "<math.h>",
        _                                   => null,
    };

    // Emit forward declarations for all functions so call order doesn't matter
    private void EmitForwardDeclarations(List<StmtNode> decls)
    {
        foreach (var decl in decls)
        {
            if (decl is FuncDeclNode f)
            {
                var ret = f.Return != null ? EmitTypeExpr(f.Return.Type) : "void";
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
