# -*- coding: utf-8 -*-
"""칭호 UI 스프라이트. 1텍셀 = 아트 1픽셀 (1920x1080 화면에서 6px로 키워 쓴다)."""
import json
import os
import sys
import numpy as np
from PIL import Image


def hexc(h, a=255):
    h = h.lstrip('#')
    return np.array([int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a], dtype=np.float64)


def over(base, top_hex, alpha):
    """base(RGBA) 위에 top 색을 alpha(0~255)로 덮은 불투명 색."""
    t = hexc(top_hex)
    a = alpha / 255.0
    out = base.copy()
    out[:3] = base[:3] * (1 - a) + t[:3] * a
    out[3] = 255
    return out


BLACK = '#07050a'
STONE = '#4d4059'
WIN_BG = '#1a1520'
GOLD = '#f2c046'
YELLOW = '#f5d442'
CREAM = '#f1e7cf'
CHECK_INK = '#2a1a08'


def blank(w, h):
    return np.zeros((h, w, 4), dtype=np.float64)


def fill(img, color, x0=0, y0=0, x1=None, y1=None):
    h, w = img.shape[:2]
    x1 = w - 1 if x1 is None else x1
    y1 = h - 1 if y1 is None else y1
    img[y0:y1 + 1, x0:x1 + 1] = color if isinstance(color, np.ndarray) else hexc(color)


def notched(w, h, ring, bg):
    """바깥 1px 테두리, 네 모서리 픽셀은 비움 (CSS의 4방향 box-shadow 테두리와 같은 모양)."""
    img = blank(w, h)
    fill(img, ring)
    fill(img, bg, 1, 1, w - 2, h - 2)
    for (x, y) in ((0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)):
        img[y, x] = 0
    return img


def stamp(img, rows, color, ox, oy):
    c = hexc(color)
    for y, r in enumerate(rows):
        for x, ch in enumerate(r):
            if ch == '#':
                img[oy + y, ox + x] = c


CHECK_BIG = [
    '......##',
    '.....###',
    '##..###.',
    '#######.',
    '.#####..',
    '..###...',
    '...#....',
]
CHECK_SMALL = [
    '.....#',
    '....##',
    '#..##.',
    '####..',
    '.##...',
]
LOCK_BIG = [
    '..####..',
    '.##..##.',
    '.#....#.',
    '########',
    '###..###',
    '###..###',
    '########',
]
LOCK_SMALL = [
    '.###.',
    '.#.#.',
    '#####',
    '##.##',
    '#####',
]
X_ICON = [
    '##....##',
    '###..###',
    '.######.',
    '..####..',
    '..####..',
    '.######.',
    '###..###',
    '##....##',
]
ARROW_DOWN = [
    '#######',
    '.#...#.',
    '..#.#..',
    '...#...',
]


def icon(rows, color='#ffffff'):
    img = blank(len(rows[0]), len(rows))
    stamp(img, rows, color, 0, 0)
    return img


def window():
    w = h = 24
    img = blank(w, h)
    fill(img, BLACK)
    fill(img, STONE, 1, 1, w - 2, h - 2)
    bg = hexc(WIN_BG)
    fill(img, bg, 2, 2, w - 3, h - 3)
    sh = over(bg, '#000000', 0x59)
    fill(img, sh, 2, h - 3, w - 3, h - 3)
    fill(img, sh, w - 3, 2, w - 3, h - 3)
    for (x, y) in [(xx, 2) for xx in range(2, w - 2)] + [(2, yy) for yy in range(2, h - 2)]:
        img[y, x] = over(img[y, x], '#ffffff', 0x0d)
    for (rx, ry) in ((4, 4), (w - 6, 4), (4, h - 6), (w - 6, h - 6)):
        fill(img, BLACK, rx - 1, ry - 1, rx + 2, ry + 2)
        fill(img, GOLD, rx, ry, rx + 1, ry + 1)
    return img


def bar_fill(color):
    base = hexc(color)
    img = blank(4, 6)
    fill(img, base)
    fill(img, over(base, '#ffffff', 0x40), 0, 0, 3, 0)
    fill(img, over(base, '#000000', 0x30), 0, 5, 3, 5)
    return img


def check_box(ring, bg, inner=None, inner_color=None, size=12):
    img = notched(size, size, ring, bg)
    if inner:
        iw, ih = len(inner[0]), len(inner)
        ox = (size - iw) // 2
        oy = (size - ih) // 2
        stamp(img, inner, inner_color, ox, oy)
    return img


def close_button(face, pressed=False):
    s = 18
    img = notched(s, s, BLACK, face)
    base = hexc(face)
    if pressed:
        fill(img, over(base, '#000000', 0x55), 1, 1, s - 2, 2)
        fill(img, over(base, '#ffffff', 0x18), 1, s - 2, s - 2, s - 2)
        stamp(img, X_ICON, '#fff3e0', 5, 6)
    else:
        fill(img, over(base, '#ffffff', 0x26), 1, 1, s - 2, 1)
        fill(img, over(base, '#000000', 0x55), 1, s - 3, s - 2, s - 2)
        stamp(img, X_ICON, '#fff3e0', 5, 4)
    return img


def scroll_handle():
    img = blank(4, 4)
    fill(img, '#15111a')
    fill(img, '#4d4059', 1, 1, 2, 2)
    return img


# 이름: (이미지, 9-slice 테두리 (왼, 아래, 오른, 위), 설명)
def all_sprites():
    S = {}
    S['TitleUI_Window'] = (window(), (8, 8, 8, 8), '창 전체 틀 (돌 테두리 + 금색 리벳)')
    S['TitleUI_ListPanel'] = (notched(8, 8, '#3a3045', '#1f1926'), (2, 2, 2, 2), '칭호 목록 영역')
    S['TitleUI_Row_Normal'] = (notched(8, 8, '#4a3e56', '#2a2233'), (2, 2, 2, 2), '칭호 줄 - 진행 중/시작 전')
    S['TitleUI_Row_Open'] = (notched(8, 8, CREAM, '#2a2233'), (2, 2, 2, 2), '칭호 줄 - 펼침')
    S['TitleUI_Row_Done'] = (notched(8, 8, GOLD, '#2e2419'), (2, 2, 2, 2), '칭호 줄 - 달성')
    S['TitleUI_Row_Locked'] = (notched(8, 8, '#2c2534', '#17131c'), (2, 2, 2, 2), '칭호 줄 - 잠김')
    S['TitleUI_Bar_Frame'] = (notched(8, 8, BLACK, '#0f0b13'), (2, 2, 2, 2), '칭호 진행 바 틀 (안쪽 높이 6)')
    S['TitleUI_Bar_Fill'] = (bar_fill(YELLOW), (0, 0, 0, 0), '칭호 진행 바 채움 - 진행 중')
    S['TitleUI_Bar_Fill_Done'] = (bar_fill(GOLD), (0, 0, 0, 0), '칭호 진행 바 채움 - 달성')
    S['TitleUI_QuestBar_Frame'] = (notched(6, 4, BLACK, '#0f0b13'), (2, 1, 2, 1), '퀘스트 진행 바 틀 (안쪽 높이 2)')
    qf = blank(2, 2); fill(qf, YELLOW)
    S['TitleUI_QuestBar_Fill'] = (qf, (0, 0, 0, 0), '퀘스트 진행 바 채움 - 진행 중')
    qd = blank(2, 2); fill(qd, GOLD)
    S['TitleUI_QuestBar_Fill_Done'] = (qd, (0, 0, 0, 0), '퀘스트 진행 바 채움 - 완료')
    S['TitleUI_CheckBox_Empty'] = (check_box('#5a4c68', '#0f0b13'), (0, 0, 0, 0), '칭호 달성 칸 - 미완료')
    S['TitleUI_CheckBox_Done'] = (check_box(BLACK, GOLD, CHECK_BIG, CHECK_INK), (0, 0, 0, 0), '칭호 달성 칸 - 달성 체크')
    S['TitleUI_CheckBox_Locked'] = (check_box('#2c2534', '#110d15', LOCK_BIG, '#4f4560'), (0, 0, 0, 0), '칭호 달성 칸 - 잠김')
    S['TitleUI_QuestCheck_Empty'] = (check_box('#5a4c68', '#0f0b13', size=8), (0, 0, 0, 0), '퀘스트 체크박스 - 미완료')
    S['TitleUI_QuestCheck_Done'] = (check_box(BLACK, GOLD, CHECK_SMALL, CHECK_INK, size=8), (0, 0, 0, 0), '퀘스트 체크박스 - 완료')
    S['TitleUI_QuestCheck_Locked'] = (check_box('#2c2534', '#0f0b13', size=8), (0, 0, 0, 0), '퀘스트 체크박스 - 잠김')
    S['TitleUI_CloseButton_Normal'] = (close_button('#a3352f'), (0, 0, 0, 0), '닫기 버튼 - 기본')
    S['TitleUI_CloseButton_Hover'] = (close_button('#c0433a'), (0, 0, 0, 0), '닫기 버튼 - 마우스 올림')
    S['TitleUI_CloseButton_Pressed'] = (close_button('#8a2b26', pressed=True), (0, 0, 0, 0), '닫기 버튼 - 누름')
    S['TitleUI_Arrow_Down'] = (icon(ARROW_DOWN), (0, 0, 0, 0), '펼치기 화살표 (흰색, 색은 Image Color로)')
    S['TitleUI_Arrow_Up'] = (icon(ARROW_DOWN[::-1]), (0, 0, 0, 0), '접기 화살표 (흰색)')
    S['TitleUI_Icon_Check'] = (icon(CHECK_BIG), (0, 0, 0, 0), '체크 아이콘 (흰색)')
    S['TitleUI_Icon_Lock'] = (icon(LOCK_BIG), (0, 0, 0, 0), '자물쇠 아이콘 (흰색)')
    S['TitleUI_Icon_Lock_Small'] = (icon(LOCK_SMALL), (0, 0, 0, 0), '작은 자물쇠 아이콘 (흰색, 글줄 안)')
    S['TitleUI_Icon_Close'] = (icon(X_ICON), (0, 0, 0, 0), 'X 아이콘 (흰색)')
    st = blank(4, 4); fill(st, '#15111a')
    S['TitleUI_Scrollbar_Track'] = (st, (1, 1, 1, 1), '스크롤바 바탕')
    S['TitleUI_Scrollbar_Handle'] = (scroll_handle(), (1, 1, 1, 1), '스크롤바 손잡이')
    dot = blank(2, 2)
    dot[0, 0] = hexc('#ffffff', 0x0b)
    S['TitleUI_DotTile'] = (dot, (0, 0, 0, 0), '창 뒤 배경 점무늬 타일 (반복)')
    return S


WRAP_REPEAT = {'TitleUI_DotTile'}


def to_image(a):
    return Image.fromarray(np.clip(np.round(a), 0, 255).astype(np.uint8), 'RGBA')


if __name__ == '__main__':
    out = sys.argv[1]
    os.makedirs(out, exist_ok=True)
    manifest = {}
    for name, (img, border, desc) in all_sprites().items():
        to_image(img).save(os.path.join(out, name + '.png'))
        manifest[name] = dict(w=img.shape[1], h=img.shape[0], border=border, desc=desc,
                              wrap=0 if name in WRAP_REPEAT else 1)
    with open(os.path.join(out, 'manifest.json'), 'w', encoding='utf-8') as f:
        json.dump(manifest, f, ensure_ascii=False, indent=1)
    print(len(manifest), 'sprites')
