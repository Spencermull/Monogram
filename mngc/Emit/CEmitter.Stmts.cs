using mngc.AST;

namespace mngc.Emit;

public partial class CEmitter
{
    private void EmitEntryPoint(EntryPointNode entry)
    {
        Line($"int main(void)");
        EmitBlock(entry.Body);
        _sb.AppendLine();
    }

    private void EmitBlock(BlockStmt block)
    {
        Line("{");
        Push();
        foreach (var stmt in block.Stmts)
            EmitStmt(stmt);
        Pop();
        Line("}");
    }

    private void EmitStmt(StmtNode stmt)
    {
        switch (stmt)
        {
            case FuncDeclNode f:   EmitFuncDecl(f); break;
            case OpDeclNode o:     EmitOpDecl(o);   break;
            case TypeDeclNode t:   EmitTypeDecl(t); break;
            case VarDeclNode v:    EmitVarDecl(v);  break;
            case BlockStmt b:      EmitBlock(b);    break;
            case ReturnStmt r:     EmitReturn(r);   break;
            case AssignStmt a:     EmitAssign(a);   break;
            case ExprStmt e:       EmitExprStmt(e); break;
            case IfStmt i:         EmitIf(i);       break;
            case MatchStmt m:      EmitMatch(m);    break;
            case ForEachStmt fe:       EmitForEach(fe);       break;
            case ForMapStmt fm:        EmitForMap(fm);        break;
            case ForTypedStmt ft:      EmitForTyped(ft);      break;
            case ForIterStmt fi:       EmitForIter(fi);       break;
            case ForMappedIterStmt fmi:EmitForMappedIter(fmi);break;
            case BreakStmt:    Line("break;");    break;
            case ContinueStmt: Line("continue;"); break;
            default: Line($"/* unhandled stmt: {stmt.GetType().Name} */"); break;
        }
    }

    private void EmitReturn(ReturnStmt r) => Line($"return {EmitExpr(r.Value)};");

    private void EmitAssign(AssignStmt a) => Line($"{EmitExpr(a.Target)} = {EmitExpr(a.Value)};");

    private void EmitExprStmt(ExprStmt e) => Line($"{EmitExpr(e.Expr)};");

    private void EmitIf(IfStmt i)
    {
        Line($"if ({EmitExpr(i.Condition)})");
        EmitBlock(i.Then);
        foreach (var ei in i.ElseIfs)
        {
            Line($"else if ({EmitExpr(ei.Cond)})");
            EmitBlock(ei.Body);
        }
        if (i.Else != null)
        {
            Line("else");
            EmitBlock(i.Else);
        }
    }

    private void EmitMatch(MatchStmt m)
    {
        // match on types requires a type checker — not yet implemented
        throw new NotSupportedException("match statement requires a type checker — not yet implemented");
    }

    /// <summary>the collection expression is assumed to be mgslice_t* — passing any other type produces invalid C</summary>
    private void EmitForEach(ForEachStmt f)
    {
        Line($"/* for-in requires slice<T> — type not verified by compiler */");
        var coll = EmitExpr(f.Collection);
        Line($"for (size_t _i = 0; _i < {coll}->len; _i++)");
        Line("{");
        Push();
        Line($"uintptr_t {f.VarName} = {coll}->data[_i];");
        foreach (var s in f.Body.Stmts) EmitStmt(s);
        Pop();
        Line("}");
    }

    /// <summary>the collection expression is assumed to be mgslice_t* — passing any other type produces invalid C</summary>
    private void EmitForMap(ForMapStmt f)
    {
        Line($"/* for-in requires slice<T> — type not verified by compiler */");
        var coll = EmitExpr(f.Collection);
        Line($"/* mapped foreach — emitted as sequential */");
        Line($"for (size_t _i = 0; _i < {coll}->len; _i++)");
        Line("{");
        Push();
        Line($"uintptr_t {f.VarName} = {coll}->data[_i];");
        foreach (var s in f.Body.Stmts) EmitStmt(s);
        Pop();
        Line("}");
    }

    private void EmitForTyped(ForTypedStmt f)
    {
        Line($"/* for -> type {f.TypeName}: {f.VarName} */");
        Line($"for ({f.TypeName}* _typed_it = {f.VarName}; _typed_it != NULL; _typed_it++)");
        Line("{");
        Push();
        Line($"{f.TypeName} {f.VarName} = *_typed_it;");
        foreach (var s in f.Body.Stmts) EmitStmt(s);
        Pop();
        Line("}");
    }

    private void EmitForIter(ForIterStmt f)
    {
        var expr = EmitExpr(f.Expr);
        var cond = EmitExpr(f.CondVal);
        Line($"uintptr_t {f.VarName} = {expr};");
        Line($"for (; {f.VarName} {f.CondOp} {cond}; /* mutate :{f.VarName} in body to advance */)");
        Line("{");
        Push();
        foreach (var s in f.Body.Stmts) EmitStmt(s);
        Pop();
        Line("}");
    }

    private void EmitForMappedIter(ForMappedIterStmt f)
    {
        var expr = EmitExpr(f.Expr);
        var cond = EmitExpr(f.CondVal);
        Line($"/* mapped (parallel) iter — emitted as sequential */");
        Line($"uintptr_t {f.VarName} = {expr};");
        Line($"for (; {f.VarName} {f.CondOp} {cond}; /* mutate :{f.VarName} in body to advance */)");
        Line("{");
        Push();
        foreach (var s in f.Body.Stmts) EmitStmt(s);
        Pop();
        Line("}");
    }
}

