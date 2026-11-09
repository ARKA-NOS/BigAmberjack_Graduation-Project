# 적 피격 카메라 쉐이크

작업일: 2026-11-09

## 적용 결과

실제 HP가 감소한 타격을 받으면 적의 `EnemyHitCameraShakeModule`이 짧은 Cinemachine Impulse를 발생시킨다. 일반 타격은 약하게, `DamageData.IsCritical == true`인 타격은 더 강하고 조금 더 길게 흔들린다. 크리티컬 확률과 데미지 배율은 계산하지 않는다.

Enemy01, Enemy02, Enemy04, Catto, MadGhost, RobotEyeBoss 프리팹에 공통 모듈 프리팹을 자식으로 연결했다. 새 적에도 이 프리팹을 자식으로 하나 추가하면 된다. 별도 매니저나 카메라 Transform 조작은 필요 없다.

## 인스펙터 설정

공통 기본값을 바꾸는 위치: `Assets/_Prefabs/Enemies/Effects/EnemyHitCameraShakeModule.prefab`의 **EnemyHitCameraShakeModule** 컴포넌트. 특정 적만 다르게 하려면 해당 적 프리팹 안의 모듈에서 값을 Override한다.

| 항목 | 기본값 | 설명 |
| --- | --- | --- |
| Normal Strength | 0.06 | 일반 타격의 흔들림 강도. 0이면 일반 타격 쉐이크를 끈다. |
| Normal Duration | 0.12초 | 일반 타격의 흔들림 길이. |
| Critical Strength | 0.12 | 크리티컬 흔들림 강도. 0이면 크리티컬 쉐이크를 끈다. |
| Critical Duration | 0.18초 | 크리티컬 흔들림 길이. |
| Use Hit Direction | 켜짐 | 피격 방향으로 밀렸다 돌아오는 방향성 있는 흔들림. |
| Fallback Direction | (1, 0.3) | 방향이 없거나 Use Hit Direction을 껐을 때의 방향. 길이는 정규화한다. |
| Impulse Channel | 1 | 카메라 Listener가 수신해야 하는 채널. |
| Full Strength Radius | 12 | 적에서 카메라까지 이 거리 안에서는 전체 강도로 적용. |
| Fade Distance | 8 | 위 반경 밖에서 추가로 이 거리만큼 멀어지면 강도가 0이 된다. |

강도는 월드 유닛 기준 신호 배율이다. Listener의 `Gain`과 화면 확대 정도에 따라 화면에서 느껴지는 크기는 달라진다. 현재 기본 카메라 Gain은 1이다. `Bump` 곡선으로 잠깐 반동이 생겼다가 원점으로 돌아오며, Z축 이동이나 회전은 넣지 않는다.

현재 카메라의 Impulse 합산 방식은 Additive다. 여러 적을 한 번에 타격하거나 보스 레이저와 타격이 겹치면 신호가 더해질 수 있다. 기본값은 한 번의 일반 타격 기준으로 약하게 설정했다.

## 연결과 수명

`NewHealthModule`의 실제 HP 감소 → `EnemyBase.DamageReceived` → `EnemyHitCameraShakeModule` → Cinemachine Impulse → 기존 PlayerCam의 Impulse Listener 순서로 동작한다.

- 모듈은 기존 피격 이벤트를 구독하므로 HP 계산, AI, 데미지 인디케이터와 독립적으로 켜거나 제거할 수 있다.
- 피해가 0·음수·NaN이거나 회복일 때는 신호를 보내지 않는다. 보스의 무적 처리로 HP 감소가 거절된 경우에도 피격 이벤트가 나오지 않으므로 흔들림이 없다.
- 모듈을 끄면 새로운 피격 신호는 나오지 않는다. 이미 발행한 짧은 신호는 남은 시간 동안 자연스럽게 끝난다. 적 사망·파괴도 마지막 타격의 흔들림을 갑자기 끊지 않는다.
- 활성화/비활성화 및 재초기화 시 구독을 정리하여 같은 타격을 중복 수신하지 않는다.
- 타격마다 별도 ImpulseDefinition을 만들어 다음 타격의 강도·길이 변경이 이전 신호에 섞이지 않도록 했다. Impulse 이벤트의 수명과 재사용은 Cinemachine의 기존 관리자가 맡는다.
- 보스 레이저는 기존 채널 64, 피격은 채널 1이다. 레이저 코드나 카메라 프리팹을 수정하지 않았다.

## 테스트 환경과 방법

환경: Unity 6000.3.22f1, 프로젝트에 이미 설치된 Cinemachine 3.1.7, 기존 PlayerCam과 적 프리팹.

1. 기존 Player/PlayerCam을 사용하는 테스트 씬에서 Enemy01 등을 배치하고 Play한다. 새 기능 때문에 씬이나 공용 카메라 프리팹을 저장할 필요는 없다.
2. 적을 평타로 공격한다. HP가 줄어드는 시점마다 화면이 약하게 흔들리고 약 0.12초 뒤 안정되어야 한다. 마지막 일격에도 반응해야 한다.
3. Hierarchy에서 적의 `EnemyHitCameraShakeModule` 자식을 선택한다. 컴포넌트 메뉴의 `Preview > Normal Shake (Play Mode, No Damage)`와 `Preview > Critical Shake (Play Mode, No Damage)`를 실행해 비교한다. 이 메뉴는 피해를 주지 않고 연출만 미리 재생한다. 크리티컬이 더 강하고 조금 더 길어야 한다.
4. Play 중 Normal/Critical Strength와 Duration을 바꾸어 원하는 감도로 맞춘다. 플레이 중 변경은 종료 시 되돌아가므로, 값을 기록한 뒤 Edit 모드에서 공통 모듈 프리팹이나 특정 적의 Override로 저장한다.
5. 모듈 컴포넌트를 끄고 공격하면 HP·기존 피격 연출은 작동하되 새로운 카메라 쉐이크는 없어야 한다. 다시 켜면 한 번씩 정상 반응해야 한다.

흔들리지 않는다면 실제 사용 중인 Cinemachine 카메라의 **Cinemachine Impulse Listener**에서 활성화 여부, `Channel Mask`의 채널 1 수신, `Gain > 0`, `Use 2D Distance`를 확인한다. 현재 PlayerCam에는 이미 설정되어 있다. 보스 전용 채널 64만 받는 독립 테스트 카메라에서는 피격 채널 1이 수신되지 않는다. 공용 카메라 파일은 허용 영역 밖이므로 자동으로 수정하지 않는다.

현재 일반 공격이 `IsCritical`을 설정하지 않으면 자동으로 일반 연출만 나온다. 나중에 공격 측에서 `IsCritical = true`를 전달하면 같은 모듈이 크리티컬 연출을 선택한다. 피격 모듈이 크리티컬 공격을 만들어내지는 않는다.

## 검증 결과

- Unity 컴파일: 오류 0, 경고 0.
- 여섯 적 프리팹: 모듈이 각각 하나, 활성 상태, Missing Script 없음, PlayerCam 채널 1 수신 확인.
- 독립 Play 씬에서 실제 Enemy01의 ApplyDamage와 기존 PlayerCam 프리팹을 연결해 검증했다. 신호 중간 지점의 카메라 보정량은 일반 약 0.0591유닛, 크리티컬 약 0.1181유닛이었다. 설정된 길이는 각각 0.12/0.18초다.
- 일반/크리티컬 모두 피해 2를 넣었을 때 HP 감소는 각각 2였다.
- 회복·무효 피해·비활성 모듈·강도 0에서 미발행, 재활성화/재초기화 후 중복 구독 없음, 방향값 오류 시 대체 방향, 거리 감쇠, 채널 64 분리, 적 파괴 뒤 마지막 일격 재생 및 자동 종료를 확인했다.
- 테스트 전의 저장된 Proto_MainHome 씬으로 복귀했다. 씬 파일은 수정하지 않았다.

검증 보조 파일은 `Tools~/CTJ/VerifyHitCameraShake.cs`다. `CheckAssets`, `BeginPlay`, `RunPlay`, `StopPlay`, `RestoreScene` 순서로 Unity CLI에서 실행할 수 있다. 시작 시 미저장 씬이면 진행을 거부한다. 검사에 사용하는 시간 고정과 Impulse 초기화는 독립 테스트 씬 안에서만 실행되고, 게임 모듈에는 포함되지 않는다.

## 사용한 문법과 이유

- `Module` 상속과 `override Initialize(ModuleOwner owner)`: 기존 모듈 초기화 흐름을 그대로 사용해 자기 EnemyBase를 찾는다. 새로운 인터페이스는 필요하지 않았다.
- `owner as EnemyBase`: 실제 소유자가 적이면 참조를 얻고, 아니면 null을 반환한다. 다른 종류의 소유자에 잘못 연결했을 때 강제 형변환 예외를 피한다.
- 이벤트 `+=` / `-=`: EnemyBase가 확정한 피격 결과에만 반응한다. `OnEnable`, `OnDisable`, `OnDestroy`와 구독 여부 플래그를 사용해 중복 등록과 해제 누락을 막는다.
- `[SerializeField] private`, `[Min]`, `[Tooltip]`, `[Header]`: 외부 코드가 설정값을 직접 바꾸는 통로는 줄이면서 인스펙터에서 강도·길이·거리 등을 조절한다. Min은 편집 편의를 위한 제한이므로 실행 시에도 유효성을 검사한다.
- 조건 연산자 `isCritical ? ... : ...`: 같은 재생 경로에서 일반/크리티컬 설정만 선택한다. 연출 코드를 두 벌로 복사하지 않는다.
- 객체 초기화식 `new CinemachineImpulseDefinition { ... }`: 이번 타격에 필요한 채널·모양·길이를 한 곳에서 구성한다. 타격마다 별도 객체이므로 재생 중인 이전 신호 설정을 바꾸지 않는다.
- `Vector2.normalized`: 방향 벡터 길이에 따라 흔들림이 커지지 않게 하고, 강도 필드가 크기를 결정하도록 한다.
- `float.IsNaN` / `float.IsInfinity`: 잘못된 피해나 방향이 카메라 좌표로 전파되는 것을 막는다.
- `[ContextMenu]`와 `#if UNITY_EDITOR`: 별도 테스트 UI 없이 Play 모드에서 두 연출을 비교한다. 편집기 전용 미리보기 메서드는 빌드에 포함하지 않는다.
