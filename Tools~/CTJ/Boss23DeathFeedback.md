# Enemy23 사망 연출

작업일: 2026-11-23. Enemy23의 사망 순간에는 임시 설정의 대량 금속 분사와 함께 화면 흰색 플래시·짧은 시간 감속을 실행한다. 몸체는 Death 애니메이션을 끝까지 재생하고, 마지막 자세를 고정한 시점에 코인을 한 번 드롭한 뒤 0.2초 동안 페이드한다.

## 프리팹과 모듈

`Assets/_Prefabs/Enemies/Bosses/Enemy23/Boss23_ProductionControlDrone.prefab`에 두 자식을 추가했다.

| 자식 | 담당 | 기본값 |
| --- | --- | --- |
| EnemyDeathScreenFlashModule | 화면 플래시 UI 생성·정리 | 흰색 알파 0.35, 0.35초 페이드 |
| EnemyDeathSlowMotionModule | 게임 시간 감속·복원 | 기존 시간 배율 ×0.2, 실시간 0.25초 유지 + 0.2초 복귀 |

각 자식의 컴포넌트나 오브젝트를 끄면 해당 기능만 해제된다. 강도·시간은 각 모듈에서 별도로 조정한다. 공통 모듈 프리팹은 `Assets/_Prefabs/Enemies/Effects/EnemyDeathScreenFlashModule.prefab`, `EnemyDeathSlowMotionModule.prefab`이다.

화면 표시에는 `Assets/_Prefabs/Enemies/Effects/EnemyDeathScreenFlash.prefab`의 기존 uGUI Canvas·Image·CanvasGroup을 사용한다. Screen Space Overlay, 정렬 순서 500, 전체 화면 Stretch이며 클릭과 입력을 가로채지 않는다. 보스와 분리하여 생성하므로 보스의 크기·회전 영향을 받지 않는다. `Time.unscaledTime`을 기준으로 흰색 화면이 사라져 게임 감속에도 0.35초로 유지된다. 씬이나 플레이어 카메라에는 컴포넌트를 추가하지 않았다.

## 데스 애니메이션과 코인

EnemyBase의 `Complete Death Animation Before Reward`를 Enemy23에서만 켰다. 이 옵션은 임시 SO의 즉시 페이드보다 우선하지만, 사망 금속의 속도·개수 및 피격/처치 쉐이크 배율은 그대로 적용한다.

1. HP 0 이하: IsDead를 확정하고 스킬·행동을 중단한다. DeathStarted는 한 번 발생한다. 데스 첫 프레임, 금속 분사, 화면 플래시와 시간 감속이 시작된다.
2. 기존 Death 클립을 끝까지 재생한다. 현재 클립 길이는 약 0.995 게임 초이므로 감속 중에는 실제 재생 시간이 더 길어진다.
3. 마지막 프레임을 명시적으로 평가하고 Animator 속도를 0으로 고정한다. 이 시점에 기존 EnemyCurrencyRewardModule로 코인을 한 번 생성한다.
4. 마지막 그림을 유지하며 기존 0.2초 페이드를 적용하고 보스를 제거한다. 코인은 기존 낙하·흡수 후 지급 규칙을 사용한다.

일반 적은 이 옵션을 켜지 않아 기존 즉시 페이드·즉시 코인 드롭을 유지한다. 새 화면/감속 모듈도 Enemy23에만 연결했다. 임시 SO의 Use Temporary Feedback을 끄면 금속·쉐이크의 임시 배율은 해제되지만, 이 문서의 일반 사망 연출 모듈과 보스의 마지막 프레임 보상 정책은 유지된다.

## 시간 복원

감속 시작 전 시간 배율을 기억했다가 복원한다. 원래 배율이 0.7이면 0.14로 감속한 뒤 0.7로 돌아온다. 첫 프레임 전체의 시간을 더하지 않고 감속 시작 시각 이후의 unscaled 시간만 센다.

Time.timeScale이 0이면 일시정지를 유지하고 효과 진행도 멈춘다. 일시정지 중 모듈을 꺼도 0을 덮어쓰지 않는다. 기존 PausingWindow가 저장한 감속 배율을 복원한 뒤 UniTask로 원래 배율까지 정리한다. 모듈/보스 제거 시 복원하고, 여러 모듈이 연속 발동해도 감속 배율을 중첩 곱하지 않는다. 다른 시스템이 0 이외의 다른 시간 배율로 바꾸면 그 값을 덮어쓰지 않는다.

## 검증

EnemyTestScene에서 저장되지 않는 런타임 인스턴스로 검증했다.

- 사망 순간 Death 클립 시작, DeathStarted 1회, 코인 0개.
- 사망 금속 32개 생성(임시 ×2의 16~32개 범위).
- 화면 플래시 약 0.355 실초 뒤 제거, 시간 감속 약 0.451 실초 뒤 원래 배율 복구.
- 약 1.264 실초에 Animator가 마지막 Death 자세(normalizedTime 1 이상, speed 0)인 상태로 총 48골드의 코인 생성. 보스 프리팹 보상 범위 45~65 안이었다.
- 같은 적에게 중복 사망 피해를 줘도 사망 이벤트와 코인 생성이 늘지 않았다. 마지막 자세 페이드 후 보스가 제거됐다.
- 두 모듈을 각각 껐을 때 독립 동작, 보스 제거 시 플래시/시간 정리, 원래 배율 0.7 복원, 일시정지 유지·해제 후 복원, 중첩 보스 사망의 배율 비누적을 확인했다.
- Enemy17은 기존 즉시 코인 드롭을 유지하고 보스 전용 화면·감속은 발생하지 않았다.
- 컴파일 오류 0건, 활성 적 7종의 사망 정책·참조 검사 통과. Enemy23의 기존 에셋 검사도 의존성 150개·애니메이션 11개를 확인하며 통과했다. 검증 후 Time.timeScale 1, 저장 변경 없는 EnemyTestScene Edit Mode로 복귀했다.

직접 확인하려면 EnemyTestScene에서 Enemy23을 활성화하고 처치한다. 마지막 사망 자세까지 코인이 나오지 않다가 그 자세에서 드롭되는지 보고, 두 자식 모듈을 각각 꺼서 차이를 비교한다. 모듈을 꺼도 금속 버스트와 데스 애니메이션·보상 시점은 유지된다. 수치 변경은 Play Mode에서 먼저 비교하고 필요하면 Edit Mode에서 적용한다.

재검증 도구는 `Tools~/CTJ/VerifyBoss23DeathFeedback.cs`다. 저장된 EnemyTestScene에서 `Enter`, Play Mode 진입 뒤 `Run`을 Pipeline script run으로 호출하며, 도구가 시작한 실행을 마친 뒤 종료한다. `CheckAssets`는 7종 적의 정책·연결을 읽기 전용으로 검사한다.

통합 범위: 공용 `RoomController`는 여전히 IsDead가 되는 시점부터 방의 살아 있는 적에서 제외하고, 최종 방의 완료 이벤트는 결과 UI를 즉시 연다. 이 문서의 테스트는 사망 연출·코인 생성 순서에 대한 검증이며, 공용 맵의 결과창 표시 지연이나 결과 금액 갱신은 수정하지 않았다. 해당 파일은 현재 허용 영역 밖이다.

사용자 지시에 따라 해당 공용 코드는 수정하지 않았고, 팀에 전달할 파일·함수·순서 및 코인 정산 연결 지점을 [인계 문서](Boss23RunEndHandoff.md)에 따로 정리했다.

## 사용한 문법과 이유

- `DeathStarted += ...` / `-=`: 기존 사망 이벤트를 구독하여 화면과 시간 효과를 별도 모듈로 분리한다. `OnDisable/OnDestroy`에서 구독과 실행 중 효과를 정리한다.
- `Time.unscaledTime`: 게임 감속과 별개로 화면 페이드와 시간 효과의 유지/복귀 시간을 계산한다. `Time.timeScale`은 저장한 원래 값에 배율을 곱하고 자기 효과가 소유한 값만 복원한다.
- `Mathf.SmoothStep`와 `Lerp`: 화면 알파와 시간 배율이 부드럽게 원래 상태로 돌아가도록 보간한다.
- `UniTask.NextFrame`과 `Application.exitCancellationToken`: 일시정지가 풀릴 때까지 에디터를 막지 않고 기다린다. 보스 제거 뒤에도 필요한 복원만 마무리하며 Play Mode 종료 시 대기를 취소한다.
- `static` 활성 모듈 참조: 여러 보스가 같은 프레임에 죽어도 전역 시간 배율에 효과를 중첩 곱하지 않는다. 새 일반 매니저나 패키지는 추가하지 않았다.
- `SerializeField bool completeDeathAnimationBeforeReward`: 현재 필요한 보스만 임시 즉시 페이드에서 제외하고 마지막 프레임 보상을 선택한다. 별도의 보스 체력/재화 구현을 만들지 않고 기존 사망 파이프라인을 재사용한다.
