using mngc.AST;

namespace mngc.TypeChecker;

public enum LifecycleState { Raw, Live, Spent, Dead }

/// <summary>
/// Tracks per-binding lifecycle state (raw → live → spent → dead) and emits
/// SemanticErrors for violations. Run as a second pass after the type checker.
/// </summary>
public class LifecycleChecker
{
    private readonly List<SemanticError> _errors = new();
    private readonly Stack<Dictionary<string, LifecycleState>> _frames = new();

    public bool HasErrors => _errors.Count > 0;
    public IReadOnlyList<SemanticError> Errors => _errors;

    public void Check(ProgramNode program)
    {
        Push();
        // Hoist function names as permanently live
        foreach (var d in program.Declarations)
            if (d is FuncDeclNode f) Declare(f.Name, LifecycleState.Live);

        if (program.EntryPoint != null)
            CheckBlock(program.EntryPoint.Body, isFunc: true);

        foreach (var d in program.Declarations)
            CheckStmt(d);

        Pop();
    }

    // ─────────────────────────────────────────────────────────────────────────

    private void CheckStmt(StmtNode stmt)
    {
        switch (stmt)
        {
            case VarDeclNode v:
                var state = v.Initializer != null ? LifecycleState.Live : LifecycleState.Raw;
                if (v.Initializer != null) CheckExpr(v.Initializer);
                Declare(v.Name, state);
                break;

            case RebindStmt rb:
                CheckExpr(rb.Value);
                SetState(rb.Name, LifecycleState.Live);
                break;

            case DerefBindStmt db:
                CheckExpr(db.Source);
                Declare(db.Name, LifecycleState.Live);
                break;

            case AssignStmt a:
                CheckExpr(a.Value);
                // Mark target live if it is a plain identifier
                if (a.Target is IdentifierExpr { Name: var n }) SetState(n, LifecycleState.Live);
                break;

            case ReturnStmt r:
                // The returned value is consumed — mark spent if plain identifier
                if (r.Value is IdentifierExpr { Name: var rn })
                    ConsumeAndCheck(rn);
                else
                    CheckExpr(r.Value);
                break;

            case ExprStmt e:
                CheckExpr(e.Expr);
                break;

            case IfStmt i:
                CheckExpr(i.Condition);
                CheckBlock(i.Then);
                foreach (var ei in i.ElseIfs) { CheckExpr(ei.Cond); CheckBlock(ei.Body); }
                if (i.Else != null) CheckBlock(i.Else);
                break;

            case FuncDeclNode f:
                Push();
                foreach (var p in f.Params) Declare(p.Name, LifecycleState.Live);
                CheckBlock(f.Body, isFunc: true);
                Pop();
                break;

            case BlockStmt b:
                CheckBlock(b);
                break;

            case ForEachStmt fe:
                CheckExpr(fe.Collection);
                Push();
                Declare(fe.VarName, LifecycleState.Live);
                foreach (var s in fe.Body.Stmts) CheckStmt(s);
                Pop();
                break;

            case ForMapStmt fm:
                CheckExpr(fm.Collection);
                Push();
                Declare(fm.VarName, LifecycleState.Live);
                foreach (var s in fm.Body.Stmts) CheckStmt(s);
                Pop();
                break;

            case ForIterStmt fi:
                CheckExpr(fi.Expr);
                CheckExpr(fi.CondVal);
                Push();
                foreach (var s in fi.Body.Stmts) CheckStmt(s);
                Pop();
                break;

            case ForMappedIterStmt fmi:
                CheckExpr(fmi.Expr);
                CheckExpr(fmi.CondVal);
                Push();
                foreach (var s in fmi.Body.Stmts) CheckStmt(s);
                Pop();
                break;

            case ContainerStmt cs:
                CheckBlock(cs.Body);
                break;

            case PhasedStmt ps:
                CheckBlock(ps.Body);
                break;

            case DePhasedStmt dp:
                CheckBlock(dp.Body);
                break;

            case TransferStmt ts:
                // ~> transfer: source becomes spent, dest becomes live
                var srcState = GetState(ts.Source);
                if (srcState == LifecycleState.Spent)
                    Error($"LIFECYCLE ERROR: value '{ts.Source}' is spent — cannot transfer ownership");
                else if (srcState == LifecycleState.Raw)
                    Error($"LIFECYCLE ERROR: value '{ts.Source}' is raw — initialize before transferring");
                else if (srcState == LifecycleState.Dead)
                    Error($"LIFECYCLE ERROR: value '{ts.Source}' is dead — cannot transfer a freed value");
                SetState(ts.Source, LifecycleState.Spent);
                Declare(ts.Dest, LifecycleState.Live);
                break;
        }
    }

    private void CheckBlock(BlockStmt block, bool isFunc = false)
    {
        Push();
        foreach (var s in block.Stmts) CheckStmt(s);
        Pop();
    }

    private void CheckExpr(ExprNode expr)
    {
        switch (expr)
        {
            case IdentifierExpr { Name: var name }:
                var st = GetState(name);
                if (st == LifecycleState.Spent)
                    Error($"LIFECYCLE ERROR: value '{name}' is spent — cannot read after consumption");
                else if (st == LifecycleState.Raw)
                    Error($"LIFECYCLE ERROR: value '{name}' is raw — assign before reading");
                else if (st == LifecycleState.Dead)
                    Error($"LIFECYCLE ERROR: value '{name}' is dead — cannot read after free");
                break;

            case BindingExpr { Name: var bname }:
                // :name sigil — same read check
                var bst = GetState(bname);
                if (bst == LifecycleState.Spent)
                    Error($"LIFECYCLE ERROR: value '{bname}' is spent — cannot use in binding after consumption");
                break;

            case CallExpr c:
                // Check args in their current state before any transitions
                foreach (var a in c.Args) CheckExpr(a.Value);
                if (c.Callee is not MemberAccessExpr) CheckExpr(c.Callee);
                // std.mem.free(:ptr) transitions ptr to dead after the arg is validated
                if (TryQualName(c.Callee) == "std.mem.free" && c.Args.Count == 1)
                {
                    var arg = c.Args[0].Value;
                    string? fname = arg is BindingExpr b2 ? b2.Name
                                  : arg is IdentifierExpr id2 ? id2.Name
                                  : null;
                    if (fname != null) SetState(fname, LifecycleState.Dead);
                }
                break;

            case BinaryExpr b:
                CheckExpr(b.Left);
                CheckExpr(b.Right);
                break;

            case UnaryExpr u:
                CheckExpr(u.Operand);
                break;

            case PipelineExpr p:
                CheckExpr(p.Left);
                // Left value is consumed by the pipeline
                if (p.Left is IdentifierExpr { Name: var consumed })
                    SetState(consumed, LifecycleState.Spent);
                CheckExpr(p.Right);
                break;

            case TernaryExpr t:
                CheckExpr(t.Cond);
                CheckExpr(t.Then);
                CheckExpr(t.Else);
                break;

            case MemberAccessExpr m:
                CheckExpr(m.Object);
                break;

            case IndexExpr i:
                CheckExpr(i.Object);
                CheckExpr(i.Index);
                break;

            case GroupExpr g:
                CheckExpr(g.Inner);
                break;

            case CastExpr ce:
                CheckExpr(ce.Operand);
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────

    private void ConsumeAndCheck(string name)
    {
        var st = GetState(name);
        if (st == LifecycleState.Spent)
            Error($"LIFECYCLE ERROR: value '{name}' is already spent");
        else if (st == LifecycleState.Raw)
            Error($"LIFECYCLE ERROR: value '{name}' is raw — assign before consuming");
        SetState(name, LifecycleState.Spent);
    }

    private void Declare(string name, LifecycleState state)
    {
        if (_frames.Count > 0)
            _frames.Peek()[name] = state;
    }

    private LifecycleState GetState(string name)
    {
        foreach (var frame in _frames)
            if (frame.TryGetValue(name, out var s)) return s;
        return LifecycleState.Live; // unknown names assumed live (type checker already flagged them)
    }

    private void SetState(string name, LifecycleState state)
    {
        foreach (var frame in _frames)
        {
            if (frame.ContainsKey(name)) { frame[name] = state; return; }
        }
        // Not found — declare in current frame
        if (_frames.Count > 0) _frames.Peek()[name] = state;
    }

    private void Push() => _frames.Push(new Dictionary<string, LifecycleState>());
    private void Pop()  { if (_frames.Count > 0) _frames.Pop(); }

    private void Error(string msg) => _errors.Add(new SemanticError(msg));

    private static string? TryQualName(ExprNode e) => e switch
    {
        IdentifierExpr id  => id.Name,
        MemberAccessExpr m => TryQualName(m.Object) is string o ? $"{o}.{m.Member}" : null,
        _                  => null,
    };
}
