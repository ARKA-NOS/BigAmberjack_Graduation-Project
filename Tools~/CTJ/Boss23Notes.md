# Enemy23 공정 통제 드론

최근 추가 조정: **[Boss23VisualFixNotes.md](Boss23VisualFixNotes.md)** (2026-11-24, 강화 연사 높이·공중 표시·레이저 단면·픽셀 데미지 숫자).

강화 패턴 기준: [Boss23EnhancedGifNotes.md](Boss23EnhancedGifNotes.md) (런처 교대·공중 4회 사격·폭격 범위 공유).

돌진 속도·연사 부유: [Boss23RushHoverNotes.md](Boss23RushHoverNotes.md).

거리·판정 기준: **[Boss23SpacingNotes.md](Boss23SpacingNotes.md)** (돌진 앞쪽 절반 판정·연사 거리 유지·공중 고도 4.5유닛·포격 범위 축소).

돌진 경고의 최초 연결: [Boss23ChargeWarningNotes.md](Boss23ChargeWarningNotes.md). 현재 판정과 높이는 위 최신 문서를 따른다.

기본 동작 문서: **[Boss23GifReferenceNotes.md](Boss23GifReferenceNotes.md)** (2026-11-24, 첨부 GIF 참고 개선).

기존 가감속·후딜·반격 시간의 설명은 [Boss23MotionNotes.md](Boss23MotionNotes.md)에 있다. 이번 GIF 조정은 기본 패턴만 적용하며 강화는 기존 상태를 유지한다.

고정된 전체 시간에 클립을 늘려 맞추는 설정은 최신 요청으로 대체됐다. 조정 위치, 실제 검증 결과, 직접 테스트 순서와 문법 설명은 위 문서를 기준으로 한다. 노션과 테스트 씬은 수정하지 않았다.

| 항목 | 현재 규칙 |
| --- | --- |
| 이동 | 지상 최고 2.4유닛/초, 공중 최고 10. 짧은 가감속·비주얼 보간 |
| 패턴 사이 휴식 | 기본 자세에서 1.25초 정지, 피해 수신 가능 |
| 피격 | HP·시각 피드백만 적용, 넉백·경직·공격 취소 없음 |
| 추적 | 6.5유닛, 피해 18 / 강화 22. 돌진 모션 2배속, 동작 후 바로 Idle. 강화 재조준 후 2연속 |
| 연사 | 탄당 10. 일반 0.32초 간격 7발 / 강화 0.144초 간격 12회·3방향 |
| 공중 전환 | 변형 후 상승, 기본 도착 후 조준 1발 / 강화 상승 시 5방향·1.2배 진행, 탄당 10 |
| 사선 레이저 | 피해 24 / 28, 강화 속도 1.2배. 첫 발사 후 쉬고 재경고 없이 반대 방향 발사 |
| 표적 폭격 | 3발 / 6발, 피해 22 / 26, 경고 1.65 / 1.15초, 반경 75 / 90px (28 PPU) |
| 무작위 선택 | 지상 추적·연사·공중 전환 → 공중 레이저·폭격 |
| LAST STAND | 35% 이하에서 기존 편도 레이저 1회 후 강화 규칙 사용 |

현재 프리팹: Assets/_Prefabs/Enemies/Bosses/Enemy23/Boss23_ProductionControlDrone.prefab

현재 모션 설정 도구는 SetupBoss23Timeline.UpdateMotion이다. 실행 검증은 VerifyBoss23Rework.CheckAssets / RunGround / RunAir / RunFlow를 사용한다. 예전 SetupBoss23Rework / SetupBoss23Feedback는 이전 기획의 필드가 포함되어 있어 현재 프리팹에 다시 실행하지 않는다.

과거 변경 기록은 [Boss23FeedbackNotes.md](Boss23FeedbackNotes.md), 레이저 표시·판정·머티리얼 원리는 [Boss23FanLaser.md](Boss23FanLaser.md)에 있다. 최초 구현 결과 JSON(Boss23ReworkVerification.json)과 직전 피드백 결과 JSON(Boss23FeedbackVerification.json)은 당시 설정의 기록이며 최신 결과는 Ground/Air/FlowTimelineVerification.json이다.
