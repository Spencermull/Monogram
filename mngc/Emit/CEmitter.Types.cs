using mngc.AST;

namespace mngc.Emit;

public partial class CEmitter
{
    private string EmitTypeExpr(TypeNode type) => type switch
    {
        PrimitiveTypeNode p                                                  => MapPrimitive(p.Name),
        // stdlib types — pull in module header, erase generic params to void* in C
        NamedTypeNode { Name: "node", GenericArgs: { Count: >= 2 } }        => TrackModule("node", "mgnode_xform_t*"),
        NamedTypeNode { Name: "node" }                                       => TrackModule("node", "mgnode_t*"),
        NamedTypeNode { Name: "lattice" }                                    => TrackModule("lattice",   "mglattice_t*"),
        NamedTypeNode { Name: "slice" }                                      => TrackModule("slice",     "mgslice_t*"),
        ArrayTypeNode { ElementType: NamedTypeNode { Name: "process" } }     => TrackModule("process",   "mgprocess_t*"),
        NamedTypeNode { Name: "delta" }                                      => TrackModule("std.delta", "mgdelta_t"),
        // general
        NamedTypeNode n      => n.GenericArgs.Count == 0
                                    ? n.Name
                                    : $"{n.Name}/* <{string.Join(", ", n.GenericArgs.Select(EmitTypeExpr))}> */",
        ArrayTypeNode a      => $"{EmitTypeExpr(a.ElementType)}*",
        TransformTypeNode t  => $"{EmitTypeExpr(t.To)} (*)({EmitTypeExpr(t.From)})",
        _                    => "/* unknown type */ void*",
    };

    private string TrackModule(string module, string ctype)
    {
        _requiredHeaders.Add(module);
        return ctype;
    }

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
        Mutability.EConst        => "extern const ",
        Mutability.XConst        => "static const ",
        _                        => "",
    };
}
