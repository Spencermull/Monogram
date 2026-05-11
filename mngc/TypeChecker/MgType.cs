namespace mngc.TypeChecker;

public abstract record MgType;

public record MgPrimitive(string Name)  : MgType;
public record MgArray(MgType Element)   : MgType;
public record MgSlice(MgType Element)   : MgType;
public record MgNode(MgType? In = null, MgType? Out = null) : MgType;
public record MgLattice                 : MgType;
public record MgProcess                 : MgType;
public record MgString                  : MgType;   // string literal / char*
public record MgFuncPtr(MgType From, MgType To)              : MgType;
public record MgFunction(List<MgType> Params, MgType Return) : MgType;
public record MgStruct(string Name)     : MgType;
public record MgUnknown                 : MgType;   // unresolved — suppresses cascading errors

public static class MgTypes
{
    public static readonly MgPrimitive Int     = new("int");
    public static readonly MgPrimitive Int8    = new("int8");
    public static readonly MgPrimitive Int16   = new("int16");
    public static readonly MgPrimitive Int32   = new("int32");
    public static readonly MgPrimitive Int64   = new("int64");
    public static readonly MgPrimitive Uint8   = new("uint8");
    public static readonly MgPrimitive Uint16  = new("uint16");
    public static readonly MgPrimitive Uint32  = new("uint32");
    public static readonly MgPrimitive Uint64  = new("uint64");
    public static readonly MgPrimitive Float   = new("float");
    public static readonly MgPrimitive Float32 = new("float32");
    public static readonly MgPrimitive Float64 = new("float64");
    public static readonly MgPrimitive Char    = new("char");
    public static readonly MgPrimitive Byte    = new("byte");
    public static readonly MgPrimitive Bool    = new("bool");
    public static readonly MgPrimitive Void    = new("void");
    public static readonly MgUnknown   Unknown = new();
    public static readonly MgString    String  = new();
}
