# Arrange the CURRENT MySQL Workbench EER diagram so tables do not overlap.
# Layered layout: a referenced (parent) table sits above the tables that point
# at it; each group of linked tables gets its own block; tables with no foreign
# key go in a grid underneath. Positions only - no table, column or key changes.
#
# Use: in Workbench open the model + the diagram, File > Save (backup),
#      then Scripting > Run Workbench Script File... and pick this file.
import grt

# Layered (Sugiyama-style) layout for an ER diagram. Pure Python, no imports,
# so the same code runs inside MySQL Workbench's scripting shell.
# nodes: {name: (w, h)}; edges: [(child, parent)] meaning child has an FK to parent.
# Returns {name: (left, top)}. Parents are placed above their children.

def _component(nodes, edges, hgap, vgap):
    names = sorted(nodes)
    E = [(c, p) for (c, p) in edges if c in nodes and p in nodes and c != p]
    E = sorted(set(E))
    linked = set()
    for c, p in E:
        linked.add(c); linked.add(p)

    # break cycles: DFS over child->parent, drop back edges
    adj = {}
    for c, p in E:
        adj.setdefault(c, []).append(p)
    state, keep = {}, []
    def dfs(u):
        state[u] = 1
        for v in sorted(adj.get(u, [])):
            if state.get(v) == 1:
                continue          # back edge -> dropped
            keep.append((u, v))
            if state.get(v) is None:
                dfs(v)
        state[u] = 2
    for n in sorted(linked, key=lambda n: -len(adj.get(n, []))):
        if state.get(n) is None:
            dfs(n)
    E = sorted(set(keep))
    parents, children = {}, {}
    for c, p in E:
        parents.setdefault(c, []).append(p)
        children.setdefault(p, []).append(c)

    # layer = longest path down from a root (roots on top)
    layer = {}
    def depth(n, seen=()):
        if n in layer:
            return layer[n]
        ps = parents.get(n, [])
        d = 0 if not ps else 1 + max(depth(p, seen + (n,)) for p in ps)
        layer[n] = d
        return d
    for n in linked:
        depth(n)
    # pull parents down next to their highest child (shortens long edges)
    for _ in range(10):
        moved = False
        for n in sorted(linked, key=lambda n: -layer[n]):
            ch = children.get(n, [])
            if ch:
                want = min(layer[c] for c in ch) - 1
                if want > layer[n]:
                    layer[n] = want; moved = True
        if not moved:
            break

    # long edges get dummy nodes so they reserve a lane between real tables
    seg = []                       # (upper, lower) adjacent-layer segments
    dummies = {}
    did = [0]
    for c, p in E:
        a, b = p, c
        la, lb = layer[p], layer[c]
        prev = a
        for L in range(la + 1, lb):
            d = '~d%d' % did[0]; did[0] += 1
            layer[d] = L; dummies[d] = True
            seg.append((prev, d)); prev = d
        seg.append((prev, b))
    up, down = {}, {}
    for a, b in seg:
        down.setdefault(a, []).append(b)
        up.setdefault(b, []).append(a)

    maxL = max(layer.values()) if layer else 0
    rows = [[] for _ in range(maxL + 1)]
    for n in sorted(layer):
        rows[layer[n]].append(n)
    # initial order: DFS-ish by name
    pos = {}
    def reindex():
        for r in rows:
            for i, n in enumerate(r):
                pos[n] = i
    reindex()

    def crossings():
        total = 0
        for L in range(maxL):
            ss = [(pos[a], pos[b]) for a in rows[L] for b in down.get(a, [])]
            for i in range(len(ss)):
                for j in range(i + 1, len(ss)):
                    if (ss[i][0] - ss[j][0]) * (ss[i][1] - ss[j][1]) < 0:
                        total += 1
        return total

    best = [list(r) for r in rows]; bestc = crossings()
    for it in range(40):
        rng = range(1, maxL + 1) if it % 2 == 0 else range(maxL - 1, -1, -1)
        for L in rng:
            nb = up if it % 2 == 0 else down
            def key(n):
                xs = [pos[m] for m in nb.get(n, [])]
                return (sum(xs) / len(xs)) if xs else pos[n]
            rows[L].sort(key=key)
            for i, n in enumerate(rows[L]):
                pos[n] = i
        c = crossings()
        if c < bestc:
            bestc = c; best = [list(r) for r in rows]
    rows = best; reindex()

    # x placement: pack each row, then nudge toward neighbour average
    W = lambda n: 24 if n in dummies else nodes[n][0]
    H = lambda n: 0 if n in dummies else nodes[n][1]
    x = {}
    for r in rows:
        cx = 0
        for n in r:
            x[n] = cx; cx += W(n) + hgap
    def center(n): return x[n] + W(n) / 2.0
    for it in range(30):
        order = range(maxL + 1) if it % 2 == 0 else range(maxL, -1, -1)
        for L in order:
            r = rows[L]
            want = {}
            for n in r:
                nb = up.get(n, []) + down.get(n, [])
                want[n] = (sum(center(m) for m in nb) / len(nb) - W(n) / 2.0) if nb else x[n]
            # left-to-right sweep keeping order and gaps
            for i, n in enumerate(r):
                lo = x[r[i - 1]] + W(r[i - 1]) + hgap if i else -1e9
                x[n] = max(want[n], lo)
            for i in range(len(r) - 2, -1, -1):
                n = r[i]
                hi = x[r[i + 1]] - hgap - W(n)
                if x[n] > hi:
                    x[n] = hi
    minx = min(x.values()) if x else 0
    out = {}
    y = 0
    for L, r in enumerate(rows):
        rh = max([H(n) for n in r] + [0])
        for n in r:
            if n not in dummies:
                out[n] = (int(x[n] - minx), int(y))
        y += rh + vgap

    return out


def er_layout(nodes, edges, hgap=50, vgap=110, iso_cols=8, iso_gap=40, comp_gap=160):
    E = [(c, p) for (c, p) in edges if c in nodes and p in nodes and c != p]
    nb = {}
    for c, p in E:
        nb.setdefault(c, set()).add(p); nb.setdefault(p, set()).add(c)
    seen, comps = set(), []
    for n in sorted(nb):
        if n in seen: continue
        stack, comp = [n], []
        seen.add(n)
        while stack:
            u = stack.pop(); comp.append(u)
            for v in nb[u]:
                if v not in seen:
                    seen.add(v); stack.append(v)
        comps.append(comp)
    comps.sort(key=lambda c: -len(c))
    out = {}
    cx, maxh = 0, 0
    for comp in comps:
        cs = set(comp)
        sub = _component(dict((n, nodes[n]) for n in comp), [e for e in E if e[0] in cs], hgap, vgap)
        w = max(sub[n][0] + nodes[n][0] for n in sub)
        h = max(sub[n][1] + nodes[n][1] for n in sub)
        for n, (l, t) in sub.items():
            out[n] = (l + cx, t)
        cx += w + comp_gap; maxh = max(maxh, h)
    iso = sorted(n for n in nodes if n not in seen)
    if iso:
        cw = max(nodes[n][0] for n in iso) + iso_gap
        ch = max(nodes[n][1] for n in iso) + iso_gap
        y = maxh + 200
        for i, n in enumerate(iso):
            out[n] = (int((i % iso_cols) * cw), int(y + (i // iso_cols) * ch))
    return out


def run():
    model = grt.root.wb.doc.physicalModels[0]
    dia = model.currentDiagram or model.diagrams[0]
    figs, nodes, edges = {}, {}, []
    for f in dia.figures:
        cls = f.__grtclassname__
        if cls == 'workbench.physical.TableFigure' and f.table:
            name = f.table.name
            for fk in f.table.foreignKeys:
                if fk.referencedTable:
                    edges.append((name, fk.referencedTable.name))
        elif cls == 'workbench.physical.ViewFigure' and f.view:
            name = f.view.name
        else:
            continue
        expanded = getattr(f, 'expanded', 1)
        h = int(f.height) if expanded else 30
        figs[name] = f
        nodes[name] = (int(f.width) or 150, h or 30)
    pos = er_layout(nodes, edges)
    margin = 40
    for name, (l, t) in pos.items():
        figs[name].left = l + margin
        figs[name].top = t + margin
    right = max(l + nodes[n][0] for n, (l, t) in pos.items()) + 2 * margin
    bottom = max(t + nodes[n][1] for n, (l, t) in pos.items()) + 2 * margin
    if dia.width < right: dia.width = right
    if dia.height < bottom: dia.height = bottom
    print('Arranged %d figures (%d relationships) on "%s".' % (len(pos), len(edges), dia.name))


run()
