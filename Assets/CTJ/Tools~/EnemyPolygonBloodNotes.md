# 단일 다각형 혈흔 파티클

2026-11-03. 변경은 `Assets/CTJ` 내부에 한정했습니다. 패키지 설치나 팀원 코드/씬 수정은 없습니다. 이 문서는 이전 단일 시트 및 혈흔 5종 무작위 방식보다 우선하는 현재 설정입니다.

## 현재 동작

- 적 6종의 ParticleModule을 **HitBloodSpray 하나**로 통일했습니다. 기존 임팩트/슬래시/혈흔 시트는 현재 피격 목록에서 연결을 해제했습니다. 원본 에셋과 이전 프리팹은 보존했습니다.
- 이미지 시트 없이 직접 만든 불규칙한 5~7각형 메시 4개를 입자 모양으로 사용합니다. 입자마다 모양·회전·크기·붉은 색조·발사 속도가 달라집니다.
- 한 타격당 14~20개의 입자를 105도 부채꼴로 분사합니다. 공격자가 전달되면 공격자가 있는 쪽의 반대편, 공격자 정보가 없으면 기존 타격 진행 방향으로 퍼집니다. 좌우뿐 아니라 대각선 타격도 처리합니다.
- 중력으로 포물선을 그리며 떨어집니다. 기본 속도는 2.8~5.2유닛/초, 위쪽 초기 속도는 1.2, 중력 배율은 1.4입니다. 높이 1.2유닛에서의 검증에서는 대략 수 유닛 안쪽으로 흩어졌습니다. 높은 위치에서 떨어지면 더 멀리 갑니다.
- 바닥에서는 얇게 눌린 다각형 흔적으로 멈춥니다. 벽에서는 표면에 붙어 초당 0.22유닛씩 아래로 흐릅니다. 벽 끝에서는 다시 떨어지고, 벽에서 바닥으로 내려오면 멈춥니다.
- **첫 접촉 후 입자별로 2~3초 유지하고, 그다음 0.55초 동안 알파를 낮춰 제거합니다.** 벽에서 바닥으로 넘어가도 유지 시간을 다시 세지 않습니다.
- 바닥을 못 찾으면 기본 2초 낙하 후 페이드로 정리합니다. 공중 페이드 도중 늦게 착지해도 흔적 시간이 잘리지 않도록 수명 상한에 여유를 두었습니다.

기존 마지막 피격의 넉백·블링크 후 사망 순서는 유지합니다. 적이 제거되어도 이미 발생한 혈흔은 자기 수명대로 남습니다. 씬을 나가면 함께 정리됩니다.

## 구조

```text
Enemy / ParticleModule (EnemyParticleModule)
  └─ Hit Effects: HitBloodSpray 1개

HitBloodSpray(Clone) — 적과 분리된 같은 씬의 오브젝트
  ├─ ParticleSystem / ParticleSystemRenderer: 다각형 입자 표시
  ├─ EnemyBloodParticle: 분사·중력·2D 충돌·벽 이동·흔적 시간·알파
  └─ EnemyHitParticle: 모든 입자가 끝나면 오브젝트 정리
```

입자마다 GameObject나 Rigidbody를 만들지 않습니다. 하나의 ParticleSystem과 배열로 상태를 관리합니다. 입자 한 번 생성 수는 최대 64개로 제한하며, 현재 설정은 14~20개입니다. 적별 동시에 남겨둘 피격 효과는 24묶음입니다. 이를 넘는 연타에서는 오래된 묶음부터 정리합니다.

Ground / Platform의 실제 Collider2D를 검사합니다. Trigger와 Agent(플레이어/적) 콜라이더는 제외합니다. PlatformEffector2D의 일방향 발판은 아래에서 통과하고 위에서 내려올 때 착지합니다. 배경 그림에 Collider2D가 없으면 혈흔도 붙지 않습니다.

## Inspector 조정 위치

공통 설정은 `Assets/CTJ/Prototypes/Particles/HitBloodSpray.prefab`의 **EnemyBloodParticle**입니다.

| 항목 | 현재 값 / 의미 |
| --- | --- |
| Particle Count | 14~20, 한 번에 나오는 개수 |
| Speed Range | 2.8~5.2, 분사 속도 및 거리 |
| Spread Angle | 105도, 산탄처럼 퍼지는 전체 각도 |
| Upward Speed | 1.2, 추가 위쪽 속도 |
| Size Range | 0.09~0.22, 입자 크기 |
| Gravity Scale | 1.4, 중력 세기 |
| Dark Color / Bright Color | 입자별 무작위 색 범위 |
| Terrain Layers | Ground + Platform |
| Trace Duration | 2~3초, 표면 접촉 후 유지 시간 |
| Fade Duration | 0.55초, 유지 이후 사라지는 시간 |
| Wall Slide Speed | 0.22유닛/초, 벽에서 내려오는 속도 |
| Max Air Time | 2초, 지형을 못 찾았을 때 페이드 시작 시각 |

특정 적만 크기를 바꾸려면 적의 `ParticleModule → Hit Effects → Polygon Blood Spray → Scale`을 조정합니다. 이는 입자 크기에 적용되며 비행 속도는 월드 단위 공통값입니다. 보스의 기존 전체 Effect Scale 1.4도 유지했습니다.

분사와 접촉 이후 수명을 직접 관리하므로 ParticleSystem 기본 Emission, Gravity Modifier, Color over Lifetime을 추가로 켜지 마세요. 위 컴포넌트에서 조절하면 물리 판정과 표시가 같이 맞춰집니다. Unity의 기본 ParticleSystem 미리보기 버튼 대신 아래의 실제 피격 테스트를 사용하세요.

## 직접 테스트

1. 저장한 테스트 씬의 평평한 곳에서 적을 좌우로 공격합니다. 피가 공격자가 있는 쪽의 반대로 퍼지고 아래로 휘어 떨어지는지 확인합니다. 적 루트의 `Test → Take 2 Damage (Play Mode)` 메뉴도 사용할 수 있습니다.
2. 바닥에 떨어진 흔적이 멈춘 후 2~3초 유지되고, 갑자기 꺼지지 않고 약 0.55초 동안 흐려지는지 봅니다. 입자마다 접촉 시점이 달라 동시에 전부 없어지지는 않습니다.
3. 적 뒤에 벽을 두고 벽 쪽으로 공격합니다. 붙은 피가 천천히 내려오고, 바닥까지 내려온 피는 그곳에서 멈추는지 확인합니다.
4. 마지막 타격으로 적을 죽입니다. 적의 사망 연출과 별개로 혈흔이 남았다가 정리되는지 확인합니다.
5. Platform 위에서도 확인합니다. 위에서 내려오는 피는 닿고, 아래에서 올라가는 피는 통과해야 합니다.
6. 설정은 Play 종료 후 프리팹에 적용해야 저장됩니다. 표면 흔적을 끄려면 ParticleModule의 해당 효과 Enabled를 끄면 됩니다.

## 검증 결과

- Unity C# 및 셰이더 컴파일, 메시 4종·머티리얼·적 6종 참조 검사.
- 넓은 좌우 분사, 중력 낙하, 바닥 정지, 2~3초 접촉 유지와 점진적 알파 감소.
- 벽에서 0.5초 동안 약 0.11유닛 이동, 벽 접촉 유지 시 타이머가 계속 초기화되지 않음.
- 벽→바닥 전환, 일방향 발판, 빠른 입자와 얇은 벽, Trigger 무시, 지형 없는 곳의 정리.
- 적 6종 모두 한 타격에 단일 혈흔 묶음, 마지막 타격과 적 제거 후 혈흔 유지/정리.
- 공격자 기준 반대 방향, 모듈 비활성화, 24묶음 상한, 씬 언로드 정리.
- 실제 URP 카메라 캡처 확인: `EnemyBloodSprayPreview.png`의 왼쪽은 공중, 가운데는 바닥, 오른쪽은 벽 흔적입니다.

검증 도구는 `VerifyEnemyBloodSpray.RunEdit / RunPlay / RenderPreview`입니다. `ConfigureEnemyBloodSpray.Apply`는 제작 초기값을 다시 쓰므로 직접 조절한 뒤에는 검증 도구만 실행하세요. 이전 ConfigureEnemyParticles 및 VerifyEnemyParticles의 혈흔 개수/수명 검사는 이전 시트 방식의 기록입니다.

## 사용 문법과 이유 — 학습용

### struct 배열과 enum

`Drop` 구조체 배열은 각 입자의 위치·속도·색·접촉 시각을 저장합니다. `SurfaceState` 열거형으로 공중/바닥/벽을 구분합니다. 입자마다 MonoBehaviour를 붙이는 대신 한 컴포넌트가 한 번에 처리하므로 별도 오브젝트가 늘지 않습니다.

### ref 매개변수

`Move(ref Drop drop, ...)`, `Attach(ref Drop drop, ...)`는 구조체를 복사해 버리지 않고 호출한 쪽의 입자 상태를 변경합니다. FixedUpdate에서 꺼낸 입자를 수정한 뒤 배열에 다시 저장합니다.

### ParticleSystem.SetParticles와 Color32

`SetParticles`로 직접 계산한 위치·크기·회전·색을 ParticleSystem에 전달합니다. 자동 시뮬레이션 속도는 0으로 두어 중력이 두 번 적용되지 않게 하고, 수명은 접촉 시점 기준으로 직접 계산합니다. 마지막 입자가 끝나면 Stop을 호출해 정리 담당 컴포넌트가 종료를 알 수 있게 합니다.

메시의 정점 색은 `Color32`(RGBA 각 8비트)로 저장합니다. 이번 Unity 버전에서 float Color 정점 배열로 만든 메시가 ParticleSystem에서 검게 출력되는 것을 실제 캡처로 확인했고, Color32 형식으로 저장해 해결했습니다. 화면 표시와 계산 데이터의 색을 모두 확인했습니다. [Unity SetParticles 문서](https://docs.unity.com/en-us/engine/6000.5/script-reference/unityengine/particlesystem/setparticles)

### CircleCast와 접촉 법선

현재 위치부터 다음 위치까지 CircleCast로 훑어 빠른 입자가 얇은 벽을 통과하지 않게 합니다. `hit.normal`은 표면 바깥 방향입니다. 위쪽 법선이면 바닥, 옆쪽 법선이면 벽으로 분류하고, 흔적의 긴 방향을 표면과 나란하게 돌립니다.

### 투영과 로컬 좌표

`down - normal * Dot(down, normal)`은 아래 방향 중 벽 안쪽으로 파고드는 성분을 없애 벽을 따라 내려갈 방향을 구합니다. 접촉 위치는 표면의 로컬 좌표로 보관해, 이후 표면 위치를 기준으로 흔적을 표시합니다.

### 텍스처 없는 메시와 알파 블렌딩

`PolygonBlood.shader`는 텍스처를 읽지 않고 정점 색과 알파로 다각형을 그리는 작은 Unlit 셰이더입니다. `Blend SrcAlpha OneMinusSrcAlpha`로 흔적이 투명해질수록 배경이 보입니다. 다각형 형상은 중심점과 외곽 꼭짓점을 삼각형으로 연결해서 제작했습니다.
