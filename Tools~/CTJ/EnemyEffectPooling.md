# 적 파티클 풀링

적의 피격·사망 파티클과 지상 착지 파티클, 폐부품·폭탄의 착탄 파티클을 Lean Pool로 재사용한다. 파티클 프리팹과 인스펙터의 기존 연결을 그대로 사용한다.

## Proto_MainHome의 PoolManager

`LeanPool`은 정적 API를 제공한다. 처음 `Spawn`으로 프리팹을 요청하면 등록된 `LeanGameObjectPool`을 찾고, 없으면 현재 활성 씬에 `LeanPool (프리팹 이름)` 오브젝트를 만든다. 따라서 `Proto_MainHome`의 편집 모드 Hierarchy에는 PoolManager가 없어도 된다. 적이 있는 씬에서 첫 피격이 나면 `LeanPool (HitBloodSpray)` 또는 `LeanPool (HitMetalFragments)`가 생긴다. 사망 버스트도 같은 방식으로 자기 프리팹에 대응하는 풀을 쓴다.

1. 다른 파티클 효과에서 직접 쓰려면 호출 코드에 `using Lean.Pool;`을 추가하고, 인스펙터에서 연결한 프리팹을 `LeanPool.Spawn(prefab, position, rotation)`에 전달한다. 프리팹 필드가 `ParticleSystem` 타입이면 반환값도 `ParticleSystem`이다.
2. 재생이 끝났을 때 활성 인스턴스를 `LeanPool.Despawn(instance)`에 넘긴다. 반환된 인스턴스가 비활성화되고 Hierarchy에서 풀의 자식으로 들어간다. 다음 요청 때 풀에서 꺼내고, 동시 재생 중이라 재사용할 것이 없으면 새 인스턴스를 만든다. 부착된 오브젝트를 재사용할 때는 `OnDisable`이나 `IPoolable.OnDespawn`에서 타이머, 트레일, 외부 참조와 같은 재생 상태를 정리한다.
3. 단순히 등록된 프리팹을 생성하고 반환하는 곳에서는 프로젝트의 `PoolSO`도 쓸 수 있다. `Create > PoolSystem > GameObject Pool SO`로 에셋을 만들고 Prefab을 지정한다. 현재 `PoolSO.Spawn()`에는 월드 좌표와 회전을 받는 인자가 없다. 적 파티클은 타격마다 두 값을 전달하므로 Lean Pool의 위치 지정 오버로드를 직접 쓴다.
4. 첫 재생 전에 인스턴스를 준비하거나 풀 개수를 제한해야 할 때만 씬에 `LeanGameObjectPool`을 명시적으로 배치한다. `Prefab`에 같은 에셋을 지정하면 정적 `Spawn`이 해당 풀을 찾는다. `Preload`는 미리 만들 개수이고, `Capacity`는 풀의 최대 개수이며 0은 무제한이다. `Recycle`은 한도에 도달했을 때 활성 인스턴스 중 가장 오래된 것을 회수해 쓴다. `Persist`는 씬 전환 후에도 풀을 유지할 때 사용한다. 이번 작업은 자동 풀과 기존 적별 동시 재생 한도를 사용한다.

관련 코드

- [LeanPool.Spawn](/C:/Users/USER/BigAmberjack_Graduation-Project/Assets/Plugins/CW/LeanPool/Required/Scripts/LeanPool.cs:76)
- [공용 PoolSO](/C:/Users/USER/BigAmberjack_Graduation-Project/Assets/_Scripts/_Core/PoolSystem/PoolSO.cs:8)
- [적 파티클 생성·반환](/C:/Users/USER/BigAmberjack_Graduation-Project/Assets/_Scripts/Agent/Enemies/EnemyHitParticle.cs:17)

## 직접 테스트

1. `EnemyTestScene`을 열고 Play를 누른다. Enemy17을 때리면 Hierarchy에 `LeanPool (HitBloodSpray)`가 생긴다. 파티클과 흔적이 사라지면 해당 인스턴스가 풀 아래로 이동하고 비활성화된다.
2. 잠시 뒤 다시 때려 풀에 들어갔던 인스턴스가 새 타격 위치에 나오는지 확인한다. 흔적이 남아 있을 때 연속으로 때리면 여러 타격 효과가 동시에 보인다.
3. 금속 효과는 Enemy21을 활성화해 확인한다. Enemy22의 폐부품이 바닥에 닿을 때도 금속 파편 풀을 쓴다. Enemy17과 Enemy21을 처치해 적이 사라진 뒤에도 사망 이펙트가 제 수명을 마치고 풀로 돌아가는지 본다.

## 사용한 문법과 이유

- `LeanPool.Spawn<ParticleSystem>`의 제네릭 반환형으로 프리팹의 동일한 컴포넌트를 받는다. 호출에서 타입을 추론하므로 `<ParticleSystem>`을 생략할 수 있다.
- `TryGetComponent(out EnemyHitParticle lifetime)`으로 수명 컴포넌트를 확인한다. 처음부터 붙어 있는 프리팹은 그 컴포넌트를 쓰고, 다른 파티클 프리팹은 인스턴스가 처음 사용될 때 한 번 붙인다.
- `OnDisable()`에서 루트와 자식 파티클을 정리하고 생성한 적의 목록에서 참조를 해제한다. 덕분에 같은 인스턴스를 다른 적이 재사용해도 이전 적의 동시 재생 한도에 걸리지 않는다.
- `IsAlive(true)`로 자식 파티클까지 재생이 끝났는지 확인한다. 수명 상한에 도달하거나 적별 동시 한도를 넘긴 경우에도 같은 반환 경로를 쓴다.
