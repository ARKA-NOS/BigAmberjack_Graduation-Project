# 이번 작업의 문법과 이유

## 1. 기존 모듈 상속과 초기화 순서

`EnemyCurrencyRewardModule : Module, IAfterInitModule`은 기존 Module 클래스를 상속하고, 공용 IAfterInitModule 인터페이스를 구현한다. 새 인터페이스를 만든 것이 아니다. `Initialize(ModuleOwner owner)`에서 소유 적을 기억하고, **모든 모듈의 Initialize가 끝난 뒤** 호출되는 `AfterInit()`에서 Stat을 가져온다. StatModule보다 먼저 기본값을 만들어 적별 설정을 놓치는 문제를 피한다.

`owner as EnemyBase`는 해당 타입으로 변환할 수 없으면 예외 대신 null을 반환한다. 잘못 붙인 모듈은 이후 조건문에서 지급하지 않는다.

## 2. ScriptableObject는 스탯 정의, StatGroup은 적별 값

새 클래스를 만들지 않고 기존 `StatData` 타입으로 `EnemyGoldReward.asset`을 만들었다. 같은 스탯 키를 공유해도 Catto의 StatGroup에는 10, MadGhost에는 15를 넣을 수 있다. 런타임에는 각 StatModule의 `Stat`을 읽으므로 다른 적의 값이나 SO 원본을 변경하지 않는다.

`[SerializeField] private`는 Inspector에 설정을 노출하면서 외부 코드의 무분별한 대입은 제한한다. `[Tooltip]`은 연결해야 할 에셋과 소수 처리 규칙을 Inspector에 설명한다.

## 3. 제네릭 모듈 조회와 out 변수

`_owner.TryGetModule(out IStatModule stats)`는 `IStatModule`을 구현한 기존 모듈을 찾아 `stats`에 넣는다. `if` 안에서 찾기에 성공했을 때만 `stats.GetStat(rewardAmountStat)`을 호출한다. GameObject가 어디에 배치되었는지보다 ModuleOwner가 수집한 역할을 기준으로 접근한다.

검증 도구의 `GetComponentInChildren<T>()`는 같은 오브젝트만 찾는 `GetComponent<T>()`와 달리 자식까지 찾는다. 실제 적의 모듈 간 연결은 기존 `GetModule<T>()`를 유지했다.

## 4. 재진입과 중복 지급 방지

`_rewardHandled`를 재화 `Add()`보다 먼저 true로 만든다. Add는 `OnCurrencyChanged` 이벤트를 즉시 호출하므로 그 콜백이 다시 사망 보상을 요청할 가능성까지 막기 위해서다. EnemyBase의 IsDead 검사와 모듈의 지급 검사로 한 사망에 한 번만 처리한다. 실패했을 때 뒤늦게 자동 재시도하는 기능은 없다.

`internal` 메서드는 같은 어셈블리 안에서만 호출할 수 있다. 이 프로젝트의 CTJ와 Lrw Currency 코드는 현재 Assembly-CSharp에 있으며, 별도 asmdef는 추가하지 않았다. `if (_currencyReward != null)`은 선택 모듈이 없거나 Unity에서 삭제된 경우 호출하지 않는다.

## 5. float 스탯을 int 재화로 바꾸기

스탯 값은 float이고 재화 수량은 int라서 `Mathf.FloorToInt()`로 소수 부분을 버린다. 23.9는 23, 1 미만은 0 지급이다. `float.IsNaN`/`IsInfinity`는 계산 결과가 정상 숫자가 아닌 경우를 검사한다.

`(double)value > int.MaxValue`는 float로 표현할 때 int 최댓값이 반올림되는 경계를 정확히 검사하기 위한 형 변환이다. `(long)currency.Count + amount`는 두 int를 더하기 전에 더 큰 정수형으로 계산해, 오버플로로 잔액이 음수가 되는지 안전하게 확인한다. 공용 CurrencyData 자체는 수정하지 않았다.

## 6. RequireComponent와 계층 분리

`[RequireComponent]`는 **자식이 아니라 같은 GameObject**에 컴포넌트를 요구한다. 자식에 이동/스킬 모듈이 있어도 루트에 다시 만들어 중복될 수 있어 EnemyBase의 이동 요구와 MeleeEnemy/RangedEnemy의 스킬 요구를 제거했다. Rigidbody2D는 같은 루트에 있어야 하므로 요구를 유지했다. Animator와 이벤트 Relay도 같은 Visual 오브젝트를 유지한다.

컴포넌트의 프리팹 fileID를 유지한 채 소유 GameObject와 부모/자식 목록만 변경했다. 따라서 공격 스킬 목록, Collider, Animator, 피격 SpriteRenderer 등 기존 직렬화 참조를 재활용한다. 추가로 두 프리팹의 실제 Unity 참조를 검사한다.
