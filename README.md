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

> **Constraint:** Generic function declarations (`func: name<T>(...)`) are parsed but not yet supported by the emitter — the compiler will reject them at compile time. Generic built-in types (`slice<T>`, `node<T, U>`) are supported.

### Variables

```monogram
int x = 42;
const float pi = 3.14;
volatile int flag = 0;
const volatile int limit = 100;
econst int MAX_CONN = 100;    // extern const — readable across modules
xconst int KEY = 0xFF34;      // static const — private to this file
```

| Qualifier | C emission | Visibility |
|---|---|---|
| `const` | `const` | module-local |
| `volatile` | `volatile` | — |
| `const volatile` | `const volatile` | — |
| `econst` | `extern const` | readable outside module, never writable |
| `xconst` | `static const` | compiler error if accessed outside file |

### rebinds

Reassign a binding without redeclaring it. The type checker verifies compatibility.

```monogram
int x = 5;
rebind x = 10;
```

### deref bind

Bind a name to a dereferenced memory location.

```monogram
int val = 42;
int[] ptr = @val;
deref bind :ref = ~ptr;   // ref == 42
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

**Library types** (imported with their module)
`delta` `transmutex` `sink` `bucket` `pool` `graph` `poly` `pod` `utdctrl` `argus` `bench` `kiln`

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
**Transfer:** `~>` move lifecycle ownership (source becomes spent)
**Ternary:** `cond ? a : b`

### Pipeline operator
```monogram
// a -> b        calls b(a)
// a -> b(:x)    calls b(a, x)  — left is prepended as first arg

func: double(:n int) => int { => n * 2; }
func: inc(:n int)    => int { => n + 1; }

int result = 5 -> double -> inc;   // 11
```

### Argument qualifiers

Optional qualifiers on function parameters that control coercion and transformation.

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

**Match** *(not yet implemented — blocked on emitter integration)*
```monogram
match: val {
    int   => { sys.stdout(:'integer\n'); }
    float => { sys.stdout(:'float\n');   }
    _     => { sys.stdout(:'other\n');   }
}
```
> The type checker rejects `match` until emitter support is added.

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

`for :v in slice` and `for -> :v in slice` require a `slice<T>` — the type checker enforces this.
The iteration variable `v` is `uintptr_t` — cast with `as` for typed access.

For iter loops (`for :i in expr cond;`) declare the variable before the loop. Mutate it in the body to advance — the increment slot is empty by design.

### Custom operators
```monogram
op: add_vec(a, b) => int {
    => a + b;
}
```

> `op:` parameters are untyped by design — all params are emitted as `void*` in C.

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

Requires `#import<mono.phase>`. Emits pthreads on POSIX, Win32 threads on Windows.

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

`std.mem.free` transitions a binding to `dead` automatically.

### Transfer operator

`~>` moves lifecycle ownership from one binding to another. The source becomes spent; the destination is declared live with the same type.

```monogram
Vec2 a = make_vec(:3.0, :4.0);
Vec2 b ~> a;      // b is live, a is spent
sys.stdout(:b.x); // valid
sys.stdout(:a.x); // LIFECYCLE ERROR — a is spent
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
// std — standard library
#import<std.io>
#import<std.mem>
#import<std.str>
#import<std.math>
#import<std.time>
#import<std.env>
#import<std.sync>
#import<std.fs>
#import<std.proc>
#import<std.delta>
#import<std.*>      // expands to stdio.h, stdlib.h, string.h, math.h

// inline data structures
#import<node>
#import<lattice>
#import<slice>
#import<process>

// mono — systems extensions
#import<mono.phase>
#import<mono.sync>
#import<mono.pipe>
#import<mono.pool>
#import<mono.linear>
#import<mono.graph>
#import<mono.inspect>
#import<mono.glob>
#import<mono.utils>
#import<mono.polymorph>
#import<mono.podlib>
#import<mono.utdctrl>

// mtx — developer tooling
#import<mtx.argus>
#import<mtx.benchmark>
#import<mtx.encode>
#import<mtx.hash>
#import<mtx.compress>
#import<mtx.kiln>
```

---

## Library reference

Monogram's libraries are organized into three tiers. Lower tiers have no dependency on higher ones.

| Tier | Prefix | Role |
|---|---|---|
| Standard Library | `std` | Thin wrappers over C stdlib. Ships with the compiler. |
| Systems Extensions | `mono` | Official systems-focused packages. Bundled with the compiler. |
| Developer Tooling | `mtx` | Higher-level tooling and dev experience. Built on top of mono. |

Inline data structures (`node`, `lattice`, `slice`, `process`) are bundled with the compiler and do not follow the tier prefix convention.

---

### sys *(built-in, no import)*
| Call | Description |
|---|---|
| `sys.stdout(:'fmt', args)` | printf to stdout |
| `sys.stderr(:'fmt', args)` | printf to stderr |
| `sys.exit(:code)` | terminate process |

---

### std — Standard Library

#### std.mem
| Call | Description |
|---|---|
| `std.mem.alloc(:size)` | malloc |
| `std.mem.calloc(:n, :size)` | calloc |
| `std.mem.realloc(:ptr, :size)` | realloc |
| `std.mem.free(:ptr)` | free — transitions binding to dead |

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
| `std.time.now()` | current unix time → int64 |
| `std.time.clock()` | processor time used → int64 |
| `std.time.diff(:a, :b)` | elapsed seconds between two time values → float64 |
| `std.time.sleep(:ms)` | sleep for milliseconds |

#### std.env
| Call | Description |
|---|---|
| `std.env.get(:'name')` | getenv → char* or NULL |

#### std.sync
| Call | Description |
|---|---|
| `std.sync.mutex()` | allocate and initialise a platform mutex |
| `std.sync.lock(:m)` | acquire mutex |
| `std.sync.unlock(:m)` | release mutex |
| `std.sync.mutex_free(:m)` | destroy and free mutex |

#### std.fs
| Call | Description |
|---|---|
| `std.fs.rename(:'src', :'dst')` | rename / move a file → int (0 success) |
| `std.fs.remove(:'path')` | delete a file → int |
| `std.fs.exists(:'path')` | 1 if file exists, 0 otherwise |

#### std.proc
| Call | Description |
|---|---|
| `std.proc.spawn(:'cmd')` | run shell command (system) → int exit code |
| `std.proc.pid()` | current process ID → int |

#### std.delta
| Call | Description |
|---|---|
| `std.delta.d2(:x1, :y1, :x2, :y2)` | 2-D delta between two points → delta |
| `std.delta.d3(:x1,:y1,:z1,:x2,:y2,:z2)` | 3-D delta → delta |
| `std.delta.mag(:d)` | Euclidean magnitude → float64 |
| `std.delta.dx(:d)` | x component → float64 |
| `std.delta.dy(:d)` | y component → float64 |
| `std.delta.dz(:d)` | z component → float64 |

#### std.net *(planned)*
Socket primitives and low-level networking. Maps to POSIX socket APIs.

---

### Inline data structures

Bundled with the compiler. Imported by name, not by `std.*` path.

#### node — linked / graph node
```monogram
#import<node>

node n = node.new(:value);
node.link(:a, :b);              // a.next = b, b.prev = a
node.get(:n)                    // => void*
node.set(:n, :value);
node.next(:n)                   // => node
node.prev(:n)                   // => node
node<int, float> t = node.transform(:n, :fn);
node.free(:n);
```

#### lattice — 2D grid
```monogram
#import<lattice>

lattice l = lattice.new(:rows, :cols);
lattice.set(:l, :r, :c, :value);
lattice.get(:l, :r, :c)                    // => void*
lattice.apply(:l, :r, :c)                  // apply bound transform fn at cell
lattice.new_transform(:rows, :cols, :fn)   // lattice with bound transform
lattice.rows(:l)                           // => int
lattice.cols(:l)                           // => int
lattice.free(:l);
```

#### slice — length-tracked array
```monogram
#import<slice>

slice<int> s = slice.new(:n);
slice.set(:s, :i, :value);
slice.get(:s, :i)           // => uintptr_t
slice.len(:s)               // => int
slice.free(:s);

for :v in s { }             // iterate; v is uintptr_t — cast with as for typed access
```

#### process — byte buffer
```monogram
#import<process>

process p = process.new(:capacity);
process.set(:p, :i, :byte);
process.get(:p, :i)                      // => byte
process.write(:p, :offset, :src, :n);
process.read(:p, :offset, :dst, :n);
process.len(:p)                          // => int
process.cap(:p)                          // => int
process.free(:p);

// thread spawn — used inside container/phased/dephased blocks
process.thread(:fn);    // spawn fn as a new thread
```

---

### mono — Systems Extensions

Bundled with the compiler. Import by module name.

#### mono.phase — concurrency blocks
Used via the `container`, `phased`, and `dephased` language keywords. See [Concurrency blocks](#concurrency-blocks).

```monogram
#import<mono.phase>

container :workers {
    process.thread(:task_a);
    process.thread(:task_b);
}
```

#### mono.sync — transmutex
Adaptive mutex: spins under low contention, upgrades to a blocking mutex under high contention.

```monogram
#import<mono.sync>

transmutex :lock = mono.sync.transmutex();
mono.sync.acquire(:lock);
    shared_counter = shared_counter + 1;
mono.sync.release(:lock);
mono.sync.free(:lock);
```

| Call | Description |
|---|---|
| `mono.sync.transmutex()` | create adaptive mutex |
| `mono.sync.acquire(:m)` | acquire — spins first, then blocks |
| `mono.sync.release(:m)` | release |
| `mono.sync.free(:m)` | destroy and free |

#### mono.pipe — pipeline terminus primitives

```monogram
#import<mono.pipe>

sink :out = mono.pipe.sink(:sys.stdout);
mono.pipe.write(:out, :data);

bucket :buf = mono.pipe.bucket(:1024);
mono.pipe.fill(:buf, :item);
mono.pipe.drain(:buf);
```

| Call | Description |
|---|---|
| `mono.pipe.sink(:fn)` | write-only terminus targeting fn |
| `mono.pipe.write(:sink, :data)` | push data into sink |
| `mono.pipe.sink_free(:sink)` | free sink |
| `mono.pipe.bucket(:cap)` | bounded drainable buffer |
| `mono.pipe.fill(:bucket, :data)` | fill — state becomes live |
| `mono.pipe.drain(:bucket)` | drain one item — returns void* |
| `mono.pipe.bucket_free(:bucket)` | free bucket |
| `mono.pipe.coagulate(:a, :sa, :b, :sb, :out_len)` | merge two byte arrays into one |

#### mono.pool — aliasing-free memory pool

```monogram
#import<mono.pool>

pool :mem = mono.pool.new(:4096);
int[] ptr_a = mono.pool.alloc(:mem, :64);
int[] ptr_b = mono.pool.alloc(:mem, :64);
// ptr_a and ptr_b are guaranteed not to alias
mono.pool.free(:mem);
```

| Call | Description |
|---|---|
| `mono.pool.new(:cap)` | create linear allocator |
| `mono.pool.alloc(:pool, :size)` | allocate unique region — no pointer overlap |
| `mono.pool.reset(:pool)` | reset used counter, keep backing memory |
| `mono.pool.free(:pool)` | free entire pool |

#### mono.linear — linear execution chains

```monogram
#import<mono.linear>

linear :chain = mono.linear.new();
mono.linear.bind(:chain, :normalize);
mono.linear.bind(:chain, :scale);
void[] result = mono.linear.run(:chain, :input);
mono.linear.free(:chain);
```

| Call | Description |
|---|---|
| `mono.linear.new()` | create a linear execution chain |
| `mono.linear.bind(:chain, :fn)` | append a transform stage |
| `mono.linear.run(:chain, :data)` | run all stages sequentially → void* |
| `mono.linear.free(:chain)` | free chain |

#### mono.graph — graph matrices and adjacency structures

```monogram
#import<mono.graph>

graph :g = mono.graph.new(:64);
int a = mono.graph.add(:g, :node_a);
int b = mono.graph.add(:g, :node_b);
mono.graph.link(:g, :a, :b);
mono.graph.free(:g);
```

| Call | Description |
|---|---|
| `mono.graph.new(:cap)` | create graph with initial node capacity |
| `mono.graph.add(:g, :node)` | add node → index |
| `mono.graph.link(:g, :a, :b)` | add undirected edge |
| `mono.graph.unlink(:g, :a, :b)` | remove edge |
| `mono.graph.has_edge(:g, :a, :b)` | 1 if edge exists |
| `mono.graph.node(:g, :i)` | retrieve node at index → void* |
| `mono.graph.count(:g)` | number of nodes |
| `mono.graph.free(:g)` | free graph |

#### mono.inspect — live structure inspection

```monogram
#import<mono.inspect>

mono.inspect.dump(:ptr, :128);
mono.inspect.name(:'counter', :counter);
mono.inspect.int(:'val', :val);
```

| Call | Description |
|---|---|
| `mono.inspect.dump(:ptr, :size)` | hex dump n bytes to stdout |
| `mono.inspect.addr(:ptr)` | print pointer address |
| `mono.inspect.name(:'label', :ptr)` | print label + address |
| `mono.inspect.int(:'label', :val)` | print integer value |
| `mono.inspect.float(:'label', :val)` | print float value |

#### mono.glob — pattern matching on memory regions

```monogram
#import<mono.glob>

int matched = mono.glob.match(:'*.mngrm', :filename);
int offset  = mono.glob.scan(:data, :len, :pattern, :plen);
```

| Call | Description |
|---|---|
| `mono.glob.match(:'pattern', :'str')` | glob match — `*` any sequence, `?` any single char |
| `mono.glob.scan(:data, :dlen, :pat, :plen)` | scan byte region for byte pattern → offset or -1 |

#### mono.utils — general utility belt

```monogram
#import<mono.utils>

mono.utils.swap(:a, :b, :8);
int p2 = mono.utils.ispow2(:64);
int n  = mono.utils.next_pow2(:100);
```

| Call | Description |
|---|---|
| `mono.utils.swap(:a, :b, :size)` | swap two memory regions in-place |
| `mono.utils.ispow2(:n)` | 1 if n is a power of 2 |
| `mono.utils.next_pow2(:n)` | next power of 2 ≥ n |

C macros available after import: `mg_min(a,b)`, `mg_max(a,b)`, `mg_clamp(v,lo,hi)`

#### mono.polymorph — runtime type dispatch

```monogram
#import<mono.polymorph>

poly :obj = mono.polymorph.new(:'Vec2', :data);
mono.polymorph.bind(:obj, :normalize);
mono.polymorph.bind(:obj, :scale);
void[] result = mono.polymorph.call(:obj, :0);
int is_vec = mono.polymorph.is(:obj, :'Vec2');
mono.polymorph.free(:obj);
```

| Call | Description |
|---|---|
| `mono.polymorph.new(:'type', :data)` | create polymorphic object with type tag |
| `mono.polymorph.bind(:poly, :fn)` | bind a method (up to 16 per object) |
| `mono.polymorph.call(:poly, :idx)` | dispatch method at index → void* |
| `mono.polymorph.is(:poly, :'type')` | 1 if type tag matches |
| `mono.polymorph.free(:poly)` | free object |

#### mono.podlib — portable datasets

Data and its processing pipeline bundled together as one unit.

```monogram
#import<mono.podlib>

pod :sensor = mono.podlib.create(:1024);
mono.podlib.attach(:sensor, :normalize);
void[] result = mono.podlib.run(:sensor);
mono.podlib.export(:sensor, :'output.pod');
mono.podlib.free(:sensor);
```

| Call | Description |
|---|---|
| `mono.podlib.create(:size)` | create portable dataset |
| `mono.podlib.attach(:pod, :fn)` | attach processing pipeline |
| `mono.podlib.run(:pod)` | execute pipeline on data → void* |
| `mono.podlib.export(:pod, :'path')` | write data to file |
| `mono.podlib.import(:'path')` | load pod from file |
| `mono.podlib.free(:pod)` | free pod |

#### mono.utdctrl — universal thread orchestration

Unified control layer over threads, telemetry, and processor management.

```monogram
#import<mono.utdctrl>

utdctrl :ctrl = mono.utdctrl.init();
mono.utdctrl.spawn(:ctrl, :worker_a);
mono.utdctrl.spawn(:ctrl, :worker_b);
mono.utdctrl.telemetry(:ctrl, :mtx.argus.logger);
mono.utdctrl.monitor(:ctrl);
mono.utdctrl.shutdown(:ctrl);
```

| Call | Description |
|---|---|
| `mono.utdctrl.init()` | create orchestration controller |
| `mono.utdctrl.spawn(:ctrl, :fn)` | spawn a managed thread |
| `mono.utdctrl.telemetry(:ctrl, :fn)` | attach telemetry handler (receives status strings) |
| `mono.utdctrl.monitor(:ctrl)` | print or emit current thread stats |
| `mono.utdctrl.shutdown(:ctrl)` | join all threads and free controller |

---

### mtx — Developer Tooling

Higher-level tooling and developer experience. Built on top of mono. Bundled with the compiler.

#### mtx.argus — logging and diagnostics

```monogram
#import<mtx.argus>

argus :log = mtx.argus.new(:'app.log');
mtx.argus.info(:log, :'server started');
mtx.argus.warn(:log, :'high memory usage');
mtx.argus.fatal(:log, :'unrecoverable error');   // logs then exit(1)
mtx.argus.free(:log);
```

| Call | Description |
|---|---|
| `mtx.argus.new(:'path')` | create logger — empty string targets stdout |
| `mtx.argus.debug(:log, :'msg')` | DEBUG level |
| `mtx.argus.info(:log, :'msg')` | INFO level |
| `mtx.argus.warn(:log, :'msg')` | WARN level |
| `mtx.argus.error(:log, :'msg')` | ERROR level |
| `mtx.argus.fatal(:log, :'msg')` | FATAL — logs then exit(1) |
| `mtx.argus.free(:log)` | flush, close file, free logger |

Output format: `[HH:MM:SS][LEVEL] message`

#### mtx.benchmark — profiling and timing

```monogram
#import<mtx.benchmark>

bench :b = mtx.benchmark.new(:'sort');
mtx.benchmark.run(:b, :my_sort, :1000);   // runs my_sort 1000 times, prints report
mtx.benchmark.free(:b);
```

| Call | Description |
|---|---|
| `mtx.benchmark.new(:'name')` | create benchmark |
| `mtx.benchmark.start(:b)` | start timer |
| `mtx.benchmark.stop(:b)` | stop timer |
| `mtx.benchmark.report(:b)` | print name, elapsed ms, iteration count |
| `mtx.benchmark.ms(:b)` | elapsed milliseconds → float64 |
| `mtx.benchmark.run(:b, :fn, :n)` | run fn n times and report |
| `mtx.benchmark.free(:b)` | free |

#### mtx.encode — encoding and decoding

```monogram
#import<mtx.encode>

char[] hex = mtx.encode.hex(:data, :len);
int valid  = mtx.encode.utf8_valid(:str);
char[] b64 = mtx.encode.base64(:data, :len);
```

| Call | Description |
|---|---|
| `mtx.encode.hex(:data, :len)` | hex-encode byte array → char* (caller frees) |
| `mtx.encode.unhex(:'hex', :out_len)` | decode hex string → byte* (caller frees) |
| `mtx.encode.base64(:data, :len)` | base64-encode → char* (caller frees) |
| `mtx.encode.utf8_valid(:'str')` | 1 if valid UTF-8 |
| `mtx.encode.ascii_only(:'str')` | 1 if all bytes ≤ 127 |

#### mtx.hash — checksums and hashing

```monogram
#import<mtx.hash>

uint64 h = mtx.hash.fnv1a(:data, :len);
uint32 c = mtx.hash.crc32(:data, :len);
uint64 d = mtx.hash.djb2(:str);
```

| Call | Description |
|---|---|
| `mtx.hash.fnv1a(:data, :len)` | FNV-1a 64-bit hash |
| `mtx.hash.crc32(:data, :len)` | CRC-32 checksum |
| `mtx.hash.djb2(:'str')` | DJB2 64-bit string hash |

#### mtx.compress — RLE compression

```monogram
#import<mtx.compress>

int out_len;
byte[] compressed = mtx.compress.encode(:data, :len, :@out_len);
byte[] original   = mtx.compress.decode(:compressed, :out_len, :@out_len);
```

| Call | Description |
|---|---|
| `mtx.compress.encode(:data, :len, :out_len)` | RLE compress → byte* (caller frees) |
| `mtx.compress.decode(:data, :len, :out_len)` | RLE decompress → byte* (caller frees) |

#### mtx.kiln — build and transform pipeline

```monogram
#import<mtx.kiln>

kiln :pipeline = mtx.kiln.new();
mtx.kiln.stage(:pipeline, :parse,     :'parse');
mtx.kiln.stage(:pipeline, :validate,  :'validate');
mtx.kiln.stage(:pipeline, :transform, :'transform');
void[] output = mtx.kiln.run(:pipeline, :input);
mtx.kiln.free(:pipeline);
```

| Call | Description |
|---|---|
| `mtx.kiln.new()` | create transform pipeline |
| `mtx.kiln.stage(:k, :fn, :'name')` | append a named stage |
| `mtx.kiln.run(:k, :data)` | run all stages sequentially → void* |
| `mtx.kiln.free(:k)` | free pipeline |

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
| Lifecycle buffer | Implemented |
| `econst` / `xconst` | Implemented |
| `rebinds` | Implemented |
| `deref bind` | Implemented |
| `~>` transfer operator | Implemented |
| `argx` qualifiers | Implemented |
| `phased` / `dephased` | Implemented |
| Thread `container` | Implemented |
| `std.time` | Implemented |
| `std.env` | Implemented |
| `std.sync` | Implemented |
| `std.fs` | Implemented |
| `std.proc` | Implemented |
| `std.delta` | Implemented |
| `mono.phase` | Implemented |
| `mono.sync` | Implemented |
| `mono.pipe` | Implemented |
| `mono.pool` | Implemented |
| `mono.linear` | Implemented |
| `mono.graph` | Implemented |
| `mono.inspect` | Implemented |
| `mono.glob` | Implemented |
| `mono.utils` | Implemented |
| `mono.polymorph` | Implemented |
| `mono.podlib` | Implemented |
| `mono.utdctrl` | Implemented |
| `mtx.argus` | Implemented |
| `mtx.benchmark` | Implemented |
| `mtx.encode` | Implemented |
| `mtx.hash` | Implemented |
| `mtx.compress` | Implemented |
| `mtx.kiln` | Implemented |
| `match` statement | Planned — blocked on emitter integration |
| Generic functions | Planned — blocked on emitter integration |
| `std.net` | Planned |

v0.2.0
