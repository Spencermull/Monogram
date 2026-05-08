namespace mngc.Lexer;

public enum TokenType
{
    // Literals
    IntLit,
    HexLit,
    FloatLit,
    CharLit,
    StringLit,
    BoolLit,

    // Identifiers & keywords
    Identifier,
    Import,
    Init,
    Func,
    Op,
    Type,
    Const,
    Volatile,
    If,
    Else,
    Match,
    For,
    In,
    True,
    False,
    Void,

    // Primitive types
    Int8, Int16, Int32, Int64,
    Uint8, Uint16, Uint32, Uint64,
    Int,
    Float32, Float64,
    Float,
    Char,
    Byte,
    Bool,

    // Symbols
    LParen,    // (
    RParen,    // )
    LBrace,    // {
    RBrace,    // }
    LAngle,    // <
    RAngle,    // >
    LBracket,  // [
    RBracket,  // ]
    Comma,     // ,
    Semicolon, // ;
    Dot,       // .
    Colon,     // :
    Assign,    // =
    Bang,      // !
    At,        // @
    Tilde,     // ~

    // Operators
    Plus,      // +
    Minus,     // -
    Star,      // *
    Slash,     // /
    Percent,   // %
    Arrow,     // ->
    FatArrow,  // =>
    And,       // &&
    Or,        // ||
    Eq,        // ==
    Neq,       // !=
    Lte,       // <=
    Gte,       // >=
    Shl,       // <<
    Shr,       // >>
    Question,  // ?

    // Meta
    Eof,
}
