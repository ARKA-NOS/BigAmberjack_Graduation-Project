# -*- coding: utf-8 -*-
"""비트맵 글리프를 모아 TrueType(.ttf)으로 내보낸다. 1픽셀 = 64유닛, em = 16픽셀."""
import struct
import sys
import time
import numpy as np

import hangul
import latin

UNIT = 64
UPM = 16 * UNIT
ASC_PX = 14   # 기준선 위 14행 (셀 0~13행)
DESC_PX = 2   # 기준선 아래 2행 (셀 14~15행)
GAP_PX = 2

FAMILY = 'Amberjack Pixel'
STYLE = 'Regular'
PS_NAME = 'AmberjackPixel-Regular'
VERSION = 'Version 1.000'


# ---------------------------------------------------------------- 글리프 모으기

def cell(adv):
    return np.zeros((16, adv), dtype=bool)


def from_area(g):
    """14x14 한글 영역을 16x16 셀에 놓는다."""
    c = cell(16)
    c[1:15, 1:15] = g
    return c


def from_latin(top, rows):
    w = max(len(r) for r in rows)
    c = cell(w + 1)
    for i, r in enumerate(rows):
        for x, ch in enumerate(r):
            if ch == '#':
                c[4 + top + i, x] = True
    return c


def wide_symbols():
    """전각 기호 (14x14 영역에 그림)."""
    S = {}

    def new():
        return hangul.Canvas()

    cv = new(); cv.hline(1, 6, 10); cv.vline(6, 1, 9); S[0x300C] = cv.g          # 「
    cv = new(); cv.vline(7, 4, 12); cv.hline(12, 3, 7); S[0x300D] = cv.g         # 」
    cv = new()                                                                    # 『
    cv.hline(1, 5, 11); cv.vline(11, 1, 3); cv.hline(3, 7, 11); cv.vline(7, 3, 10)
    cv.hline(10, 5, 7); cv.vline(5, 1, 10); S[0x300E] = cv.g
    cv = new()                                                                    # 』
    cv.hline(12, 2, 8); cv.vline(2, 10, 12); cv.hline(10, 2, 6); cv.vline(6, 3, 10)
    cv.hline(3, 6, 8); cv.vline(8, 3, 12); S[0x300F] = cv.g
    cv = new(); cv.px(2, 11); cv.px(3, 12); S[0x3001] = cv.g                     # 、
    cv = new(); hangul.ring(cv, 1, 10, 3, 12); S[0x3002] = cv.g                  # 。
    S[0x3000] = new().g                                                           # 전각 공백
    # 화살표 ← ↑ → ↓
    cv = new(); cv.hline(6, 1, 12)
    for i in range(1, 4):
        cv.px(1 + i, 6 - i); cv.px(1 + i, 6 + i)
    S[0x2190] = cv.g
    cv = new(); cv.vline(6, 1, 12)
    for i in range(1, 4):
        cv.px(6 - i, 1 + i); cv.px(6 + i, 1 + i)
    S[0x2191] = cv.g
    cv = new(); cv.hline(6, 1, 12)
    for i in range(1, 4):
        cv.px(12 - i, 6 - i); cv.px(12 - i, 6 + i)
    S[0x2192] = cv.g
    cv = new(); cv.vline(6, 1, 12)
    for i in range(1, 4):
        cv.px(6 - i, 12 - i); cv.px(6 + i, 12 - i)
    S[0x2193] = cv.g
    # 삼각형 ▲ △ ▼ ▽
    cv = new()
    for i in range(6):
        cv.hline(4 + i, 6 - i, 6 + i)
    S[0x25B2] = cv.g
    cv = new()
    for i in range(6):
        cv.px(6 - i, 4 + i); cv.px(6 + i, 4 + i)
    cv.hline(9, 1, 11); S[0x25B3] = cv.g
    cv = new()
    for i in range(6):
        cv.hline(9 - i, 6 - i, 6 + i)
    S[0x25BC] = cv.g
    cv = new()
    for i in range(6):
        cv.px(6 - i, 9 - i); cv.px(6 + i, 9 - i)
    cv.hline(4, 1, 11); S[0x25BD] = cv.g
    return S


def collect():
    """{코드포인트: 셀 비트맵}"""
    glyphs = {}
    glyphs[0x20] = cell(5)
    glyphs[0xA0] = cell(5)
    for ch, (top, rows) in latin.G.items():
        glyphs[ord(ch)] = from_latin(top, rows)
    for cp, g in wide_symbols().items():
        glyphs[cp] = from_area(g)
    for i, ch in enumerate(hangul.COMPAT_CONS + hangul.COMPAT_VOWEL):
        glyphs[0x3131 + i] = from_area(hangul.compat_jamo(ch))
    for code in range(0xAC00, 0xD7A4):
        glyphs[code] = from_area(hangul.syllable(code))
    return glyphs


def notdef():
    c = cell(11)
    c[4, 1:10] = True
    c[13, 1:10] = True
    c[4:14, 1] = True
    c[4:14, 9] = True
    return c


# ---------------------------------------------------------------- 외곽선

def trace(bitmap):
    """채워진 픽셀의 경계를 따라 닫힌 윤곽선을 만든다(바깥 시계방향, 구멍 반시계방향)."""
    H, W = bitmap.shape
    filled = set()
    for r in range(H):
        for c in range(W):
            if bitmap[r, c]:
                filled.add((c, ASC_PX - 1 - r))
    edges = {}

    def add(a, b):
        edges.setdefault(a, []).append(b)

    for (x, y) in filled:
        if (x, y + 1) not in filled:
            add((x, y + 1), (x + 1, y + 1))
        if (x + 1, y) not in filled:
            add((x + 1, y + 1), (x + 1, y))
        if (x, y - 1) not in filled:
            add((x + 1, y), (x, y))
        if (x - 1, y) not in filled:
            add((x, y), (x, y + 1))

    contours = []
    while edges:
        start = None
        for v, outs in edges.items():
            if len(outs) == 1:
                start = v
                break
        if start is None:
            start = next(iter(edges))
        loop = [start]
        cur = start
        prev = None
        while True:
            outs = edges[cur]
            if len(outs) == 1 or prev is None:
                nxt = outs[0]
            else:
                dx, dy = prev
                order = [(dy, -dx), (dx, dy), (-dy, dx)]
                nxt = None
                for o in order:
                    cand = (cur[0] + o[0], cur[1] + o[1])
                    if cand in outs:
                        nxt = cand
                        break
                if nxt is None:
                    nxt = outs[0]
            outs.remove(nxt)
            if not outs:
                del edges[cur]
            prev = (nxt[0] - cur[0], nxt[1] - cur[1])
            cur = nxt
            if cur == start:
                break
            loop.append(cur)
        # 일직선 위의 점 제거
        pts = []
        n = len(loop)
        for i in range(n):
            a, b, c = loop[i - 1], loop[i], loop[(i + 1) % n]
            d1 = (b[0] - a[0], b[1] - a[1])
            d2 = (c[0] - b[0], c[1] - b[1])
            if d1[0] * d2[1] - d1[1] * d2[0] != 0:
                pts.append(b)
        contours.append([(x * UNIT, y * UNIT) for (x, y) in pts])
    return contours


def encode_glyph(contours):
    if not contours:
        return b'', None, 0, 0
    pts = [p for c in contours for p in c]
    xs = [p[0] for p in pts]
    ys = [p[1] for p in pts]
    bbox = (min(xs), min(ys), max(xs), max(ys))
    out = bytearray(struct.pack('>hhhhh', len(contours), *bbox))
    n = 0
    for c in contours:
        n += len(c)
        out += struct.pack('>H', n - 1)
    out += struct.pack('>H', 0)
    flags = []
    xb = bytearray()
    yb = bytearray()
    px = py = 0
    for (x, y) in pts:
        f = 0x01
        dx, dy = x - px, y - py
        if dx == 0:
            f |= 0x10
        elif -255 <= dx <= 255:
            f |= 0x02 | (0x10 if dx > 0 else 0)
            xb.append(abs(dx))
        else:
            xb += struct.pack('>h', dx)
        if dy == 0:
            f |= 0x20
        elif -255 <= dy <= 255:
            f |= 0x04 | (0x20 if dy > 0 else 0)
            yb.append(abs(dy))
        else:
            yb += struct.pack('>h', dy)
        flags.append(f)
        px, py = x, y
    fb = bytearray()
    i = 0
    while i < len(flags):
        j = i
        while j + 1 < len(flags) and flags[j + 1] == flags[i] and j + 1 - i < 255:
            j += 1
        if j > i:
            fb += bytes([flags[i] | 0x08, j - i])
        else:
            fb.append(flags[i])
        i = j + 1
    out += fb + xb + yb
    while len(out) % 4:
        out.append(0)
    return bytes(out), bbox, len(pts), len(contours)


# ---------------------------------------------------------------- 표

def checksum(data):
    data = data + b'\0' * ((4 - len(data) % 4) % 4)
    return sum(struct.unpack('>%dI' % (len(data) // 4), data)) & 0xFFFFFFFF


def cmap_table(cmap):
    """cmap: {코드포인트: 글리프번호} (BMP만)"""
    cps = sorted(cmap)
    segs = []
    for cp in cps:
        gid = cmap[cp]
        if segs and cp == segs[-1][1] + 1 and gid - cp == segs[-1][2]:
            segs[-1][1] = cp
        else:
            segs.append([cp, cp, gid - cp])
    segs.append([0xFFFF, 0xFFFF, 1])
    seg_count = len(segs)
    sr = 2 * (2 ** int(np.floor(np.log2(seg_count))))
    es = int(np.floor(np.log2(seg_count)))
    rs = 2 * seg_count - sr
    body = struct.pack('>HHHH', seg_count * 2, sr, es, rs)
    body += b''.join(struct.pack('>H', s[1]) for s in segs)
    body += struct.pack('>H', 0)
    body += b''.join(struct.pack('>H', s[0]) for s in segs)
    body += b''.join(struct.pack('>H', s[2] % 65536) for s in segs)
    body += b''.join(struct.pack('>H', 0) for s in segs)
    sub = struct.pack('>HHH', 4, 6 + len(body), 0) + body
    head = struct.pack('>HH', 0, 2) + struct.pack('>HHI', 0, 3, 20) + struct.pack('>HHI', 3, 1, 20)
    return head + sub


def name_table():
    names = {
        0: 'Made for the BigAmberjack graduation project.',
        1: FAMILY,
        2: STYLE,
        3: '1.000;NONE;' + PS_NAME,
        4: FAMILY + ' ' + STYLE,
        5: VERSION,
        6: PS_NAME,
    }
    recs = []
    for nid in sorted(names):
        recs.append((3, 1, 0x0409, nid, names[nid].encode('utf-16-be')))
    recs.append((3, 1, 0x0412, 1, '앰버잭 픽셀'.encode('utf-16-be')))
    recs.append((3, 1, 0x0412, 4, '앰버잭 픽셀 Regular'.encode('utf-16-be')))
    recs.sort(key=lambda r: (r[0], r[1], r[2], r[3]))
    count = len(recs)
    offset = 6 + 12 * count
    header = struct.pack('>HHH', 0, count, offset)
    strings = b''
    entries = b''
    for (p, e, l, n, s) in recs:
        entries += struct.pack('>HHHHHH', p, e, l, n, len(s), len(strings))
        strings += s
    return header + entries + strings


def build(path):
    glyph_bitmaps = collect()
    cps = sorted(glyph_bitmaps)
    order = [None] + cps                 # 0번은 .notdef
    bitmaps = [notdef()] + [glyph_bitmaps[cp] for cp in cps]
    cmap = {cp: i + 1 for i, cp in enumerate(cps)}

    glyf = bytearray()
    loca = [0]
    metrics = []
    bbox_all = [32767, 32767, -32768, -32768]
    max_pts = max_ctrs = 0
    min_lsb = min_rsb = 32767
    max_ext = 0
    for bm in bitmaps:
        adv = bm.shape[1] * UNIT
        data, bbox, npts, nctr = encode_glyph(trace(bm))
        glyf += data
        loca.append(len(glyf))
        if bbox:
            lsb = bbox[0]
            bbox_all = [min(bbox_all[0], bbox[0]), min(bbox_all[1], bbox[1]),
                        max(bbox_all[2], bbox[2]), max(bbox_all[3], bbox[3])]
            max_pts = max(max_pts, npts)
            max_ctrs = max(max_ctrs, nctr)
            min_lsb = min(min_lsb, lsb)
            min_rsb = min(min_rsb, adv - bbox[2])
            max_ext = max(max_ext, bbox[2])
        else:
            lsb = 0
        metrics.append((adv, lsb))
    num = len(bitmaps)

    now = int(time.time()) + 2082844800
    head = struct.pack('>IIIIHHqqhhhhHHhhh', 0x00010000, 0x00010000, 0, 0x5F0F3CF5, 0x0009, UPM,
                       now, now, *bbox_all, 0, 8, 2, 1, 0)
    adv_max = max(m[0] for m in metrics)
    hhea = struct.pack('>IhhhHhhhhhhhhhhhH', 0x00010000, ASC_PX * UNIT, -DESC_PX * UNIT, GAP_PX * UNIT,
                       adv_max, min_lsb, min_rsb, max_ext, 1, 0, 0, 0, 0, 0, 0, 0, num)
    maxp = struct.pack('>IHHHHHHHHHHHHHH', 0x00010000, num, max_pts, max_ctrs, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0)
    nz = [m[0] for m in metrics if m[0] > 0]
    avg = int(round(sum(nz) / len(nz)))
    ur1 = (1 << 0) | (1 << 1) | (1 << 28) | (1 << 31)
    ur2 = (1 << (37 - 32)) | (1 << (45 - 32)) | (1 << (48 - 32)) | (1 << (52 - 32)) | (1 << (56 - 32))
    os2 = struct.pack('>HhHHH' + 'h' * 11, 4, avg, 400, 5, 0,
                      650, 700, 0, 140, 650, 700, 0, 480, 64, 4 * UNIT, 0)
    os2 += bytes(10)
    os2 += struct.pack('>IIII', ur1, ur2, 0, 0)
    os2 += b'NONE'
    os2 += struct.pack('>HHHhhhHH', 0x00C0, min(cps), 0xFFFF if max(cps) > 0xFFFF else max(cps),
                       ASC_PX * UNIT, -DESC_PX * UNIT, GAP_PX * UNIT,
                       max(ASC_PX * UNIT, bbox_all[3]), max(DESC_PX * UNIT, -bbox_all[1]))
    os2 += struct.pack('>II', (1 << 0) | (1 << 19), 0)
    os2 += struct.pack('>hhHHH', 7 * UNIT, 10 * UNIT, 0, 32, 1)
    hmtx = b''.join(struct.pack('>Hh', a, l) for (a, l) in metrics)
    loca_b = b''.join(struct.pack('>I', o) for o in loca)
    post = struct.pack('>IihhIIIII', 0x00030000, 0, -2 * UNIT, UNIT, 0, 0, 0, 0, 0)

    tables = {
        b'head': head, b'hhea': hhea, b'maxp': maxp, b'OS/2': os2, b'hmtx': hmtx,
        b'cmap': cmap_table(cmap), b'loca': loca_b, b'glyf': bytes(glyf),
        b'name': name_table(), b'post': post,
    }
    tags = sorted(tables)
    n = len(tags)
    sr = 16 * (2 ** int(np.floor(np.log2(n))))
    es = int(np.floor(np.log2(n)))
    rs = n * 16 - sr
    offset = 12 + 16 * n
    directory = struct.pack('>IHHHH', 0x00010000, n, sr, es, rs)
    body = b''
    for tag in tags:
        data = tables[tag]
        directory += struct.pack('>4sIII', tag, checksum(data), offset + len(body), len(data))
        body += data + b'\0' * ((4 - len(data) % 4) % 4)
    font = bytearray(directory + body)
    adj = (0xB1B0AFBA - checksum(bytes(font))) & 0xFFFFFFFF
    head_off = offset + sum(len(tables[t]) + ((4 - len(tables[t]) % 4) % 4) for t in tags[:tags.index(b'head')])
    font[head_off + 8:head_off + 12] = struct.pack('>I', adj)
    with open(path, 'wb') as f:
        f.write(font)
    return num, len(font)


if __name__ == '__main__':
    n, size = build(sys.argv[1])
    print('glyphs', n, 'bytes', size)
