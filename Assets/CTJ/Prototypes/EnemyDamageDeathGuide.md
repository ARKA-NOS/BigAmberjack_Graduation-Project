# 플레이어 공격 전달 · 적 피격/사망 테스트

## 결론과 연결 경로

현재 플레이어 공격은 이미 피해 데이터를 전달한다. 동료 코드나 플레이어 프리팹은 수정하지 않았다.

`PlayerMeleeSkillModule.HandleDmgCast()` → `OverlapDamageCaster.CastDamage()` → 충돌한 오브젝트의 `IDamageable.ApplyDamage()` → CTJ `EnemyBase` → 기존 `HealthModule`

- 플레이어 공격 모듈은 현재 프리팹 설정 기준으로 공격력 2를 전달한다.
- 공용 OverlapDamageCaster는 맞은 Collider의 **같은 GameObject**에서 `TryGetComponent<IDamageable>()`를 호출한다. 부모를 탐색하지 않는다.
- Catto/Mad Ghost는 루트에 몸체 Collider2D와 EnemyBase 파생 컴포넌트를 함께 두었으므로 그대로 피해를 받을 수 있다. 근접 AttackHitbox 자식에 피해 수신기를 추가하지 않았다.
- 이번 검증은 실제 플레이어 프리팹에서 공격 모듈과 Caster를 런타임 복제하고, 모듈의 실제 `HandleDmgCast()`를 호출해 물리 판정부터 사망까지 확인했다. 공격 입력·VFX·전체 플레이어 초기화는 우회했으며, 전체 플레이어 조작 테스트를 했다는 의미는 아니다.

## 플레이어 없이 바로 확인하기

1. `Assets/CTJ/Prototypes/Scenes/EnemyArtTest.unity`를 열고 Play를 누른다.
2. Hierarchy에서 **Catto_Melee 루트**를 선택한다. Visual이나 청록색 타깃이 아니다.
3. Inspector의 **Melee Enemy 컴포넌트 제목을 우클릭**하거나 우측 메뉴를 열고 `Test → Take 2 Damage (Play Mode)`를 선택한다.
4. 같은 루트의 Health Module에서 Current Health가 `6 → 4`로 줄어드는지 확인한다. 두 번 더 실행하면 `2 → 0`이 된다.
5. 체력이 0이 되는 즉시 이동·물기 공격·몸체/타격 Collider가 정지한다. 0.9초 사망 애니메이션을 재생하고 사망 시점부터 1.1초 뒤 Hierarchy에서 제거된다.
6. **MadGhost_Ranged 루트의 Ranged Enemy 컴포넌트**에서도 같은 메뉴를 사용한다. `8 → 6 → 4 → 2 → 0`, 총 4회에 사망한다.
7. Play를 종료하고 다시 시작하면 두 적이 새 체력으로 다시 나온다. Play 중 사라진 오브젝트를 씬에 저장할 필요는 없다.

이 메뉴는 에디터에서만 존재하며 Play가 아니면 아무 일도 하지 않는다. `IDamageable` 수신 경로에 2 피해를 직접 넣는 간편 검사이므로, 플레이어 공격 범위·마우스 입력 검사는 아래 절차로 구분한다.

### 추가로 볼 동작

- Catto가 물기 준비 중일 때 마지막 피해를 넣으면 남은 타격 이벤트가 청록색 타깃을 공격하지 않아야 한다.
- 죽은 적의 Collider는 모두 꺼져 시체가 길을 막거나 추가 공격을 받지 않는다. 물리 시뮬레이션도 꺼지므로 그 위치에서 사망 연출을 재생한다.
- Mad Ghost는 사망 후 새 탄을 발사하지 않는다. **이미 발사한 탄은 기존 수명/충돌 규칙대로 남는다.** 공격자 사망 시 탄을 모두 지우는 규칙은 추가하지 않았다.
- 일반 피격에는 별도 경직·무적 시간·넉백을 추가하지 않았다. 체력이 남아 있으면 기존 행동을 계속하고 OnHit 알림을 보낸다.

## 실제 플레이어와 확인하기

1. 플레이어 생성이 정상 동작하는 **CTJ 소유 씬**의 평지에 Catto_Melee / MadGhost_Ranged 프리팹을 놓는다. 기존 동료 씬·프리팹 원본은 수정하지 않는다.
2. 적 Target은 비워 두면 기존 방식대로 지연 생성된 PlayerController를 찾는다. 또는 생성 후 `SetTarget(player.transform)`으로 연결한다.
3. 플레이어 공격 방향을 적에게 맞추고 **마우스 왼쪽 버튼을 눌렀다 놓는다**. 현재 PlayerInputSo는 입력의 `canceled` 시점에 공격 이벤트를 보낸다.
4. 플레이어 DamageCaster의 빨간 Gizmo 안에 적 몸체가 들어가는지 확인한다. 현재 프리팹은 Circle, Radius=2이며 공격 방향 쪽으로 Caster가 배치된다.
5. 적의 Health Module을 보면서 한 번의 유효 공격에 2씩 감소하는지 확인한다. 기본 테스트 체력에서는 Catto 3회, Mad Ghost 4회에 사망한다.

### 수정하지 않은 공용 영역의 주의점

- 현재 `Assets/GameModules/Player/Prefabs/Player.prefab` 원본에는 **HealthModule과 StatModule이 모두 없다**. 공용 Agent는 HealthModule을 필수로 요구하므로, 스폰 과정에서 따로 보완하지 않는 환경에서는 초기화 Assert가 나거나 플레이어 피격 시 오류가 날 수 있다. 이번 작업에서는 원본을 수정하지 않았다. 위의 플레이어 없는 테스트 메뉴로 적 자체의 검증은 가능하다.
- 공용 Caster는 현재 레이어 필터를 사용하지 않고 최대 5개의 Collider를 조회한다. 플레이어의 넓은 판정이 주변 임시 타깃 등 다른 IDamageable도 맞힐 수 있다. 벽/지형이 빽빽하거나 Collider가 많은 상황은 별도로 확인해야 한다. 공용 공격 필터나 판정 개수는 변경하지 않았다.
- 피해 수신용 EnemyDamageProbe를 적이나 실제 플레이어에 중복 부착하지 않는다. 같은 오브젝트/계층에 수신기를 추가할 필요가 없다.

## 바꾸고 싶은 값

| 대상 | 수정할 CTJ 에셋/필드 | 현재 값 |
| --- | --- | --- |
| Catto 최대 체력 | `Prototypes/Stats/Catto_Stats.asset` → Stats의 MaxHealth 항목 → Base Value | 6 |
| Mad Ghost 최대 체력 | `Prototypes/Stats/MadGhost_Stats.asset` → Stats의 MaxHealth 항목 → Base Value | 8 |
| 사망 후 제거 시간 | 각 적 루트 → Death Despawn Delay | 1.1초 |
| 사망 연출 | `Catto_Death.anim` / `MadGhost_Death.anim` | 9프레임, 10fps, 0.9초, 비반복 |

StatGroup은 Lrw의 기존 클래스를 사용한 **CTJ 전용 데이터 에셋**이다. Lrw의 MaxHealth StatData는 스탯 종류를 나타내는 참조일 뿐이며 수정하지 않았다. Health Module의 Current Health를 에디터에서 바꾸는 것은 초기 최대 체력 설정이 아니다. 실행 시 StatGroup 값으로 초기화된다.

각 Animator의 Base Layer에 `Death` 상태를 추가했다. 다른 상태로 나가는 전환은 없고 코드가 직접 진입한다. 사망 클립 길이를 늘리면 Death Despawn Delay도 그보다 길게 바꾼다. Catto는 원본 26–34번, Mad Ghost는 22–30번 프레임을 사용했다(0부터 시작).

## 사용한 문법과 선택 이유

### `new virtual`과 인터페이스 재구현

공용 `Agent.ApplyDamage()`는 virtual이 아니어서 `override`로 바꿀 수 없다. 동료 코드를 수정하지 않기 위해 EnemyBase가 이미 선언한 `IDamageable` 구현을 유지하고 `public new virtual void ApplyDamage(...)`에 공통 피격 처리를 넣었다.

- `new`: 같은 이름의 부모 메서드를 의도적으로 숨긴다는 표시다. 부모 구현 자체를 바꾸지는 않는다.
- `virtual`: 기존 EnemyPrototype이 로그를 추가하는 override를 유지할 수 있게 한다. 해당 override도 이제 `base.ApplyDamage(...)`를 호출한다.
- MeleeEnemy와 RangedEnemy는 동일한 피격 처리를 다시 작성하지 않고 상속한다. 각자의 Attack 구현만 다르다.
- `IDamageable`로 호출하면 CTJ 구현에 도달한다. 반면 **Agent 타입으로 직접 캐스팅해 호출하면 공용 메서드가 실행**된다. 두 호출을 같은 것으로 보면 안 된다. 실제 플레이어 Caster는 IDamageable을 사용하며 이번 검증도 이 경로로 통과했다.

### 이벤트 `+=` / `-=`

EnemyBase가 기존 `HealthModule.OnHealthChanged`에 구독하여 체력 0을 감지한다. 따라서 ApplyDamage 외에 다른 시스템이 HealthModule을 직접 0으로 만들더라도 사망한다. OnDestroy에서 구독을 해제해 남은 참조를 정리한다. 별도의 체력 숫자를 EnemyBase에 중복 저장하지 않는다.

### `IsDead { get; private set; }`와 조기 반환

외부에서는 사망 여부를 읽을 수 있지만 임의로 바꿀 수 없다. 사망 플래그를 가장 먼저 설정해 중복 피해·여러 번의 사망 실행을 막는다. 0 이하 피해와 NaN/Infinity도 무시해 잘못된 값이 체력을 오염시키지 않게 한다.

### `enabled = false`와 Rigidbody2D.simulated

Enemy 컴포넌트만 꺼서 FSM의 Update/FixedUpdate를 멈추고, Visual의 Animator는 켜 둔다. GameObject 전체를 끄면 사망 애니메이션까지 멈추기 때문이다. Rigidbody2D.simulated=false와 모든 Collider 비활성화로 시체의 이동/충돌을 중단한다. Catto의 기존 OnDisable은 공격 잠금을 해제하고 타깃을 정리하므로 남은 이벤트에도 피해가 나가지 않는다.

### `Destroy(gameObject, delay)`

애니메이션을 보여 줄 시간 뒤에 오브젝트를 제거한다. 제거는 Enemy 컴포넌트가 비활성화되어도 예약대로 진행된다. 사망 애니메이션 이벤트에 제거를 의존시키지 않았다. 풀링·부활 규칙은 아직 없으므로 사망한 인스턴스를 다시 활성화해 재사용하지 않는다.

### `#if UNITY_EDITOR`와 `[ContextMenu]`

에디터에서만 컴포넌트 메뉴에 피해 테스트 기능을 노출한다. 배포 빌드에는 테스트 메서드가 포함되지 않는다. 이 메뉴에서도 직접 체력 숫자를 바꾸지 않고 IDamageable을 통해 피해를 넣는다.

### 검증 도구의 리플렉션

`Tools~/VerifyEnemyDeath.cs`만 `GetField`/`GetMethod`로 플레이어 공격 모듈을 검사한다. private 메서드를 테스트하려고 동료 코드를 public으로 변경하지 않기 위한 선택이다. 게임 실행 코드에는 리플렉션을 추가하지 않았다. 전체 입력/시각 효과 초기화 대신 실제 공격 모듈의 피해 전달 부분만 분리해 검증했다.

## 검증 기록

2026-09-11, Unity 6000.3.22f1:

- CTJ 변경 스크립트 컴파일 오류 없음. 두 프리팹의 체력 데이터·Death 상태·비반복 클립·제거 시간 참조 검사 통과.
- 실제 플레이어 프리팹의 공격 모듈/OverlapDamageCaster를 이용한 Play 검증: Catto `6/4/2/0`, Mad Ghost `8/6/4/2/0`.
- 사망 시 모든 Collider 해제, 물리 이동 중단, Death 상태와 사망 프레임 재생, 지연 제거 확인.
- Catto 공격 중 사망 후 타격 이벤트를 호출해도 추가 피해 0. Mad Ghost 사망 후 새로운 발사체 없음.
- 0/음수/NaN/Infinity 피해 무시, HealthModule을 직접 0으로 바꾸는 경로의 사망도 확인.
- 두 적의 상속된 ContextMenu 메서드가 정확히 2 피해를 전달하는 것도 확인.
- 최종 Play 테스트 종료 후 Console 오류 0건, 경고 0건 확인.
- 최초 검증에서는 플레이어의 범위 공격이 옆의 임시 타깃까지 맞힌 것을 사후 타격으로 잘못 센 테스트 기준을 보완한 뒤 재검증 통과.
- 테스트는 Play 인스턴스에서만 실행했으며 기존 씬 에셋과 플레이어 원본을 저장/수정하지 않았다.
