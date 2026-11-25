# Enemy23 표적 포격 폭발음

2026-11-25.

`Boss23_Bomb.prefab`의 `EnemyLobProjectile > Impact Cue`가 비어 있어 착탄음이 나오지 않았다. 전용 SoundCueSO `Boss23_BombImpactSound.asset`를 만들고 `Assets/_Sound/Enemy/PlazmaBoom.mp3`를 연결했다. 기본 음량은 0.8, 피치는 1이다.

기존 `FinishFlight`가 실제 충돌 지점에서 공용 SoundManager에 재생을 요청하는 기능을 사용한다. 일반 포격·강화 포격·발악 진입 폭격이 같은 폭탄 프리팹을 사용하므로 모두 적용된다. 게임 C# 로직은 추가하지 않았다.

## 조절 위치와 직접 테스트

- 음량·피치: `Assets/_Prefabs/Enemies/Bosses/Enemy23/Boss23_BombImpactSound.asset`의 Volume / Pitch Range.
- 폭탄 연결: 같은 폴더 `Boss23_Bomb.prefab` 루트의 EnemyLobProjectile / Impact Cue.
- `EnemyTestScene`에서 Enemy23이 표적 포격을 사용하게 한다. 발사음 다음, 경고 원이 닫히고 폭탄이 착탄해 폭발하는 순간 `PlazmaBoom`이 재생되어야 한다. 강화 포격과 발악 진입 폭격도 같은 방식으로 확인한다.

## 검증 및 사용한 방식

실행 검증 진입점은 `VerifyBoss23Rework.RunBombSound`이고 결과는 `Boss23BombSoundVerification.json`에 기록한다. 일반·강화 폭탄을 실제 포물선으로 발사해, 비행이 끝난 시점의 AudioSource 클립·재생 위치·재생 시간을 검사한다.

일반 약 1.657초, 강화 약 1.155초 비행 후 착탄 지점 `(4, 0)`에서 `PlazmaBoom`이 재생되는 것을 확인했다. 두 경우 모두 착탄 시점 AudioSource 재생 시간은 0초였고 피치는 1이었다. 테스트 당시 옵션 음량을 반영한 실제 AudioSource 음량은 0.68이었다.

이번 게임 동작 변경은 인스펙터의 **직렬화된 참조 연결**뿐이다. 기존 `SoundCueSO`에 클립을 담고, 폭탄이 그 데이터를 참조하게 해 기존 공용 사운드 경로를 사용한다. 설정 도구의 `SerializedObject`는 이 인스펙터 값을 저장하고, `try/finally`는 프리팹 편집이 끝나면 임시 편집 내용을 메모리에서 정리하기 위해 사용한다.
