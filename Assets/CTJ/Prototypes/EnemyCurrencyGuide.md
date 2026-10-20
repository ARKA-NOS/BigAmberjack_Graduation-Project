# 적 사망 Gold 지급과 프리팹 계층 (2026-10-20)

## 동작과 설정

- 바닥 아이템을 생성하지 않고 사망 시 Gold를 즉시 지급한다. 일반 아이템 드롭/줍기는 이번 작업에 포함하지 않는다.
- `EnemyBase.Die()`가 사망·공격 취소·물리 정리·제거 예약을 끝낸 뒤, 선택 모듈인 `EnemyCurrencyRewardModule.GrantDeathReward()`를 호출한다.
- 보상 모듈은 소유 적의 `StatModule`에서 `EnemyGoldReward`의 현재 값을 읽어 소수점을 버린 뒤 공용 `CurrencySystem.GetCurrencyData(rewardCurrency).Add(amount)`로 전달한다.
- 테스트 기본값: `Catto_Stats`는 10, `MadGhost_Stats`는 15. 기존 HP 40/30, 공격 수치, 피격 옵션은 유지했다.
- 수량 변경: 적의 `StatModule > Base Stats`로 연결된 StatGroup에서 `EnemyGoldReward > Base Value`를 수정한다. 공용 StatData의 Default Value가 아니라 적별 StatGroup 값을 수정한다.
- `CurrencyRewardModule`의 `Reward Currency`는 기존 Lrw `Gold.asset`, `Reward Amount Stat`은 CTJ `EnemyGoldReward.asset`이다.
- 보상 수량이 1 미만이면 지급하지 않는다. 모듈을 끄거나 제거해도 적의 사망 처리는 유지된다.
- 실제 사망만 지급한다. 살아 있는 적의 비활성화/Destroy/씬 종료, 사망 후 추가 타격은 보상을 만들지 않는다.
- 매니저 미초기화, 재화 미등록, 잘못된 수량, int 잔액 한도 초과는 경고 후 지급을 건너뛴다. 뒤늦은 자동 지급/재시도는 없다.
- Gold 잔액/저장 정책은 Lrw 소유이며 수정하지 않았다. 현재 Gold는 비영구 재화다.

## 두 프리팹의 계층

```text
Catto_Melee / MadGhost_Ranged
│  EnemyBase 파생 컴포넌트, Rigidbody2D, 피격용 Trigger
├─ Visual                    SpriteRenderer, Animator, 이동 애니메이션, 이벤트 Relay
│  └─ AttackHitbox / FirePoint
├─ EnemyMovement             EnemyMovementModule
├─ TerrainBody               지형 전용 Collider2D (기존 레이어/크기 유지)
├─ StatModule                공용 StatModule, 적별 StatGroup
├─ HealthModule              공용 HealthModule
├─ SkillModule               EnemySkillModule
│  └─ MeleeAttack / ProjectileAttack
├─ HitReactionModule         EnemyHitReactionModule
└─ CurrencyRewardModule      EnemyCurrencyRewardModule
```

- `ModuleOwner`는 자식 모듈도 수집한다. 동일한 모듈 타입을 루트/자식에 중복으로 추가하면 안 된다.
- 공용 HealthModule은 Initialize에서 Max HP를 읽는다. **StatModule을 HealthModule보다 앞에 유지한다.**
- 새 공격 컴포넌트는 `SkillModule`의 같은 오브젝트 또는 자식에 추가하고, EnemySkillModule의 Skills 목록에 연결한다. 이 탐색 범위 밖에 놓으면 공용 스킬 시스템에 등록되지 않는다.
- 좌우 반전을 위해 AttackHitbox/FirePoint는 Visual 아래를 유지했다. 공격 스킬 컴포넌트는 분리했지만 기존 참조와 Animator 이벤트 경로는 보존했다.
- 물리 Rigidbody와 피격 Trigger는 EnemyBase와 함께 루트에 남겼다. 플레이어 공격이 Collider에서 IDamageable을 찾는 기존 경로를 유지하기 위해서다.
- 쓰지 않는 EffectModule이나 새 추상 타입은 추가하지 않았다.

## 직접 테스트

1. `Assets/CTJ/Scenes/CTJ_MapTestScene.unity`를 연다. 사용자가 추가한 GameManager 아래 CurrencyManager에 기존 Gold 에셋이 등록되어 있는지 확인한다. GameManager를 중복으로 추가하지 않는다.
2. Play를 누른다. CurrencyManager 컴포넌트 메뉴의 **Debug Currency**로 현재 Gold를 확인한다. `Cheat`는 다른 재화까지 바꾸므로 사용하지 않는다.
3. Catto를 공격한다. 살아 있을 때는 Gold가 그대로이고, HP 0이 되는 순간 **+10**이어야 한다. 사망 연출 종료까지 기다리지 않아도 지급된다.
4. MadGhost를 처치해 **+15**를 확인한다. 두 마리를 각각 한 번 처치했다면 시작 잔액보다 **25 증가**한다. 다른 적/보상 획득이 없는 조건에서 비교한다.
5. 사망 직후 추가 공격을 해도 잔액이 다시 증가하지 않는지 확인한다.
6. Catto/MadGhost의 추적, 지형 충돌, 근접 타격 이벤트, 원거리 경고·발사, 피격 취소·붉은 점멸·사망 애니메이션을 확인한다.
7. Play 종료 후 StatGroup의 보상 수량을 원하는 정수로 변경하고 다시 시작해 해당 수량만 지급되는지 확인한다. 0은 보상 없음이다.

## 자동 검증

`Tools~/VerifyEnemyCurrency.cs`는 프리팹 모듈 중복/계층/참조 및 실제 CurrencyManager를 이용한 지급을 검사한다. 전용 EnemyArtTest Play 씬에서만 실행하며 씬/공용 에셋/재화 저장 파일을 쓰지 않는다. 미초기화·미등록·잔액 초과 테스트에서 경고 3건은 의도한 결과다. 기존 이동/스킬/전투/피격 검증 도구도 자식 모듈을 조회하도록 맞췄다.

실행 결과: 보상 검증 20항목 + 기존 스킬 5 / 전투 13 / 충돌 10 / 피격 22 / 이동 12 / 타이밍 8시나리오 통과. Unity 컴파일 오류 0, 최종 테스트 Console 오류 0. 프리팹 참조와 모듈 중복 검사도 통과했다. 테스트 종료 후 기존 CTJ_MapTestScene과 Play 시작 씬 설정을 복구했다.
