# Enemy23 공중 표시와 폭발음·코인 습득음

작업일: 2026-11-26.

## 변경 내용과 원인

공중 변형 후반 프레임과 비행·사격 자세에서 23 번호와 경광등이 머리보다 5px 낮게 배치되어 있었다. 실제 스프라이트의 머리 높이를 기준으로 Rise, Descend, AirIdle, AirShot, AirShotFront, AirShotRear의 위치 곡선을 보정했다. 번호 중심은 머리 윗면에서 9.5px 아래에 놓고, 경광등은 번호보다 10px 위에 둔다. 기존에 올렸던 경광등의 2px도 이 간격에 포함된다.

Rise의 번호 Y는 28 PPU 기준 **8.5 / 5.5 / 4.5 / 10.5 / 15.5 / 15.5px**다. Descend는 역순이고, 비행·사격 자세는 15.5px다. 위치를 스프라이트 교체 시점에 맞춰 변경하므로 표시만 별도로 미끄러지지 않는다. 비교: [수정 전](Boss23AirAlignment20261126Before.png), [수정 후](Boss23AirAlignment20261126After.png).

플라즈마 착탄음은 `Assets/_Sound/Enemy/PlazmaBoom.mp3`의 앞 **0.3초를 제외한 사본** `Assets/_Prefabs/Enemies/Effects/Audio/Clips/PlazmaBoom_Impact.wav`로 바꿨다. 길이는 2.472초에서 2.172초가 되며, 잘린 경계에서 클릭음이 생기지 않도록 시작 5ms만 짧게 페이드 인했다. 나머지 샘플은 PCM16 변환 오차 범위에서 동일하다. `Boss23_BombImpactSound.asset`가 이 사본을 참조한다. 원본 사운드 파일과 원본 `.meta`는 수정하지 않았다.

코인 습득음은 SoundManager의 대기열 때문에 지연된 것이 아니었다. 기존 코드가 코인 도착 후 **6프레임 / 12FPS = 0.5초**의 소멸 애니메이션을 끝낸 뒤 소리를 요청했다. `EnemyRewardCoin.BeginCollect(true)`에서 도착 즉시 한 번 재생하도록 요청 위치를 옮겼다. 소멸 애니메이션과 지급 시점은 유지한다. 시간 초과로 자동 정산될 때는 습득음을 재생하지 않는다.

## 검증

`EnemyTestScene`에서 실제 Play Mode로 아래를 확인했다. 테스트가 끝난 뒤 Edit Mode로 돌아왔으며 씬 파일과 미저장 상태는 변경하지 않았다. 게임 컴파일 오류·경고는 0, 마지막 Unity 콘솔 오류·경고도 0이었다.

| 대상 | 확인 결과 |
| --- | --- |
| 일반 공중 전환 | 약 2.823초, 상승 후 4회 사격, 앞뒤 런처 교대 중 표시 위치 정상 |
| 강화 공중 전환 | 약 2.242초, 4회·16발, 표시 위치 정상 |
| 하강 | 약 0.551초, 변형 중 표시 위치와 착지 후 Idle 정상 |
| 일반·강화 폭탄 착탄 | `PlazmaBoom_Impact`가 착탄 위치에서 재생 시작, 확인 시 재생 시간 0초 |
| 코인 1개 | 도착 즉시 1회 요청, 생성 약 0.019초 뒤 확인, 약 0.528초 뒤 3골드 정산 |
| 코인 12개 동시 | 도착 즉시 총 12회 요청, 약 0.008초 뒤 확인, 약 0.511초 뒤 36골드 정산 |
| 코인 시간 초과 | 습득음 0회, 3골드 한 번 정산 |
| 오디오 데이터 | 앞 0.3초 제거·시작 5ms 페이드·사전 로드 참조 확인, 최대 샘플 차이 약 0.000031 |

코인의 측정 시간은 테스트 코인을 플레이어 중심에 생성한 뒤 관측한 시간이다. 일반 비행 시간이나 스피커 출력 지연을 뜻하지 않는다. AudioSource와 재생 요청 시점을 검사했으며, 실제 청취는 아래 과정으로 확인하면 된다.

결과: [공중](Boss23AirMarkingsVerification.json), [위치 데이터](Boss23AirAudioAfter.json), [착탄음](Boss23BombTrimVerification.json), [오디오 샘플](Boss23BombTrimAssetVerification.json), [코인 습득음](CoinPickupSoundVerification.json).

## 직접 테스트하는 순서

1. `Assets/_Scenes/Test/EnemyTestScene.unity`에서 플레이어와 Enemy23을 활성화하고 실행한다. 공중 전환·공중 사격·하강 중 번호와 경광등이 머리 위치를 따라가는지 확인한다. HP 35% 이하의 강화 상태에서도 확인한다.
2. 일반·강화 표적 포격을 유도한다. 폭탄이 터질 때 앞부분의 공기 흡입음 없이 폭발 부분부터 들려야 한다. 음량·피치는 `Assets/_Prefabs/Enemies/Bosses/Enemy23/Boss23_BombImpactSound.asset`에서 조절한다.
3. 적 처치 후 코인을 받는다. 코인이 플레이어 중심에 도착해 소멸 모션을 시작하는 순간 소리가 나야 하며, 모션 종료 때 다시 재생되면 안 된다. 여러 코인을 동시에 받아도 소리가 뒤에 몰려 재생되지 않아야 한다.

## 사용한 문법과 이유

- `AnimationCurve`와 Constant 탄젠트: 스프라이트 프레임별 위치를 클립 데이터에 저장한다. 런타임에 이미지 픽셀을 분석하거나 매 프레임 위치를 새로 계산하지 않는다.
- `if (collectedByPlayer)`: 실제 플레이어 도착과 시간 초과 정산을 구분한다. 기존 `BeginCollect`의 상태 전환 순간에 재생 요청을 넣어 애니메이션 완료까지 기다리는 지연을 제거한다.
- `SoundCueSO`의 직렬화된 클립 참조: 공용 재생 경로를 재사용하면서 Enemy23 착탄음만 가공 사본으로 교체한다.
- 편집 도구의 `BinaryWriter` / `using`: WAV 헤더와 PCM16 샘플을 기록하고 파일을 자동으로 닫는다. 이 처리는 로컬 도구에서 한 번 수행되며 게임 중에는 실행하지 않는다.

도구: `FixBoss23AirAndAudio.Run`은 공중 클립과 가공 오디오 사본을 다시 생성한다. `InspectBoss23AirAudio.Inspect/CheckTrim`은 위치·샘플을 검사한다. 실행 검증은 `VerifyBoss23Rework.RunAirMarkings/RunCoinPickupSound/RunBombSound`다. 과거 `SetupBoss23VisualFix.Run`의 공중 기본값보다 이번 보정이 우선하므로 그 도구로 이번 값을 덮어쓰지 않는다.
