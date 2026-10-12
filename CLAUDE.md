# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트

**Unity 6000.3.22f1**, URP 2D, 새 Input System, Cinemachine 3로 만든 2D 액션 플랫포머(졸업 팀 프로젝트). 코드 주석과 설계 문서는 대부분 한국어로 작성한다.

CLI 빌드/테스트 파이프라인은 없다. 컴파일, 실행, 테스트는 Unity 에디터에서 한다. 루트의 `.sln`/`.csproj`는 Unity가 자동 생성하므로 수정하지 않는다. `com.unity.test-framework`는 설치되어 있지만 테스트 어셈블리는 없다. "테스트"는 씬 기반 테스터(`Assets/Lrw/Script/Test/`, `Assets/CTJ/Scripts/Testing/`, `Assets/SDW/Scripts/Testing/`)와 매니저의 `[ContextMenu]` 치트/디버그 메서드로 한다. `com.coplaydev.unity-mcp`(MCP for Unity) 패키지가 있어서 에디터 브리지로 재컴파일, 콘솔 로그 확인 등을 할 수 있다. `Assets/CTJ/Tools~/InvokeUnity.ps1`은 `Library/Pipeline/.unity-pipeline-port`를 통해 실행 중인 에디터에 명령(`recompile`, `get_console_logs`, `run_script` 등)을 보내는 헬퍼다. `Tools~` 폴더는 Unity가 임포트하지 않는다.

빌드 세팅의 씬: `Assets/Scenes/SampleScene.unity`, `Assets/Lrw/Scene/Test Home.unity`, `Assets/Lrw/Scene/Test Map.unity`.

## 폴더 담당

`Assets/` 최상위 폴더는 팀원별로 나뉜다. 작업은 자기 영역 안에서 하고, 그 작업이 목적이 아니면 공용 모듈/데이터는 수정하지 않는다.
- `Assets/DevLib/` — 공용 재사용 라이브러리. 각각 `.asmdef`가 있다(ModuleSystem, BattleSystem, EventChannelSystem, ObjectPool, PolyNavMesh, SoundSystem, AnimatorSystem).
- `Assets/Lrw/` — 코어 게임 프레임워크(`Script/_Core`), 에이전트 시스템(Stat, Health, Skill, Effect, Mask, Control), 재화, 업그레이드, 캐릭터 선택, UI.
- `Assets/Scripts/` — `Agents`(Agent 베이스, 플레이어 컨트롤러/FSM/모듈)와 `CoreSystem`(VFX, 이벤트, 코드 생성 템플릿).
- `Assets/CTJ/` — 적(`EnemyBase`, `MeleeEnemy`, `RangedEnemy`, 적 FSM). 프리팹/애니메이터 구성은 `Assets/CTJ/EnemyCombatGuide.md`와 `Assets/CTJ/Prototypes/README.md` 참고.
- `Assets/SDW/` — 절차적 던전 생성(`DungeonGenerator`, 랜덤 워크 레이아웃, 방 프리팹, 포탈, 방 적 스포너).
- `Assets/PBG/` — 타일맵과 맵 프리팹.
- `Assets/GameModules/`, `Assets/Lrw/GameModule/` — ScriptableObject 데이터 에셋.

게임플레이 코드 대부분은 `Assembly-CSharp`에 있다. DevLib, StatSystem, `_Debug`만 별도 어셈블리이므로 이 asmdef 안의 코드는 게임 코드를 참조할 수 없다.

## 아키텍처

**모듈 시스템(엔티티 구성)** — `DevLib.ModuleSystem.ModuleOwner`는 `Awake`에서 자식의 모든 `IModule`을 구체 타입을 키로 수집하고, 각각 `Initialize(owner)`를 호출한 뒤 `IAfterInitModule`을 구현한 모듈에 `AfterInit()`를 호출한다. 조회는 `GetModule<T>()` / `TryGetModule<T>()`(정확한 타입 우선, 없으면 할당 가능한 타입 검색 — 그래서 `IHealthModule`, `IRenderer`, `IControlDasher` 같은 인터페이스로도 찾을 수 있다). 한 오너에 같은 구체 타입 모듈은 하나만 둘 수 있다. `Agents.Agent : ModuleOwner, IDamageable`는 플레이어와 적의 베이스이며 `IHealthModule`이 필수다. 플레이어 모듈은 `AbstractPlayerModule`을 상속하며, 오너를 `PlayerController`로 캐스팅하고 `PlayerInputSo`를 노출한다.

**에이전트 FSM(ScriptableObject 기반)** — `Agents.FSM.StateMachine`은 `StateSo` 에셋 목록인 `StateListSo`로 만들어진다. 각 `StateSo.className`을 `Type.GetType`으로 찾아 `(Agent agent, int stateClipHash, int layerIndex)` 생성자로 리플렉션 생성한다. 상태 클래스 이름이나 네임스페이스를 바꾸면 이 문자열이 깨진다. `StateListSoEditor`의 "Generate" 버튼은 `assetIndex`를 매기고 `CoreSystem.CodeFormat`으로 대응하는 enum(예: `PlayerStateEnum`)을 생성한다. 네임스페이스는 폴더 경로에서 만들어진다(`Assets/Scripts/` 접두사 제거). 상태 순서를 바꾸면 enum을 다시 생성한다. 적은 별도의 제네릭 FSM(`Lrw/Script/_Core/_FSM`)을 사용한다.

**전역 매니저 + 서비스 로케이터** — `GameManager`(`DontDestroyOnLoad`, 실행 순서 -20)는 `Awake`에서 자식 `AbstractManager`들의 `Initialize()`를, 파괴 시 `GameEnd()`를 호출한다. 매니저(`CurrencyManager`, `UpgradeManager`, `CharacterManager`, `UIManager`)는 자기 인터페이스로 정적 `ServiceLocator`에 등록하고, `GameEnd`에서 상태를 저장한 뒤 Null 객체 구현(`NullCurrencyManager` 등)을 다시 등록한다. 사용하는 쪽은 `ServiceLocator.Get<IXxxManager>()`를 호출한다. 매니저를 추가할 때는 인터페이스 + Null 객체 패턴을 따른다.

**이벤트** — 두 가지 방식이 공존한다.
- `Lrw/Script/_Core/_EventSystem`의 `EventBus<T> where T : IEvent`(정적, 타입별, 선택적 키 범위). 예: `UIManager`가 `UIOpenCloseEvent`를 구독.
- DevLib의 `EventChannelSO`(`GameEvent` 하위 클래스를 전달하는 ScriptableObject 채널). 사운드/VFX 생성 이벤트 등에 사용.

**저장** — `SaveSystem`은 `JsonUtility`로 JSON을 `Application.persistentDataPath`에 `{TypeName}_{name}.save`로 저장한다(임시 파일 교체 방식). `JsonUtility`는 Dictionary를 직렬화하지 못하므로 `JsonDict<K,V>`로 감싼다. 저장은 매니저의 `GameEnd()`에서 한다. 에디터 메뉴 `Tools/Open Save Folder`로 저장 폴더를 연다.

**입력** — `Assets/Settings/Controls.cs`는 Input Actions 에셋에서 자동 생성된다. `PlayerInputSo`가 `Controls.IPlayerActions`를 구현하고 C# 이벤트를 노출하며, 모듈이 이를 구독한다.

## 컨벤션

- Lrw/Agents 코드에서는 `Debug.Log` 대신 `FDebug.Log/LogWarning/LogError`(에디터 전용 로그)와 `FDebug.Assert`(예외 발생)를 쓴다.
- 네임스페이스는 폴더 경로를 따른다(`Lrw.Script.Upgrade`, `Agents.Players.FSM`, `DevLib.ModuleSystem`).
- private 필드는 `_camelCase`. 직렬화 자동 프로퍼티는 `[field: SerializeField] public T X { get; private set; }`.
- 커밋 메시지는 `feat : ...` 형식 접두사를 쓴다.
-  PR 대상은 `main`이나 `main`에 직접 커밋하지 않는다.
- 팀원별 개인 설정(작업 브랜치, 담당 폴더 등)은 이 파일이 아니라 git에 올라가지 않는 `CLAUDE.local.md`에 적는다.
