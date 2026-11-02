# 보스 화면 체력 바

2026-11-02. 수정 범위는 Assets/CTJ입니다. 일반 적의 EnemyTemporaryStatusUI에서 초록색 Filled Image와 어두운 배경을 가져와 보스 전용 화면 UI를 만들었습니다. 기존 일반 적 UI는 유지하고 RobotEyeBoss의 작은 체력·경직 바만 교체했습니다.

## 구성과 조정 위치

- `Prototypes/Bosses/RobotEyeBoss.prefab`의 루트 `RobotEyeBoss > Display Name`: 표시 이름입니다. 기본값은 **눈깔 보스**이며, 공백이면 보스로 표시합니다.
- 보스 자식 `HealthBarModule`: `BossHealthBarModule`이 화면 UI를 생성하고 제거합니다. `Health Bar Prefab`에 `BossHealthBarCanvas`가 연결되어 있습니다.
- `Prototypes/Bosses/BossHealthBarCanvas.prefab`: 기본 Canvas UI 프리팹입니다. Screen Space - Overlay이고 정렬 순서는 100입니다. UI Toolkit을 사용하지 않습니다.
- `Panel`의 RectTransform: Anchor Min X=0.25, Max X=0.75, Width=0으로 **화면 가로의 50%**를 사용합니다. 위쪽을 기준으로 가운데 배치합니다. Pos Y=-28을 바꾸면 전체 UI의 상단 간격을 조절할 수 있습니다.
- `Panel/Frame`: 체력 바 높이 26, 안쪽 여백 3입니다. `BossHealthBarUI > Normal Border Color`가 평상시 테두리 색입니다. 무적 중에는 흰색으로 표시합니다. 내부 배경은 불투명해서 흰 테두리 색이 안쪽까지 비치지 않습니다.
- `Panel/BossName`: 바 위에 이름을 표시합니다. `Panel/InvulnerableLabel`: 바의 우측 하단에 **무적**을 표시합니다. 경직 표시는 없습니다.
- CanvasScaler 기준 해상도는 1920×1080, Match는 0.5입니다. 바 너비는 앵커로 정하고, 높이·문자·여백은 해상도에 맞추어 함께 조절합니다.

한글은 기존 `Assets/Lrw/Asset/UI/TitleUI/Font/AmberjackPixel.ttf`를 참조합니다. 해당 파일을 수정하거나 새 폰트·패키지를 설치하지 않았습니다.

## 동작

`BossHealthBarModule`은 다른 모듈들의 초기화가 끝난 Start에서 UI를 생성합니다. UI는 보스의 자식으로 생성하지 않고 같은 씬의 루트에 둡니다. 그래서 보스가 이동하거나 크기가 바뀌어도 화면 위치는 고정됩니다.

`BossHealthBarUI`는 보스의 `INewHealthModule.OnHealthChanged`를 구독해서 체력을 갱신합니다. 피해뿐 아니라 회복도 같은 경로를 사용합니다. 무적 표시에는 `BossBase.IsInvulnerable`을 사용합니다. 현재 눈깔 보스의 실제 레이저 무적 구간과 연동되며, HP가 바뀌지 않아도 무적 종료를 반영합니다. UI 자체가 무적을 부여하지는 않습니다.

보스가 죽으면 즉시 숨기고, 비활성화·제거되면 생성한 Canvas도 제거합니다. 다시 활성화하면 현재 체력으로 새로 표시합니다. UI를 보스와 같은 씬에 소속시켜 추가 로드한 씬이 종료될 때도 함께 정리됩니다. 표시 모듈만 끄거나 삭제하면 전투 로직을 바꾸지 않고 UI를 제거할 수 있습니다.

현재 한 마리의 보스와 전투하는 구성입니다. 여러 보스를 동시에 생성하면 각자의 바가 같은 자리에 나타납니다. 다중 보스 목록 표시는 이번 범위에 포함하지 않았습니다.

## 직접 테스트하는 방법

1. 현재 작업 씬에 미저장 내용이 있다면 먼저 저장하고, `Prototypes/Bosses/BossPatternTest.unity`를 열어 Play합니다.
2. 화면 상단 중앙에 눈깔 보스 이름과 초록색 체력 바가 나타나는지 확인합니다. 보스 몸체에 붙어 있던 체력·경직 바는 없어야 합니다.
3. 왼쪽 테스트 패널의 **Deal 250 damage**를 누릅니다. 최대 HP 2500 기준으로 체력 바가 10% 줄어야 합니다.
4. **Deal 1500 damage (2500 -> 1000)**를 눌러 HP를 1000 이하로 만듭니다. 레이저 무적 구간에서 테두리가 흰색이 되고 오른쪽 아래에 무적이 표시되는지 확인합니다. 이때 피해 버튼을 눌러도 바가 줄어들지 않아야 합니다.
5. 레이저 3회와 중앙 복귀가 끝나면 무적 문구가 사라지고 테두리도 원래 색으로 돌아와야 합니다. 이후 **Deal 3000 damage**로 죽이면 UI가 사라집니다.
6. Game 뷰를 16:9와 4:3 등으로 바꿉니다. 가로 길이가 화면의 절반이며, 이름은 바 위, 무적 문구는 우측 아래인지 확인합니다. 4번부터 다시 보고 싶으면 Stop 후 Play합니다.
7. Play 중 보스 자식 HealthBarModule을 껐다 켜 봅니다. 껐을 때 사라지고, 켰을 때 현재 HP를 표시하는 바가 하나만 생성되어야 합니다. 이 테스트 중 변경한 값을 프리팹에 Apply할 필요는 없습니다.

확인한 항목: 컴파일과 프리팹 참조, 1920×1080·1280×720·1024×768 배치, 한글 글리프, 실제 피해·회복, 실제 레이저 무적 진입/종료, 무적 중 피해 무시, 이름 변경, 보스 이동·스케일로부터 화면 위치 독립, 모듈/UI 재활성화, 사망·보스 제거·추가 씬 언로드 시 UI 정리입니다. 검증 도구는 `VerifyBossHealthBar.cs`이며 캡처는 확인 후 삭제합니다.

## 사용한 문법과 이유 — 학습용

| 문법 또는 Unity 기능 | 사용 이유 |
| --- | --- |
| `[SerializeField] private` | 외부 코드가 필드를 직접 바꾸는 것은 막으면서, 이름·색·프리팹을 인스펙터에서 설정합니다. |
| `public string DisplayName => ...` | 읽기 전용 프로퍼티입니다. UI는 이름을 읽고, 기본값 처리도 보스 한 곳에서 담당합니다. `string.IsNullOrWhiteSpace`는 빈 문자열과 공백 이름을 함께 검사합니다. |
| `Module` 상속과 `override Initialize` | 기존 프로젝트의 모듈 초기화에 참여합니다. `owner as BossBase`로 보스 소유자일 때만 연결하며 새 모듈 관리 체계는 만들지 않습니다. |
| `Start`와 `OnEnable`의 구분 | 첫 생성은 모든 모듈의 AfterInit이 끝난 Start에서 처리합니다. 이후 활성화에는 OnEnable을 사용하며 `_started`로 초기화 전 접근을 막습니다. |
| `+=` / `-=` 이벤트 구독·해제 | 체력 변경 때만 fillAmount를 갱신합니다. 비활성화·제거 시 해제해서 오래된 UI가 이벤트를 받지 않도록 합니다. 다시 켜질 때 현재 체력도 읽습니다. |
| `Mathf.Clamp01(current / max)` | Image.fillAmount가 요구하는 0~1 비율로 제한합니다. 최대 HP가 0이면 나누지 않고 0을 사용합니다. |
| `LateUpdate` | 무적은 체력 이벤트 없이도 켜지고 꺼집니다. 보스가 그 프레임의 상태를 갱신한 뒤 실제 무적 여부와 표시를 맞춥니다. |
| `Instantiate`와 `SceneManager.MoveGameObjectToScene` | UI가 보스의 Transform을 상속받지 않는 루트 Canvas가 되게 하면서, 보스의 씬과 수명은 함께 유지합니다. |
| `OnDisable` / `OnDestroy` | 화면 숨김, 이벤트 해제, 생성된 UI 제거에 사용합니다. 이미 정리한 참조는 null 검사로 중복 처리를 막습니다. |
| RectTransform 앵커와 CanvasScaler | 고정 픽셀 너비 대신 화면 비율을 유지하고 문자·높이도 해상도에 맞춥니다. |
| `Graphic.raycastTarget = false` | 체력 바와 문자가 게임의 클릭 입력을 가로채지 않게 합니다. |

새 인터페이스나 추상 클래스를 추가하지 않았고, 기존 BossBase·Module·INewHealthModule을 그대로 사용했습니다.
