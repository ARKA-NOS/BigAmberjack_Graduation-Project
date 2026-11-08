# 코인 트레일 작업 기록

작업일: 2026-11-07

## 적용 내용과 조절 위치

`Assets/_Prefabs/Enemies/Effects/EnemyRewardCoin.prefab`의 `EnemyCoinTrail` 컴포넌트에서 조절한다.

- 낙하, 바닥 바운스, 플레이어 중심으로 날아가는 동안 이동 경로를 남긴다.
- 코인 근처는 둥글고 두꺼우며, 멀리 남은 꼬리는 한 점으로 좁아진다. 금색에서 주황색으로 이어지며 가장자리는 살짝 부드럽다.
- 도착하면 새로운 잔상 생성을 멈추고 남은 경로만 짧게 페이드한다. 코인 확대·축소와 소멸 애니메이션, 골드 지급은 기존 순서를 유지한다.
- 현재 일반 적 3종, 테스트 적 Catto/MadGhost, EyeBoss의 보상 모듈이 이 코인 프리팹을 참조하므로 함께 적용된다.
- 다른 팀원의 상자 코인 프리팹에는 컴포넌트를 추가하지 않았다. 컴포넌트가 없는 코인도 기존대로 동작한다.

| 인스펙터 항목 | 기본값 | 의미 |
| --- | --- | --- |
| Trail Material | EnemyCoinTrail | 전용 URP 비조명 머티리얼 |
| Trail Lifetime | 0.45초 | 각 경로 지점이 남는 시간. 커질수록 길어진다. |
| Trail Width | 0.16유닛 | 가장 두꺼운 부분의 너비 |
| Width Profile | 코인 쪽 0.8 → 1 → 꼬리 0 | 가로축 0은 코인, 1은 꼬리 끝. 마지막 값을 0으로 유지하면 뾰족한 끝이 된다. |
| Head Color / Tail Color | 연한 금색 A=0.45 / 주황색 A=0.06 | 코인 쪽과 꼬리 쪽의 색·알파 |
| Min Vertex Distance | 0.035유닛 | 경로 기록 간격. 작을수록 매끄럽고 정점이 많아진다. |
| Collect Fade Duration | 0.16초 | 도착 후 남은 트레일의 페이드 시간 |
| Sorting Order Offset | -1 | 코인 그림보다 한 단계 뒤에 그린다. |

길이는 고정 유닛 수가 아니라 이동 속도와 유지 시간에 따라 달라진다. 일정하게 초속 16유닛으로 이동하면 0.45초의 경로는 약 7.2유닛이다. 실제 검증에서는 가속·도착을 포함해 최대 약 5.62유닛이 측정됐다. 설정을 바꿨을 때는 새 코인을 생성해 비교한다.

`EnemyCoinTrail`을 끄거나 제거하면 연출만 빠진다. 런타임의 `CoinTrail` 자식은 컴포넌트가 관리하며 보상 계산을 맡지 않는다. 기존 파티클 시스템과 공용 풀의 변경은 없다.

## 직접 테스트하는 과정

두께·알파 피드백 반영: 최대 너비는 `0.32 → 0.16`으로 절반, 코인 쪽 알파는 `0.85 → 0.45`, 꼬리 알파는 `0.15 → 0.06`으로 낮췄다. 새 컴포넌트의 코드 기본값과 현재 코인 프리팹에 저장된 값을 함께 수정했다.

1. 기존 플레이어와 CurrencyManager가 정상 동작하는 테스트 씬에서 적을 처치한다. 현재 씬을 변경했다면 먼저 직접 저장한다.
2. 코인이 흩어져 낙하하고 바닥에서 튀어 오를 때 금색 꼬리가 이어지는지 본다.
3. 코인이 플레이어 중심으로 가속할 때 꼬리가 길어지며, 먼 끝은 한 점으로 좁아지는지 본다. 코인이 날아오는 동안 플레이어도 이동해 본다.
4. 플레이어에게 도착하면 새 꼬리가 생기지 않고 약 0.16초 안에 잔상이 사라지는지 본다. 도착 확대·축소와 소멸 애니메이션도 확인한다.
5. 코인이 소멸할 때 골드가 기존 보상 총액만큼 증가하고 중복 지급되지 않는지 확인한다.
6. 더 긴 꼬리가 필요하면 프리팹의 `Trail Lifetime`을, 더 굵은 꼬리가 필요하면 `Trail Width`를 올리고 다시 적을 처치해 비교한다.
7. `EnemyCoinTrail`을 꺼서 코인 이동·소멸·보상이 그대로 유지되는지 확인한다. 테스트가 끝나면 원하는 설정으로 돌린다.

## 수행한 검증

- 두께·알파 피드백 반영 후 재컴파일 오류·경고 0건, 프리팹 참조 정상. 실제 이동 렌더링으로 너비 0.16과 낮아진 알파를 확인하고 미리보기 이미지를 갱신했다.
- Unity 6000.3.22f1에서 새 C# 스크립트 컴파일 및 코인 프리팹의 스크립트·머티리얼·셰이더 참조 확인.
- URP에서 실제 이동 프레임을 렌더링하여 직선과 곡선의 물방울 형태 확인. `CoinTrailPreview.png`는 검증용 경로를 그린 화면이며 실제 맵 화면은 아니다.
- 저장되지 않는 빈 씬에서 코인 4개를 실행. 그중 1개는 트레일을 꺼 두었다. 낙하·바운스·자석 이동 중 잔상, 도착 시 방출 중단, 페이드 후 정점 정리 확인.
- 이동 중 컴포넌트를 껐다 켜도 이전 경로가 지워지고 TrailRenderer가 중복 생성되지 않음.
- 3골드씩 4개, 합계 12골드가 지급되었으며 변경 이벤트도 정확히 4회. 도착 전 지급 없음.
- 검증 중 Unity 콘솔 로그 0건. 원래 `Assets/_Scenes/Proto_MainHome.unity` 씬으로 복귀했으며 저장하지 않음.

재검증 보조 코드는 `Tools~/CTJ/VerifyCoinTrail.cs`에 있다. Unity가 프로젝트를 열고 자동화 연결이 활성화된 경우 다음 순서로 **한 명령씩 완료를 확인한 뒤** 실행한다. 현재 씬이 저장되지 않았거나 여러 씬이 열려 있으면 시작을 거부한다.

```powershell
unity cmd run_script --file 'Tools~/CTJ/VerifyCoinTrail.cs' --entry VerifyCoinTrail.CheckAssets --result-only
unity cmd run_script --file 'Tools~/CTJ/VerifyCoinTrail.cs' --entry VerifyCoinTrail.BeginPlay --result-only
# Unity가 Play Mode로 전환된 다음 실행한다.
unity cmd run_script --file 'Tools~/CTJ/VerifyCoinTrail.cs' --entry VerifyCoinTrail.RunPlay --result-only
unity cmd run_script --file 'Tools~/CTJ/VerifyCoinTrail.cs' --entry VerifyCoinTrail.RunVisualPlay --result-only
unity cmd run_script --file 'Tools~/CTJ/VerifyCoinTrail.cs' --entry VerifyCoinTrail.StopPlay --result-only
# Edit Mode 복귀가 완료된 다음 실행한다.
unity cmd run_script --file 'Tools~/CTJ/VerifyCoinTrail.cs' --entry VerifyCoinTrail.RestoreScene --result-only
```

검증이 실패해도 `StopPlay`와 `RestoreScene`으로 원래 씬에 복귀한다. 프리팹이나 씬 에셋을 저장하는 테스트는 아니다.

## 사용한 문법과 이유

두께·알파 조정에는 새 문법을 추가하지 않았다. 기존 `float` 필드와 `new Color(r, g, b, a)`의 네 번째 값인 알파를 조절했다. 알파는 0에 가까울수록 투명하다. Unity가 프리팹에 저장된 직렬화 값을 우선 사용하므로 코드 기본값뿐 아니라 프리팹도 함께 바꿨다.

- `[SerializeField]`, `[Min]`, `[Tooltip]`: 필드는 외부 코드에서 직접 바꾸지 않으면서 인스펙터에서 조절하고, 유효 범위와 설명을 제공한다.
- `[DisallowMultipleComponent]`: 코인 하나에 트레일 관리자가 중복 부착되는 것을 방지한다.
- `AnimationCurve`와 `Keyframe`: 일정한 너비의 선 대신 코인 근처가 볼록하고 끝이 0인 모양을 만든다. `Gradient`는 경로를 따라 색과 알파를 바꾼다.
- `TrailRenderer`: Unity가 이동 경로의 정점과 수명을 관리한다. 개별 입자를 뿌리는 파티클보다 연결된 꼬리를 표현하기에 적합하다. [Unity 공식 Trail Renderer 설명](https://docs.unity3d.com/6000.0/Documentation/Manual/class-TrailRenderer.html)
- `MaterialPropertyBlock`과 `Shader.PropertyToID`: 공유 머티리얼을 복제하거나 전체 코인의 색을 변경하지 않고, 도착한 코인 하나의 트레일만 페이드한다.
- `Mathf.SmoothStep`: 페이드의 시작과 끝을 부드럽게 연결한다.
- `GetComponent<EnemyCoinTrail>()`와 null 검사: 기존 코인은 선택적으로 트레일을 사용한다. `BeginTrail`/`StopTrail` 호출만 연결해 보상·이동 로직과 분리한다.
- `OnDisable`/`OnDestroy`: 비활성화 시 경로를 비우고, 컴포넌트를 제거하면 만들어 둔 자식도 정리한다.
- 셰이더의 `TEXCOORD0`와 `smoothstep`: 트레일 너비 방향의 UV로 가장자리 알파를 조금 낮춘다. 길이 방향의 끝 모양은 너비 커브가 담당한다.

## 혈흔 풀링 확인 — 수정 없음

현재 혈흔은 공용 오브젝트 풀을 사용하지 않는다.

- `Assets/_Scripts/Agent/Enemies/EnemyParticleModule.cs`의 `PlayHit`에서 선택한 ParticleSystem 프리팹을 `Instantiate`한다. `_activeEffects`는 살아 있는 효과 수를 제한하는 목록이며, 재사용 풀이 아니다.
- `Assets/_Scripts/Agent/Enemies/EnemyHitParticle.cs`는 수명이 끝나거나 파티클이 모두 종료되면 `Destroy(gameObject)`한다. 동시 효과 상한을 넘은 오래된 효과도 제거한다.
- `EnemyBloodParticle`의 입자 배열은 해당 효과 인스턴스 내부의 계산·표시 버퍼다. 혈흔 GameObject를 다른 피격에 재사용하는 공용 풀은 아니다.
- 프로젝트의 `Assets/_Scripts/VFXSystem/Manager/CreateManager.cs`에는 `PoolManagerSO.Pop<PoolableVfx>()`와 `Push()`를 사용하는 별도 VFX 경로가 있지만, 현재 혈흔 생성 경로에는 연결되어 있지 않다.

요청에 따라 혈흔 코드·프리팹 및 공용 풀은 변경하지 않았다.
