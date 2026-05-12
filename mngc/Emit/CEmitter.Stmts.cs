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
            case BreakStmt:        Line("break;");         break;
            case ContinueStmt:     Line("continue;");      break;
            case RebindStmt rb:    EmitRebind(rb);         break;
            case DerefBindStmt db: EmitDerefBind(db);      break;
            case ContainerStmt cs: EmitContainer(cs);      break;
            case PhasedStmt ps:    EmitPhased(ps);         break;
            case DePhasedStmt dp:  EmitDephased(dp);       break;
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

    private void EmitRebind(RebindStmt r) => Line($"{r.Name} = {EmitExpr(r.Value)};");

    private void EmitDerefBind(DerefBindStmt d)
    {
        // deref bind :ref = ~ptr  →  <inferred-type> ref = *<source>;
        // We don't track the inner type here so we emit void* and let C handle it.
        Line($"void* {d.Name} = (void*)({EmitExpr(d.Source)});");
    }

    private void EmitContainer(ContainerStmt cs)
    {
        _requiredHeaders.Add("mono.phase");
        Line($"/* container :{cs.VarName} — scoped thread group, all threads joined on exit */");
        Line($"mg_container_t* {cs.VarName} = mg_container_new();");
        Line("{");
        Push();
        EmitContainerBody(cs.Body, cs.VarName, detach: false);
        Pop();
        Line("}");
        Line($"mg_container_join({cs.VarName});");
    }

    private void EmitPhased(PhasedStmt ps)
    {
        _requiredHeaders.Add("mono.phase");
        Line($"/* phased :{ps.VarName} — all threads must complete before execution continues */");
        // Count thread spawns to initialise the barrier correctly
        int threadCount = ps.Body.Stmts.Count(s => s is ExprStmt { Expr: CallExpr c } &&
            TryQualifiedCallName(c) == "process.thread");
        Line($"mg_phased_t* {ps.VarName} = mg_phased_new({Math.Max(threadCount, 1)});");
        Line("{");
        Push();
        EmitContainerBody(ps.Body, ps.VarName, detach: false);
        Pop();
        Line("}");
        Line($"mg_phased_join({ps.VarName});");
    }

    private void EmitDephased(DePhasedStmt dp)
    {
        _requiredHeaders.Add("mono.phase");
        Line($"/* dephased — fire-and-forget, no synchronisation */");
        Line("{");
        Push();
        EmitContainerBody(dp.Body, containerVar: null, detach: true);
        Pop();
        Line("}");
    }

    // Emit the body of a container/phased/dephased block.
    // Thread spawns (process.thread(:fn)) are intercepted and emitted as mg_thread_spawn.
    private void EmitContainerBody(BlockStmt body, string? containerVar, bool detach)
    {
        foreach (var stmt in body.Stmts)
        {
            if (stmt is ExprStmt { Expr: CallExpr call } &&
                TryQualifiedCallName(call) == "process.thread" &&
                call.Args.Count == 1)
            {
                var fn = EmitExpr(call.Args[0].Value);
#if _WIN32_NOT_SET
                var handleVar = $"_th_{fn.Replace(".", "_")}";
                Line($"HANDLE {handleVar} = CreateThread(NULL, 0, (LPTHREAD_START_ROUTINE)mg_thread_run, {fn}, 0, NULL);");
                if (containerVar != null) Line($"mg_container_add({containerVar}, {handleVar});");
#else
                var handleVar = $"_th_{fn.Replace(".", "_").Replace(":", "")}";
                Line($"pthread_t {handleVar};");
                Line($"pthread_create(&{handleVar}, NULL, mg_thread_run, (void*)(uintptr_t){fn});");
                if (!detach && containerVar != null) Line($"mg_container_add({containerVar}, {handleVar});");
                else if (detach) Line($"pthread_detach({handleVar});");
#endif
            }
            else
            {
                EmitStmt(stmt);
            }
        }
    }

    private static string? TryQualifiedCallName(CallExpr c)
    {
        static string? Flatten(ExprNode e) => e switch
        {
            IdentifierExpr id  => id.Name,
            MemberAccessExpr m => Flatten(m.Object) is string o ? $"{o}.{m.Member}" : null,
            _                  => null,
        };
        return Flatten(c.Callee);
    }
}

