using mngc.AST;

namespace mngc.Emit;

public partial class CEmitter
{
    private string EmitTypeExpr(TypeNode type) => type switch
    {
        PrimitiveTypeNode p  => MapPrimitive(p.Name),
        NamedTypeNode n      => n.GenericArgs.Count == 0
                                    ? n.Name
                                    : $"{n.Name}/* <{string.Join(", ", n.GenericArgs.Select(EmitTypeExpr))}> */",
        ArrayTypeNode a      => $"{EmitTypeExpr(a.ElementType)}*",
        TransformTypeNode t  => $"{EmitTypeExpr(t.To)} (*)({EmitTypeExpr(t.From)})",
        _                    => "/* unknown type */ void*",
    };

    private static string MapPrimitive(string name) => name switch
    {
        "int8"    => "int8_t",
        "int16"   => "int16_t",
        "int32"   => "int32_t",
        "int64"   => "int64_t",
        "uint8"   => "uint8_t",
        "uint16"  => "uint16_t",
        "uint32"  => "uint32_t",
        "uint64"  => "uint64_t",
        "int"     => "int",
        "float32" => "float",
        "float64" => "double",
        "float"   => "float",
        "char"    => "char",
        "byte"    => "unsigned char",
        "bool"    => "bool",
        "void"    => "void",
        _         => name,
    };

    private static string MapMutability(Mutability mut) => mut switch
    {
        Mutability.Const         => "const ",
        Mutability.Volatile      => "volatile ",
        Mutability.ConstVolatile => "const volatile ",
        _                        => "",
    };
}
