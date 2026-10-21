# 총 처치 수와 보스 가감속 — 2026-10-21

## 구조와 적용 위치

`EnemyBase.Die()` → 해당 적의 `EnemyDeathModule.ReportDeath()` → `EnemyKillCountManager`의 총합 +1.

- Catto, MadGhost, RobotEyeBoss 프리팹에 `DeathModule` 자식을 각각 하나씩 추가했습니다. 모듈은 기존 ModuleOwner 초기화 경로를 따릅니다.
- 사망 연출, 콜라이더 정지, 제거 예약은 기존 EnemyBase에 그대로 남깁니다. DeathModule의 책임은 사망 집계 전달과 중복 방지입니다. 재화 보상 모듈과 독립적입니다.
- 매니저는 `Assets/CTJ/Prototypes/Prefabs/EnemyKillCountManager.prefab`입니다. CTJ_MapTestScene, EnemyArtTest, BossPatternTest에 독립 루트로 하나씩 배치했습니다.
- 매니저는 공용 `AbstractManager`를 상속하고 기존 `ServiceLocator`로 조회합니다. 공용 GameManager 코드나 프리팹은 수정하지 않았습니다. 별도 인터페이스, 태그, 몹 식별 SO는 추가하지 않았습니다.

## 필수 함수와 사용 예

```csharp
using CTJ.Enemies;
using Lrw.Script._Core._ServiceLocator;

EnemyKillCountManager counter = ServiceLocator.Get<EnemyKillCountManager>();
if (counter != null)
{
    int total = counter.GetKillCount(); // 현재 총 처치 수 조회
    counter.ResetKillCount();          // 명시적으로 0으로 초기화
}
```

`Initialize()`는 등록을 담당하며 **처치 수를 초기화하지 않습니다**. GameManager 초기화와 OnEnable이 겹쳐 호출되어도 초기화되지 않습니다. 총합을 지우려면 `ResetKillCount()`를 호출합니다.

### 집계 규칙

- 적 종류나 일반/엘리트/보스 구분 없이, DeathModule을 가진 적의 실제 사망 한 번당 +1입니다. 보스도 +1입니다.
- 현재는 최종 타격자 구분 없이 HP 0으로 확정된 사망을 셉니다. 공격자별 통계는 추가하지 않았습니다.
- 반복 피해, 반복 HP 0 콜백, 사망 후 늦은 알림은 중복 집계하지 않습니다. Reset 이후에도 이미 죽은 적의 알림으로 다시 증가하지 않습니다.
- 생존 적의 비활성화, 강제 삭제, 씬 언로드는 처치가 아닙니다. 사망 애니메이션이 끝나기를 기다리지 않고 사망 확정 시 집계합니다.
- DeathModule을 꺼 둔 적은 집계하지 않습니다. 모듈은 적의 수명 동안 한 번만 알리며, 현재 Destroy 방식에 맞췄습니다. 풀링/부활 초기화는 이번 범위에 포함하지 않았습니다.
- 매니저는 적 생성 때가 아니라 사망 때 조회합니다. 죽기 전까지 매니저가 준비되어 있으면 됩니다.
- 활성 매니저가 없다면 경고 후 집계를 생략하지만 사망과 재화 처리는 계속됩니다. 누락분을 나중에 재집계하지 않습니다.
- 활성 매니저는 하나만 허용합니다. 중복 컴포넌트는 경고 후 비활성화하며, 먼저 등록된 매니저의 수치를 덮어쓰지 않습니다.

### 수명과 GameManager 통합

현재 수치는 **매니저 인스턴스가 살아 있는 동안** 유지합니다. 비활성화/재활성화 및 재등록은 수치를 유지합니다. 디스크 저장, 자동 생성, 독자적인 DontDestroyOnLoad, 자동 런 시작 감지는 넣지 않았습니다.

씬 교체로 독립 매니저가 파괴되고 새 매니저가 생성되면 0부터 시작합니다. 기존 씬을 유지한 채 방 오브젝트만 교체하면 유지됩니다. 추후 GameManager로 통합할 때는 이 프리팹을 GameManager의 자식으로 옮기면 기존 Initialize/GameEnd 경로를 사용하며 부모의 수명을 따릅니다. 이때 각 씬의 독립 매니저는 함께 남겨 두지 말고, 런 시작 시점에서 ResetKillCount를 호출하도록 연결하세요.

## 보스 이동

FlightModule의 `Ease Duration = 0.2`가 최고 속도까지의 가속 시간이며 감속에도 동일하게 사용됩니다.

- 등장/중앙 복귀/레이저 준비 위치 이동은 최고 속도 4, 플레이어 방향 이동은 8, 레이저 횡단은 6입니다.
- 모든 정상 이동에서 가속 → 필요하면 등속 → 감속 순서로 이동합니다. 거리가 짧으면 최고 속도를 낮춰 등속 구간을 생략합니다.
- 경고 때 저장한 목적지와 아레나 제한은 바꾸지 않았습니다.
- 가감속 때문에 전체 이동 시간은 기존 등속 이동보다 조금 늘어납니다. 충분히 긴 거리에서는 `거리 / 최고 속도 + Ease Duration`입니다. 예를 들어 8유닛을 최고 속도 8로 이동하면 약 1.2초입니다.
- 도착할 때 정확한 목적지 좌표를 확정하고 FSM을 진행합니다. 사망/타깃 유실/비활성화의 Stop은 안전을 위해 즉시 정지합니다.
- 레이저의 편도 3회, 횡단당 1회 피해, 이동 구간을 포함하는 충돌 판정은 유지합니다.

## 사용한 문법과 이유

### 기존 추상 클래스 상속과 override

`EnemyKillCountManager : AbstractManager`와 `Initialize()`, `GameEnd()` 재정의는 이미 프로젝트에 존재하는 매니저 규칙에 맞추기 위한 것입니다. 새로운 추상 클래스를 만들지 않았습니다. 독립 사용 때는 OnEnable이 Initialize를 호출합니다.

### 제네릭 서비스 조회

`ServiceLocator.Get<EnemyKillCountManager>()`의 `<...>`는 조회할 타입입니다. 기존 Currency 계열과 같은 등록소를 재사용하므로 적마다 씬의 매니저 참조를 수동으로 꽂거나 별도 싱글턴을 만들 필요가 없습니다. 종료 시에는 현재 등록된 것이 자기 자신일 때만 해제합니다.

### internal, private와 읽기 함수

모듈의 ReportDeath와 매니저의 RegisterDeath는 `internal`로 같은 어셈블리 안의 사망 처리에서만 사용하도록 범위를 줄였습니다. 카운트 필드는 `private`이며 외부에서는 GetKillCount/ResetKillCount를 사용합니다. `=>`는 한 줄짜리 메서드의 축약 표현입니다.

### bool 중복 방지와 조기 return

`_reported`를 전달 전에 true로 설정합니다. 중복 콜백이 발생해도 다시 집계되지 않습니다. EnemyBase의 IsDead 검사와 역할을 나누어, 모듈 단독 호출도 방어합니다.

### Unity 속성과 생명주기

`[DisallowMultipleComponent]`는 같은 GameObject에 같은 컴포넌트를 중복 추가하지 못하게 합니다. 서로 다른 자식 오브젝트의 중복까지 막지는 않으므로, 프리팹의 DeathModule은 하나만 배치합니다. `[DefaultExecutionOrder(-10)]`은 일반 적 Awake보다 매니저 등록을 앞당깁니다. `[ContextMenu]`는 테스트용 로그·초기화 메뉴를 Inspector에 제공합니다.

### 가속도, 제곱근과 Lerp

가속 구간의 이동 거리는 `0.5 × 가속도 × 시간²`로 계산합니다. 감속 구간은 목적지에서 남은 거리를 빼서 같은 원리를 적용합니다. `Mathf.Sqrt`는 짧은 거리에서 도달 가능한 최고 속도와 가속 시간을 구하는 데 사용합니다.

`Vector2.Lerp(출발점, 목적지, 이동비율)`은 누적 이동 거리의 비율을 실제 좌표로 바꿉니다. **매 프레임 현재 위치를 출발점으로 삼는 Lerp가 아닙니다.** 정해진 이동 시간이 끝나면 정확히 목적지에 도착하므로 도착 판정이 끝없이 지연되지 않습니다. `Time.fixedDeltaTime`을 사용해 기존 물리 틱과 맞췄습니다.

## 직접 테스트

### 처치 수

1. CTJ_MapTestScene을 Play하고 루트의 EnemyKillCountManager를 선택합니다.
2. 컴포넌트 메뉴의 `Debug → Log Kill Count`를 실행하면 Console에 현재 합계가 나옵니다. 새 Play 시작 값은 0입니다.
3. Catto와 MadGhost를 각각 처치한 뒤 조회하면 2가 되어야 합니다. 피격만 주거나 살아 있는 적을 비활성화/삭제했을 때는 증가하면 안 됩니다.
4. `Debug → Reset Kill Count` 후 조회하면 0입니다. 죽은 적의 제거 애니메이션이 끝나도 다시 올라가지 않아야 합니다. 이후 새 적을 처치하면 1입니다.
5. BossPatternTest에서는 HP 2500인 보스에 `Deal 3000 damage`를 눌러 사망시키고 합계 1을 확인할 수 있습니다. 이미 HP 1000 이하 특수 패턴 중이면 무적 종료 후 피해를 주세요.

### 부드러운 이동

1. BossPatternTest를 Play합니다. 등장 이동의 출발·도착부터 가감속을 확인할 수 있습니다.
2. A/D 또는 방향키로 더미를 이동시킨 후 원형 탄막을 기다립니다. 후딜레이 후 저장된 위치로 출발할 때 가속하고 도착 직전에 감속해야 합니다.
3. 보스 자식 FlightModule에서 Ease Duration을 0.1, 0.2, 0.35로 바꿔 체감을 비교합니다. 이동 시작 때 계산하므로 다음 이동부터 적용됩니다. Stop하면 Play 중 수정한 값은 원복됩니다.
4. `Deal 1500 damage`로 특수 패턴을 시작하고, 레이저 편도 3회가 정상 완료되며 중앙 복귀 후 다음 행동으로 넘어가는지 확인합니다.

## 검증 도구

- `VerifyEnemyDeathAndFlight.Validate`: 세 적과 매니저 프리팹의 모듈·스크립트·참조 검사.
- `VerifyEnemyDeathAndFlight.Run`: 독립 BossPatternTest의 처치 중복 방지/초기화/조회/씬 언로드/중복 매니저/지연 등록/실제 GameManager 호환 및 가감속 이동량 검사.
- `VerifyRobotEyeBoss.Run`: 기존 패턴, 피해, 중단, 먼 타깃, 투사체 수명 및 스킬 목록 회귀 검사.
- 기존 EnemyArtTest 도구: 일반 적 이동·공격·피격·충돌·타이밍·재화 보상 회귀 검사.

모든 파일 변경은 Assets/CTJ 내부입니다. 공용 시스템이나 저장 데이터를 수정하지 않았습니다.

검증 결과: Unity 컴파일 오류 0건, 세 적/매니저 프리팹 참조 검사 통과. 신규 처치·가감속 검사 46개, 기존 보스 검사 60개, 일반 적 회귀 시나리오 90개가 통과했습니다. 최종 Play Console 오류는 0건입니다. 중복/누락 매니저 예외 검사의 예상 경고 2건과 재화 예외 검사의 예상 경고 3건은 의도한 결과입니다.
