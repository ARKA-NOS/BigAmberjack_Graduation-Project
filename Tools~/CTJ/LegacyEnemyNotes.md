# 기존 적 에셋 위치

| 영역 | Legacy 폴더에 있는 항목 |
| --- | --- |
| `Assets/_Prefabs/Enemies/Legacy` | Enemy01~05, RobotEyeBoss, Catto, MadGhost의 적 프리팹과 관련 투사체 |
| `Assets/_Scripts/Agent/Enemies/Legacy` | 적별 스킬 SO와 스탯, 방패병과 눈 보스 구현, 방패 밀치기·돌진, 도약, 눈 보스 레이저·원형 탄막 스킬 |
| `Assets/_Graphics/Animations/Enemy/Legacy` | 기존 적용 애니메이션과 Animator Controller |
| `Assets/_Graphics/Assets/Enemy/Legacy` | 기존 캐릭터의 아처·좀비·방패병·눈 보스·Catto·MadGhost 그림 |
| `Assets/_Scripts/Test/Enemy/Legacy` | `BossTestControls` |

공용 `EnemyBase`, `BossBase`, FSM, 이동·전투 모듈과 공용 효과는 기존 위치에서 사용한다. 적 전용 조준선·경고 UI와 레이저 효과용 그림도 기존 위치에 있다.

Unity Editor의 `AssetDatabase.MoveAsset`으로 이동해 `.meta`의 GUID를 유지했다. Unity 씬과 프리팹은 이 GUID와 로컬 파일 ID를 통해 참조를 해석한다. `LoadAssetAtPath`로 여는 로컬 제작·검증 도구에는 새 경로를 반영했다.

Unity에서 이동한 에셋·폴더 133개의 GUID, 프리팹 12개와 씬·맵 에셋 9개의 의존성, 컴파일 오류 및 프리팹의 누락 컴포넌트와 참조를 확인했다.

직접 확인하려면 Unity Project 창에서 `Assets/_Prefabs/Enemies/Legacy`를 열어 프리팹을 선택한다. `EnemyTestScene`에는 기존 적 참조가 유지되므로 Inspector에서 기존 인스턴스의 원본 프리팹으로 이동해 Legacy 경로와 컴포넌트 연결을 확인할 수 있다. 게임에서 더 이상 스폰하지 않으려면 해당 맵이나 인카운터에서 프리팹 연결을 바꿔야 한다.
