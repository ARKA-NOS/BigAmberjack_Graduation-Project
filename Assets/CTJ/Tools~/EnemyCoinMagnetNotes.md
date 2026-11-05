# 큰 바운스 후 플레이어 중심으로 회수되는 코인

후속 수정(2026-11-05): 회수 시 확대·축소 연출을 추가했습니다. 현재 프리팹의 바운스 비율은 0.5이며, 아래 0.65는 최초 적용 당시 값입니다. 최신 연출 설정은 [EnemyFeedbackPolishNotes.md](EnemyFeedbackPolishNotes.md)를 참고하세요.

2026-11-04. 이번 수정과 삭제 범위는 `Assets/CTJ`입니다. 이전 코인 설명보다 이 문서가 우선합니다.

## 동작

적 사망 → 코인 흩뿌리기/중력 낙하 → 바닥에서 크게 한 번 튀기 → 정점에서 자석 이동 시작 → 플레이어 중심에서 소멸 애니메이션 → 담당 Gold 지급.

바운스 높이는 **플레이어 몸체 높이의 65%, 최소 0.8유닛**입니다. 플레이어 크기를 알 수 없으면 키 2유닛을 기준으로 1.3유닛 튑니다. 천장이 낮으면 실제 상승은 천장에 막힐 수 있습니다.

`EnemyCurrencyRewardModule`이 사망 시 적이 바라보던 Target을 코인에 넘깁니다. PlayerController의 자식이 전달되어도 플레이어 본체로 찾아 올라갑니다. 대상이 없으면 기존 ServiceLocator의 IPlayerController를 확인하고, 없을 때 씬의 PlayerController를 재탐색합니다. 탐색 간격은 0.2초입니다.

목표점은 몸체 Collider2D의 **bounds.center**입니다. Transform 원점이 발밑에 있어도 몸체 중앙으로 모입니다. 콜라이더가 없으면 SpriteRenderer 중심, 이것도 없으면 Transform 위치를 사용합니다. 자석 이동 중과 소멸 애니메이션 중에도 중심을 다시 읽으므로 플레이어를 따라갑니다. 자석 이동은 지형에 막히지 않습니다.

플레이어가 아직 없으면 정점에서 기다립니다. 5초 동안 끝내 찾지 못하면 현재 위치에서 소멸/정산하는 예외 처리가 있습니다. 바닥을 못 찾은 코인은 Max Air Time 이후 자석 이동으로 넘어갑니다. 씬이 정리되어 코인이 먼저 제거될 때 남은 보상을 한 번 정산하는 기존 처리는 유지합니다.

## 인스펙터 조절

`Assets/CTJ/Prototypes/Prefabs/EnemyRewardCoin.prefab`의 EnemyRewardCoin 컴포넌트에서 조절합니다.

| 필드 | 현재 프리팹 값 | 역할 |
| --- | --- | --- |
| Bounce Height Ratio | 0.65 | 플레이어 키 대비 바운스 높이. 최소 0.5 |
| Minimum Bounce Height | 0.8 | 작은 캐릭터에서도 눈에 띄도록 보장할 최소 높이 |
| Fallback Player Height | 2 | 플레이어 크기를 알 수 없을 때의 기준 키 |
| Horizontal Retention | 0.65 | 튈 때 유지할 가로 속도 비율 |
| Initial Homing Speed | 2.5 | 자석 이동 시작 속도 |
| Max Homing Speed | 16 | 자석 이동 최고 속도 |
| Homing Acceleration | 35 | 최고 속도까지 빨라지는 정도 |
| Collect Distance | 0.12 | 중심에 도착했다고 판단할 거리 |
| Target Wait Timeout | 5 | 플레이어를 찾지 못했을 때 기다릴 시간 |
| Gravity Scale | 3 | 현재 프리팹의 기존 중력 배율. 높이는 유지하고 상승 시간을 바꿈 |
| Max Air Time | 3 | 바닥 없는 공간의 대기 제한 |
| Collect Frames Per Second | 12 | 기존 6장 소멸 애니메이션이 0.5초 동안 재생됨 |

코인 수는 기존대로 3골드당 1개, 최대 12개입니다. 총액을 균등 분배하고 나머지를 1골드씩 나누며 총 보상 스탯은 변경하지 않았습니다. 예전 즉시 지급 코드 백업도 유지합니다.

## 혈흔 정리

- 삭제: `Assets/CTJ/Assets/NEw pack blood` 전체, 시트 기반 HitBlood/HitBlood2~5 프리팹·머티리얼·메타.
- 삭제: 옛 ConfigureEnemyParticles/VerifyEnemyParticles 도구, 해당 혈흔 캡처 3장.
- 유지: HitBloodSpray, BloodPolygon 머티리얼·메시·셰이더와 현재 모듈. 여섯 적은 이 폴리곤 혈흔 하나를 공유합니다.
- 일반 HitImpact/HitSlash/HitSparks는 혈흔이 아닌 재사용 가능한 별도 이펙트라 유지했습니다. 이전 학습 문서는 기록으로 남겼습니다.

삭제 전에 에셋 GUID 171개의 외부 참조를 검사했으며, 삭제 대상 밖의 사용 참조는 없었습니다. 삭제 후 여섯 적의 누락된 스크립트·오브젝트 참조와 폴리곤 머티리얼/메시를 다시 검사했습니다.

## 직접 테스트

1. GameManager/CurrencyManager와 플레이어가 있는 CTJ_MapTestScene에서 Play 합니다.
2. Enemy01 등을 바닥 위에서 처치합니다. 코인이 흩어졌다가 착지하고, 캐릭터 키 절반보다 높게 한 번 튀는지 봅니다.
3. 코인이 올라가는 동안 플레이어를 움직입니다. 정점 이후 코인이 가속해서 따라오며 발밑이 아닌 몸체 중앙에서 사라지는지 봅니다.
4. 중앙에서 기존 금색 소멸 애니메이션이 끝날 때 Gold가 증가하는지 봅니다. 기본 Enemy01은 총 10골드/4개, Enemy02는 15골드/5개입니다.
5. 발판 위와 벽 옆에서도 확인합니다. 별도 테스트에서는 플레이어를 늦게 생성해 기다리던 코인이 따라오는지도 확인할 수 있습니다.
6. 적을 타격해 폴리곤 혈흔만 나오고, 바닥 흔적/벽 흘러내림이 유지되는지 봅니다.

영구 조절은 Play 종료 후 코인 프리팹에서 합니다. 씬과 플레이어 프리팹은 이번 작업에서 저장하지 않았습니다.

## 실행 검증

Unity 컴파일 오류 없음. 독립된 빈 씬에서 검사한 뒤 원래 BossPatternTest 씬을 복원했습니다.

- 키 2유닛: 바닥 접촉 후 상승 높이 약 **1.300유닛**.
- 키 4유닛: 약 **2.600유닛**. 각 키의 65%이며, 절반 이상 조건 통과.
- 원점에서 어긋난 콜라이더 중심, 이동하는 대상, 늦게 지정되는 대상: 모두 중심 추적/정확한 지급 통과.
- 여섯 적, 보스의 임시 보상값, 0/1/3/4/6/7/10/11/36/37/1000000골드: 총액·개수 상한·나머지 분배·한 번 지급 통과.
- 트리거 무시, 일방향 발판, 벽, 얇은 바닥, 바닥 없는 경우, 씬 언로드 정산 통과.
- 소멸 6프레임 재생과 완료 시 지급, 실제 렌더링에서 바운스와 중심 소멸 확인.

검증 진입점은 VerifyEnemyCoinReward의 RunEdit / RunPlay / RunMagnetPlay, 혈흔 참조는 VerifyEnemyBloodSpray.RunEdit입니다. 실행 검사는 기존 BeginPlay → 검사 → StopPlay → RestoreScene 순서를 사용합니다. 검증 대상은 가상 몸체 콜라이더와 실제 적 프리팹이며, 실제 플레이어 조작감은 위 직접 테스트에서 확인합니다.

## 사용 문법과 이유

- **enum CoinPhase**: 산개·바운스·추적·소멸을 서로 다른 상태로 표현합니다. 여러 bool이 동시에 켜지는 조합을 피하고 FixedUpdate/Update에서 현재 처리할 일을 명확히 나눕니다.
- **Collider2D.bounds**: 월드 좌표의 중심과 크기를 얻습니다. offset이나 Transform 스케일이 있어도 실제 몸체 중심과 키를 사용합니다.
- **Mathf.Sqrt(2 × 중력 × 높이)**: 원하는 높이까지 올라갈 초기 속도를 계산합니다. 바운스 속도를 임의로 지정하는 대신 키에 비례한 높이를 보장합니다.
- **v × dt + 0.5 × g × dt²**: 한 물리 프레임 동안의 이동량입니다. 동시에 v에 g × dt를 더해 속도를 갱신하므로, 프레임별 중력 오차로 정점이 낮아지는 것을 줄입니다.
- **Vector2.MoveTowards / Mathf.MoveTowards**: 목표점을 넘지 않는 위치 이동과 속도 증가에 사용합니다. 매번 갱신한 플레이어 중심을 향해 가속합니다.
- **out Vector2 center / out float height**: 대상 검색 성공 여부를 반환하면서 중심과 키도 함께 전달합니다. 같은 콜라이더를 두 번 찾지 않습니다.
- **as PlayerController**: 공용 IPlayerController에서 Transform을 사용하기 위한 안전한 형변환입니다. NullPlayerController 등 실제 캐릭터가 아닌 서비스는 null이 되어 씬 검색으로 이어집니다. 공용 인터페이스는 수정하지 않습니다.
- **internal SetCollector**: 보상 모듈이 대상을 전달하는 동일 어셈블리 내부 진입점입니다. 코인의 물리/회수 상태는 코인 자신이 관리합니다.

새 패키지나 공용 인터페이스는 추가하지 않았습니다.
