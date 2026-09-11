# Catto · Mad Ghost 프로토타입 사용 안내

## 바로 테스트하기

1. Unity에서 `Assets/CTJ/Prototypes/Scenes/EnemyArtTest.unity`를 연다. 기존 씬에 저장하지 않은 작업이 있다면 먼저 보존한다.
2. Play를 누른다. 별도의 플레이어 생성 시스템이나 키 입력은 필요 없다.
3. 왼쪽 Catto는 청록색 타깃까지 걸어간 뒤 물기 공격을 반복한다. 오른쪽 Mad Ghost는 노란색 타깃을 향해 분홍색 탄을 발사한다.
4. Hierarchy에서 `Catto Target - move this to dodge` 또는 `Ghost Target - move this to dodge`를 선택한다. Inspector의 `Enemy Damage Probe`에서 Received Hit Count와 Total Damage 증가를 확인한다. Console에도 공격자와 피해량이 표시된다.
5. Play 상태에서 Scene 뷰로 전환하고 이동 도구(W)로 타깃을 옮긴다. 타깃은 조작 캐릭터가 아니라 이동 가능한 테스트 블록이다. 수평 이동은 각자의 발판 안에서 한다.
6. Catto가 물기를 시작할 때 타깃을 위쪽으로 빠르게 옮겨 본다. 타격 이벤트 전에 판정을 벗어나면 피해가 발생하지 않는다. 계속 위에 두면 이후에는 다시 추적을 시도한다.
7. Mad Ghost의 탄이 발사된 뒤 타깃을 위로 옮기면 탄은 기존 방향으로 직진한다. 계속 범위 안에 있으면 다음 탄부터 새 위치를 조준한다.
8. 타깃을 몹 반대편으로 옮겨 Visual과 AttackHitbox/FirePoint가 함께 반전되는지 확인한다. Catto는 공격 중에는 방향을 고정하고 공격 종료 후 다시 바라본다.
9. Play를 종료하면 타깃 위치와 피해 기록은 초기화된다. 테스트 중 변경을 프리팹에 Apply할 필요는 없다.

감지/공격 범위는 타깃 Transform까지의 **2D 직선 거리**다. 높이 차이도 포함된다. 적 선택 후 Scene 뷰에서 Gizmos를 켜면 노란색 감지 범위와 빨간색 공격 시작 범위를 볼 수 있다. 현재 이동은 좌우 걷기뿐이므로 첫 테스트는 평지에서 진행한다.

## 준비된 에셋

모든 신규 에셋은 `Assets/CTJ/Prototypes` 아래에 있다. 원본 Horror Enemy Pack과 기존 Projectile 프리팹은 수정하지 않았다.

| 경로 | 용도 |
| --- | --- |
| `Prefabs/Catto_Melee.prefab` | MeleeEnemy 기반 근접 몹. 이벤트·타격 판정 연결 완료 |
| `Prefabs/MadGhost_Ranged.prefab` | RangedEnemy 기반 지상 원거리 몹. 총구·발사체 연결 완료 |
| `Prefabs/MadGhostProjectile.prefab` | 기존 CTJ Projectile을 복제한 테스트 탄 |
| `Animations/Catto.controller`, `MadGhost.controller` | 각각 Idle / Move / Attack 3개 상태 |
| `Animations/*.anim` | 각 몹의 대기·이동·공격 클립, 총 6개 |
| `Sprites/Catto.png`, `MadGhost.png` | 원본 단일 행 시트를 복제하고 일정 크기로 분할한 시트 |
| `Scenes/EnemyArtTest.unity` | 두 몹, 임시 피해 수신 타깃, 바닥, 카메라가 있는 독립 테스트 씬 |

기존 Lrw FSM과 EnemyBase 구조를 유지한다. AI의 Chase 상태와 Animator의 Move 상태는 이름이 달라도 괜찮다. AI는 행동을 결정하고 Animator는 그 결과를 화면에 표시한다.

```text
Catto_Melee / MadGhost_Ranged
├─ Rigidbody2D + 몸체 BoxCollider2D
├─ MeleeEnemy 또는 RangedEnemy
├─ StatModule + HealthModule (공용 Agent의 필수 참조)
└─ Visual
   ├─ SpriteRenderer + Animator + EnemyLocomotionAnimator
   ├─ EnemyAnimationEventRelay       ← Catto만 사용
   └─ AttackHitbox / FirePoint       ← 근접 판정 / 원거리 발사 위치
```

몸체는 루트에 고정하고 Visual의 X Scale만 반전한다. 타격 판정과 발사 위치를 Visual 아래에 두었기 때문에 방향이 같이 바뀐다. 애니메이션은 SpriteRenderer.sprite만 바꾸고 Transform에는 키를 넣지 않았다.

공용 `Agent.Awake()`는 HealthModule을 필수로 찾는다. 따라서 기존 `StatModule`과 `HealthModule`을 연결하고, Lrw의 기존 `MaxHealth.asset`을 읽기 전용으로 참조했다. StatModule의 Base Stats는 비워 두었으며 HealthModule이 최대 체력 스탯을 기본 100으로 초기화한다. 이 연결은 초기화 오류를 막기 위한 것이며, CTJ 적의 피격 메서드는 여전히 OnHit 알림만 보낸다. 적의 체력 감소·사망 처리는 이번 작업에 추가하지 않았다.

## 현재 공격 설정과 조정 지점

| 설정 | Catto | Mad Ghost |
| --- | --- | --- |
| Detection Range | 6 | 10 |
| Attack Range | 1.05 | 7 |
| Move Speed | 2 | 1.5 |
| Attack Interval | 1초 | 1.2초 |
| Damage | 10 | 5 |
| 공격 클립 | 0.6초, 10fps | 0.3초, 10fps |
| 피해/발사 시점 | 시작 후 0.2초, 접촉 검사 | 공격 시작 즉시 발사 |

### Catto

- `Catto_Attack.anim`에 `OnAttackHit`(0.2초), `OnAttackFinished`(0.55초)를 넣었다. 앞의 이벤트는 실제 타격, 뒤의 이벤트는 공격 잠금 해제다.
- 공격 중 이동과 방향을 고정한다. 몸에 계속 닿아 있다고 자동으로 피해가 반복되지는 않는다. 이벤트 순간 AttackHitbox와 지정 타깃 Collider가 겹칠 때만 한 번 피해를 전달한다.
- AttackHitbox의 Offset=(0.65, 0.34), Size=(0.9, 0.65)다. 공격 시작 거리와 실제 판정은 별개이므로 Attack Range를 늘렸다면 Hitbox도 함께 확인한다.
- Animation 창에서 공격 클립을 선택하고 이벤트 마커를 옮기면 타격감을 조정할 수 있다. 종료 이벤트는 클립 끝보다 약간 앞에 유지한다. Attack Timeout=1.5초는 종료 이벤트 누락 시 복구용이지 정상 공격 주기가 아니다.

### Mad Ghost

- 외형은 유령이지만 현재는 사용자 요청에 맞춘 **지상 이동형**이다. 비행이나 높이 추적 기능은 없다.
- 제공된 낫 공격 3프레임을 발사 동작으로 임시 사용한다. 탄은 첫 프레임에 나오며 Animation Event로 발사하지 않는다.
- 루트의 Attack Interval로 발사 주기를, Visual/FirePoint의 위치로 총구를 조정한다. 발사체 프리팹의 Speed=8, Lifetime=4초다.
- RangedEnemy의 Attack Animator는 선택 참조다. 비워도 발사 자체는 작동한다. 현재 연결된 Animator는 발사 시 `Base Layer.Attack`을 재생하고 종료 후 Idle로 돌아간다.
- 발사체는 발사 순간 타깃 위치를 향하며 유도하지 않는다. 기본 Blocking Layers는 전체 레이어이며 기존 코드가 적·다른 탄·비타깃 Trigger를 제외한다. 실제 맵에서는 벽/지형 레이어를 기준으로 좁혀 조정할 수 있다.

## 스프라이트와 애니메이션 구성 이유

원본 개별 PNG 중 Mad Ghost는 5배 확대된 파일이어서 Catto와 그대로 섞으면 픽셀 밀도가 달라진다. 두 몹 모두 원본 단일 행 시트의 픽셀 크기를 기준으로 복제·분할했다.

- Catto: 셀 48×32, 전체 35프레임. Mad Ghost: 셀 64×64, 전체 31프레임.
- PPU=32, Filter Mode=Point, 압축 없음. 픽셀 밀도를 통일했으며 전체 캔버스 크기와 캐릭터 몸체 크기는 다르다.
- 모든 프레임에 Bottom Center 피벗과 고정 셀을 적용했다. 프레임마다 투명 영역을 잘라 피벗 위치가 흔들리는 것을 막는다.
- Unlit 스프라이트 머티리얼을 사용해 테스트 씬에 2D 조명이 없어도 보이도록 했다. 현재 카메라에는 별도 픽셀 퍼펙트 설정을 추가하지 않았다.

프레임 번호는 0부터 시작하며 원본 ReadMe의 구분을 사용했다.

| 몹 | Idle | Move | Attack |
| --- | --- | --- | --- |
| Catto | 14–17, 앉은 대기, 6fps | 3–6, 걷기, 10fps | 18–23, 물기, 10fps |
| Mad Ghost | 0–3, 대기, 6fps | 4–7, 이동, 10fps | 13–15, 낫 공격, 10fps |

현재는 Catto의 일어서기/앉기/턴 동작과 Mad Ghost의 낫 꺼내기/집어넣기를 생략했다. 먼저 판정과 거리감을 확정한 다음 연결 동작을 추가하는 순서를 권한다. Hit/Death 프레임도 시트에 남아 있지만 상태에는 아직 연결하지 않았다.

## 기존 맵과 플레이어에 붙이기

1. CTJ 소유 씬의 Collider2D가 있는 평평한 바닥 위에 두 적 프리팹을 드래그한다. 처음에는 루트 Scale=(1,1,1)을 유지한다.
2. 프리팹 원본의 Target은 비어 있다. 기존 EnemyBase가 PlayerController를 0.2초마다 찾아 최초 연결하므로 플레이어가 나중에 생성되어도 된다. 생성 관리자가 있다면 `enemy.SetTarget(player.transform)`으로 직접 연결해도 된다.
3. 임시 블록을 타깃으로 쓸 때는 직접 Target을 지정한다. 자동 탐색은 EnemyDamageProbe가 아니라 PlayerController를 찾는다.
4. **현재 공용 PlayerController는 Agent를 통해 IDamageable을 상속한다.** 실제 플레이어의 StatModule/HealthModule이 구성되어 있다면 공용 ApplyDamage 경로로 피해를 받을 수 있다. 플레이어의 체력·죽음 처리까지 이번 테스트에서 검증한 것은 아니다.
5. 실제 플레이어에는 EnemyDamageProbe를 추가하지 않는다. 같은 계층에 IDamageable 구현이 둘이면 피해 수신기가 모호해질 수 있다. Probe는 이번 씬의 별도 테스트 타깃에만 사용한다.
6. 보스, 사망·넉백, 절벽 감지, 장애물 우회, 풀링은 이번 작업에 포함하지 않았다. 우선 평지에서 거리 → 공격 간격 → 타격/총구 위치 → 연결 애니메이션 순으로 조정하면 된다.

## 이번에 사용한 문법과 이유

### 게임 실행 코드

- `[SerializeField] private Animator attackAnimator`: 외부 코드에 필드를 공개하지 않고 Inspector에서 Animator를 연결한다. 기존 RangedEnemy에 선택적인 발사 연출만 추가했다.
- `static readonly int` + `Animator.StringToHash(...)`: `IsMoving`과 `Base Layer.Attack` 이름을 정수 식별자로 한 번 계산해 재사용한다. `const`는 컴파일 시점 값만 가능하지만 이 값은 Unity 함수 호출 결과이므로 readonly를 쓴다. static은 모든 인스턴스가 값을 공유한다는 뜻이다.
- `LateUpdate()`: EnemyLocomotionAnimator가 현재 Rigidbody2D의 수평 속도를 읽어 IsMoving Bool을 갱신한다. AI 이동 코드를 다시 구현하지 않고 실제 움직임을 관찰한다. 지면 판정이나 경로 탐색 역할은 없다.
- `Mathf.Abs(velocity) > 0.01f`: 좌우 이동을 같은 조건으로 처리하고 작은 물리 오차가 걷기 애니메이션을 켜지 않도록 한다.
- `&&`의 단락 평가와 null 검사: Animator가 없거나 비활성화된 기존 원거리 적도 공격 기능을 그대로 사용할 수 있도록 순서대로 조건을 검사한다.
- `[RequireComponent(typeof(Animator))]`와 `GetComponentInParent<EnemyBase>()`: Visual에 필요한 Animator를 함께 두고, 부모 루트의 공통 적 컴포넌트를 찾는다. 새 인터페이스나 추가 상속 계층은 만들지 않았다.

### 에셋 구성·검증 도구에만 사용한 문법

`Assets/CTJ/Tools~`는 Unity가 자동 임포트/컴파일하지 않는 보조 폴더다. 여기의 C#은 기존 Unity Pipeline의 run_script로만 실행했으며 게임 로직이나 빌드에 포함되지 않는다. 일반적인 프리팹 사용에는 실행할 필요가 없다.

- `AnimationUtility`와 `ObjectReferenceKeyframe[]`: 프레임별 Sprite 참조를 클립에 기록한다. 실수로 위치/Scale 키가 들어가는 것을 피하며 이벤트 시간도 명시적으로 기록했다.
- `SerializedObject` / `SerializedProperty`: private 직렬화 필드를 에디터 도구에서 Inspector와 같은 방식으로 연결한다. 런타임 공개 setter를 도구용으로 늘리지 않았다.
- `try/finally`: 생성/검증 도중 예외가 나도 임시로 연 씬과 프리팹 편집 내용을 닫고 테스트 타깃 위치를 복원한다.
- `async Task`, `await Task.Yield()`, `Func<bool>`: 검증 도구에서 다음 프레임을 기다리며 실제 공격 시작/종료 조건을 확인한다. Unity 메인 스레드를 정지시키지 않는다. 적의 기존 코루틴을 이것으로 교체한 것은 아니다.

`BuildEnemyArt.cs`는 최초 구성 기록이며 Prototypes 폴더가 이미 있으면 Build 실행을 거부한다. 조정한 에셋을 실수로 덮어쓰지 않기 위함이다. `InspectEnemyArt.cs`는 이번 Play 검증 도구다. 별도 패키지나 asmdef는 추가하지 않았다.

## 검증 기록

2026-09-11, Unity 6000.3.22f1:

- Unity 스크립트 재컴파일 완료, 컴파일 오류 없음.
- 프리팹 3개, Animator 2개, 클립 6개, 전체 스프라이트 66개 및 Catto의 두 이벤트 검사.
- 실제 Play에서 추적 후 Catto의 반복 타격, Mad Ghost의 반복 발사와 타깃 피해 확인.
- Catto 타격 이벤트 전에 회피하면 피해 0, 두 몹 모두 왼쪽으로 방향 전환 후 타격 성공.
- 최초 테스트에서 발견한 공용 Agent의 HealthModule 필수 참조 누락은 CTJ의 두 적 프리팹에 기존 모듈을 연결하여 보완.
- 보완 후 Play 약 160초 시점에 두 몹의 HealthModule 초기화(최대 체력 100), Catto 159회/1590 피해, Mad Ghost 133회/665 피해 확인. 회피/왼쪽 공격도 재검증 통과. 최종 Console 오류 0건.
- 테스트 종료 후 기존 CTJ_MapTestScene과 Play 시작 씬 설정 복원. 기존 씬은 저장하거나 수정하지 않음.

독립 테스트 타깃의 IDamageable 피해 수신을 검증한 것이며, 실제 플레이어 스폰/피격 통합 및 복잡한 맵 이동은 위 절차로 별도 확인해야 한다.
