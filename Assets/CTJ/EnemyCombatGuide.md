# CTJ 근접·원거리 적 설정과 테스트

2026-09-11: Catto(근접), Mad Ghost(원거리)의 연결된 프리팹과 `Assets/CTJ/Prototypes/Scenes/EnemyArtTest.unity` 테스트 씬을 추가했다. 바로 실행하는 절차와 에셋별 값/문법 설명은 `Assets/CTJ/Prototypes/README.md`에 있다. 아래는 직접 구성할 때의 공통 안내다.

## 구현 범위

- `MeleeEnemy`와 `RangedEnemy`는 `EnemyBase`를 상속하고 Lrw의 FSM을 그대로 사용한다.
- 두 적 모두 Idle → Chase → Attack으로 움직인다. 플레이어의 지연 생성 탐색도 유지한다.
- 근접 적은 공격 애니메이션 시작 시 이동과 방향을 고정한다. 타격 이벤트 순간에 Hitbox와 타깃 Collider가 겹쳐야 피해가 전달된다.
- 원거리 적은 발사 시점의 플레이어 위치를 향해 직선 발사체를 발사한다. 발사 후 유도하지 않는다.
- 피해 전달은 기존 `IDamageable.ApplyDamage()`를 호출한다. 적의 체력/사망/넉백 규칙은 추가하지 않았다. 적이 피해를 받으면 기존 `OnHit` UnityEvent만 호출한다.
- 현재 `PlayerController`는 공용 `Agent`를 통해 `IDamageable`을 상속하고 HealthModule에 피해를 전달한다. 플레이어의 모듈 연결은 별도로 확인한다. 아래 `EnemyDamageProbe`는 별도 임시 타깃의 로그/횟수 검증용이며 체력 시스템이 아니다.
- 공용 `AttackTypeEnum`에는 `Melee`만 있으므로 원거리 분류를 추가하지 않았다. 전달되는 `DamageData`의 분류는 현재 기본값이고, 피해량·공격자·방향만 활용한다.
- 벽·절벽을 피하는 추적과 점프는 포함하지 않는다. Catto/Mad Ghost 프리팹에는 Idle/Move/Attack 애니메이션이 연결되어 있다. 첫 테스트는 평평한 발판에서 진행한다.

## 1. 공통 테스트 환경

공용 Agent는 HealthModule을 필수로 찾는다. 적을 직접 구성할 때 기존 Lrw의 StatModule과 HealthModule을 연결하고, HealthModule의 Max Hp Stat Data에 기존 MaxHealth StatData를 지정한다. 준비된 Catto/Mad Ghost 프리팹에는 이미 연결되어 있다. 공용 모듈이나 데이터 원본은 수정하지 않는다.

1. `Assets/CTJ/Scenes/CTJ_MapTestScene.unity`에서 작업하거나 CTJ 안에 테스트 씬을 만든다.
2. 바닥에 Collider2D가 있어야 한다. 적 루트에는 Rigidbody2D(Dynamic), 몸체 Collider2D(Trigger 끔)를 둔다. Freeze Rotation Z를 켠다.
3. 한 오브젝트에는 EnemyPrototype/MeleeEnemy/RangedEnemy 중 하나만 붙인다. 테스트용 적을 복제했다면 EnemyPrototype을 제거한 뒤 새 타입을 붙인다.
4. 적 아래에 `Visual` 자식을 만든다. 오른쪽을 바라보는 것을 기본으로 하고, 적의 `Facing Root`에 이 자식을 지정한다. 몸체 Collider와 Rigidbody는 루트에 그대로 둔다.
5. Target은 플레이어 루트 Transform이다. 비워두면 기존 방식대로 PlayerController를 0.2초마다 찾아 연결한다. 테스트용 사각형 등 다른 타깃을 쓸 때는 직접 지정한다.
6. 현재 플레이어는 이미 IDamageable을 상속하므로 EnemyDamageProbe를 중복으로 붙이지 않는다. 실제 플레이어는 HealthModule의 Current Health 변화를 확인하고, 별도 임시 타깃에는 `CTJ.Testing.EnemyDamageProbe`를 붙여 피해 로그를 확인한다. Collider가 자식이어도 부모 수신기를 찾는다.
7. 플레이어 생성 시스템 없이 검사하려면 CTJ 씬에 Collider2D + EnemyDamageProbe를 가진 임시 Target을 만들어 적의 Target 필드에 지정한다. Target을 Scene 뷰에서 이동하여 거리/접촉을 검사할 수 있다.

Play 모드에서 추가한 컴포넌트와 값은 종료하면 사라진다. 프리팹 원본에 Apply하지 않는다.

## 2. 근접 적 구성

권장 Hierarchy:

```text
MeleeEnemy (MeleeEnemy, Rigidbody2D, 몸체 Collider2D)
└─ Visual (SpriteRenderer, Animator, EnemyAnimationEventRelay)
   └─ AttackHitbox (BoxCollider2D, Is Trigger 켬)
```

1. `Visual`과 AttackHitbox는 활성 상태로 둔다. AttackHitbox에는 별도의 Rigidbody2D를 붙이지 않는다.
2. AttackHitbox의 BoxCollider2D를 적 앞쪽에 놓는다. 처음에는 로컬 위치 X=0.8, Size=(1.6, 1.2) 정도로 시작하고 실제 스프라이트 크기에 맞춘다.
3. MeleeEnemy의 `Attack Hitbox`에 해당 BoxCollider2D, `Attack Animator`에 Visual의 Animator, `Facing Root`에 Visual을 지정한다.
4. Detection Range=6, Attack Range=1.5, Attack Interval=1, Damage=10으로 시작한다. Attack Range는 공격을 **시작하는 거리**이고 실제 피해 범위는 AttackHitbox다. 서로 맞춰야 한다.
5. CTJ 안에 Animator Controller와 공격 Animation Clip을 만든다. Base Layer에 이름이 `Attack`인 상태를 만들고 해당 클립을 연결한다. MeleeEnemy의 Attack State Name은 `Base Layer.Attack`이다.
6. 기본 상태 `Idle`을 두고 Attack → Idle 전환을 추가한다. Has Exit Time을 켜고 Exit Time=1, Transition Duration=0, 조건은 비운다. Any State → Attack 전환은 필요 없다. 코드는 `Animator.Play`로 공격을 시작한다.
7. Attack 클립은 Loop Time을 끈다. 테스트 클립은 약 0.6초로 만들고 Visual의 색상 등 간단한 변화가 있어도 된다. Visual의 localScale.x를 애니메이션으로 덮어쓰면 방향 반전과 충돌하므로 피한다.
8. Animation 창에서 타격 프레임(예: 0.25초)에 이벤트 `OnAttackHit`를 넣는다. 공격 후딜이 끝나는 지점(예: 0.55초, 상태 전환보다 앞)에 `OnAttackFinished` 이벤트를 넣는다. 이벤트 인자는 없다.
9. `EnemyAnimationEventRelay`는 Animator와 **같은 GameObject**에 붙인다. Relay의 Enemy는 부모 MeleeEnemy를 자동으로 찾는다. Animator가 적 루트에 있다면 MeleeEnemy가 이벤트를 직접 받으므로 Relay를 추가하지 않는다.
10. Attack Timeout은 실제 공격 길이보다 길게 둔다(기본 3초). 종료 이벤트를 빼먹으면 경고 후 잠금을 풀어 적이 영구 정지하지 않게 한다. 이것으로 타격 이벤트를 대신 실행하지는 않는다.

## 3. 원거리 적과 발사체 구성

권장 Hierarchy:

```text
RangedEnemy (RangedEnemy, Rigidbody2D, 몸체 Collider2D)
└─ Visual (SpriteRenderer)
   └─ FirePoint (Transform)
```

1. 발사체용 GameObject에 SpriteRenderer, EnemyProjectile을 붙인다. Rigidbody2D와 CircleCollider2D는 RequireComponent로 함께 추가된다. CircleCollider2D 반지름을 예를 들어 0.1로 맞춘다.
2. 발사체 Speed=10, Lifetime=5로 시작한다. Blocking Layers에는 현재 프로젝트에서 사용하는 벽/바닥 레이어를 선택한다. 새 레이어를 만들 필요는 없다.
3. 발사체 루트는 활성 상태, 컴포넌트와 Collider는 Enabled 상태로 둔다. Rigidbody의 Simulated를 켠다. 실행 시 코드는 Kinematic/Gravity Scale=0, Collider의 Is Trigger를 설정한다.
4. 이 오브젝트를 `Assets/CTJ/Prefabs` 같은 CTJ 내부 경로에 프리팹으로 저장하고 씬에 놓은 견본은 제거한다.
5. RangedEnemy의 `Projectile Prefab`에 이 프리팹을, `Fire Point`에 Visual 아래의 FirePoint를 지정한다. FirePoint를 오른쪽 총구 위치로 옮긴다. Facing Root를 지정하면 반대편을 볼 때 총구도 반전된다.
6. Detection Range=10, Attack Range=7, Attack Interval=1, Damage=5로 시작한다. 에디터에서 RangedEnemy를 새로 추가하거나 Reset하면 거리 기본값이 10/7로 설정된다. 기존 오브젝트나 런타임 AddComponent에는 Inspector 값을 확인한다.
7. 공격 범위 안에서는 정지하고 Attack Interval마다 발사한다. 범위를 나갔다 즉시 다시 들어와도 이전 공격의 쿨타임은 유지된다.
8. 발사체는 타깃의 자식 Collider도 검사하고 첫 충돌에서 한 번만 피해를 전달한 뒤 제거된다. 벽/바닥 또는 수명 만료 시에도 제거된다. 적과 다른 발사체, 벽 레이어의 Trigger는 통과한다.
9. 발사체는 Collider2D.Cast로 이동 구간 전체를 검사하므로 단순 Trigger 이벤트에만 의존하지 않는다. Physics 2D 레이어 매트릭스에서 발사체와 플레이어/벽 질의를 허용하는지 함께 확인한다.

## 4. 정상 작동 확인 순서

Console의 Collapse를 끄고 EnemyDamageProbe의 Received Hit Count/Total Damage를 함께 확인한다.

| 상황 | 기대 결과 |
| --- | --- |
| 시작 시 플레이어 없음 → 나중에 생성 | Target 자동 연결 후 거리 기준 행동 |
| 근접 감지 범위 진입 | 타깃까지 추적하고 Attack Range에서 정지 |
| 타격 이벤트 전 Hitbox 안에 머무름 | 아직 피해 없음 |
| 타격 이벤트 순간 Hitbox 안에 있음 | 로그 1회, 횟수 +1, Damage만큼 누적 |
| 공격 시작 후 타격 프레임 전에 밖으로 이동 | 빗나감, 피해 없음; 종료 이벤트 뒤 다시 추적 |
| 같은 공격에 OnAttackHit 이벤트를 두 개 넣음 | 첫 판정만 사용. 빗나간 뒤 두 번째 이벤트에 들어와도 피해 없음 |
| 플레이어에 Collider가 여러 개 있음 | 같은 공격으로 여러 번 피해를 주지 않음 |
| OnAttackFinished 누락 | Attack Timeout 뒤 경고/잠금 해제, 타격 추가 없음 |
| 근접 적 비활성화 후 이벤트 호출 | 피해 없음 |
| 원거리 공격 범위 진입 | 이동 정지, 지정 주기로 발사 |
| 발사 후 플레이어 위치 변경 | 탄은 이미 정한 방향으로 직진 |
| 플레이어 앞에 벽 | 탄이 벽에서 제거되고 플레이어 피해 없음 |
| 얇은 벽 + 빠른 탄 | Cast 구간 안의 벽에서 탄 제거 |
| 탄이 아무것도 맞히지 않음 | Lifetime 뒤 제거 |
| 범위 밖 → 다시 안으로 빠르게 이동 | Attack Interval보다 빠른 연속 발사 없음 |

## 5. 사용한 문법과 선택 이유

- `sealed class ... : EnemyBase`: 이미 필요한 근접/원거리 타입을 구현한다. 공통 이동/FSM은 상속받고 Attack과 ApplyDamage를 재정의한다.
- `virtual`/`override IsAttackInProgress`: 기본은 즉시 공격이고 근접만 애니메이션 종료까지 잠긴다. FSM이 현재 타입 이름을 직접 검사하지 않고 이 값으로 전환 여부를 판단한다.
- `protected` 거리 필드와 `Reset()`: 파생 원거리 타입이 에디터 초기 거리(10/7)를 설정하도록 필요한 두 필드만 상속 클래스에 공개한다. Reset은 매 실행 초기화 함수가 아니다.
- `Time.time` 기반 다음 공격 시각: 상태를 재진입해도 타이머가 초기화되어 공격 간격을 무시하지 않도록 한다.
- Animation Event용 `public void` 메서드: 클립에서 이름으로 호출할 수 있게 한다. 자식 Animator에는 얇은 Relay 컴포넌트를 두어 부모 적에게 전달한다.
- `IEnumerator`/`WaitForSeconds`: 종료 이벤트 누락 시에만 작동하는 복구 제한 시간을 둔다. 기존 플레이어 탐색 코루틴은 유지한다.
- `List<Collider2D>`/`List<RaycastHit2D>`와 `readonly`: 물리 검사 결과 목록을 재사용한다. readonly는 목록 교체만 금지하며 내용 추가/갱신은 가능하다.
- `GetComponentInParent<IDamageable>()`: 플레이어의 Collider가 자식에 있고 피격 코드는 부모에 있는 일반적인 구조를 지원한다.
- `internal static EnemyDamageUtility`: 실제로 두 공격 방식이 공유하는 타깃 확인/피해 데이터 전달만 모았다. 새 인터페이스나 추가 추상 계층은 없다. internal은 CTJ 폴더 제한이 아닌 같은 어셈블리 접근 제한이다.
- `LayerMask` 비트 검사: 충돌 레이어가 Blocking Layers에 포함됐는지 확인한다.

Unity 문서: [Animation Events](https://docs.unity3d.com/6000.3/Documentation/Manual/script-AnimationWindowEvent.html), [Collider2D.Overlap](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Collider2D.Overlap.html), [Collider2D.Cast](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Collider2D.Cast.html).

## 검증 기록

2026-09-09: Unity 6000.3.22f1의 기존 Assembly-CSharp 컴파일 응답 설정으로 CTJ 스크립트 13개와 공용 코드를 함께 컴파일했고 종료 코드 0으로 통과했다. 생성한 검증용 DLL은 임시 폴더에 출력했으며 프로젝트 어셈블리를 덮어쓰지 않았다. 변경 파일은 Assets/CTJ 내부로 확인했다. 실제 프리팹/Animator 에셋 구성 및 Play 모드 행동 검증은 아직 수행하지 않았으며 위 절차로 확인해야 한다.
