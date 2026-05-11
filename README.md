# Monogram

A compiled, statically typed programming language that transpiles to C.

**File extension:** `.mngrm` — **Compiler:** `mngc`

## Example

```monogram
#import<std.io>
#import<slice>

func: greet(:name char[]) {
    sys.stdout(:'Hello, %s!\n', :name);
}

init void main() {
    greet(:'Monogram');
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
dotnet run -- build <file.mngrm>            # compile to binary
dotnet run -- build <file.mngrm> -o out.exe # specify output path
dotnet run -- build <file.mngrm> --keep-c   # keep intermediate .c file
dotnet run -- run   <file.mngrm>            # compile and run
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

Return styles: `=>` regular return, `->` mapping/transform return.

### Variables
```monogram
int x = 42;
const float pi = 3.14;
volatile int flag = 0;
const volatile int limit = 100;
```

### Types

**Primitives**
`int` `int8` `int16` `int32` `int64` `uint8` `uint16` `uint32` `uint64`
`float` `float32` `float64` `char` `byte` `bool` `void`

**Type declarations**
```monogram
type Point { :x float, :y float }         // struct
type Transform (int -> int)               // function pointer
type Items []                             // collection
```

**Generic types**
```monogram
node<int, float>    // transform node
slice<int>          // length-tracked array
```

**Arrays**
```monogram
int[] buf = std.mem.alloc(:256);
```

**Type casting**
```monogram
int n = someVal as int;
node<int, float> typed = rawNode as node<int, float>;
```

### Operators

**Arithmetic:** `+` `-` `*` `/` `%`
**Bitwise:** `&` `|` `^` `<<` `>>`
**Logical:** `&&` `||` `!`
**Comparison:** `==` `!=` `<` `>` `<=` `>=`
**Memory:** `@` address-of, `~` dereference
**Pipeline:** `->` transform/pipe, `=>` return
**Ternary:** `cond ? a : b`

### Pipeline operator
```monogram
// a -> b        calls b(a)
// a -> b(:x)    calls b(a, x)  — left is prepended as first arg

func: double(:n int) => int { => n * 2; }
func: inc(:n int)    => int { => n + 1; }

int result = 5 -> double -> inc;   // 11
```

### Control flow

**If / else**
```monogram
if (x > 0) {
    sys.stdout(:'positive\n');
} else if (x < 0) {
    sys.stdout(:'negative\n');
} else {
    sys.stdout(:'zero\n');
}
```

**Match**
```monogram
match: val {
    int   => { sys.stdout(:'integer\n'); }
    float => { sys.stdout(:'float\n');   }
    _     => { sys.stdout(:'other\n');   }
}
```

### Loops

Five loop forms:

```monogram
// Sequential foreach over a slice
for :v in items { }

// Mapped (parallel-intent) foreach
for -> :v in items { }

// Sequential iteration with condition
for :i in counter >= 1; { }

// Mapped iteration with condition
for :i -> counter >= 1; { }

// Typed pointer iteration
for -> type Node: ptr { }
```

`for :v in slice` and `for -> :v in slice` require a `slice<T>` collection.
The bound variable `v` is `uintptr_t` — cast with `as` for typed access.

### Custom operators
```monogram
op: add_vec(a, b) => int {
    => a + b;
}
```

### Memory
```monogram
int[] buf  = std.mem.alloc(:256);
int[] buf2 = std.mem.calloc(:64, :4);
std.mem.free(:buf);

int x     = 10;
int[] ptr = @x;     // address-of
int val   = ~ptr;   // dereference
```

### Imports
```monogram
#import<std.io>
#import<std.mem>
#import<std.str>
#import<std.math>
#import<node>
#import<lattice>
#import<process>
#import<slice>
```

---

## Standard library

### sys
| Call | Description |
|---|---|
| `sys.stdout(:'fmt', args)` | printf to stdout |
| `sys.stderr(:'fmt', args)` | printf to stderr |
| `sys.exit(:code)` | terminate process |

### std.mem
| Call | Description |
|---|---|
| `std.mem.alloc(:size)` | malloc |
| `std.mem.calloc(:n, :size)` | calloc |
| `std.mem.realloc(:ptr, :size)` | realloc |
| `std.mem.free(:ptr)` | free |

### std.str
| Call | Description |
|---|---|
| `std.str.len(:s)` | strlen |
| `std.str.copy(:dst, :src)` | strcpy |
| `std.str.cat(:dst, :src)` | strcat |
| `std.str.cmp(:a, :b)` | strcmp |
| `std.str.chr(:s, :c)` | strchr |
| `std.str.fmt(:buf, :'fmt', args)` | sprintf |

### std.math
| Call | Description |
|---|---|
| `std.math.sqrt(:x)` | sqrt |
| `std.math.pow(:base, :exp)` | pow |
| `std.math.abs(:x)` | fabs |
| `std.math.floor(:x)` | floor |
| `std.math.ceil(:x)` | ceil |
| `std.math.sin(:x)` | sin |
| `std.math.cos(:x)` | cos |
| `std.math.tan(:x)` | tan |
| `std.math.log(:x)` | log |

### std.io
| Call | Description |
|---|---|
| `std.io.open(:'path', :'mode')` | fopen |
| `std.io.close(:file)` | fclose |
| `std.io.read(:buf, :n, :file)` | fgets |
| `std.io.write(:buf, :file)` | fputs |
| `std.io.flush(:file)` | fflush |
| `std.io.scanf(:'fmt', args)` | scanf |

### node — graph node
```monogram
node n = node.new(:value);
node.link(:a, :b);          // a.next = b, b.prev = a
node.get(:n)                // => void*
node.set(:n, :value);
node.next(:n)               // => node
node.prev(:n)               // => node
node<int, float> t = node.transform(:n, :fn);
node.free(:n);
```

### lattice — 2D grid
```monogram
lattice l = lattice.new(:rows, :cols);
lattice.set(:l, :r, :c, :value);
lattice.get(:l, :r, :c)     // => void*
lattice.apply(:l, :r, :c)   // apply transform fn at cell
lattice.rows(:l)             // => int
lattice.cols(:l)             // => int
lattice.free(:l);
```

### slice — length-tracked array
```monogram
slice<int> s = slice.new(:n);
slice.set(:s, :i, :value);
slice.get(:s, :i)           // => uintptr_t
slice.len(:s)               // => int
slice.free(:s);

for :v in s { }             // iterate; v is uintptr_t
```

### process — byte buffer
```monogram
process p = process.new(:capacity);
process.set(:p, :i, :byte);
process.get(:p, :i)                      // => byte
process.write(:p, :offset, :src, :n);
process.read(:p, :offset, :dst, :n);
process.len(:p)                          // => int
process.cap(:p)                          // => int
process.free(:p);
```

---

## Status

v0.1.0 — lexer, parser, C emitter, GCC driver, LSP server, VS Code extension.
