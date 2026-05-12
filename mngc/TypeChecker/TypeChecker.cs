using mngc.AST;
using mngc.Emit;

namespace mngc.TypeChecker;

public class TypeChecker
{
    private readonly Scope _scope = new();
    private readonly List<SemanticError> _errors = new();

    private MgType _currentReturn = MgTypes.Void;
    private int    _loopDepth     = 0;
    private string _currentFunc   = "<program>";

    public bool HasErrors => _errors.Count > 0;
    public IReadOnlyList<SemanticError> Errors => _errors;

    // ── Namespace roots that are silently accepted as identifier prefixes ─────
    private static readonly HashSet<string> KnownNamespaces = new()
    {
        "sys", "std", "node", "lattice", "slice", "process", "mono", "mtx", "delta"
    };

    // ─────────────────────────────────────────────────────────────────────────
    // Entry
    // ─────────────────────────────────────────────────────────────────────────

    public void Check(ProgramNode program)
    {
        HoistDeclarations(program.Declarations);
        CheckEntryPoint(program.EntryPoint);
        foreach (var decl in program.Declarations)
            CheckStmt(decl);
    }

    // ── Pass 1: register all top-level names so forward calls work ───────────

    private void HoistDeclarations(List<StmtNode> decls)
    {
        foreach (var decl in decls)
        {
            switch (decl)
            {
                case FuncDeclNode f:
                    var pTypes  = f.Params.Select(p => ResolveTypeNode(p.Type)).ToList();
                    var retType = f.Return != null ? ResolveTypeNode(f.Return.Type) : MgTypes.Void;
                    DeclareOrError(new Symbol(f.Name, new MgFunction(pTypes, retType), SymbolKind.Function),
                                   $"function '{f.Name}' is already declared");
                    break;

                case OpDeclNode o:
                    var opParams = o.OpParams.Select(_ => (MgType)MgTypes.Unknown).ToList();
                    var opRet    = ResolveNamedType(o.ReturnType);
                    DeclareOrError(new Symbol(o.Name, new MgFunction(opParams, opRet), SymbolKind.Function),
                                   $"op '{o.Name}' is already declared");
                    break;

                case TypeDeclNode t:
                    DeclareOrError(new Symbol(t.Name, new MgStruct(t.Name), SymbolKind.TypeAlias),
                                   $"type '{t.Name}' is already declared");
                    break;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Statements
    // ─────────────────────────────────────────────────────────────────────────

    private void CheckEntryPoint(EntryPointNode entry)
    {
        var saved       = (_currentReturn, _currentFunc);
        _currentReturn  = MgTypes.Int;
        _currentFunc    = entry.Name;
        _scope.Push();
        CheckBlockBody(entry.Body);
        _scope.Pop();
        (_currentReturn, _currentFunc) = saved;
    }

    private void CheckStmt(StmtNode stmt)
    {
        switch (stmt)
        {
            case FuncDeclNode f:           CheckFuncDecl(f);      break;
            case OpDeclNode o:             CheckOpDecl(o);        break;
            case TypeDeclNode:             break;                  // already hoisted
            case VarDeclNode v:            CheckVarDecl(v);       break;
            case BlockStmt b:              CheckBlock(b);         break;
            case AssignStmt a:             CheckAssign(a);        break;
            case ReturnStmt r:             CheckReturn(r);        break;
            case ExprStmt e:               InferType(e.Expr);     break;
            case IfStmt i:                 CheckIf(i);            break;
            case MatchStmt:
                Error("match statement is not yet supported — requires type checker integration with emitter");
                break;
            case ForEachStmt fe:           CheckForEach(fe);      break;
            case ForMapStmt fm:            CheckForMap(fm);       break;
            case ForTypedStmt ft:          CheckForTyped(ft);     break;
            case ForIterStmt fi:           CheckForIter(fi);      break;
            case ForMappedIterStmt fmi:    CheckForMappedIter(fmi); break;
            case BreakStmt:
                if (_loopDepth == 0) Error("'break' used outside of a loop");
                break;
            case ContinueStmt:
                if (_loopDepth == 0) Error("'continue' used outside of a loop");
                break;
            case RebindStmt rb:    CheckRebind(rb);        break;
            case DerefBindStmt db: CheckDerefBind(db);     break;
            case ContainerStmt cs: CheckContainer(cs);     break;
            case PhasedStmt ps:    CheckPhased(ps);        break;
            case DePhasedStmt dp:  CheckDephased(dp);      break;
        }
    }

    // Push a new block scope, check all stmts, pop.
    private void CheckBlock(BlockStmt block)
    {
        _scope.Push();
        CheckBlockBody(block);
        _scope.Pop();
    }

    // Check stmts without pushing a scope (used when the caller already pushed one).
    private void CheckBlockBody(BlockStmt block)
    {
        foreach (var stmt in block.Stmts)
            CheckStmt(stmt);
    }

    private void CheckFuncDecl(FuncDeclNode f)
    {
        if (f.GenericParams.Count > 0)
        {
            Error($"function '{f.Name}': generic functions are not yet supported");
            return;
        }

        var retType = f.Return != null ? ResolveTypeNode(f.Return.Type) : MgTypes.Void;
        var saved   = (_currentReturn, _currentFunc);
        _currentReturn = retType;
        _currentFunc   = f.Name;

        _scope.Push();  // function scope — params live here
        foreach (var p in f.Params)
        {
            var pt = ResolveTypeNode(p.Type);
            if (!_scope.TryDeclare(new Symbol(p.Name, pt, SymbolKind.Parameter)))
                Error($"function '{f.Name}': duplicate parameter name '{p.Name}'");
        }
        CheckBlock(f.Body); // pushes another scope for the body
        _scope.Pop();

        (_currentReturn, _currentFunc) = saved;
    }

    private void CheckOpDecl(OpDeclNode o)
    {
        _scope.Push();
        foreach (var p in o.OpParams)
            _scope.TryDeclare(new Symbol(p, MgTypes.Unknown, SymbolKind.Parameter));
        CheckBlock(o.Body);
        _scope.Pop();
    }

    private void CheckVarDecl(VarDeclNode v)
    {
        var declared = ResolveTypeNode(v.Type);

        if (v.Initializer != null)
        {
            var init = InferType(v.Initializer);
            if (!IsCompatible(init, declared))
                Error($"cannot initialize '{v.Name}' (type '{FormatType(declared)}') with value of type '{FormatType(init)}'");
        }

        if (!_scope.TryDeclare(new Symbol(v.Name, declared, SymbolKind.Variable)))
            Error($"'{v.Name}' is already declared in this scope");
    }

    private void CheckAssign(AssignStmt a)
    {
        var target = InferType(a.Target);
        var value  = InferType(a.Value);
        if (!IsCompatible(value, target))
            Error($"cannot assign '{FormatType(value)}' to '{FormatType(target)}'");
    }

    private void CheckReturn(ReturnStmt r)
    {
        var value = InferType(r.Value);
        if (!IsCompatible(value, _currentReturn))
            Error($"'{_currentFunc}': cannot return '{FormatType(value)}' from function with return type '{FormatType(_currentReturn)}'");
    }

    private void CheckIf(IfStmt i)
    {
        InferType(i.Condition);
        CheckBlock(i.Then);
        foreach (var ei in i.ElseIfs) { InferType(ei.Cond); CheckBlock(ei.Body); }
        if (i.Else != null) CheckBlock(i.Else);
    }

    private void CheckRebind(RebindStmt r)
    {
        var sym = _scope.Resolve(r.Name);
        if (sym == null) { Error($"rebind: '{r.Name}' is not declared"); return; }
        if (sym.Kind == SymbolKind.Function) { Error($"rebind: '{r.Name}' is a function and cannot be rebound"); return; }
        var valueType = InferType(r.Value);
        if (!IsCompatible(valueType, sym.Type))
            Error($"rebind: cannot assign '{FormatType(valueType)}' to '{r.Name}' (type '{FormatType(sym.Type)}')");
    }

    private void CheckDerefBind(DerefBindStmt d)
    {
        var srcType = InferType(d.Source);
        var innerType = srcType is MgArray a ? a.Element : MgTypes.Unknown;
        if (!_scope.TryDeclare(new Symbol(d.Name, innerType, SymbolKind.Variable)))
            Error($"deref bind: '{d.Name}' is already declared in this scope");
    }

    private void CheckContainer(ContainerStmt cs)
    {
        _scope.Push();
        CheckBlockBody(cs.Body);
        _scope.Pop();
    }

    private void CheckPhased(PhasedStmt ps)
    {
        _scope.Push();
        CheckBlockBody(ps.Body);
        _scope.Pop();
    }

    private void CheckDephased(DePhasedStmt dp) => CheckBlock(dp.Body);

    // ── For loops ─────────────────────────────────────────────────────────────

    private void CheckForEach(ForEachStmt f)
    {
        var coll = InferType(f.Collection);
        if (coll is not (MgSlice or MgUnknown))
            Error($"'for :in' requires a slice<T> collection, got '{FormatType(coll)}'");

        _loopDepth++;
        _scope.Push();
        _scope.TryDeclare(new Symbol(f.VarName, MgTypes.Uint64, SymbolKind.Variable));
        CheckBlockBody(f.Body);
        _scope.Pop();
        _loopDepth--;
    }

    private void CheckForMap(ForMapStmt f)
    {
        var coll = InferType(f.Collection);
        if (coll is not (MgSlice or MgUnknown))
            Error($"'for -> :in' requires a slice<T> collection, got '{FormatType(coll)}'");

        _loopDepth++;
        _scope.Push();
        _scope.TryDeclare(new Symbol(f.VarName, MgTypes.Uint64, SymbolKind.Variable));
        CheckBlockBody(f.Body);
        _scope.Pop();
        _loopDepth--;
    }

    private void CheckForTyped(ForTypedStmt f)
    {
        if (_scope.Resolve(f.VarName) == null)
            Error($"'for -> type': variable '{f.VarName}' is not declared");

        var typeSym = _scope.Resolve(f.TypeName);
        if (typeSym == null || typeSym.Kind != SymbolKind.TypeAlias)
            Error($"'for -> type': '{f.TypeName}' is not a declared type");

        var innerType = typeSym?.Type ?? MgTypes.Unknown;

        _loopDepth++;
        _scope.Push();
        _scope.TryDeclare(new Symbol(f.VarName, innerType, SymbolKind.Variable));
        CheckBlockBody(f.Body);
        _scope.Pop();
        _loopDepth--;
    }

    private void CheckForIter(ForIterStmt f)
    {
        InferType(f.Expr);
        InferType(f.CondVal);

        _loopDepth++;
        _scope.Push();
        _scope.TryDeclare(new Symbol(f.VarName, MgTypes.Unknown, SymbolKind.Variable));
        CheckBlockBody(f.Body);
        _scope.Pop();
        _loopDepth--;
    }

    private void CheckForMappedIter(ForMappedIterStmt f)
    {
        InferType(f.Expr);
        InferType(f.CondVal);

        _loopDepth++;
        _scope.Push();
        _scope.TryDeclare(new Symbol(f.VarName, MgTypes.Unknown, SymbolKind.Variable));
        CheckBlockBody(f.Body);
        _scope.Pop();
        _loopDepth--;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Expression type inference
    // ─────────────────────────────────────────────────────────────────────────

    private MgType InferType(ExprNode expr) => expr switch
    {
        LiteralExpr l         => InferLiteral(l),
        IdentifierExpr id     => InferIdentifier(id),
        BindingExpr b         => InferBinding(b),
        GroupExpr g           => InferType(g.Inner),
        UnaryExpr u           => InferUnary(u),
        BinaryExpr b          => InferBinary(b),
        TernaryExpr t         => InferTernary(t),
        PipelineExpr p        => InferPipeline(p),
        MemberAccessExpr m    => InferMember(m),
        CallExpr c            => InferCall(c),
        IndexExpr i           => InferIndex(i),
        TransformChainExpr tc => InferType(tc.Object),
        CastExpr c            => ResolveTypeNode(c.TargetType),
        _                     => MgTypes.Unknown,
    };

    private static MgType InferLiteral(LiteralExpr l) => l.Kind switch
    {
        LiteralKind.Int    => MgTypes.Int,
        LiteralKind.Hex    => MgTypes.Int,
        LiteralKind.Float  => MgTypes.Float64,
        LiteralKind.Char   => MgTypes.Char,
        LiteralKind.String => MgTypes.String,
        LiteralKind.Bool   => MgTypes.Bool,
        _                  => MgTypes.Unknown,
    };

    private MgType InferIdentifier(IdentifierExpr id)
    {
        if (KnownNamespaces.Contains(id.Name)) return MgTypes.Unknown;
        var sym = _scope.Resolve(id.Name);
        if (sym == null) { Error($"undeclared identifier '{id.Name}'"); return MgTypes.Unknown; }
        return sym.Type;
    }

    private MgType InferBinding(BindingExpr b)
    {
        // :name — same as IdentifierExpr but with the sigil stripped
        if (KnownNamespaces.Contains(b.Name)) return MgTypes.Unknown;
        var sym = _scope.Resolve(b.Name);
        if (sym == null) { Error($"undeclared identifier ':{b.Name}'"); return MgTypes.Unknown; }
        return sym.Type;
    }

    private MgType InferUnary(UnaryExpr u)
    {
        var operand = InferType(u.Operand);
        return u.Op switch
        {
            "@" => new MgArray(operand),
            "~" => operand is MgArray a ? a.Element : MgTypes.Unknown,
            "!" => MgTypes.Bool,
            "-" => operand,
            _   => operand,
        };
    }

    private MgType InferBinary(BinaryExpr b)
    {
        var left  = InferType(b.Left);
        var right = InferType(b.Right);

        if (b.Op is "==" or "!=" or "<" or ">" or "<=" or ">=" or "&&" or "||")
            return MgTypes.Bool;

        if (IsNumeric(left) && IsNumeric(right))
            return WiderNumeric(left, right);

        return left is not MgUnknown ? left : right;
    }

    private MgType InferTernary(TernaryExpr t)
    {
        InferType(t.Cond);
        var then = InferType(t.Then);
        var els  = InferType(t.Else);
        return then is not MgUnknown ? then : els;
    }

    private MgType InferPipeline(PipelineExpr p)
    {
        InferType(p.Left);

        if (p.Right is CallExpr call)
        {
            var qual = TryQualifiedName(call.Callee);
            if (qual != null && StdlibMap.Entries.ContainsKey(qual))
            {
                foreach (var a in call.Args) InferType(a.Value);
                return StdlibReturnType(qual);
            }

            var calleeType = InferType(call.Callee);
            if (calleeType is MgFunction func)
            {
                int total = call.Args.Count + 1; // +1 for the piped left value
                if (total != func.Params.Count)
                    Error($"pipeline: function '{TryQualifiedName(call.Callee) ?? "?"}' expects {func.Params.Count} argument(s) but received {total} (including piped value)");
                foreach (var a in call.Args) InferType(a.Value);
                return func.Return;
            }

            foreach (var a in call.Args) InferType(a.Value);
            return MgTypes.Unknown;
        }

        if (p.Right is IdentifierExpr id)
        {
            var sym = _scope.Resolve(id.Name);
            if (sym?.Type is MgFunction f && f.Params.Count == 1) return f.Return;
            return MgTypes.Unknown;
        }

        InferType(p.Right);
        return MgTypes.Unknown;
    }

    private MgType InferMember(MemberAccessExpr m)
    {
        var qual = TryQualifiedName(m);
        if (qual != null)
        {
            if (StdlibMap.Entries.ContainsKey(qual)) return MgTypes.Unknown; // function reference
            var objQual = TryQualifiedName(m.Object);
            if (objQual != null && IsNamespacePath(objQual)) return MgTypes.Unknown;
        }

        InferType(m.Object);
        return MgTypes.Unknown; // struct field access — fields not tracked yet
    }

    private MgType InferCall(CallExpr c)
    {
        var qual = TryQualifiedName(c.Callee);

        if (qual != null && StdlibMap.Entries.ContainsKey(qual))
        {
            foreach (var a in c.Args) InferType(a.Value);
            return StdlibReturnType(qual);
        }

        var calleeType = InferType(c.Callee);

        if (calleeType is MgFunction func)
        {
            if (c.Args.Count != func.Params.Count)
                Error($"'{qual ?? "function"}' expects {func.Params.Count} argument(s) but got {c.Args.Count}");

            for (int i = 0; i < c.Args.Count; i++)
            {
                var argType = InferType(c.Args[i].Value);
                if (i < func.Params.Count && !IsCompatible(argType, func.Params[i]))
                    Error($"argument {i + 1} of '{qual ?? "function"}': cannot pass '{FormatType(argType)}' as '{FormatType(func.Params[i])}'");
            }
            return func.Return;
        }

        foreach (var a in c.Args) InferType(a.Value);
        return MgTypes.Unknown;
    }

    private MgType InferIndex(IndexExpr i)
    {
        var obj = InferType(i.Object);
        InferType(i.Index);
        return obj switch
        {
            MgArray a => a.Element,
            MgSlice s => s.Element,
            _         => MgTypes.Unknown,
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Type resolution
    // ─────────────────────────────────────────────────────────────────────────

    private MgType ResolveTypeNode(TypeNode type) => type switch
    {
        PrimitiveTypeNode p                                                   => ResolvePrimitive(p.Name),
        NamedTypeNode { Name: "slice",   GenericArgs: [var e] }               => new MgSlice(ResolveTypeNode(e)),
        NamedTypeNode { Name: "slice" }                                       => new MgSlice(MgTypes.Unknown),
        NamedTypeNode { Name: "node",    GenericArgs: [var i, var o] }        => new MgNode(ResolveTypeNode(i), ResolveTypeNode(o)),
        NamedTypeNode { Name: "node" }                                        => new MgNode(),
        NamedTypeNode { Name: "lattice" }                                     => new MgLattice(),
        NamedTypeNode { Name: "process" }                                     => new MgProcess(),
        NamedTypeNode { Name: "delta" }                                       => new MgStruct("delta"),
        NamedTypeNode n                                                       => ResolveNamedType(n.Name),
        ArrayTypeNode a                                                       => new MgArray(ResolveTypeNode(a.ElementType)),
        TransformTypeNode t                                                   => new MgFuncPtr(ResolveTypeNode(t.From), ResolveTypeNode(t.To)),
        _                                                                     => MgTypes.Unknown,
    };

    private static MgType ResolvePrimitive(string name) => name switch
    {
        "int"     => MgTypes.Int,
        "int8"    => MgTypes.Int8,
        "int16"   => MgTypes.Int16,
        "int32"   => MgTypes.Int32,
        "int64"   => MgTypes.Int64,
        "uint8"   => MgTypes.Uint8,
        "uint16"  => MgTypes.Uint16,
        "uint32"  => MgTypes.Uint32,
        "uint64"  => MgTypes.Uint64,
        "float"   => MgTypes.Float,
        "float32" => MgTypes.Float32,
        "float64" => MgTypes.Float64,
        "char"    => MgTypes.Char,
        "byte"    => MgTypes.Byte,
        "bool"    => MgTypes.Bool,
        "void"    => MgTypes.Void,
        _         => MgTypes.Unknown,
    };

    private MgType ResolveNamedType(string name)
    {
        var sym = _scope.Resolve(name);
        if (sym != null && sym.Kind == SymbolKind.TypeAlias) return sym.Type;
        // Unknown named type — could be from a library module; don't error.
        return new MgStruct(name);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Type compatibility
    // ─────────────────────────────────────────────────────────────────────────

    private static bool IsCompatible(MgType from, MgType to)
    {
        if (from is MgUnknown || to is MgUnknown) return true;
        if (from == to) return true;
        if (IsNumeric(from) && IsNumeric(to)) return true;
        // string literal is compatible with char* / char[]
        if (from is MgString && to is MgArray { Element: MgPrimitive { Name: "char" } }) return true;
        if (from is MgString && to is MgPrimitive { Name: "char" }) return true;
        // void* is compatible with any pointer
        if (to is MgArray { Element: MgPrimitive { Name: "void" } }) return true;
        if (from is MgArray { Element: MgPrimitive { Name: "void" } }) return true;
        // same struct name
        if (from is MgStruct sa && to is MgStruct sb && sa.Name == sb.Name) return true;
        return false;
    }

    private static bool IsNumeric(MgType t) => t is MgPrimitive p && p.Name is
        "int" or "int8" or "int16" or "int32" or "int64" or
        "uint8" or "uint16" or "uint32" or "uint64" or
        "float" or "float32" or "float64" or "byte";

    private static MgType WiderNumeric(MgType a, MgType b)
    {
        if (a is MgPrimitive pa && b is MgPrimitive pb)
        {
            if (pa.Name.StartsWith("float") || pb.Name.StartsWith("float"))
                return pa.Name == "float64" || pb.Name == "float64" ? MgTypes.Float64 : MgTypes.Float32;
        }
        return a;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Stdlib return type table
    // ─────────────────────────────────────────────────────────────────────────

    private static MgType StdlibReturnType(string name) => name switch
    {
        // std.time
        "std.time.now"       => MgTypes.Int64,
        "std.time.clock"     => MgTypes.Int64,
        "std.time.diff"      => MgTypes.Float64,
        "std.time.sleep"     => MgTypes.Void,
        // std.env
        "std.env.get"        => new MgArray(MgTypes.Char),
        // std.sync
        "std.sync.mutex"     => new MgArray(MgTypes.Void),
        "std.sync.lock"      => MgTypes.Void,
        "std.sync.unlock"    => MgTypes.Void,
        "std.sync.mutex_free" => MgTypes.Void,
        // std.fs
        "std.fs.rename"      => MgTypes.Int,
        "std.fs.remove"      => MgTypes.Int,
        "std.fs.exists"      => MgTypes.Int,
        // std.proc
        "std.proc.spawn"     => MgTypes.Int,
        "std.proc.pid"       => MgTypes.Int,
        // std.delta
        "std.delta.d2"       => new MgStruct("delta"),
        "std.delta.d3"       => new MgStruct("delta"),
        "std.delta.mag"      => MgTypes.Float64,
        "std.delta.dx"       => MgTypes.Float64,
        "std.delta.dy"       => MgTypes.Float64,
        "std.delta.dz"       => MgTypes.Float64,
        // process.thread
        "process.thread"     => MgTypes.Void,
        "process.join"       => MgTypes.Void,
        // sys
        "sys.exit"           => MgTypes.Void,
        "std.mem.alloc"      => new MgArray(MgTypes.Void),
        "std.mem.calloc"     => new MgArray(MgTypes.Void),
        "std.mem.realloc"    => new MgArray(MgTypes.Void),
        "std.str.len"        => MgTypes.Int,
        "std.str.cmp"        => MgTypes.Int,
        "std.str.chr"        => new MgArray(MgTypes.Char),
        "std.math.sqrt"      => MgTypes.Float64,
        "std.math.pow"       => MgTypes.Float64,
        "std.math.abs"       => MgTypes.Float64,
        "std.math.floor"     => MgTypes.Float64,
        "std.math.ceil"      => MgTypes.Float64,
        "std.math.sin"       => MgTypes.Float64,
        "std.math.cos"       => MgTypes.Float64,
        "std.math.tan"       => MgTypes.Float64,
        "std.math.log"       => MgTypes.Float64,
        "std.io.open"        => new MgArray(MgTypes.Void),
        "std.io.close"       => MgTypes.Int,
        "std.io.read"        => new MgArray(MgTypes.Char),
        "std.io.scanf"       => MgTypes.Int,
        "std.io.flush"       => MgTypes.Int,
        "slice.len"          => MgTypes.Int,
        "slice.get"          => MgTypes.Uint64,
        "slice.new"          => new MgSlice(MgTypes.Unknown),
        "node.new"           => new MgNode(),
        "node.get"           => new MgArray(MgTypes.Void),
        "node.next"          => new MgNode(),
        "node.prev"          => new MgNode(),
        "node.transform"     => new MgNode(MgTypes.Unknown, MgTypes.Unknown),
        "lattice.new"        => new MgLattice(),
        "lattice.new_transform" => new MgLattice(),
        "lattice.rows"       => MgTypes.Int,
        "lattice.cols"       => MgTypes.Int,
        "lattice.get"        => new MgArray(MgTypes.Void),
        "process.new"        => new MgProcess(),
        "process.get"        => MgTypes.Byte,
        "process.len"        => MgTypes.Int,
        "process.cap"        => MgTypes.Int,
        _                    => MgTypes.Unknown,
    };

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private void Error(string msg) => _errors.Add(new SemanticError(msg));

    private void DeclareOrError(Symbol sym, string msg)
    {
        if (!_scope.TryDeclare(sym)) Error(msg);
    }

    // Flattens a member-access / identifier chain to a dotted string.
    private static string? TryQualifiedName(ExprNode expr) => expr switch
    {
        IdentifierExpr id  => id.Name,
        MemberAccessExpr m => TryQualifiedName(m.Object) is string obj ? $"{obj}.{m.Member}" : null,
        _                  => null,
    };

    private static bool IsNamespacePath(string path) =>
        KnownNamespaces.Contains(path.Split('.')[0]);

    private static string FormatType(MgType t) => t switch
    {
        MgPrimitive p                      => p.Name,
        MgArray a                          => $"{FormatType(a.Element)}[]",
        MgSlice s                          => $"slice<{FormatType(s.Element)}>",
        MgNode { In: null }                => "node",
        MgNode n                           => $"node<{FormatType(n.In!)}, {FormatType(n.Out!)}>",
        MgLattice                          => "lattice",
        MgProcess                          => "process",
        MgString                           => "char*",
        MgFuncPtr f                        => $"({FormatType(f.From)} -> {FormatType(f.To)})",
        MgFunction f                       => $"func({string.Join(", ", f.Params.Select(FormatType))}) => {FormatType(f.Return)}",
        MgStruct s                         => s.Name,
        MgUnknown                          => "?",
        _                                  => "?",
    };
}
