# 공통 넉백 수치 조정 — 2026-10-26

CTJ_MapTestScene에 배치된 Enemy01_TwistedArm의 사용자 설정을 기준으로, 현재 적 프리팹 6종(Catto, MadGhost, 01, 02, 04, RobotEyeBoss)의 Knockback Speed를 2, Knockback Duration을 0.15초로 맞췄습니다. 새 EnemyHitReactionModule을 추가할 때의 기본값도 같습니다.

씬의 Enemy01 덮어쓰기 값은 이미 동일하여 수정하지 않았습니다. Enable Knockback, Hit Stun Duration, 점멸·아웃라인·스케일 변형 등 다른 값은 변경하지 않았습니다. 따라서 넉백이 꺼진 02번 터렛과 보스는 계속 밀려나지 않습니다. 이전 문서의 6.4/7.04 등은 과거 조정 기록이며 현재 프리팹 기준값은 2/0.15입니다.

## 직접 확인

1. 각 적 프리팹의 HitReactionModule에서 Speed = 2, Duration = 0.15인지 확인합니다.
2. CTJ_MapTestScene에서 Play 후 일반 적을 공격하거나 `Test/Take 2 Damage (Play Mode)`를 사용합니다.
3. 0.15초 동안 짧게 밀려나는지 확인합니다. Hit Stun Duration은 별도이므로 넉백 이동이 끝나도 설정된 경직 시간까지 행동은 멈출 수 있습니다.
4. 터렛과 보스는 기존처럼 제자리에 있어야 합니다.

## 문법 설명

새 문법이나 API는 추가하지 않았습니다. `2f`, `0.15f`의 `f`는 float 리터럴이라는 뜻입니다. `[SerializeField]`가 있는 필드는 프리팹에 저장된 값이 코드 초기값보다 우선하므로, 코드의 기본값과 기존 프리팹 값을 함께 바꿨습니다. 오브젝트·머티리얼·스탯 참조는 변경하지 않았습니다.
