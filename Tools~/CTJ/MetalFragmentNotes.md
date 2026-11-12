기계 적 피격 파편

`HitMetalFragments.prefab`은 혈흔처럼 홈과 돌기가 불규칙한 다각형 본체 메시 4종을 사용한다. 외곽의 가로세로 길이는 비슷하고 정점 수는 8~13개다. Unity 6000.3.22f1에서 한 렌더러에 메시 10개를 전달해 4개만 등록되는 것을 확인했다. 피격마다 생성하는 파티클 시스템 수를 줄이기 위해 본체 렌더러 하나에 4종을 담았다. 작은 부품은 자식 렌더러의 메시 목록에서 꺾쇠, 핀, 육각 와셔 중 하나를 뽑는다. Enemy02와 Enemy04의 `ParticleModule → Hit Effects → Metal Fragments`에서 사용한다.

기본 13~19개 조각이 피격 방향으로 흩어지고, 회전하면서 중력으로 떨어진다. `Size Range`는 `0.08~0.20`이다. 본체 색은 밝기 조정 전의 RGB `0.26, 0.28, 0.31`부터 `0.58, 0.60, 0.63` 사이에서 고른다. 조각마다 25% 확률로 회백색 또는 밝은 회색 부품을 가장자리에 일부 겹친다. 이 부품은 본체와 같은 회전 및 충돌을 공유한다. Ground와 Platform에 부딪힐 때 속도의 일부를 잃으며 튕기고, 1.0~1.5초 뒤 0.25초 동안 페이드아웃한다.

| 조정할 항목 | `EnemyMetalFragmentParticle`의 Inspector 값 |
| --- | --- |
| 조각 수·크기·퍼지는 각도 | Fragment Count, Size Range, Spread Angle |
| 방출 강도와 위로 튀는 정도 | Speed Range, Upward Speed |
| 조각 색과 결합 부품 색 | Dark Metal, Light Metal, Attachment Colors |
| 부품 메시 목록 | Attachment Particles 자식의 Particle System Renderer |
| 부품이 붙을 확률 | Attachment Chance |
| 표면에 부딪힐 때의 탄성과 마찰 | Bounce, Tangent Retention |
| 낙하 속도와 사라지는 시간 | Gravity Scale, Lifetime Range, Fade Duration |

1. 테스트 씬의 Ground 또는 Platform 바닥에 Enemy02와 Enemy04를 배치하고, 옆에 Ground 벽을 둔다.
2. Play Mode에서 각 적을 벽 방향으로 여러 번 때린다. 밝기가 다른 회색 조각과 불규칙한 외곽선이 섞여 흩어지고, 벽과 바닥에서 튕기는지 확인한다. 같은 거리에서 Enemy01을 때려 혈흔과 크기를 비교할 수 있다.
3. 여러 번 때려 회백색과 밝은 회색의 꺾쇠, 핀, 육각 와셔가 일부 조각에 붙어 회전하는지 확인한다. 확률은 개별 조각 기준이라 한 번의 타격마다 비율은 달라진다.

Unity 컴파일과 프리팹 참조 검사에 이어 Play Mode에서 본체 메시 4종 선택, 벽과 바닥의 반사, 회전 중 회색 부품 위치 유지, 파편 수명 종료, Enemy02와 Enemy04의 실제 피해 이벤트 연결을 확인했다.

사용한 문법과 이유

- `[SerializeField]` 필드의 초기값은 컴포넌트를 새로 추가할 때 쓰인다. 기존 프리팹은 저장된 값을 읽으므로 크기와 색상은 코드 초기값과 프리팹을 함께 맞췄다. `Color.Lerp`는 두 회색 사이에서 입자마다 다른 색을 고르는 데 쓴다.
- `ref Fragment`는 한 조각의 위치와 속도를 충돌 함수에서 직접 갱신한다. 본체와 부품의 물리 상태는 하나만 유지한다.
- `Vector2.Dot`은 입자의 속도를 벽에 수직인 성분과 벽을 따라가는 성분으로 나눌 때 사용한다. 수직 성분의 부호를 뒤집고 각 성분에 탄성과 마찰을 적용한다.
- `ParticleSystemRenderer.SetMeshes`로 본체와 부품에 서로 다른 메시 목록을 연결하고, `ParticleSystem.Particle.SetMeshIndex`로 그중 하나를 고른다. 물리 상태는 본체가 하나만 갖고, 작은 부품은 회전된 오프셋으로 가장자리에 렌더링한다. [Unity 메시 목록 API](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/particlesystemrenderer/setmeshes)
- `Mesh.vertices`는 외곽점의 좌표를 담고, `Mesh.triangles`는 세 점씩 묶어 채울 면을 정한다. 안쪽으로 파인 부분을 가로지르지 않도록 각 조각의 인덱스를 따로 지정했다.
- `Physics2D.CircleCast`로 각 물리 스텝의 이동 구간을 검사한다. 빠른 조각이 한 프레임 사이에 벽을 지나치는 경우를 줄이고, 한 스텝에 여러 표면을 만날 수 있도록 남은 이동 시간을 사용한다.
