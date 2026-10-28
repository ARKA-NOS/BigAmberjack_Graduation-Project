# NewHealthModule 전환 — 설정, 테스트, 학습 메모

> 2026-10-28 정리: 아래에 언급된 일회성 생성 도구·과거 검증 파일·캡처 일부는 삭제되었습니다. 설명과 검증 결과는 당시 작업 기록입니다. 현재 남긴 도구는 [README.md](README.md)를 확인하세요.

2026-10-28. 모든 수정은 Assets/CTJ 안에서 진행했습니다. Lrw/DevLib의 공용 코드, 공용 프리팹 및 SO는 수정하지 않았습니다.

## 변경 결과

- Catto, MadGhost, 01, 02, 04, RobotEyeBoss의 구형 체력 자식을 제거하고 `Assets/Lrw/GameModule/Module/NewHealthModule.prefab`을 중첩 프리팹으로 연결했습니다.
- 각 CTJ 인스턴스는 공용 Core Health SO를 사용합니다. `Other Healths`는 빈 배열, `Health View Model`은 비워 두어 기존 단일 HP 및 적 전용 임시 UI를 유지합니다. 공용 프리팹 원본의 실드/추가 체력 설정은 그대로입니다.
- EnemyBase의 피해 전달을 `NewHealthModule.ApplyDamage(amount)`로 전환했습니다. 회복은 공용 Agent.ApplyHeal에서 같은 NewHealthModule로 전달됩니다.
- 사망 감지, 보스 HP 조건/타깃 사망 확인, 피격 아웃라인, 임시 체력 바, 보스 테스트 HUD가 INewHealthModule을 사용합니다.
- 기존 이벤트의 `current <= 0` 조건으로 사망을 확정하는 방식을 유지했습니다. 새 모듈은 OnHealthChanged/OnDead를 호출한 뒤 IsDead를 설정하므로, 이벤트 안에서 IsDead만 읽으면 사망 확정이 늦어질 수 있습니다.
- CTJ_Player도 체력 컴포넌트와 설정만 전환했습니다. 아래 기존 Missing Script 문제 때문에 Unity 전체 저장 대신 해당 YAML 블록만 수정했습니다. 적 6종과 달리 이 플레이어의 새 모듈은 중첩 프리팹 연결이 아닌 개별 컴포넌트입니다.
- Tools~의 생성·검증 코드도 새 인터페이스와 피해/회복 함수로 바꿨습니다. 이전 체력 컴포넌트를 다시 생성하거나 읽기 전용 CurrentHealth에 대입하지 않도록 했습니다.

| 프리팹 | 기존·변경 후 최대 HP |
| --- | ---: |
| Catto_Melee | 40 |
| MadGhost_Ranged | 30 |
| Enemy01_TwistedArm | 50 |
| Enemy02_ProjectileRobot | 30 |
| Enemy04_MechanicArcher | 55 |
| RobotEyeBoss | 2500 |
| CTJ_Player | 100 |

HP 수치는 계속 `StatModule → Base Stats → MaxHealth` 항목에서 조정합니다. NewHealthModule은 Core Health SO가 지정한 MaxHealth 스탯을 해당 적의 StatModule에서 읽습니다. HP마다 새로운 Core Health SO를 만들 필요는 없습니다.

CTJ_MapTestScene의 01/02, EnemyArtTest의 Catto/MadGhost, BossPatternTest의 보스 인스턴스에 새 모듈이 반영된 것도 확인했습니다. 씬 파일을 저장하거나 변경하지 않았습니다.

## 남아 있는 공용/기존 문제

### 공용 Agent의 구형 Assert — 동료 수정 예정

`Assets/Lrw/Script/Agent/Agent.cs:25–26`은 아직 구형 IHealthModule을 조회하고 없으면 Assert를 발생시킵니다. 새 모듈로 바꾼 적에는 구형 모듈이 없으므로 다음 로그가 발생합니다.

```text
[Enemy01_TwistedArm(Clone)] IHealthModule is not found
```

사용자 답변에 따라 **동료가 공용 Assert를 수정**하기로 했습니다. CTJ에 구형 호환 모듈을 추가하거나 공용 초기화를 우회하지 않았습니다. 새 체력은 정상으로 초기화되며, 아래 통합 검사에서 이 알려진 Assert와 실제 오류를 구분해 기록했습니다. 동료가 수정한 뒤에는 해당 로그가 없어야 합니다.

### 기존 CTJ_Player와 과거 도구

- CTJ_Player의 `EffectModule`, `Player Melee Skilll`에 기존 Missing Script 2개가 있습니다. 체력 전환과 관계없는 부분이므로 삭제하거나 다른 기능으로 교체하지 않았습니다. 플레이어 전체 동작이 검증된 상태는 아닙니다.
- Tools~의 `ConfigureEnemyDeath.cs`, `VerifyEnemyDeath.cs`는 현재 프로젝트에 없는 예전 `Agents` 네임스페이스/`PlayerMeleeSkillModule`에 의존해 독립 컴파일에 실패합니다. 해당 의존은 이번 수정 전부터 있던 부분입니다. 체력 API만 전환했고 과거 플레이어 공격 테스트를 새 스킬 구조로 재작성하지 않았습니다.
- Tools~ 폴더는 Unity 자동 컴파일/빌드에 포함되지 않습니다. 위 두 과거 도구의 실패와 실제 게임 코드 컴파일 결과는 구분해야 합니다. 이번 체력 검사에는 새 `VerifyEnemyNewHealth`를 사용합니다.

## 직접 테스트하기

1. 동료가 공용 Agent의 구형 Assert를 정리한 뒤 Unity 컴파일 완료를 기다립니다. 정리 전에는 위의 알려진 Assert가 표시될 수 있습니다.
2. CTJ_MapTestScene을 열고 01 또는 02의 자식에서 `NewHealthModule`이 활성화되어 있는지 확인합니다. 기존 HealthModule이 함께 붙어 있으면 안 됩니다.
3. Play 후 플레이어로 적을 공격합니다. 또는 EnemyBase 컴포넌트 메뉴의 `Test/Take 2 Damage (Play Mode)`로 반복해서 2씩 피해를 줍니다. 피격 색·아웃라인·경직이 기존처럼 작동해야 합니다.
4. 임시 체력 바가 필요한 경우 Play 전에 `Assets/CTJ/Prototypes/Prefabs/EnemyTemporaryStatusUI.prefab`을 적 루트 자식으로 붙입니다. 체력은 비율대로 줄고 회복 시 늘어나야 합니다. UI가 필요하지 않으면 이 자식만 제거할 수 있습니다. 이번에 모든 적에게 UI를 다시 붙이지는 않았습니다.
5. HP를 0으로 만듭니다. 적이 죽고 공격/충돌이 정리되어야 하며, CurrencyManager의 Gold와 EnemyKillCountManager의 처치 수가 한 번만 증가해야 합니다. 보스의 기존 Gold 보상은 0입니다.
6. BossPatternTest에서는 2500에서 1001까지 피해를 주면 일반 패턴, 1000이 되면 기존 레이저 패턴으로 들어가야 합니다. 한 번에 치명타를 주면 레이저 패턴으로 살아남지 않고 죽어야 합니다.
7. 슈퍼아머가 있는 테스트 적은 준비·공격 중에도 HP가 줄어야 합니다. 경직/넉백 방지는 기존과 같고 후딜에는 해제됩니다.

체력을 코드로 확인하거나 변경할 때는 다음 경로를 사용합니다.

```csharp
using Lrw.Script.Agent.HealthSystem.NewHealthSystem;

INewHealthModule health = enemy.GetModule<INewHealthModule>();
float current = health.CurrentHealth;
float maximum = health.MaxHealth;
health.ApplyHeal(5f);
```

플레이어/적의 **일반 공격 피해**는 기존 IDamageable/EnemyBase.ApplyDamage로 전달해야 피격 반응과 보스 무적 규칙을 함께 거칩니다. `health.ApplyDamage(5f)`를 직접 호출하는 것은 체력 모듈 수준의 처리이므로 EnemyBase의 피격 연출·BossBase의 무적 검사를 우회합니다. 직접 모듈 피해로 0이 되어도 OnHealthChanged를 통해 사망/보상은 동작합니다.

## 검증 결과

- Unity 게임 코드 컴파일 완료, 컴파일 오류 없음.
- 적 6종의 스크립트/직렬화 참조, 공용 NewHealthModule 중첩 연결, Core Health 설정 및 기존 HP 보존 검사 통과. 적 프리팹에 Missing Script 없음.
- CTJ 씬 3개의 배치된 적 모두 새 모듈 사용. 씬 변경 없이 검사하고 원래 씬으로 복귀했습니다. 씬 3개 및 공용 Agent/새 모듈 프리팹의 SHA256이 작업 전후 동일합니다.
- 독립 Play 씬에서 6종의 초기 HP, 유효하지 않은 피해 무시, 피해/회복 이벤트, 최대 HP까지 회복, 임시 체력 바, 피격 점멸/아웃라인, 치명타 및 모듈 직접 피해에 의한 사망, 중복 보상 방지, 사망 후 회복 불가를 확인했습니다.
- 실제 CurrencyManager 복제본으로 Gold 지급, EnemyKillCountManager로 처치 수를 확인했습니다. 저장 함수는 호출하지 않았습니다.
- 보스 1001/1000 임계값, 기존 무적, 새 체력 모듈을 가진 타깃의 사망 감지, 치명타 우선 처리를 확인했습니다.
- 체력 통합 검사에서 공용 Agent의 알려진 Assert 10건, 그 외 오류 0건이었습니다. Assert를 숨기거나 Console을 비우지 않았습니다.
- 기존 슈퍼아머 Play 검사 및 적 6종의 아웃라인 생명주기 검사도 통과했습니다.
- 변경된 기존 Tools~ C# 파일 19개를 실행 없이 컴파일: 17개 통과, 위의 과거 도구 2개는 제거된 플레이어 타입 때문에 실패했습니다. 새 마이그레이션/통합 검증 도구는 컴파일·실행을 통과했습니다.

재검증은 기존 InvokeUnity.ps1의 run_script로 다음 순서대로 가능합니다.

1. MigrateEnemyNewHealth.Validate / ValidateScenes — 편집 모드, 에셋·씬 읽기 검사.
2. VerifyEnemy01.BeginPlay — 저장된 원본 씬을 보존하고 독립 임시 씬에서 Play.
3. VerifyEnemyNewHealth.RunPlay — 새 체력/보상/UI 통합 검사.
4. VerifyEnemySuperArmor.RunPlay, VerifyEnemyOutline.RunPlay — 회귀 검사.
5. VerifyEnemy01.StopPlay, Edit 복귀 후 VerifyEnemy01.RestoreScene.

MigrateEnemyNewHealth.Run은 기존 프리팹을 교체하는 일회성 도구입니다. 이미 전환된 파일은 다시 교체하지 않습니다. 일반적인 확인에는 Validate만 사용하면 됩니다.

## 사용 문법·API와 이유 — 학습용

- **기존 인터페이스 재사용**: `INewHealthModule`은 공용 시스템에 이미 있는 인터페이스입니다. 새로운 추상 계층을 만들지 않고 체력 조회·이벤트·피해/회복 API를 이 타입으로 통일했습니다.
- **읽기 전용 프로퍼티**: 새 `CurrentHealth`는 `get`만 제공합니다. 여러 체력 종류를 합산한 결과이므로 `CurrentHealth -= damage`처럼 외부에서 직접 바꾸지 않고 `ApplyDamage`를 호출해야 합니다. 새 모듈이 피해 순서와 이벤트를 관리하게 됩니다.
- **이벤트 구독/해제 (`+=`, `-=`)**: EnemyBase, 아웃라인, 임시 UI가 새 OnHealthChanged에 구독합니다. UI 비활성화나 객체 제거 시 해제해 불필요한 콜백을 남기지 않습니다. 이벤트의 인자 형태는 이전과 같아 current/delta/max를 그대로 사용할 수 있습니다.
- **제네릭 모듈 조회**: `GetModule<INewHealthModule>()`에서 꺾쇠 안은 찾을 타입입니다. 이름 문자열이나 특정 자식 위치에 의존하지 않고 기존 ModuleOwner가 수집한 모듈을 조회합니다.
- **중첩 프리팹과 오버라이드**: 공용 모듈 프리팹을 적의 자식으로 넣고 적에게 필요 없는 Other Healths만 빈 배열로 덮어썼습니다. PrefabUtility.RecordPrefabInstancePropertyModifications는 해당 인스턴스 설정을 기록하며 Lrw 원본을 저장하지 않습니다.
- **AfterInit 초기화 순서**: 새 모듈은 Initialize 이후 AfterInit에서 Core Health를 생성합니다. 모든 StatModule의 Initialize가 끝난 뒤 최대 HP를 읽으므로 실제 HP 확인은 ModuleOwner의 Awake 완료 이후에 수행했습니다. 임시 UI는 기존처럼 Start에서 바인딩합니다.

새 패키지, 런타임 어댑터, 호환용 체력 모듈은 추가하지 않았습니다.
