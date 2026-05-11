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

---

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

> **Constraint:** Generic function declarations (`func: name<T>(...)`) are parsed but not yet supported by the emitter — the compiler will throw at compile time. Generic built-in types (`slice<T>`, `node<T, U>`) are supported.

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

**Built-in generic types**
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

**Match** *(not yet implemented — requires type checker)*
```monogram
match: val {
    int   => { sys.stdout(:'integer\n'); }
    float => { sys.stdout(:'float\n');   }
    _     => { sys.stdout(:'other\n');   }
}
```
> The compiler rejects `match` at compile time until a type checker is implemented.

**Break / Continue**
```monogram
for :i in counter >= 1; {
    if (i == 5) { break; }
    if (i == 3) { continue; }
    sys.stdout(:'%d\n', :i);
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

`for :v in slice` and `for -> :v in slice` require a `slice<T>` collection — the element type is not verified by the compiler at this stage.
The iteration variable `v` is `uintptr_t` — cast with `as` for typed access.

For iter loops (`for :i in expr cond;`) declare the variable before the loop. Mutate it in the body to advance — the increment slot is empty by design.

### Custom operators
```monogram
op: add_vec(a, b) => int {
    => a + b;
}
```

> `op:` parameters are untyped by design — all params are emitted as `void*` in C.

### Visibility-qualified constants

```monogram
econst int MAX_CONNECTIONS = 100;   // extern const — readable across modules
xconst int INTERNAL_KEY = 0xFF34;   // static const — private to this file
```

| Qualifier | C emission | Visibility |
|---|---|---|
| `const` | `const` | module-local |
| `econst` | `extern const` | readable outside module, never writable |
| `xconst` | `static const` | compiler error if accessed outside file |

### rebinds

Reassign a binding without redeclaring it.

```monogram
int x = 5;
rebind x = 10;
```

Emits a plain C assignment. The type checker verifies the right-hand side is compatible with the declared type.

### deref bind

Bind a name to a dereferenced memory location.

```monogram
int val = 42;
int[] ptr = @val;
deref bind :ref = ~ptr;   // ref == 42
```

Emits `type name = *source;`.

### Argument qualifiers (argx)

Optional call-site qualifiers on function parameters that control coercion and transformation.

```monogram
func: scale(:v Vec2, argx :factor float32) => Vec2 { }
scale(:a, :2);   // int auto-cast to float32 at call site

func: process(argm :v Vec2 -> normalize) => float32 { }
process(:a);     // a is normalized before reaching the body

func: write(xarg :data byte[]) { }   // exact type required — no coercion
```

| Qualifier | Behaviour |
|---|---|
| `argx` | auto-coerce to declared type at call site |
| `xarg` | exact type required — no casting |
| `argm` | transform argument through a named function before entering body |
| `xargm` | strict mapped — both input and output types enforced |

### Concurrency blocks

```monogram
#import<mono.phase>

phased :stage_one {
    process.thread(:task_a);
    process.thread(:task_b);
}
// all threads in stage_one finish before execution continues

dephased {
    process.thread(:logger);   // fire and forget — no sync
}

container :workers {
    process.thread(:task_a);
    process.thread(:task_b);
}
// all threads joined when workers exits scope
```

Requires `#import<mono.phase>`. Emits POSIX pthreads.

### Lifecycle buffer

Always active — zero import, zero runtime overhead. The compiler tracks four states per binding.

| State | Meaning |
|---|---|
| `raw` | allocated, not yet initialised |
| `live` | initialised and valid |
| `spent` | consumed — cannot be read |
| `dead` | freed or out of scope |

```monogram
int x;             // raw
x = 5;             // live
=> x;              // x is now spent
sys.stdout(:x);    // LIFECYCLE ERROR — x is spent
```

The `~>` transfer operator moves lifecycle ownership:

```monogram
Vec2 a = make_vec(:3.0, :4.0);
Vec2 b ~> a;      // b is live, a is spent
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

Import a specific module:
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

Import all core std headers at once:
```monogram
#import<std.*>
```
Expands to `stdio.h`, `stdlib.h`, `string.h`, and `math.h`.

---

## Library reference

Monogram's libraries are organized into three tiers. Each tier has a defined scope — lower tiers have no dependency on higher ones.

### Tier overview

| Tier | Prefix | Role |
|---|---|---|
| Standard Library | `std` | Thin wrappers over C stdlib. Ships with the compiler. |
| Systems Extensions | `mono` | Official systems-focused packages. Maintained by Monogram. |
| Developer Tooling | `mtx` | Higher-level tooling and dev experience. Built on top of mono. |

Inline data structures (`node`, `lattice`, `slice`, `process`) are bundled with the compiler and do not follow the tier prefix convention.

---

### std — Standard Library

Ships with the compiler. All `std.*` modules map to C stdlib functionality with no external dependencies.

#### sys *(built-in, no import)*
| Call | Description |
|---|---|
| `sys.stdout(:'fmt', args)` | printf to stdout |
| `sys.stderr(:'fmt', args)` | printf to stderr |
| `sys.exit(:code)` | terminate process |

#### std.mem
| Call | Description |
|---|---|
| `std.mem.alloc(:size)` | malloc |
| `std.mem.calloc(:n, :size)` | calloc |
| `std.mem.realloc(:ptr, :size)` | realloc |
| `std.mem.free(:ptr)` | free |

#### std.str
| Call | Description |
|---|---|
| `std.str.len(:s)` | strlen |
| `std.str.copy(:dst, :src)` | strcpy |
| `std.str.cat(:dst, :src)` | strcat |
| `std.str.cmp(:a, :b)` | strcmp |
| `std.str.chr(:s, :c)` | strchr — first occurrence of char in string |
| `std.str.fmt(:buf, :'fmt', args)` | sprintf |

#### std.math
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

#### std.io
| Call | Description |
|---|---|
| `std.io.open(:'path', :'mode')` | fopen |
| `std.io.close(:file)` | fclose |
| `std.io.read(:buf, :n, :file)` | fgets |
| `std.io.write(:buf, :file)` | fputs |
| `std.io.flush(:file)` | fflush |
| `std.io.scanf(:'fmt', args)` | scanf |

#### std.time
| Call | Description |
|---|---|
| `std.time.now()` | current unix time (time_t) |
| `std.time.clock()` | processor time used (clock_t) |
| `std.time.diff(:a, :b)` | elapsed seconds between two time_t values |
| `std.time.sleep(:ms)` | sleep for milliseconds |

#### std.env
| Call | Description |
|---|---|
| `std.env.get(:'name')` | getenv — returns char* or NULL |

#### std.sync
| Call | Description |
|---|---|
| `std.sync.mutex()` | allocate and initialise a mutex |
| `std.sync.lock(:m)` | acquire mutex |
| `std.sync.unlock(:m)` | release mutex |
| `std.sync.mutex_free(:m)` | destroy and free mutex |

#### std.fs
| Call | Description |
|---|---|
| `std.fs.rename(:'src', :'dst')` | rename / move a file |
| `std.fs.remove(:'path')` | delete a file |
| `std.fs.exists(:'path')` | 1 if file exists, 0 otherwise |

#### std.proc
| Call | Description |
|---|---|
| `std.proc.spawn(:'cmd')` | run shell command (system) |
| `std.proc.pid()` | current process ID |

#### std.delta
| Call | Description |
|---|---|
| `std.delta.d2(:x1, :y1, :x2, :y2)` | 2-D delta between two points |
| `std.delta.d3(:x1,:y1,:z1,:x2,:y2,:z2)` | 3-D delta |
| `std.delta.mag(:d)` | magnitude (Euclidean length) of a delta |
| `std.delta.dx(:d)` | x component |
| `std.delta.dy(:d)` | y component |
| `std.delta.dz(:d)` | z component |

#### std.net *(planned)*
Socket primitives and low-level networking. Maps to POSIX socket APIs.

---

### Inline data structures

Bundled with the compiler. Imported by name, not by `std.*` path.

#### node — graph node
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

#### lattice — 2D grid
```monogram
lattice l = lattice.new(:rows, :cols);
lattice.set(:l, :r, :c, :value);
lattice.get(:l, :r, :c)           // => void*
lattice.apply(:l, :r, :c)         // apply transform fn at cell
lattice.new_transform(:rows, :cols, :fn)  // lattice with bound transform
lattice.rows(:l)                   // => int
lattice.cols(:l)                   // => int
lattice.free(:l);
```

#### slice — length-tracked array
```monogram
slice<int> s = slice.new(:n);
slice.set(:s, :i, :value);
slice.get(:s, :i)           // => uintptr_t
slice.len(:s)               // => int
slice.free(:s);

for :v in s { }             // iterate; v is uintptr_t
```

#### process — byte buffer
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

### mono — Systems Extensions *(planned)*

Official systems-focused packages maintained by Monogram. All `mono.*` modules are planned and not yet implemented.

| Module | Description |
|---|---|
| `mono.pipe` | Pipeline terminus primitives — `sink`, `bucket`, `coagulate` |
| `mono.pool` | Aliasing-free memory pool — unique allocation regions, no pointer overlap guaranteed |
| `mono.phase` | Barrier-synchronized operation blocks — `phased` and `dephased` coordination |
| `mono.lock` | Adaptive locking — `transmutex` upgrades from spinlock to blocking mutex under contention |
| `mono.linear` | Bilinearism and parallel linear execution paths |
| `mono.graph` | Polymaps, graph matrices, adjacency structures |
| `mono.inspect` | Live structure inspection without halting execution |
| `mono.glob` | Pattern matching on memory regions — globs and blobs |
| `mono.utils` | General utility belt |
| `mono.polymorph` | Runtime type dispatch and polymorphism utilities |
| `mono.pod` | Portable datasets — bundles data with its processing pipeline as one unit |
| `mono.utdctrl` | Unified thread orchestration over threads, telemetry, and processor management |
| `mono.delta` | Position deltas and change tracking between two states |

---

### mtx — Developer Tooling *(planned)*

Higher-level tooling and developer experience. Built on top of `mono`. All `mtx.*` modules are planned and not yet implemented.

| Module | Description |
|---|---|
| `mtx.argus` | Logging, diagnostics, crash reporting, runtime monitoring |
| `mtx.benchmark` | Profiling, timing harnesses, throughput measurement |
| `mtx.encode` | UTF-8, ASCII, and binary encoding and decoding |
| `mtx.hash` | Checksums and hashing primitives |
| `mtx.compress` | Compression primitives |

---

## Compiler status

| Feature | Status |
|---|---|
| Lexer | Implemented |
| Parser | Implemented |
| C emitter | Implemented |
| GCC driver | Implemented |
| LSP server | Implemented |
| VS Code extension | Implemented |
| Type checker | Implemented |
| `match` statement | Planned — blocked on emitter integration |
| Generic functions | Planned — blocked on emitter integration |
| `econst` / `xconst` | Implemented |
| `rebinds` | Implemented |
| `deref bind` | Implemented |
| `argx` qualifiers | Implemented |
| `phased` / `dephased` | Implemented |
| Thread `container` | Implemented |
| Lifecycle buffer | Implemented |
| `std.time` | Implemented |
| `std.env` | Implemented |
| `std.sync` | Implemented |
| `std.fs` | Implemented |
| `std.proc` | Implemented |
| `std.delta` | Implemented |
| `std.net` | Planned |
| `mono.*` / `mtx.*` libraries | Planned |

v0.2.0
