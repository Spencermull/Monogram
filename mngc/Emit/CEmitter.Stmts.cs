using mngc.AST;

namespace mngc.Emit;

public partial class CEmitter
{
    private void EmitEntryPoint(EntryPointNode entry)
    {
        throw new NotImplementedException();
    }

    private void EmitStmt(StmtNode stmt)
    {
        throw new NotImplementedException();
    }

    private void EmitBlock(BlockStmt block)
    {
        throw new NotImplementedException();
    }
}

// Expression stubs live here until CEmitter.Exprs.cs is written
public partial class CEmitter
{
    private string EmitExpr(ExprNode expr)
    {
        throw new NotImplementedException();
    }
}
