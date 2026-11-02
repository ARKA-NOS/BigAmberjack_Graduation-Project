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

`BossHealthBarModule`은 보스의 `INewHealthModule.OnDead`를 구독합니다. 사망 이벤트를 받으면 생성한 UI 오브젝트를 **같은 콜백 안에서 SetActive(false)**로 끄고 Destroy를 예약합니다. 보스의 사망 애니메이션이나 제거 지연 시간이 남아 있어도 화면에는 남지 않습니다.

결과 화면은 SDW의 `RunEndedEvent`를 받아 열립니다. 보스가 살아 있는 플레이어 사망 상황에서도 발생하므로, 체력 바 모듈도 같은 기존 이벤트를 구독해 `PlayerDead`와 `BossCleared` 모두 UI를 제거합니다. 종료 이벤트 발행이나 결과 UI 코드는 수정하지 않았습니다. 종료를 받은 모듈은 `_closed`를 유지하여 재활성화해도 바를 다시 만들지 않습니다. 다음 탐험에 새로 생성되는 보스는 새 모듈이므로 정상 표시됩니다.

살아 있고 탐험도 끝나지 않은 상태에서 모듈을 껐다 켜면 현재 체력으로 새로 표시합니다. UI는 보스와 같은 씬에 속하므로 씬 언로드 때도 함께 정리됩니다. 비활성화·제거 때 체력 이벤트와 정적 EventBus 구독을 모두 해제합니다.

### 사망 이벤트의 실제 순서

현재 공용 `NewHealthModule.HealthDeltaEvent`는 **OnHealthChanged(HP 0) → OnDead → IsDead = true** 순서입니다. EnemyBase는 첫 체력 변경 콜백에서 자신의 사망 처리를 시작합니다. `OnDead` 콜백 안에서 공용 HealthModule.IsDead가 아직 false인 것을 실행으로 확인했으므로, UI 종료는 이 값을 다시 기다리지 않고 사망 이벤트 자체를 기준으로 확정합니다.

기존 바는 HP 변경과 LateUpdate로만 숨김을 판단했고, 모듈은 보스 사망 이벤트와 탐험 종료 이벤트를 직접 구독하지 않았습니다. 특히 보스가 생존한 상태로 탐험이 끝나면 표시 조건이 계속 참이어서 결과 창 위에 남을 수 있었습니다. 생성·제거 책임이 있는 BossHealthBarModule에서 이 연결을 보완했습니다.

현재 한 마리의 보스와 전투하는 구성입니다. 여러 보스를 동시에 생성하면 각자의 바가 같은 자리에 나타납니다. 다중 보스 목록 표시는 이번 범위에 포함하지 않았습니다.

## 직접 테스트하는 방법

1. 현재 작업 씬에 미저장 내용이 있다면 먼저 저장하고, `Prototypes/Bosses/BossPatternTest.unity`를 열어 Play합니다.
2. 화면 상단 중앙에 눈깔 보스 이름과 초록색 체력 바가 나타나는지 확인합니다. 보스 몸체에 붙어 있던 체력·경직 바는 없어야 합니다.
3. 왼쪽 테스트 패널의 **Deal 250 damage**를 누릅니다. 최대 HP 2500 기준으로 체력 바가 10% 줄어야 합니다.
4. **Deal 1500 damage (2500 -> 1000)**를 눌러 HP를 1000 이하로 만듭니다. 레이저 무적 구간에서 테두리가 흰색이 되고 오른쪽 아래에 무적이 표시되는지 확인합니다. 이때 피해 버튼을 눌러도 바가 줄어들지 않아야 합니다.
5. 레이저 3회와 중앙 복귀가 끝나면 무적 문구가 사라지고 테두리도 원래 색으로 돌아와야 합니다. 이후 **Deal 3000 damage**로 죽이면 UI가 사라집니다.
6. Game 뷰를 16:9와 4:3 등으로 바꿉니다. 가로 길이가 화면의 절반이며, 이름은 바 위, 무적 문구는 우측 아래인지 확인합니다. 4번부터 다시 보고 싶으면 Stop 후 Play합니다.
7. Play 중 보스 자식 HealthBarModule을 껐다 켜 봅니다. 껐을 때 사라지고, 켰을 때 현재 HP를 표시하는 바가 하나만 생성되어야 합니다. 이 테스트 중 변경한 값을 프리팹에 Apply할 필요는 없습니다.
8. 보스의 Death Despawn Delay를 Play 중 길게 두고 처치합니다. 보스 오브젝트는 남아 있어도 화면 체력 바는 사망 순간 꺼지고, 다음 프레임에 생성된 HealthBar 오브젝트가 제거돼야 합니다.
9. 실제 던전에서 보스가 살아 있는 채로 플레이어가 사망하여 결과 창이 열리는 경우에도 보스 체력 바가 없어야 합니다. 최종 보스 클리어로 열리는 결과 창도 동일하게 확인합니다.
10. 탐험 종료 뒤 남은 보스의 HealthBarModule을 껐다 켜도 UI가 재생성되지 않아야 합니다. 다음 탐험의 새 보스에서는 체력 바가 정상적으로 나타나야 합니다.

확인한 항목: 컴파일과 프리팹 참조, 1920×1080·1280×720·1024×768 배치, 한글 글리프, 실제 피해·회복, 실제 레이저 무적 진입/종료, 무적 중 피해 무시, 이름 변경, 보스 이동·스케일로부터 화면 위치 독립, 모듈/UI 재활성화, 사망·보스 제거·추가 씬 언로드 시 UI 정리입니다. 검증 도구는 `VerifyBossHealthBar.cs`이며 캡처는 확인 후 삭제합니다.

이번 사망 처리 수정에서는 `RunEdit`와 `RunDeathEvents`를 실행했습니다. 사망 콜백 안에서 즉시 비활성화, 5초 제거 지연보다 먼저 UI 제거, 살아 있는 보스에 PlayerDead/BossCleared 종료 이벤트 전달, 중복 이벤트, 모듈 재활성화, Start 전 종료 이벤트, 새 보스 생성, 씬 언로드, 정적 구독 수 복원을 확인했습니다. 오류는 0건입니다. 결과 화면 자체를 새로 생성하지 않고, 결과 화면과 같은 공용 이벤트 경로를 독립 씬에서 검증했습니다. 사용자 씬과 체력 바 프리팹의 배치는 수정하지 않았습니다.

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
| `EventBus<RunEndedEvent>` | 기존 제네릭 이벤트 버스를 구독해 결과 화면 오브젝트를 직접 참조하지 않고 탐험 종료를 감지합니다. 공용 이벤트 발행 코드는 그대로 사용합니다. |
| `bool _closed` | 사망/탐험 종료를 받은 사실을 기억합니다. 이후 OnEnable에서 CreateView를 호출해도 종료된 전투의 바가 다시 뜨지 않습니다. |
| `SetActive(false)` 다음 `Destroy` | Destroy는 실제 제거가 프레임 끝으로 미뤄집니다. 먼저 비활성화해서 결과 창이 열리는 프레임부터 표시·갱신을 중단합니다. |
| RectTransform 앵커와 CanvasScaler | 고정 픽셀 너비 대신 화면 비율을 유지하고 문자·높이도 해상도에 맞춥니다. |
| `Graphic.raycastTarget = false` | 체력 바와 문자가 게임의 클릭 입력을 가로채지 않게 합니다. |

새 인터페이스나 추상 클래스를 추가하지 않았고, 기존 BossBase·Module·INewHealthModule을 그대로 사용했습니다.
