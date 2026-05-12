namespace mngc.Stdlib;

public static class StdlibHeaders
{
    public static readonly Dictionary<string, string> All = new()
    {
        ["node"]          = Node,
        ["lattice"]       = Lattice,
        ["process"]       = Process,
        ["slice"]         = Slice,
        ["std.time"]      = StdTime,
        ["std.sync"]      = StdSync,
        ["std.fs"]        = StdFs,
        ["std.proc"]      = StdProc,
        ["std.delta"]     = StdDelta,
        ["mono.phase"]    = MonoPhase,
        ["mono.sync"]     = MonoSync,
        ["mono.pipe"]     = MonoPipe,
        ["mono.pool"]     = MonoPool,
        ["mono.linear"]   = MonoLinear,
        ["mono.graph"]    = MonoGraph,
        ["mono.inspect"]  = MonoInspect,
        ["mono.glob"]     = MonoGlob,
        ["mono.utils"]    = MonoUtils,
        ["mono.polymorph"]= MonoPolymorph,
        ["mono.podlib"]   = MonoPodlib,
        ["mono.utdctrl"]  = MonoUtdctrl,
        ["mtx.argus"]     = MtxArgus,
        ["mtx.benchmark"] = MtxBenchmark,
        ["mtx.encode"]    = MtxEncode,
        ["mtx.hash"]      = MtxHash,
        ["mtx.compress"]  = MtxCompress,
        ["mtx.kiln"]      = MtxKiln,
    };

    public const string Node = @"#include <stdlib.h>
typedef struct mgnode_t {
    void* value;
    struct mgnode_t* next;
    struct mgnode_t* prev;
} mgnode_t;
typedef struct { mgnode_t* from; mgnode_t* to; void* (*fn)(void*); } mgnode_xform_t;
static inline mgnode_t* node_new(void* v) {
    mgnode_t* n = (mgnode_t*)malloc(sizeof(mgnode_t));
    n->value = v; n->next = NULL; n->prev = NULL; return n;
}
static inline void      node_link(mgnode_t* a, mgnode_t* b) { a->next = b; b->prev = a; }
static inline mgnode_t* node_next(mgnode_t* n) { return n ? n->next : NULL; }
static inline mgnode_t* node_prev(mgnode_t* n) { return n ? n->prev : NULL; }
static inline void*     node_get(mgnode_t* n)  { return n ? n->value : NULL; }
static inline void      node_set(mgnode_t* n, void* v) { if (n) n->value = v; }
static inline void      node_free(mgnode_t* n) { free(n); }
static inline mgnode_xform_t* node_transform(mgnode_t* from, void* (*fn)(void*)) {
    mgnode_xform_t* t = (mgnode_xform_t*)malloc(sizeof(mgnode_xform_t));
    t->from = from; t->fn = fn;
    t->to = fn ? node_new(fn(from->value)) : NULL;
    return t;
}";

    public const string Lattice = @"#include <stdlib.h>
typedef struct { void** data; size_t rows; size_t cols; void* (*fn)(void*, void*); } mglattice_t;
static inline mglattice_t* lattice_new(size_t rows, size_t cols) {
    mglattice_t* l = (mglattice_t*)malloc(sizeof(mglattice_t));
    l->data = (void**)calloc(rows * cols, sizeof(void*));
    l->rows = rows; l->cols = cols; l->fn = NULL; return l;
}
static inline mglattice_t* lattice_new_transform(size_t rows, size_t cols, void* (*fn)(void*, void*)) {
    mglattice_t* l = lattice_new(rows, cols); l->fn = fn; return l;
}
static inline void* lattice_get(mglattice_t* l, size_t r, size_t c) {
    return (l && r < l->rows && c < l->cols) ? l->data[r * l->cols + c] : NULL;
}
static inline void lattice_set(mglattice_t* l, size_t r, size_t c, void* v) {
    if (l && r < l->rows && c < l->cols) l->data[r * l->cols + c] = v;
}
static inline void* lattice_apply(mglattice_t* l, size_t r, size_t c) {
    void* v = lattice_get(l, r, c);
    return (l && l->fn) ? l->fn(v, l->data) : v;
}
static inline size_t lattice_rows(mglattice_t* l) { return l ? l->rows : 0; }
static inline size_t lattice_cols(mglattice_t* l) { return l ? l->cols : 0; }
static inline void   lattice_free(mglattice_t* l) { if (l) { free(l->data); free(l); } }";

    public const string Slice = @"#include <stdlib.h>
#include <stdint.h>
typedef struct { uintptr_t* data; size_t len; size_t cap; } mgslice_t;
static inline mgslice_t* slice_new(size_t n) {
    mgslice_t* s = (mgslice_t*)malloc(sizeof(mgslice_t));
    s->data = (uintptr_t*)calloc(n, sizeof(uintptr_t)); s->len = n; s->cap = n; return s;
}
static inline uintptr_t slice_get(mgslice_t* s, size_t i) { return (s && i < s->len) ? s->data[i] : 0; }
static inline void      slice_set(mgslice_t* s, size_t i, uintptr_t v) { if (s && i < s->len) s->data[i] = v; }
static inline size_t    slice_len(mgslice_t* s) { return s ? s->len : 0; }
static inline void      slice_free(mgslice_t* s) { if (s) { free(s->data); free(s); } }";

    public const string Process = @"#include <stdlib.h>
#include <string.h>
typedef struct { unsigned char* buf; size_t cap; size_t len; } mgprocess_t;
static inline mgprocess_t* process_new(size_t cap) {
    mgprocess_t* p = (mgprocess_t*)malloc(sizeof(mgprocess_t));
    p->buf = (unsigned char*)calloc(cap, 1); p->cap = cap; p->len = 0; return p;
}
static inline unsigned char process_get(mgprocess_t* p, size_t i) {
    return (p && i < p->cap) ? p->buf[i] : 0;
}
static inline void process_set(mgprocess_t* p, size_t i, unsigned char v) {
    if (!p || i >= p->cap) return;
    p->buf[i] = v; if (i >= p->len) p->len = i + 1;
}
static inline void process_write(mgprocess_t* p, size_t off, const void* src, size_t n) {
    if (!p || off + n > p->cap) return;
    memcpy(p->buf + off, src, n); if (off + n > p->len) p->len = off + n;
}
static inline void process_read(mgprocess_t* p, size_t off, void* dst, size_t n) {
    if (p && off + n <= p->cap) memcpy(dst, p->buf + off, n);
}
static inline size_t process_len(mgprocess_t* p) { return p ? p->len : 0; }
static inline size_t process_cap(mgprocess_t* p) { return p ? p->cap : 0; }
static inline void   process_free(mgprocess_t* p) { if (p) { free(p->buf); free(p); } }";

    public const string StdTime = @"#include <time.h>
#include <stdint.h>
#ifdef _WIN32
#include <windows.h>
static inline void mg_sleep_ms(uint32_t ms) { Sleep(ms); }
#else
#include <unistd.h>
static inline void mg_sleep_ms(uint32_t ms) { usleep((useconds_t)ms * 1000); }
#endif";

    public const string StdSync = @"#include <stdlib.h>
#ifdef _WIN32
#include <windows.h>
typedef CRITICAL_SECTION mg_mutex_t;
static inline mg_mutex_t* mg_mutex_new(void)  { mg_mutex_t* m = (mg_mutex_t*)malloc(sizeof(mg_mutex_t)); InitializeCriticalSection(m); return m; }
static inline void mg_mutex_lock(mg_mutex_t* m)   { EnterCriticalSection(m); }
static inline void mg_mutex_unlock(mg_mutex_t* m) { LeaveCriticalSection(m); }
static inline void mg_mutex_free(mg_mutex_t* m)   { DeleteCriticalSection(m); free(m); }
#else
#include <pthread.h>
typedef pthread_mutex_t mg_mutex_t;
static inline mg_mutex_t* mg_mutex_new(void)  { mg_mutex_t* m = (mg_mutex_t*)malloc(sizeof(mg_mutex_t)); pthread_mutex_init(m, NULL); return m; }
static inline void mg_mutex_lock(mg_mutex_t* m)   { pthread_mutex_lock(m); }
static inline void mg_mutex_unlock(mg_mutex_t* m) { pthread_mutex_unlock(m); }
static inline void mg_mutex_free(mg_mutex_t* m)   { pthread_mutex_destroy(m); free(m); }
#endif";

    public const string StdFs = @"#include <stdio.h>
#include <stdlib.h>
#ifdef _WIN32
#include <io.h>
static inline int mg_file_exists(const char* path) { return _access(path, 0) == 0; }
#else
#include <unistd.h>
static inline int mg_file_exists(const char* path) { return access(path, F_OK) == 0; }
#endif";

    public const string StdProc = @"#include <stdlib.h>
#ifdef _WIN32
#include <windows.h>
static inline int mg_proc_pid(void) { return (int)GetCurrentProcessId(); }
#else
#include <unistd.h>
static inline int mg_proc_pid(void) { return (int)getpid(); }
#endif";

    public const string StdDelta = @"#include <math.h>
typedef struct { double dx; double dy; double dz; } mgdelta_t;
static inline mgdelta_t mg_delta_2d(double x1, double y1, double x2, double y2) { return (mgdelta_t){x2-x1, y2-y1, 0.0}; }
static inline mgdelta_t mg_delta_3d(double x1, double y1, double z1, double x2, double y2, double z2) { return (mgdelta_t){x2-x1, y2-y1, z2-z1}; }
static inline double mg_delta_mag(mgdelta_t d) { return sqrt(d.dx*d.dx + d.dy*d.dy + d.dz*d.dz); }
static inline double mg_delta_dx(mgdelta_t d) { return d.dx; }
static inline double mg_delta_dy(mgdelta_t d) { return d.dy; }
static inline double mg_delta_dz(mgdelta_t d) { return d.dz; }";

    public const string MonoPhase = @"#include <stdlib.h>
#include <string.h>
#ifdef _WIN32
#include <windows.h>
typedef struct { HANDLE* threads; int count; int cap; } mg_container_t;
static inline mg_container_t* mg_container_new(void) { mg_container_t* c = (mg_container_t*)calloc(1, sizeof(mg_container_t)); c->threads = NULL; c->count = 0; c->cap = 0; return c; }
static inline void mg_container_add(mg_container_t* c, HANDLE h) { if (c->count >= c->cap) { c->cap = c->cap ? c->cap*2 : 4; c->threads = (HANDLE*)realloc(c->threads, c->cap*sizeof(HANDLE)); } c->threads[c->count++] = h; }
static inline void mg_container_join(mg_container_t* c) { if (c->threads) WaitForMultipleObjects(c->count, c->threads, TRUE, INFINITE); free(c->threads); free(c); }
static inline void mg_container_detach(mg_container_t* c) { for (int i = 0; i < c->count; i++) CloseHandle(c->threads[i]); free(c->threads); free(c); }
typedef struct { HANDLE* threads; int count; int cap; HANDLE barrier_event; } mg_phased_t;
static inline mg_phased_t* mg_phased_new(void) { mg_phased_t* p = (mg_phased_t*)calloc(1, sizeof(mg_phased_t)); p->barrier_event = CreateEvent(NULL,TRUE,FALSE,NULL); return p; }
static inline void mg_phased_add(mg_phased_t* p, HANDLE h) { if (p->count >= p->cap) { p->cap = p->cap ? p->cap*2 : 4; p->threads = (HANDLE*)realloc(p->threads, p->cap*sizeof(HANDLE)); } p->threads[p->count++] = h; }
static inline void mg_phased_join(mg_phased_t* p) { if (p->threads) WaitForMultipleObjects(p->count, p->threads, TRUE, INFINITE); CloseHandle(p->barrier_event); free(p->threads); free(p); }
#else
#include <pthread.h>
typedef struct { pthread_t* threads; int count; int cap; } mg_container_t;
static inline mg_container_t* mg_container_new(void) { mg_container_t* c = (mg_container_t*)calloc(1, sizeof(mg_container_t)); return c; }
static inline void mg_container_add(mg_container_t* c, pthread_t h) { if (c->count >= c->cap) { c->cap = c->cap ? c->cap*2 : 4; c->threads = (pthread_t*)realloc(c->threads, c->cap*sizeof(pthread_t)); } c->threads[c->count++] = h; }
static inline void mg_container_join(mg_container_t* c) { for (int i = 0; i < c->count; i++) pthread_join(c->threads[i], NULL); free(c->threads); free(c); }
static inline void mg_container_detach(mg_container_t* c) { for (int i = 0; i < c->count; i++) pthread_detach(c->threads[i]); free(c->threads); free(c); }
#ifdef __APPLE__
#include <dispatch/dispatch.h>
typedef struct { pthread_t* threads; int count; int cap; dispatch_semaphore_t sem; } mg_phased_t;
static inline mg_phased_t* mg_phased_new(int n) { mg_phased_t* p = (mg_phased_t*)calloc(1, sizeof(mg_phased_t)); p->sem = dispatch_semaphore_create(n); return p; }
static inline void mg_phased_add(mg_phased_t* p, pthread_t h) { if (p->count >= p->cap) { p->cap = p->cap ? p->cap*2 : 4; p->threads = (pthread_t*)realloc(p->threads, p->cap*sizeof(pthread_t)); } p->threads[p->count++] = h; }
static inline void mg_phased_join(mg_phased_t* p) { for (int i = 0; i < p->count; i++) pthread_join(p->threads[i], NULL); free(p->threads); free(p); }
#else
#include <pthread.h>
typedef struct { pthread_t* threads; int count; int cap; pthread_barrier_t barrier; } mg_phased_t;
static inline mg_phased_t* mg_phased_new(int n) { mg_phased_t* p = (mg_phased_t*)calloc(1, sizeof(mg_phased_t)); pthread_barrier_init(&p->barrier, NULL, n); return p; }
static inline void mg_phased_add(mg_phased_t* p, pthread_t h) { if (p->count >= p->cap) { p->cap = p->cap ? p->cap*2 : 4; p->threads = (pthread_t*)realloc(p->threads, p->cap*sizeof(pthread_t)); } p->threads[p->count++] = h; }
static inline void mg_phased_join(mg_phased_t* p) { for (int i = 0; i < p->count; i++) pthread_join(p->threads[i], NULL); pthread_barrier_destroy(&p->barrier); free(p->threads); free(p); }
#endif
#endif
static inline void* mg_thread_run(void* fn) { void (*f)(void) = (void(*)(void))fn; f(); return NULL; }";

    // ── mono.sync — transmutex (adaptive spinlock → blocking mutex) ───────────
    public const string MonoSync = @"#include <stdlib.h>
#ifdef _WIN32
#include <windows.h>
typedef struct { CRITICAL_SECTION cs; volatile LONG spin_fails; } mgtransmutex_t;
static inline mgtransmutex_t* mg_transmutex_new(void) { mgtransmutex_t* m = (mgtransmutex_t*)calloc(1,sizeof(mgtransmutex_t)); InitializeCriticalSectionAndSpinCount(&m->cs,512); return m; }
static inline void mg_transmutex_acquire(mgtransmutex_t* m) { EnterCriticalSection(&m->cs); }
static inline void mg_transmutex_release(mgtransmutex_t* m) { LeaveCriticalSection(&m->cs); }
static inline void mg_transmutex_free(mgtransmutex_t* m)    { DeleteCriticalSection(&m->cs); free(m); }
#else
#include <pthread.h>
typedef struct { pthread_mutex_t mutex; volatile int contention; } mgtransmutex_t;
static inline mgtransmutex_t* mg_transmutex_new(void) { mgtransmutex_t* m = (mgtransmutex_t*)calloc(1,sizeof(mgtransmutex_t)); pthread_mutex_init(&m->mutex,NULL); return m; }
static inline void mg_transmutex_acquire(mgtransmutex_t* m) {
    for (volatile int i = 0; i < 512; i++) { if (pthread_mutex_trylock(&m->mutex) == 0) { m->contention = 0; return; } }
    m->contention++; pthread_mutex_lock(&m->mutex);
}
static inline void mg_transmutex_release(mgtransmutex_t* m) { pthread_mutex_unlock(&m->mutex); }
static inline void mg_transmutex_free(mgtransmutex_t* m)    { pthread_mutex_destroy(&m->mutex); free(m); }
#endif";

    // ── mono.pipe — pipeline terminus primitives ───────────────────────────────
    public const string MonoPipe = @"#include <stdlib.h>
#include <string.h>
typedef struct { void (*write_fn)(void*); void* ctx; } mgsink_t;
typedef struct { void** data; size_t len; size_t cap; int filled; } mgbucket_t;
static inline mgsink_t*   mg_sink_new(void (*fn)(void*))   { mgsink_t* s = (mgsink_t*)malloc(sizeof(mgsink_t)); s->write_fn = fn; s->ctx = NULL; return s; }
static inline void        mg_sink_write(mgsink_t* s, void* v) { if (s && s->write_fn) s->write_fn(v); }
static inline void        mg_sink_free(mgsink_t* s)          { free(s); }
static inline mgbucket_t* mg_bucket_new(size_t cap)          { mgbucket_t* b = (mgbucket_t*)malloc(sizeof(mgbucket_t)); b->data = (void**)calloc(cap,sizeof(void*)); b->cap = cap; b->len = 0; b->filled = 0; return b; }
static inline void        mg_bucket_fill(mgbucket_t* b, void* v) { if (b && b->len < b->cap) { b->data[b->len++] = v; b->filled = 1; } }
static inline void*       mg_bucket_drain(mgbucket_t* b)    { if (!b || b->len == 0) return NULL; void* v = b->data[--b->len]; if (b->len == 0) b->filled = 0; return v; }
static inline void        mg_bucket_free(mgbucket_t* b)      { if (b) { free(b->data); free(b); } }
static inline void*       mg_coagulate(void* a, size_t sa, void* b2, size_t sb, size_t* out) { *out = sa+sb; unsigned char* r = (unsigned char*)malloc(*out); memcpy(r,a,sa); memcpy(r+sa,b2,sb); return r; }";

    // ── mono.pool — aliasing-free memory pool ─────────────────────────────────
    public const string MonoPool = @"#include <stdlib.h>
#include <stdint.h>
typedef struct { unsigned char* base; size_t cap; size_t used; } mgpool_t;
static inline mgpool_t* mg_pool_new(size_t cap)              { mgpool_t* p = (mgpool_t*)malloc(sizeof(mgpool_t)); p->base = (unsigned char*)malloc(cap); p->cap = cap; p->used = 0; return p; }
static inline void*     mg_pool_alloc(mgpool_t* p, size_t n) { if (!p || p->used + n > p->cap) return NULL; void* r = p->base + p->used; p->used += n; return r; }
static inline void      mg_pool_reset(mgpool_t* p)           { if (p) p->used = 0; }
static inline void      mg_pool_free(mgpool_t* p)            { if (p) { free(p->base); free(p); } }";

    // ── mono.linear — linear execution chains ─────────────────────────────────
    public const string MonoLinear = @"#include <stdlib.h>
typedef struct { void* (**fns)(void*); size_t count; size_t cap; } mglinear_t;
static inline mglinear_t* mg_linear_new(void)                { mglinear_t* l = (mglinear_t*)calloc(1,sizeof(mglinear_t)); return l; }
static inline void mg_linear_bind(mglinear_t* l, void* (*fn)(void*)) {
    if (l->count >= l->cap) { l->cap = l->cap ? l->cap*2 : 4; l->fns = (void*(**)(void*))realloc(l->fns, l->cap*sizeof(void*(*)(void*))); }
    l->fns[l->count++] = fn;
}
static inline void* mg_linear_run(mglinear_t* l, void* data) { for (size_t i = 0; i < l->count; i++) data = l->fns[i](data); return data; }
static inline void  mg_linear_free(mglinear_t* l)             { if (l) { free(l->fns); free(l); } }";

    // ── mono.graph — graph matrices and adjacency structures ──────────────────
    public const string MonoGraph = @"#include <stdlib.h>
#include <stdint.h>
typedef struct { void** nodes; uint8_t* adj; size_t count; size_t cap; } mggraph_t;
static inline mggraph_t* mg_graph_new(size_t cap) {
    mggraph_t* g = (mggraph_t*)calloc(1,sizeof(mggraph_t));
    g->nodes = (void**)calloc(cap,sizeof(void*));
    g->adj   = (uint8_t*)calloc(cap*cap,1);
    g->cap   = cap; return g;
}
static inline size_t mg_graph_add(mggraph_t* g, void* node)            { if (!g || g->count >= g->cap) return (size_t)-1; g->nodes[g->count] = node; return g->count++; }
static inline void   mg_graph_link(mggraph_t* g, size_t a, size_t b)   { if (g && a < g->count && b < g->count) { g->adj[a*g->cap+b] = 1; g->adj[b*g->cap+a] = 1; } }
static inline void   mg_graph_unlink(mggraph_t* g, size_t a, size_t b) { if (g && a < g->count && b < g->count) { g->adj[a*g->cap+b] = 0; g->adj[b*g->cap+a] = 0; } }
static inline int    mg_graph_has_edge(mggraph_t* g, size_t a, size_t b){ return (g && a < g->count && b < g->count) ? g->adj[a*g->cap+b] : 0; }
static inline size_t mg_graph_count(mggraph_t* g)                       { return g ? g->count : 0; }
static inline void*  mg_graph_node(mggraph_t* g, size_t i)              { return (g && i < g->count) ? g->nodes[i] : NULL; }
static inline void   mg_graph_free(mggraph_t* g)                        { if (g) { free(g->nodes); free(g->adj); free(g); } }";

    // ── mono.inspect — live structure inspection ──────────────────────────────
    public const string MonoInspect = @"#include <stdio.h>
#include <stdint.h>
static inline void mg_inspect_dump(const void* ptr, size_t n) {
    const unsigned char* b = (const unsigned char*)ptr;
    for (size_t i = 0; i < n; i++) { if (i && i%16==0) printf(""\n""); printf(""%02x "", b[i]); }
    printf(""\n"");
}
static inline void mg_inspect_addr(const void* ptr)                     { printf(""[inspect] addr: %p\n"", ptr); }
static inline void mg_inspect_name(const char* name, const void* ptr)   { printf(""[inspect] %s @ %p\n"", name, ptr); }
static inline void mg_inspect_int(const char* name, long long val)      { printf(""[inspect] %s = %lld\n"", name, val); }
static inline void mg_inspect_float(const char* name, double val)       { printf(""[inspect] %s = %g\n"", name, val); }";

    // ── mono.glob — pattern matching on memory regions ────────────────────────
    public const string MonoGlob = @"#include <string.h>
static inline int mg_glob_match(const char* pat, const char* str) {
    if (!*pat) return !*str;
    if (*pat == '*') { while (*pat == '*') pat++; if (!*pat) return 1; for (; *str; str++) if (mg_glob_match(pat, str)) return 1; return 0; }
    if (*pat == '?' || *pat == *str) return mg_glob_match(pat+1, str+1);
    return 0;
}
static inline long mg_blob_scan(const unsigned char* data, size_t dlen, const unsigned char* pat, size_t plen) {
    if (plen == 0) return 0;
    for (size_t i = 0; i + plen <= dlen; i++) { if (memcmp(data+i, pat, plen) == 0) return (long)i; }
    return -1;
}";

    // ── mono.utils — general utility belt ─────────────────────────────────────
    public const string MonoUtils = @"#include <string.h>
#define mg_min(a,b) ((a)<(b)?(a):(b))
#define mg_max(a,b) ((a)>(b)?(a):(b))
#define mg_clamp(v,lo,hi) ((v)<(lo)?(lo):(v)>(hi)?(hi):(v))
static inline void mg_swap(void* a, void* b, size_t n) { unsigned char tmp[256]; size_t r = n<256?n:256; memcpy(tmp,a,r); memcpy(a,b,r); memcpy(b,tmp,r); }
static inline int  mg_ispow2(size_t n) { return n && !(n & (n-1)); }
static inline size_t mg_next_pow2(size_t n) { if (!n) return 1; n--; n|=n>>1;n|=n>>2;n|=n>>4;n|=n>>8;n|=n>>16; return n+1; }";

    // ── mono.polymorph — runtime type dispatch ────────────────────────────────
    public const string MonoPolymorph = @"#include <stdlib.h>
#include <string.h>
typedef struct { const char* type_name; void* data; void* (*methods[16])(void*); size_t method_count; } mgpoly_t;
static inline mgpoly_t* mg_poly_new(const char* type_name, void* data) { mgpoly_t* p = (mgpoly_t*)calloc(1,sizeof(mgpoly_t)); p->type_name = type_name; p->data = data; return p; }
static inline void mg_poly_bind(mgpoly_t* p, void* (*fn)(void*))        { if (p && p->method_count < 16) p->methods[p->method_count++] = fn; }
static inline void* mg_poly_call(mgpoly_t* p, size_t idx)               { return (p && idx < p->method_count && p->methods[idx]) ? p->methods[idx](p->data) : NULL; }
static inline int   mg_poly_is(mgpoly_t* p, const char* type)           { return p && strcmp(p->type_name, type) == 0; }
static inline void  mg_poly_free(mgpoly_t* p)                           { free(p); }";

    // ── mono.podlib — portable dataset (data + pipeline) ─────────────────────
    public const string MonoPodlib = @"#include <stdlib.h>
#include <stdio.h>
#include <string.h>
typedef struct { void* data; size_t size; void* (*pipeline)(void*); } mgpod_t;
static inline mgpod_t* mg_pod_create(size_t size)                   { mgpod_t* p = (mgpod_t*)calloc(1,sizeof(mgpod_t)); p->data = calloc(1,size); p->size = size; return p; }
static inline void     mg_pod_attach(mgpod_t* p, void* (*fn)(void*)){ if (p) p->pipeline = fn; }
static inline void*    mg_pod_run(mgpod_t* p)                        { return (p && p->pipeline) ? p->pipeline(p->data) : (p ? p->data : NULL); }
static inline int      mg_pod_export(mgpod_t* p, const char* path)  { FILE* f = fopen(path,""wb""); if (!f) return 0; fwrite(p->data,1,p->size,f); fclose(f); return 1; }
static inline mgpod_t* mg_pod_import(const char* path)              { FILE* f = fopen(path,""rb""); if (!f) return NULL; fseek(f,0,SEEK_END); long s = ftell(f); rewind(f); mgpod_t* p = mg_pod_create((size_t)s); fread(p->data,1,s,f); fclose(f); return p; }
static inline void     mg_pod_free(mgpod_t* p)                      { if (p) { free(p->data); free(p); } }";

    // ── mono.utdctrl — universal thread orchestration ─────────────────────────
    public const string MonoUtdctrl = @"#include <stdlib.h>
#include <stdio.h>
#ifdef _WIN32
#include <windows.h>
typedef struct { HANDLE* threads; int count; int cap; void (*telemetry_fn)(const char*); int active; } mgutdctrl_t;
static inline mgutdctrl_t* mg_utdctrl_init(void)                          { return (mgutdctrl_t*)calloc(1,sizeof(mgutdctrl_t)); }
static inline void mg_utdctrl_spawn(mgutdctrl_t* c, void (*fn)(void))     { if (!c) return; if (c->count>=c->cap){c->cap=c->cap?c->cap*2:4;c->threads=(HANDLE*)realloc(c->threads,c->cap*sizeof(HANDLE));} HANDLE h=CreateThread(NULL,0,(LPTHREAD_START_ROUTINE)fn,NULL,0,NULL); c->threads[c->count++]=h; c->active++; }
static inline void mg_utdctrl_telemetry(mgutdctrl_t* c, void (*fn)(const char*)) { if (c) c->telemetry_fn = fn; }
static inline void mg_utdctrl_monitor(mgutdctrl_t* c)                     { if (c && c->telemetry_fn) { char buf[64]; snprintf(buf,64,""[utdctrl] threads=%d"",c->active); c->telemetry_fn(buf); } else if (c) printf(""[utdctrl] threads=%d\n"",c->active); }
static inline void mg_utdctrl_shutdown(mgutdctrl_t* c)                    { if (!c) return; WaitForMultipleObjects(c->count,c->threads,TRUE,INFINITE); for(int i=0;i<c->count;i++) CloseHandle(c->threads[i]); free(c->threads); free(c); }
#else
#include <pthread.h>
typedef struct { pthread_t* threads; int count; int cap; void (*telemetry_fn)(const char*); int active; } mgutdctrl_t;
static inline mgutdctrl_t* mg_utdctrl_init(void)                          { return (mgutdctrl_t*)calloc(1,sizeof(mgutdctrl_t)); }
static inline void mg_utdctrl_spawn(mgutdctrl_t* c, void* (*fn)(void*))   { if (!c) return; if (c->count>=c->cap){c->cap=c->cap?c->cap*2:4;c->threads=(pthread_t*)realloc(c->threads,c->cap*sizeof(pthread_t));} pthread_create(&c->threads[c->count++],NULL,fn,NULL); c->active++; }
static inline void mg_utdctrl_telemetry(mgutdctrl_t* c, void (*fn)(const char*)) { if (c) c->telemetry_fn = fn; }
static inline void mg_utdctrl_monitor(mgutdctrl_t* c)                     { if (c && c->telemetry_fn) { char buf[64]; snprintf(buf,64,""[utdctrl] threads=%d"",c->active); c->telemetry_fn(buf); } else if (c) printf(""[utdctrl] threads=%d\n"",c->active); }
static inline void mg_utdctrl_shutdown(mgutdctrl_t* c)                    { if (!c) return; for(int i=0;i<c->count;i++) pthread_join(c->threads[i],NULL); free(c->threads); free(c); }
#endif";

    // ── mtx.argus — logging and diagnostics ──────────────────────────────────
    public const string MtxArgus = @"#include <stdio.h>
#include <stdlib.h>
#include <time.h>
typedef enum { ARGUS_DEBUG=0, ARGUS_INFO=1, ARGUS_WARN=2, ARGUS_ERROR=3, ARGUS_FATAL=4 } argus_level_t;
typedef struct { FILE* out; argus_level_t min_level; } mgargus_t;
static inline mgargus_t* mg_argus_new(const char* path) { mgargus_t* a = (mgargus_t*)malloc(sizeof(mgargus_t)); a->out = (path && path[0]) ? fopen(path,""a"") : stdout; a->min_level = ARGUS_DEBUG; return a; }
static inline void mg_argus_log(mgargus_t* a, argus_level_t lvl, const char* msg) {
    if (!a || lvl < a->min_level) return;
    static const char* lvl_str[] = {""DEBUG"",""INFO"",""WARN"",""ERROR"",""FATAL""};
    time_t t = time(NULL); struct tm* tm = localtime(&t); char ts[20]; strftime(ts,sizeof(ts),""%H:%M:%S"",tm);
    fprintf(a->out, ""[%s][%s] %s\n"", ts, lvl_str[lvl], msg);
    if (lvl == ARGUS_FATAL) { fflush(a->out); exit(1); }
}
static inline void mg_argus_debug(mgargus_t* a, const char* m) { mg_argus_log(a, ARGUS_DEBUG, m); }
static inline void mg_argus_info(mgargus_t* a, const char* m)  { mg_argus_log(a, ARGUS_INFO,  m); }
static inline void mg_argus_warn(mgargus_t* a, const char* m)  { mg_argus_log(a, ARGUS_WARN,  m); }
static inline void mg_argus_error(mgargus_t* a, const char* m) { mg_argus_log(a, ARGUS_ERROR, m); }
static inline void mg_argus_fatal(mgargus_t* a, const char* m) { mg_argus_log(a, ARGUS_FATAL, m); }
static inline void mg_argus_free(mgargus_t* a)                  { if (a) { if (a->out && a->out != stdout && a->out != stderr) fclose(a->out); free(a); } }";

    // ── mtx.benchmark — profiling and timing harness ─────────────────────────
    public const string MtxBenchmark = @"#include <stdio.h>
#include <stdlib.h>
#include <time.h>
typedef struct { const char* name; clock_t start; double elapsed_ms; size_t iters; } mgbench_t;
static inline mgbench_t* mg_bench_new(const char* name) { mgbench_t* b = (mgbench_t*)calloc(1,sizeof(mgbench_t)); b->name = name; return b; }
static inline void   mg_bench_start(mgbench_t* b)               { if (b) b->start = clock(); }
static inline void   mg_bench_stop(mgbench_t* b)                 { if (b) b->elapsed_ms = ((double)(clock()-b->start) / CLOCKS_PER_SEC) * 1000.0; }
static inline void   mg_bench_report(mgbench_t* b)               { if (b) printf(""[bench] %s: %.3f ms (%zu iters)\n"", b->name, b->elapsed_ms, b->iters); }
static inline double mg_bench_ms(mgbench_t* b)                   { return b ? b->elapsed_ms : 0.0; }
static inline void   mg_bench_run(mgbench_t* b, void (*fn)(void), size_t n) {
    if (!b) return; b->iters = n; b->start = clock();
    for (size_t i = 0; i < n; i++) fn();
    b->elapsed_ms = ((double)(clock()-b->start) / CLOCKS_PER_SEC) * 1000.0;
    mg_bench_report(b);
}
static inline void mg_bench_free(mgbench_t* b) { free(b); }";

    // ── mtx.encode — encoding and decoding ───────────────────────────────────
    public const string MtxEncode = @"#include <stdlib.h>
#include <string.h>
#include <stdint.h>
static inline char* mg_hex_encode(const unsigned char* data, size_t len) {
    char* s = (char*)malloc(len*2+1); if (!s) return NULL;
    for (size_t i = 0; i < len; i++) { static const char h[] = ""0123456789abcdef""; s[i*2]=h[data[i]>>4]; s[i*2+1]=h[data[i]&0xf]; }
    s[len*2] = 0; return s;
}
static inline unsigned char* mg_hex_decode(const char* hex, size_t* out_len) {
    size_t n = strlen(hex)/2; *out_len = n; unsigned char* r = (unsigned char*)malloc(n);
    for (size_t i = 0; i < n; i++) { unsigned int v; sscanf(hex+i*2,""%02x"",&v); r[i]=(unsigned char)v; }
    return r;
}
static inline char* mg_base64_encode(const unsigned char* src, size_t len) {
    static const char b64[] = ""ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/"";
    size_t olen = 4*((len+2)/3); char* out = (char*)malloc(olen+1); size_t j = 0;
    for (size_t i = 0; i < len; ) {
        uint32_t o = 0; int r = 0;
        for (int k = 0; k < 3 && i < len; k++, i++) { o=(o<<8)|src[i]; r++; }
        o <<= (3-r)*8;
        for (int k = 0; k < 4; k++) { out[j++] = (k<=r) ? b64[(o>>((3-k)*6))&0x3f] : '='; }
    }
    out[j] = 0; return out;
}
static inline int mg_utf8_valid(const unsigned char* s) {
    while (*s) { if ((*s & 0x80)==0) { s++; continue; } int n=(*s&0xe0)==0xc0?1:(*s&0xf0)==0xe0?2:(*s&0xf8)==0xf0?3:-1; if (n<0) return 0; s++; for(int i=0;i<n;i++,s++) if((*s&0xc0)!=0x80) return 0; }
    return 1;
}
static inline int mg_ascii_only(const char* s) { while (*s) { if ((unsigned char)*s > 127) return 0; s++; } return 1; }";

    // ── mtx.hash — checksums and hashing ─────────────────────────────────────
    public const string MtxHash = @"#include <stdint.h>
#include <stddef.h>
static inline uint64_t mg_fnv1a(const void* data, size_t len) {
    uint64_t h = 14695981039346656037ULL; const unsigned char* p = (const unsigned char*)data;
    for (size_t i = 0; i < len; i++) h = (h ^ p[i]) * 1099511628211ULL;
    return h;
}
static inline uint32_t mg_crc32(const void* data, size_t len) {
    uint32_t crc = 0xffffffff; const unsigned char* p = (const unsigned char*)data;
    for (size_t i = 0; i < len; i++) { crc ^= p[i]; for (int j=0;j<8;j++) crc=(crc&1)?(crc>>1)^0xedb88320:(crc>>1); }
    return crc ^ 0xffffffff;
}
static inline uint64_t mg_djb2(const char* str) { uint64_t h = 5381; int c; while ((c = (unsigned char)*str++)) h = ((h<<5)+h)+c; return h; }";

    // ── mtx.compress — RLE compression ───────────────────────────────────────
    public const string MtxCompress = @"#include <stdlib.h>
#include <string.h>
static inline unsigned char* mg_rle_encode(const unsigned char* src, size_t n, size_t* out_len) {
    unsigned char* out = (unsigned char*)malloc(n*2+2); size_t j = 0;
    for (size_t i = 0; i < n; ) {
        unsigned char c = src[i]; size_t run = 1;
        while (i+run < n && src[i+run] == c && run < 255) run++;
        out[j++] = (unsigned char)run; out[j++] = c; i += run;
    }
    *out_len = j; return out;
}
static inline unsigned char* mg_rle_decode(const unsigned char* src, size_t n, size_t* out_len) {
    size_t cap = n*4; unsigned char* out = (unsigned char*)malloc(cap); size_t j = 0;
    for (size_t i = 0; i+1 < n; i+=2) {
        size_t run = src[i]; if (j+run > cap) { cap=(j+run)*2; out=(unsigned char*)realloc(out,cap); }
        memset(out+j, src[i+1], run); j += run;
    }
    *out_len = j; return out;
}";

    // ── mtx.kiln — build and transform pipeline ───────────────────────────────
    public const string MtxKiln = @"#include <stdlib.h>
typedef struct { void* (**stages)(void*); size_t count; size_t cap; const char** names; } mgkiln_t;
static inline mgkiln_t* mg_kiln_new(void) { return (mgkiln_t*)calloc(1,sizeof(mgkiln_t)); }
static inline void mg_kiln_stage(mgkiln_t* k, void* (*fn)(void*), const char* name) {
    if (!k) return;
    if (k->count >= k->cap) {
        k->cap = k->cap ? k->cap*2 : 4;
        k->stages = (void*(**)(void*))realloc(k->stages, k->cap*sizeof(void*(*)(void*)));
        k->names  = (const char**)realloc(k->names, k->cap*sizeof(const char*));
    }
    k->stages[k->count] = fn; k->names[k->count] = name; k->count++;
}
static inline void* mg_kiln_run(mgkiln_t* k, void* data)  { for (size_t i = 0; k && i < k->count; i++) data = k->stages[i](data); return data; }
static inline void  mg_kiln_free(mgkiln_t* k)              { if (k) { free(k->stages); free(k->names); free(k); } }";
}
