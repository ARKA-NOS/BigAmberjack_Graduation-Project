# Enemy23 발악 진입·착지 그로기

2026-11-25. 기존 35% 진입 시 공중 변환 → 편도 레이저를 아래 흐름으로 교체했다. 강화 이후의 일반 패턴은 유지한다.

후속 조정의 스파크·그로기 자세·회복·중앙 복귀 설정은 [Boss23RecoveryNotes.md](Boss23RecoveryNotes.md)를 따른다.

## 진행 순서

1. HP가 최대 HP의 35% 이하가 되면 현재 공격을 정리하고, 발악 진입을 한 번 실행한다.
2. 실제 무적은 발악 진입 시 켜지고 착지 순간에 해제된다. `BossBase.ApplyDamage`가 피해를 차단하고, 기존 보스 체력바가 흰 테두리와 `무적` 표시를 보여준다. 보호막에 별도의 문구는 없다.
3. `SkillModule/LastStandEntry`의 전용 스킬로 떠오른다. 일반 공중 전환의 `Rise` 변신 애니메이션은 사용하지 않는다. **공중 높이에 도착해 멈춘 다음** 보라색 원판 → 안쪽 하늘색 호 → 바깥쪽 하늘색 호를 순차 페이드로 생성한다. 기본 총 생성 시간은 0.88초다. 생성 중에는 이동·공격·비주얼 호버링이 없고, 완성된 뒤 공격을 시작한다. 두 호는 완전한 원이 아닌 250도 곡선으로 각각 일정 속도로 회전한다.
4. **폭격 → 3방향 조준 산탄 → 폭격 → 산탄 → 폭격 → 산탄 → 폭격** 순서로 실행한다. 산탄은 3회·총 9발, 전체 확산각은 70도다. 각 사격 때 플레이어 중심을 다시 조준한다.
5. 폭격은 기존 `DroneBombSkill`의 포물선·지면 경고를 재사용한다. 4발 모두 강화 피해 26·비행 약 1.15초, 기존과 같은 반경 1.875유닛이다. 경고가 뜬 착탄점은 이후 플레이어가 이동해도 바뀌지 않는다.
6. 마지막 폭탄의 비행과 모션이 끝나면 아레나 중앙의 공중 공격 위치로 이동한다. 기존 강화 사선 레이저를 실행한다. 피해 28, 두 차례 훑으며 중간 재경고는 없다.
7. 레이저가 끝나면 보호막과 무적을 **낙하 중에도 유지**한다. 정지 상태에서 낙하 가속을 시작해 바닥으로 떨어진다.
8. 바닥 높이에 도착하면 무적을 해제한다. 보호막은 0.28초 동안 빠른 불규칙 점멸과 호의 각도 흔들림으로 지직거리며 사라진다. 비주얼 위치를 물리 위치에 맞추고 큰 착지 Impulse를 발생시킨다. **착지 시점부터 정확히 5초간** 멈추고 노랑·주황 스파크를 낸다. 이때 입력 피해량을 2배로 전달한다. 공용 방어·피해 감소 계산은 그대로 적용한다.
9. 그로기가 끝나면 스파크와 피해 배율을 해제한다. 약 0.655초 동안 일어선 뒤 0.65초 쉬고 기존 강화 무작위 패턴으로 돌아간다. 타깃 변경·해제 등으로 진입이 중단되면 무적과 보호막도 함께 정리한다.

## 조절 위치

프리팹: `Assets/_Prefabs/Enemies/Bosses/Enemy23/Boss23_ProductionControlDrone.prefab`.

| 오브젝트 / 컴포넌트 | 항목 | 기본값 / 의미 |
| --- | --- | --- |
| 루트 / ProductionControlDrone | Last Stand Health Ratio | 0.35, 진입 HP 비율 |
| 루트 / ProductionControlDrone | Air Attack Altitude | 바닥 기준 4.5유닛, 마지막 레이저를 실행할 중앙 높이 |
| 루트 / ProductionControlDrone | Entry Fall Speed | 최고 낙하 속도 20유닛/초 |
| 루트 / ProductionControlDrone | Entry Fall Acceleration | 낙하 가속도 32유닛/초². 낮추면 낙하 시작이 더 완만해짐 |
| 루트 / ProductionControlDrone | Groggy Duration / Damage Multiplier | 5초 / 2배 |
| SkillModule/LastStandEntry | Rise Speed / Return Speed | 상승 10 / 중앙 복귀 8유닛/초 |
| SkillModule/LastStandEntry | Action Interval | 행동 시작 간 최소 간격 0.65초. 현재 모션도 끝나야 다음 행동으로 진행 |
| SkillModule/LastStandEntry | Shotgun Angle / Shot Damage | 전체 확산각 70도 / 탄당 피해 10 |
| SkillModule/LastStandEntry | Projectile Speed / Range | 12유닛/초 / 20유닛 |
| SkillModule/TargetBomb | Last Stand Damage / Warning Duration | 진입 폭격과 일반 강화 폭격이 함께 사용하는 피해·착탄 시간 |
| LastStandFeedbackModule | Landing Shake Strength / Duration / Radius | 1.1 / 0.45초 / 20유닛 |
| LastStandFeedbackModule | Center Fade Duration / Arc Fade Duration / Layer Delay | 원판 0.28초 / 호 각각 0.22초 / 각 단계 사이 0.08초 |
| LastStandFeedbackModule | Inner Rotation Speed / Outer Rotation Speed | 초당 +42도 / -32도. 부호로 회전 방향 변경 |
| LastStandFeedbackModule | Shutdown Duration / Flicker Frequency | 해제 연출 0.28초 / 점멸 주파수 기준 36 |
| Visual/InvulnerableShield/CircleAndRings | Shield Color / Ring Color / Line Width | 원판·선 색상과 선 두께 |
| Visual/InvulnerableShield/CircleAndRings | Inner Arc Degrees / Outer Arc Degrees | 각각 250도, 호가 차지하는 각도 |
| Visual/InvulnerableShield | RectTransform 크기 | 보호막 전체 크기. 기본 280 × 280, 로컬 스케일 0.01 |
| Visual/HitVisual/ProcessNumber/GroggySparks | ParticleSystem | 번호 주변 뒤편 발광 스파크. Start Size 0.09~0.20, 수명 0.35~0.65초, 초당 110개. 자세한 설정은 후속 조정 문서 참고 |

착지 흔들림은 기존 보스 레이저와 같은 **Impulse 채널 64**를 쓴다. 카메라를 새로 연결하는 경우 기존 Listener가 해당 채널을 수신하고 `Use 2D Distance`가 켜져 있어야 한다. 이번 작업은 카메라·씬 파일을 변경하지 않았다.

## 기절 스프라이트 확인

`Assets/_Graphics/Assets/Enemy/Enemy23/SHADOW Series - Voidrone Boss`의 PNG와 원본 Aseprite 태그를 확인했다. `idle`, `hit`, `death`, 지상·비행·웅크림·각 공격 태그가 있고, 전용 `stun`/`groggy` 태그나 기절 PNG는 없다. `normal state/hit.png`는 피격 섬광 한 프레임이므로 5초 기절 자세로 사용하지 않았다. 후속 조정에서 가장 낮게 웅크린 `transition_4`를 그로기 유지 자세로 사용하고, 웅크림 역순 모션으로 회복하도록 연결했다.

## 직접 테스트

1. `EnemyTestScene`을 연다. Enemy23이 없다면 위 프리팹을 바닥이 있는 위치에 배치하고, 루트의 `Target`에 플레이어 Transform을 연결한다. 기존 배치를 쓴다면 새 `LastStandEntry` 연결을 가리는 프리팹 오버라이드가 없는지 확인한 뒤 Play Mode에 진입한다. 자동 검증은 임시 배치로 실행했으며 저장된 씬 파일은 변경하지 않았다.
2. 플레이어 공격으로 Enemy23의 HP를 35% 이하까지 낮춘다. 체력바에 `무적`이 표시되고 HP가 더 줄지 않는지 확인한다. 상승 중에는 보호막이 보이지 않아야 한다.
3. 상승 중 `Rise` 변신을 사용하지 않는지 확인한다. 공중 높이에 도착하면 멈춘 상태에서 원판과 두 호가 순서대로 부드럽게 나타나야 한다. 완성되기 전에는 공격하지 않으며, 완성 후 공중에서 산탄 3회와 폭격 4회가 교대로 발생해야 한다. 폭격 경고가 뜬 뒤 이동해도 원래 위치에 착탄해야 한다.
4. 두 호가 틈을 남긴 채 일정하게 회전하고, 중앙으로 복귀해 강화 레이저가 두 번 훑는지 확인한다. 레이저 종료 후에는 보호막·무적 표시를 유지하며 처음엔 느리다가 빠르게 낙하해야 한다. 낙하 중 공격해도 HP가 줄면 안 된다.
5. 바닥에 닿는 순간 화면이 크게 흔들리고 보호막은 지직거리며 꺼져야 한다. 무적 표시는 착지 때 해제되고, 커진 노랑·주황 스파크가 나와야 한다. 착지부터 5초간 멈추며 같은 공격의 HP 감소량이 평소의 2배인지 비교한다.
6. 5초 뒤 스파크가 꺼지고 일어서는 모션과 짧은 휴식을 거친 뒤 강화 패턴이 재개되는지, 이후 HP가 계속 낮아도 진입 패턴이 반복되지 않는지 확인한다.

## 실행 검증

Unity에서 컴파일 오류 0을 확인했고, EnemyTestScene의 독립 실행 배치에서 검증했다. 테스트 종료 후 임시 오브젝트를 정리하며 씬 파일은 저장하지 않는다.

- 전용 진입: 9발·폭격 4발·강화 레이저 2회, 보호막 및 체력바 무적 표시, 35% 경계, 고정 착탄점 확인.
- 보호막: 원판·안쪽 호·바깥쪽 호 순서로 부분 알파 값이 관측됐고 약 0.90초 후 완성됐다. 생성 중 이동·공격이 없고, 안쪽 호가 초당 약 42도로 회전하는 것을 확인했다. 착지 해제 시 급격한 알파 변화 7회와 최종 비활성화를 관측했다.
- 그로기: 약 5.00초. 입력 피해 10으로 낙하 중 HP 감소 0, 일반 상태 9.259262, 그로기 상태 18.518524를 확인했다. 차이는 기존 공용 방어 계산이며 정확히 2배다.
- 낙하: 0에서 가속해 착지 직전 약 14.72유닛/초를 관측했다. 설정 최고 속도 20은 짧은 낙하 거리 때문에 도달하지 않을 수 있다.
- 착지 Impulse: 채널 64에서 약 1.09의 최대 변위 신호를 확인했다. 노랑·주황 입자 모두 생성된다.
- 기존 지상 패턴 8개와 공중 패턴 7개는 앞선 구현의 회귀 검증을 통과했다. 이번 수정에서는 전용 진입 전체 흐름과 중단 정리, 순차 페이드·호 회전·해제 점멸·낙하 무적을 다시 검증했다.
- 자동 검증 진입점은 `VerifyBoss23Rework.CheckAssets`, `RunGround`, `RunAir`, `RunEntryOnly`다. 상세 진입 결과는 `Boss23LastStandVerification.json`에 기록한다.

## 문법과 구현 이유

- **`enum`과 `switch`**: 기존 보스 FSM에 진입 공격·레이저·낙하·그로기 상태를 추가했다. 공중에 있을 때 그로기 타이머가 시작되는 것을 상태 전이로 방지한다.
- **상속·`override`**: `DroneLastStandEntrySkill`은 기존 `EnemyAttackSkill`을 상속한다. 고유 SkillSO와 여섯 번째 지정 실행 슬롯을 연결해 공용 스킬 등록·실행·정리 과정을 사용한다.
- **`Mathf.MoveTowards`와 `Time.fixedDeltaTime`**: 고정 물리 틱마다 낙하 속도를 증가시킨다. 최고 속도를 넘지 않고, 착지 위치도 지나치지 않게 이동한다. 착지 순간의 시각적 지연은 `SnapToBody`로 제거한다.
- **`List<EnemyLobProjectile>`**: 마지막으로 발사한 폭탄까지 비행이 끝났는지 확인해, 아직 떨어지는 폭탄이 있는데 레이저로 넘어가는 일을 방지한다.
- **공용 메서드 재사용**: 기존 폭격 스킬의 단일 발사 메서드를 분리했다. 진입 공격에서도 같은 지형 검사·포물선·경고·공격음을 사용한다. 폭탄은 발사한 스킬이 끝나도 자체 수명으로 동작한다.
- **`MaskableGraphic.OnPopulateMesh`와 `VertexHelper`**: 새 텍스처 없이 Canvas UI의 원판과 두 호를 그린다. `CanvasRenderer`를 필수 컴포넌트로 지정해 실제 메시가 표시되도록 한다.
- **`Mathf.SmoothStep`과 단계별 알파**: 원판·안쪽 호·바깥쪽 호의 시작 시간을 달리해 생성 순서를 만든다. 마지막 호까지 알파가 1이 된 뒤 `IsShieldReady`를 통해 스킬에 공격 시작을 허용한다. 공격 시간을 임의로 연장하는 공용 시스템은 추가하지 않았다.
- **`Mathf.Repeat`와 각도 적분**: `회전 속도 × Time.deltaTime`을 누적해 일정 속도로 돌리고 360도 안으로 정리한다. 호는 250도만 그려 틈을 남긴다. 해제 때만 복합 사인파로 알파와 각도를 빠르게 흔든다.
- **ParticleSystem Gradient / RandomColor**: 노랑 영역을 넓게, 주황 영역을 좁게 둔 색상 그라데이션에서 무작위로 시작 색을 고른다. 짧은 수명·중력·속도에 따른 가는 Stretch로 스파크를 표현한다.
- **Cinemachine Impulse**: 보스 착지 모듈이 신호만 발생시키고 카메라는 기존 Listener로 수신한다. 카메라 추적 코드를 직접 수정하지 않는다.
