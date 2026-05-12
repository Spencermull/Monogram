using mngc.AST;

namespace mngc.Emit;

public partial class CEmitter
{
    private void EmitFuncDecl(FuncDeclNode f)
    {
        if (f.GenericParams.Count > 0)
            throw new NotSupportedException("generic functions are not yet supported by the emitter");
        var ret   = f.Return != null ? EmitTypeExpr(f.Return.Type) : "void";
        // argx/xarg params are emitted with the declared type (coercion is caller-side)
        // argm/xargm params are also emitted with declared type; transform is applied in prologue
        var parms = string.Join(", ", f.Params.Select(p => $"{EmitTypeExpr(p.Type)} {p.Name}"));
        Line($"{ret} {f.Name}({parms})");
        Line("{");
        Push();
        // argm / xargm prologue: apply the transform to the param before the body executes
        foreach (var p in f.Params.Where(p => p.ArgQual is ArgQualifier.Argm or ArgQualifier.Xargm && p.ArgTransform != null))
            Line($"{p.Name} = {p.ArgTransform}({p.Name});");
        foreach (var s in f.Body.Stmts)
            EmitStmt(s);
        Pop();
        Line("}");
        _sb.AppendLine();
    }

    private void EmitOpDecl(OpDeclNode o)
    {
        Line($"/* op: params are untyped by design — void* erased */");
        var parms = string.Join(", ", o.OpParams.Select(p => $"void* {p}"));
        Line($"{o.ReturnType} {o.Name}({parms})");
        EmitBlock(o.Body);
        _sb.AppendLine();
    }

    private void EmitTypeDecl(TypeDeclNode t)
    {
        switch (t.Body)
        {
            case StructTypeBody s:
                Line($"typedef struct {{");
                Push();
                foreach (var field in s.Fields)
                    Line($"{EmitTypeExpr(field.Type)} {field.Name};");
                Pop();
                Line($"}} {t.Name};");
                break;

            case TransformTypeBody tr:
                Line($"typedef {EmitTypeExpr(tr.To)} (*{t.Name})({EmitTypeExpr(tr.From)});");
                break;

            case CollectionTypeBody:
                Line($"/* collection type {t.Name} — define backing struct manually */");
                Line($"typedef struct {t.Name}_s {t.Name};");
                break;

            case null:
                Line($"typedef struct {t.Name}_s {t.Name};");
                break;
        }
        _sb.AppendLine();
    }

    private void EmitVarDecl(VarDeclNode v)
    {
        var mut  = MapMutability(v.Mutability);
        var type = EmitTypeExpr(v.Type);
        var init = v.Initializer != null ? $" = {EmitExpr(v.Initializer)}" : "";
        Line($"{mut}{type} {v.Name}{init};");
    }
}
