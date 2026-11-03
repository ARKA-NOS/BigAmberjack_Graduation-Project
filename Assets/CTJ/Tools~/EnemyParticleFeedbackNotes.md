# 피격 파티클 모듈 · 혈흔 적용 · 넉백 조정

2026-11-04 정리: 아래의 시트 기반 혈흔 원본·프리팹·제작/검증 도구·캡처는 삭제했습니다. 현재 혈흔은 [EnemyPolygonBloodNotes.md](EnemyPolygonBloodNotes.md)의 폴리곤 방식만 사용합니다. 아래 내용은 학습용 작업 기록입니다.

후속 수정: 혈흔 5종 무작위 선택과 마지막 피격 후 사망 순서는 [EnemyBloodRandomDeathNotes.md](EnemyBloodRandomDeathNotes.md)를 참고하세요. 아래의 단일 혈흔 설명은 최초 적용 기록입니다.

2026-11-03. 수정은 `Assets/CTJ`에 한정했습니다. 기존 팀원 코드·맵·프로젝트 설정은 수정하지 않았고 패키지도 추가하지 않았습니다. 후속 요청에 따라 `NEw pack blood`를 적용하고, 넉백을 약 0.25유닛으로 줄였습니다. 아래 내용은 후속 요청까지 반영한 현재 설정입니다.

## 현재 적용된 결과

Catto, MadGhost, Enemy01(좀비), Enemy02(터렛), Enemy04(아처), RobotEyeBoss에 `ParticleModule` 자식을 추가했습니다. 각 모듈에는 아래 3개의 교체 가능한 ParticleSystem 프리팹이 연결되어 있습니다.

| 프리팹 | 임시 표현 | 대략적인 표시 시간 |
| --- | --- | --- |
| `Prototypes/Particles/HitImpact.prefab` | 맞은 지점에 밝은 별 모양 임팩트 | 0.13초 |
| `Prototypes/Particles/HitSlash.prefab` | 방향을 따라 짧게 회전하는 베기 잔상 | 0.18초 |
| `Prototypes/Particles/HitBlood.prefab` | 새 혈흔 시트의 피가 퍼졌다 사라지는 애니메이션 | 0.4초 |

임팩트와 슬래시는 프로젝트에 이미 있는 `Essential 2D Particle FX`의 Star / Slash 텍스처를 읽기 참조합니다. 혈흔은 `Assets/CTJ/Assets/NEw pack blood/1_100x100px.png`를 사용합니다. 기존 Sparks 연결을 Blood로 교체했으며, `HitSparks.prefab`은 다시 연결할 수 있도록 보존했습니다. 머티리얼과 ParticleSystem 프리팹은 CTJ에 있습니다. 공격 종류별 베기/둔기 구분 정보가 아직 이 모듈에 전달되지 않으므로, 현재 Slash는 설정된 적의 모든 유효 타격에 나옵니다. 적별로 끄거나 교체할 수 있습니다.

혈흔은 한 번에 파티클 1개를 내보내고, Texture Sheet Animation으로 6×6 시트의 앞 24프레임을 한 번 재생합니다. 뒤쪽 빈 칸과 다시 붉은 덩어리가 나타나는 프레임은 제외했습니다. 원본 PNG와 기존 Sprite 슬라이스/ID는 유지하고, 해당 시트의 Import 설정만 Point / Mipmap 끔 / 압축 없음으로 바꿔 픽셀 모양을 살렸습니다. 혈흔에는 별도 알파 페이드를 추가하지 않고 시트 자체의 소멸 프레임을 사용합니다.

기존 공용 `VfxModule` / `PlayParticleVFX`도 확인했습니다. 그쪽은 등록된 자식 효과의 재생을 제공하며, 별도 풀링 경로는 이벤트 채널과 풀 관리자 연결이 필요합니다. 이번에는 기존 적 모듈 구조 안에서 피격 위치·치명타격·생성 수·독립 수명을 처리하는 작은 모듈을 구성했습니다. 전역 매니저나 새로운 추상 클래스/인터페이스는 추가하지 않았습니다.

## 넉백과 스케일

| 대상 | 설정 | 변경 |
| --- | --- | --- |
| 이동형 적 4종 | Knockback Speed | 6 → **2.5** |
| 이동형 적 4종 | Knockback Duration | **0.2초 유지** |
| 일반 적 5종 | Scale Pulse Amount | 0.2 → **0.4** |
| 일반 적 5종 | Scale Pulse Duration | 0.2 → **0.24초** |

이동형 적은 Catto / MadGhost / Enemy01 / Enemy04입니다. 터렛의 넉백 끔, 보스의 넉백·스케일 끔은 유지했습니다. 기존 경직 시간·공격 취소·슈퍼아머 규칙은 바꾸지 않았습니다.

넉백은 처음에 빠르게 밀린 뒤 감속합니다. `속도 × 시간 ÷ 2`로 계산한 기준 거리는 **0.25유닛**입니다. 막히지 않은 독립 물리 검증에서 이동형 4종의 좌우 거리는 모두 **0.2705유닛**, 별도 좀비 검증에서는 **0.2646유닛**이었습니다. 실제 거리는 피격이 들어온 물리 프레임과 벽 등에 따라 조금 달라집니다. 이전 강화 값인 Speed 6을 사용하던 설명보다 이 값이 우선합니다.

Scale Pulse Amount의 50~100%를 무작위로 사용하므로 이제 각 타격에 약 **20~40%**의 가로/세로 변형이 생깁니다. 그림 전용 Transform만 바뀌어 콜라이더·발사점·좌우 방향 전환에는 영향이 없습니다. 연타해도 변형이 누적되지 않습니다.

## 구조와 이벤트 순서

```text
Enemy
├─ Visual
├─ HitReactionModule    넉백 / 경직 / 점멸 / 펀치 스케일
└─ ParticleModule       EnemyParticleModule: 재생할 효과 목록과 생성 위치

Scene
├─ HitImpact(Clone)     EnemyHitParticle: 자기 수명이 끝나면 제거
├─ HitSlash(Clone)
└─ HitBlood(Clone)
```

1. `EnemyBase.ApplyDamage()`가 이번 타격의 피해 데이터·좌표·방향·법선을 임시 보관합니다.
2. 기존 NewHealthModule이 실제 피해를 계산하고 OnHealthChanged를 발행합니다.
3. 체력 변화량이 음수일 때만, 사망 처리 전에 `EnemyBase.DamageReceived`를 한 번 발행합니다.
4. ParticleModule이 이벤트를 받아 월드에 효과를 만듭니다.
5. 기존 사망/경직 처리로 이어집니다. 적이 즉시 제거되어도 생성된 파티클은 끝까지 재생됩니다.

체력이 실제로 줄지 않은 타격이나 보스 무적, 0/잘못된 피해 입력에는 효과가 나오지 않습니다. 슈퍼아머는 피해 자체를 막지 않으므로 파티클은 재생되고, 경직·밀림·공격 취소만 기존대로 차단됩니다. NewHealthModule을 직접 호출해 체력만 바꾸는 경로에는 피격 위치 정보가 없으므로 이 모듈은 발동하지 않습니다. 전투에서는 기존 `EnemyBase.ApplyDamage` / IDamageable 경로를 사용하세요.

피격 이벤트는 모듈 활성화 시 구독하고 비활성화/제거 시 해제합니다. `ParticleModule`을 끄거나 삭제해도 기존 HP·블링크·넉백은 동작합니다. 이미 생성된 효과는 자신의 수명만큼 재생한 후 정리됩니다.

## Inspector 조정 위치

적 프리팹의 `ParticleModule → EnemyParticleModule`:

| 항목 | 의미 |
| --- | --- |
| Hit Effects | 효과 항목을 추가/제거하는 목록. 각 항목의 Enabled로 개별 표시를 켜고 끔 |
| Prefab | 루트에 ParticleSystem이 있는 프리팹. 하위 ParticleSystem도 함께 재생 |
| Scale | 해당 효과의 크기 배율 |
| Offset | 효과 회전을 기준으로 한 위치 보정 |
| Align To Hit Direction | 피격 방향으로 효과의 로컬 +X를 향하게 함 |
| Angle Offset | 방향 기준 추가 회전각. 현재 Slash는 35도 |
| Effect Scale | 적별 전체 효과 크기. 기본 1, 보스 1.4. 적 루트의 2.5배 스케일 등을 자동 상속하지 않음 |
| Use Hit Point | 켜면 전달받은 피격 좌표, 끄면 Fallback Origin 위치 사용 |
| Fallback Origin / Max Hit Point Distance | 좌표가 없거나 비정상적·지나치게 멀 때 사용할 기준점과 허용 거리 |
| Max Concurrent Effects | 적 한 마리의 동시에 남겨둘 효과 오브젝트 수. 기본 12. 초과하면 오래된 것부터 제거 |
| Max Effect Lifetime | 안전한 수명 상한. 기본 5초. 보통은 모든 자식 파티클이 끝나면 더 빨리 제거 |

**커스텀 효과 연결:** CTJ 안에 새 ParticleSystem 프리팹을 만들고 Hit Effects의 Prefab에 연결하면 됩니다. 피/기계 파편 등은 새 프리팹의 Start Color, Burst, Gravity Modifier, Velocity over Lifetime, 텍스처를 조정하세요. 새 코드나 SO가 필수는 아닙니다. Renderer의 Sorting Layer / Order도 각 프리팹에서 바꿀 수 있습니다. 임시 효과는 어두운 배경에서도 보이는 Unlit 머티리얼을 사용하며, Sorting Order는 임팩트/슬래시 30, 혈흔 31입니다.

**혈흔 조정:** 적의 `ParticleModule → Hit Effects → Blood (NEw pack blood) → Scale`로 해당 적의 혈흔 크기를 바꿉니다. 전체 공통 크기는 `HitBlood.prefab → Particle System → Start Size`의 2.8~3.4, 재생 시간은 Start Lifetime의 0.4초입니다. 크기는 투명 여백을 포함한 시트 한 칸 기준입니다. 기계형 적에 피 대신 파편을 쓰려면 이 항목의 Prefab을 `HitSparks`로 바꾸면 됩니다.

피격 효과는 일회 재생용입니다. 런타임 복제본의 Loop를 끄고 Stop Action을 None으로 바꿔 루트가 먼저 사라져 자식 파편까지 끊기는 것을 방지합니다. 원본 프리팹에는 이 런타임 변경이 저장되지 않습니다. 기본 프리팹의 Simulation Space는 World이며, 같은 씬의 루트 오브젝트로 생성되므로 적을 따라 끌려가거나 적의 스케일/반전을 상속하지 않습니다. 씬 언로드 때는 같이 정리됩니다.

새 생성을 연타마다 수행하는 소규모 프로토타입 구성입니다. 무제한 누적은 적별 동시 생성 제한과 수명 상한으로 막습니다. 별도 전역 풀링 시스템은 이번에 추가하지 않았습니다.

## 직접 테스트하는 방법

1. 저장한 테스트 씬에서 해당 적과 플레이어를 배치하고 Play합니다. 플레이어로 공격하거나 적 루트 컴포넌트 메뉴의 `Test → Take 2 Damage (Play Mode)`를 사용합니다. 메뉴 테스트는 루트 위치를 타격점으로 전달하므로 몸체 중앙에서 보고 싶다면 Use Hit Point를 잠시 끄세요.
2. 이동형 적이 약 0.25유닛 밀리고, 그림이 눌렸다가 복원되는지 확인합니다. 거리를 볼 때는 평평하고 벽이 없는 곳에서 피격 직전/직후 루트의 X 좌표를 비교합니다. AI의 후속 이동은 구분하세요. 터렛은 제자리에 있고, 보스는 기존 이동을 유지합니다.
3. 맞은 자리에 임팩트·슬래시·붉은 혈흔이 나오고 약 0.4초 이내에 사라지는지 확인합니다. 적이 죽는 마지막 타격도 확인합니다.
4. ParticleModule의 Hit Effects에서 Slash나 Blood의 Enabled를 껐다 켜서 각각의 차이를 확인합니다. Scale / Effect Scale로 크기를 조정합니다.
5. ParticleModule 컴포넌트 자체를 끄면 새 파티클만 나오지 않고 기존 피격 반응은 유지되어야 합니다. 여러 번 껐다 켜도 한 타격의 효과 수가 늘면 안 됩니다.
6. 빠르게 연타해도 효과가 무한히 쌓이지 않고, 공격을 멈추면 정리되는지 확인합니다. Scene 전환 후에도 이전 효과가 남지 않아야 합니다.
7. 최종 조절은 Play를 종료한 뒤 프리팹에서 합니다. 씬 인스턴스가 피격 수치를 별도로 Override했다면 그 값이 프리팹보다 우선합니다.

## 검증 결과

- Unity 컴파일 오류 없음. 적 6종의 스크립트·오브젝트 참조·파티클 프리팹·셰이더 지원 검사 통과.
- 6종 모두 실제 파티클 방출, 피격 좌표/방향, 월드 크기, 수명 정리, 모듈 해제/재구독, 연타 20회의 생성 제한, 잘못된 좌표 대체, 치명타격/즉시 제거 후 재생 통과.
- 보스 무적 중 파티클 없음, 슈퍼아머 중 파티클 유지/행동 방해 없음, 모듈 제거 후 기존 피격 반응 유지 통과.
- 최종 Speed 2.5 / Duration 0.2에서 이동형 4종 좌우 넉백 0.2705유닛, 별도 좀비 0.2646유닛. 무작위 스케일 범위 20~40%, 연타 후 원본 크기 복원 및 콜라이더 분리 확인.
- 지연된 자식 파티클의 재생 보존, 루프 해제, 원본 프리팹 보존, 최대 수명 제한, 별도 씬 언로드 정리 통과.
- 기존 Enemy01 배회·공격·피격 취소·HP·쿨타임·사망 회귀 검증 통과.
- 혈흔 시트·머티리얼·6종 연결 검사 통과. 실제 URP 카메라로 0.04 / 0.16 / 0.36초 프레임을 렌더링해 퍼짐과 소멸 확인: `EnemyBloodFrames.png`. 전체 피격 화면: `EnemyParticlePreview.png`.

`VerifyEnemyParticles.RunEdit / RunBloodEdit / RunPlay / RunKnockbackDistances / RunParticleLimits / RenderPreview / RenderBloodFrames`가 검사 진입점입니다. 혈흔 적용 후 RunBloodEdit, RunPlay, RunKnockbackDistances, RenderPreview, RenderBloodFrames를 확인했습니다. 나머지는 최초 모듈 구현 당시 검증 기록입니다. `ConfigureEnemyParticles.Apply`와 `ApplyBloodAndKnockback`은 기본값을 다시 쓰는 제작 도구이므로, 이후 직접 조절한 프리팹을 검증할 때는 Verify만 실행하세요. 이 도구들은 `Tools~` 안에 있어 게임 빌드에 포함되지 않습니다.

## 사용 문법과 이유 — 학습용

### event / Action / 구독 해제

`event Action<DamageData, Vector2, Vector2, Vector2>`는 피해 정보 4개를 전달하는 C# 이벤트입니다. EnemyBase는 파티클 구현을 알 필요 없이 피격 사실만 알리고, 필요한 모듈이 구독합니다. `event`이므로 다른 클래스가 마음대로 알림을 발행하거나 전체 구독 목록을 대입할 수 없습니다. `+=`로 구독하고 `-=`로 해제하며, 중복 구독 방지 플래그도 사용합니다. 기존 OnHit은 좌표 정보가 없어 별도 이벤트를 사용했습니다.

### nullable 값 튜플과 try / finally

`(DamageData damage, Vector2 point, Vector2 direction, Vector2 normal)?`는 이번 피해의 네 값을 하나로 묶은 값 튜플입니다. 뒤의 `?`로 "현재 처리 중인 타격 없음"을 null로 표현합니다. `HasValue` 확인 뒤 `Value`를 사용합니다. 새 컨텍스트 클래스나 매 타격 콜백 객체를 만들지 않고 관련 값들을 같이 보관할 수 있습니다.

`try { ApplyDamage(...); } finally { ... }`는 성공하거나 예외가 발생해도 이전 타격 컨텍스트를 복원합니다. 이벤트 안에서 다른 피해 처리가 중첩되어도 바깥쪽 타격의 정보가 덮인 채 남지 않게 합니다. 이벤트를 발행하기 전에 현재 컨텍스트를 비워 같은 타격의 중복 알림도 막습니다.

### Serializable 클래스와 List

`[Serializable] HitEffect`와 `List<HitEffect>`로 Inspector에서 효과 프리팹·크기·방향을 항목별로 편집합니다. 새로운 종류의 파편을 넣을 때 상속 클래스나 코드를 만들지 않고 목록을 추가하면 됩니다. 활성 인스턴스 목록은 남아 있는 효과만 관리하고, 제한을 넘으면 오래된 항목부터 정리합니다.

### ParticleSystem 모듈 접근

`var main = particle.main; main.loop = false;`처럼 main 모듈을 변수로 받은 뒤 설정합니다. ParticleSystem의 모듈 API는 일반 MonoBehaviour 필드와 달리 구조체를 통해 네이티브 시스템을 조작하기 때문입니다. `Play(true)` / `Stop(true, ...)`의 true는 하위 시스템도 처리한다는 의미입니다. `IsAlive(true)`로 지연 재생과 자식 파편까지 끝났는지 확인합니다.

### Texture Sheet Animation / MinMaxCurve — 이번 혈흔에 사용

`var animation = particles.textureSheetAnimation;`으로 파티클의 시트 애니메이션 모듈을 가져옵니다. `numTilesX = 6`, `numTilesY = 6`은 텍스처를 36칸으로 나눈다는 의미입니다. Animator나 별도 프레임 교체 스크립트 없이 ParticleSystem이 UV를 바꾸며 프레임을 재생합니다.

`AnimationCurve.Linear(0f, 0f, 1f, 24f / 36f)`는 수명이 0→100% 진행되는 동안 전체 시트의 앞 24칸만 사용하도록 하는 선형 곡선입니다. `24f / 36f`의 `f`는 실수 나눗셈을 하려는 표기이며, 정수 `24 / 36`이면 결과가 0이 됩니다. `cycleCount = 1`로 반복을 막고 Start Lifetime으로 전체 재생 시간을 조절합니다.

`new ParticleSystem.MinMaxCurve(2.8f, 3.4f)`는 Start Size를 매번 두 값 사이에서 무작위로 선택합니다. 같은 피격 그림이 반복되는 느낌을 줄이는 용도입니다. 이번 적용은 기존 EnemyParticleModule을 재사용하며 새로운 런타임 클래스나 상속 구조를 추가하지 않았습니다.

### 월드 좌표와 회전

`Mathf.Atan2(y, x) * Mathf.Rad2Deg`로 피격 방향의 각도를 구하고 `Quaternion.Euler`로 Z축 회전을 만듭니다. `rotation * offset`은 보정 위치도 같은 방향으로 돌립니다. 부모를 지정하지 않은 Instantiate로 적의 펀치 스케일을 상속하지 않고, `SceneManager.MoveGameObjectToScene`으로 소유 적과 같은 씬에 둡니다.

### 안전한 제거

`Destroy(gameObject)`는 끝난 효과를 정리합니다. 적이 사라져도 파티클 수명 컴포넌트가 자체 Update를 가지므로 정리가 중단되지 않습니다. 동시 수 제한으로 제거할 때는 먼저 SetActive(false)해서 Destroy가 실제로 처리되기 전 프레임에도 화면에 남지 않게 했습니다.
