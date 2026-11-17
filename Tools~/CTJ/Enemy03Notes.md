Enemy03 — 뒤틀린 인간 (`enemy_zombie`)

2026-11-12, [09-03 기획서](https://app.notion.com/p/09-03_-0cc9e7231ca78281b3ac81d89426bb30?source=copy_link)를 읽고 구현했다. 노션과 허용 영역 밖의 파일은 수정하지 않았다.

사용할 프리팹은 `Assets/_Prefabs/Enemies/Legacy/Enemy/Enemy03/Enemy03_TwistedHuman.prefab`이다. Enemy01의 Zombie 그림과 기존 체력·피격·아웃라인·파티클·데미지 숫자·카메라 흔들림·사망·코인 모듈을 재사용한다. Enemy01 원본 프리팹과 애니메이터는 변경하지 않았다.

적의 기본값은 HP 80, 이동 속도 3.8, 감지 거리 9, 추적 해제 거리 12, 피격 경직 0.25초, 처치 보상 8골드다. 몸체 높이는 기본 스케일에서 1.8유닛이다. Enemy03 스탯 그룹에서 방어력 항목을 제거했다. 접촉 자체로는 피해를 주지 않는다. 배회 중심은 생성 위치이며 기본 좌우 반경은 3유닛이다.

| 항목 | 베기 — SlashAttack | 도약 — LeapAttack |
| --- | --- | --- |
| 자동 선택 조건 | 플레이어와 1.5유닛 이내 | 플레이어와 3유닛 이상 6유닛 이하, 베기 쿨타임 중 |
| 준비 | 0.45초, 준비 중 방향 전환 | 0.7초, 준비 시작 때 착지점과 방향 확정 |
| 실행 | 전방 1.6 × 1.2 박스 | 준비 시작 때 플레이어가 있던 지점으로 도약, 착지점 반경 1 |
| 피해 | 12, 공격당 1회 | 18, 착지당 1회 |
| 유효 판정 | 0.12초 | 0.15초 |
| 후딜 | 판정 종료 후 0.8초 | 착지 판정 종료 후 다음 공격까지 1초 |
| 쿨타임 | 2.2초 | 5초 |

쿨타임은 프로젝트 공용 `AbstractCoolTimeSkill` 규칙대로 **준비 시작 시점**부터 센다. 준비 중 맞으면 두 공격 모두 취소된다. 베기 판정 시작 뒤에는 방향이 고정된다. 도약은 준비에 진입할 때 사거리를 검사하고, 당시 플레이어 위치의 바닥을 착지점으로 정한다. 플레이어가 사거리나 추적 해제 범위를 벗어나도 정해 둔 지점으로 도약한다. 발사한 도약은 타깃이 교체되어도 착지와 대기까지 진행한다. 그동안 다음 스킬의 실행이 차단된다. 착지 피해는 원래 타깃이 실제 피해 범위에 있을 때 적용한다.

도약은 가로 속도를 서서히 높이고, 기본 0.46초에 정점에 도달한 뒤 0.20초 동안 급강하한다. 궤적의 스텝별 이동량을 Rigidbody2D 속도로 적용한다. 출발 직전에도 같은 궤적으로 Ground와 Platform을 검사한다. 벽과 천장에 막히거나 착지할 바닥이 없으면 뛰지 않는다. 빠른 낙하 중에도 바닥과 접촉할 수 있도록 Enemy03의 Rigidbody2D는 Continuous 충돌 감지를 사용한다. 착지하거나 공중에서 피격 경직이 시작되면 원래 중력을 복구한다. 착지 피해는 콜라이더가 실제 바닥과 접촉한 위치를 기준으로 발생한다.

`SkillModule/LeapAttack`의 `Landing Particle Prefab`에 루트에 ParticleSystem이 있는 프리팹을 연결한다. 바닥 접점에서 `EnemyParticleModule`이 한 번 재생하고 수명이 끝나면 정리한다. 현재 슬롯은 비어 있다.

도약 준비의 마지막 0.35초에는 준비 시작 때 저장한 착지 피해 범위를 붉은 원으로 표시한다. 원의 반경은 실제 피해 반경과 같은 1유닛이다. 발사 직전에는 저장한 착지점의 바닥과 경로를 검사한다. 해당 지점이 막혔거나 바닥이 사라졌으면 도약을 취소한다. `LeapAttack`의 Warning Lead Time으로 표시 시간을, `Enemy03_LeapWarning` 프리팹의 Area Color로 색과 투명도를 조정한다.

기본 착지 높이 허용 차이는 ±0.5유닛이다. 복잡한 다층 발판을 찾아가는 경로 탐색은 아니다. 일반 추적 점프는 이 프리팹에서 0으로 두었고 도약 스킬이 점프를 담당한다. 벽이나 절벽 때문에 도약할 수 없을 때는 걸어서 접근하되, 기존 절벽 방지 규칙을 유지한다.

조정 위치는 다음과 같다.

| 조정 내용 | Inspector 위치 |
| --- | --- |
| HP·베기 준비 시간·골드 | `Enemy03_TwistedHuman_Stats.asset` → Stats |
| 감지·해제 거리, 이동 속도 | 프리팹 루트 → MeleeEnemy |
| 배회 반경·배회 속도 | PatrolModule |
| 스킬 우선순위·사거리·높이 제한 | SkillModule → Skills |
| 베기 피해·판정 시간·후딜·쿨타임 | SkillModule/SlashAttack |
| 베기 박스 크기·위치 | Visual/AttackHitbox → BoxCollider2D |
| 도약 준비·높이·가속·상승 시간·급강하 시간·착지 범위·피해·후딜·쿨타임·경고 시간 | SkillModule/LeapAttack |
| 착지 파티클 | SkillModule/LeapAttack → Landing Particle Prefab |
| 도약 웅크림·떨림 | LeapAttack → Prototype Pose |
| 준비 중 공격 취소·경직·넉백 | HitReactionModule |

베기의 `Attack Delay Stat`은 Stats의 EnemyAttackDelay를 읽는다. 이 경우 `Fallback Attack Delay`를 바꿔도 준비 시간은 변하지 않는다. 도약은 해당 Stat 연결이 없으므로 자신의 `Fallback Attack Delay`(0.7)를 사용한다. 두 스킬 모두 `Post Blink Delay`는 0이다. 베기/도약 피해는 각 스킬의 Damage에 따로 설정되어 있다.

`Visual/LeapPose/HitVisual`로 그림 부모를 분리했다. 도약 준비 자세는 LeapPose의 스케일과 위치만 바꾸고, 피격 펀치 스케일과 애니메이션은 HitVisual에 적용한다. 물리 몸체와 공격 판정은 웅크리는 그림에 따라 찌그러지지 않는다. LeapPose를 임의로 콜라이더의 부모로 바꾸지 않는다.

`Enemy03_Zombie.controller`는 대기·걷기·준비·사망 그림을 공유하고, 베기는 별도 빠른 클립을 사용한다. 도약과 착지 상태는 대기 클립을 바탕으로 웅크림과 착지 스쿼시를 표현한다. 기획의 베기 바람음과 착지 충격음은 음원 선정과 사운드 연결 작업으로 남아 있다.

공용 스폰 시스템의 `EnemySpawnEntry.EnemyPrefab`에 이 프리팹을 넣을 수 있다. 실제 전투 구역/방의 스폰 목록 등록과 플레이어로부터 5~7유닛 떨어진 스폰 포인트 배치는 맵 담당 영역이다. 이번에는 공용 맵 코드와 방 데이터를 변경하지 않았다. 적 스스로 생성 위치를 옮기지 않는다.

직접 테스트할 때는 다음 순서로 확인한다.

1. `Assets/_Scenes/Test`의 테스트 씬에서 Ground 또는 Platform 레이어인 바닥에 프리팹을 배치한다. 루트 Y는 발 위치다. 첫 확인은 최소 12유닛 이상의 평평한 바닥, 프리팹 기본 Scale로 한다.
2. PlayerPack/현재 플레이어, CurrencyManager의 Gold 등록, EnemyKillCountManager를 준비하고 Play한다. 플레이어가 늦게 생성되어도 기존 타깃 탐색으로 연결된다. 테스트용 타깃만 있다면 `SetTarget(Transform)`을 호출한다.
3. 9유닛 안으로 다가가 추적을 확인하고, 12유닛 밖으로 나가 추적이 해제되는지 확인한다. 대기 시 생성 위치 기준으로 배회한다.
4. 1.5유닛 안에서 베기를 유도한다. 약 0.45초 준비한 뒤 전방에 한 번만 피해를 주고 잠시 멈춘다. 준비 중 반대편으로 이동하면 따라 돌지만 판정이 시작된 뒤에는 뒤돌지 않아야 한다.
5. 베기 실행 후 빠르게 3~6유닛으로 거리를 벌린다. Enemy03이 웅크리기 시작하면 옆으로 움직인다. 처음 서 있던 곳에 붉은 피해 범위가 나타나고, Enemy03이 초반에는 가로로 천천히 움직이다가 가속해 그곳으로 빠르게 내리꽂히는지 확인한다. 테스트용 파티클을 연결했다면 바닥에 닿을 때 한 번 재생되는지 확인한다.
6. 착지 직후 Enemy03 가까이 다가가 착지 판정이 끝나고 1초가 지난 다음 할퀴기 준비가 시작되는지 확인한다. 베기와 도약 준비 중 각각 때려 취소와 0.25초 경직도 확인한다.
7. 절벽 또는 벽 앞에서 확인한다. 착지점이 허공이거나 궤적이 막히면 도약하지 않아야 한다. Platform 바닥에서도 같은 동작을 확인한다.
8. 처치하면 즉시 사망 애니메이션과 피격 연출이 시작되고 마지막 자세에서 페이드아웃한다. 코인은 기본 3개(3+3+2골드)이며 회수 후 합계 8골드, 처치 수는 1 증가해야 한다.

실행 검증은 Unity 6000.3.22f1의 별도 빈 Play Mode 씬에서 했다. 프리팹 참조 검사(208개 유효 참조, Missing Script 없음), 고유 Skill SO 2개 등록, Unity 컴파일 오류·경고 0개를 확인했다.

자동 실행에서 3·6유닛은 준비에 진입하고 2.99·6.01유닛은 진입하지 않았다. 준비 중 플레이어가 이동해도 준비 시작 위치에 경고를 표시하고 착지했다. 6유닛 도약에서 가로 속도는 약 1.9에서 14.8유닛/초까지 올라갔고, 착지 전 관측한 최대 하강 속도는 약 14.4유닛/초였다. 두께 0.1유닛 바닥의 접촉, 테스트 파티클의 한 번 재생과 정리, 착지 시 중력 복구, 다음 공격까지 약 1초 대기를 확인했다.

학습용 문법 설명은 구현 설명과 구분해서 아래에 정리한다.

- `override`: 두 스킬이 `EnemyAttackSkill`의 준비·발동·종료 지점을 재정의한다. 공용 쿨타임과 취소 처리를 다시 만들지 않고 공격별 행동만 넣기 위한 것이다.
- `virtual`의 기본 구현: `ControlsMovement`는 기본 false, `FixedUpdateAttack()`은 기본 빈 함수다. 도약 중에만 이를 재정의하여 FSM의 정지 명령 대신 스킬이 물리 이동을 유지한다. 기존 적 스킬은 기본 동작을 그대로 사용한다.
- `is EnemyAttackSkill { ControlsMovement: true } attack`: 타입 확인과 프로퍼티 조건을 함께 검사하는 패턴 매칭이다. 해당 스킬일 때만 안전하게 물리 갱신을 넘기며, null 검사도 포함된다.
- `[SerializeField]`, `[Min]`, `[Range]`, `[Tooltip]`: 외부에서 함부로 바꾸지 않는 private 필드를 Inspector에서 조정하고 의미를 안내한다. 실제 실행에서도 `Mathf.Max`로 필요한 최소값을 보완한다.
- `out Vector2 velocity`, `out float flightTime`, `out _`: `TryPlanLeap`은 성공 여부뿐 아니라 초기 속도와 비행 시간을 반환한다. 사용 가능 여부만 검사할 때는 `out _`로 필요 없는 결과를 버린다.
- `List<Collider2D>`/`List<RaycastHit2D>`와 `_hitApplied`: 물리 쿼리 결과 목록을 재사용하고, 여러 콜라이더에 닿거나 여러 프레임 겹쳐도 한 번의 공격은 한 번만 피해를 주도록 한다.
- `readonly struct LeapTrajectory`는 한 번 결정한 시작점, 정점, 착지점과 각 구간의 길이를 함께 보관한다. 가로 진행률에는 제곱을 섞고 낙하 진행률에는 세제곱을 써서 뒤로 갈수록 스텝당 이동량이 커진다.
- `Mathf.CeilToInt`와 `Time.fixedDeltaTime`으로 구간을 물리 스텝 수로 바꾼다. 경로 검사와 이동은 같은 스텝 위치를 사용한다. 두 위치의 차이를 스텝 시간으로 나눠 `Rigidbody2D.linearVelocity`에 적용한다.
- `Collider2D.GetContacts`로 착지 콜라이더의 접점을 구한다. 바닥 접점이 여러 개면 평균 위치에서 파티클을 재생한다.
- `CompletesAfterTargetLost`의 `override`: 발사된 도약이 타깃을 잃어도 착지와 대기를 마치도록 한다. `IsUsing`이 유지되는 동안 스킬 모듈은 후속 공격을 차단한다.
- `SerializedObject`, `PrefabUtility`: Tools의 제작/검증 코드에서만 사용한다. 복제한 프리팹의 Inspector 값을 연결하고 저장하여 원본 적이나 현재 씬을 수정하지 않는다.
- `protected override bool ShouldShowTelegraph`: 공용 스킬의 경고 표시 시점을 도약에서만 늦춘다. `UpdateTelegraph()`은 저장된 월드 좌표에 원을 배치한다.
- `Vector2 _lockedLandingCenter`: `PrepareAttack()`에서 사거리를 확인하고 계산한 좌표 값을 저장한다. `Vector2`는 값 타입이라 이후 플레이어가 이동해도 저장한 값은 그대로다. 경고와 비행은 이 좌표를 공유한다.
- `FinishesOutsideChaseRange`의 `override`: 도약이 준비 상태에 들어가면 플레이어가 추적 해제 범위를 넘어도 공격 상태를 유지한다.
- `MaskableGraphic.OnPopulateMesh(VertexHelper)`: 기본 Canvas UI의 메시 정점을 직접 배치해 피해 반경에 맞는 반투명 원을 그린다.

`SetupEnemy03.cs`와 `VerifyEnemy03.cs`는 Tools 전용 도구이므로 빌드에 포함되지 않는다. Setup은 이미 프리팹이 있으면 중단해서 Inspector 조정을 덮어쓰지 않는다. Verify는 저장되지 않은 씬에서 시작하지 않는다.
