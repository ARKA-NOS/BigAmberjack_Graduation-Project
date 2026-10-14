# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트

**Unity 6000.3.22f1**, URP 2D, 새 Input System, Cinemachine 3로 만든 2D 액션 플랫포머(졸업 팀 프로젝트). 코드 주석과 설계 문서는 대부분 한국어로 작성한다.

루트의 `.sln`/`.csproj`는 Unity가 자동 생성하므로 수정하지 않는다. `com.unity.test-framework`는 설치되어 있지만 테스트 어셈블리는 없다. "테스트"는 각 팀원 폴더의 씬 기반 테스터와 매니저의 `[ContextMenu]` 치트/디버그 메서드로 한다.

**Unity CLI(`unity`)** — 공식 Unity CLI로 에디터 밖에서 프로젝트를 다룬다. 프로젝트에 `com.unity.pipeline` 패키지가 있어서, 에디터가 이 프로젝트를 열고 있으면 CLI로 실행 중인 에디터에 명령을 보낼 수 있다.
- `unity status` — 연결된 에디터 확인(포트, 프로젝트, 상태)
- `unity list` / `unity cmd` — 실행 중인 에디터에서 쓸 수 있는 명령 목록
- `unity cmd <명령> [인자]` — 에디터에 명령 실행(재컴파일, 콘솔 로그 확인 등)
- `unity open` — 맞는 에디터 버전으로 프로젝트 열기
- `unity build` / `unity test` — 배치 모드 빌드 / EditMode·PlayMode 테스트 실행. 에디터가 이 프로젝트를 열고 있으면 배치 모드는 실행되지 않는다.

`com.coplaydev.unity-mcp`(MCP for Unity) 패키지도 있어서 MCP 브리지로도 재컴파일, 콘솔 로그 확인 등을 할 수 있다.

빌드 세팅의 씬: `Assets/Scenes/SampleScene.unity`, `Assets/Lrw/Scene/Test Home.unity`, `Assets/Lrw/Scene/Test Map.unity`.

## 폴더 담당

`Assets/` 최상위 폴더는 팀원별로 나뉜다. 작업은 자기 영역 안에서 하고, 그 작업이 목적이 아니면 공용 모듈/데이터나 다른 팀원의 폴더는 수정하지 않는다.
- `Assets/DevLib/` — 공용 재사용 라이브러리. 각각 `.asmdef`가 있다(ModuleSystem, BattleSystem, EventChannelSystem, ObjectPool, PolyNavMesh, SoundSystem, AnimatorSystem).
- `Assets/Lrw/` — 코어 게임 프레임워크(`Script/_Core`), 에이전트 시스템, 재화, 업그레이드, 캐릭터 선택, UI.
- `Assets/Scripts/` — `Agents`(Agent 베이스, 플레이어)와 `CoreSystem`(VFX, 이벤트, 코드 생성 템플릿).
- `Assets/CTJ/` — 적.
- `Assets/SDW/` — 절차적 던전 생성.
- `Assets/PBG/` — 타일맵과 맵 프리팹.
- `Assets/GameModules/`, `Assets/Lrw/GameModule/` — ScriptableObject 데이터 에셋.

게임플레이 코드 대부분은 `Assembly-CSharp`에 있다. DevLib, StatSystem, `_Debug`만 별도 어셈블리이므로 이 asmdef 안의 코드는 게임 코드를 참조할 수 없다.

## 공용 아키텍처

**모듈 시스템(엔티티 구성)** — `DevLib.ModuleSystem.ModuleOwner`는 `Awake`에서 자식의 모든 `IModule`을 구체 타입을 키로 수집하고, 각각 `Initialize(owner)`를 호출한 뒤 `IAfterInitModule`을 구현한 모듈에 `AfterInit()`를 호출한다. 조회는 `GetModule<T>()` / `TryGetModule<T>()`(정확한 타입 우선, 없으면 할당 가능한 타입 검색 — 그래서 인터페이스로도 찾을 수 있다). 한 오너에 같은 구체 타입 모듈은 하나만 둘 수 있다. `Agents.Agent : ModuleOwner, IDamageable`는 플레이어와 적의 베이스이며 `IHealthModule`이 필수다.

**전역 매니저 + 서비스 로케이터** — `GameManager`(`DontDestroyOnLoad`, 실행 순서 -20)는 `Awake`에서 자식 `AbstractManager`들의 `Initialize()`를, 파괴 시 `GameEnd()`를 호출한다. 매니저는 자기 인터페이스로 정적 `ServiceLocator`에 등록하고, `GameEnd`에서 상태를 저장한 뒤 Null 객체 구현을 다시 등록한다. 사용하는 쪽은 `ServiceLocator.Get<IXxxManager>()`를 호출한다. 매니저를 추가할 때는 인터페이스 + Null 객체 패턴을 따른다.

**이벤트** — 두 가지 방식이 공존한다.
- `Lrw/Script/_Core/_EventSystem`의 `EventBus<T> where T : IEvent`(정적, 타입별, 선택적 키 범위).
- DevLib의 `EventChannelSO`(`GameEvent` 하위 클래스를 전달하는 ScriptableObject 채널). 사운드/VFX 생성 이벤트 등에 사용.

**저장** — `SaveSystem`은 `JsonUtility`로 JSON을 `Application.persistentDataPath`에 `{TypeName}_{name}.save`로 저장한다(임시 파일 교체 방식). `JsonUtility`는 Dictionary를 직렬화하지 못하므로 `JsonDict<K,V>`로 감싼다. 저장은 매니저의 `GameEnd()`에서 한다. 에디터 메뉴 `Tools/Open Save Folder`로 저장 폴더를 연다.

**입력** — `Assets/Settings/Controls.cs`는 Input Actions 에셋에서 자동 생성된다. `PlayerInputSo`가 `Controls.IPlayerActions`를 구현하고 C# 이벤트를 노출한다.

## 컨벤션

- 코드는 SOLID 원칙을 지켜서 작성한다.
  - **단일 책임(SRP)** — 클래스/모듈은 한 가지 역할만 맡는다. 기능이 늘면 기존 클래스를 키우지 말고 모듈로 나눈다.
  - **개방-폐쇄(OCP)** — 새 동작은 기존 코드를 고치지 않고 확장(새 모듈, 새 상태, 새 ScriptableObject 데이터)으로 추가한다.
  - **리스코프 치환(LSP)** — 하위 클래스는 베이스(`Agent`, `AbstractManager` 등)의 계약을 깨지 않는다. 베이스가 보장하는 동작을 무효화하는 오버라이드를 만들지 않는다.
  - **인터페이스 분리(ISP)** — 큰 인터페이스 하나보다 역할별 작은 인터페이스(`IHealthModule`, `IDamageable` 등)를 둔다.
  - **의존성 역전(DIP)** — 구체 클래스 대신 인터페이스에 의존한다. 매니저는 `ServiceLocator.Get<IXxxManager>()`로, 모듈은 `GetModule<인터페이스>()`로 가져오고, 시스템 간 통신은 직접 참조보다 이벤트(`EventBus`)를 우선한다.
- 네임스페이스는 폴더 경로를 따른다(`Lrw.Script.Upgrade`, `Agents.Players.FSM`, `DevLib.ModuleSystem`).
- private 필드는 `_camelCase`. 직렬화 자동 프로퍼티는 `[field: SerializeField] public T X { get; private set; }`.
- 커밋 메시지는 `feat : ...` 형식 접두사를 쓴다.
- PR 대상은 `main`이며 `main`에 직접 커밋하지 않는다.
- 각 팀원 영역의 세부 구조/가이드는 해당 폴더의 문서를 참고한다.
- 팀원별 개인 설정(작업 브랜치, 담당 폴더, 담당 영역 상세 등)은 이 파일이 아니라 git에 올라가지 않는 `CLAUDE.local.md`에 적는다.
