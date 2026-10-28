# 09-04 메카닉 궁수 — 구현·테스트·문법 설명

2026-10-26. [기획 원문](https://app.notion.com/p/09-04_-7c09e7231ca7826ebe5e01f892950b08?source=copy_link)은 읽기만 했습니다.

## 사용할 에셋

- 적: `Assets/CTJ/Prototypes/Prefabs/Enemy04_MechanicArcher.prefab`
- 기본 스탯: `Assets/CTJ/Prototypes/Stats/Enemy04_MechanicArcher_Stats.asset`
- 스킬 키: `Assets/CTJ/Prototypes/Skills/Enemy04_MechanicArcher_Shot.asset`
- 화살: `Assets/CTJ/Prototypes/Prefabs/Enemy04_Arrow.prefab`
- 붉은 조준선: `Assets/CTJ/Prototypes/Prefabs/Enemy04_AimLine.prefab`

MadGhost의 지상 원거리 적 구조와 임시 스프라이트를 복제했습니다. 옅은 파란색으로 구별하며 몸체 콜라이더 높이는 1.7m입니다. 전용 활/공격 애니메이션과 소리는 아직 연결하지 않았습니다. 공격은 타이머로 발사하고 기존 이동·사망 애니메이션을 사용합니다.

## 동작과 조절 위치

| 항목 | 기본값 | 수정 위치 |
| --- | --- | --- |
| 체력 | 55 | 전용 Stats 에셋의 HP |
| 피해 | 14 | `SkillModule/ProjectileAttack → Damage` |
| 이동 속도 | 3.2m/s | 루트 `RangedEnemy → Move Speed` |
| 탐지 / 추적 해제 | 12m / 14m | 루트 `Detection Range / Chase Release Range` |
| 최소 유지 거리 | 5m | `KeepDistanceModule → Minimum Distance` |
| 사격 시작 범위 | 5~11m | `SkillModule → Skills[0] → Min Range / Max Range` |
| 준비 | 합계 0.8초 | Stats의 공격 준비 0.65초 + `Post Blink Delay` 0.15초 |
| 발사 후 빈틈 | 0.6초 | `ProjectileAttack → Recovery Time` |
| 쿨타임 | 2.8초 | `ProjectileAttack → Base Cool Time` |
| 화살 속도 / 수명 | 15m/s / 0.8초 | `ProjectileAttack → Projectile Speed / Projectile Lifetime` |
| 화살 폭 | 0.25m | `Enemy04_Arrow → CircleCollider2D → Radius = 0.125` |
| 조준선 표시 | 켜짐 | `ProjectileAttack → Show Aim Line` |
| 조준선 두께 | 0.045m | `ProjectileAttack → Aim Line Width` |
| 조준선 알파 | 100/255 | `Enemy04_AimLine → Area Color` |
| 경직 | 0.22초 | `HitReactionModule → Hit Stun Duration` |
| 넉백 속도 / 시간 | 7.04 / 0.18초 | `HitReactionModule → Knockback Speed / Duration` |
| 처치 보상 | Gold 10 | 전용 Stats 에셋의 Enemy Gold Reward |

`Minimum Distance`를 바꾸면 스킬의 `Min Range`도 같은 값으로 맞추세요. 지면 이동과 사격 조건은 서로 다른 책임이라 각각 설정합니다. 모듈 선택 시 자홍색 기즈모로 최소 거리를 확인할 수 있습니다.

평상시 기다리다가 12m 안으로 들어온 플레이어를 추적합니다. 11m보다 멀면 접근하고, 5m보다 가까우면 플레이어를 바라보며 뒤로 걷습니다. 5~11m에서는 정지하고 쿨타임을 기다리거나 사격합니다. 거리 계산은 기존 스킬과 동일한 **루트 위치 사이의 2D 거리**입니다. 벽·낭떠러지 앞에서는 정지하며, 점프나 여러 층 경로 탐색은 하지 않습니다. 완전히 겹쳤을 때는 마지막으로 알던 플레이어 쪽의 반대로 후퇴합니다.

추적 중 14m를 넘으면 현재 위치에서 대기합니다. 시작 위치로 귀환하지 않습니다. 공격 준비/발사 후 빈틈 동안에는 후퇴하지 않습니다. 준비 중 상대가 최소 사거리 안으로 들어와도 이미 시작한 사격은 마무리하며, 빈틈이 끝난 뒤 후퇴합니다.

공격 준비의 앞 0.65초는 플레이어를 따라 조준하고 몸체도 좌우로 돌아봅니다. 마지막 0.15초는 조준선과 몸체 방향을 고정하여 회피할 시간을 줍니다. 이 고정 시간에는 점멸을 넣지 않았습니다. 발사 후 화살은 추적하지 않습니다. 화살이 지형/타깃에 닿거나 수명을 다하면 제거됩니다. **속도 × 수명 = 약 12m**이며 물리 프레임 단위 오차가 있습니다. 속도를 바꾸면서 사거리를 유지하려면 수명도 `12 / 속도`로 맞추세요.

쿨타임은 공용 스킬 시스템대로 **준비 시작부터** 계산합니다. Recovery Time은 발사 후 행동 잠금 시간이며 쿨타임과 다른 값입니다. 준비 중 맞으면 취소되지만 이미 발사한 화살은 공격 취소·추적 해제·발사자 사망으로 제거되지 않습니다.

기획의 “0.2초 경직 / 경직·넉백 1.1배”는 경직 0.22초, 기존 넉백 속도 6.4 × 1.1 = 7.04로 해석했습니다. 다른 적의 최근 0.3초 설정은 그대로입니다. 흰색 혼합 점멸과 스케일 변형도 기존 피격 모듈을 재사용합니다.

## 구조와 이번 범위

```text
Enemy04_MechanicArcher (RangedEnemy, Rigidbody2D, Trigger hurtbox)
├─ Visual / HitVisual / FirePoint
├─ EnemyMovement
├─ TerrainBody
├─ StatModule
├─ HealthModule
├─ SkillModule / ProjectileAttack
├─ HitReactionModule
├─ CurrencyRewardModule
├─ DeathModule
└─ KeepDistanceModule
```

- `EnemyKeepDistanceModule`만 새 런타임 모듈입니다. 기존 Idle/Chase/Attack FSM을 사용하며, 별도 FSM이나 적 전용 추상 클래스를 추가하지 않았습니다.
- `EnemyBase`는 이동 요청 시 선택형 거리 유지 모듈이 있으면 위임합니다. 모듈이 없는 기존 적은 원래 경로를 실행합니다.
- 기존 `EnemyProjectileSkill`에 `Face Target During Preparation` 옵션을 추가했습니다. 기본값은 꺼짐이며 04번만 켰습니다. 기존 02 터렛의 회전 로직은 그대로입니다.
- 공용 스킬 등록, 고유 Skill SO, HealthModule, 피격, CurrencyManager 연결, 처치 집계를 그대로 사용합니다. 사망 시 Gold 10과 처치 수 +1을 한 번 지급합니다.
- 기존 정책에 따라 재화는 즉시 지급합니다. 기획의 “사망 위치 드롭 후 자동 획득”에 해당하는 바닥 드롭 연출은 이번 작업에 포함하지 않았습니다.
- 기존 일반 적들에서 임시 체력 바를 제거한 사용자 변경을 보존했습니다. 04번에도 붙이지 않았습니다. 필요하면 공유 `EnemyTemporaryStatusUI` 프리팹을 루트 자식으로 추가할 수 있습니다.
- 최초 생성 위치인 “플레이어에서 7~10m 떨어진 발판”은 맵의 배치/생성기에서 지정합니다. 동료의 방/스폰 데이터나 현재 테스트 씬은 수정하지 않았습니다.
- 방어력은 기존 EnemyBase처럼 전달된 양을 그대로 HP에서 차감하므로 기획의 방어력 0과 같습니다.

## 직접 테스트하기

1. 저장한 테스트 씬에 `Enemy04_MechanicArcher`를 드래그합니다. 루트 원점이 발 위치이므로 Ground 레이어 발판 윗면에 놓습니다. 좌우 15m 이상 공간이 있으면 거리 확인이 쉽습니다.
2. 기존 PlayerPack과 GameManager/Gold, 별도 EnemyKillCountManager를 사용합니다. Play 후 PlayerController를 찾으면 자동으로 Target이 연결됩니다. 더미 타깃을 쓸 때는 루트 Target을 직접 연결하고 Collider2D + EnemyDamageProbe를 붙입니다.
3. 처음 13m에 있으면 대기하고, 12m 안에 들어오면 접근하며, 11m 안에서 멈추는지 봅니다.
4. 5m보다 가까이 접근합니다. 적이 플레이어를 바라본 채 3.2m/s로 뒤로 걷는지, 반대편으로 넘어가면 후퇴 방향이 바뀌는지 확인합니다. 뒤에 벽/낭떠러지를 배치해 넘어가지 않는지도 봅니다.
5. 5~11m에서 위아래·좌우로 이동합니다. 붉은 조준선이 약 0.65초 따라오다가 마지막 0.15초 고정된 방향으로 화살이 나가야 합니다. 이후 약 0.6초 정지합니다. Show Aim Line을 꺼도 사격은 유지되어야 합니다.
6. 준비 중 적을 공격합니다. 조준선이 즉시 꺼지고 화살이 나오지 않아야 합니다. 경직이 끝난 뒤 다시 행동합니다. 실제 플레이어 스킬/무적/회피에 따른 최종 피해는 실제 맵에서도 확인하세요.
7. 발사된 화살 앞을 벽으로 가리면 막히고, 발사 후 플레이어가 멀어지거나 적이 죽어도 날아가던 화살은 유지되어야 합니다.
8. HP 55를 소진시킨 뒤 Gold가 10, 총 처치 수가 1 증가하는지 확인합니다. `Test/Take 2 Damage (Play Mode)` 메뉴로도 HP 감소/사망 검사를 할 수 있습니다.

## 사용한 문법과 이유 — 학습용

- **컴포지션(선택형 모듈)**: 새 적 클래스를 계속 상속하기보다, 이번 적에게 필요한 거리 유지 규칙을 모듈 자식으로 붙입니다. 기존 모듈 목록에서 조회하므로 배회/이동/공격의 책임을 분리할 수 있습니다.
- **`[SerializeField] private` / `[Min]` / `[Tooltip]`**: 외부 코드가 임의로 설정을 바꾸지 않게 private으로 두면서 인스펙터에서 조절하고 설명을 읽게 합니다. Min은 인스펙터 입력 제약이며 런타임 계산에도 `Mathf.Max`를 사용했습니다.
- **읽기 전용 프로퍼티 `MinimumDistance => ...`**: 외부 검증과 기즈모에서 값을 읽되 필드에 직접 쓰지 못하게 합니다. `=>`는 한 줄 반환을 간결하게 쓰는 표현식 본문입니다.
- **`Vector2` / `sqrMagnitude` / `Mathf.Sign`**: 2D 거리 비교는 제곱 값으로 하고, 좌우 방향은 +1/-1로 구합니다. 완전히 수직으로 겹쳐 방향이 정해지지 않을 때는 마지막 좌우 방향을 유지합니다.
- **`Mathf.Sqrt(r*r - y*y)`**: 원형 사거리 r에서 높이 차이 y가 있을 때 필요한 가로 거리를 구하는 피타고라스 정리입니다. 불필요하게 r 전체만큼 추가 후퇴하지 않도록 합니다.
- **`out _`**: 기존 `MoveOnGroundTowards`가 돌려주는 blocked 값은 이번 규칙에서 사용하지 않으므로 버립니다. 지형 검사는 그 메서드 안에서 수행되며 막히면 이동 속도를 0으로 만듭니다.
- **`?.`**: 초기화 전 참조가 없을 때 `_movement?.Stop()`은 호출을 건너뜁니다. 실제 동작 조건은 앞의 null 검사와 활성화 검사로 확인합니다.
- **`internal`**: 같은 어셈블리의 EnemyBase/스킬에서만 사용할 이동·방향 전환 진입점입니다. 사용자용 공개 API를 불필요하게 늘리지 않습니다.
- **`SerializedObject` / `PrefabUtility.LoadPrefabContents`**: Tools~ 생성 도구에서 프리팹 복제본의 직렬화 필드를 설정하고 새 경로에만 저장합니다. 플레이 중 리플렉션으로 적을 제어하는 방식은 아닙니다.
- **`try/finally`**: 검증 실패 시에도 임시 오브젝트와 렌더 텍스처를 정리하고 Time.timeScale을 되돌립니다. 테스트가 사용자 씬/진행 데이터를 남기지 않도록 합니다.

## 검증 도구

`BuildEnemy04.cs`는 최초 생성용이며 기존 에셋이 있으면 덮어쓰지 않습니다. `VerifyEnemy04.RunEdit`은 참조와 설정을, `RunPlay`는 저장하지 않는 빈 씬에서 행동/피해/보상을 검사합니다. 기존 `VerifyEnemy01.BeginPlay/StopPlay/RestoreScene`을 사용합니다. Tools~ 코드는 게임 빌드에 포함되지 않습니다.

2026-10-26 검증 결과:

- Unity 컴파일 성공, Missing Script / 끊어진 오브젝트 참조 없음, 마지막 Console 오류 0건.
- 04번 격리 Play: 지연 타깃, 12/14m 탐지·해제, 3.2m/s 접근·좌우 후퇴, 몸체 방향, 사거리 정지, 벽/낭떠러지 안전 검사 통과.
- 조준선 토글·추적·방향 고정·0.8초 준비·피해 14·0.6초 후딜·2.8초 쿨타임·준비 중 피격 취소·0.22초 경직 검사 통과.
- 시야 차단, 얇은 벽의 화살 차단, 수명에 따른 약 12m 비행(측정 11.992m), 타깃 해제/발사자 사망 후 화살 유지 검사 통과.
- 실제 CurrencyManager 런타임 인스턴스로 Gold 10, EnemyKillCountManager 처치 수 +1 및 중복 사망 무시 확인. 저장 함수는 호출하지 않았습니다.
- 기존 01번 배회·추격·공격·피격 회귀 검사 및 02번 전개·조준·발사·피격 취소 회귀 검사 통과.
- 실제 URP 렌더링으로 붉은 조준선 표시 확인. 미리보기: `Enemy04_AimingPreview.png` (청록 사각형은 검사 타깃).
- 임시 씬을 저장하지 않고 CTJ_MapTestScene으로 복귀했습니다. 기존 적 4개 프리팹과 해당 씬의 작업 전후 SHA-256이 같아 사용자 변경이 보존됨을 확인했습니다.

자동 검사는 더미 타깃을 사용했습니다. 실제 PlayerPack의 공격·회피·무적·맵 생성과의 최종 체감은 위 수동 순서로 확인하세요.
