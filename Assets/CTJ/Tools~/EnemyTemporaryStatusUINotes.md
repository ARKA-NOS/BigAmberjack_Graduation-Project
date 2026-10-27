# 임시 적 체력·경직 UI와 경직 연장

## 적용 결과

- Catto, MadGhost, 01, 02의 `Hit Stun Duration`을 **0.3초**로 설정했습니다. 기존 실제 경직은 0.18~0.2초였으므로 약 0.1초 길어졌습니다.
- `Knockback Speed`와 `Knockback Duration`은 변경하지 않았습니다. 기존만큼 밀린 뒤 남은 경직 시간 동안 행동을 제한합니다.
- 보스는 기존 경직/넉백 비활성화를 유지합니다. 체력 바는 표시되지만, 경직이 없으면 흰 게이지는 차지 않습니다.
- 기존 적 프리팹 5종에 `TemporaryStatusUI` 자식을 붙였습니다. 공유 프리팹은 `Assets/CTJ/Prototypes/Prefabs/EnemyTemporaryStatusUI.prefab`입니다.
- UGUI의 World Space Canvas와 Image만 사용합니다. UI Toolkit, 외부 패키지, 새로운 추상 클래스/인터페이스는 추가하지 않았습니다.

## 두 게이지의 동작

| 위치 | 표시 | 기준 |
| --- | --- | --- |
| 위 | 초록색 체력 바 | 소유 적의 `IHealthModule.CurrentHealth / MaxHealth` |
| 아래 | 흰색 경직 바 | `EnemyHitReactionModule.ReactionRemainingNormalized` |

흰 바는 체력 바와 같은 너비이며 **높이가 절반**입니다. 실제 채움 영역은 체력 158×12, 경직 158×6 픽셀입니다. 기본 월드 배율은 0.01이라 전체 바 너비는 약 1.6 유닛입니다.

평상시 흰 바는 비어 있습니다. 피격으로 경직이 생기면 가득 찬 뒤 남은 시간만큼 줄어듭니다. 연타 시 누적 시간을 별도로 더하지 않고 실제 갱신된 경직 구간을 다시 가득 찬 상태로 표시합니다.

UI가 경직을 종료시키는 구조는 아닙니다. **AI의 행동 잠금과 UI가 같은 종료 시각을 사용**하므로, UI를 삭제하거나 숨겨도 경직은 정상적으로 끝납니다. 넉백 시간이 경직 시간보다 길게 설정된 경우에는 더 긴 실제 행동 잠금 시간이 끝날 때까지 게이지가 유지됩니다. 점멸이나 스케일 효과의 재생 시간과는 독립적입니다.

체력은 HealthModule의 체력 변경 이벤트를 구독하므로 피해뿐 아니라 회복도 반영됩니다. 사망하면 두 바를 숨깁니다. 적의 방향 전환과 피격 스케일에는 영향을 받지 않도록 적 루트 바로 아래에 배치했습니다.

## Inspector 조절 위치

- **경직 시간:** 적의 `HitReactionModule > EnemyHitReactionModule > Hit Stun Duration`.
- **바 위치:** `TemporaryStatusUI > EnemyTemporaryStatusUI > World Offset`. 적 루트 기준 월드 오프셋입니다. 초기 배치는 몸통 Collider 윗부분에서 0.25 유닛 위에 잡았습니다. 터렛은 완전히 전개된 몸통 높이를 기준으로 배치합니다.
- **바 전체 크기:** 같은 컴포넌트의 `World Units Per Pixel`.
- **색과 개별 크기:** 공유 UI 프리팹의 `HealthBackground/HealthFill`, `ReactionBackground/ReactionFill` 안 Image 색상과 RectTransform 크기. 개별 적만 바꾸려면 해당 프리팹의 UI 자식에서 오버라이드합니다.

표시용 이미지의 Raycast Target은 꺼져 있고 GraphicRaycaster도 없습니다. 게임 입력을 가로채지 않습니다.

## 말끔하게 제거하는 방법

1. 적 프리팹에서 루트 바로 아래의 **`TemporaryStatusUI` 자식 전체를 삭제**합니다. UI 컴포넌트만 삭제하면 정적인 Canvas가 남을 수 있으므로 자식 오브젝트째 제거하세요.
2. 잠시 숨기기만 하려면 해당 자식 GameObject를 비활성화하거나 `EnemyTemporaryStatusUI` 컴포넌트를 끄면 됩니다. UI를 켜면 현재 체력/경직 값을 다시 읽습니다.
3. 프로젝트에서도 완전히 걷어낼 때는 사용 중인 모든 적/씬의 UI 자식 제거 후, 전용 UI 프리팹과 `EnemyTemporaryStatusUI.cs`를 Unity Project 창에서 제거하면 됩니다. 제작·검증용 `Tools~/BuildEnemyTemporaryStatusUI.cs`, `Tools~/VerifyEnemyTemporaryStatusUI.cs`도 더 이상 필요하지 않습니다.

`EnemyBase`와 `HealthModule`은 임시 UI를 참조하지 않습니다. `EnemyHitReactionModule`에 추가된 읽기 전용 남은 시간 프로퍼티도 UI 타입을 참조하지 않습니다. **공용 HealthModule, 피격 모듈, 공유 TestWhite 스프라이트는 삭제하지 마세요.**

## 직접 테스트하는 방법

1. CTJ_MapTestScene 또는 테스트 씬에서 적 프리팹으로 Play합니다. 머리 위 초록 바는 가득 차고, 아래 흰 바는 비어 있어야 합니다.
2. 플레이어로 적을 공격하거나 적 루트 컴포넌트의 메뉴에서 `Test > Take 2 Damage (Play Mode)`를 실행합니다. 초록 바가 실제 HP 비율로 줄고 흰 바가 찼다가 0.3초에 걸쳐 줄어드는지 확인합니다.
3. 구분이 어렵다면 Play 중 `Hit Stun Duration`을 1초로 바꿔 확인한 뒤 원하는 값으로 되돌립니다. 흰 바가 비기 전에는 추격/새 공격을 재개하지 않아야 합니다.
4. 경직 중 다시 맞혀 흰 바가 다시 차는지 확인합니다. HealthModule의 `Current Health`를 높여 초록 바가 회복되는지도 확인합니다.
5. 피격 후 UI 자식을 비활성화합니다. UI 없이도 경직이 끝나야 하며, 다시 활성화하면 현재 HP가 표시되어야 합니다.
6. 제거 안전성은 테스트용 적 복제본의 `TemporaryStatusUI` 자식을 삭제해서 확인합니다. 이후에도 피해·경직·사망 처리는 정상이어야 합니다.
7. 적 사망 시 두 바가 사라지고, 터렛은 제자리 경직을 유지하는지 확인합니다. 보스의 흰 게이지가 비어 있는 것은 기존 경직 비활성화 설정 때문입니다.

Play 중 바꾼 값은 종료하면 복원됩니다. 최종 값은 Edit 모드에서 프리팹에 적용하세요.

## 사용한 문법·API와 이유 — 학습용

- **읽기 전용 프로퍼티 `=>`:** `RemainingReactionTime`과 `ReactionRemainingNormalized`로 실제 상태를 외부에 읽기만 허용합니다. UI가 적의 타이머를 수정하지 않습니다.
- **`Mathf.Max`, `Mathf.Clamp01`:** 넉백과 경직 중 남은 시간이 더 긴 것을 사용하고, UI 비율은 0~1로 제한합니다. 분모가 0인 경우는 0을 반환합니다.
- **기존 `IHealthModule`:** 구체적인 체력 UI 구현 대신 기존 체력 모듈의 계약을 이용합니다. 새로운 인터페이스는 만들지 않았습니다.
- **이벤트 `+=` / `-=`:** 체력 변화 때만 체력 바를 갱신합니다. 비활성화·삭제 시 반드시 구독 해제하여 삭제된 UI 참조나 중복 구독이 남지 않게 합니다.
- **`Start`와 `Awake`의 역할 분리:** Awake에서는 UI 자체 참조만 얻고, Start에서 체력/피격 모듈을 조회합니다. 소유 적의 Awake와 모듈 초기화가 끝난 뒤 연결하여 초기화 순서의 NullReference를 피합니다.
- **표시 전용 `MonoBehaviour`:** 공용 ModuleOwner에 등록할 필요가 없습니다. 삭제해도 모듈 사전이나 적 로직에 UI 참조가 남지 않아 임시 기능을 제거하기 쉽습니다.
- **`Image.Type.Filled` / `fillAmount`:** 이미지의 표시 비율만 바꿉니다. HP/경직을 별도로 계산하거나 UI에서 타이머 코루틴을 돌리지 않습니다.
- **`LateUpdate`와 월드 좌표:** 적 이동 이후 바 위치와 남은 경직을 갱신합니다. 부모의 스케일을 보정해 루트가 2배 크기이거나 좌우 반전되어도 바 크기와 감소 방향이 유지됩니다.

## 검증 기록

- Unity C# 컴파일 및 프리팹 5종의 Missing Script / 끊어진 참조 검사 통과.
- 적 5종의 HP 변경 이벤트, 회복, 경직 갱신, UI 추적, 방향 반전, 비활성화/재연결, 사망 숨김 검사 통과.
- 넉백 0.45초 / 별도 경직 0.05초처럼 시간이 다른 경우에도 실제 잠금 종료와 게이지가 일치함을 확인했습니다.
- UI 자식 삭제 후에도 HP 감소, 0.3초 경직 및 정상 해제를 확인했습니다.
- 01의 배회·추격·애니메이션 공격·피격 취소와 02의 전개·조준·발사·피격 취소 회귀 검사 통과.
- 실제 카메라 렌더링에서 체력 65%, 경직 약 50% 표시와 흰 바 높이 비율 0.5를 확인했습니다. 미리보기는 `Tools~/EnemyStatusBarsPreview.png`입니다.
- Unity 외부 씬 변경 창은 사용자에게 Reload를 요청했고, 이후 저장하지 않은 빈 테스트 씬에서 검증했습니다. 종료 후 원래 CTJ_MapTestScene을 저장 없이 복원했습니다.
