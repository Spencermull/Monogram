using mngc.AST;

namespace mngc.Emit;

public partial class CEmitter
{
    private string EmitExpr(ExprNode expr) => expr switch
    {
        LiteralExpr l         => EmitLiteral(l),
        IdentifierExpr id     => id.Name,
        BindingExpr b         => b.Name,                        // strip sigil
        GroupExpr g           => $"({EmitExpr(g.Inner)})",
        UnaryExpr u           => $"{MapUnaryOp(u.Op)}{EmitExpr(u.Operand)}",
        BinaryExpr b          => $"{EmitExpr(b.Left)} {b.Op} {EmitExpr(b.Right)}",
        TernaryExpr t         => $"{EmitExpr(t.Cond)} ? {EmitExpr(t.Then)} : {EmitExpr(t.Else)}",
        MemberAccessExpr m    => $"{EmitExpr(m.Object)}.{m.Member}",
        IndexExpr i           => $"{EmitExpr(i.Object)}[{EmitExpr(i.Index)}]",
        TransformChainExpr tc => $"{EmitExpr(tc.Object)}",     // .transform stripped
        CallExpr c            => EmitCall(c),
        PipelineExpr p        => EmitPipeline(p),
        _                     => $"/* unknown expr: {expr.GetType().Name} */",
    };

    private static string EmitLiteral(LiteralExpr l) => l.Kind switch
    {
        LiteralKind.Bool   => l.Value == "true" ? "true" : "false",
        LiteralKind.Char   => l.Value,   // already 'x' from lexer
        LiteralKind.String => $"\"{l.Value[1..^1]}\"",  // swap ' for "
        _                  => l.Value,
    };

    private static string MapUnaryOp(string op) => op switch
    {
        "@" => "&",   // address-of
        "~" => "*",   // dereference
        _   => op,    // !, -  map 1:1
    };

    private string EmitCall(CallExpr c)
    {
        var args = c.Args.Select(a => EmitExpr(a.Value)).ToList();

        var qualName = TryGetQualifiedName(c.Callee);
        if (qualName != null && StdlibMap.Entries.TryGetValue(qualName, out var entry))
        {
            _requiredHeaders.Add(entry.Header);
            return entry.Emit(args);
        }

        return $"{EmitExpr(c.Callee)}({string.Join(", ", args)})";
    }

    // Flatten a member-access chain to a dotted string: sys.stdout, std.mem.alloc, etc.
    private static string? TryGetQualifiedName(ExprNode expr) => expr switch
    {
        IdentifierExpr id => id.Name,
        MemberAccessExpr m => TryGetQualifiedName(m.Object) is string obj ? $"{obj}.{m.Member}" : null,
        _ => null,
    };

    // a -> b        becomes  b(a)
    // a -> b(x, y)  becomes  b(a, x, y)  (left is prepended as first arg)
    private string EmitPipeline(PipelineExpr p)
    {
        var left = EmitExpr(p.Left);

        if (p.Right is CallExpr c)
        {
            var allArgs = new[] { left }
                .Concat(c.Args.Select(a => EmitExpr(a.Value)))
                .ToList();

            var qualName = TryGetQualifiedName(c.Callee);
            if (qualName != null && StdlibMap.Entries.TryGetValue(qualName, out var entry))
            {
                _requiredHeaders.Add(entry.Header);
                return entry.Emit(allArgs);
            }

            return $"{EmitExpr(c.Callee)}({string.Join(", ", allArgs)})";
        }

        if (p.Right is IdentifierExpr id)
            return $"{id.Name}({left})";

        return $"/* pipeline */ {EmitExpr(p.Right)}({left})";
    }
}
