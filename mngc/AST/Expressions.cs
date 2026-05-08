namespace mngc.AST;

public abstract record ExprNode;

public record LiteralExpr(string Value, LiteralKind Kind) : ExprNode;
public record IdentifierExpr(string Name) : ExprNode;
public record BindingExpr(string Name) : ExprNode;          // :name sigil
public record GroupExpr(ExprNode Inner) : ExprNode;
public record UnaryExpr(string Op, ExprNode Operand) : ExprNode;
public record BinaryExpr(ExprNode Left, string Op, ExprNode Right) : ExprNode;
public record TernaryExpr(ExprNode Cond, ExprNode Then, ExprNode Else) : ExprNode;
public record PipelineExpr(ExprNode Left, ExprNode Right) : ExprNode;
public record MemberAccessExpr(ExprNode Object, string Member) : ExprNode;
public record CallExpr(ExprNode Callee, List<Arg> Args) : ExprNode;
public record IndexExpr(ExprNode Object, ExprNode Index) : ExprNode;
public record TransformChainExpr(ExprNode Object) : ExprNode;
public record CastExpr(TypeNode TargetType, ExprNode Operand) : ExprNode;

public record Arg(bool IsSigil, ExprNode Value);

public enum LiteralKind { Int, Hex, Float, Char, String, Bool }
