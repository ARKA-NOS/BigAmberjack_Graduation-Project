# -*- coding: utf-8 -*-
"""16px 조합형 한글 픽셀 글꼴 생성기 (자모를 칸에 맞춰 픽셀로 그린다)."""
import numpy as np

AW = 14  # 한글 그리기 영역 (셀 16x16 안쪽 14x14)
AH = 14

CHO = 'ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ'
JUNG = 'ㅏㅐㅑㅒㅓㅔㅕㅖㅗㅘㅙㅚㅛㅜㅝㅞㅟㅠㅡㅢㅣ'
JONG = ['', 'ㄱ', 'ㄲ', 'ㄳ', 'ㄴ', 'ㄵ', 'ㄶ', 'ㄷ', 'ㄹ', 'ㄺ', 'ㄻ', 'ㄼ', 'ㄽ', 'ㄾ', 'ㄿ', 'ㅀ',
        'ㅁ', 'ㅂ', 'ㅄ', 'ㅅ', 'ㅆ', 'ㅇ', 'ㅈ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ']

PAIRS = {'ㄲ': 'ㄱㄱ', 'ㄸ': 'ㄷㄷ', 'ㅃ': 'ㅂㅂ', 'ㅆ': 'ㅅㅅ', 'ㅉ': 'ㅈㅈ',
         'ㄳ': 'ㄱㅅ', 'ㄵ': 'ㄴㅈ', 'ㄶ': 'ㄴㅎ', 'ㄺ': 'ㄹㄱ', 'ㄻ': 'ㄹㅁ', 'ㄼ': 'ㄹㅂ',
         'ㄽ': 'ㄹㅅ', 'ㄾ': 'ㄹㅌ', 'ㄿ': 'ㄹㅍ', 'ㅀ': 'ㄹㅎ', 'ㅄ': 'ㅂㅅ'}

# 세로 모음: 폭, 기둥 위치, 곁줄기(열 범위), 곁줄기 개수
VERT = {
    'ㅏ': dict(w=3, stems=[0], tick=(1, 2), n=1),
    'ㅑ': dict(w=3, stems=[0], tick=(1, 2), n=2),
    'ㅓ': dict(w=3, stems=[2], tick=(0, 1), n=1),
    'ㅕ': dict(w=3, stems=[2], tick=(0, 1), n=2),
    'ㅐ': dict(w=4, stems=[0, 3], tick=(1, 2), n=1),
    'ㅒ': dict(w=4, stems=[0, 3], tick=(1, 2), n=2),
    'ㅔ': dict(w=5, stems=[2, 4], tick=(0, 1), n=1),
    'ㅖ': dict(w=5, stems=[2, 4], tick=(0, 1), n=2),
    'ㅣ': dict(w=1, stems=[0], tick=None, n=0),
}
HORIZ = {'ㅗ': ('up', 1), 'ㅛ': ('up', 2), 'ㅜ': ('down', 1), 'ㅠ': ('down', 2), 'ㅡ': (None, 0)}
COMBO = {'ㅘ': ('ㅗ', 'ㅏ'), 'ㅙ': ('ㅗ', 'ㅐ'), 'ㅚ': ('ㅗ', 'ㅣ'), 'ㅝ': ('ㅜ', 'ㅓ'),
         'ㅞ': ('ㅜ', 'ㅔ'), 'ㅟ': ('ㅜ', 'ㅣ'), 'ㅢ': ('ㅡ', 'ㅣ')}


class Canvas:
    def __init__(self, w=AW, h=AH):
        self.w, self.h = w, h
        self.g = np.zeros((h, w), dtype=bool)

    def px(self, x, y):
        if 0 <= x < self.w and 0 <= y < self.h:
            self.g[y, x] = True

    def hline(self, y, xa, xb):
        for x in range(min(xa, xb), max(xa, xb) + 1):
            self.px(x, y)

    def vline(self, x, ya, yb):
        for y in range(min(ya, yb), max(ya, yb) + 1):
            self.px(x, y)


def bresenham(x0, y0, x1, y1):
    pts = []
    dx, dy = abs(x1 - x0), -abs(y1 - y0)
    sx = 1 if x0 < x1 else -1
    sy = 1 if y0 < y1 else -1
    err = dx + dy
    while True:
        pts.append((x0, y0))
        if x0 == x1 and y0 == y1:
            break
        e2 = 2 * err
        if e2 >= dy:
            err += dy
            x0 += sx
        if e2 <= dx:
            err += dx
            y0 += sy
    return pts


# ---------------------------------------------------------------- 자음

def legs(cv, x0, y0, x1, y1):
    """ㅅ 모양 다리. 꼭짓점에서 좌우 아래 모서리로 대칭."""
    h = y1 - y0 + 1
    xc = (x0 + x1) // 2
    d = xc - x0
    s = max(0, min((h - 1) - d, (h - 1) // 3))
    if s > 0:
        cv.vline(xc, y0, y0 + s - 1)
        cv.vline(x0 + x1 - xc, y0, y0 + s - 1)
    for (x, y) in bresenham(xc, y0 + s, x0, y1):
        cv.px(x, y)
        cv.px(x0 + x1 - x, y)


def ring(cv, x0, y0, x1, y1):
    w, h = x1 - x0 + 1, y1 - y0 + 1
    if w >= 7 and h >= 7:
        cv.hline(y0, x0 + 2, x1 - 2)
        cv.hline(y1, x0 + 2, x1 - 2)
        cv.vline(x0, y0 + 2, y1 - 2)
        cv.vline(x1, y0 + 2, y1 - 2)
        for (x, y) in ((x0 + 1, y0 + 1), (x1 - 1, y0 + 1), (x0 + 1, y1 - 1), (x1 - 1, y1 - 1)):
            cv.px(x, y)
    else:
        cv.hline(y0, x0 + 1, x1 - 1)
        cv.hline(y1, x0 + 1, x1 - 1)
        cv.vline(x0, y0 + 1, y1 - 1)
        cv.vline(x1, y0 + 1, y1 - 1)


def mid(y0, y1):
    return y0 + (y1 - y0) // 2


def c_g(cv, x0, y0, x1, y1, curve):
    cv.hline(y0, x0, x1)
    if curve and y1 - y0 >= 6 and x1 - x0 >= 4:
        cv.vline(x1, y0, y1 - 2)
        cv.px(x1 - 1, y1 - 1)
        cv.px(x1 - 2, y1)
    else:
        cv.vline(x1, y0, y1)


def c_n(cv, x0, y0, x1, y1):
    cv.vline(x0, y0, y1)
    cv.hline(y1, x0, x1)


def c_d(cv, x0, y0, x1, y1):
    cv.hline(y0, x0, x1)
    cv.vline(x0, y0, y1)
    cv.hline(y1, x0, x1)


def c_r(cv, x0, y0, x1, y1):
    ym = mid(y0, y1)
    cv.hline(y0, x0, x1)
    cv.vline(x1, y0, ym)
    cv.hline(ym, x0, x1)
    cv.vline(x0, ym, y1)
    cv.hline(y1, x0, x1)


def c_m(cv, x0, y0, x1, y1):
    cv.hline(y0, x0, x1)
    cv.hline(y1, x0, x1)
    cv.vline(x0, y0, y1)
    cv.vline(x1, y0, y1)


def c_b(cv, x0, y0, x1, y1):
    ym = mid(y0, y1)
    cv.vline(x0, y0, y1)
    cv.vline(x1, y0, y1)
    cv.hline(ym, x0, x1)
    cv.hline(y1, x0, x1)


def c_j(cv, x0, y0, x1, y1):
    cv.hline(y0, x0, x1)
    legs(cv, x0, y0 + 1, x1, y1)


def c_ch(cv, x0, y0, x1, y1):
    xc = (x0 + x1) // 2
    cv.px(xc, y0)
    cv.px(x0 + x1 - xc, y0)
    cv.hline(y0 + 1, x0, x1)
    legs(cv, x0, y0 + 2, x1, y1)


def c_k(cv, x0, y0, x1, y1, curve):
    c_g(cv, x0, y0, x1, y1, curve)
    cv.hline(mid(y0, y1), x0, x1)


def c_t(cv, x0, y0, x1, y1):
    cv.hline(y0, x0, x1)
    cv.hline(mid(y0, y1), x0, x1)
    cv.hline(y1, x0, x1)
    cv.vline(x0, y0, y1)


def c_p(cv, x0, y0, x1, y1):
    k = 1 if x1 - x0 + 1 <= 6 else 2
    cv.hline(y0, x0, x1)
    cv.hline(y1, x0, x1)
    cv.vline(x0 + k, y0, y1)
    cv.vline(x1 - k, y0, y1)


def c_h(cv, x0, y0, x1, y1):
    xc = (x0 + x1) // 2
    cv.px(xc, y0)
    cv.px(x0 + x1 - xc, y0)
    cv.hline(y0 + 1, x0, x1)
    top = y0 + 3 if y1 - y0 + 1 >= 8 else y0 + 2
    ring(cv, x0 + 1, top, x1 - 1, y1)


SYMMETRIC = set('ㅅㅈㅊㅎ')


def max_width(ch, h):
    if ch == 'ㅇ':
        return h + 1
    if ch == 'ㅎ':
        return h + 2
    if ch in 'ㅅㅈㅊ':
        return 2 * h - 1
    return 2 * h


def draw_cons(cv, ch, x0, y0, x1, y1, curve=False):
    if ch in PAIRS:
        a, b = PAIRS[ch]
        w = x1 - x0 + 1
        if a == b:
            hw = (w - 1) // 2
            draw_cons(cv, a, x0, y0, x0 + hw - 1, y1, curve)
            draw_cons(cv, b, x0 + hw + 1, y0, x0 + 2 * hw, y1, curve)
        else:
            wl = (w - 1) // 2
            wr = w - 1 - wl
            draw_cons(cv, a, x0, y0, x0 + wl - 1, y1, curve)
            draw_cons(cv, b, x1 - wr + 1, y0, x1, y1, curve)
        return
    h = y1 - y0 + 1
    w = x1 - x0 + 1
    mw = max_width(ch, h)
    if w > mw:
        cut = w - mw
        x0 += cut // 2
        x1 -= cut - cut // 2
        w = mw
    if ch in SYMMETRIC and w % 2 == 0 and w > 3:
        x1 -= 1
    if ch == 'ㄱ':
        c_g(cv, x0, y0, x1, y1, curve)
    elif ch == 'ㄴ':
        c_n(cv, x0, y0, x1, y1)
    elif ch == 'ㄷ':
        c_d(cv, x0, y0, x1, y1)
    elif ch == 'ㄹ':
        c_r(cv, x0, y0, x1, y1)
    elif ch == 'ㅁ':
        c_m(cv, x0, y0, x1, y1)
    elif ch == 'ㅂ':
        c_b(cv, x0, y0, x1, y1)
    elif ch == 'ㅅ':
        legs(cv, x0, y0, x1, y1)
    elif ch == 'ㅇ':
        ring(cv, x0, y0, x1, y1)
    elif ch == 'ㅈ':
        c_j(cv, x0, y0, x1, y1)
    elif ch == 'ㅊ':
        c_ch(cv, x0, y0, x1, y1)
    elif ch == 'ㅋ':
        c_k(cv, x0, y0, x1, y1, curve)
    elif ch == 'ㅌ':
        c_t(cv, x0, y0, x1, y1)
    elif ch == 'ㅍ':
        c_p(cv, x0, y0, x1, y1)
    elif ch == 'ㅎ':
        c_h(cv, x0, y0, x1, y1)
    else:
        raise ValueError(ch)


# ---------------------------------------------------------------- 모음

def draw_vert(cv, v, a, top, bot, tick1, tick2):
    spec = VERT[v]
    for s in spec['stems']:
        cv.vline(a + s, top, bot)
    if spec['tick']:
        t0, t1 = spec['tick']
        rows = [tick1] if spec['n'] == 1 else list(tick2)
        for r in rows:
            cv.hline(r, a + t0, a + t1)


def xlayout(vw, want, gap):
    total = want + gap + vw
    if total > AW:
        want = AW - gap - vw
        total = AW
    off = (AW - total) // 2
    x0 = off
    x1 = off + want - 1
    a = x1 + gap + 1
    return x0, x1, a


def h_ticks(n, c):
    return [c] if n == 1 else [c - 2, c + 2]


def min_h(ch):
    """자음을 그리는 데 필요한 최소 높이."""
    if not ch:
        return 4
    if ch in PAIRS:
        return max(min_h(p) for p in PAIRS[ch])
    return 5 if ch in 'ㄹㅌㅎ' else 4


def tight_rows(cho, kind):
    """받침이 있을 때 가로 모음 쪽 세로 배치: (초성 아래끝, 막대 행, 곁줄기 행들)."""
    if min_h(cho) <= 4:
        if kind == 'up':
            return 3, 7, [5, 6]
        if kind == 'down':
            return 3, 5, [6, 7]
        return 4, 6, []
    if kind == 'up':
        return 4, 7, [6]
    if kind == 'down':
        return 4, 6, [7]
    return 4, 6, []


# 받침 자음별 최대 폭
JONG_W = {'ㄱ': 10, 'ㄴ': 10, 'ㄷ': 10, 'ㄹ': 10, 'ㅁ': 9, 'ㅂ': 9, 'ㅅ': 9, 'ㅇ': 7, 'ㅈ': 9,
          'ㅊ': 9, 'ㅋ': 10, 'ㅌ': 10, 'ㅍ': 10, 'ㅎ': 8}
JY0, JY1 = 9, 13


def draw_jong(cv, j):
    if j in PAIRS:
        draw_cons(cv, j, 1, JY0, 12, JY1)
    else:
        w = JONG_W[j]
        x0 = (AW - w) // 2
        draw_cons(cv, j, x0, JY0, x0 + w - 1, JY1)


def layout_v(cv, cho, jung, has):
    spec = VERT[jung]
    vw = spec['w']
    want = 8
    if cho in PAIRS:
        want = 10
    gap = 2 if vw <= 3 else 1
    if has:
        top, bot, cy0, cy1, t1, t2 = 0, 7, 1, 6, 3, (2, 5)
    else:
        top, bot, cy0, cy1, t1, t2 = 0, 13, 2, 11, 6, (4, 8)
    x0, x1, a = xlayout(vw, want, gap)
    draw_vert(cv, jung, a, top, bot, t1, t2)
    if cho:
        draw_cons(cv, cho, x0, cy0, x1, cy1, curve=True)


def layout_h(cv, cho, jung, has):
    kind, n = HORIZ[jung]
    c = 6
    if not has:
        bx0, bx1 = 0, 12
        cx0, cx1 = 2, 10
        if kind == 'up':
            cy0, cy1, bar, trows = 1, 8, 12, [10, 11]
        elif kind == 'down':
            cy0, cy1, bar, trows = 1, 7, 9, [10, 11, 12, 13]
        else:
            cy0, cy1, bar, trows = 1, 8, 10, []
    else:
        bx0, bx1 = 1, 11
        cx0, cx1 = 2, 10
        cy0 = 0
        cy1, bar, trows = tight_rows(cho, kind)
    cv.hline(bar, bx0, bx1)
    for tc in h_ticks(n, c):
        for r in trows:
            cv.px(tc, r)
    if cho:
        if cho in PAIRS:
            cx0, cx1 = 1, 11
        draw_cons(cv, cho, cx0, cy0, cx1, cy1)


def layout_c(cv, cho, jung, has):
    hk, vv = COMBO[jung]
    spec = VERT[vv]
    vw = spec['w']
    rx0, rx1, a = xlayout(vw, 10, 1)
    if has:
        top, bot = 0, 7
        kind = {'ㅗ': 'up', 'ㅜ': 'down', 'ㅡ': None}[hk]
        cy0 = 0
        cy1, bar, trows = tight_rows(cho, kind)
        vt = 7 if hk == 'ㅜ' else 3
    else:
        top, bot = 0, 13
        if hk == 'ㅗ':
            cy0, cy1, bar, trows, vt = 1, 6, 10, [8, 9], 6
        elif hk == 'ㅜ':
            cy0, cy1, bar, trows, vt = 1, 5, 7, [8, 9, 10, 11, 12], 9
        else:
            cy0, cy1, bar, trows, vt = 1, 7, 9, [], 6
    draw_vert(cv, vv, a, top, bot, vt, (vt - 2, vt + 2))
    cv.hline(bar, rx0, rx1)
    cw = 10 if cho in PAIRS else 8
    cw = min(cw, rx1 - rx0 + 1)
    cx0 = rx0 + (rx1 - rx0 + 1 - cw) // 2
    cx1 = cx0 + cw - 1
    tc = (cx0 + cx1) // 2
    for r in trows:
        cv.px(tc, r)
    if cho:
        draw_cons(cv, cho, cx0, cy0, cx1, cy1)


def syllable(code):
    s = code - 0xAC00
    L, V, T = s // 588, (s % 588) // 28, s % 28
    return compose(CHO[L], JUNG[V], JONG[T])


def compose(cho, jung, jong):
    cv = Canvas()
    has = bool(jong)
    if jung in VERT:
        layout_v(cv, cho, jung, has)
    elif jung in HORIZ:
        layout_h(cv, cho, jung, has)
    else:
        layout_c(cv, cho, jung, has)
    if has:
        draw_jong(cv, jong)
    return cv.g


COMPAT_CONS = 'ㄱㄲㄳㄴㄵㄶㄷㄸㄹㄺㄻㄼㄽㄾㄿㅀㅁㅂㅃㅄㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ'
COMPAT_VOWEL = 'ㅏㅐㅑㅒㅓㅔㅕㅖㅗㅘㅙㅚㅛㅜㅝㅞㅟㅠㅡㅢㅣ'


def center_ink(g):
    cols = np.where(g.any(axis=0))[0]
    if len(cols) == 0:
        return g
    left, right = cols[0], cols[-1]
    ink = right - left + 1
    target = (AW - ink) // 2
    out = np.zeros_like(g)
    out[:, target:target + ink] = g[:, left:right + 1]
    return out


def compat_jamo(ch):
    cv = Canvas()
    if ch in COMPAT_CONS:
        if ch in PAIRS:
            draw_cons(cv, ch, 1, 2, 12, 11)
        else:
            draw_cons(cv, ch, 2, 2, 10, 11)
        return center_ink(cv.g)
    g = compose('', ch, '')
    return center_ink(g)
