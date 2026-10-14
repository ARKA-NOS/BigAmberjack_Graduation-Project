# 적 추적·점프 개선 (2026-10-12)

2026-10-14: 아래 이동 설정은 유지한다. 공격 사거리·높이 조건은 이제 `EnemySkillModule → Skills`, 쿨타임/피해량은 각 스킬 컴포넌트에서 설정한다. 공용 쿨타임 수정 확인과 공격 모듈 전환 결과는 `EnemySkillGuide.md`에 있다.

## 2026-10-14 공용 Agent 네임스페이스 변경 대응

- 원인: 공용 Agent가 `Agents`에서 `Lrw.Script.Agent`로 이동했지만 이동 모듈의 참조가 이전 이름을 사용해 CS0246 컴파일 오류가 발생했다. 이전 using만 제거해도 `GetComponentInParent<Agent>()`의 Agent 타입을 찾을 수 없다.
- 수정: `using Lrw.Script.Agent;`를 추가했다. 걷기·점프·지형 판정 로직이나 프리팹 설정은 변경하지 않는다.
- 문법과 이유: `using`은 다른 네임스페이스의 타입을 짧은 이름으로 사용할 수 있게 하는 선언이다. 이 경우 `Agent`가 `Lrw.Script.Agent.Agent`를 뜻하도록 연결한다. 패키지를 설치하거나 새로운 클래스를 만드는 문법이 아니다.
- `GetComponentInParent<Agent>()`의 `<Agent>`는 찾을 컴포넌트 타입을 지정하는 제네릭 문법이다. 지형 검사에서 플레이어·다른 적의 Collider를 지형으로 취급하지 않도록 사용한다. 타입 검사를 삭제하면 기존 동작까지 달라지므로 참조만 바로잡는다.
- 직접 확인: Unity 재컴파일 완료 후 Console의 CS0246 재발 여부를 확인한다. `EnemyArtTest` 씬을 Play해 적이 평지에서 추적·공격하는지, 타깃을 위쪽 발판으로 옮겼을 때 기존 점프/우회가 유지되는지 확인한다. 바닥은 Ground 레이어, 적의 Terrain Layers도 Ground여야 한다.
- 검증 결과 (Unity 6000.3.22f1): 재컴파일 오류 0건, Catto/Mad Ghost 두 프리팹의 Body Collider·Ground 마스크·점프 높이 참조 정상. CTJ 전용 Play 테스트 `RunScenarios` 12건 모두 통과했고 종료 후 Console 오류 0건을 확인했다. Play를 종료하고 이전 Play 시작 씬 설정을 복원했으며 씬 파일은 저장하지 않았다. 실제 생성 맵 전체에 대한 검증은 아니다.

## 원인과 변경 범위

- 기존 Chase는 `target.x - enemy.x`의 부호로 X 속도만 설정했다. 같은 X의 위층 플레이어에게는 방향이 0이 되고, 점프 코드도 없어 정지했다.
- 공격 전환은 루트 간 거리만 확인했다. 벽/발판 너머의 타깃도 Attack 상태로 들어갈 수 있었다.
- `EnemyMovementModule : DevLib.ModuleSystem.Module`을 CTJ에 추가했다. 기존 EnemyBase → Agent → ModuleOwner의 초기화/검색 구조를 그대로 이용한다.
- FSM은 Idle/Chase/Attack 선택, 이동 모듈은 걷기·지면·점프·근거리 발판 우회를 담당한다. 모듈에는 별도 Update가 없어 공격 중에 이동을 덮어쓰지 않는다.
- 근접/원거리 모두 착지하고 지형에 시야가 막히지 않았을 때 공격을 시작한다. 근접은 발 높이 차이도 검사한다. 이미 시작한 근접 공격의 이벤트/후딜 잠금은 유지한다.
- 위층의 가까운 발판 아래에서는 양쪽을 제한된 거리만 탐색해 발판 밖으로 이동하고, 위로 뛰어 발이 윗면을 넘은 뒤 타깃 방향으로 돌아온다.
- 타깃 높이는 가능한 한 몸체 Collider의 아래쪽으로 비교한다. 중앙 피벗의 Player와 발 피벗의 Enemy를 같은 바닥에서 다른 층으로 오인하지 않는다.

변경된 프리팹은 `Catto_Melee`, `MadGhost_Ranged`이다. 기존 스케일(2배), 레이어, 체력, 공격력은 유지했다. 기존 EnemyBase 프리팹에 이동 모듈이 없으면 ModuleOwner.Awake의 모듈 수집 전에 런타임으로 보완한다. 직접 편집하는 프리팹에는 이동 모듈을 명시적으로 붙여 설정하는 것이 좋다.

공용 Lrw/SDW/Scripts/ProjectSettings 파일은 수정하지 않았다. 기존 플레이어의 공용 스킬 쿨타임 문제도 이번 수정에 포함하지 않는다.

## Inspector 설정

적 루트의 Enemy Movement Module:

| 필드 | 기본값 | 의미 |
| --- | --- | --- |
| Body Collider | 프리팹 몸체 Collider | 공격 Trigger가 아닌 실제 몸체 |
| Terrain Layers | Ground | 점프·지면·시야 검사에 사용할 지형 레이어 |
| Ground Check Distance | 0.1 | 발밑 접지 허용 거리 |
| Obstacle Check Distance | 0.3 | 앞쪽 장애물 감지 거리 |
| Horizontal Tolerance | 0.06 | 목표 X 근처에서 좌우 떨림을 막는 허용 오차 |
| Jump Height | 3 | 월드 단위 이론 점프 높이. 실제 물리 적분에서는 조금 낮을 수 있음 |
| Minimum Target Height | 0.45 | 이보다 위에 있는 타깃을 향해 점프 고려 |
| Jump Cooldown | 0.5 | 점프 최소 간격. 접지 조건도 필요해 공중 연속 점프는 안 됨 |
| Max Ledge Detour | 3 | 좌우 가장자리 탐색 한도 |

이동 속도는 기존 EnemyBase의 Move Speed에 그대로 둔다. 기존 프리팹/씬의 속도 설정이 유실되지 않도록 이전하지 않았다.

MeleeEnemy의 Max Attack Height Difference는 기본 0.5이다. 실제 몸체 발 높이를 비교한다. Ground 레이어는 지형 Collider가 붙은 오브젝트에 지정해야 한다. 부모의 레이어만 바꾸고 자식 Tilemap Collider는 다른 레이어로 남기면 감지가 안 된다. Trigger는 지형 검사에서 제외한다.

## 직접 테스트하기

### 기존 씬에서 확인

1. 저장하지 않은 씬 변경을 보존한 뒤 `EnemyArtTest.unity`를 연다. Catto/Mad Ghost 프리팹에 이동 모듈과 Body Collider 참조가 보이는지 확인한다.
2. Play 후 Catto의 테스트 타깃을 적과 같은 X, 적의 발보다 약 2~2.5 위로 옮긴다. 머리 위 장애물이 없다면 제자리 점프해야 한다. 공중 타깃은 착지 조건 때문에 공중에서 근접 타격하지 않는다.
3. 타깃을 원래 바닥으로 내려놓는다. 불필요하게 뛰지 않고 추적·공격하는지, 타깃을 반대편으로 옮기면 방향이 바뀌는지 확인한다.
4. Mad Ghost는 사거리 안에서 발사 경로가 열려 있으면 제자리 사격하는 것이 정상이다. 무조건 타깃에게 붙거나 점프하지 않는다.
5. Console에 새로운 오류가 없는지 확인하고 Play를 종료한다. 테스트 중 변경을 프리팹에 Apply하지 않는다.

### 발판 우회 확인

1. CTJ 테스트 씬의 충분히 넓은 평지에 Catto를 놓는다. 몸체가 기존 바닥/타깃과 겹치지 않게 한다.
2. 적 머리 위에 Ground 레이어, 비 Trigger BoxCollider2D인 발판을 둔다. Catto 스케일 2 기준: 바닥 윗면 Y=0, 발판 윗면 Y=2, 폭 1.5, 두께 0.2 정도로 시작한다.
3. 타깃의 발을 발판 위에 놓고 적의 Target으로 지정한다. 적이 가장자리 밖으로 이동 → 점프 → 발판 위 착지 → 공격하는지 확인한다.
4. 높이 0.6 정도의 낮은 장애물을 적과 타깃 사이에 놓아 넘는지 확인한다.
5. 점프 높이보다 높은 발판이나 탐색 한도를 넘는 넓은 천장도 확인한다. 이런 경우 길을 못 찾는 것은 현재 기능 한계다. 벽을 통과하거나 무한 공중 점프해서는 안 된다.

### 실제 플레이어와 확인

CTJ_MapTestScene에서 생성/배치된 PlayerPack의 내부 PlayerController를 타깃으로 쓴다. 자동 탐색은 기존과 같이 최초 생성 대기를 지원한다. 모듈이 작동하려면 적의 Rigidbody2D가 Dynamic/Simulated, Gravity Scale > 0이어야 한다.

플레이어 평타가 공용 쿨타임 코드 때문에 막히는 경우는 이번 이동 테스트와 별개다. 사망 중단 검사는 적 컴포넌트 메뉴의 `Test/Take 2 Damage (Play Mode)`로도 가능하다.

## 자동 검증 도구

`Tools~/VerifyEnemyMovement.cs`는 Unity 자동 컴파일/빌드 대상이 아닌 기존 방식의 검증 스크립트다. `InspectEnemyArt.BeginPlayTest`로 별도 Play 세션을 시작한 다음 `RunScenarios`를 호출한다. 각 결과의 `passed`를 모두 확인한다. 종료 시 `InspectEnemyArt.StopPlayTest`와 `RestoreStartScene`으로 기존 설정을 복원한다. 테스트 오브젝트는 Play 인스턴스에서 생성하고 finally에서 제거하며, 기존 씬을 저장하지 않는다.

검증 사례: 수직 타깃 점프, Catto 1/2배 발판 우회, Mad Ghost 1배 발판 우회, 낮은 장애물, 근접/원거리 평지 공격, 넓은 낮은 천장, 도달 불가 높이, 점프 중 사망, 중앙 피벗 타깃, 좌우 방향 전환. 발판 아래에서 먼저 피해를 주지 않는지도 검사한다.

### 검증 기록

2026-10-12, Unity 6000.3.22f1:

- 수정 전 같은 X, Y=2.5 타깃: 2초 동안 최대 Y=0.05, 최종 속도 (0, 0)으로 정지 재현.
- 최종 소스 Unity 재컴파일 성공, 컴파일 오류 0. 두 프리팹의 이동 모듈/몸체 Collider/Ground 마스크/점프 높이 참조 확인.
- RunScenarios 12건 모두 passed=true. 수직 점프 최고 약 2.96, 높이 2의 좁은 발판 우회·착지·공격 확인. 발판 아래에서 미리 준 피해 없음.
- 기존 CheckDodgeAndFacing 통과: 근접 타격 전 회피 피해 0, 두 적 모두 왼쪽 공격 성공.
- 테스트 후 콘솔 오류/경고 조회 0건. 원래 편집 씬과 Play 시작 씬 설정 복원.
- 검증용 Ground BoxCollider2D 환경에서 확인한 결과다. 실제 생성 맵의 모든 Tilemap/발판 배치와 PlayerPack 전투 전체를 검증한 것은 아니다.

## 사용한 문법과 선택 이유

- `sealed class EnemyMovementModule : Module` / `override Initialize`: 실제 두 적이 공유하는 이동 기능 한 개만 모듈화했다. 새로운 인터페이스/추상 계층은 추가하지 않았다. ModuleOwner가 필요한 참조를 연결하는 시점을 그대로 사용한다.
- `[RequireComponent]` / `[DisallowMultipleComponent]`: 새로 만든 적에는 필수 이동 모듈을 요구하고 같은 오브젝트의 중복 부착을 막는다. 기존 프리팹에 소급되지 않으므로 EnemyBase.Awake에서 모듈 수집 전에 누락을 보완한다.
- `ContactFilter2D` / `Physics2D.BoxCast`: 실제 몸체 크기를 조금 줄인 박스를 이동시켜 지형과 만나는지 검사한다. 발판/벽 레이어만 사용하고 Trigger·Agent 몸체는 제외한다.
- `out RaycastHit2D`, `out _`: 검사 성공 여부와 충돌 정보를 함께 받는다. 정보가 필요 없는 곳에서는 `_`로 버린다. `RaycastHit2D`는 BoxCast 결과에도 사용하는 타입이다.
- `Collider2D.bounds`: 월드 크기를 사용하므로 스케일 1/2배를 고정 로컬 센서 값으로 따로 맞추지 않는다. Tilemap/Composite의 전체 bounds를 발판 높이로 취급하지 않고 작은 BoxCast로 윗면을 찾는다.
- `Mathf.Sqrt(2 * g * h)`: 원하는 높이를 기준으로 점프 초기 Y 속도를 계산한다. 기존 중력은 유지하며 질량에 따라 높이가 달라지는 임펄스 방식은 사용하지 않았다.
- `List<RaycastHit2D>` 재사용: 검사마다 새 결과 배열을 만들지 않는다. 타깃 Collider 검색도 타깃이 바뀔 때만 수행한다.
- `Time.fixedDeltaTime`: 한 물리 프레임 안에 목표 X를 지나치지 않도록 수평 속도를 제한한다. 기존 FSM의 FixedUpdate 경로에서만 이동을 실행한다.
- 검증 도구의 `async Task` / `Task.WhenAll` / `finally`: 서로 떨어진 테스트 구역에서 실제 프레임을 기다리며 사례를 검사하고 종료 후 테스트 오브젝트를 정리한다. 게임 실행 코드에 비동기 작업을 도입한 것은 아니다.

## 범위와 다음 단계

이것은 근거리 지형 대응이지 플랫폼 경로 탐색기가 아니다. 여러 층의 경유 발판 선택, 넓은 낭떠러지 점프, 안전한 낙하 경로, 이동 발판 탑승, 일방통행 발판 특화 처리는 포함하지 않는다. 도달할 수 없는 타깃 아래에서는 멈출 수 있다. 큰 적은 몸이 지나갈 공간과 착지까지의 수평 이동 시간이 더 필요하다.

적도 이미 Agent/ModuleOwner와 StatModule/HealthModule을 사용한다. 이번에 이동까지 같은 모듈 시스템으로 맞췄다. 공격을 SkillModule로 통일하는 방향은 타당하지만, 먼저 공용 AbstractNormalSkill의 쿨타임 조건을 담당자와 수정하고 공격 간격의 소유자를 정해야 한다. 현재 EnemyBase.AttackInterval과 공용 스킬 쿨타임을 중복 적용하는 임시 전환은 하지 않았다.
