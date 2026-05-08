namespace mngc.Lexer;

public enum TokenType
{
    // Literals
    IntLit,
    HexLit,
    FloatLit,
    CharLit,
    StringLit,

    // Identifiers & keywords
    Identifier,
    Import,
    Init,
    Func,
    Op,
    Type,
    Const,
    Volatile,
    Transform,
    If,
    Else,
    Match,
    For,
    In,
    As,
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
    Caret,     // ^  (bitwise XOR)
    Ampersand, // &  (bitwise AND)
    Pipe,      // |  (bitwise OR)
    Question,  // ?

    // Meta
    Invalid,
    Eof,
}
