# 적 스킬 모듈 구성·양산·테스트 (2026-10-14)

현재 공격 설정은 이 문서를 기준으로 한다. 기존 EnemyCombatGuide/EnemyDamageDeathGuide의 MeleeEnemy·RangedEnemy 내부 공격 필드 설명은 이전 구조다. 이동 설정은 EnemyMovementGuide를 그대로 사용한다.

추가된 추적 해제 거리, HP 40/30, 준비 스탯과 Canvas 공격 예고의 상세 설정/테스트는 `EnemyCombatRulesGuide.md`를 참고한다.

## 이번에 완성한 범위

- Catto 근접 공격과 Mad Ghost 발사체 공격을 공용 `SkillModule` / `AbstractCoolTimeSkill` 기반으로 전환했다.
- `EnemySkillModule : SkillModule`은 스킬 목록의 **순서·거리·높이·지면·시야**를 보고 선택한다. 쿨타임 검사, 실행, 강제 종료는 공용 구현을 호출한다.
- FSM은 Idle → Chase → Attack을 유지한다. 사용 범위 안에서는 멈춰 쿨타임을 기다리고, 사용할 수 있는 다른 스킬이 있으면 선택한다.
- 한 적에 여러 스킬을 넣을 수 있다. 사용 중인 스킬은 끝날 때까지 기다린다. 우선순위가 높다는 이유만으로 현재 스킬을 취소하지 않는다.
- 죽음, 적/모듈 비활성화, 타깃 교체·소실 때 실행/차징을 취소한다. 이미 발사된 투사체는 기존처럼 수명/충돌까지 남는다.
- 기존 MeleeEnemy/RangedEnemy는 프리팹 GUID와 코드 참조를 보존하는 얇은 클래스다. 이제 이름이 아니라 연결한 스킬 목록이 실제 공격 종류를 결정한다.
- 구형 EnemyPrototype만 기존 AttackRange/AttackInterval과 로그 공격을 유지한다. SDW 생성기 등 다른 팀 코드와 공용 스킬 코드는 수정하지 않았다.

## 구조와 설정 위치

| 역할 | 수정하는 곳 |
| --- | --- |
| 타깃, 탐지, 이동 속도, 사망 | EnemyBase를 상속한 기존 적 컴포넌트 |
| 지면·벽 검사, 점프 | EnemyMovementModule |
| 공격 선택 순서, 사거리, 높이 조건 | EnemySkillModule → Skills |
| 피해량, 쿨타임, 애니메이션/발사체 참조 | EnemyMeleeSkill / EnemyProjectileSkill |
| 공용 스킬 식별 키, 취소 허용 플래그 | Skill SO 에셋 |
| 체력 | 기존 StatModule → Base Stats, HealthModule |

Skill SO는 스킬 식별용 데이터다. 피해량/쿨타임 수치는 현재 스킬 컴포넌트에 둔다. `CoolTimeStartTime`, `IsUsing` 등 실행 상태도 컴포넌트에만 있으므로 같은 SO를 공유하는 적 10마리의 쿨타임은 각각 독립이다.

현재 값:

| 프리팹 | 피해량 | Base Cool Time | Max Range | 높이 차이 | 종료 방식 |
| --- | --- | --- | --- | --- | --- |
| Catto_Melee | 10 | 1초 | 1.05 | 0.5 | OnAttackFinished; 누락 시 1.5초 복구 |
| MadGhost_Ranged | 5 | 1.2초 | 7 | -1(제한 없음) | 발사 후 Recovery Time 0.3초 |

쿨타임은 **발동 시작**부터 센다. 실제 다음 발동은 쿨타임과 현재 공격 종료를 모두 만족해야 한다. 차징 스킬은 차징 완료 후 실제 발동부터 쿨타임이 시작된다. EnemyBase의 Legacy Attack Interval을 바꿔도 모듈 적의 공격 간격은 바뀌지 않는다.

## 적을 추가하는 순서

1. `Assets/CTJ/Prototypes/Prefabs`의 Catto_Melee 또는 MadGhost_Ranged에서 **Prefab Variant**를 만든다. 저장 위치는 Assets/CTJ 안으로 제한한다.
2. 비주얼, 몸체 Collider, Facing Root, 이동 속도, Detection Range와 Chase Release Range를 바꾼다. 최초 감지는 Detection Range, 감지 후 추적 유지 여부는 Chase Release Range로 판단한다.
3. 체력을 다르게 하려면 기존 Stats 에셋을 CTJ 안에 복제하고, 해당 Variant의 StatModule에 연결한다. 공유 원본 값을 바꾸면 원본을 사용하는 다른 적도 바뀐다.
4. 스킬의 Damage, Base Cool Time, 판정/발사 참조를 설정한다. 기존 공격 동작 재사용이면 새 C# 클래스는 필요 없다.
5. EnemySkillModule의 Skills에서 사용 거리와 순서를 조절한다. Max Height Difference는 발 위치 기준이며 -1이면 제한 없다. 현재 두 프리팹은 Require Grounded / Require Clear Path가 켜져 있다.
6. 테스트 후 기존 CombatRoomDataSO의 적 프리팹 목록에 전달하면 된다. SDW 생성기는 여전히 EnemyBase를 받으므로 별도 생성기를 만들 필요가 없다. 다른 팀 소유 데이터는 이 작업에서 수정하지 않았다.

### 한 적에 두 번째 스킬 넣기

1. 적 루트(EnemySkillModule과 같은 오브젝트) 또는 그 아래 활성 자식에 `EnemyProjectileSkill`/`EnemyMeleeSkill`을 추가한다. **SkillModule은 하나만** 둔다. 같은 종류의 스킬 컴포넌트는 여러 개 둘 수 있다.
2. `Create → SkillSystem → Skill SO`로 새 키를 만들거나 적절한 기존 키를 연결한다. **같은 적 내부의 두 스킬에 같은 Skill SO를 연결하면 안 된다.** 서로 다른 적끼리 공유하는 것은 가능하다.
3. 새 컴포넌트를 Skills 목록의 Skill 칸에 드래그한다. 목록은 위부터 우선순위다. 위 스킬이 쿨타임/조건 불충족이면 다음 것을 확인한다.
4. 원거리는 Visual 아래에 FirePoint를 만들고 **바닥 위**에 배치한다. EnemyProjectileSkill.Fire Point와 해당 목록 항목의 Origin에 연결한다. Ground와 겹쳐 생성된 발사체는 즉시 막힌다.
5. 근접은 Animator, Attack State Name, 활성 Trigger Hitbox가 필수다. Hitbox와 FirePoint를 Facing Root 아래에 두어 좌우 반전을 함께 적용한다.
6. 같은 Animator에 여러 공격을 넣는다면 서로 다른 상태 이름과 올바른 클립을 연결한다. Recovery Time/종료 이벤트가 실제 클립보다 일찍 끝나면 다음 스킬이 애니메이션을 잘라버릴 수 있다.

예: 근접 스킬 Max Range=1.05를 0번, 발사체 Max Range=6을 1번에 둔다. 가까우면 근접부터, 근접이 쿨타임이면 발사체, 멀면 발사체만 사용한다. 가까울 때 원거리 사용을 금지하려면 해당 항목의 Min Range를 설정한다. **Min Range는 사용 제한일 뿐 후퇴 AI는 아니다.** 도주/거리 유지 동작은 이번 구현에 없다.

## 애니메이션 이벤트와 새 동작 확장

- 근접 타격 프레임: 기존 `OnAttackHit`. 그 시점에 타깃과 Hitbox가 겹쳐야 피해를 준다. 중복 이벤트나 여러 타깃 Collider로 한 공격이 중복 피해를 주지 않는다.
- 근접 종료 프레임: 기존 `OnAttackFinished`. Animator가 Visual 자식에 있으면 기존 EnemyAnimationEventRelay가 적 → 현재 스킬로 전달한다.
- 근접 타격 직전: `OnAttackWarning`. Catto는 클립 0.08초부터 0.2초 타격까지 표시가 깜빡인다.
- 원거리는 Attack Delay Stat의 준비 시간과 예고 점멸 후 발사한다. 기존 Mad Ghost 클립에는 타격 이벤트를 새로 넣을 필요가 없다.
- 새로운 **공격 동작**이 필요하면 `EnemyAttackSkill`을 상속한다. 준비는 PrepareAttack, 실제 동작은 OnReleaseAttack, 표시 갱신은 UpdateTelegraph를 override한다. 끝나면 EndSkill, 추가 취소 처리는 OnStopSkill에서 base도 호출한다. OnUseSkill을 직접 재정의한다면 반드시 base를 호출하고 준비 이전에 피해/발사를 실행하지 않는다.
- 공용 `AbstractSkill` / `AbstractChargeSkill` 파생 컴포넌트도 목록에 넣을 수 있다. 차징은 항목의 Charge Duration 뒤 입력 해제로 발동한다. 별도 종료 처리는 그 스킬이 맡아야 한다.
- **플레이어 전용 스킬을 그대로 붙일 수 있다는 뜻은 아니다.** 현재 PlayerMeleeAttackSkill은 PlayerController, 플레이어 FSM, 전용 렌더러·스탯에 의존한다. 공용 실행 규약/쿨타임을 통일했으며, 공격 구현은 적용 가능한 소유자를 지원해야 한다.
- 공용 모듈은 Awake 시 활성 자식 스킬을 한 번 등록한다. 실행 중 새 컴포넌트 삽입/비활성 자식 활성화에 의한 신규 등록은 지원하지 않는다. 목록에서 이미 등록된 스킬을 켜고 끄는 것은 Use Skill로 한다. 런타임 보상 스킬 장착 기능은 이번 범위가 아니다.

## 직접 테스트하는 과정

1. `Assets/CTJ/Prototypes/Scenes/EnemyArtTest.unity`를 열고 Play한다. 이 씬의 타깃은 실제 플레이어가 아닌 EnemyDamageProbe이며 Console에 피격 횟수·누적 피해가 나온다.
2. Catto가 접근해 타격 프레임에만 10 피해, Ghost는 범위 안에서 1.2초 이상 간격으로 발사해 5 피해를 주는지 본다. 두 프리팹은 땅에 서 있어야 공격한다.
3. Catto 공격 준비 중 타깃을 위/옆으로 이동한다. 타격 시 겹치지 않으면 피해가 없어야 한다. 다시 가까이 두면 다음 공격이 나와야 한다.
4. Play 인스턴스의 스킬 Base Cool Time을 2~3초로 바꿔 간격 변화를 확인한다. 적을 복제해도 다른 적의 쿨타임에 묶이지 않아야 한다.
5. 적 컴포넌트의 Context Menu → `Test/Take 2 Damage (Play Mode)`를 반복한다. Catto는 체력 40, Ghost는 30이므로 이 메뉴로는 20회/15회다. 기본 플레이어 평타 10으로는 4회/3회다. 공격 준비 중 죽어도 이후 근접 타격이 나오면 안 된다.
6. 위 조합 방법으로 Variant에 두 번째 스킬을 넣고, 거리·목록 순서·쿨타임별 선택을 본다. 원거리 공격이 즉시 사라지면 FirePoint가 바닥/벽과 겹치는지 먼저 확인한다.
7. 실제 PlayerPack 확인은 CTJ_MapTestScene에서 한다. Target은 PlayerPack 포장 루트가 아니라 **PlayerController가 붙은 Transform**이어야 한다. 비워두면 기존 지연 탐색을 사용한다. 실제 플레이어의 공격 마스크/체력 구성은 이 변경과 별개이며 이번 자동 검증은 Probe를 사용했다.

## 자동 검증 (이번 작업 결과)

Unity 6000.3.22f1 백그라운드 에디터의 CTJ 전용 씬에서 수행했다. 게임 화면의 외형은 이번 검사에 포함하지 않는다.

- 프로젝트 C# 재컴파일: 오류 0. 두 프리팹의 스킬 키/목록/Animator/Hitbox/FirePoint/발사체 참조 및 Missing Script 검사 통과.
- VerifyEnemySkills: 5개 시나리오 묶음 통과. 초기 사용, 실제 초 단위 쿨타임(2.2초), 공유 SO의 개체별 쿨타임, 한 번만 타격, 애니메이션 종료, 회피, 비활성화, 타깃 변경, Agent 참조를 통한 피격 사망/취소, 두 스킬 우선순위/쿨타임 대체/사거리 선택, 투사체 피해/후딜, 차징 입력·해제·취소 검사.
- VerifyEnemyMovement: 기존 이동/발판/높이/사망 12개 시나리오 통과.
- InspectEnemyArt.CheckDodgeAndFacing: 회피 시 피해 0, 두 적의 좌측 타깃 공격 통과.
- 테스트용 발사 위치를 바닥에 둔 첫 조합 테스트는 실패했고, 별도 FirePoint를 지면 위에 둔 후 재검증했다. 런타임 복제본만 사용했으며 씬/플레이어/공용 SO를 저장하지 않았다.

재실행 순서(프로젝트를 Unity에서 연 상태, 기존 Play 종료 후): `Tools~/InvokeUnity.ps1`의 run_script로 InspectEnemyArt.BeginPlayTest → VerifyEnemySkills.RunScenarios → VerifyEnemyMovement.RunScenarios → InspectEnemyArt.CheckDodgeAndFacing → InspectEnemyArt.StopPlayTest → Play 종료 확인 후 InspectEnemyArt.RestoreStartScene. 프리팹 정적 검사는 VerifyEnemySkills.ValidatePrefabs. 모든 소스는 CTJ/Tools~에 있다.

기존 Tools~의 BuildEnemyArt/ConfigureEnemyDeath는 이전 구조를 생성하는 기록이므로 이번 프리팹에 다시 실행하지 않는다.
