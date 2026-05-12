namespace mngc.AST;

public abstract record StmtNode;

// Program structure
public record ImportNode(string ModulePath, bool Wildcard);
public record EntryPointNode(string Name, BlockStmt Body);
public record ProgramNode(List<ImportNode> Imports, EntryPointNode EntryPoint, List<StmtNode> Declarations);

// Argument qualifier for function parameters
public enum ArgQualifier { None, Argx, Xarg, Argm, Xargm }

// Shared building blocks
public record ParamNode(string Name, TypeNode Type, ArgQualifier ArgQual = ArgQualifier.None, string? ArgTransform = null);
public record FieldNode(string Name, TypeNode Type);
public record ReturnSig(TypeNode Type, bool IsMapping);

// Declarations
public record FuncDeclNode(
    string Name,
    List<string> GenericParams,
    List<ParamNode> Params,
    ReturnSig? Return,
    BlockStmt Body
) : StmtNode;

public record OpDeclNode(
    string Name,
    List<string> OpParams,
    string ReturnType,
    BlockStmt Body
) : StmtNode;

public abstract record TypeBodyNode;
public record StructTypeBody(List<FieldNode> Fields) : TypeBodyNode;
public record TransformTypeBody(TypeNode From, TypeNode To) : TypeBodyNode;
public record CollectionTypeBody() : TypeBodyNode;

public record TypeDeclNode(
    string Name,
    List<string> GenericParams,
    TypeBodyNode? Body
) : StmtNode;

public enum Mutability { None, Const, Volatile, ConstVolatile, EConst, XConst }

public record VarDeclNode(
    Mutability Mutability,
    TypeNode Type,
    string Name,
    ExprNode? Initializer
) : StmtNode;

// Statements
public record BlockStmt(List<StmtNode> Stmts) : StmtNode;
public record AssignStmt(ExprNode Target, ExprNode Value) : StmtNode;
public record ReturnStmt(ExprNode Value) : StmtNode;
public record ExprStmt(ExprNode Expr) : StmtNode;
public record BreakStmt : StmtNode;
public record ContinueStmt : StmtNode;
public record RebindStmt(string Name, ExprNode Value) : StmtNode;
public record DerefBindStmt(string Name, ExprNode Source) : StmtNode;
public record ContainerStmt(string VarName, BlockStmt Body) : StmtNode;
public record PhasedStmt(string VarName, BlockStmt Body) : StmtNode;
public record DePhasedStmt(BlockStmt Body) : StmtNode;

public record ElseIf(ExprNode Cond, BlockStmt Body);
public record IfStmt(
    ExprNode Condition,
    BlockStmt Then,
    List<ElseIf> ElseIfs,
    BlockStmt? Else
) : StmtNode;

public record MatchArm(TypeNode? Pattern, BlockStmt Body); // null = wildcard _
public record MatchStmt(string Subject, List<MatchArm> Arms) : StmtNode;

// Five for loop forms
public record ForTypedStmt(string TypeName, string VarName, BlockStmt Body) : StmtNode;
public record ForMapStmt(string VarName, ExprNode Collection, BlockStmt Body) : StmtNode;
public record ForEachStmt(string VarName, ExprNode Collection, BlockStmt Body) : StmtNode;
public record ForMappedIterStmt(string VarName, ExprNode Expr, string CondOp, ExprNode CondVal, BlockStmt Body) : StmtNode;
public record ForIterStmt(string VarName, ExprNode Expr, string CondOp, ExprNode CondVal, BlockStmt Body) : StmtNode;
