# AI_GUIDELINES.md

## Highest Priority: Required Dependency / Component Rules

- DI로 주입되는 필수 의존성은 null 체크하지 않는다. 생성자, `Construct`, `Bind`, `Initialize`에서 받은 필수 인자는 존재를 전제로 하고 직접 사용한다.
- 필수 `[SerializeField]` 컴포넌트는 프리팹/씬에서 반드시 연결되어 있어야 한다. 코드에서 `GetComponent`, `GetComponentInChildren`, `GetComponentInParent`로 fallback resolve 하지 않는다.
- 금지 예시: `if (_eventBus == null)`, `if (_configData == null)`, `if (_animator == null) _animator = GetComponent<Animator>();`, `if (_spriteRenderer == null) _spriteRenderer = GetComponentInChildren<SpriteRenderer>();`
- null 체크는 현재 플레이어처럼 spawn 전에는 없을 수 있는 런타임 상태, 명시적 optional dependency, nullable 이벤트 payload, 풀/팩토리 실패 반환값에만 둔다.

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
- 발판, 플로팅 점수 텍스트, 배경 오브젝트, UI 팝업 등 반복 생성되는 오브젝트는 풀링 대상이다.
- `Destroy` 남발을 금지하고, 재사용 가능한 오브젝트는 비활성화 후 풀로 반환한다.
- 큰 텍스처, 오디오, 애니메이션은 초기 로딩에 모두 넣지 않고 필요한 시점에 로드한다.
- 사용하지 않는 Addressable 리소스는 참조를 해제한다 (`ResourceService.Release` / `Dispose`).
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
- 로컬 저장은 우선 `PlayerPrefs`를 사용하되, 데이터 크기를 작게 유지한다 (예: 최고 점수 저장).
- 외부 SDK, 광고, 결제, 분석 도구는 WebGL 지원 여부와 번들 크기를 확인한 뒤 추가한다.
- 동기 로딩으로 긴 프리즈를 만들지 않는다. 에셋 로드는 `ResourceService.PreLoadAsync` 로 시작 시 일괄 처리한다.

### UI/UX for Mobile WebGL

- 화면은 세로형 모바일 뷰포트를 기본으로 한다.
- 터치 영역은 충분히 크게 잡고, 작은 아이콘만으로 핵심 조작을 요구하지 않는다.
- Safe Area, 브라우저 주소창 높이 변화, 화면 비율 차이를 고려한다.
- 게임 중 UI는 점수처럼 필수 정보만 최소한으로 유지한다.
- 결과(게임오버) 화면은 느린 모바일 브라우저에서도 즉시 반응해야 한다.

### Implementation Rule

- 새 기능을 추가할 때는 기능 완성뿐 아니라 Mobile WebGL에서의 비용을 함께 검토한다.
- 리소스를 추가할 때는 예상 빌드 용량, 메모리 점유, 로딩 시점을 함께 기록한다.
- 성능에 영향을 줄 수 있는 변경은 Unity Profiler, WebGL 빌드 실행, 브라우저 테스트 중 최소 하나로 검증한다.

공통 AI 가이드라인 — Claude Code(`CLAUDE.md`)와 OpenAI Codex(`AGENTS.md`) 양쪽에서 참조합니다.

---

## 프로젝트 개요

JumJump는 **모바일 세로 화면용 2D 점프 게임**이다.
플레이어는 화면 좌/우에서 번갈아 나타나 중앙으로 이동해 오는 발판을, 탭 입력으로 점프해 착지하며 위로 올라간다.
착지에 성공하면 점수가 오르고, 발판을 놓치면(side hit / miss) 넉백되며 게임 오버된다.
발판에는 다양한 **기믹**(작아짐, 빨라짐, 느려짐, 유령, 더블, 미리보기 등)이 점수에 따라 확률적으로 부여된다.

- 엔진: **Unity 6000.3.6f1**
- 단일 씬 구조: `Assets/Game/01.Scenes/GameScene.unity`
- 빌드는 Unity Editor 표준 Build Settings 사용. 별도 빌드 스크립트 없음.
- 스크립트 루트: `Assets/Game/02.Scripts/`, 리소스 루트: `Assets/Game/03.Resources/`

---

## 아키텍처

### 의존성 주입 (VContainer)

**`Assets/Game/02.Scripts/GameSceneLifeScope.cs`** 가 씬 전체의 DI 배선 파일.
모든 서비스·팩토리·레지스트리·프레젠터·상태가 여기서 등록된다.
`FindObjectOfType`, 정적 싱글턴 사용 금지 — 반드시 주입으로 해결한다.

```csharp
builder.RegisterInstance(_gameConfigData);                                // ScriptableObject 인스턴스
builder.Register<IEventBus, EventBus>(Lifetime.Scoped);                   // 인터페이스 바인딩
builder.Register<PlatformFactory>(Lifetime.Scoped);                       // 일반 서비스/팩토리
builder.Register<UIGameScenePresenter>(Lifetime.Scoped);                  // Presenter (순수 C#)
builder.RegisterEntryPoint<GameFlowService>(Lifetime.Scoped).AsSelf();    // 라이프사이클 포함
builder.RegisterEntryPoint<GameBootstrapService>(Lifetime.Scoped).AsSelf(); // IAsyncStartable
builder.RegisterComponent(_followCamera);                                 // 씬에 존재하는 MonoBehaviour
```

- 라이프사이클(`IInitializable` / `ITickable` / `IDisposable` / `IAsyncStartable`)이 있는 서비스는 `RegisterEntryPoint(...).AsSelf()` 로 등록한다.
- 씬에 미리 배치된 MonoBehaviour(카메라, 풀 루트, UI 컨테이너 등)는 `[SerializeField]` 로 참조를 잡고 `RegisterComponent` 로 등록한다.

### 게임 흐름 (State Machine)

`GameFlowService` (`IInitializable` / `ITickable` / `IDisposable` / `IGameFlowStateContext`) 가 상태 전환을 관리한다:

```
Ready → Playing → GameOver → (Restart) → Ready
```

- 상태 타입: `GameStateType { Ready, Playing, GameOver }` (`Enum.cs`)
- 각 상태는 `IGameFlowState` 를 구현하고 VContainer로 주입된다:
  `ReadyGameFlowState`, `PlayingGameFlowState`, `GameOverGameFlowState` (`Service/GameFlowState/`)
- `IGameFlowState` 멤버: `StateType`, `OnEnter()`, `OnUpdate()`, `OnExit()`, `OnTapRequested(context)`, `OnPlayerMissedLanding(context)`, `OnRestartRequested(context)`
- 상태는 입력 이벤트를 직접 받지 않는다. `GameFlowService` 가 EventBus 이벤트(`TapRequestedEvent`, `PlayerMissedLandingEvent`, `RestartRequestedEvent`)를 받아 현재 상태에 위임한다.
- 상태 전환은 `IGameFlowStateContext.ChangeState(GameStateType)` 를 통한다. 전환 시 이전 상태 `OnExit()` → 새 상태 `OnEnter()`.

### 비동기 부트스트랩

`GameBootstrapService` (`IAsyncStartable`) 가 시작 순서를 보장한다:

```
ResourceService.PreLoadAsync → 각 Factory/Presenter.Warmup (풀 등록) → Player 스폰
→ PlayerSpawnedEvent 발행 → GameResourcesReadyEvent 발행
```

`GameResourcesReadyEvent` 이후에만 게임 시작 로직(발판 배치, 입력 처리, UI 생성)이 동작하도록 보장한다.

### 레이어 구조

| 레이어 | 위치 | 역할 |
|---|---|---|
| Controller | `Controller/` | 엔티티 로직 (`Player`, `PlatformController`) 과 발판 보조 타입 (`PlatformVisual`, `PlatformMotion`, `PlatformLandingResolver`) |
| Service | `Service/` | 비즈니스 로직·크로스 시스템 조율 (흐름, 점수, 입력, 리소스, 풀, UI, 배경, 발판 스폰) |
| Service/GameFlowState | `Service/GameFlowState/` | 게임 흐름 상태 구현체 |
| Service/PlatformGimmick | `Service/PlatformGimmick/` | 발판 기믹 행동 구현체 |
| Factory | `Factory/` | 오브젝트 생성 (직접 `new` 금지, 반드시 Factory 경유) |
| Registry | `Registry/` | 활성 오브젝트 인메모리 추적 (소유권 없음): `PlayerRegistry`, `PlatformRegistry` |
| Presenter | `Presenter/` | UI MVP 패턴. View(`BaseSceneUI`/`BasePopup` 파생) + Presenter(순수 C# 클래스) 쌍 |
| Event | `Event/` | `EventBus` + 이벤트 struct 정의 (`GameEvents.cs`) |
| Interface | `Interface/` | 모든 인터페이스 (파일 1개 = 인터페이스 1개) |
| Data | `Data/` | 순수 데이터 (`GameConfigData`, `PlatformCheatData`, `PlatformGimmickSetting`, `BackgroundDepthLayer`) |
| Util | `Util/` | `static` 헬퍼·상수 (`GameConst`, `ButtonUtils`) |
| Camera | `Camera/` | `VerticalFollowCamera` |

---

## 핵심 시스템

### 발판 시스템

`PlatformController` (MonoBehaviour) 가 발판 1개를 담당한다. 책임을 보조 타입으로 분리한다:

| 타입 | 종류 | 책임 |
|---|---|---|
| `PlatformController` | MonoBehaviour | 풀 수명, 물리, 착지 판정 조율, 기믹 Tick 호출 |
| `PlatformVisual` | `internal struct` | 스프라이트·콜라이더·애니메이터·카메라 가시성·스케일·알파 |
| `PlatformMotion` | `internal struct` | 목표 X로의 이동, 일시정지/재개, 속도 스케일 |
| `PlatformLandingResolver` | `internal static class` | 착지/사이드히트/스택착지 가능 여부의 순수 판정 함수 |

- 생성: `PlatformFactory` (풀 경유). 스폰 로직: `PlatformSpawnService`.
- 착지 판정은 `PlatformController` 의 트리거/충돌 콜백과 `Player` 의 콜백 양쪽에서 `TryResolveLanding` / `TryResolveStackedLanding` 으로 진입한다.

### 발판 기믹 시스템

- 기믹 타입: `PlatformGimmickType { Normal, Small, Fast, Slow, SmallAndFast, Ghost, Double, Reveal }`
- 인터페이스: `IPlatformGimmickBehaviour` — `Type`, `RequiresTick`, `Reset`, `Apply`, `Tick`, `OnLanding`
- 베이스: `BasePlatformGimmickBehaviour` — 공통 스케일/속도/페이드 랜덤 헬퍼 제공
- 구현체는 `Service/PlatformGimmick/` 에 타입별 1파일
- `PlatformGimmickBehaviourFactory` 가 타입 → 행동 인스턴스를 배열로 매핑. 미등록 타입은 `Normal` 로 폴백.
  (`Double` 은 행동 객체가 아니라 `PlatformSpawnService` 의 예약 스폰 로직으로 처리된다.)
- `PlatformGimmickSetting` (`Data/`) 이 타입별 시작 점수·스폰 확률·스케일 범위 등을 보유. `GameConfigData` 가 배열로 소유.
- `PlatformCheatData` 로 특정 기믹을 강제 스폰해 테스트할 수 있다.

**행동별 예외 규칙은 행동 객체가 책임진다.** 공용 상태 전이 메서드에는 모든 발판 공통 규칙만 두고, 특정 기믹에서만 달라지는 동작(미리보기 알파, 유령 페이드, 더블 예약 등)은 해당 Behaviour / SpawnService가 활성화·해제한다.

### 풀링 시스템

`PoolService` 가 키 기반으로 `ComponentPool<T>` (Unity `ObjectPool<T>` 래퍼) 를 관리한다.

```csharp
_poolService.Register<T>(key, createFunc, onGet, onRelease, prewarmCount);
var instance = _poolService.Get<T>(key);
_poolService.Release(key, instance);
```

- 풀 등록은 각 Factory / Presenter 의 `Warmup()` 에서 1회 수행하고, `GameBootstrapService` 가 부트스트랩 중 호출한다.
- 풀 대상: `Player`, `Platform`, `UI_DynamicFont` 등.
- 풀링 오브젝트는 `Destroy` 금지 — 비활성화 후 `Release` 로 반환한다.

### 리소스 시스템 (Addressables)

`ResourceService` 가 Addressables 로딩/해제의 단일 접근점이다.

- `PreLoadAsync(ct)` — `GameConfigData.PreLoadLabel` 라벨의 에셋을 시작 시 일괄 로드.
- `GetPrefab(key)` / `GetAsset<T>(key)` — 프리로드 이후 **동기** 접근. 프리로드 전 호출은 에러.
- 핸들을 캐싱해 중복 로드를 막고, `Dispose` 시 일괄 해제한다.

### UI 시스템 (MVP 패턴)

#### 역할 분리

| 역할 | 클래스 | 책임 |
|---|---|---|
| **View** | `UI_Xxx : BaseSceneUI` / `BasePopup` | `[SerializeField]` 로 UI 요소 보유. 데이터 표시 메서드(`SetScore`, `SetCountdown` 등)와 입력 콜백 주입 메서드(`AddEvents`/`RemoveEvents`)만 제공. 비즈니스 로직 없음. |
| **Presenter** | `UIXxxPresenter` | 순수 C# 클래스. VContainer Scoped 등록. EventBus 구독, 상태 판단, View 메서드 호출. `Bind(view)` / `Unbind()` 로 View 수명 관리. |

- **View 클래스명 = 프리팹명 = Addressable 키** (예: `UI_GameScene`, `UI_GameOverPopup`).
- View는 입력 콜백을 `AddEvents(Action, ...)` 로 Presenter에게서 주입받는다. `Awake` 에서 버튼 리스너를 등록하지 않는다.
- 버튼 리스너 연결은 `ButtonUtils.SetListener(button, action)` 로 한다 (내부에서 `RemoveAllListeners` 후 `AddListener`).
- `[SerializeField]` 참조는 null 체크 없이 직접 접근한다 — 미연결 시 `NullReferenceException` 이 즉시 드러나도록 한다.

#### 베이스 / Canvas

- `BaseSceneUI` (abstract MonoBehaviour) — 씬 루트 HUD View. `Awake` 에서 `Canvas` 캐시 + `sortingOrder = GameConst.UI.SceneUISortingOrder(100)`.
- `BasePopup` (abstract MonoBehaviour) — 팝업 View. `Awake` 에서 `Canvas` 캐시 + `sortingOrder = GameConst.UI.PopupSortingOrder(1000)`. `OnShown()` / `OnHidden()` 가상 메서드(애니메이션 훅).
- 프리팹 루트에는 반드시 `Canvas` 와 View 컴포넌트가 있어야 한다.

#### 생성·관리

| 타입 | 생성 / 관리 |
|---|---|
| `BaseSceneUI` 파생 (View) | `UIService.Create<T>(key)` → 타입별 캐시. `UIFactory` 가 Addressable 로드 + `InjectGameObject` + `GetComponent<T>`. |
| `BasePopup` 파생 (View) | `PopupService.Push<T>(key)` / `Pop()` / `PopAll()` — 스택 + 타입별 캐시. 최초 Push 시 1회 생성 후 재사용. |
| `UIXxxPresenter` | VContainer 생성자 주입. 해당 GameFlowState 또는 부트스트랩이 보유. |

#### Bind 패턴

```csharp
// Scene UI: 생성 후 명시적으로 Bind
var view = _uiService.Create<UI_GameScene>(_configData.GameSceneUiAddressableKey);
_scenePresenter.Bind(view);

// Popup: Presenter가 Show() 내부에서 Push + AddEvents + 카운트다운 일괄 처리
_popupPresenter.Show();  // 내부: Push → AddEvents → 카운트다운 시작
_popupPresenter.Hide();  // 내부: 카운트다운 취소 → PopAll
```

상태(`IGameFlowState`)가 팝업 Presenter를 통해 열고 닫는다: `OnEnter()` → `presenter.Show()`, `OnExit()` → `presenter.Hide()`.

### 이벤트 시스템 (EventBus)

제네릭·타입 안전·제로 할당(`struct` 이벤트). `IEventBus` / `EventBus` (`Event/`).

```csharp
// 정의 (Event/GameEvents.cs) — 반드시 struct, Event 접미사
public struct PlayerLandedEvent
{
    public PlatformController Platform { get; private set; }
    public PlayerLandedEvent(PlatformController platform) { Platform = platform; }
}

// 구독 / 해제
_eventBus.Subscribe<PlayerLandedEvent>(OnPlayerLanded);
_eventBus.Unsubscribe<PlayerLandedEvent>(OnPlayerLanded);

// 발행
_eventBus.Publish(new PlayerLandedEvent(platform));

// 핸들러 — 반드시 in 키워드 사용
private void OnPlayerLanded(in PlayerLandedEvent ev) { }
```

현재 이벤트 목록(`GameEvents.cs`): `TapRequestedEvent`, `RestartRequestedEvent`, `GameStartedEvent`, `GameResetEvent`, `GameStateChangedEvent`, `GameResourcesReadyEvent`, `PlayerSpawnedEvent`, `PlayerJumpRequestedEvent`, `PlayerJumpStartedEvent`, `PlayerLandedEvent`, `PlayerMissedLandingEvent`, `PlatformsResetEvent`, `ScoreChangedEvent`, `GameOverEvent`.

- 구독 위치: `Initialize` / `OnEnable` / `Start` → 대응하는 `Dispose` / `OnDisable` / `OnDestroy` 에서 반드시 해제.
- Presenter는 `Bind` 에서 구독, `Unbind` 에서 해제.

### 점수 · 입력 · 카메라 · 배경

- `ScoreService` (`IInitializable`/`IDisposable`) — `PlayerLandedEvent` 마다 가산, `RestartRequestedEvent` 에 0으로 초기화, 최고 점수는 `PlayerPrefs` 저장. `ScoreChangedEvent` 발행.
- `InputActionTapService` — New Input System 액션으로 탭 입력을 받아 `TapRequestedEvent` 발행.
- `VerticalFollowCamera` — 플레이어를 수직으로 부드럽게 추적.
- `BackgroundEnvironmentService` + `BackgroundEnvironmentFactory` — 고도에 따른 그라디언트 배경과 패럴랙스 오브젝트를 풀로 관리.

### 데이터 시스템

- `GameConfigData` (ScriptableObject) — 모든 런타임 튜닝값과 Addressable/Pool 키의 단일 소스. `RegisterInstance` 로 주입.
- `PlatformGimmickSetting` — 기믹별 스폰/스케일 설정. `GameConfigData` 가 배열로 소유.
- `PlatformCheatData` (ScriptableObject) — 디버그용 강제 기믹 스폰.
- `BackgroundDepthLayer` — 배경 패럴랙스 깊이 레이어 설정.

---

## 코드 스타일 & 컨벤션

### 네이밍 규칙

| 심볼 | 규칙 | 예시 |
|---|---|---|
| private 필드 | `_camelCase` | `_jumpElapsed`, `_eventBus` |
| `[SerializeField]` 필드 | `[SerializeField] private` + `_camelCase` | `[SerializeField] private Animator _animator;` |
| 프로퍼티 | `PascalCase` | `public bool IsJumping => ...` |
| 메서드 | `PascalCase` | `public void Initialize()` |
| 클래스 | `PascalCase` | `PlatformController` |
| 인터페이스 | `I` + `PascalCase` | `IEventBus`, `IPlatformGimmickBehaviour` |
| 열거형 타입/값 | `PascalCase` | `PlatformGimmickType.Ghost` |
| 이벤트 struct | `PascalCase` + `Event` 접미사 | `PlayerLandedEvent` |
| 상수 | `PascalCase` | `GameConst.UI.PopupSortingOrder` |

`[SerializeField]` 필드는 반드시 `private` — `public` 으로 선언 금지.

### 클래스 내부 레이아웃 순서

```
1. public 프로퍼티 / expression-bodied getter
2. public Action<> 콜백
3. [SerializeField] private 필드
4. private 필드 (주입된 의존성은 private readonly)
5. [Inject] public void Construct(...)   ← MonoBehaviour만
6. public 메서드 (Bind, Initialize, Warmup 포함)
7. protected virtual/override 메서드
8. private 메서드
9. Unity 라이프사이클 (Awake, Start, OnEnable, OnDisable, OnDestroy, Update, FixedUpdate)
```

### 의존성 주입 패턴

**서비스 / Presenter (non-MonoBehaviour) — 생성자 주입, `readonly` 필드:**
```csharp
public sealed class ScoreService : IInitializable, IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly GameConfigData _configData;

    public ScoreService(IEventBus eventBus, GameConfigData configData)
    {
        _eventBus = eventBus;
        _configData = configData;
    }
}
```

**MonoBehaviour — `[Inject]` 메서드 주입, 메서드명 `Construct`:**
```csharp
[Inject]
public void Construct(IEventBus eventBus, GameConfigData configData)
{
    _eventBus = eventBus;
    _configData = configData;
}
```

**Factory가 VContainer 이후 빌드한 오브젝트 — 수동 주입 (`Bind` / `InjectDependencies`):**
```csharp
public void Bind(IEventBus eventBus, PlayerRegistry playerRegistry, GameConfigData configData, Camera gameCamera)
{
    _eventBus = eventBus;
    // ...
}
```

### 의존성·참조 검증 (fail-fast)

의존성과 컴포넌트 참조는 **진입 단계(`Awake` / `Construct` / `Bind` / `Initialize`)에서 1회만 resolve·validate** 하고, 누락 시 `Debug.LogError` 로 즉시 실패시킨다.

- 각 메서드마다 `_dep == null` 가드를 반복하지 않는다.
- `[SerializeField]` 참조는 null 체크 없이 직접 접근한다 — 프리팹 미연결을 `NullReferenceException` 으로 즉시 노출시킨다. 방어 코드(`if (_ref == null)`, `?.`)는 버그를 숨긴다.
- `AddComponent` / `GetComponent` 는 진입 단계에서 1회 cache. 런타임 반복 탐색·조건부 `AddComponent` 금지.

```csharp
// 진입 단계 1회 validate (PlatformController.Awake 패턴)
private void Awake()
{
    if (_spriteRenderer == null || _landingCollider == null || _rigidbody == null || _animator == null)
    {
        Debug.LogError($"[{nameof(PlatformController)}] Missing required component.");
        enabled = false;
        return;
    }
    // 이후 메서드들은 null 체크 없이 직접 사용
}
```

### 널 처리 & 가드

- `void` 메서드를 예상치 못한 상태·누락 의존성 때문에 중단할 때는 `Debug.LogWarning`/`LogError` 를 먼저 남긴다.
- 단순 필터링·정상 흐름의 값 반환(`return 0`, `return false`)은 로그 없이 얼리 리턴.

### 비동기 (UniTask)

- 반환 타입은 `async UniTask` — `async void` 사용 금지.
- 발사 후 망각은 `.Forget()`.
- `CancellationToken` 은 마지막 파라미터, 기본값 `= default`. 취소 가능한 루프는 `CancellationTokenSource` 로 관리하고 `OnDestroy`/`Hide` 에서 취소·해제.
- 프레임 대기는 `await UniTask.Yield(PlayerLoopTiming.Update, ct)`.

### `static` 사용 제한

- `Utils`/`Helper` 처럼 타입 자체가 `static class` 인 경우가 아니면 `static` 멤버를 추가하지 않는다.
- 정적 싱글턴(`public static Instance`) 금지.

### 에러 핸들링

게임 로직에서 try/catch 지양 — 사전 유효성 검사로 방지한다.

```csharp
Debug.LogError($"[{GetType().Name}] Missing dependency: {nameof(_eventBus)}");
```

### 주석 스타일

- 한국어·영어 혼용 — 주변 파일 스타일을 따른다.
- 계약이 불명확한 공개 API에만 XML `/// <summary>`.
- 인라인 `//` 는 로직이 자명하지 않을 때만.

---

## 신규 콘텐츠 추가 가이드

### 발판 기믹 추가
1. `Enum.cs` 의 `PlatformGimmickType` 에 값 추가
2. `Service/PlatformGimmick/` 에 `XxxPlatformGimmickBehaviour : BasePlatformGimmickBehaviour` 생성
3. `PlatformGimmickBehaviourFactory` 생성자에 `Register(...)` 추가
4. `GameConfigData` 의 `PlatformGimmickSettings` 에 `PlatformGimmickSetting` 항목 추가

### 이벤트 추가
1. `Event/GameEvents.cs` 에 `struct` 추가 (`Event` 접미사 필수, 새 파일 생성 금지)
2. 구독은 `Initialize`/`OnEnable`/`Bind`, 해제는 `Dispose`/`OnDisable`/`Unbind`
3. 핸들러 시그니처: `private void OnFoo(in FooEvent ev)`

### 서비스 추가
1. `Service/` 에 클래스 생성, 필요 시 `IInitializable` + `IDisposable` 구현
2. 생성자 주입, `private readonly` 필드
3. `GameSceneLifeScope` 에 `RegisterEntryPoint`(라이프사이클 있을 때) 또는 `Register` 로 등록

### Scene UI 추가
1. `BaseSceneUI` 파생 View(`UI_Xxx`) 와 `UIXxxPresenter` 생성 (`Presenter/`)
2. 프리팹 루트에 Canvas + View 컴포넌트, Addressable 키 = View 클래스명
3. `GameConfigData` 에 Addressable 키 프로퍼티 추가
4. `GameSceneLifeScope` 에 Presenter 를 Scoped 등록
5. 적절한 시점에 `UIService.Create<T>(key)` → `presenter.Bind(view)`

### Popup 추가
1. `BasePopup` 파생 View 와 `UIXxxPresenter` 생성
2. 프리팹 루트에 Canvas + View 컴포넌트(Sort Order 관리), Addressable 키 = View 클래스명
3. `GameConfigData` 에 Addressable 키 프로퍼티 추가
4. `GameSceneLifeScope` 에 Presenter 를 Scoped 등록
5. `presenter.Show()` 호출 (내부에서 Push + Bind 처리)

### 풀링 오브젝트 추가
1. 프리팹을 Addressable `PreLoad` 라벨에 등록
2. `GameConfigData` 에 Addressable 키·Pool 키·prewarm 수 추가
3. 담당 Factory/Presenter 의 `Warmup()` 에서 `PoolService.Register`
4. `GameBootstrapService.StartAsync` 에서 `Warmup()` 호출

---

## 주요 기술 스택

| 라이브러리 | 용도 |
|---|---|
| **VContainer** | 의존성 주입 |
| **UniTask** | 비동기 처리 (코루틴 대체) |
| **Addressables** | `ResourceService` 경유 에셋 로드 |
| **New Input System** | 탭 입력 |
| **TextMeshPro** | 텍스트 렌더링 (점수, 카운트다운, 플로팅 점수) |

---

## 자주 하는 실수 (Common Pitfalls)

- **EventBus 구독 후 반드시 해제** — 해제 누락 시 파괴된 오브젝트에서 핸들러가 호출됨
- **풀링된 오브젝트 `Destroy` 금지** — `Release` 로 풀에 반환
- **`base.Awake()` 호출 생략 금지** — 베이스(`BaseSceneUI`/`BasePopup`)에 Canvas 설정이 있음
- **서비스/Presenter의 주입 필드는 `readonly`** — 생성 후 재할당 금지
- **이벤트 타입은 반드시 `struct`** — EventBus가 `where T : struct` 제약을 가짐
- **Factory를 통하지 않은 직접 `new` 금지** — 의존성 그래프·풀링이 깨짐
- **`[SerializeField]` 에 방어적 null 체크 금지** — 프리팹 미연결 버그를 숨김
- **매직넘버를 코드에 직접 박지 않기** — `GameConst` 또는 `GameConfigData` 로 분리
