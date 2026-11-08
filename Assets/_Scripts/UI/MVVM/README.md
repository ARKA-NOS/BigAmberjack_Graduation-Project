# UI MVVM 가이드

게임 데이터를 화면에 보여주는 새 UI는 이 폴더(`_Scripts/UI/MVVM`, 네임스페이스 `_Scripts.UI.MVVM`)의 MVVM 구조로 만든다.
기존 `_Scripts/UI/BindSystem`은 레거시다. 이미 그걸 쓰는 UI만 남겨 두고, 새 UI에는 쓰지 않는다.

## 구조

```
 Model (모듈, 매니저)            ViewModel (SO 에셋)               View (uGUI)
 INewHealthModule ──이벤트──▶  HealthViewModel               ◀──구독──  PlayerHealthView
                                 Observable<float> Ratio  ──알림──▶     Image.fillAmount
                    ◀──Action──  (데이터 안의 Action) ◀──클릭── Button

 Connector (Model 쪽 오브젝트에 붙는 UI 컴포넌트)
   PlayerHealthConnector: healthViewModel.Connect(owner.GetModule<INewHealthModule>())
```

| 역할 | 하는 일 | 알아도 되는 것 | 몰라야 하는 것 |
|---|---|---|---|
| Model | 게임 로직. 이벤트와 읽기 프로퍼티를 인터페이스로 노출 | 자기 자신 | ViewModel, View, UI 네임스페이스 전부 |
| ViewModel | Model을 구독해서 화면에 보여줄 값(Observable)으로 바꾼다 | Model **인터페이스** | View, `UnityEngine.UI`, TMP (`Sprite`·`Color` 같은 데이터 타입은 괜찮다) |
| View | Observable을 구독해 uGUI를 그리고, 연출(지연 바, 트윈)을 맡는다 | ViewModel | Model |
| Connector | 런타임에 Model을 찾아 ViewModel에 연결한다 | Model 인터페이스, ViewModel, (복제본이면) View | 화면 그리는 코드 |

## 코어 구성 요소

| 타입 | 설명 |
|---|---|
| `Observable<T>` / `IReadOnlyObservable<T>` | 값이 바뀔 때만 알린다. `Subscribe`는 기본적으로 구독 즉시 현재 값으로 한 번 호출한다. `(prev, current)` 오버로드, `SetWithoutNotify`, `ForceNotify` |
| `ObservableList<T>` / `IReadOnlyObservableList<T>` | `ListChange<T>`(Add/Remove/Replace/Reset)를 보낸다. 구독 즉시 Reset을 한 번 보낸다. `SetAll`, `NotifyItemChanged` |
| `DisposableBag`, `AddTo` | 구독 핸들을 모아 한 번에 해제한다 |
| `ViewModelBase<TModel>` | SO ViewModel 베이스. `Connect(model)` / `Disconnect()`, `Connected` 상태, `CreateRuntime` / `ReleaseRuntime` |
| `ITickableViewModel` | 이벤트 없이 매 프레임 바뀌는 값(쿨타임)을 `Tick()`에서 읽는다. 연결되면 UIManager가 매 프레임 호출한다 |
| `ViewBase<TViewModel>` | View 베이스. OnEnable에서 `OnBind`, OnDisable에서 구독 해제. `SetViewModel`로 복제본 주입 |
| `ConnectorBase` | 첫 Start에서 `Connect()`, 이후 OnEnable/OnDisable마다 연결/해제. `Reconnect()` |
| `UGuiBindingExtensions` | `BindText`, `BindFill`, `BindActive`, `BindVisible`(CanvasGroup), `BindClick` |

## 새 UI 만드는 순서

1. **Model 확인** — 보여줄 값이 Model 인터페이스에 이벤트/프로퍼티로 있는지 본다. 없으면 Model 담당자와 이벤트를 추가한다(Model에 ViewModel 필드를 넣지 않는다).
2. **ViewModel** — `ViewModelBase<TModel>`을 상속하고 `[CreateAssetMenu(fileName = "Xxx View Model", menuName = "UI/MVVM/Xxx View Model")]`를 붙인다.
   - Observable은 `private readonly`로 들고 `IReadOnlyObservable`로 노출한다.
   - `OnConnect`에서 Model 이벤트를 구독하고 현재 값으로 채운다. `OnDisconnect`에서 구독을 해제한다. `ResetState`에서 기본값으로 되돌린다.
   - 매 프레임 읽어야 하는 값이 있으면 `ITickableViewModel`도 구현한다.
3. **View** — `ViewBase<XxxViewModel>`을 상속하고 `OnBind`에서 구독한 뒤 `.AddTo(Disposables)`로 등록한다.
4. **Connector** — `ConnectorBase`를 상속해 Model 쪽 오브젝트(플레이어 프리팹 등)에 붙인다. `Connect()`에서 Model을 찾아 `viewModel.Connect(model)`, `Disconnect()`에서 `viewModel.Disconnect()`.
5. **에셋 연결** — `Create > UI > MVVM > ...`로 ViewModel 에셋을 만들고 Connector와 View 양쪽에 같은 에셋을 넣는다.

파일 위치: 특정 UI 전용 ViewModel/View/Connector는 그 UI 폴더에 같이 둔다(예: `UI/PlayerUI/HUD_UI/HP_UI/`). 여러 UI가 같이 쓰는 것만 공용 폴더로 뺀다.

## 예시 1. 플레이어 체력 HUD (하나뿐인 UI = 에셋 하나)

```csharp
[CreateAssetMenu(fileName = "Health View Model", menuName = "UI/MVVM/Health View Model")]
public class HealthViewModel : ViewModelBase<INewHealthModule>
{
    private readonly Observable<float> _current = new();
    private readonly Observable<float> _max = new();
    private readonly Observable<float> _ratio = new();

    public IReadOnlyObservable<float> Current => _current;
    public IReadOnlyObservable<float> Max => _max;
    public IReadOnlyObservable<float> Ratio => _ratio;

    private INewHealthModule _model;

    protected override void OnConnect(INewHealthModule model)
    {
        _model = model;
        _model.OnHealthChanged += HandleHealthChanged;
        HandleHealthChanged(model.CurrentHealth, 0f, model.MaxHealth);
    }

    protected override void OnDisconnect()
    {
        _model.OnHealthChanged -= HandleHealthChanged;
        _model = null;
    }

    // OnEnable에서도 불리므로 Observable만 건드린다(_model은 null일 수 있다).
    protected override void ResetState()
    {
        _current.Value = 0f;
        _max.Value = 0f;
        _ratio.Value = 0f;
    }

    private void HandleHealthChanged(float current, float delta, float max)
    {
        _current.Value = current;
        _max.Value = max;
        _ratio.Value = max > 0f ? current / max : 0f;
    }
}
```

```csharp
// 플레이어 프리팹의 자식 오브젝트에 붙인다. 체력 모듈은 이 컴포넌트의 존재를 모른다.
public class PlayerHealthConnector : ConnectorBase
{
    [SerializeField] private HealthViewModel healthViewModel;

    protected override void Connect()
    {
        ModuleOwner owner = GetComponentInParent<ModuleOwner>();
        if (owner != null && owner.TryGetModule(out INewHealthModule health))
            healthViewModel.Connect(health);
    }

    protected override void Disconnect() => healthViewModel.Disconnect();
}
```

```csharp
// 씬의 HUD 캔버스에 붙인다.
public class PlayerHealthView : ViewBase<HealthViewModel>
{
    [SerializeField] private CanvasGroup group;
    [SerializeField] private Image fill;
    [SerializeField] private Image delayedFill;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private float delaySpeed = 0.5f;

    protected override void OnBind(HealthViewModel vm)
    {
        // 플레이어가 없으면(연결 전/해제 후) HUD를 숨긴다. View 자신은 끄지 않고 CanvasGroup으로 숨긴다.
        vm.Connected.BindVisible(group).AddTo(Disposables);
        vm.Ratio.BindFill(fill).AddTo(Disposables);
        vm.Current.BindText(hpText, hp => Mathf.CeilToInt(hp).ToString()).AddTo(Disposables);

        // 이전 값이 필요한 '바뀔 때 1번' 연출
        vm.Current.Subscribe((prev, current) =>
        {
            if (current < prev) PlayHitFlash();
        }).AddTo(Disposables);
    }

    // 지연 바처럼 목표값을 따라가는 연출은 View가 한다.
    private void Update()
    {
        if (ViewModel == null) return;
        delayedFill.fillAmount = Mathf.MoveTowards(delayedFill.fillAmount, ViewModel.Ratio.Value, delaySpeed * Time.deltaTime);
    }

    private void PlayHitFlash() { /* DOTween 등 */ }
}
```

## 예시 2. 스킬 쿨타임 (Tick)

쿨타임은 이벤트 없이 매 프레임 줄어든다. ViewModel이 `ITickableViewModel`을 구현하면 연결된 동안 UIManager가 `Tick()`을 매 프레임 부르고, 값이 실제로 바뀔 때만 View에 알림이 간다.
스킬 1/2처럼 같은 종류가 여러 개여도 개수가 정해져 있으면 에셋을 슬롯마다 따로 만든다.

```csharp
[CreateAssetMenu(fileName = "Skill Slot View Model", menuName = "UI/MVVM/Skill Slot View Model")]
public class SkillSlotViewModel : ViewModelBase<ISkill>, ITickableViewModel
{
    private readonly Observable<Sprite> _icon = new();
    private readonly Observable<float> _coolTimeRatio = new(); // 1 = 막 사용함, 0 = 사용 가능
    private readonly Observable<bool> _blocked = new();

    public IReadOnlyObservable<Sprite> Icon => _icon;
    public IReadOnlyObservable<float> CoolTimeRatio => _coolTimeRatio;
    public IReadOnlyObservable<bool> Blocked => _blocked;

    private ISkill _skill;

    protected override void OnConnect(ISkill skill)
    {
        _skill = skill;
        _icon.Value = skill.SkillSo != null ? skill.SkillSo.SkillImage : null;
        Tick();
    }

    protected override void OnDisconnect() => _skill = null;

    protected override void ResetState()
    {
        _icon.Value = null;
        _coolTimeRatio.Value = 0f;
        _blocked.Value = false;
    }

    public void Tick()
    {
        _coolTimeRatio.Value = _skill is IHasCoolTime coolTime && coolTime.CoolTime > 0f
            ? coolTime.RemainingCoolTime / coolTime.CoolTime
            : 0f;
        _blocked.Value = !_skill.CanUseSkill() && _coolTimeRatio.Value <= 0f;
    }
}
```

장착 스킬이 바뀌는 경우 Connector가 스킬 교체 이벤트를 받아 `Reconnect()`를 호출하면 새 스킬로 다시 연결된다.

## 예시 3. 목록 (ObservableList)

칸 하나만 바뀌면 그 칸만 `Replace`로 알린다. View는 `ListChange.Kind`로 나눠 처리한다.

```csharp
[CreateAssetMenu(fileName = "Storage View Model", menuName = "UI/MVVM/Storage View Model")]
public class StorageViewModel : ViewModelBase<IInventoryModule>
{
    private readonly ObservableList<Sprite> _icons = new();
    public IReadOnlyObservableList<Sprite> Icons => _icons;

    private IInventoryModule _model;

    protected override void OnConnect(IInventoryModule model)
    {
        _model = model;
        _model.OnSlotChanged += HandleSlotChanged;

        ItemContainer storage = model.Inventory.Storage;
        List<Sprite> icons = new(storage.MaxCapacity);
        for (int i = 0; i < storage.MaxCapacity; i++)
            icons.Add(GetIcon(storage[i]));
        _icons.SetAll(icons);
    }

    protected override void OnDisconnect()
    {
        _model.OnSlotChanged -= HandleSlotChanged;
        _model = null;
    }

    protected override void ResetState() => _icons.Clear();

    private void HandleSlotChanged(InventorySlotType type, int index)
    {
        if (type != InventorySlotType.Storage) return;
        _icons[index] = GetIcon(_model.Inventory.Storage[index]);
    }

    private static Sprite GetIcon(ItemInstance item) => item?.Data != null ? item.Data.Icon : null;
}
```

```csharp
public class StorageView : ViewBase<StorageViewModel>
{
    [SerializeField] private Transform slotParent;
    [SerializeField] private Image slotTemplate;
    private readonly List<Image> _slots = new();

    protected override void OnBind(StorageViewModel vm)
    {
        vm.Icons.Subscribe(change =>
        {
            switch (change.Kind)
            {
                case ListChangeKind.Replace:
                    SetSlot(change.Index, change.NewItem);
                    break;
                default: // Reset, Add, Remove: 칸 수가 바뀌므로 전부 다시 그린다
                    RebuildAll(vm.Icons);
                    break;
            }
        }).AddTo(Disposables);
    }

    private void RebuildAll(IReadOnlyList<Sprite> icons)
    {
        while (_slots.Count < icons.Count) _slots.Add(Instantiate(slotTemplate, slotParent));
        for (int i = 0; i < _slots.Count; i++)
        {
            _slots[i].gameObject.SetActive(i < icons.Count);
            if (i < icons.Count) SetSlot(i, icons[i]);
        }
    }

    private void SetSlot(int index, Sprite icon)
    {
        _slots[index].sprite = icon;
        _slots[index].enabled = icon != null;
    }
}
```

## 예시 4. 입력 (데이터 안의 Action)

버튼 입력은 ViewModel이 노출하는 데이터에 `Action`으로 담고, View는 `Invoke`만 한다. Action 안에서 ViewModel이 Model 메서드를 호출한다.
결과 화면·대화처럼 한 번에 바뀌는 묶음은 속성을 쪼개지 않고 `Observable<XxxData>` 하나로 둬도 된다.

```csharp
public class RunResultData
{
    public int Gold { get; }
    public Action OnNext { get; }   // "다음" 버튼
    public RunResultData(int gold, Action onNext) { Gold = gold; OnNext = onNext; }
}

// ViewModel
private readonly Observable<RunResultData> _result = new();
public IReadOnlyObservable<RunResultData> Result => _result;
// 결과를 받으면: _result.Value = new RunResultData(gold, () => _model.GoNext());

// View
protected override void OnBind(RunResultViewModel vm)
{
    vm.Result.Subscribe(Show).AddTo(Disposables);
    nextButton.BindClick(() => vm.Result.Value?.OnNext?.Invoke()).AddTo(Disposables);
}
```

## 예시 5. 같은 UI가 여러 개 (적 체력바 = 런타임 복제)

에셋 하나는 인스턴스 하나다. 적마다 체력바가 있으면 템플릿 에셋을 복제해서 쓴다. View와 Connector를 같은 적 프리팹 안에 둔다.

```csharp
// 적 프리팹 안. View의 viewModel 칸은 비워 둔다.
public class EnemyHealthConnector : ConnectorBase
{
    [SerializeField] private HealthViewModel template;
    [SerializeField] private EnemyHealthView view;

    private HealthViewModel _instance;

    protected override void Connect()
    {
        ModuleOwner owner = GetComponentInParent<ModuleOwner>();
        if (owner == null || !owner.TryGetModule(out INewHealthModule health)) return;

        _instance = ViewModelBase.CreateRuntime(template);
        view.SetViewModel(_instance);
        _instance.Connect(health);
    }

    protected override void Disconnect()
    {
        if (_instance == null) return;

        // 적이 파괴될 때는 View가 먼저 파괴됐을 수 있다.
        if (view != null) view.SetViewModel(null);
        _instance.ReleaseRuntime(); // 연결 해제 + 파괴
        _instance = null;
    }
}
```

## 주의

- **View/Connector에서 OnEnable·OnDisable·Start가 더 필요하면** `protected override`로 만들고 `base.Xxx()`를 꼭 호출한다. `private void OnEnable()`로 새로 선언하면 구독/연결이 빠진다.
- **ViewModel에서 OnEnable·OnDisable을 쓰면** 마찬가지로 `base`를 호출한다(이전 플레이 값 초기화가 여기서 일어난다).
- **View 자신의 GameObject를 `BindActive`로 끄지 않는다.** 꺼지는 순간 구독이 해제되어 다시 켜지지 않는다. 자식 오브젝트를 끄거나 `BindVisible(CanvasGroup)`을 쓴다.
- **구독 순서는 상관없다.** View가 먼저 켜지면 기본값으로 그렸다가 연결될 때 새 값을 받고, 나중에 켜지면 구독 즉시 현재 값을 받는다. 예전처럼 꺼진 View를 위해 `root`를 따로 둘 필요가 없다.
- **참조 타입 내부만 바뀌면 알림이 가지 않는다.** 같은 객체를 다시 넣으면 `Value` setter는 같은 값으로 보고 무시한다. 새 객체를 넣거나 `ForceNotify()`(목록은 `NotifyItemChanged`)를 쓴다.
- **Tick은 UIManager가 돌린다.** GameManager(UIManager)가 없는 씬에서는 빈 Ticker(`NullUITicker`)가 등록되어 있어 Tick 기반 값(쿨타임)이 멈춘다. 이벤트 기반 값은 그대로 동작한다.
- **ViewModel 에셋은 공유 상태다.** 한 에셋에는 한 Connector만 연결한다. 두 Connector가 같은 에셋에 Connect하면 나중 것이 앞의 연결을 끊는다.
- **ViewModel 값은 저장되지 않는다.** 저장이 필요한 값은 Model(매니저)에 두고 SaveSystem으로 저장한다.
- **창 열기/닫기는 이 구조 밖이다.** 지금처럼 `UIManagerEvents.OpenCloseWindow`와 `AbstractWindow`를 쓰고, 창 안의 내용만 MVVM으로 만든다.
