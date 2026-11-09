# 적 데미지 인디케이터

작업일: 2026-11-09

## 적용 결과

적의 실제 HP가 줄어든 타격마다 숫자가 크게 나타나고, 작아졌다가 다시 커지며 위로 올라가 사라진다. 일반 타격은 흰색, `DamageData.IsCritical == true`인 타격은 노란색과 더 강한 크기 변화로 표시한다.

크리티컬 확률이나 데미지 배율 계산은 추가하지 않았다. 공용 `DamageData`에 이미 있던 `IsCritical`을 읽는다. 같은 피해량 2를 넣으면 플래그가 달라도 HP는 똑같이 2만 줄어들며 연출만 달라진다.

Enemy01, Enemy02, Enemy04, Catto, MadGhost, RobotEyeBoss 프리팹에 `EnemyDamageIndicatorModule` 자식을 연결했다. 일반 Canvas와 TextMeshProUGUI를 사용하며, 기존 `NanumSquareRoundB SDF` 글꼴을 참조한다. 전용 테두리 머티리얼만 허용 영역 안에 만들었고 원본 글꼴·공용 UI·공용 데미지 구조는 수정하지 않았다.

## 구조와 제거 방법

1. `EnemyBase`가 `NewHealthModule.OnHealthChanged`에서 실제 HP 감소를 확인한다.
2. `EnemyDamageIndicatorModule`이 `DamageReceived` 이벤트를 받아 위치, 감소량, 크리티컬 여부를 ViewModel로 전달한다.
3. `EnemyDamageIndicatorBinder`가 표시 요청을 받아 `EnemyDamageIndicatorPopup`을 생성한다.
4. 숫자는 적의 부모에서 분리된 월드 Canvas로 재생되고, 자기 수명이 끝나면 제거된다. 적이 먼저 파괴되어도 마지막 숫자는 끝까지 남는다. 씬이 언로드되면 같은 씬의 숫자도 함께 정리된다.

게임 로직 모듈은 UI 컴포넌트나 텍스트 프리팹을 직접 참조하지 않는다. 기존 `AbstractUIViewModel<T>`와 `AbstractUIBinder<T>`를 사용한다.

이 ViewModel은 지속 상태가 아니라 **일회성 표시 요청**을 전달한다. 여러 적이 같은 에셋을 사용하지만 각 Binder는 `SourceId`가 자신의 모듈 인스턴스와 일치할 때만 처리한다. `OnDataChanged` 안에서 값을 즉시 복사하므로 한 프레임에 여러 번 Bind해도 마지막 숫자만 남거나 모든 적에서 중복 표시되는 문제가 없다. 다음 프레임의 `DataUpdate`에서 요청을 소비하도록 바꾸면 안 된다.

적의 `EnemyDamageIndicatorModule` 오브젝트를 끄거나 제거하면 표시 기능을 뺄 수 있다. 이미 생성된 숫자는 남은 연출을 완료한다. 게임 코드의 HP, AI, 피격 반응, 코인 보상은 이 모듈에 의존하지 않는다.

## 표시하는 데미지 수치

- `EnemyBase.DamageReceived`의 사본에 있는 `DamageAmount`는 방어와 잔여 HP를 반영한 실제 감소량이다. 원래 공격 데이터나 공용 체력 계산을 변경하지 않는다.
- 예: 공격 10, 방어 적용 후 HP 감소 5 → 숫자 `5`.
- 예: 남은 HP 3인 적에게 공격 100 → 마지막 숫자 `3`.
- 0·음수·NaN 피해, 회복, HP 감소가 없는 타격에서는 숫자가 생성되지 않는다. 보스가 무적 상태에서 타격을 거부하면 표시 요청도 나오지 않는다.
- 정수는 정수로, 소수는 최대 한 자리로 표시한다. 아주 작은 양의 피해는 `0`으로 보이지 않도록 최소 `0.1`로 표시한다.

## 인스펙터 조절 위치

`Assets/_Prefabs/Enemies/Effects/EnemyDamageIndicator.prefab`의 **EnemyDamageIndicatorPopup**:

| 항목 | 기본값 / 역할 |
| --- | --- |
| Normal Color | 흰색 |
| Duration | 일반 표시 수명 0.8초 |
| Normal Scale | 기본 크기의 1.45 → 0.8 → 1.12 → 1배 |
| Critical Color | 노란색 |
| Critical Size Multiplier | 기본 크기 1.25배 |
| Critical Scale | 1.8 → 0.65 → 1.4 → 1배. 위의 1.25배와 곱해짐 |
| Critical Duration Multiplier | 1.1배, 즉 0.88초 |
| Rise Distance | 위로 이동하는 거리 1.1유닛 |
| Critical Rise Multiplier | 크리티컬 상승 거리 1.2배 |
| Rise Start | 전체 수명의 22%부터 상승 |
| Fade Start | 전체 수명의 55%부터 투명해짐 |
| Horizontal Spread / Drift | 시작 위치 0.16유닛, 이동 중 좌우 흔들림 0.12유닛 이내 |

기본 글자 크기는 같은 프리팹의 `Damage` 자식 TextMeshProUGUI에서 `Font Size`를 조절한다. 월드 Canvas의 기본 스케일은 0.01이며, 적의 스케일·좌우 반전과 무관하게 표시된다.

`Assets/_Prefabs/Enemies/Effects/EnemyDamageIndicatorModule.prefab`:

- **EnemyDamageIndicatorModule / World Offset**: 피격 지점에서 위로 0.45유닛 보정.
- **Use Hit Point**: 끄면 적의 원점에서 표시. 켜도 원점에서 4유닛을 벗어난 잘못된 좌표는 원점으로 대체한다.
- **EnemyDamageIndicatorBinder / Max Visible Per Enemy**: 적 한 마리의 동시 표시 상한, 기본 16개. 초과하면 오래된 숫자를 먼저 정리한다.
- 모듈의 `View Model`과 Binder의 `Bind Data`는 같은 에셋이어야 한다. 현재 연결은 완료되어 있다.

## 직접 테스트하는 방법

1. 저장한 테스트 씬에 플레이어와 기존 적 프리팹을 배치하고 Play한다.
2. 적을 때린다. 흰색 숫자가 **크게 시작 → 축소 → 재확대 → 상승·페이드** 순서로 나오는지 확인한다. 적 여러 마리를 동시에 때리거나 연타해 각 타격이 따로 표시되는지도 본다.
3. 적을 죽인다. 마지막 타격 숫자가 적의 제거와 함께 끊기지 않고 자기 연출을 끝내는지 확인한다.
4. 실제 크리티컬 공격이 아직 없으므로, 실행 중 적의 `EnemyDamageIndicatorModule` 자식을 선택하고 컴포넌트 메뉴에서 `Preview/Critical 24 (Play Mode, No Damage)`를 실행한다. 노란색으로 더 크게 움직이며 **실제 HP는 바뀌지 않는다**. `Preview/Normal 24 (Play Mode, No Damage)`와 비교할 수 있다.
5. 해당 모듈을 껐다 켜고 다시 공격한다. 꺼진 동안에는 숫자가 안 나오며, 다시 켰을 때 중복 표시가 생기지 않는지 확인한다.

추후 공격 쪽에서 이미 계산한 피해를 전달할 때 `DamageData.IsCritical = true`를 지정하면 크리티컬 표시가 자동 선택된다. 별도로 표시만 요청하려면 다음 진입점을 사용할 수 있다. 이 호출을 일반 피격 처리에 중복으로 추가하지 않는다.

```csharp
indicatorModule.ShowDamage(24f, true, worldPosition);
```

## 사용한 문법과 이유

- `Module` 상속과 `Initialize(ModuleOwner owner)`: 기존 적 모듈 수집·초기화 규칙을 그대로 사용한다.
- C# 이벤트의 `+=` / `-=`: 실제 피격 결과만 구독하고, 비활성화·제거 때 해제한다. 재활성화 시 중복 구독되지 않도록 `_subscribed`로 관리한다.
- `readonly struct DamageIndicatorData`: 표시 요청을 값으로 복사한다. 여러 적의 연타가 다음 요청의 데이터로 덮이는 일을 막고, 생성 후 필드가 바뀌지 않게 한다.
- 제네릭 `AbstractUIViewModel<DamageIndicatorData>` / `AbstractUIBinder<DamageIndicatorData>`: 공용 UI 바인딩 방식을 재사용한다. 새로운 인터페이스나 추상 클래스는 만들지 않았다.
- `base.Awake()`: 공용 Binder가 ViewModel 이벤트를 구독하도록 유지한다. 종료 시에는 상속한 `OnDestroy`가 구독을 해제한다.
- 튜플과 구조체 복사: `EnemyBase`의 기존 피격 튜플을 복사해 표시 이벤트의 `DamageAmount`만 실제 HP 감소량으로 맞춘다. 크리티컬 여부·가해자·방향은 보존한다.
- `AnimationCurve` / `Keyframe`: 시간 비율에 따라 크기를 조절한다. 타이밍과 강도를 코드 수정 없이 인스펙터에서 바꿀 수 있다.
- `Mathf.InverseLerp` / `SmoothStep`: 상승·페이드가 지정한 시점부터 부드럽게 진행되도록 한다.
- `Time.time - _startedAt`: 생성되기 전의 프레임 시간까지 더해 첫 큰 자세를 건너뛰지 않도록, 실제 생성 시점부터 수명을 계산한다. 게임의 Time Scale을 따른다.
- `CultureInfo.InvariantCulture`와 `"0.#"`: 운영체제 언어에 따라 숫자의 소수점 표시가 달라지지 않게 한다.
- `SceneManager.MoveGameObjectToScene`: 적의 부모에 붙이지 않으면서 같은 씬에서 생성·정리되게 한다.

## 검증 결과

- Unity 6000.3.22f1에서 컴파일 오류·경고 0건.
- 적 6종에 모듈이 각각 1개 있으며 ViewModel, Binder, 팝업 프리팹, 글꼴 참조가 일치함.
- 실제 Enemy01 두 개체를 이용해 흰색 일반 표시와 더 큰 노란색 크리티컬 표시 확인. 같은 2 피해에서 실제 HP 감소는 동일하게 2.
- 일반 크기 배율 실측: 1.45 → 약 0.8002 → 약 1.1199. 위로 약 1.1유닛 이동, 페이드 후 제거 확인.
- 같은 프레임의 11·12·13 표시 요청이 각각 한 번씩 출력됨.
- 방어 100을 런타임 인스턴스에만 적용해 10 공격 → HP 5 감소 → 숫자 5 확인.
- 0·음수·NaN 공격과 회복은 표시하지 않음. 모듈 비활성화·재활성화 후 중복 구독 없음.
- 사망 타격 표시 확인. 적을 즉시 제거해도 숫자는 남았다가 자기 수명 뒤 제거되며 잔여 팝업 없음.
- 최종 글꼴로 일반·크리티컬의 4단계를 렌더링해 가독성 확인. `DamageIndicatorPreview.png`의 위쪽은 일반, 아래쪽은 크리티컬이며 왼쪽부터 시작·축소·재확대·상승과 페이드다.
- 검증 마지막 콘솔 로그 0건. 원래 `Proto_MainHome` 씬에 저장 없이 복귀했다.

재검증은 `Tools~/CTJ/VerifyDamageIndicators.cs`의 `CheckAssets`, `BeginPlay`, `RunPlay`, `CaptureStages`, `StopPlay`, `RestoreScene` 순서로 한다. Play 진입/종료가 완료된 뒤 다음 명령을 실행하며, 씬을 저장하지 않는 독립 검증이다. 예:

```powershell
unity cmd run_script --file 'Tools~/CTJ/VerifyDamageIndicators.cs' --entry VerifyDamageIndicators.CheckAssets --result-only
```

`SetupDamageIndicators.cs`는 최초 프리팹 구성 기록이다. `Create`는 기존 구성의 덮어쓰기를 막으므로 평소 조정에는 다시 실행할 필요가 없다.
