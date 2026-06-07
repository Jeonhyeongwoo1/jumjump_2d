# AI_GUIDELINES.md

## JumJump Target Platform: Mobile WebGL

JumJump의 최종 배포 목표는 **모바일 브라우저에서 실행되는 Unity WebGL 빌드**다.
모든 기능 구현, 리소스 추가, UI 구성, 저장 방식, 플러그인 선택은 Android/iOS 네이티브 앱이 아니라 **Mobile WebGL 제약**을 우선 기준으로 판단한다.

### Build Target

- Unity WebGL 빌드를 최종 산출물로 본다.
- 1차 타겟은 모바일 세로 화면이다.
- 입력은 터치/탭 중심으로 설계한다.
- 키보드, 마우스, 게임패드 의존 기능은 필수 경로에 두지 않는다.
- 브라우저 새로고침, 탭 전환, 메모리 압박 상황에서도 재진입이 가능해야 한다.

### Performance Budget

- 모바일 WebGL에서는 CPU, 메모리, GPU, 다운로드 용량을 모두 제한된 자원으로 본다.
- 코어 게임플레이는 60 FPS를 목표로 하되, 저사양 기기에서는 30 FPS에서도 플레이 감각이 유지되어야 한다.
- 매 프레임 GC allocation을 만들지 않는다.
- `Update`, `LateUpdate`, `FixedUpdate` 안에서 LINQ, 문자열 조합, 임시 컬렉션 생성, 반복적인 `new`를 피한다.
- 물리 연산은 필요한 범위로 제한하고, 충돌 레이어를 명확히 분리한다.
- 카메라, UI, 이펙트는 과도한 오버드로우를 만들지 않도록 관리한다.

### Resource Management

- 런타임 생성/삭제를 반복하지 않고 Object Pool을 사용한다.
- 발판, 장애물, 코인, 이펙트, UI 팝업 등 반복 생성되는 오브젝트는 풀링 대상이다.
- `Destroy` 남발을 금지하고, 재사용 가능한 오브젝트는 비활성화 후 풀로 반환한다.
- 큰 텍스처, 오디오, 애니메이션은 초기 로딩에 모두 넣지 않고 필요한 시점에 로드한다.
- 사용하지 않는 Addressable 리소스는 참조를 해제한다.
- 스프라이트는 가능한 Atlas로 묶고, 텍스처 크기와 압축 포맷을 모바일 WebGL 기준으로 관리한다.
- 배경, 스킨, 꾸미기 리소스는 MVP 단계에서 최소 세트만 포함한다.

### Asset Guidelines

- 원본 PSD, 대형 이미지, 미사용 오디오는 빌드 포함 경로에 두지 않는다.
- 스프라이트 해상도는 실제 화면 표시 크기를 기준으로 산정한다.
- 투명 영역이 큰 이미지는 잘라내거나 Atlas Packing 효율을 확인한다.
- 오디오는 짧은 효과음과 루프 BGM을 분리하고, 압축 설정을 명확히 한다.
- UI 이미지는 재사용 가능한 9-slice 또는 공통 Atlas를 우선 사용한다.

### WebGL Constraints

- WebGL에서는 스레드, 파일 시스템, 네이티브 플러그인, 브라우저 정책 제약을 고려한다.
- 모바일 브라우저 자동 재생 정책 때문에 BGM/효과음은 사용자 첫 입력 이후 활성화한다.
- 로컬 저장은 우선 `PlayerPrefs`를 사용하되, 데이터 크기를 작게 유지한다.
- 외부 SDK, 광고, 결제, 분석 도구는 WebGL 지원 여부와 번들 크기를 확인한 뒤 추가한다.
- 동기 로딩으로 긴 프리즈를 만들지 않는다.

### UI/UX for Mobile WebGL

- 화면은 세로형 모바일 뷰포트를 기본으로 한다.
- 터치 영역은 충분히 크게 잡고, 작은 아이콘만으로 핵심 조작을 요구하지 않는다.
- Safe Area, 브라우저 주소창 높이 변화, 화면 비율 차이를 고려한다.
- 게임 중 UI는 점수, 재화, 일시정지처럼 필수 정보만 최소한으로 유지한다.
- 결과 화면과 상점 화면은 느린 모바일 브라우저에서도 즉시 반응해야 한다.

### Implementation Rule

- 새 기능을 추가할 때는 기능 완성뿐 아니라 Mobile WebGL에서의 비용을 함께 검토한다.
- 리소스를 추가할 때는 예상 빌드 용량, 메모리 점유, 로딩 시점을 함께 기록한다.
- 성능에 영향을 줄 수 있는 변경은 Unity Profiler, WebGL 빌드 실행, 브라우저 테스트 중 최소 하나로 검증한다.

공통 AI 가이드라인 — Claude Code(`CLAUDE.md`)와 OpenAI Codex(`AGENTS.md`) 양쪽에서 참조합니다.

---

## 프로젝트 개요

Hero-Survivors는 Vampire Survivors 스타일의 로그라이크 서바이벌 게임입니다.  
엔진: **Unity 6000.3.6f1** / 단일 씬 구조: `Assets/Game/01.Scenes/GameScene.unity`  
빌드는 Unity Editor 표준 Build Settings 사용. 별도 빌드 스크립트 없음.

---

## 아키텍처

### 의존성 주입 (VContainer)

**`Assets/Game/02.Scripts/GameSceneLifeScope.cs`** 가 씬 전체의 DI 배선 파일.  
모든 서비스·팩토리·레지스트리·프레젠터·상태가 여기서 등록됨.  
`FindObjectOfType`, 정적 싱글턴 사용 금지 — 반드시 주입으로 해결한다.

```csharp
builder.Register<WeaponFactory>(Lifetime.Scoped);                        // 일반 서비스
builder.RegisterEntryPoint<WeaponService>(Lifetime.Scoped).AsSelf();     // 라이프사이클 포함
builder.Register<IEventBus, EventBus>(Lifetime.Scoped);                  // 인터페이스 바인딩
builder.RegisterComponent(_uiRoot);                                       // MonoBehaviour
```

### 게임 흐름 (State Machine)

`GameFlowManager` (`IAsyncStartable` / `ITickable` / `IDisposable`) 가 상태 전환을 관리한다:

```
ReadyGameState → SelectCharacterState → GameWaveState → WaveEndCollectCurrencyState → WeaponShopState → (반복)
```

상태는 `Dictionary<Type, IGameState>` 에 캐싱되어 재사용됨.  
상태 전환: 이전 상태 `Exit()` → 새 상태 `Enter()`.

### 레이어 구조

| 레이어 | 위치 | 역할 |
|---|---|---|
| Controller | `Controller/` | 엔티티 로직 (Player, Monster, Weapon, Projectile) |
| Service | `Service/` | 비즈니스 로직·크로스 시스템 조율 |
| Factory | `Factory/` | 오브젝트 생성 (직접 `new` 금지, 반드시 Factory 경유) |
| Registry | `Registry/` | 활성 오브젝트 인메모리 추적 (소유권 없음) |
| Component | `Component/` | 재사용 가능한 MonoBehaviour 조각 |
| Presenter | `Presenter/` | UI MVP 패턴. View(`BaseSceneUI`/`BasePopup` 파생) + Presenter(순수 C# 클래스) 쌍으로 구성 |
| Effect | `Effect/` | 상태 이상 구현체 |
| Event | `Event/` | EventBus + 이벤트 struct 정의 |
| Interface | `Interface/` | 모든 인터페이스 (파일 1개 = 인터페이스 1개) |
| Data | `Data/` | 순수 데이터 클래스 (로직 없음) |

---

## 핵심 시스템

### 무기 시스템

제네릭 베이스: `BaseWeaponController<TWeaponData, TWeaponStatData>`  
모든 무기가 구현해야 하는 메서드: `Initialize()`, `Tick()`, `Rebuild()`, `RefreshOwnerStats()`

무기 타입 (`Controller/Weapon/`):
- `ProjectileBasedWeaponController` — 투사체 발사
- `MeleeWeaponController` — 근거리 AoE 스윙
- `OrbitWeapon` — 플레이어 주변 공전 투사체
- `AreaWeapon` — 고정 AoE 구역
- `ElectricStaffWeaponController` — 체인 번개

등급(Common / Rare / Epic)별 스탯은 `*WeaponStatData` 에 정의.  
`WeaponRuntimeStatCalculator` 가 런타임 최종값 계산.

### 투사체 시스템

- 베이스: `ProjectileController` — 타입별 **payload struct** 로 초기화
- 풀링 필수 — `Release()` 에서 `_onReleaseAction?.Invoke(this)` 로 반환, `Destroy` 절대 금지
- 생성: `ProjectileFactory` / 추적: `ProjectileRegistry` / 관리: `ProjectileService`

### 상태 이상(Status Effect) 시스템

베이스: `BaseGamePlayEffect`. 이펙트별 스택 규칙 설정:
- `RefreshDuration` — 재적용 시 타이머 초기화
- `Independent` — 다수 인스턴스 공존
- `StackValue` — 중첩마다 강도 증가

모든 이펙트는 `GamePlayEffectService` 경유, `GamePlayEffectRegistry` 에서 추적.

### UI 시스템 (MVP 패턴)

#### 역할 분리

| 역할 | 클래스 | 책임 |
|---|---|---|
| **View** | `UI_Xxx : BaseSceneUI` / `BasePopup` | SerializeField로 UI 요소 보유. 데이터 표시 메서드(`SetScore`, `SetCountdown` 등) + 사용자 입력을 C# 이벤트(`event Action`)로 발사. 비즈니스 로직 없음. |
| **Presenter** | `UIXxxPresenter` | 순수 C# 클래스. VContainer Scoped 등록. EventBus 구독, 상태 판단, View 메서드 호출. `Bind(view)` / `Unbind()` 로 View 수명 관리. |

#### 구조

```
BaseSceneUI (abstract MonoBehaviour)  ← 씬 루트 View (Canvas 필수)
  └─ UI_GameScene                     ← 게임 씬 HUD View

BasePopup (abstract MonoBehaviour)   ← 팝업 View (Canvas 필수, Sort Order 관리)
  └─ UI_GameOverPopup                 ← 게임오버 팝업 View

UIGameScenePresenter                  ← UI_GameScene 전용 Presenter
UIGameOverPopupPresenter              ← UI_GameOverPopup 전용 Presenter
```

#### 생성·관리

| 타입 | 생성 주체 | 관리 서비스 |
|---|---|---|
| `BaseSceneUI` 파생 (View) | `UIFactory.Create<T>(key)` | `UIService` (타입별 캐시) |
| `BasePopup` 파생 (View) | `UIFactory.Create<T>(key)` | `PopupService` (스택 + 타입별 캐시) |
| `UIXxxPresenter` | VContainer 생성자 주입 | 해당 GameFlowState가 보유 |

- `UIFactory.Create<T>()` — Addressable 프리팹 로드 → 인스턴스화 → **루트의 `GetComponent<T>()`** 로 View 반환
- View 생성 후 **Presenter.Bind(view)** 를 호출하여 이벤트 구독과 초기화를 수행한다

#### Bind 패턴

```csharp
// Scene UI: 생성 후 명시적으로 Bind
var view = _uiService.Create<UI_GameScene>(configData.GameSceneUiAddressableKey);
_scenePresenter.Bind(view);

// Popup: Presenter가 Show() 내부에서 Push + Bind 일괄 처리
_popupPresenter.Show();  // 내부: Push → Bind(최초 1회) → 카운트다운 시작
_popupPresenter.Hide();  // 내부: 카운트다운 취소 → PopAll
```

#### PopupService 스택 동작

```csharp
_popupService.Push<UI_GameOverPopup>(key);  // 생성(최초 1회) → SetActive(true) → OnShown()
_popupService.Pop();                         // OnHidden() → SetActive(false) → 스택에서 제거
_popupService.PopAll();                      // 상태 이탈 시 전체 정리
```

- 팝업은 최초 Push 시 1회 생성되어 내부 캐시에 유지된다 (Addressable 재로드 없음).
- `OnShown()` / `OnHidden()` 은 `BasePopup` 가상 메서드 — 애니메이션 훅용. 비즈니스 로직은 Presenter가 담당.
- 상태(`IGameFlowState`)가 팝업 Presenter를 통해 열고 닫는다: `OnEnter()` → `presenter.Show()`, `OnExit()` → `presenter.Hide()`.

#### 프리팹 ↔ View 매핑 3원칙

1. **View 클래스명 = 프리팹명 = Addressable 키** (예: `UI_GameOverPopup`)
2. **프리팹 루트에 Canvas 컴포넌트** — 없으면 렌더링 불가 (`BaseSceneUI`/`BasePopup.Awake()`에서 오류 출력)
3. **프리팹 루트에 View 컴포넌트 부착** — `UIFactory`가 `GetComponent<T>()` 로 탐색

### 이벤트 시스템 (EventBus)

제네릭·타입 안전·제로 할당(struct 이벤트).

```csharp
// 정의 (Event/GameEvents.cs)
public struct PlayerLevelUpEvent
{
    public int Level { get; private set; }
    public PlayerLevelUpEvent(int level) { Level = level; }
}

// 구독 / 해제
_eventBus.Subscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);
_eventBus.Unsubscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);

// 발행
_eventBus.Publish(new PlayerLevelUpEvent(newLevel));

// 핸들러 — 반드시 in 키워드 사용
private void OnPlayerLevelUp(in PlayerLevelUpEvent ev) { }
```

구독 위치: `OnEnable` / `Initialize` → 해제 위치: 대응하는 `OnDisable` / `Dispose`  
`Start` 에서 구독했다면 `OnDestroy` 에서 해제.

### 데이터 시스템

`GameDataService` — 시작 시 JSON 일괄 로드, 타입별 딕셔너리 제공.  
`CreatureStatComponent` — `ModifiableStat` + `StatModifier` 로 런타임 스탯 관리.

---

## 코드 스타일 & 컨벤션

### 네이밍 규칙

| 심볼 | 규칙 | 예시 |
|---|---|---|
| private 필드 | `_camelCase` | `_currentHP`, `_weaponFactory` |
| `[SerializeField]` 필드 | `[SerializeField] private` + `_camelCase` | `[SerializeField] private Transform _pivotTransform;` |
| protected 필드 | `_camelCase` | `protected float _currentHP;` |
| 프로퍼티 | `PascalCase` | `public bool IsDead => ...` |
| 메서드 | `PascalCase` | `public void Initialize()` |
| 클래스 | `PascalCase` | `CreatureController` |
| 인터페이스 | `I` + `PascalCase` | `ITargetable`, `IWeaponOwner` |
| 열거형 타입 | `PascalCase` | `CreatureStateType` |
| 열거형 값 | `PascalCase` | `CreatureStateType.Idle` |
| 이벤트 struct | `PascalCase` + `Event` 접미사 | `PlayerSpawnedEvent` |
| 상수 | `PascalCase` | `MonsterAppearDuration` |
| 제네릭 타입 파라미터 | `T` + 설명형 이름 | `TWeaponData`, `TWeaponStatData` |

`[SerializeField]` 필드는 반드시 `private` — `public`으로 선언 금지.

### 클래스 내부 레이아웃 순서

```
1. public 프로퍼티 / expression-bodied getter
2. public Action<> 콜백
3. [SerializeField] private 필드
4. protected 필드
5. private 필드 (주입된 의존성은 private readonly 로 마지막에)
6. [Inject] public void Construct(...)   ← MonoBehaviour만
7. public 메서드 (Initialize, InjectDependencies 포함)
8. protected virtual/override 메서드
9. private 메서드
10. Unity 라이프사이클 (Awake, Start, OnEnable, OnDisable, OnDestroy, Update, FixedUpdate)
```

### 의존성 주입 패턴

**서비스 (non-MonoBehaviour) — 생성자 주입, `readonly` 필드:**
```csharp
public class MonsterSpawnService : IInitializable, IDisposable
{
    private readonly PoolManager _poolManager;
    private readonly MonsterRegistry _registry;

    public MonsterSpawnService(PoolManager poolManager, MonsterRegistry registry)
    {
        _poolManager = poolManager;
        _registry = registry;
    }
}
```

**MonoBehaviour — `[Inject]` 메서드 주입, 메서드명 `Construct`:**
```csharp
[Inject]
public void Construct(GameDataService gameDataService, IEventBus eventBus)
{
    _gameDataService = gameDataService;
    _eventBus = eventBus;
}
```

**팩토리가 VContainer 이후에 오브젝트를 빌드하는 경우 — 수동 주입:**
```csharp
public void InjectDependencies(ITargetable target, IMonsterBehavior brain, IEventBus eventBus)
{
    _target = target;
    _brain  = brain;
    _eventBus = eventBus;
}
```

### 널 처리 & 가드

중첩 `if` 대신 null-conditional 연산자와 얼리 리턴 선호:
- `return;`으로 예외/방어 처리를 하는 경우에는 반드시 `Debug.LogWarning` 또는 `Debug.LogError`를 먼저 남긴다.
- 단순 필터링이나 정상 흐름의 값 반환(`return 0`, `return false` 등)이 아니라, 예상치 못한 상태·누락된 의존성·잘못된 설정 때문에 `void` 메서드를 중단하는 경우가 대상이다.
```csharp
// 프로퍼티 폴백
public int CreatureId => _creatureData?.Id ?? 0;

// 얼리 리턴 가드
public virtual float TakeDamage(float damage, bool isCritical)
{
    if (!IsValid() || damage <= 0f) return 0f;
    // ...
}

// Null-conditional 호출
_eventBus?.Subscribe<PassiveItemAddedEvent>(OnPassiveItemAdded);

// 딕셔너리 안전 접근
if (!_stats.TryGetValue(type, out var stat))
{
    Debug.LogError($"Failed type error {type}");
    return 0;
}

// void 메서드에서 예외/방어 처리로 중단할 때는 로그 필수
if (_player == null)
{
    Debug.LogError($"[{GetType().Name}] Missing dependency: {nameof(_player)}");
    return;
}
```

### 비동기 (UniTask)

- 반환 타입은 `async UniTask` — `async void` 사용 금지
- VContainer 엔트리포인트는 `async Awaitable`
- `CancellationToken` 은 마지막 파라미터로, 기본값 `= default`
- 병렬 처리: `UniTask.WhenAll(tasks)`

```csharp
public async UniTask DoWorkAsync(CancellationToken cancellationToken = default)
{
    await UniTask.WaitUntil(() => _service.IsReady, cancellationToken: cancellationToken);
}
```

### 에러 핸들링

게임 로직에서 try/catch 사용 지양 — 사전 유효성 검사로 방지한다.

```csharp
Debug.LogError($"[{GetType().Name}] Missing dependency: {nameof(_weaponFactory)}");
Debug.LogWarning($"[{GetType().Name}] Stat type {type} not found, returning 0.");
```

### 주석 스타일

- 한국어·영어 혼용 — 주변 파일 스타일을 따른다
- 공개 API 중 계약이 불명확한 경우: XML `/// <summary>` 사용
- 인라인 `//` 는 로직이 자명하지 않을 때만
- `#region` 은 `Const.cs` 류의 상수 파일에만 제한적으로 사용

---

## 신규 콘텐츠 추가 가이드

### 무기 추가
1. `Enum.cs` 의 `EWeaponType` 에 값 추가
2. `Controller/Weapon/` 에 `*WeaponController : BaseWeaponController<TData, TStat>` 생성
3. 대응하는 `*WeaponData` / `*WeaponStatData` 생성
4. `GameSceneLifeScope` 에 등록
5. `WeaponFactory.GetWeapon()` 에 매핑 추가

### 투사체 추가
1. `ProjectileController` 상속, 필요 시 payload 필드 추가
2. 프리팹을 `ProjectilePrefab` Addressable 그룹에 등록
3. `ProjectileFactory` 로 생성, `_onReleaseAction` 으로 반환

### 상태 이상 추가
1. `BaseGamePlayEffect` 상속, `StackRule` 과 지속시간 설정
2. `GameDataService` 이펙트 딕셔너리에 항목 추가
3. `EffectType` 열거형에 값 추가

### 몬스터 행동 추가
1. `BaseMonsterBehaviour` 상속
2. `EMonsterRoleType` 열거형에 값 추가
3. `MonsterBehaviourFactory` 에 매핑 추가

### 이벤트 추가
1. `Event/GameEvents.cs` 에 `struct` 추가 (`Event` 접미사 필수)
2. `OnEnable`/`Initialize` 에서 구독, `OnDisable`/`Dispose` 에서 해제
3. 핸들러 시그니처: `private void OnFoo(in FooEvent ev)`

### 서비스 추가
1. `Service/` 에 클래스 생성, 필요 시 `IInitializable` + `IDisposable` 구현
2. 생성자 주입, `private readonly` 필드
3. `GameSceneLifeScope` 에 `RegisterEntryPoint`(라이프사이클 있을 경우) 또는 `Register` 로 등록

---

## 주요 기술 스택

| 라이브러리 | 용도 |
|---|---|
| **VContainer** | 의존성 주입 |
| **UniTask** | 비동기 처리 (코루틴 대체) |
| **UniRx** | 통화/경험치 레지스트리의 `BehaviorSubject` (반응형 UI) |
| **DOTween** | 트윈·UI 애니메이션 |
| **Addressables** | `ResourceService` 경유 에셋 로드 |
| **New Input System** | 플레이어 입력·조이스틱 |

---

## 자주 하는 실수 (Common Pitfalls)

- **EventBus 구독 후 반드시 해제** — 해제 누락 시 파괴된 오브젝트에서 이벤트가 발생함
- **풀링된 오브젝트 `Destroy` 금지** — `_onReleaseAction` 으로 반환
- **`base.OnEnable()` / `base.OnDisable()` 호출 생략 금지** — 베이스 클래스에 구독 로직이 있을 수 있음
- **서비스의 주입 필드는 `readonly`** — 생성 후 재할당 금지
- **이벤트 타입은 반드시 `struct`** — EventBus가 `where T : struct` 제약을 가짐
- **Factory를 통하지 않은 직접 `new` 금지** — 의존성 그래프가 깨짐
