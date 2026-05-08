namespace mngc.Stdlib;

public static class StdlibHeaders
{
    public static readonly Dictionary<string, string> All = new()
    {
        ["node"]    = Node,
        ["lattice"] = Lattice,
        ["process"] = Process,
        ["slice"]   = Slice,
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
typedef struct { void** data; size_t len; size_t cap; } mgslice_t;
static inline mgslice_t* slice_new(size_t n) {
    mgslice_t* s = (mgslice_t*)malloc(sizeof(mgslice_t));
    s->data = (void**)calloc(n, sizeof(void*)); s->len = n; s->cap = n; return s;
}
static inline void*  slice_get(mgslice_t* s, size_t i) { return (s && i < s->len) ? s->data[i] : NULL; }
static inline void   slice_set(mgslice_t* s, size_t i, void* v) { if (s && i < s->len) s->data[i] = v; }
static inline size_t slice_len(mgslice_t* s) { return s ? s->len : 0; }
static inline void   slice_free(mgslice_t* s) { if (s) { free(s->data); free(s); } }";

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
}
