# Enemy23 사선 레이저 발사음

작업일: 2026-11-24.

`Assets/_Sound/Enemy/yodguard-short-energy-beam-shot-4-482500.mp3`를 Enemy23 사선 레이저 발사음으로 연결했다. 원본 오디오와 임포트 설정은 수정하지 않았다.

## 연결과 조절

- 전용 Cue: `Assets/_Prefabs/Enemies/Bosses/Enemy23/Boss23_FanLaserSound.asset`
- Clips: 요청한 클립 1개.
- Volume: **0.7**, Pitch Range: **1~1**, Priority: **128**.
- 보스 `SoundModule > EnemySoundModule > Attack Sounds`의 **FanLaser** 항목에 연결했다.

기존 `DroneDownwardLaserSkill.NotifyAttackPerformed`가 빔을 켠 프레임에 발생하고, EnemySoundModule이 공용 SoundManager에 재생을 요청한다. 일반은 1회, 강화는 두 번의 발사마다 각각 1회 재생한다. 다른 공격음 연결은 유지했다. 게임 C# 코드는 추가·변경하지 않았다.

클립 길이는 3.024초이며, 약 0.035초부터 유효한 신호가 확인돼 원본 그대로 사용했다. 볼륨과 피치는 위 Cue에서 조정한다.

## 직접 테스트

1. `Assets/_Scenes/Test/EnemyTestScene.unity`에서 플레이어·Enemy23과 SoundManager가 있는 GameManager를 켜고 실행한다.
2. 사선 레이저 패턴의 경고 이후 빔이 실제로 켜질 때 새 에너지 빔 효과음이 재생되는지 듣는다.
3. HP 35% 이하의 강화 사선 레이저에서는 첫 발사와 두 번째 발사에 각각 재생돼야 한다.
4. 필요하면 전용 Cue의 Volume을 조절한다. 연사나 공중 사격의 사운드가 함께 바뀌면 안 된다.

## 검증

EnemyTestScene의 실제 등록된 SoundManager와 AudioSource로 확인했다. 일반 1회·강화 2회의 발사 프레임마다 요청한 AudioClip의 `isPlaying == true`, `time == 0` 상태를 확인했다. 기존 레이저 피해·왕복 횟수도 통과했다. 씬을 저장하지 않았고 검증용 Play Mode는 종료했다.

초기 검증 코드는 별도 SoundManager를 만들었으나, 이미 DontDestroyOnLoad에 등록된 매니저가 있어 그 테스트 객체는 등록되지 않았다. 이 때문에 검증 코드에서 예외 1건이 발생했다. 실제 등록된 매니저를 검사하도록 고친 뒤 최종 검증을 통과했다. 게임 연결이나 공용 SoundManager 코드를 수정한 것은 아니다.

결과: [발사 프레임과 실제 음원 재생](Boss23LaserSoundVerification.json).

## 사용 문법과 이유

이번 게임 변경은 **ScriptableObject 참조 연결**이다. 기존 SoundCueSO에 클립·볼륨·피치를 저장하고 스킬별 사운드 모듈이 이를 읽는다. 새로운 사운드 재생 로직이나 이벤트를 만들지 않았다.

에디터 보조 도구의 `SerializedObject`는 프리팹의 기존 FanLaser 항목만 찾아 Cue를 바꾸는 데 사용했다. 테스트에서는 `AudioSource.clip / isPlaying / time`을 검사해 연결 값뿐 아니라 실제 재생 상태도 확인했다.

연결 보조 도구: `SetupBoss23LaserSound.Run`. 실행 검증: `VerifyBoss23Rework.RunLaserSound`.
