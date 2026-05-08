# Monogram

A compiled, statically typed programming language that transpiles to C.

**File extension:** `.mngrm` — **Compiler:** `mngc`

## Example

```monogram
#import<std.io>

init void main() {
    sys.stdout(:'Hello from Monogram!\n');
}
```

## Building the compiler

Requires [.NET 9](https://dotnet.microsoft.com/download) and [GCC](https://gcc.gnu.org/) on PATH.

```bash
cd mngc
dotnet build
```

## Usage

```bash
dotnet run -- <file.mngrm>            # compile to binary
dotnet run -- <file.mngrm> -o out.exe # specify output path
dotnet run -- <file.mngrm> --keep-c   # keep intermediate .c file
```

## Language overview

### Entry point
```monogram
init void main() { }
```

### Functions
```monogram
func: add(:a int, :b int) => int {
    => a + b;
}
```

### Types
`int` `int8/16/32/64` `uint8/16/32/64` `float` `float32/64` `char` `byte` `bool` `void`

### Standard library
| Monogram | C |
|---|---|
| `sys.stdout(:'fmt', args)` | `printf` |
| `sys.stderr(:'fmt', args)` | `fprintf(stderr, ...)` |
| `sys.exit(:code)` | `exit` |
| `std.mem.alloc(:size)` | `malloc` |
| `std.str.len(:s)` | `strlen` |
| `std.math.sqrt(:x)` | `sqrt` |

## Status

v0.1 — lexer, parser, C emitter, and GCC driver are implemented.
