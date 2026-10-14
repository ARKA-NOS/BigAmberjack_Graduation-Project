# 적 추적 해제·HP·공격 예고 UI (2026-10-14)

이번 변경은 Assets/CTJ 안에만 있다. 공용 Agent/StatModule/HealthModule, 플레이어, 다른 팀의 생성기·씬은 수정하지 않았다. UI Toolkit/셰이더가 아니라 기본 uGUI의 World Space Canvas + Image를 사용한다.

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

## 2. HP와 준비 시간 설정

`StatModule → Base Stats`에 연결된 `Catto_Stats`/`MadGhost_Stats`에서 값을 바꾼다.

- MaxHealth: 40 / 30. HealthModule의 Current Health는 실행 때 MaxHealth로 초기화된다. 프리팹의 Current Health=0은 즉사 설정이 아니다.
- EnemyAttackDelay: 0.35 / 0.5초. 스킬의 Attack Delay Stat은 `Stats/EnemyAttackDelay.asset`을 참조한다.
- 공용 StatData/StatGroup은 설정만 공유하고, 실행 중 Stat 값과 종료 시각은 적마다 따로 가진다.
- 이번 공격은 시작 시 읽은 준비 시간을 끝까지 사용한다. 실행 중 스탯 변경은 다음 공격부터 반영한다.
- 다른 스킬은 다른 StatData를 연결해 준비 시간을 별도로 줄 수 있다. 연결하지 않은 구형/새 컴포넌트는 Fallback Attack Delay를 사용한다.

쿨타임은 기존처럼 **준비 시작부터** 센다. 준비 시간 + 공격/후딜이 더 길면 동작 종료까지 다음 스킬을 시작하지 않는다. EnemyBase의 구형 Attack Interval과 이중 계산하지 않는다.

## 3. 공격 예고 UI와 타이밍

공용 표시 프리팹: `Prefabs/AttackTelegraphCanvas.prefab`. 각 공격 스킬의 Telegraph Prefab에 연결한다. 첫 사용 시 스킬별로 하나 만들고 이후 Show/Hide로 재사용하며, 소유 적/스킬 제거 시 정리한다. UI에는 피해 판정, 입력 처리, GraphicRaycaster가 없다.

- 색상 RGBA: (255, 110, 110, 50). 알파 50은 50%가 아니라 **50/255 ≈ 19.6%**다.
- 근접: 실제 BoxCollider2D의 위치·크기·좌우 반전을 따라 사각형을 표시한다. 준비 0.35초 후 공격 애니메이션 시작 → 클립 0.08초에 OnAttackWarning으로 점멸 → 0.2초 OnAttackHit에서 겹침 검사와 피해 → 0.55초 OnAttackFinished로 종료한다. 따라서 기본 피해 시점은 공격 시작 약 0.55초 후다.
- 원거리: 준비 시작에 발사 방향을 고정하고 탄의 지름·최대 이동 거리로 직선 경로를 표시한다. 현재 탄은 지름 0.25, 속력 8 × 수명 4 = 이동 거리 32이다. 스킬 사용 사거리 7과 탄의 이동 거리는 별개다. 준비 마지막 0.12초 점멸 후 발사하며, 피한 타깃에게 몰래 재조준하지 않는다.
- 점멸 간격은 0.04초로 알파 0/50을 번갈아 표시한다. 근접은 애니메이션 이벤트 사이의 0.12초, 원거리는 UI의 Blink Duration 동안 점멸한다. 근접 클립을 교체하면 Warning을 Hit보다 약 0.12초 앞에 배치한다.
- 근접 타격/원거리 발사 순간 표시를 숨긴다. 타깃 교체·비활성화·사망·추적 해제에서도 숨긴다. 준비 도중 들어온 오래된 타격/종료 이벤트는 무시한다.
- UI는 예고만 담당한다. 원거리 경로는 벽에 맞춰 잘라내지 않는 최대 경로이며, 실제 탄은 기존 충돌 검사로 벽에서 사라진다. Box가 아닌 근접 Collider는 바운딩 박스로 표시하므로 정밀 윤곽을 의미하지 않는다.

## 4. 직접 테스트하는 순서

1. **Play를 종료하고** Unity 컴파일이 끝났는지 확인한다. `EnemyArtTest.unity`를 연 뒤 Play한다. 이 씬의 색깔 타깃은 실제 플레이어가 아닌 EnemyDamageProbe다.
2. Catto 앞의 연붉은 사각형과 Mad Ghost의 가느다란 발사 경로가 준비 때 나타나고, 짧게 깜빡인 뒤 사라지는지 본다. 타격 직전에 타깃을 옮기면 근접 피해가 없어야 한다. 원거리 타깃을 위로 옮겨도 예고한 방향으로 발사되어야 한다.
3. Catto 타깃을 적 기준 5 정도 떨어뜨려 추적을 시작시킨 뒤 7~8 거리로 옮긴다. 계속 따라오는지 본다. 9보다 멀어지면 멈추고, 6~9 거리에서는 다시 추적하지 않다가 6 안에 들어오면 재개해야 한다. 거리는 최초 생성점이 아닌 **현재 적 위치** 기준이다. Ghost는 10/14로 동일하게 확인한다.
4. 실제 PlayerTestPack을 사용하는 `CTJ_MapTestScene`에서는 기본 평타 10 기준 Catto 4회, Ghost 3회에 죽는지 Health Module의 Current Health로 확인한다. Enemy Target은 PlayerController가 붙은 내부 Transform이며, 비워두면 기존 지연 탐색을 사용한다.
5. 플레이어 없이 확인하려면 적 컴포넌트 메뉴의 `Test/Take 2 Damage (Play Mode)`를 사용한다. 이 메뉴는 피해 2이므로 Catto 20회, Ghost 15회가 필요하다. 평타 10과 혼동하지 않는다.
6. 공격 준비 중 마지막 피해를 주거나 적을 비활성화해 UI와 미발생 공격이 같이 취소되는지 확인한다. Play 변경은 프리팹에 Apply하지 않고 종료한다.

실행 중 C# 수정으로 도메인 재로드가 발생하면 기존 공용 모듈/FSM의 비직렬화 초기화 참조가 유실될 수 있다. 이 경우 Play를 종료하고 컴파일 후 다시 시작한다. 공용 핫 리로드 복구 구조 변경은 이번 범위에 포함하지 않는다.

## 5. 새 적/스킬에서 재사용

1. CTJ 안에 Catto/Ghost Prefab Variant와 별도 Stats 에셋을 만든다. HP·준비 시간·탐지/해제 거리를 조절한다.
2. 공격 스킬에 Attack Delay Stat과 같은 Telegraph Prefab을 연결한다. 새 Skill SO 키와 EnemySkillModule의 Skills 항목도 연결한다.
3. 기존 EnemyAttackSkill 상속 시 PrepareAttack에서 방향/이번 공격 데이터를 준비하고, UpdateTelegraph에서 표시 모양을 설정하며, **실제 발사/애니메이션 시작은 OnReleaseAttack에서** 한다. OnUseSkill에서 즉시 피해를 주면 준비 시간을 우회한다.
4. 근접 클립의 Warning/Hit/Finished 이벤트와 EnemyAnimationEventRelay를 연결한다. 별도 Canvas나 UI Toolkit 패널을 새로 만들 필요가 없다.

## 6. 검증 범위

- Unity 6000.3.22f1: 컴파일 및 두 적의 HP·준비 스탯·UI·애니메이션·발사체 연결 검사.
- `Tools~/VerifyEnemyCombatRules.cs`: 추적 경계/재감지/제자리 대기, 4/3회 피해 누적 사망, 준비 중 이벤트 무시, 실제 점멸 알파, 좌측 UI 정렬, 회피, 발사 방향 고정, 취소/사망/Animator 비활성화, 개체별 준비 스탯을 검사한다.
- 기존 이동 12건과 스킬 5건도 함께 재실행한다. 자동 테스트는 Probe와 피해 수신 경로를 사용한다. 실제 플레이어의 입력·장비·모든 생성 맵을 자동 검증한 것은 아니다.
- Play 중 소스 수정으로 초기화 예외가 발생한 세션과 최종 컴파일 후 새 Play 세션을 구분한다. 기존 Console 로그는 임의로 지우지 않는다.

최종 재검증 결과: 전투 규칙 13 + 이동 12 + 스킬 5 = 30건 모두 통과. 컴파일 오류 0, 새 Play 세션 종료 후 Console 오류/경고 0. CTJ_MapTestScene이 isDirty=false로 복원되고 Play 시작 씬 설정도 복구됐음을 확인했다. 실제 Game 화면의 Canvas 위치/색상은 `Tools~/AttackTelegraphPreview.png`로 확인했다.

실행 순서: InspectEnemyArt.BeginPlayTest → VerifyEnemyCombatRules.RunScenarios → VerifyEnemyMovement.RunScenarios → VerifyEnemySkills.RunScenarios → InspectEnemyArt.StopPlayTest → 종료 확인 → InspectEnemyArt.RestoreStartScene. 씬 파일을 저장하지 않는다.

문법별 설명은 `EnemySkillSyntaxNotes.md`의 마지막 절에 별도로 정리했다.
