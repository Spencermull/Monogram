namespace mngc.Stdlib;

public static class StdlibHeaders
{
    public static readonly Dictionary<string, string> All = new()
    {
        ["node"]     = Node,
        ["lattice"]  = Lattice,
        ["process"]  = Process,
        ["slice"]    = Slice,
        ["std.time"] = StdTime,
        ["std.sync"] = StdSync,
        ["std.fs"]   = StdFs,
        ["std.proc"] = StdProc,
        ["std.delta"] = StdDelta,
        ["mono.phase"] = MonoPhase,
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
}
