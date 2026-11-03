# 눈깔 보스 공격 연출 수정

2026-11-01. Assets/CTJ 내부만 수정했습니다. BossPatternTest / CTJ_MapTestScene 파일은 수정하지 않았습니다.

## 현재 동작

1. **레이저 시선**: 실제 발사 중에는 눈동자가 레이저의 고정 방향을 봅니다. 플레이어를 쫓지 않으며, 발사가 끝나면 플레이어 추적으로 복귀합니다. 기존 5px 여백은 두 눈동자 모두를 고려해 유지합니다.
2. **붉은 눈 전환**: 3회 횡단 후 위에서 중앙으로 내려올 때, 베이스 아래쪽이 아레나 상단에 드러나는 높이부터 붉어집니다. 회색 Iris6 위의 RedIris(Iris1) 알파가 0→1로 올라가며 중앙 도착 시 완전히 붉어집니다. 시간 상수 대신 복귀 높이 진행률을 사용하므로 복귀 속도를 바꿔도 도착 시점과 맞습니다.
3. **일반 공격 경고**: 원형 채우기를 제거하고 실제 탄환 수와 방향에 맞춘 긴 막대 경고를 그립니다. 현재 12방향, 각 25유닛 경로, 두께 0.3유닛입니다. 시작/끝에는 탄환 반지름도 포함합니다. 준비가 시작될 때 발사 위치·수·속도·수명을 기억하여 경고와 실제 공격이 일치하도록 했습니다. 기존 준비 시간·점멸·유지 시간·발사 시 숨김은 유지됩니다.
4. **화염 탄환과 폭발**: Fire Effect and Bullet 16x16의 비행 5프레임을 반복합니다. 플레이어/벽/바닥 충돌 시 이동과 콜라이더를 즉시 끄고, 접촉 지점에서 팽창 후 흩어지는 폭발 6프레임을 한 번 재생한 뒤 제거합니다. 피해는 충돌 시 한 번만 주며 폭발은 추가 피해가 없는 표시입니다. 수명 만료나 보스 패턴의 강제 탄환 정리는 충돌 폭발을 만들지 않습니다.

일반 공격 경고는 **최대 비행 경로 전체**를 표시합니다. 지형을 맞으면 실제 탄환은 그 지점에서 멈춥니다. 경고 길이를 아레나 크기나 가까운 벽까지 임의로 줄이지 않습니다. 맵 벽 > 보스 > 경고의 기존 표시 순서를 유지합니다.

## 구성과 조정 위치

| 위치 | 설정 | 현재 값 / 역할 |
| --- | --- | --- |
| RobotEyeBoss → EyeVisualModule | Padding Pixels / Follow Smooth Time | 5px / 0.08초, 일반 시선 추적 |
| RobotEyeBoss → EyeVisualModule | Red Iris | Iris 자식의 붉은 렌더러. 기본 알파 0 |
| RobotEyeBoss → SkillModule → RadialVolley | Projectile Count | 12. 막대 수도 함께 변경 |
| RobotEyeProjectile → EnemyProjectile | Speed / Lifetime | 5 / 5초. 최대 경고 길이는 두 값의 곱 |
| RobotEyeProjectile → EnemyProjectileVisual | Flight Frames Per Second | 12 FPS. 비행 5프레임 반복 |
| RobotEyeProjectile → EnemyProjectileVisual | Impact Frames Per Second | 18 FPS. 6프레임이므로 약 0.33초 |
| RobotEyeProjectile → EnemyProjectileVisual | Impact Scale | 1.4. 충돌 후 그림만 키우는 배율 |
| RobotEyeProjectile → Visual | Local Scale | 3. 비행 그림 크기. 루트/콜라이더 크기는 유지 |

공용 EnemyProjectile에는 선택적인 EnemyProjectileVisual 호출만 추가했습니다. 애니메이션 컴포넌트를 붙이지 않은 MadGhost/Enemy02/Enemy04 탄환은 기존처럼 충돌 즉시 제거됩니다. 피해 판정은 EnemyProjectile, 프레임 재생은 EnemyProjectileVisual, 경고 그림은 RadialLanesGraphic, 눈동자 연출은 RobotEyeVisualModule이 맡습니다. 새 인터페이스/추상 클래스/패키지는 추가하지 않았습니다.

화염 원본 PNG와 기존 슬라이스 ID는 보존했습니다. 원본이 자동 슬라이스로 일부 이웃 그림이 합쳐져 있어서 필요한 프레임만 `CTJ_FireFlight_0~4`, `CTJ_FireImpact_0~5`라는 이름의 16×16 중앙 피벗 슬라이스로 추가했습니다. 픽셀 원본을 바꾸지 않고 Point/무압축/Full Rect 임포트 설정을 사용합니다. 폭발 프레임 순서는 시트 위쪽 행의 불꽃 팽창 2장, 다음 행의 파열/흩어짐 4장입니다.

## 직접 테스트

1. BossPatternTest를 열고 Play 합니다. 보스가 내려온 뒤 일반 공격까지 기다립니다. 탄환 수만큼 긴 막대가 모든 방향으로 나타나고, 마지막 경고가 끝난 뒤 동일한 방향으로 발사되는지 확인합니다.
2. 경고가 시작된 뒤 타깃을 움직여도 경고의 발사 원점과 방향은 바뀌지 않아야 합니다. 경고 사이로 피하면 맞지 않고, 경고 위에 있으면 해당 탄환에 맞아야 합니다.
3. 탄환을 보며 불꽃이 반복되는지 확인합니다. 타깃·바닥·벽에 닿으면 접촉 위치에서 터지고, 폭발은 이동하지 않으며 약 0.33초 후 사라져야 합니다. 타깃의 Hit Count는 폭발 프레임마다 증가하면 안 됩니다.
4. 테스트 UI의 Deal 1500 damage 버튼으로 레이저 페이즈를 시작합니다. 발사 중 반대쪽으로 움직여도 눈동자가 레이저 방향을 바라보는지 확인합니다.
5. 3회 횡단 후 위에서 내려오는 눈이 드러날 때 회색에서 붉은색으로 점진적으로 변하고, 중앙 도착 시 완전히 붉은지 확인합니다. 이후 일반 공격에서도 붉은색을 유지하며 플레이어를 다시 따라봅니다.
6. 필요하면 Play를 종료하고 위 표의 프리팹 설정을 바꿔 재실행합니다. 다른 적의 탄환도 한 번 확인해 기존 충돌/피해가 유지되는지 봅니다.

검증 결과: 컴파일/프리팹 참조 검사 통과. 실제 레이저 3회 중 시선 고정과 복귀 중 중간 알파값을 확인했습니다. 실제 Canvas 메시 12개 막대와 실제 탄환 12개의 방향·거리·고정 원점이 일치했습니다. 타깃(다중 콜라이더) 피격 1회, 얇은 벽/바닥 고속 충돌, 벽 내부 생성, 폭발 6프레임과 이동 중단, 수명 만료를 확인했습니다. 기존 Enemy02 전개·발사·지형 차단·발사 후 소유 적 제거 테스트도 통과했습니다. 검증 중 Error/Assert/Exception은 0건입니다.

검증 도구: VerifyRobotEyeVisual.RunPlay / VerifyRobotEyeAttack.RunEdit·RunPlay. 기존 독립 씬 절차로 실행하며, 렌더링 캡처는 확인 후 정리했습니다.

## 사용한 문법과 이유

- **상태 읽기와 로컬 벡터 변환**: BossLaserSkill.IsFiring / BeamDirection을 읽어 실제 레이저와 눈동자 방향을 맞춥니다. InverseTransformVector는 위치가 아닌 방향을 변환하므로 보스의 이동 위치가 섞이지 않습니다.
- **Mathf.InverseLerp / SmoothStep / Max**: 복귀 높이를 0~1로 바꾸고 부드러운 증가 곡선을 적용합니다. Max는 이미 붉어진 정도가 순간적인 위치 변화로 되돌아가지 않도록 합니다.
- **Color 구조체 수정**: `Color color = redIris.color; color.a = RedBlend; redIris.color = color;`처럼 값을 복사·수정·재할당합니다. RGB는 피격 효과가 관리하고 이 모듈은 알파를 관리합니다. 회색 눈 위에 붉은 눈을 겹치므로 중간 단계에도 눈동자 모양이 남습니다.
- **MaskableGraphic.OnPopulateMesh / VertexHelper**: 기본 Canvas UI 안에 막대 하나당 꼭짓점 4개, 삼각형 2개를 만듭니다. 탄환 12개를 위한 Canvas 12개를 생성할 필요 없이 하나의 경고에서 알파와 점멸을 함께 관리합니다.
- **Sin / Cos와 준비 시 값 저장**: `2π × i / count`로 각 탄환의 방향을 구합니다. 준비 시점의 count, Speed, Lifetime을 보관해 도중 설정이 바뀌어도 이미 보여 준 경고와 실제 발사가 달라지지 않도록 합니다.
- **Sprite 배열과 나머지 연산(%)**: 비행은 경과 시간 × FPS의 정수 부분을 프레임 수로 나눈 나머지로 반복합니다. 폭발은 마지막 인덱스에서 제한하고 마지막 프레임 표시 시간까지 기다립니다.
- **선택 컴포넌트와 지연 Destroy**: EnemyProjectileVisual이 있을 때만 충돌 애니메이션 시간을 반환받아 Destroy를 지연합니다. `_consumed`, collider.enabled=false, body.simulated=false를 먼저 적용하여 오브젝트가 잠시 남아 있어도 이동/피해 판정이 다시 실행되지 않도록 합니다.
