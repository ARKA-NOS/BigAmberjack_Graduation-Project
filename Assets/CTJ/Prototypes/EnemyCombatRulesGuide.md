# 적 추적 해제·HP·공격 예고 UI (2026-10-15 갱신)

이번 변경은 Assets/CTJ 안에만 있다. 공용 Agent/StatModule/HealthModule, 플레이어, 다른 팀의 생성기·씬은 수정하지 않았다. UI Toolkit/셰이더가 아니라 기본 uGUI의 World Space Canvas + Image를 사용한다.

10월 15일 추가 피드백: Catto 경고는 **점멸 없이 일정한 알파**를 유지한다. Mad Ghost는 점멸을 유지하되 알파를 50에서 30으로 낮췄다. 두 적의 피격 몸통은 Trigger, TerrainBody는 Ground 전용 물리 콜라이더로 분리했다. 이전 준비 자세·타격·후딜과 추가 유지 0.15초는 보존한다.

## 현재 테스트 값

| 프리팹 | 탐지 거리 | 추적 해제 거리 | HP | 기본 평타로 처치 | 공격 준비 시간 |
| --- | --- | --- | --- | --- | --- |
| Catto_Melee | 6 | 9 | 40 | 4회 | 0.35초 |
| MadGhost_Ranged | 10 | 14 | 30 | 3회 | 0.5초 |

PlayerTestPack Variant의 PlayerMeleeAttackSkill → physicalDamage → Physics Power → Player Stat Group 연결을 Unity에서 확인했으며 기본 피해는 10이다. 강화·버프·다른 캐릭터로 피해가 바뀌면 처치 횟수도 달라진다. 적에게 무적이나 피해 무시는 추가하지 않았다. 기존 HealthModule에 피해를 누적하고 HP가 0일 때만 사망한다.

## 1. 추적 규칙

- Idle에서는 Detection Range 안에 들어와야 Chase를 시작한다.
- Chase/Attack에서는 Chase Release Range **밖으로** 나가야 Idle로 돌아간다. 경계와 같은 거리는 추적을 유지한다.
- 해제하면 현재 위치에서 수평 이동을 멈춘다. 생성 위치로 돌아가는 로직은 없다. 공중이면 기존 중력으로 착지한다.
- 해제 후 두 거리 사이에 머무는 플레이어는 다시 추적하지 않는다. Detection Range 안으로 들어와야 재감지한다.
- 준비 중 해제되면 공격과 UI를 취소한다. 이미 발사된 탄은 기존 충돌/수명 규칙대로 남는다.
- Inspector의 Chase Release Range를 수정한다. Detection Range보다 작은 값은 탐지 거리 이상으로 보정한다. 선택 Gizmo는 탐지 노랑, 해제 청록, 공격 빨강이다.

## 몸통 트리거와 지형 충돌

- Catto_Melee/MadGhost_Ranged 루트 BoxCollider2D: `Is Trigger=true`, Enemy 레이어. 공용 IDamageable이 있는 루트이므로 플레이어의 기존 Trigger 포함 OverlapDamageCaster가 피해를 전달한다.
- 새 `TerrainBody` 자식: 루트와 같은 크기/Offset의 비 Trigger BoxCollider2D. 별도 Rigidbody2D 없이 루트 Rigidbody2D에 연결된다. Include Layers=Ground, Exclude Layers=Ground 이외 전부, Layer Override Priority=1이다. 플레이어/다른 적은 통과하고 바닥·벽과만 물리 충돌한다.
- TerrainBody 자체는 기존 Ignore Raycast 레이어(2)이며 피해 수신 컴포넌트를 붙이지 않는다. 전역 Physics2D 충돌 행렬이나 공용 레이어 정의는 수정하지 않았다. **바닥·벽은 기존 Ground 레이어(6)**여야 한다. 다른 지형 레이어를 쓰려면 TerrainBody의 Include/Exclude와 EnemyMovementModule의 Terrain Layers를 함께 맞춘다.
- EnemyMovementModule의 Body Collider는 TerrainBody를 참조한다. 적의 몸 크기를 바꿀 때 루트 피격 몸통과 TerrainBody의 Size/Offset을 함께 조정한다. Visual 아래에 TerrainBody를 두거나 추가 Rigidbody2D를 붙이지 않는다.
- Chase는 Update뿐 아니라 FixedUpdate의 이동 직전에도 기존 공격 조건을 확인해 Attack으로 전환한다. Attack 상태는 준비·타격·회수·사거리 내 쿨타임 대기 동안 수평 이동을 멈춘다. 사거리 밖이면 다시 추적하며, 지면·높이 차이·시야 조건은 유지한다.
- Ignore Raycast가 모든 명시적 Overlap 쿼리를 자동 제외하는 것은 아니다. 현재 플레이어 캐스터의 무레이어필터/고정 결과 버퍼 설정은 수정하지 않았다. 단일 적에 대한 기존 캐스터 피해 10이 한 번 들어오는 것은 검사했으며, 다수 적 밀집 시 결과 버퍼/필터 튜닝은 공용 플레이어 담당과 별도 확인할 사항이다.

## 2. HP와 준비 시간 설정

`StatModule → Base Stats`에 연결된 `Catto_Stats`/`MadGhost_Stats`에서 값을 바꾼다.

- MaxHealth: 40 / 30. HealthModule의 Current Health는 실행 때 MaxHealth로 초기화된다. 프리팹의 Current Health=0은 즉사 설정이 아니다.
- EnemyAttackDelay: 0.35 / 0.5초. 스킬의 Attack Delay Stat은 `Stats/EnemyAttackDelay.asset`을 참조한다.
- 스킬 컴포넌트의 Post Blink Delay: 두 적 모두 0.15초. 준비/점멸이 끝나고 경고가 켜진 상태로 기다리는 추가 시간이다. 점멸을 끈 Catto도 유지한다. 0.1~0.2초 사이에서 체감 조정한다. EnemyAttackDelay에 이미 포함된 값이 아니다.
- 공용 StatData/StatGroup은 설정만 공유하고, 실행 중 Stat 값과 종료 시각은 적마다 따로 가진다.
- 이번 공격은 시작 시 읽은 준비 시간을 끝까지 사용한다. 실행 중 스탯 변경은 다음 공격부터 반영한다.
- 다른 스킬은 다른 StatData를 연결해 준비 시간을 별도로 줄 수 있다. 연결하지 않은 구형/새 컴포넌트는 Fallback Attack Delay를 사용한다.

쿨타임은 기존처럼 **준비 시작부터** 센다. 준비 시간 + 공격/후딜이 더 길면 동작 종료까지 다음 스킬을 시작하지 않는다. EnemyBase의 구형 Attack Interval과 이중 계산하지 않는다.

## 3. 공격 예고 UI와 타이밍

공용 표시 프리팹: `Prefabs/AttackTelegraphCanvas.prefab`. 각 공격 스킬의 Telegraph Prefab에 연결한다. 첫 사용 시 스킬별로 하나 만들고 이후 Show/Hide로 재사용하며, 소유 적/스킬 제거 시 정리한다. UI에는 피해 판정, 입력 처리, GraphicRaycaster가 없다.

- 공용 기본 색상 RGBA: (255, 110, 110, 50). Catto의 Telegraph Opacity=1이라 알파 **50/255 ≈ 19.6%**, Ghost는 0.6이라 **30/255 ≈ 11.8%**다. 스킬별 런타임 표시 색만 조절하므로 공용 UI 프리팹 원본이나 다른 적의 색을 바꾸지 않는다.
- 근접: 실제 BoxCollider2D의 위치·크기·좌우 반전을 따라 사각형을 표시한다. 사용 즉시 `AttackPreparation` 상태를 재생한다. **Blink Before Attack=false로 점멸하지 않는다.** 준비 0.35초 → 켜진 상태 추가 유지 0.15초 → `Attack` 상태로 전환 → 클립 0.05초에 OnAttackHit → 0.4초에 OnAttackFinished 순서다.
- 준비 클립 `Catto_AttackPreparation`은 0초 Catto_018 → 0.05초 Catto_019로 바꾸고 마지막 자세를 유지한다. Loop/자동 Attack 전환/이벤트를 넣지 않는다. 준비 시간이 길어져도 스탯 기반 타이머가 끝날 때까지 이 자세로 기다린다.
- 타격 클립 `Catto_Attack`은 0/0.05/0.1/0.15/0.2초에 자세를 빠르게 바꾼 뒤, 마지막 Catto_023 자세를 0.4초까지 유지한다. 피해 이벤트는 0.05초, 종료 이벤트는 0.4초다. 전체 Animator.speed를 늦추지 않아 준비 스탯을 늘려도 타격 속도/회수 속도가 함께 느려지지 않는다.
- 원거리: 준비 시작에 발사 방향을 고정하고 탄의 지름·최대 이동 거리로 직선 경로를 표시한다. 현재 탄은 지름 0.25, 속력 8 × 수명 4 = 이동 거리 32이다. 스킬 사용 사거리 7과 탄의 이동 거리는 별개다. 준비 마지막 0.12초 점멸 → 켜진 상태 0.15초 유지 → 발사하며, 피한 타깃에게 몰래 재조준하지 않는다.
- Ghost의 점멸 간격은 0.04초로 알파 0/30을 번갈아 표시한다. EnemyAttackSkill의 준비 단계 타이머가 점멸 시작과 종료를 관리하며, Catto는 점멸 단계를 건너뛴다. EndBlink는 배율 적용된 알파를 즉시 복구한다. `OnAttackWarning`은 구형 Relay/클립 호환용으로 남았지만 이제 아무 동작도 하지 않으며, Catto 클립에서는 해당 이벤트를 제거했다.
- 점멸을 켠 스킬은 준비 스탯이 Blink Duration보다 짧아도 점멸 0.12초와 유지 0.15초를 생략하지 않는다. 각 단계가 실제 시작한 프레임에서 시간을 세므로 저프레임에서 마지막 유지 구간을 건너뛰지 않는다. 실제 전환은 프레임 경계 때문에 설정값보다 조금 늦을 수 있다.
- 근접 타격/원거리 발사 순간 표시를 숨긴다. 타깃 교체·비활성화·사망·추적 해제에서도 숨긴다. 준비 도중 들어온 오래된 타격/종료 이벤트는 무시한다.
- UI는 예고만 담당한다. 원거리 경로는 벽에 맞춰 잘라내지 않는 최대 경로이며, 실제 탄은 기존 충돌 검사로 벽에서 사라진다. Box가 아닌 근접 Collider는 바운딩 박스로 표시하므로 정밀 윤곽을 의미하지 않는다.

기본값의 명목상 타임라인(프레임 경계 지연 제외):

| 적 | 일반 표시/준비 | 점멸 | 켜진 채 유지 | 공격 및 회수 |
| --- | --- | --- | --- | --- |
| Catto | 0~0.35초 | 없음 | 0.35~0.50초 | 0.50초 Attack 시작, 0.55초 타격, 0.70~0.90초 마지막 자세 유지 후 종료 |
| Mad Ghost | 0~0.38초 | 0.38~0.50초 | 0.50~0.65초 | 0.65초 발사, 0.95초 후딜 종료 |

## 4. 직접 테스트하는 순서

1. **Play를 종료하고** Unity 컴파일이 끝났는지 확인한다. `EnemyArtTest.unity`를 연 뒤 Play한다. 이 씬의 색깔 타깃은 실제 플레이어가 아닌 EnemyDamageProbe다.
2. Catto 앞 사각형은 **깜빡이지 않고** 일정한 밝기를 유지해야 한다. Mad Ghost 발사 경로는 이전보다 옅고, 짧게 깜빡인 뒤 **켜진 채 0.15초 유지**되어야 한다. 준비/유지 중에는 근접 피해/탄 생성이 없어야 하며, 타격/발사 시점에 표시가 꺼져야 한다.
3. Catto는 경고 시작부터 입을 벌리는 준비 자세로 기다려야 한다. 준비가 끝나면 빠르게 물고, 마지막 자세에서 약 0.2초 멈춘 뒤 대기/추적으로 돌아가야 한다. 유지 구간에 Catto Target을 위로 옮기면 피해가 없어야 한다. Ghost Target을 위로 옮겨도 탄은 예고한 원래 방향으로 발사되어야 한다. Console의 `[CTJ 피격 테스트]` 로그는 실제 받은 피해다.
4. Catto 타깃을 적 기준 5 정도 떨어뜨려 추적을 시작시킨 뒤 7~8 거리로 옮긴다. 계속 따라오는지 본다. 9보다 멀어지면 멈추고, 6~9 거리에서는 다시 추적하지 않다가 6 안에 들어오면 재개해야 한다. 거리는 최초 생성점이 아닌 **현재 적 위치** 기준이다. Ghost는 10/14로 동일하게 확인한다.
5. 실제 PlayerTestPack을 사용하는 `CTJ_MapTestScene`에서는 기본 평타 10 기준 Catto 4회, Ghost 3회에 죽는지 Health Module의 Current Health로 확인한다. Enemy Target은 PlayerController가 붙은 내부 Transform이며, 비워두면 기존 지연 탐색을 사용한다.
6. 플레이어 없이 확인하려면 적 컴포넌트 메뉴의 `Test/Take 2 Damage (Play Mode)`를 사용한다. 이 메뉴는 피해 2이므로 Catto 20회, Ghost 15회가 필요하다. 평타 10과 혼동하지 않는다.
7. 특히 점멸 후 유지 구간에서 타깃을 지우거나 적을 비활성화/사망시켜 UI와 미발생 공격이 같이 취소되는지 확인한다. Catto는 취소 시 준비 자세에 고정되지 않아야 한다. Play 변경은 프리팹에 Apply하지 않고 종료한다.
8. CTJ_MapTestScene의 실제 플레이어로 두 적을 좌우로 통과한다. 서로 밀리거나 적을 발판처럼 밟지 않아야 한다. 적은 Ground 바닥에 서고 Ground 벽은 통과하지 않아야 한다. 실제 공격 입력으로 적에게 피해가 들어가는지도 확인한다.
9. Catto의 공격 범위 밖에 서서 접근을 기다린다. 사거리 1.05 안에서 멈추고 준비/공격/후딜을 보여야 한다. 그 자리에 있으면 쿨타임 중에도 멈춰 있으며, 멀어지면 다시 따라와야 한다. 장애물 뒤/공중이면 기존 공격 조건 때문에 바로 공격하지 않는 것이 정상이다.

튜닝 위치: 적 루트의 EnemyMeleeSkill/EnemyProjectileSkill → Blink Before Attack(false/true), Telegraph Opacity(1/0.6), Post Blink Delay(0.15), StatModule의 Stats 에셋 → EnemyAttackDelay(0.35/0.5), 공용 AttackTelegraphCanvas → Blink Duration/Interval(0.12/0.04). Animation 창에서는 Catto_AttackPreparation의 준비 자세, Catto_Attack의 타격/종료 이벤트와 마지막 자세 키 간격을 확인한다.

실행 중 C# 수정으로 도메인 재로드가 발생하면 기존 공용 모듈/FSM의 비직렬화 초기화 참조가 유실될 수 있다. 이 경우 Play를 종료하고 컴파일 후 다시 시작한다. 공용 핫 리로드 복구 구조 변경은 이번 범위에 포함하지 않는다.

## 5. 새 적/스킬에서 재사용

1. CTJ 안에 Catto/Ghost Prefab Variant와 별도 Stats 에셋을 만든다. HP·준비 시간·탐지/해제 거리를 조절한다.
2. 공격 스킬에 Attack Delay Stat과 같은 Telegraph Prefab을 연결한다. 새 Skill SO 키와 EnemySkillModule의 Skills 항목도 연결한다.
3. 기존 EnemyAttackSkill 상속 시 PrepareAttack에서 방향/이번 공격 데이터와 **준비 모션**을 시작하고, UpdateTelegraph에서 표시 모양을 설정한다. **실제 발사/타격 애니메이션 시작은 OnReleaseAttack에서** 한다. OnUseSkill에서 즉시 피해를 주면 준비/점멸 후 유지를 우회한다.
4. 근접 스킬의 Preparation State Name에는 준비 클립 상태, Attack State Name에는 빠른 타격+회수 클립 상태, Idle State Name에는 종료/취소 시 복귀할 상태를 연결한다. 준비 상태는 선택 사항이지만 Catto에는 연결했다. 타격 클립의 Hit/Finished 이벤트와 EnemyAnimationEventRelay를 연결한다. Warning 이벤트는 추가하지 않는다. 별도 Canvas나 UI Toolkit 패널은 필요 없다.

## 6. 검증 범위

- Unity 6000.3.22f1: 컴파일 및 두 적의 HP·준비 스탯·UI·애니메이션·발사체 연결 검사.
- `Tools~/VerifyEnemyCombatRules.cs`: 추적 경계/재감지/제자리 대기, 4/3회 피해 누적 사망, 준비 중 이벤트 무시, 실제 점멸 알파, 좌측 UI 정렬, 회피, 발사 방향 고정, 취소/사망/Animator 비활성화, 개체별 준비 스탯을 검사한다.
- 기존 이동 12건과 스킬 5건도 함께 재실행한다. 자동 테스트는 Probe와 피해 수신 경로를 사용한다. 실제 플레이어의 입력·장비·모든 생성 맵을 자동 검증한 것은 아니다.
- Play 중 소스 수정으로 초기화 예외가 발생한 세션과 최종 컴파일 후 새 Play 세션을 구분한다. 기존 Console 로그는 임의로 지우지 않는다.

2026-10-15 추가 피드백 후 최종 재검증 결과: 충돌/사거리/캐스터 10 + 타이밍 8 + 전투 규칙 13 + 이동 12 + 스킬 5 = **48건 모두 통과**. 컴파일 오류 0, 검사한 Console 로그 179건 중 오류/경고 0. CTJ_MapTestScene이 isDirty=false로 복원되고 Play 시작 씬 설정도 복구됐음을 확인했다. 두 CTJ 적의 충돌 구성만 바꿨고 플레이어/전역 물리 설정/기획서는 변경하지 않았다.

`RunCollisionScenarios`는 Player(7)/Default(0)/Enemy(8) 레이어의 비 Trigger 물리 대역을 이용한 양방향 통과, 바닥·벽 접촉, 기존 플레이어 캐스터 복제본의 단일 피해 10, 사거리 정지/공격/쿨타임 대기/추적 재개를 검사한다. 실제 PlayerPack 전체의 입력·장비·모듈 조합 테스트는 아니다. Catto는 검사에서 타깃까지 약 1.044 거리(설정 사거리 1.05)에 멈췄다.

추가 타이밍 검사는 `RunTimingScenarios`다. Time.captureDeltaTime으로 30/60 Hz 시뮬레이션 시간 간격을 적용하고 finally에서 원래 값으로 복구한다. 실제 기기의 FPS/시인성 벤치마크는 아니다. 경고가 켜진 실제 유지 시간은 30 Hz 약 0.167초, 60 Hz 약 0.150초였다. 두 적의 공격 전 피해 금지, 실제 준비 스프라이트, 빠른 타격 이벤트, 마지막 회수 자세, 유지 중 타깃 상실/사망/재사용, 긴 준비 스탯/0초 스탯도 검사했다. 실제 플레이어 입력을 이용한 체감 조정은 위 수동 테스트로 확인한다.

`Tools~/AttackTelegraphPreview.png`는 10월 14일의 위치/색상 확인 이미지이며, 이번 타이밍 변경을 촬영한 자료는 아니다.

실행 순서: InspectEnemyArt.BeginPlayTest → VerifyEnemyCombatRules.RunCollisionScenarios → VerifyEnemyCombatRules.RunTimingScenarios → VerifyEnemyCombatRules.RunScenarios → VerifyEnemySkills.RunScenarios → VerifyEnemyMovement.RunScenarios → InspectEnemyArt.StopPlayTest → 종료 확인 → InspectEnemyArt.RestoreStartScene. 씬 파일을 저장하지 않는다.

문법별 설명은 `EnemySkillSyntaxNotes.md`의 마지막 절에 별도로 정리했다.
