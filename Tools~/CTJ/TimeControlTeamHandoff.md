# TimeStopService 시간 제어 통일 — 팀원 전달

2026-11-26. 아래 외부 파일은 수정하지 않았다. GameManager 프리팹에 서비스는 연결해 두었다. 이 네 파일의 직접 쓰기를 옮기면 게임 코드에서 `Time.timeScale = ...`는 TimeStopService 한곳에 남는다.

## 공통 가져오기

```csharp
using _Scripts._Core._ServiceLocator;
using _Scripts.Agent.Enemies.Managers;

TimeStopService timeService = ServiceLocator.Get<TimeStopService>();
```

GameManager가 없는 씬에서는 서비스가 null일 수 있다. 해당 씬에도 GameManager를 연결하거나 누락 오류를 안내한다. null일 때 직접 Time.timeScale을 바꾸는 대체 코드를 만들면 쓰기 지점이 다시 분산된다.

## 적 피격 정지의 조절 위치

각 적 프리팹의 `TimeFreezeModule`에서 **Freeze Duration**(실시간 지속 시간)과 **Stop Time Scale**(정지 중의 절대 시간 배율)을 모두 정한다. 기본값은 0.05초·0.05배다. 0배는 완전 정지다. 공용 모듈 프리팹은 `Assets/_Prefabs/Enemies/Modules/TimeFreezeModule.prefab`이며, 특정 적만 다르게 하려면 해당 적의 자식 모듈 값을 오버라이드한다.

TimeStopService는 전달받은 값을 적용·복구한다. 서비스 인스펙터에서 적의 정지 배율을 정하지 않는다. `TimeFreezeModule`은 실제 플레이어 피해 이벤트에 이미 연결되어 있으므로 같은 피격에 추가 호출할 필요가 없다.

| 파일 | 기존 직접 쓰기 | 바꿀 요청 |
| --- | --- | --- |
| `Assets/_Scripts/UI/UI_System/PausingWindow.cs` | Freeze의 0배, Unfreeze의 이전 배율 복구 | `SetPaused(this, true/false)` |
| `Assets/_Scripts/UI/RunEnd/RunResultSource.cs` | PauseGame의 0배, ResumeGame의 이전 배율 복구 | `SetPaused(this, true/false)` |
| `Assets/_Scripts/Editor/CheatEditor/StageCheatSection.cs` | 슬라이더 값을 Time.timeScale에 대입 | `SetBaseTimeScale(value)` |
| `Assets/_Scripts/Test/StopTest.cs` | 정지키 0배, 재생키 1배 | `SetPaused(this, true/false)` |

## PausingWindow

현재 Freeze/Unfreeze 안의 `_prevTimeScale` 저장·복구를 제거하고, 아래처럼 요청 소유권을 사용한다. 기존 `_isFrozen`과 입력 차단·unscaled UI 페이드는 유지한다. 열 때 가져온 서비스를 필드에 보관하고 닫거나 OnDisable에서 같은 서비스에 해제를 요청한다.

```csharp
private TimeStopService _timeService;

private void Freeze()
{
    if (_isFrozen) return;

    _timeService = ServiceLocator.Get<TimeStopService>();
    if (_timeService == null)
    {
        Debug.LogError("GameManager의 TimeStopService가 필요합니다.");
        return;
    }

    _timeService.SetPaused(this, true);
    _isFrozen = true;
}

private void Unfreeze()
{
    if (!_isFrozen) return;

    if (_timeService != null) _timeService.SetPaused(this, false);
    _timeService = null;
    _isFrozen = false;
}
```

창이 두 개 열렸을 때 한 창이 닫혀도 다른 창의 정지를 유지한다. 피격 정지가 끝난 뒤 창을 닫을 때 과거 0.05배를 복구하는 문제도 막는다.

## RunResultSource

`_prevTimeScale`을 제거하고 서비스 필드를 보관한다. PauseGame에서 서비스가 있는지 검사한 뒤 `SetPaused(this, true)`를 호출한다. ResumeGame은 보관한 서비스에 `SetPaused(this, false)`를 요청하고 필드를 해제한다. 기존 `_isPaused` 중복 호출 방지와 GoNext의 ResumeGame 호출은 유지한다. 결과 화면이 다른 경로로 종료되는 경우에도 이 요청을 해제해야 한다.

## StageCheatSection

슬라이더 표시에는 순간 감속 중인 Time.timeScale 대신 **BaseTimeScale**을 쓴다.

```csharp
var timeService = ServiceLocator.Get<TimeStopService>();
if (timeService != null)
{
    float value = EditorGUILayout.Slider("게임 속도", timeService.BaseTimeScale, 0f, 5f);
    if (!Mathf.Approximately(value, timeService.BaseTimeScale))
        timeService.SetBaseTimeScale(value);
}
```

기본 배율을 2로 바꾸면 피격 정지나 사망 슬로우가 끝난 뒤 그 2배속으로 돌아간다.

## StopTest

정지키는 `SetPaused(this, true)`, 재생키는 `SetPaused(this, false)`를 요청한다. OnDisable에서도 자신이 건 정지를 해제한다. 다른 UI가 정지 중일 때 재생키를 눌러도 그 UI의 정지를 풀지 않는다. 1배속으로 강제 변경할 목적이라면 별도로 SetBaseTimeScale(1f)를 사용하지만 일시정지 소유권을 대신 해제하지는 않는다.

## 다른 연출에서 요청할 때

```csharp
// 실시간 0.08초 동안 절대 0.1배속을 요청합니다.
EventBus<HitStopEvent>.Raise(new HitStopEvent(0.08f, 0.1f));

// 직접 서비스에 요청해도 같습니다.
timeService.RequestHitStop(duration: 0.08f, stopTimeScale: 0.1f);

// 보스 사망처럼 유지 후 부드럽게 복귀하는 슬로우.
timeService.RequestSlowMotion(this, 0.2f, 0.25f, 0.2f);
// 해당 요청자가 제거될 때 필요하면 취소합니다.
timeService.CancelSlowMotion(this);
```

이벤트 방식을 사용할 파일은 `using _Scripts._Core._EventSystem;`도 추가한다. 두 번째 인자를 생략하는 기존 호출(`RequestHitStop(0.08f)`, `new HitStopEvent(0.08f)`)은 0.05배속을 사용한다. 원하는 연출 배율이 있으면 두 인자를 명시한다.

다른 배율의 요청이 겹치면 살아 있는 요청 중 가장 느린 배율을 적용하고, 각자의 실시간 종료 시각에 만료한다. 0.05배 요청이 먼저 끝나고 0.3배 요청이 남으면 0.3배로 돌아간다. 동일 배율끼리는 더 늦은 종료 시각까지 연장하며, 지속 시간을 합산하지 않는다.

TimeFreezeModule은 이미 이 이벤트를 요청하고, EnemyDeathSlowMotionModule도 서비스 요청으로 이관했다. 서비스는 일시정지·피격 정지·사망 슬로우를 조합해 적용하고 비활성화 시 정리한다. `Time.fixedDeltaTime`은 변경하지 않는다.

이관 전에는 기존 UI가 직접 설정한 0배 정지를 서비스가 덮어쓰지 않도록 감지한다. 하지만 서비스의 0배 피격 정지와 외부 코드의 0배 일시정지가 같은 순간 겹치는 경우 등은 값만으로 서로 구분할 수 없다. 위 소유권 API로 이관해야 모든 중첩을 정확하게 처리할 수 있다.
