# Enemy01·04 외형 교체 / Enemy02 외곽선 정리

2026-10-28. 수정 범위: `Assets/CTJ`만. 패키지 추가 없음.

같은 날 후속 수정: 위치·방향에 따른 외곽선 누락과 받침 아래쪽 잘림을 해결했습니다. 최신 원인·검증·문법 설명은 [Enemy02ContourFixNotes.md](Enemy02ContourFixNotes.md)를 참고하세요. 아래의 최초 렌더링 검증 수치는 후속 수정 전 기록입니다.

## 적용 결과

| 대상 | 변경 |
| --- | --- |
| Enemy01_TwistedArm | Zombie 스프라이트 및 전용 Idle / Move / AttackPreparation / Attack / Death 애니메이션 |
| Enemy04_MechanicArcher | ArcherHero 스프라이트 및 전용 애니메이션. 활을 당기는 준비 자세 → 발사 → 기존 후딜 → 대기/이동 |
| Enemy02_ProjectileRobot | 몸통·런처 사이 내부 경계선을 없애고 합쳐진 외형의 바깥에만 아웃라인 표시 |

프리팹은 `Assets/CTJ/Prototypes/Prefabs`, 새 컨트롤러와 클립은 `Assets/CTJ/Prototypes/Animations`에 있습니다. 기존 Catto / MadGhost의 원본 애니메이션은 수정하지 않았습니다.

01의 `AttackWarningModule` 컴포넌트를 끄고, 근접 스킬의 `Preparation Warning` 연결을 비웠습니다. 공격 준비 시간과 준비 자세는 유지하며, 구분용 붉은색 표시만 제거했습니다. **피격 시 흰색 점멸·테두리는 유지됩니다.**

01의 준비 시간은 기존 0.8초, 공격 클립 시작 후 0.05초에 `OnAttackHit`, 0.4초에 `OnAttackFinished`가 실행됩니다. 피해량 8과 중복 타격 방지도 유지합니다. 04의 피해량 14, `Recovery Time = 0.6`도 유지합니다. 04는 클립 종료 이벤트로 후딜을 끝내지 않고 기존 스킬 타이머가 종료합니다.

루트 스케일·콜라이더·공격 범위·HP·쿨타임을 유지하고 비주얼 자식 크기만 기존 몸체 높이에 맞췄습니다. 04의 `FirePoint`는 활에 맞춰 로컬 X를 0.5 → 0.7로 옮겼습니다. 원본 PNG와 기존 슬라이스 ID/피벗은 유지하며, 사용하는 시트 4개는 픽셀 아트가 흐려지지 않도록 Point / 무압축 / Mipmap Off로 설정했습니다.

## 인스펙터에서 조정할 곳

- 외형: `Visual/HitVisual`의 `Animator`. `Enemy01_Zombie` / `Enemy04_ArcherHero` 컨트롤러를 사용합니다. Animator가 매 프레임 스프라이트를 갱신하므로 SpriteRenderer의 그림 하나만 바꾸면 다시 덮어씁니다.
- 04 준비/복귀 상태: `ProjectileAttack → EnemyProjectileSkill → Preparation State Name / Idle State Name`. 각각 `Base Layer.AttackPreparation`, `Base Layer.Idle`입니다. 다른 원거리 적은 기본 빈 값이므로 기존 동작을 유지합니다.
- 02 외곽선: `OutlineModule → EnemyOutlineModule → Outer Silhouette Only`를 켰습니다. `Width`는 화면 픽셀 기준 두께, `Normal Color / Hit Color / Hit Duration`은 기존과 동일합니다.
- 02 방식은 현재처럼 **불투명 파츠, 같은 Sorting Layer/SortingGroup, Simple·Full Rect 스프라이트**를 대상으로 합니다. 반투명 파츠나 별도 SortingGroup을 섞으면 뒤의 선이 비칠 수 있으므로 해당 구성에서는 별도 검토가 필요합니다. 현재 02의 구성은 조건을 충족합니다.

## 직접 테스트하는 순서

1. `CTJ_MapTestScene`을 열고 기존 01·02를 확인합니다. 04도 확인하려면 `Enemy04_MechanicArcher.prefab`을 바닥 위에 임시 배치합니다. 현재 저장된 씬에는 04 인스턴스가 없습니다.
2. Play 후 기존 플레이어 생성/타깃 연결 방식을 사용합니다. 01이 Zombie로 배회·추격하고, 공격 준비 중에는 원래 색을 유지하며 팔을 들어 올리는지 확인합니다. 공격에 맞으면 기존 피해가 전달되어야 합니다.
3. 04의 공격 거리에 들어갑니다. 활 당기기 → 발사 → 약 0.6초 후딜 → 다음 행동 순서를 확인합니다. 준비 도중 01·04를 때리면 공격이 취소되고 대기 자세로 돌아와야 합니다.
4. 01·04의 체력을 소진시켜 각각 새 사망 애니메이션을 확인합니다. 피해/HP 처리는 기존 NewHealthModule 경로를 그대로 사용합니다.
5. 02의 앞·위쪽을 이동해 런처를 회전시키고 전개/수납도 확인합니다. 몸통 위의 장식·회전축·런처가 겹치는 내부에는 선이 없어야 하고, 밖으로 튀어나온 실루엣만 붉게 둘러져야 합니다. 피격 때도 같은 외곽선만 잠시 흰색으로 변해야 합니다.
6. Play를 종료합니다. 임시 배치한 04를 유지할지는 씬 저장 전에 결정하면 됩니다.

## 실제 검증 결과

- Unity 재컴파일 완료: 오류 0. 검사 시 Console 오류/경고 없음. 셰이더 컴파일 오류 없음.
- 세 프리팹 Missing Script / 직렬화 참조 누락 검사 통과. 새 클립의 모든 스프라이트 키가 지정한 Zombie 또는 ArcherHero 시트를 참조합니다.
- 독립 Play 씬에서 01·04의 준비 자세, 피해량, 공격 종료, 피격 취소, 피격 표시, 사망 애니메이션 통과. 01 중복 이벤트가 피해를 반복하지 않고, 04의 애니메이션이 0.6초 후딜을 앞당기지 않는 것도 확인했습니다.
- 전체 CTJ 적 6종의 아웃라인 피격/회복/연타/활성화/제거/렌더러 동기화 검증 통과.
- 실제 URP 렌더링에서 02의 아웃라인 Off/On 이미지를 비교했습니다. 런처 각도 0° / 45° / -90° 모두 **내부 파츠 픽셀 변경 0개**, 바깥 테두리 픽셀 변경은 각각 1,312 / 1,331 / 1,260개였습니다. 전개·수납·피격 색도 캡처 확인했습니다.
- `CTJ_MapTestScene`을 저장 없이 다시 열었으며 미저장 변경이 없습니다. 사용자가 저장한 씬 파일의 SHA-256이 테스트 전후 동일합니다. 기존 사용자 변경인 `EnemyAttackState.cs` 끝의 빈 줄도 보존했습니다.

검증 스크립트: `VerifyEnemyArtRefresh.cs`의 `RunEdit`, `RunPlay`, `RenderProof`, 기존 `VerifyEnemyOutline.cs`의 `RunPlay`. Play 검증은 `VerifyEnemy01.BeginPlay`가 만든 독립 씬 전용입니다. 테스트가 종료되면 `StopPlay`와 `RestoreScene`으로 돌아옵니다. 생성 도구 `ConfigureEnemyArtRefresh.Apply`는 애니메이션을 다시 구성하므로, 이후 직접 클립을 수정했다면 무심코 재실행하지 마세요.

## 사용한 문법과 이유 — 학습용

### 1. 스프라이트 키프레임과 애니메이션 이벤트

`ObjectReferenceKeyframe`은 시간마다 Sprite 같은 Unity 오브젝트 참조를 교체하는 키입니다. 이를 `AnimationUtility.SetObjectReferenceCurve`로 `SpriteRenderer.m_Sprite`에 연결했습니다. 직접 매 프레임 스프라이트를 바꾸는 런타임 코드를 추가하지 않고 Animator를 재사용합니다.

근접 피해는 기존 `AnimationEvent`의 `OnAttackHit`에서 발생합니다. 반면 원거리 발사는 스킬이 직접 처리하므로 04 클립에는 피해/종료 이벤트를 넣지 않았습니다. 같은 발사나 종료가 중복 실행되는 것을 막기 위한 구분입니다.

### 2. 프레임마다 다른 피벗 위치 보정

제공된 시트의 자동 슬라이스는 각 프레임의 잘린 크기가 다릅니다. 중앙 피벗만 믿으면 발이 들리거나 몸이 좌우로 흔들립니다. `sprite.rect.position + sprite.pivot`에서 원본 셀의 공통 발 위치를 빼서 로컬 위치 키에 기록했습니다.

위치 키의 탄젠트를 `float.PositiveInfinity`로 지정하면 프레임 사이를 부드럽게 보간하지 않고 계단처럼 전환합니다. 그림이 바뀌는 순간에 보정도 같이 바뀌도록 하기 위함입니다. 스케일에는 키를 넣지 않아 기존 피격 크기 변형과 충돌하지 않습니다.

`Func<Sprite, Vector2>`는 “Sprite를 받아 기준 위치 Vector2를 돌려주는 함수”를 전달하는 델리게이트 타입입니다. Zombie 32픽셀 셀과 ArcherHero 64픽셀 셀의 기준 계산만 다르게 전달하고 클립 생성 코드는 공유합니다. 이 코드는 `Tools~` 안의 제작 도구이며 게임 실행 코드가 아닙니다.

### 3. 선택적인 Animator 상태 이름

`[SerializeField] private string`은 필드를 외부 코드에 공개하지 않으면서 인스펙터에 저장합니다. 추가한 상태 이름을 빈 문자열로 기본 설정하고 `string.IsNullOrEmpty`로 검사하여, 설정한 04만 새 애니메이션을 실행합니다.

`protected override`에서는 부모 스킬의 종료/취소 처리를 `base.OnEndSkill()` / `base.OnStopSkill()`로 실행한 뒤 대기 자세로 복귀합니다. 사망 상태에서는 복귀를 건너뛰어 Death 애니메이션을 덮어쓰지 않습니다.

### 4. 합쳐진 외곽선과 셰이더

02는 각 파츠의 **바깥 알파 경계**를 구하고, 그 선들을 모든 원본 파츠보다 낮은 `sortingOrder`에 그립니다. 내부에서 겹치는 선은 앞의 불투명 원본 파츠가 덮으므로, 결과적으로 합쳐진 실루엣 밖의 선만 보입니다. 런타임 RenderTexture나 추가 카메라는 사용하지 않습니다.

HLSL의 `max`로 주변 8방향 알파 중 가장 큰 값을 구하고 `(1 - centerAlpha)`를 곱해 원본 바깥쪽에만 선을 남깁니다. `ddx` / `ddy`는 인접 화면 픽셀에 따른 UV 변화량으로, 확대 배율과 관계없이 화면 픽셀 단위 두께를 계산하는 데 사용합니다. 바깥 선이 잘리지 않도록 이 모드에서만 사각 메쉬 꼭짓점과 UV를 확장합니다. 원본 Sprite 에셋과 콜라이더 크기는 바뀌지 않습니다.

`MaterialPropertyBlock`은 공용 머티리얼을 복제하지 않고 렌더러마다 폭·UV 범위·외곽 모드 값을 전달합니다. 기존 적은 `Outer Silhouette Only = false`이므로 기존 안쪽 테두리 방식을 유지합니다.
