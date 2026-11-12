기계 적 피격 파편

`HitMetalFragments.prefab`은 혈흔처럼 홈과 돌기가 불규칙한 다각형 본체 메시 4종을 사용한다. 외곽의 가로세로 길이는 비슷하고 정점 수는 8~13개다. Unity 6000.3.22f1에서 한 렌더러에 메시 10개를 전달해 4개만 등록되는 것을 확인했다. 피격마다 생성하는 파티클 시스템 수를 줄이기 위해 본체 렌더러 하나에 4종을 담았다. 작은 부품은 자식 렌더러의 메시 목록에서 꺾쇠, 핀, 육각 와셔 중 하나를 뽑는다. Enemy02와 Enemy04의 `ParticleModule → Hit Effects → Metal Fragments`에서 사용한다.

현재 피격 프리팹은 5~8개 조각을 피격 방향으로 방출한다. `Size Range`는 `0.12~0.22`다. 본체는 `Body Color Weights`에 들어 있는 회색 4종과 회갈색 1종 중 조각마다 하나를 고른다. 현재 회색의 가중치는 각각 `5`, 회갈색은 `1`이다. 가중치가 `0`인 색은 추첨에서 제외되고, 목록이 비었거나 유효한 가중치가 하나도 없으면 밝은 회색을 사용한다. 조각마다 25% 확률로 회백색 또는 밝은 회색 부품을 가장자리에 일부 겹친다. 이 부품은 본체와 같은 회전 및 충돌을 공유한다. Ground와 Platform에 부딪힐 때 속도의 일부를 잃으며 튕기고, 1.0~1.5초 뒤 0.25초 동안 페이드아웃한다.

`DeathMetalBurst.prefab`은 `HitMetalFragments`의 프리팹 Variant다. Enemy02와 Enemy04의 `ParticleModule → Death Burst Prefab`에 연결되어 `DeathStarted` 이벤트가 발생한 프레임에 적의 효과 기준점에서 한 번 재생된다. 파편 36~48개가 속도 6.5~9로 360도 방향에 퍼진다. 피격 프리팹에서 색상, 모양, 크기, 중력, 튕김을 변경하면 사망 버스트에도 반영된다. 사망용 프리팹의 `Fragment Count`, `Speed Range`, `Spread Angle`, `Upward Speed`는 사망 효과에서 개별 조절한다.

| 조정할 항목 | `EnemyMetalFragmentParticle`의 Inspector 값 |
| --- | --- |
| 조각 수·크기·퍼지는 각도 | Fragment Count, Size Range, Spread Angle |
| 방출 강도와 위로 튀는 정도 | Speed Range, Upward Speed |
| 조각 색과 출현 가중치, 결합 부품 색 | Body Color Weights, Attachment Colors |
| 부품 메시 목록 | Attachment Particles 자식의 Particle System Renderer |
| 부품이 붙을 확률 | Attachment Chance |
| 표면에 부딪힐 때의 탄성과 마찰 | Bounce, Tangent Retention |
| 낙하 속도와 사라지는 시간 | Gravity Scale, Lifetime Range, Fade Duration |

1. 테스트 씬의 Ground 또는 Platform 바닥에 Enemy02와 Enemy04를 배치하고, 옆에 Ground 벽을 둔다.
2. Play Mode에서 각 적을 벽 방향으로 여러 번 때린다. 밝기가 다른 회색 조각과 불규칙한 외곽선이 섞여 흩어지고, 벽과 바닥에서 튕기는지 확인한다. 같은 거리에서 Enemy01을 때려 혈흔과 크기를 비교할 수 있다.
3. 여러 번 때려 회백색과 밝은 회색의 꺾쇠, 핀, 육각 와셔가 일부 조각에 붙어 회전하는지 확인한다. 확률은 개별 조각 기준이라 한 번의 타격마다 비율은 달라진다.
4. `HitMetalFragments` 프리팹의 `EnemyMetalFragmentParticle → Body Color Weights`에서 항목을 추가·삭제하고 각 항목의 `Color`와 `Weight`를 바꿀 수 있다. 가중치를 확인하려면 두 색을 각각 `4`와 `1`로, 나머지는 `0`으로 설정하고 여러 번 때려 본다. 활성화한 두 색의 출현 확률은 각각 80%와 20%다.
5. Enemy02와 Enemy04를 처치한다. 마지막 타격의 방향성 파편에 이어 같은 프레임에 몸 중심에서 더 많은 금속 파편이 사방으로 나오는지 확인한다. 조각은 바닥이나 벽에 닿으면 튕기며 페이드아웃한다.

Unity 컴파일과 프리팹 참조 검사에 이어 Play Mode에서 본체 메시 4종과 색상 5종 선택, 높은 가중치의 색이 더 자주 나오고 0인 색은 제외되는 동작, Enemy02와 Enemy04의 피격 이벤트 연결을 확인했다.

별도 Play 씬에서 Enemy02와 Enemy04를 실제로 처치해 마지막 타격 프레임에 각각 금속 버스트 하나가 생성되는 것을 확인했다. 두 버스트는 38개와 37개 파편을 상하좌우로 방출한 뒤 정리됐고, 혈흔 파티클은 생성되지 않았다.

사용한 문법과 이유

- Unity 인스펙터에는 `[Serializable]`을 붙인 `WeightedColor` 구조체의 배열을 저장한다. 방출할 때 배열을 `Dictionary<Color, float>`로 옮겨 색을 키, 가중치를 값으로 사용한다. 같은 색이 여러 행에 있으면 마지막 행의 가중치가 적용된다. [Unity 직렬화 규칙](https://docs.unity3d.com/6000.3/Documentation/Manual/script-serialization-rules.html)
- `Random.value`에 가중치 합계를 곱해 추첨값을 만들고, 색별 가중치를 순서대로 뺀다. 추첨값이 처음 음수가 되는 구간의 색을 선택하므로 가중치가 큰 색에 더 넓은 구간이 배정된다.
- `ref Fragment`는 한 조각의 위치와 속도를 충돌 함수에서 직접 갱신한다. 본체와 부품의 물리 상태는 하나만 유지한다.
- `Vector2.Dot`은 입자의 속도를 벽에 수직인 성분과 벽을 따라가는 성분으로 나눌 때 사용한다. 수직 성분의 부호를 뒤집고 각 성분에 탄성과 마찰을 적용한다.
- `ParticleSystemRenderer.SetMeshes`로 본체와 부품에 서로 다른 메시 목록을 연결하고, `ParticleSystem.Particle.SetMeshIndex`로 그중 하나를 고른다. 물리 상태는 본체가 하나만 갖고, 작은 부품은 회전된 오프셋으로 가장자리에 렌더링한다. [Unity 메시 목록 API](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/particlesystemrenderer/setmeshes)
- `Mesh.vertices`는 외곽점의 좌표를 담고, `Mesh.triangles`는 세 점씩 묶어 채울 면을 정한다. 안쪽으로 파인 부분을 가로지르지 않도록 각 조각의 인덱스를 따로 지정했다.
- `Physics2D.CircleCast`로 각 물리 스텝의 이동 구간을 검사한다. 빠른 조각이 한 프레임 사이에 벽을 지나치는 경우를 줄이고, 한 스텝에 여러 표면을 만날 수 있도록 남은 이동 시간을 사용한다.
- 사망용 프리팹 Variant는 부모의 컴포넌트와 값을 상속하고 네 가지 방출 설정에 Override를 둔다. 기존 `[SerializeField] ParticleSystem deathBurstPrefab`에 Variant를 넣으면 `EnemyParticleModule`이 `DeathStarted` 이벤트에서 피격과 같은 방식으로 파티클을 생성한다.
