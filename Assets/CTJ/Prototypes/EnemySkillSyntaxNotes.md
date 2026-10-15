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
- Animation Event는 C# 문법이 아니라 클립의 지정 시점에 메서드를 호출하는 Unity 기능이다. OnAttackHit은 그 순간 겹침/피해만 담당한다. IsUsing/HasReleased 검사로 준비 이전의 이벤트가 피해를 만들지 않게 했다. 10월 14일의 OnAttackWarning 점멸은 아래 10월 15일 변경에서 공통 타이머로 대체했다.

## 점멸 후 유지·준비 자세·빠른 타격에서 사용한 문법 (2026-10-15)

- `private enum PreparationPhase { Waiting, Blinking, Holding }`: 동시에 여러 bool이 켜지는 조합 대신 준비 중 한 단계를 이름으로 표현한다. 적 AI용 FSM을 새로 만든 것이 아니라 기존 공격 스킬 내부의 세 단계다. `switch`와 `case`는 현재 단계에 해당하는 처리만 실행하고, `break`는 다음 case로 넘어가지 않도록 끝낸다.
- `_phaseEndTime = Time.time + _holdDuration`: 점멸이 실제 끝난 프레임부터 0.15초를 센다. 모든 시각을 공격 시작 때 절대 시각으로만 계산하면 프레임이 늦을 때 유지 구간이 건너뛰어질 수 있어서 단계별 종료 시각을 썼다. 설정값은 공격 시작에 복사하므로 도중 스탯 변경은 다음 공격부터 반영한다. Time.time 기반이라 게임 일시정지와 함께 멈춘다.
- `[SerializeField, Min(0f)] private float postBlinkDelay`: Inspector에서 적마다 유지 시간을 바꾸되 다른 코드가 필드를 직접 덮어쓰지 않도록 private으로 둔다. `public float PostBlinkDelay => ...`는 검사/읽기용 프로퍼티이며 외부 setter는 없다.
- `EndBlink()`는 `IsBlinking=false`와 Image 색 복구를 한 번에 처리한다. 다음 UI Update를 기다리지 않고 원래 알파를 돌려주어 마지막 점멸이 꺼진 프레임이어도 유지 구간이 투명하게 남지 않는다. 피해 판정은 UI가 아닌 기존 스킬에서만 한다.
- `string.IsNullOrEmpty(preparationStateName)`는 준비 상태를 선택 사항으로 취급하는 검사다. 연결한 경우 `Animator.HasState`로 유효성을 확인한 뒤 `Animator.Play(hash, 0, 0f)`로 해당 상태를 처음부터 재생한다. 문자열을 Animator 식별자로 바꾸는 `StringToHash`는 기존 공격 재생 방식과 같다.
- 준비/타격 클립 분리는 C# 상속을 추가한 것이 아니라 Animator 상태와 AnimationClip 데이터 구성이다. 준비 클립은 Loop와 자동 전환 없이 마지막 스프라이트를 유지한다. 실제 준비 길이는 스탯+경고 타이머가 결정하므로 클립 대기와 스탯 대기가 이중으로 누적되지 않는다.
- 스프라이트 키의 **시간 간격**을 0.05초/0.2초처럼 다르게 둔다. Sample Rate=60은 편집 시간 격자이며 게임을 60FPS로 강제하지 않는다. 공격 클립의 0.05초 OnAttackHit과 0.4초 OnAttackFinished가 피해 및 행동 잠금 해제를 결정한다. Animator.speed를 전역 변경하지 않아 타격/회수까지 같이 느려지는 문제를 피한다.
- `OnEndSkill`/`OnStopSkill`에서 `base`를 먼저 호출해 UI·타깃 상태를 정리한 다음 Idle로 복귀한다. `Enemy.IsDead`일 때는 Idle을 재생하지 않아 사망 애니메이션을 덮어쓰지 않는다. 별도 인터페이스/추상 클래스/패키지는 추가하지 않았다.
- 검증 코드의 `Time.captureDeltaTime`은 30/60 Hz 시뮬레이션 간격으로 애니메이션·타이머를 검사하기 위해 잠깐 사용한다. `try/finally`에서 원래 값을 복원한다. 게임 코드나 프로젝트 FPS 설정에는 적용하지 않는다.

## 무점멸·알파 배율·트리거 몸통에서 사용한 문법 (2026-10-15 추가 피드백)

- `blinkBeforeAttack && Telegraph != null ? Telegraph.BlinkDuration : 0f`: `&&`로 두 조건을 함께 확인하고 `조건 ? 참일 때 값 : 거짓일 때 값`인 삼항 연산자로 이번 공격의 점멸 시간을 결정한다. Catto는 0초여서 Blinking 단계를 건너뛰지만 준비/마지막 유지/후딜은 남는다.
- `Show(float opacity = 1f)`의 `= 1f`는 선택적 매개변수의 기본값이다. 기존 Show() 호출도 원래 알파로 작동한다. `Mathf.Clamp01(opacity)`로 0~1 사이로 제한한 뒤 `_displayColor.a *= ...`로 알파만 곱한다. Color는 값 형식이므로 `_displayColor = areaColor`는 복사이며 공용 프리팹의 색을 수정하지 않는다. `[Range(0f, 1f)]`는 Inspector 슬라이더를 제공한다.
- 루트 `isTrigger`와 자식의 Layer Override는 C# 문법이 아니라 Unity 물리 설정이다. Trigger 몸통은 피격 검색용, 비 Trigger TerrainBody는 지형 물리 반응용으로 역할을 나눴다. Collider별 Include/Exclude를 사용해서 ProjectSettings/다른 팀 설정을 바꾸지 않았다. [Unity Collider2D.excludeLayers 문서](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Collider2D-excludeLayers.html).
- Ground가 6번이므로 `1 << 6`은 그 레이어 비트(64), `~(1 << 6)`은 그 비트만 끈 나머지 모든 레이어다. 저장된 Exclude Layers 값 4294967231은 이를 부호 없는 32비트 값으로 표현한 것이다. 반대쪽 Collider 설정과 충돌할 때의 우선순위는 Layer Override Priority=1로 뒀다. [Unity 우선순위 문서](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Collider2D-layerOverridePriority.html).
- `GetComponentsInChildren<Collider2D>()`는 루트와 자식에서 콜라이더를 찾는다. Body Collider 참조가 빠진 경우에만 호출하며 `candidate.isTrigger`를 제외하고 `candidate.attachedRigidbody == _body`를 확인해 같은 물리 몸체의 지형 콜라이더만 선택한다. 새 모듈/인터페이스는 만들지 않았다.
- Chase의 `StateFixedUpdate`에서 공격 가능 여부를 먼저 확인하고 `return`하면 그 물리 틱의 MoveTowardsTarget 호출을 건너뛴다. Update 사이에 물리 틱이 여러 번 와도 사거리 안에서 계속 전진하지 않게 한다. 기존 Attack.Enter/FixedUpdate가 수평 속도를 0으로 유지하며 쿨타임은 기존 SkillModule 규칙을 따른다.

## 근접 범위 표시 제거 (2026-10-16)

런타임 C# 문법이나 시스템은 추가하지 않았다. Catto 프리팹의 `Telegraph Prefab` 참조만 None(`{fileID: 0}`)으로 비웠다. 기존 `telegraphPrefab != null` 조건이 Canvas 생성을 건너뛰고, `Telegraph?.EndBlink()`/`?.Hide()`는 참조가 없으면 호출하지 않는다. 준비·타격·후딜 타이머는 UI와 별도로 실행되므로 그대로 유지된다. 알파를 0으로 해서 숨긴 것이 아니라 UI 인스턴스를 생성하지 않는 방식이다. 검증에서는 `GetComponentsInChildren<AttackTelegraphUI>(true)`와 Canvas 배열 길이가 0인지 검사해 비활성 UI까지 남지 않았는지 확인한다.
