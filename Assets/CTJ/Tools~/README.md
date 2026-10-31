# CTJ 작업 도구 정리

2026-10-28. Assets/CTJ/Tools~ 안의 완료된 일회성 도구와 검증 산출물을 정리했습니다.

## 삭제한 파일

- 최초 프리팹/애니메이션 생성, 초기 설정, 완료된 체력 마이그레이션, 당시 에셋 검사 도구 12개.
- 예전 공격·이동·피격·재화·보스 패턴 등을 확인하던 과거 검증 스크립트 13개. 이 중 ConfigureEnemyDeath / VerifyEnemyDeath는 제거된 플레이어 타입을 참조하던 파일이었습니다.
- 검증용 PNG 캡처 30개와 이전 Unity 로그 1개.

합계 56개입니다. 생성 결과인 실제 프리팹·스프라이트·애니메이션·머티리얼·씬은 삭제하지 않았습니다. 이 폴더 밖의 게임 코드/에셋에서 삭제 대상에 의존하는 참조는 발견되지 않았습니다.

작업 설명서와 문법 학습 자료는 모두 보존했습니다. 과거 문서에 나오는 삭제된 생성 도구·검증 스크립트·캡처 이름은 당시 작업 기록이며, 현재 실행할 파일 목록은 아래와 같습니다.

## 남긴 도구

| 파일 | 보존한 용도 |
| --- | --- |
| InvokeUnity.ps1 | Unity 상태·컴파일·Console 조회와 필요한 검증 실행 |
| VerifyEnemy01.cs | 독립 검증 씬 시작/종료/원래 씬 복구와 01 확인 |
| VerifyEnemy02.cs | 터렛 전개·수납·조준·발사 검증 |
| VerifyEnemyVerticalFlip.cs | 선택적 상하 반전·천장 터렛 전개/조준/UI/보상 검증 |
| VerifyEnemy04.cs | 최근 변경한 수평 사격·이동 방향·사거리 검증 |
| VerifyEnemy04RangeStability.cs | 다른 발판에서 사거리 경계 떨림 재발 방지 |
| VerifyEnemyArtRefresh.cs | 새 외형의 참조·공격/사망 애니메이션 연결 검사 |
| VerifyEnemyNewHealth.cs | 현재 NewHealthModule 기반 체력·UI·보상 통합 검사 |
| VerifyEnemyCoinReward.cs | 사망 코인 분배·바운스·스프라이트 소멸·지형·지연 지급·씬 정리 정산 검사 |
| VerifyEnemyOutline.cs | 현재 아웃라인의 피격·활성화·제거 검사 |
| VerifyEnemyOuterContour.cs | 위치·회전·수납에 따른 외곽선 누락 재발 방지 |
| VerifyEnemySuperArmor.cs | 현재 슈퍼아머의 준비/공격/후딜 조건 검사 |

일부 검증은 특정 테스트 수치를 전제로 하므로 게임 기획 수치를 조정했다면 기대값부터 확인해야 합니다. 렌더링 캡처 함수는 필요할 때만 실행하며, 다시 생성된 PNG는 확인 후 정리할 수 있습니다.

## 확인 방법

1. 파일 탐색기로 이 폴더를 열어 남긴 도구와 설명서를 확인합니다.
2. 삭제한 파일은 일회성 제작·검증용이며, 적의 실제 컴포넌트는 Scripts와 프리팹에 있습니다.
3. 게임 테스트를 할 때는 CTJ_MapTestScene의 기존 적을 이용합니다. EnemyArtTest / BossPatternTest와 여기에 연결된 EnemyDamageProbe / BossTestControls도 사용 참조가 있어 보존했습니다.

이번 정리는 게임 동작이나 C# 문법을 추가하지 않았습니다. 경로와 참조를 검사한 뒤 지정된 파일만 삭제했습니다.

정리 시점에 남긴 C# 검증 파일 9개를 실행 없이 컴파일했으며 모두 오류 없이 통과했습니다. CTJ_MapTestScene의 파일 해시도 정리 전후 동일했습니다.

이후 코인 보상 작업으로 VerifyEnemyCoinReward.cs와 EnemyCoinRewardNotes.md를 추가했습니다. Backups/EnemyCurrencyRewardModule.Immediate.cs.txt는 사용자 요청으로 보관한 이전 즉시 지급 코드이며, 삭제 대상이 아닙니다.

2026-10-29 코인 수정의 현재 설정·테스트·문법 설명은 EnemyCoinBounceNotes.md에 있습니다. EnemyCoinRewardNotes.md는 최초 페이드 방식의 기록입니다.

2026-10-29 천장 터렛의 설정·코드 사용법·테스트·문법 설명은 EnemyVerticalFlipNotes.md에 있습니다.
