# 사망 보상을 코인 연출로 지급하기

> 2026-10-29 변경: 현재는 3골드 기준, 중력 바운스, 스프라이트 소멸 애니메이션을 사용합니다. 아래는 최초 구현 기록입니다. 최신 설정·테스트·문법 설명은 [EnemyCoinBounceNotes.md](EnemyCoinBounceNotes.md)를 보세요.

2026-10-28. 수정 범위는 Assets/CTJ만입니다. 공용 CurrencyManager, GameManager, 씬 파일은 수정하지 않았습니다.

## 동작과 책임

1. EnemyBase의 기존 사망 처리에서 EnemyCurrencyRewardModule.GrantDeathReward를 호출합니다.
2. 모듈은 기존 StatModule의 EnemyGoldReward를 읽고 소수 부분을 버린 총액을 계산합니다. 0이면 생성하지 않습니다.
3. 코인 프리팹을 적과 독립된 루트 오브젝트로 생성하고 금액을 나눕니다. 이때 Gold는 증가하지 않습니다.
4. EnemyRewardCoin은 중력을 적용하여 흩어지고, 바닥에 닿으면 0.65만큼 떠오르며 0.7초 동안 투명해집니다.
5. 각 코인이 보이지 않게 되는 시점에 맡은 금액만 CurrencyData.Add에 전달하고 제거됩니다. 따라서 UI는 코인이 사라지는 순서대로 갱신됩니다.

EnemyBase / NewHealthModule / 사망 집계 모듈의 동작은 유지합니다. 플레이어가 코인과 접촉할 필요는 없습니다. 기존 Gold 에셋의 CurrencyIcon을 참조하며, 어두운 맵에서도 보이도록 CTJ의 PrototypeSpriteUnlit 머티리얼을 사용합니다.

## 인스펙터에서 조절할 곳

**적의 CurrencyRewardModule 오브젝트 → EnemyCurrencyRewardModule**

| 필드 | 기본값 | 용도 |
| --- | --- | --- |
| Reward Currency | 공용 Gold | 지급할 재화 |
| Reward Amount Stat | EnemyGoldReward | 적별 StatGroup에서 읽을 총 보상 금액 |
| Coin Prefab | EnemyRewardCoin | 생성할 코인 외형과 움직임 설정 |
| Currency Per Coin | 5 | 1~5골드는 1개, 6~10골드는 2개, 11~15골드는 3개 |
| Max Coin Count | 12 | 연출할 최대 코인 수. 초과 금액도 모두 분배 |
| Spawn Offset | (0, 0.35) | 적 루트의 월드 위치 기준 생성 위치 |

총액은 기존 `Prototypes/Stats/*_Stats.asset`의 **EnemyGoldReward 항목 Base Value**에서 조절합니다. 별도의 즉시 지급은 하지 않습니다. 예: 11골드 → 3개에 4/4/3골드, 61골드 → 12개에 6골드 하나와 5골드 열한 개. Catto/Enemy01/Enemy04는 기존 10골드, MadGhost/Enemy02는 기존 15골드입니다. RobotEyeBoss는 기존 보상 0을 유지하며, 보상 스탯을 넣으면 같은 코인을 사용합니다.

**Prototypes/Prefabs/EnemyRewardCoin.prefab → EnemyRewardCoin**

| 필드 | 기본값 | 용도 |
| --- | --- | --- |
| Horizontal Speed | 1.8 | 좌우로 퍼지는 최대 초기 속도 |
| Upward Speed Range | 2.8~4.2 | 위로 튀는 초기 속도 범위 |
| Gravity Scale | 1.5 | 프로젝트 Physics2D.gravity의 배율 |
| Terrain Layers | Ground | 바닥과 벽. 플레이어/적 레이어는 넣지 않기 |
| Collision Radius | 0.1 | 지형 검사 반지름, 월드 단위 |
| Max Air Time | 3초 | 바닥 없는 곳에서 무한 낙하하지 않게 연출을 종료하는 기준 |
| Rise Height | 0.65 | 착지 후 상승 거리 |
| Fade Duration | 0.7초 | 상승과 페이드에 걸리는 시간 |

코인 크기는 프리팹 Transform Scale(기본 0.24), 외형은 SpriteRenderer의 Sprite에서 바꿉니다. 크기를 크게 바꾸면 Collision Radius도 맞춰 주세요. 움직임 설정은 월드 단위라 Transform Scale에 곱해지지 않습니다. 모든 적이 같은 프리팹을 참조하므로 공통 조절이 적용됩니다. 특정 적만 다르게 하려면 코인 프리팹 Variant를 만들어 해당 모듈에 연결하면 됩니다.

## 지형과 수명 처리

- Rigidbody/Collider로 물리 힘을 주지 않고 CircleCast로 경로를 검사하므로 캐릭터를 밀거나 막지 않습니다. 얇은 바닥도 이동 경로 전체에서 검사합니다.
- 트리거를 무시하며, 일방향 PlatformEffector2D 발판은 상승 중 통과하고 하강할 때 윗면에 착지합니다. 벽/천장에 닿으면 그 안쪽 속도를 제거하고 떨어집니다.
- 적이 먼저 제거되어도 코인은 계속 작동합니다. 바닥을 못 찾으면 Max Air Time 뒤 현재 위치에서 떠오르고 정상 지급합니다.
- 룸/씬 정리 때문에 연출 도중 코인 자체가 제거되면 OnDestroy에서 남은 금액을 정산합니다. 정상 소멸 때 이미 지급한 코인은 다시 지급하지 않습니다. 게임/Play 종료에는 정산하지 않습니다.
- CurrencyManager 미초기화, 재화/스탯/코인 참조 누락은 경고로 알립니다. 지급 시 정수 보유량 한도를 넘으면 해당 지급을 막습니다.

## 직접 테스트하는 순서

1. CTJ_MapTestScene에서 GameManager와 CurrencyManager가 활성화되어 있고 Gold가 등록되어 있는지 확인하고 Play 합니다.
2. Enemy01 등을 처치합니다. 사망 직후가 아닌 **코인이 사라질 때** Gold가 증가하는지 확인합니다. Enemy01의 기본값은 2개 × 5골드입니다.
3. 코인이 작은 포물선으로 흩어진 뒤 Ground 바닥에서 떠오르며 투명해지는지 봅니다. 플레이어 접촉은 필요 없습니다.
4. 테스트할 적의 StatGroup에서 보상값을 11로 설정하면 3개가 나와 합계 11을 줍니다. 61이면 12개가 나와 합계 61을 줍니다. 테스트 뒤 원하는 보상값으로 돌려놓습니다. 공유 StatGroup을 수정하면 이를 쓰는 적 모두에 적용됩니다.
5. 여러 적을 연속 처치하고, 적의 사망 애니메이션이 먼저 끝나도 각각의 총액만 한 번 지급되는지 확인합니다.
6. 새로운 맵에서 코인이 바닥을 통과한다면 먼저 코인 프리팹의 Terrain Layers와 실제 지형 Collider2D 레이어를 확인합니다.

자동 확인은 Tools~의 VerifyEnemyCoinReward.RunEdit / RunPlay, 기존 VerifyEnemyNewHealth.RunPlay를 사용합니다. RunPlay는 VerifyEnemy01.BeginPlay로 연 독립 씬에서만 실행하고, 끝나면 StopPlay / RestoreScene으로 복구합니다. 검증은 공용 저장 함수를 호출하지 않습니다. RenderProof는 확인용 PNG를 Tools~에 생성하므로 확인 후 삭제할 수 있습니다.

확인 결과: Unity 컴파일 오류 0건. 여섯 프리팹 연결, 기존 체력/사망/처치 수 연동, 0·1·5·6·11·60·61·1,000,000골드 분배, 보스에게 임시 보상 부여, 중복 사망, 적 선제 제거, 바닥 없는 경우, 트리거/일방향 발판/벽/얇은 바닥, 씬 언로드 정산 검사를 통과했습니다. 어두운 배경의 산개/상승/페이드 렌더링도 확인했고 임시 캡처는 삭제했습니다.

## 사용 문법과 선택 이유

- **기존 모듈 + MonoBehaviour 조합**: 사망 보상 금액을 읽는 책임은 ModuleOwner에 속한 모듈에 두고, 적보다 오래 살아야 하는 코인은 독립 MonoBehaviour로 둡니다. 새 인터페이스/추상 클래스를 만들 필요가 없습니다.
- **정수 나눗셈 `/`와 나머지 `%`**: 총액을 코인 수로 나누고 남은 만큼 앞의 코인에 1씩 추가합니다. float 반올림으로 돈이 줄거나 늘지 않습니다. `1 + (amount - 1) / 기준금액`으로 올림 개수를 구합니다.
- **`FixedUpdate`, `Time.fixedDeltaTime`**: `속도 += 중력 × 시간`, `위치 += 속도 × 시간`으로 작은 중력 운동을 계산합니다. 프로젝트의 중력 설정을 그대로 사용합니다.
- **`ContactFilter2D`, `Physics2D.CircleCast`, `List<RaycastHit2D>`**: 선택한 레이어의 지형만 검사합니다. 결과 리스트를 재사용하여 매 프레임 배열을 새로 만들지 않습니다. 코인에는 캐릭터를 밀 Collider/Rigidbody가 없습니다.
- **`TryGetComponent(out PlatformEffector2D platform)`**: 일방향 발판일 때만 예외 처리를 적용합니다. `out`은 메서드가 찾아낸 컴포넌트를 지역 변수에 넘겨주는 문법입니다.
- **`Mathf.Clamp01`, `SmoothStep`, Color의 알파**: 페이드 진행도를 0~1로 제한하고 부드러운 상승 곡선을 만듭니다. Color는 값 형식이라 복사본의 알파를 바꾼 뒤 Renderer에 다시 대입합니다.
- **`_rewardHandled` / `_completed`**: 한 적의 코인 생성과 한 코인의 지급을 각각 한 번만 허용합니다. CurrencyData.Add가 이벤트를 호출하기 전에 완료 표시를 하여 재진입/중복 지급을 막습니다.
- **`OnDestroy`와 `OnApplicationQuit`**: 게임 도중 씬 정리로 없어지는 경우와 실제 게임/Play 종료를 구분합니다. 정상 완료 후 OnDestroy가 와도 동일한 완료 플래그로 중복 지급을 막습니다.

## 이전 방식 백업

`Tools~/Backups/EnemyCurrencyRewardModule.Immediate.cs.txt`에 변경 전 코드를 그대로 보관했습니다. Tools~와 .txt 확장자 때문에 게임에 컴파일되지 않아 중복 클래스/중복 지급이 생기지 않습니다.

즉시 지급으로 되돌리려면 이 파일 내용을 `Scripts/Enemies/EnemyCurrencyRewardModule.cs`에 복원합니다. 기존 .meta는 유지하세요. 기존 재화/스탯 필드와 EnemyBase의 호출을 유지했으므로 프리팹의 이전 연결을 다시 만들 필요는 없습니다. 이 경우 코인 지연 지급용 검증의 기대값도 바꿔야 합니다.
