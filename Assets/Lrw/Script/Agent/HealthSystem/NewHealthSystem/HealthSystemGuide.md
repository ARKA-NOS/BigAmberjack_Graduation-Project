# 체력 시스템 (NewHealthSystem)

기본 체력 1개 위에 추가 체력(보호막, 방어 체력, 흡혈 초과 체력 등)을 겹쳐 쓰는 구조다.
`NewHealthModule`은 `IHealthModule`을 그대로 구현하므로 기존 `HealthModule` 자리에 바꿔 끼울 수 있다.

## 구성

| 파일 | 역할 |
|---|---|
| `NewHealthModule` | 체력 목록 관리, 피해/회복 순서, 사망 판정, UI 바인드 |
| `AbstractHealthSo` | 체력 하나의 데이터(표시, 우선순위, 최대치, 회복 출처, 수명) + 런타임 체력 생성 |
| `AbstractHealth` | 체력 하나의 런타임 상태. 공통 흐름은 고정, 종류별 동작은 훅으로 |
| `IHealth` | 체력 하나의 읽기 전용 조회 |
| `IHealthLayerModule` | 추가/제거/조회 (`AddHealth`, `RemoveHealth`, `TryGetHealth`, `Healths`) |
| `HealthChangeData` / `HealthChangeResult` | 피해/회복 요청과 결과(실제 적용량, 남은 양) |
| `HealSourceType` / `HealthTag` | 회복 출처 / 체력 분류(피해 `IgnoreTags`, UI 묶음 조회) |

## 피해 / 회복 순서

- **피해**: 추가 체력을 `Priority` 높은 순으로 → 남은 피해는 기본 체력. 기본 체력은 Priority와 상관없이 항상 마지막이다.
  - 빈 체력과 `IgnoreTags`에 걸린 체력은 건너뛴다.
  - 체력마다 피해를 변환할 수 있다(`DamageReductionHealth`: 받는 피해 × Rate, 남은 양은 원래 단위로 되돌려 넘김).
- **회복**: 기본 체력 먼저 → 넘친 양을 추가 체력에 `Priority` 낮은 순으로. 각 체력은 SO의 `HealSources`에 있는 출처의 회복만 받는다.
- **사망**: 기본 체력이 0이 되면 한 번 `IsDead = true`, `OnDead`. 이후 피해/회복/재생은 무시한다.
- 비면 제거(`RemoveWhenEmpty`), 지속시간(`Duration`)은 추가 체력에만 적용된다.
- 같은 SO를 다시 `AddHealth`하면 새로 만들지 않고 시작값까지 다시 채우고 지속시간을 리셋한다.
- 피해/회복/갱신 도중(예: `OnDead` 안)에 들어온 추가/제거는 처리가 끝난 뒤 반영된다.

`IHealthModule`의 `CurrentHealth` / `MaxHealth` / `OnHealthChanged`는 **기본 체력 기준**이다. `CurrentHealth` setter는 피해/회복 흐름을 거치지 않고 기본 체력을 바로 바꾼다(기존과 같음). 보호막 등을 거치려면 `TakeDamage` / `Heal`을 쓴다.

## 데이터만으로 만드는 체력 예시 (`Assets/Lrw/GameModule/Health`)

| 에셋 | 타입 | 설정 |
|---|---|---|
| Core Health SO | Default | MaxStat = MaxHealth, HealSources = All |
| Shield Health SO | Default | FixedMax 30, HealSources = None, RemoveWhenEmpty |
| Regen Shield Health SO | Regen | FixedMax 20, 3초 후 초당 10 재생 |
| Armor Health SO | DamageReduction | FixedMax 20, Rate 0.5(반감), RemoveWhenEmpty |
| Overheal Health SO | Default | FixedMax 30, StartRatio 0, HealSources = LifeSteal |

최대치는 `MaxStat`이 있으면 스탯 값(업그레이드로 늘릴 수 있음), 없으면 `FixedMax`를 쓴다. 그래서 어떤 종류든 기본 체력으로도, 추가 체력으로도 쓸 수 있다(예: Regen SO를 기본 체력에 넣으면 재생되는 기본 체력).

## 새 체력 종류 추가

1. `Healths/HealthClass/XxxHealth.cs` — `AbstractHealth`를 상속하고 필요한 훅만 오버라이드한다.
   - `ToLayerDamage` / `ToRawDamage` — 피해 변환
   - `CanHeal` — 회복 받을지(기본: `HealSources` 검사)
   - `OnTick`, `OnDamaged`, `OnHealed`, `OnOwnerDamaged`(다른 체력이 막아도 주인이 맞았을 때), `OnReapply`, `OnInit`, `OnDispose`
   - 값은 `SetValue()`로만 바꾼다(0~Max 클램프와 이벤트가 같이 처리됨).
2. `Healths/HealthSO/XxxHealthSo.cs` — `AbstractHealthSo`를 상속하고 파라미터와 `CreateHealth`를 구현, `[CreateAssetMenu(menuName = "Health/Xxx Health SO")]`.
3. `Create > Health > ...`로 에셋을 만들어 `NewHealthModule`의 `baseHealthSo` 또는 `healthSos`에 넣거나, 런타임에 `GetModule<IHealthLayerModule>().AddHealth(so)`.

## UI 연결 (BindSystem)

- 기본 체력만: `NewHealthModule.baseGaugeViewModel`에 `GaugeViewModel` 에셋 → 기존 `PlayerHealthUIBinder`가 그대로 동작
- 모든 체력: `Create > UI > Health List View Module`로 에셋을 만들고 `NewHealthModule.healthListViewModel`과 Binder 양쪽에 연결
  - `HealthLayerUIBinder` — 체력마다 `HealthLayerUI` 프리팹(바 Image, 아이콘, 텍스트)을 만들어 표시
  - `HealthGaugeUIBinder` — `targetHealth`로 고른 체력 하나만 표시, 없으면 숨김
- 코드에서 조회: `HealthList.Views`, `HealthList.TryGet(so, out view)`, `HealthList.Total`, `HealthList.GetTotal(HealthTag.Shield)`
- ViewModel 에셋은 데이터 소스(에이전트) 하나당 하나씩 만든다.

## 피해 연결

- `Agent.ApplyDamage` → `ApplyHealthDamage(damageData)` → `IHealthModule.TakeDamage`. `DamageData.LifeStealRatio`가 0보다 크면 실제로 깎인 체력 × 비율만큼 공격자(`Dealer`)를 `LifeSteal` 출처로 회복시킨다.
- 직접 쓸 때: `HealthModule.TakeDamage(HealthChangeData.Damage(10f))`, `HealthModule.Heal(HealthChangeData.Heal(10f, HealSourceType.Skill))`

### CTJ 담당 메모

`EnemyBase.ApplyDamage`는 `HealthModule.CurrentHealth -= amount;`로 기본 체력을 직접 깎아서 보호막/감소 체력/흡혈을 거치지 않는다. 이 줄을 아래처럼 바꾸면 적도 같은 체력 흐름을 쓴다(기존 `HealthModule`을 쓰는 적도 동작은 같다).

```csharp
ApplyHealthDamage(damageData);
```

## 디버그

`NewHealthModule` 컴포넌트 우클릭(플레이 모드) → `Debug/Take 10 Damage`, `Debug/Heal 10`, `Debug/LifeSteal Heal 10`, `Debug/Log Healths`
