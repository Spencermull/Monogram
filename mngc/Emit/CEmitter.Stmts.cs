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
        Line($"/* match: {m.Subject} */");
        bool first = true;
        foreach (var arm in m.Arms)
        {
            var pattern = arm.Pattern != null
                ? $"/* {m.Subject} is {EmitTypeExpr(arm.Pattern)} */"
                : "/* _ (wildcard) */";

            if (first) { Line($"if ({pattern} 0)"); first = false; }
            else        Line($"else if ({pattern} 0)");
            EmitBlock(arm.Body);
        }
    }

    private void EmitForEach(ForEachStmt f)
    {
        var coll = EmitExpr(f.Collection);
        Line($"for (size_t _i = 0; /* TODO: _i < len({coll}) */ ; _i++)");
        Line("{");
        Push();
        Line($"/* {f.VarName} = {coll}[_i] */");
        foreach (var s in f.Body.Stmts) EmitStmt(s);
        Pop();
        Line("}");
    }

    private void EmitForMap(ForMapStmt f)
    {
        var coll = EmitExpr(f.Collection);
        Line($"/* mapped (parallel) foreach — emitted as sequential */");
        Line($"for (size_t _i = 0; /* TODO: _i < len({coll}) */ ; _i++)");
        Line("{");
        Push();
        Line($"/* {f.VarName} = {coll}[_i] */");
        foreach (var s in f.Body.Stmts) EmitStmt(s);
        Pop();
        Line("}");
    }

    private void EmitForTyped(ForTypedStmt f)
    {
        Line($"/* for -> type {f.TypeName}: {f.VarName} */");
        Line($"for ({f.TypeName}* _it = {f.VarName}; _it != NULL; _it++)");
        Line("{");
        Push();
        Line($"{f.TypeName} {f.VarName} = *_it;");
        foreach (var s in f.Body.Stmts) EmitStmt(s);
        Pop();
        Line("}");
    }

    private void EmitForIter(ForIterStmt f)
    {
        var expr = EmitExpr(f.Expr);
        var cond = EmitExpr(f.CondVal);
        Line($"for (; {expr} {f.CondOp} {cond}; )");
        Line("{");
        Push();
        Line($"/* {f.VarName} bound to {expr} */");
        foreach (var s in f.Body.Stmts) EmitStmt(s);
        Pop();
        Line("}");
    }

    private void EmitForMappedIter(ForMappedIterStmt f)
    {
        var expr = EmitExpr(f.Expr);
        var cond = EmitExpr(f.CondVal);
        Line($"/* mapped (parallel) iter — emitted as sequential */");
        Line($"for (; {expr} {f.CondOp} {cond}; )");
        Line("{");
        Push();
        Line($"/* {f.VarName} bound to {expr} */");
        foreach (var s in f.Body.Stmts) EmitStmt(s);
        Pop();
        Line("}");
    }
}

