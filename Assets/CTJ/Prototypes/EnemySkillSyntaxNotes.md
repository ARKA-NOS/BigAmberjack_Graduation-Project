# 이번 변경의 문법·설계 메모

Unity 개발 1년 반 정도에서 기존 MonoBehaviour 경험과 연결해서 볼 수 있도록 정리했다. 실제 코드는 Scripts/Enemies/EnemySkillModule.cs 및 Skills 폴더에 있다.

## 상속과 override, base

`EnemySkillModule : SkillModule`은 기존의 등록/실행 기능을 물려받는다는 뜻이다. 스킬을 실행할 때 공용 UseSkill을 호출하므로 적만의 쿨타임 계산기를 따로 유지하지 않는다.

`EnemyAttackSkill : AbstractCoolTimeSkill`은 두 공격이 함께 쓰는 적 소유자, 타깃 고정, 종료 정리를 추가한다. 이 추상 클래스 자체는 Add Component로 붙이지 않고 실제 동작이 있는 EnemyMeleeSkill/EnemyProjectileSkill을 붙인다.

`protected override void OnUseSkill()`은 부모가 정해둔 시작 단계에 적의 동작을 채우는 코드다. `base.OnUseSkill()`을 생략하면 부모의 쿨타임 시작 시각/타깃 설정이 실행되지 않는다. 이번 공유 구조에서는 반드시 호출한다.

현재 Agent.ApplyDamage가 virtual이므로 EnemyBase도 `override`를 사용한다. 이전 `new virtual`은 같은 이름을 숨길 뿐이어서 Agent 타입으로 호출할 때 적의 사망/방어 조건이 우회될 수 있었다. override면 실제 객체가 EnemyBase인 경우 그 구현으로 연결된다.

## [Serializable], [SerializeField], List

SkillEntry는 GameObject에 붙는 컴포넌트가 아니라, 목록 한 칸의 설정을 묶는 일반 클래스다. `[Serializable]`을 붙여 Unity가 각 필드를 저장할 수 있게 했다.

`[SerializeField] private List<SkillEntry> skills`는 외부 코드의 자유로운 교체를 막으면서 Inspector에서 여러 칸을 편집하게 한다. 목록의 순서 자체가 우선순위다. 별도의 우선순위 숫자를 만들지 않아 중복 순위 규칙도 필요 없다.

공용 부모의 `[field: SerializeField]`는 자동 프로퍼티의 숨은 저장 필드에 Unity 직렬화를 적용한다. YAML/SerializedObject에서 `<SkillSo>k__BackingField` 같은 이름이 보이는 이유다. 일반 게임 코드에서는 SkillSo 프로퍼티를 사용하면 된다.

## IReadOnlyList와 HashSet

`IReadOnlyList<SkillEntry>`로 목록을 읽게 하면 외부 코드에 Add/Remove API를 직접 공개하지 않는다. Inspector의 실제 저장소는 List다.

`HashSet<AbstractSkill>`은 중복 없는 등록 스킬 집합이다. 목록에 실수로 다른 적의 스킬을 연결했을 때 실행하지 않도록 Contains로 검사한다. 다른 HashSet<SkillSO>는 공용 모듈과 동일하게 같은 키의 첫 스킬만 인정한다. 매 프레임 GetComponents를 반복하지 않고 Initialize에서 한 번 준비한다.

## 패턴 매칭과 as

`owner as EnemyBase`는 부모 타입인 ModuleOwner를 적 타입으로 확인한다. 적이 아니면 예외 대신 null이 된다.

`skill is IChargeSkill { IsCharging: true }`는 “차징 인터페이스를 구현했고, 현재 차징 중인가?”를 한 번에 검사한다. 클래스 이름마다 if를 추가하지 않고 공용 인터페이스의 능력으로 판정한다.

`ActiveSkill is EnemyAttackSkill skill`은 현재 스킬이 CTJ의 이벤트 수신형 공격일 때만 타격 이벤트를 보낸다. unrelated 공용 스킬에 근접 타격 이벤트가 전달되지 않는다. 새 인터페이스는 만들지 않았다.

## ?.와 =>

`_skills?.StopAttack()`은 스킬 모듈이 있을 때만 종료하라는 null 조건부 호출이다. 스킬 모듈 없는 구형 EnemyPrototype과 초기화/파괴 시점을 같이 처리한다.

`public bool IsBusy => ...`는 짧은 읽기 전용 프로퍼티 표기다. 값이 저장되는 별도 bool이 아니라 읽을 때 현재 스킬의 IsUsing/IsCharging으로 계산한다. 종료된 스킬을 계속 바쁜 상태로 기억하는 문제를 줄인다.

## Time.time, 종료 시각, 상태와 데이터 분리

공용 쿨타임은 `Max(0, 시작 시각 + 지속 시간 - Time.time)`으로 남은 초를 구한다. 초기 시작 시각은 NegativeInfinity라 첫 사용은 바로 가능하다. SO는 여러 적이 공유하므로 시작 시각을 SO에 저장하면 안 된다.

근접 이벤트 누락 복구와 원거리 후딜은 종료 시각을 기록하고 UpdateSkill에서 확인한다. 별도 코루틴 없이 StopSkill/EndSkill의 IsUsing으로 진행을 끊을 수 있다. Time.time 기반이라 일시정지(Time.timeScale=0)하면 쿨타임과 후딜도 같이 멈춘다.

## 검증 코드의 async/await와 try/finally

Tools~의 async Task는 Unity 에디터 메인 루프가 계속 움직이는 동안 프레임/애니메이션 결과를 기다리기 위해 썼다. 게임 실행 코드는 async로 바꾸지 않았다. Task.WhenAll은 서로 멀리 떨어진 테스트 공간들을 함께 관찰한다.

try/finally는 검사 중 예외가 발생해도 테스트용 GameObject를 제거하기 위한 문법이다. 에디터의 실제 씬/프리팹 저장은 호출하지 않는다. `SerializedObject`로 런타임 복제본만 구성하여 private 직렬화 필드를 테스트한다.

## 추적 해제·HP·Canvas 예고 추가에서 사용한 문법 (2026-10-14)

- `[SerializeField, Min(0f)] private float chaseReleaseRange`: Inspector에 저장할 값이면서 음수 입력을 제한한다. 속성만으로 런타임 값까지 보장되지는 않으므로 `Mathf.Max(detectionRange, chaseReleaseRange)`로 읽을 때도 보정한다. 두 거리를 나눠 감지 경계에서 추적/대기가 반복되는 현상을 막는다.
- `StatData`는 공용 스탯의 키인 ScriptableObject다. `owner.TryGetModule(out IStatModule stats)`의 `out`은 성공 시 찾은 모듈을 지역 변수에 담는다. 새 인터페이스 없이 기존 IStatModule을 사용했다.
- `_attackDelay?.Value ?? fallbackAttackDelay`: 스탯이 있으면 현재 값, 없으면 호환용 기본값을 사용한다. `?.`는 null 조건부 접근, `??`는 왼쪽이 null일 때 오른쪽을 선택하는 연산자다. 준비 시간은 시작 시 `Time.time + 값`으로 고정하여 같은 공격 도중 스탯 변경으로 시점이 튀지 않게 했다.
- 기존 EnemyAttackSkill의 `virtual`/`override` 훅을 사용한다. PrepareAttack은 방향 고정, UpdateTelegraph는 표시 크기, OnReleaseAttack은 대기 후 실제 동작을 담당한다. 두 스킬에서 이미 필요한 단계만 추가했으며 새로운 추상 클래스/인터페이스는 만들지 않았다. 상속 스킬의 UpdateSkill에서 `base.UpdateSkill()`을 호출해야 공통 준비/취소 처리가 실행된다.
- `new Color32(255, 110, 110, 50)`은 0~255 바이트 색상이다. Color로 변환되면 알파가 50/255가 된다. `% 2`로 점멸 구간의 짝수/홀수를 나눠 알파 0과 원래 알파를 교대한다. Time.time을 사용하므로 게임 일시정지 때 준비와 점멸도 함께 멈춘다.
- `RectTransform`, `Canvas.renderMode = RenderMode.WorldSpace`, `Image`는 기본 uGUI 구성이다. 화면 픽셀이 아닌 월드 위치에 놓아 실제 Collider와 맞춘다. raycastTarget=false라 입력을 가리지 않으며, UI 자체는 피해를 주지 않는다.
- `attackHitbox is BoxCollider2D box`는 타입 검사와 변수 선언을 함께 하는 패턴 매칭이다. Box일 때 실제 size/offset을 사용하고, `TransformPoint`로 로컬 offset을 월드 좌표로 바꾼다. `lossyScale`로 몸체 크기를 계산한 뒤 Canvas 부모 스케일을 역수로 보정해 2배 프리팹에서 4배로 표시되는 것을 막는다.
- `Mathf.Atan2(y, x) * Mathf.Rad2Deg`는 발사 방향을 Z 회전 각도로 바꾼다. `Normalize()`로 고정한 방향을 표시와 실제 발사에 같이 사용하므로 예고 뒤 회피가 가능하다.
- Animation Event는 C# 문법이 아니라 클립의 지정 시점에 메서드를 호출하는 Unity 기능이다. OnAttackWarning은 점멸만, OnAttackHit은 그 순간 겹침/피해만 담당한다. IsUsing/HasReleased 검사로 준비 이전의 이벤트가 피해를 만들지 않게 했다.
