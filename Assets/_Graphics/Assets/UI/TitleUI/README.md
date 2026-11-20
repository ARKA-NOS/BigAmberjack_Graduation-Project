# 칭호 UI 에셋

칭호 달성도 창(C안, 펼침 목록)에 쓰는 스프라이트와 픽셀 폰트.

| 폴더 | 내용 |
|---|---|
| `Sprites/` | UI 스프라이트 30장. 임포트 설정은 `.meta`에 들어 있다. |
| `Font/AmberjackPixel.ttf` | 16px 한글 픽셀 폰트. 한글 11,172자, 한글 자모, 영문·숫자·기호. |
| `Tools~/` | 스프라이트와 폰트를 만든 파이썬 스크립트. 이름이 `~`로 끝나서 Unity는 이 폴더를 가져오지 않는다. |

## 스프라이트

아트 1픽셀이 텍스처 1픽셀이다. 1920×1080 화면에서는 **6배**(아트 1픽셀 = 6px)로 키워서 쓴다.

- Filter Mode Point, 압축 없음, 밉맵 없음, Pixels Per Unit 100(기본값), Mesh Type Full Rect로 설정되어 있다.
- 크기와 위치를 6px 단위로 맞추면 픽셀 크기가 고르게 보인다.
- 흰색 아이콘은 색을 입혀서 쓴다(uGUI는 Image Color, UI Toolkit은 `-unity-background-image-tint-color`).

| 스프라이트 | 텍스처 | 9-slice | 1920×1080 크기 | 용도 |
|---|---|---|---|---|
| `TitleUI_Window` | 24×24 | 8 | 자유 | 창 틀. 네 모서리에 금색 리벳이 있고, 오른쪽 위 리벳은 닫기 버튼에 가려진다. |
| `TitleUI_ListPanel` | 8×8 | 2 | 자유 | 칭호 목록 영역 |
| `TitleUI_Row_Normal` | 8×8 | 2 | 자유 | 칭호 줄: 시작 전, 진행 중 |
| `TitleUI_Row_Open` | 8×8 | 2 | 자유 | 칭호 줄: 펼침 |
| `TitleUI_Row_Done` | 8×8 | 2 | 자유 | 칭호 줄: 달성 (펼쳐도 이 스프라이트 유지) |
| `TitleUI_Row_Locked` | 8×8 | 2 | 자유 | 칭호 줄: 잠김 |
| `TitleUI_Bar_Frame` | 8×8 | 2 | 높이 48 | 칭호 진행 바 틀 (안쪽 높이 36) |
| `TitleUI_Bar_Fill` / `_Done` | 4×6 | - | 높이 36 | 칭호 진행 바 채움. 너비 = 안쪽 너비 × 진행도 |
| `TitleUI_QuestBar_Frame` | 6×4 | 좌우 2, 위아래 1 | 높이 24 | 퀘스트 진행 바 틀 (안쪽 높이 12) |
| `TitleUI_QuestBar_Fill` / `_Done` | 2×2 | - | 높이 12 | 퀘스트 진행 바 채움 |
| `TitleUI_CheckBox_Empty` / `_Done` / `_Locked` | 12×12 | - | 72×72 | 칭호 줄 오른쪽 달성 칸 |
| `TitleUI_QuestCheck_Empty` / `_Done` / `_Locked` | 8×8 | - | 48×48 | 퀘스트 체크박스 |
| `TitleUI_CloseButton_Normal` / `_Hover` / `_Pressed` | 18×18 | - | 108×108 | 닫기 버튼 |
| `TitleUI_Arrow_Down` / `_Up` | 7×4 | - | 42×24 | 펼치기/접기 화살표 (흰색) |
| `TitleUI_Icon_Check` | 8×7 | - | 48×42 | 체크 아이콘 (흰색) |
| `TitleUI_Icon_Lock` | 8×7 | - | 48×42 | 자물쇠 아이콘 (흰색) |
| `TitleUI_Icon_Lock_Small` | 5×5 | - | 30×30 | 글줄 안에 넣는 작은 자물쇠 (흰색) |
| `TitleUI_Icon_Close` | 8×8 | - | 48×48 | X 아이콘 (흰색) |
| `TitleUI_Scrollbar_Track` / `_Handle` | 4×4 | 1 | 너비 24 | 스크롤바 바탕과 손잡이 |
| `TitleUI_DotTile` | 2×2 | - | 12×12 반복 | 창 뒤 배경 점무늬. Wrap Mode가 Repeat다. |

### UI Toolkit (USS)

9-slice는 표의 값을 `-unity-slice-*`에 정수로 넣고, `-unity-slice-scale: 6px`로 6배를 준다. 통짜 이미지는 요소 크기를 표의 1920×1080 크기로 맞춘다.

```css
.title-row {
    background-image: url("/Assets/Lrw/Asset/UI/TitleUI/Sprites/TitleUI_Row_Normal.png");
    -unity-slice-left: 2;
    -unity-slice-right: 2;
    -unity-slice-top: 2;
    -unity-slice-bottom: 2;
    -unity-slice-scale: 6px;
}

.title-row--done {
    background-image: url("/Assets/Lrw/Asset/UI/TitleUI/Sprites/TitleUI_Row_Done.png");
}

.title-check {
    width: 72px;
    height: 72px;
    background-image: url("/Assets/Lrw/Asset/UI/TitleUI/Sprites/TitleUI_CheckBox_Empty.png");
}

.title-backdrop {
    background-color: #0B0910;
    background-image: url("/Assets/Lrw/Asset/UI/TitleUI/Sprites/TitleUI_DotTile.png");
    background-repeat: repeat;
    background-size: 12px 12px;
}
```

- Panel Settings의 Reference Sprite Pixels Per Unit을 바꾸지 않았다면(기본 100) 스프라이트로 넣어도 텍스처로 넣어도 배율이 같다.
- 닫기 버튼의 마우스 올림과 누름은 `:hover`, `:active`에서 `background-image`를 `_Hover`, `_Pressed`로 바꾼다.

### uGUI (Image)

- 9-slice 스프라이트는 Image Type을 `Sliced`로 하고 Pixels Per Unit Multiplier를 `0.1666667`(1/6)로 한다.
- 진행 바 채움은 Image Type `Filled`, Fill Method `Horizontal`로 하고 `fillAmount`에 진행도를 넣는다.
- 닫기 버튼은 Button의 Transition을 `Sprite Swap`으로 하고 Highlighted에 `_Hover`, Pressed에 `_Pressed`를 넣는다.
- 배경 점무늬는 Image Type `Tiled`, Pixels Per Unit Multiplier `0.1666667`.

### 상태별 조합

| 칭호 상태 | 줄 | 바 채움 | 오른쪽 칸 | 글자색 |
|---|---|---|---|---|
| 시작 전, 진행 중 | `Row_Normal` (펼치면 `Row_Open`) | `Bar_Fill` | `CheckBox_Empty` | 이름·퍼센트 `#F1E7CF` |
| 달성 | `Row_Done` | `Bar_Fill_Done` | `CheckBox_Done` | 이름 `#FFE7A8`, 퍼센트 `#F2C046` |
| 잠김 | `Row_Locked` | 없음 | `CheckBox_Locked` | `#8F849C`, 퍼센트 자리에 `?%` |

퀘스트 행은 완료하면 `QuestCheck_Done`과 `QuestBar_Fill_Done`으로 바꾸고, 이름을 `#CBBFA6`, 수치를 `#F2C046`으로 바꾼다.

### 스프라이트 없이 단색으로 칠하는 곳

| 위치 | 색 |
|---|---|
| 펼친 영역 바탕 | `#15111A` |
| 퀘스트 행 바탕 | `#1F1926` |
| "달성 효과" 라벨 바탕 | `#2F2639` |
| 화살표 | `#D9CFE4`, 잠김 `#5C516A` |
| 닫기 X 아이콘만 따로 쓸 때 | `#FFF3E0` |
| 보조 글자 | `#A99DB5` |

## 폰트 (Amberjack Pixel)

16px 격자로 만든 폰트라 글자 크기를 16의 배수(16, 32, 48, 64)로 쓰면 픽셀이 고르게 나온다.

| 위치 | 1920×1080 크기 |
|---|---|
| 창 제목 "칭호" | 64 (검은 그림자를 아래·오른쪽으로 4px) |
| 칭호 이름 | 48 |
| 퍼센트, 퀘스트 이름 | 32 |
| 설명 | 16 (작게) 또는 32 |

픽셀이 번지지 않게 하려면 폰트 에셋의 Render Mode를 `RASTER_HINTED`, Sampling Point Size를 `16`, Padding을 `1`로 만든다.

### UI Toolkit

1. Project 창에서 `AmberjackPixel.ttf`를 우클릭하고 `Create > Text > Font Asset`으로 폰트 에셋을 만든다.
2. 만든 폰트 에셋의 Inspector에서 Generation Settings를 Sampling Point Size `16`, Padding `1`, Render Mode `RASTER_HINTED`, Atlas `2048 × 2048`로 바꾸고, `Multi Atlas Textures`를 켠 뒤 Apply한다.
3. USS에서 `-unity-font-definition: url("/Assets/Lrw/Asset/UI/TitleUI/Font/<만든 에셋 이름>.asset");`로 지정한다.

### TextMeshPro (uGUI)

`Window > TextMeshPro > Font Asset Creator`에서 Source Font File `AmberjackPixel`, Sampling Point Size Custom `16`, Padding `1`, Atlas Resolution `2048 × 2048`, Render Mode `RASTER_HINTED`, Character Set `Unicode Range (Hex)`에 아래 범위를 넣고 Generate한 뒤 `Font/`에 저장한다.

```
20-7E,A0,B0,B7,D7,2018-201D,2022,2026,2190-2193,25B2-25BD,3000-3002,300C-300F,3131-3163,AC00-D7A3
```

글자가 빠졌다고 나오면 Atlas Resolution을 `4096 × 4096`으로 올린다.

16의 배수가 아닌 배율로 화면이 커지는 경우(예: 1366×768) 가장자리가 고르지 않으면, Render Mode를 `SDFAA`, Sampling Point Size를 `64`, Padding을 `6`으로 다시 만든다. 모서리가 살짝 둥글어지는 대신 어떤 배율에서도 깨끗하다.

## 다시 만들기 (`Tools~`)

파이썬 3, Pillow, numpy가 필요하다.

```bash
python "Tools~/font/build_font.py" AmberjackPixel.ttf
python "Tools~/sprites/sprites.py" out
python "Tools~/sprites/write_meta.py" out
```

- 한글 자모 모양과 배치는 `font/hangul.py`, 영문·숫자·기호 모양은 `font/latin.py`에 있다.
- 스프라이트 색과 모양은 `sprites/sprites.py`에 있다.
- `write_meta.py`는 GUID를 새로 만든다. 이미 프로젝트에 있는 스프라이트를 갱신할 때는 PNG만 덮어쓰고 기존 `.meta`는 그대로 둔다. 그래야 씬과 프리팹의 참조가 유지된다.
