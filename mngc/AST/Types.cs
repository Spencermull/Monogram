namespace mngc.AST;

public abstract record TypeNode;

public record PrimitiveTypeNode(string Name) : TypeNode;
public record NamedTypeNode(string Name, List<TypeNode> GenericArgs) : TypeNode;
public record ArrayTypeNode(TypeNode ElementType) : TypeNode;
public record TransformTypeNode(TypeNode From, TypeNode To) : TypeNode;
