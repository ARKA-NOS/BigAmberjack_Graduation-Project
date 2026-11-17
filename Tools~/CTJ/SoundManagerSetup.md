# 공용 사운드 매니저 연결

## 매니저와 믹서

1. `GameManager` 아래에 활성 자식 오브젝트 `Sound`를 만들고 `SoundManager` 컴포넌트를 추가한다. `GameManager`가 시작할 때 활성 자식에 있는 `AbstractManager`를 초기화한다. `AudioSource`는 매니저가 실행 중에 생성한다.
2. Project 창에서 `Create > Audio > Audio Mixer`로 믹서를 만들고 `Master` 아래에 `Music`, `SFX`, `UI` 그룹을 추가한다.
3. 각 그룹을 선택하고 Inspector의 `Attenuation` 볼륨 슬라이더를 우클릭해 `Expose … to script`를 선택한다. Audio Mixer 창 오른쪽 상단의 `Exposed Parameters`에서 노출한 항목의 이름을 아래 표에 맞춘다. [Unity AudioMixer.SetFloat 문서](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Audio.AudioMixer.SetFloat.html)
4. Audio Mixer 창에서 그룹을 `SoundManager`의 해당 필드에 끌어다 놓는다. `Master` 그룹은 `0 dB`로 두고, 게임의 전체 음량은 기존 옵션에서 조정한다. UI와 월드 효과음은 게임의 효과음 슬라이더를 공유한다.

| 믹서 그룹 | 노출 파라미터 | `SoundManager` 필드 |
| --- | --- | --- |
| `Music` | `MusicVolume` | `Music Output` |
| `SFX` | `SfxVolume` | `Sfx Output` |
| `UI` | `UiVolume` | `Ui Output` |

믹서 연결 전에도 테스트할 수 있다. 그룹을 비워 두면 각 `AudioSource`의 볼륨에 옵션 값이 반영된다. 그룹이 있는데 슬라이더를 바꿔도 믹서 수치가 움직이지 않으면 `Exposed Parameters`에서 이름을 확인한다.

`Initial Music`에 클립을 넣으면 매니저가 시작할 때 반복 재생한다. 프로젝트에서 배경음을 바꾸는 지점에서는 `PlayMusic(clip)`을 호출한다. 이미 재생 중인 클립을 다시 요청하면 재생 위치를 유지한다. `GameManager`가 유지되는 동안 배경음도 이어지므로 씬마다 배경음을 바꾸려면 해당 씬의 시작 로직에서 새 클립을 요청한다.

## 효과음 연결

1. Project 창에서 `Create > Audio > Sound Cue`를 선택한다.
2. `Clips`에 재생할 클립을 하나 이상 넣는다. 여러 개를 넣으면 재생마다 하나를 고른다. `Volume`은 클립 재생 음량이고, `Pitch Range`는 재생 속도와 음높이의 범위다. `(1, 1)`은 원본 피치다.
3. UI 버튼에는 `SoundEmitter`를 붙이고 `Cue`를 연결한다. `Button.onClick`에서 같은 오브젝트의 `SoundEmitter.PlayUi()`를 고른다. 월드 오브젝트의 UnityEvent나 애니메이션 이벤트에는 `SoundEmitter.PlaySfx()`를 연결할 수 있다. 애니메이션 이벤트를 쓰면 `Animator`와 같은 오브젝트에 `SoundEmitter`를 둔다.

타격 위치처럼 실행할 때 결정되는 좌표를 전달할 때는 코드에서 호출한다.

```csharp
using _Scripts.Audio;
using _Scripts._Core._ServiceLocator;

// 예: 기존 EnemyBase.DamageReceived 콜백 안에서 사용한다.
ServiceLocator.Get<ISoundManager>()?.PlaySfx(hitCue, hitPoint);

// 예: 씬의 배경음을 교체하는 함수 안에서 사용한다.
ServiceLocator.Get<ISoundManager>()?.PlayMusic(musicClip);
```

마지막 타격으로 체력이 0이 되면 `EnemyBase`는 `DamageReceived`와 `DeathStarted`를 둘 다 발행한다. 사망 타격에 사망음만 쓰려면 `DamageReceived` 콜백에서 `IsDead`가 `true`인 경우 일반 피격음을 건너뛴다.

`Sfx Count`와 `Ui Count`는 채널별 동시 재생 상한이다. 기본값은 각각 12개와 2개다. 모든 슬롯이 바쁘면 `Priority` 숫자가 작을수록 유지된다. 순위가 같은 효과음끼리는 가장 오래 재생한 슬롯을 새 효과음에 넘긴다. 월드 소리의 거리 감쇠는 `Sfx Spatial Blend`, `Sfx Min Distance`, `Sfx Max Distance`로 맞춘다.

## 플레이 테스트

1. `EnemyTestScene`의 `GameManager` 인스턴스와 카메라의 `AudioListener`를 사용한다. 우선 테스트 씬의 인스턴스에만 `SoundManager`, 클릭용 Cue, 짧은 `Initial Music`을 연결하고 Play Mode에 들어간다.
2. 배경음이 반복되고 버튼을 빠르게 눌렀을 때 클릭이 겹쳐 재생되는지 듣는다. 월드 효과음이 필요하면 애니메이션 이벤트나 타격 콜백 한 곳에 연결하고 카메라에서 서로 다른 거리에 있는 적으로 비교한다.
3. 옵션의 음악 음량을 `0`으로 내렸을 때 효과음이 들리는지, 효과음 음량을 `0`으로 내렸을 때 배경음이 들리는지 확인한다. 전체 음량을 `0`으로 내리면 모두 음소거된다.
4. 슬롯 교체를 확인하려면 `Sfx Count`를 `1`로 하고 긴 효과음 두 개에 다른 `Priority`를 준다. 먼저 낮은 우선순위 소리를 틀고 높은 우선순위 소리를 요청하면 교체된다. 반대 순서로 요청하면 높은 우선순위 소리가 유지된다.

## 사용한 C#과 Unity 문법

- `sealed`로 클래스의 상속을 막고, 호출부에는 `ISoundManager`의 재생 함수만 노출했다. `ServiceLocator.Get<ISoundManager>()`를 쓰면 호출부가 매니저 오브젝트의 위치를 알 필요가 없다.
- `[CreateAssetMenu]`와 `[SerializeField]`를 사용해 `ScriptableObject` 에셋에 클립·음량·피치를 지정한다. 여러 프리팹이 같은 Cue 에셋을 참조할 수 있어 소리 교체를 한 곳에서 할 수 있다. `Cue`는 이렇게 묶인 재생 설정을 뜻한다. C#의 `Queue<T>`는 먼저 들어온 항목을 먼저 꺼내는 자료구조다.
- C# 이벤트의 `+=`, `-=`로 `OnSettingsChanged`를 구독하고 해제한다. `Start`에서 구독하면 `GameManager`가 자식 매니저를 모두 초기화한 뒤 설정을 읽고 믹서에 반영할 수 있다. [Unity AudioMixer.SetFloat 호출 시점](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Audio.AudioMixer.SetFloat.html)
- `percent / (float)SettingsData.MaxPercent`로 정수 백분율을 0~1의 실수로 만든다. 믹서에는 `20 × log10(음량)`으로 환산한 dB를 적용한다. 음량 `0`에서는 로그 계산을 건너뛰고 `-80 dB`를 적용한다.
- `AudioSource`를 채널별 배열에 담아 재사용한다. 배열을 순회하면서 비어 있는 소스를 먼저 고르고, 슬롯이 꽉 찼을 때만 현재 요청의 우선순위와 먼저 재생한 순서를 비교한다.
