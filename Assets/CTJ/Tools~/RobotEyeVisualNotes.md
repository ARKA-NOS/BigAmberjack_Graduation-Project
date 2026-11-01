# 눈깔 보스 외형과 시선 추적

2026-11-01. 수정 범위는 Assets/CTJ입니다. 사용자 씬과 다른 동료의 코드는 수정하지 않았습니다.

## 적용 내용

`Prototypes/Bosses/RobotEyeBoss.prefab`의 임시 원형 파츠 3개를 다음 구조로 교체했습니다.

```text
RobotEyeBoss
├─ Visual
│  ├─ EyeGlobe          ← Eyes_V2/EyeGlobe_sprite_1
│  │  └─ Iris           ← 기본 Iris6
│  │     └─ RedIris     ← Iris1을 알파로 겹치는 표시
│  └─ FirePoint         ← 기존 발사 위치 유지
└─ EyeVisualModule      ← RobotEyeVisualModule
```

기본 눈동자는 회색 Iris6입니다. **레이저 3회 횡단을 마치고 위에서 중앙으로 내려올 때**, 눈 베이스의 아래쪽이 아레나 위쪽에 드러나는 구간부터 붉은 Iris1의 알파를 올립니다. 중앙에 도착하면 완전히 붉어지고 이후 일반 패턴에서도 유지됩니다. 레이저 준비/발사 중에는 Iris6을 유지합니다. 종료 여부인 `HasFinishedLaserPhase`는 보스가 제공하고, 실제 알파/시선 처리는 표시 모듈이 담당합니다. 보스의 공격 속도·이동 속도·피해 수치는 변경하지 않았습니다.

레이저가 실제로 발사되는 동안은 눈동자를 `BossLaserSkill.BeamDirection`으로 즉시 고정합니다. 이때 플레이어가 이동해도 시선이 흔들리지 않습니다. 레이저 발사가 끝나면 기존의 부드러운 플레이어 추적으로 돌아갑니다.

베이스 높이는 기존 2.2 월드 유닛을 유지합니다. 새 스프라이트는 43×44px, PPU 100이므로 EyeGlobe 스케일은 5입니다. 두 눈동자는 모두 21×21px이며 베이스와 같은 픽셀 비율을 사용합니다. 충돌 영역과 발사 위치는 그대로입니다.

세 PNG의 원본 픽셀은 수정하지 않았습니다. Unity 임포트 설정만 Point 필터/무압축/Full Rect로 맞춰 픽셀 외형과 기존 아웃라인이 유지되게 했습니다. 원래 스프라이트 ID와 슬라이스는 보존했습니다. 피격 점멸은 베이스와 눈동자 모두에, 아웃라인은 베이스에 연결했습니다.

## 인스펙터 조절

보스 자식 **EyeVisualModule → Robot Eye Visual Module**에서 설정합니다.

| 항목 | 현재 값 | 설명 |
| --- | --- | --- |
| Globe | EyeGlobe의 SpriteRenderer | 눈 베이스와 이동 범위 기준 |
| Iris | EyeGlobe/Iris의 SpriteRenderer | 움직일 눈동자. 베이스 바로 아래에 배치 |
| Red Iris | Iris/RedIris의 SpriteRenderer | 기본 눈동자 위에 알파로 겹칠 붉은 눈동자 |
| Normal Iris | Iris6 | 기본 눈동자 |
| After Laser Iris | Iris1 | 레이저 페이즈 종료 후 유지할 눈동자 |
| Padding Pixels | 5 | 베이스 원본 픽셀 기준 안쪽 여백. 화면 픽셀 기준이 아님 |
| Follow Smooth Time | 0.08초 | 시선이 목표로 부드럽게 이동하는 정도. 클수록 느긋하고 0이면 즉시 이동 |

눈동자 중심만 제한하는 것이 아니라 **눈동자의 반지름까지 빼고** 이동 반경을 계산합니다. 현재는 작은 쪽 베이스 반지름 21.5px − 눈동자 반지름 10.5px − 여백 5px = **중심 이동 반경 최대 6px**입니다. 원형 스프라이트용 계산이므로 대각선에서도 눈동자가 밖으로 튀어나가지 않습니다. 보간 이후에도 한 번 더 제한합니다.

플레이어가 없거나 비활성 상태/사망 상태라면 눈동자는 중앙으로 돌아옵니다. 나중에 Target이 지정되면 다시 추적합니다. 베이스의 로컬 공간을 사용하므로 보스가 이동하거나 루트 크기를 바꿔도 원본 픽셀 기준 여백은 유지합니다. 다른 눈동자를 넣을 때는 베이스 안에 들어갈 크기의 원형 그림을 사용하고 Iris의 로컬 회전은 0을 유지하세요.

## 직접 테스트

1. 저장된 작업 씬을 보존하고 `Prototypes/Bosses/BossPatternTest.unity`를 열어 Play 합니다. 이 씬은 기존 보스 프리팹을 참조하므로 새 외형을 사용합니다.
2. A/D 또는 방향키로 테스트 타깃을 좌우 이동하고 Space로 점프합니다. 눈동자가 타깃 위치를 부드럽게 따라가며 베이스 가장자리까지 닿지 않는지 확인합니다.
3. 보스 자식 EyeVisualModule의 Padding Pixels를 5에서 8로 바꾸면 눈동자가 움직일 수 있는 범위가 줄어듭니다. Follow Smooth Time을 늘리면 반응이 느려집니다. 영구 설정은 Play 종료 후 프리팹에서 바꿉니다.
4. 테스트 UI의 **Deal 1500 damage (2500 -> 1000)** 버튼으로 레이저 페이즈를 시작합니다. 준비·발사·횡단 동안 회색 Iris6이 유지되는지 봅니다. 실제 레이저가 나오는 동안 반대쪽으로 이동해도 눈동자는 빔 방향을 보아야 합니다.
5. 세 번 횡단하고 위에서 중앙으로 내려오는 동안 서서히 붉어지는지 확인합니다. 중앙 도착 후에는 완전히 붉으며, 이후 일반 패턴과 타깃을 잠시 잃은 상황에서도 유지해야 합니다.
6. 실제 플레이어 검증은 CTJ_MapTestScene에 RobotEyeBoss 프리팹을 배치하고 기존 타깃 연결 방법을 사용합니다. 보스의 EyeVisualModule을 끄면 시선/눈동자 교체만 멈추며 공격 패턴은 그대로 동작합니다.

이번 자동 확인: Unity 컴파일 오류 0건, 프리팹 Missing Script/깨진 참조 없음. 독립 Play 씬에서 8방향 시선 추적, 매 프레임 여백 유지, 스케일 변경, 여백 조정, 타깃 부재/재연결, 실제 레이저 3회와 중앙 복귀 후 Iris1 전환을 통과했습니다. 실행 중 Error/Assert/Exception은 0건이었고 전후 렌더링도 확인했습니다. 검증 캡처는 확인 후 제거했으며 CTJ_MapTestScene 파일 해시는 작업 전후 동일합니다.

현재 검증 도구는 `VerifyRobotEyeVisual.RunEdit / RunPlay`입니다. RunPlay는 기존 VerifyEnemy01의 BeginPlay → 검사 → StopPlay → RestoreScene 순서로 만드는 독립 임시 씬에서만 실행합니다.

## 사용한 문법과 이유

- **기존 Module 상속**: 공용 ModuleOwner의 검색/초기화에 맞춥니다. 외형 처리를 보스 FSM과 분리하되, 현재 필요한 RobotEyeBoss 전용 모듈로 구성했습니다. 새 인터페이스나 추상 클래스는 추가하지 않았습니다.
- **읽기 전용 공개 프로퍼티**: `HasFinishedLaserPhase { get; private set; }`는 보스만 변경하고 외형 모듈은 읽습니다. 기존 HasUsedLaserPhase는 시작 즉시 true가 되므로, 종료 후의 외형 변경에는 별도 종료 상태가 필요합니다.
- **LateUpdate**: 보스의 이동과 상태 갱신 이후 눈동자 위치를 계산합니다. 공격 방향이나 실제 발사 위치는 건드리지 않고 시선만 따라가게 합니다.
- **InverseTransformVector와 알파 겹치기**: 레이저의 월드 방향은 베이스의 로컬 방향으로 바꿔 시선을 고정합니다. 복귀 높이는 InverseLerp로 0~1 진행률을 구하고 SmoothStep으로 완화하여 RedIris의 알파에 적용합니다. 기본 눈동자는 불투명하게 유지하므로 전환 중 눈동자 전체가 투명해지지 않습니다.
- **Transform.InverseTransformPoint**: 월드에 있는 타깃 위치를 눈 베이스 기준 로컬 좌표로 변환합니다. 스케일이 5인 베이스에서도 픽셀→로컬 단위 계산이 일관됩니다.
- **Vector2.ClampMagnitude**: 벡터 길이만 제한해 원형 이동 범위에 가둡니다. X/Y를 각각 Clamp하는 사각 범위와 달리 대각선으로 이동할 때 원의 모서리 밖으로 나가지 않습니다.
- **Vector2.SmoothDamp와 ref**: 현재 위치가 목표를 부드럽게 따라가도록 보간합니다. `ref _velocity`는 함수가 계산한 속도를 다음 프레임까지 저장하는 용도입니다. 순간 목표 전환 후에도 최종 위치는 허용 반경으로 제한합니다.
- **Sprite.bounds / pixelsPerUnit**: 스프라이트의 로컬 크기를 읽고 픽셀 여백을 같은 단위로 바꿉니다. 피벗이 중심에서 달라도 bounds.center를 반영해 눈동자 그림의 중심을 맞춥니다. 런타임 텍스처 읽기나 Read/Write 설정은 필요 없습니다.
- **[SerializeField], [Min], [Tooltip]**: 필드는 private로 유지하면서 인스펙터에서 참조·여백·보간 시간을 설정하고, 값의 의미를 바로 확인할 수 있게 했습니다.
