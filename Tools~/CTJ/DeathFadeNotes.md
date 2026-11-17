# 적 사망 마지막 프레임 페이드

## 동작

- HP가 0이 되는 프레임에 기존처럼 Death 애니메이션과 피격 반응을 시작한다. 처치 수와 코인 보상도 기존 피격 반응 종료 시점에 처리한다.
- Death 상태가 있는 Enemy01, Enemy04, Catto, MadGhost는 클립 종료를 기다린 뒤 마지막 프레임을 다시 샘플링하고 Animator를 멈춘다. 그 그림을 유지한 채 SpriteRenderer의 원래 알파에서 0까지 기본 0.2초 동안 페이드하고 제거한다.
- Death Animator가 없는 Enemy02와 RobotEyeBoss는 기존 `Death Despawn Delay`가 지난 뒤 같은 페이드를 수행한다. 피격 반응이 더 길면 반응 종료를 우선한다.
- 사망 페이드 시간은 각 적 프리팹의 EnemyBase `Death Fade Duration`에서 조절한다. 기본값은 모두 0.2초다.

## 직접 확인

1. 플레이어와 적이 있는 테스트 씬에서 Enemy01 또는 Enemy04를 처치한다. 마지막 타격 즉시 사망 동작이 시작되고, 끝 그림이 바뀌지 않은 채 빠르게 투명해진 다음 적이 사라져야 한다.
2. Enemy02도 처치한다. Death 클립이 없으므로 기존 대기 시간 뒤 포탑의 몸통과 발사기가 함께 페이드해야 한다.
3. 사망 순간 코인과 처치 수가 종전대로 한 번만 발생하는지 확인한다. 코인이나 혈흔 등 적 밖에서 생성된 이펙트는 적의 페이드와 무관하게 남아야 한다.
4. 빠르기가 마음에 들지 않으면 각 적 프리팹의 `Death Fade Duration`을 0.15~0.3초에서 조절한다. Play 모드의 인스펙터 변경은 종료 후 되돌아간다.

## 검증 범위

- 여섯 적 프리팹에 `Death Fade Duration: 0.2`가 직렬화된 것을 확인했다. Death Animator 참조는 4종에 있고 Enemy02와 RobotEyeBoss에는 없다.
- Death 클립 4종은 루프가 꺼져 있고 마지막 스프라이트 키가 종료 시각에 있다. 따라서 종료 후 `normalizedTime = 1`로 마지막 그림을 샘플링한다.
- 이 작업 환경의 Unity Pipeline 서버에 연결되지 않아 Unity 재컴파일과 Play 실행은 수행하지 못했다. 코드·프리팹의 참조 관계와 `git diff --check`를 확인했다.

## 사용한 문법과 이유

- `IEnumerator`와 `yield return null`: Animator가 실제 마지막 프레임에 도달할 때까지 프레임마다 기다린다. 프리팹마다 사망 클립 길이가 달라 고정된 초 수만 기다리지 않는다.
- `AnimatorStateInfo.normalizedTime`: Death 상태의 진행률이다. 1 이상이면 클립 끝에 도달한 것으로 판단한다. `Animator.Play(..., 1f)`와 `Update(0f)`로 마지막 키를 적용한 뒤 `Animator.speed = 0`으로 화면을 고정한다.
- `GetComponentsInChildren<SpriteRenderer>(true)`: 몸통과 발사기처럼 그림이 여러 개여도 모두 페이드한다. `true`는 비활성 자식도 포함한다.
- `Mathf.SmoothStep`: 알파가 시작과 끝에서 급격히 꺾이지 않도록 보간한다. 각 스프라이트의 원래 알파를 곱해 반투명 부위의 비율도 유지한다.
