# 팀 전달: Enemy23 사망 연출과 결과창 연결

2026-11-23. 사용자 요청에 따라 공용 맵·결과 UI 코드는 수정하지 않았다. 아래는 코드에서 확인한 현재 흐름과 팀원이 수정할 위치다. 이번에 구현한 보스 사망 연출은 [Boss23DeathFeedback.md](Boss23DeathFeedback.md)를 참고한다.

## 현재 완료된 CTJ 작업

- Enemy23 HP가 0이 되면 DeathStarted 1회, 금속 버스트, 흰색 화면 플래시와 짧은 감속이 시작된다.
- Death 클립 마지막 프레임을 고정한 뒤 코인을 드롭하고, 몸체는 0.2초 페이드 후 제거된다.
- 코인은 생성 시 골드를 더하지 않는다. 기존 낙하·바운스·흡수/소멸 연출이 끝날 때 CurrencyData에 지급한다. 코인 자체가 먼저 파괴되는 경우에도 OnDestroy에서 중복 없이 정산한다.
- 위 작업과 관련된 씬, 공용 맵·결과 UI 파일은 수정하지 않았다.

## 왜 공용 코드 연결이 필요한가

현재 방 완료는 사망 연출 종료가 아닌 **IsDead**를 기준으로 한다. 따라서 마지막 데스 프레임과 코인 정산을 기다리지 않고 결과창이 열릴 수 있다. 결과 데이터는 그때의 골드를 한 번만 복사하므로, 늦게 들어온 보스 코인이 결과 금액에 반영되지 않을 수 있다. 결과창의 다음 버튼을 먼저 누르면 사망 연출이나 아직 드롭하지 않은 보스 보상이 끊길 수도 있다.

현재 호출 흐름:

1. `EnemyBase.Die`: HP 0에서 IsDead = true.
2. `RoomController.IsAlive`: `enemy != null && !enemy.IsDead`로 판단하므로 보스가 살아 있는 적에서 제외된다.
3. `KillAllEnemiesCondition.WaitForAllEnemiesDead`: 0.2 게임 초 간격으로 HasAliveEnemy를 확인한 뒤 Cleared를 호출한다.
4. `RoomController.HandleCleared`: 포털 잠금을 풀고 방 데이터의 OnCleared 및 Cleared 이벤트를 전달한다.
5. `LinearStageGenerator.HandleLastRoomCleared`: 마지막 방이면 RunEndedEvent(BossCleared)를 즉시 발생시킨다.
6. `RunResultViewModel.HandleRunEnded`: 입력을 잠그고 현재 Gold·KillCount 등을 RunResultData에 복사한다. RunResultView는 즉시 표시한다.

## 수정 후보 파일과 함수

| 파일 | 현재 위치 | 필요한 수정 |
| --- | --- | --- |
| `Assets/_Scripts/Maps/LinearStageGenerator.cs` | `WatchLastRoomCleared`, `HandleLastRoomCleared` | 최종 보스의 **사망 연출 완료와 보상 정산 완료를 기다린 뒤** BossCleared를 한 번 발생시키는 연결 지점. 일반 방 클리어와 분리해서 처리하는 접근을 권장한다 |
| `Assets/_Scripts/Maps/Conditions/KillAllEnemiesCondition.cs` | `WaitForAllEnemiesDead` | 최종 보스 방 자체의 완료를 늦출 정책이라면, 단순 HasAliveEnemy 이외의 완료 조건을 추가하는 후보. 모든 일반 방에 사망 애니메이션 대기를 일괄 적용하지 않도록 주의한다 |
| `Assets/_Scripts/Maps/RoomController.cs` | `IsAlive`, `HasAliveEnemy`, `HandleCleared` | IsDead와 연출/보상 완료의 의미를 분리한다. 특히 HandleCleared는 포털 해제와 방 보상도 실행하므로 결과 이벤트만 늦출지 방 완료 전체를 늦출지 결정해야 한다 |
| `Assets/_Scripts/UI/RunEnd/RunResultViewModel.cs` | `HandleRunEnded` | 완료 이벤트를 늦춘다면 현재 snapshot 방식을 유지할 수 있다. 결과창을 먼저 열 정책이라면 골드 변경을 구독해 데이터를 갱신하는 추가 구현이 필요하다. `_isEnded` 중복 방지도 유지한다 |
| `Assets/_Scripts/UI/RunEnd/RunResultSource.cs` | `Gold`, `HandleRunEnded` | Gold는 현재 CurrencyData.Count만 읽는다. 미정산 코인은 포함하지 않는다. 최종 정산 후 다시 읽거나 골드 변경 전달이 필요하다 |
| `Assets/_Scripts/UI/RunEnd/RunResultData.cs` / `RunResultView.cs` | `Gold`, `Show` | 결과를 먼저 열고 실시간 갱신하는 방식을 택했을 때만 수정 후보다. 최종 정산 후 결과를 한 번 만드는 방식이라면 필수 수정은 아니다 |

우선 권장 순서는 **최종 보스 사망 → Death 마지막 자세에서 코인 드롭 → 코인 정산 완료 → 최종 금액 조회 → 결과 이벤트·결과창**이다. 결과 UI를 즉시 열어 놓고 이후 골드만 더하는 정책과 섞지 않는다.

## CTJ 쪽에서 연결 시 함께 정해야 할 지점

현재 외부에서 구독하는 별도의 `DeathAnimationCompleted`나 `RewardBatchCompleted` 이벤트는 **구현되어 있지 않다**. 아래는 다음 통합 작업에서 추가할 위치에 대한 제안이며, 이미 사용할 수 있는 API가 아니다.

- `Assets/_Scripts/Agent/Enemies/EnemyBase.cs`의 `FadeAndDespawnAsync`: 마지막 Death 상태를 `normalizedTime = 1`, Animator.speed = 0으로 고정한 뒤 `GrantDeathReward`를 호출한다. 연출/드롭 완료 알림을 만들 경우 이 지점에서 한 번 전달한다. 완료 신호 없이 간단히 기다릴 때는 보스 GameObject가 제거될 때까지 기다리는 방법도 있지만, 강제 파괴·씬 언로드와 정상 사망 완료를 구분해야 한다.
- `EnemyCurrencyRewardModule.GrantDeathReward`: 정확히 어떤 코인들이 보스의 한 보상 묶음에 속하는지 파악할 수 있는 생성 지점이다. 이 묶음의 미정산 개수나 완료 핸들을 전달하는 방식이 필요하다.
- `EnemyRewardCoin.PayReward`: `_completed`를 먼저 확정한 뒤 실제 골드를 더하는 정산 지점이다. 정상 흡수, 데스 타임, 코인 파괴에 의한 지급이 모두 여기로 모인다. 묶음 완료 카운트/알림을 추가한다면 **중복 방지 뒤 실제 지급 처리와 함께 한 번** 전달해야 한다.
- `EnemyDeathModule.ReportDeath`의 처치 수 집계는 보스 마지막 프레임보다 먼저 처리될 수 있다. 결과 snapshot은 연출·보상 이후 생성하면 최종 처치 수도 포함된다.

전체 씬의 코인을 FindObjects로 세어 기다리면 상자·잡몹 코인이 섞일 수 있다. 보스 보상 묶음만 추적하는 연결을 권장한다. 코인 생성 완료와 골드 지급 완료는 서로 다른 시점이다.

## 통합 후 팀 검증 항목

1. 실제 최종 보스 방에서 처치하고 화면 플래시·감속·Death 클립이 결과창에 가려지지 않고 마무리되는지 확인한다.
2. 마지막 Death 자세 전에 코인이 없고, 그 자세에서 정확히 한 묶음이 생성되는지 확인한다.
3. 가까운/먼 플레이어 위치, 코인을 즉시 못 먹는 경우와 코인 데스 타임 만료를 각각 검사한다. 보스 코인 정산 후 결과 골드와 CurrencyData.Count가 같아야 한다.
4. 동일 프레임 중복 사망, 보스 처치와 플레이어 사망의 겹침, 씬 이동/런 재시작으로 대기가 취소되는 경우에도 결과 이벤트와 보상을 중복 처리하지 않는다.
5. 감속 중 일시정지, 재개와 씬 전환 후 Time.timeScale이 원래 값으로 복원되는지 확인한다.
6. 일반 방의 포털·전투 완료·상자 해금은 기존 규칙을 유지하는지 회귀 확인한다.

이 문서는 코드 검토에 따른 인계 자료다. 공용 파일의 수정이나 실제 최종 던전 결과창까지의 실행 검증을 완료했다는 의미는 아니다.
