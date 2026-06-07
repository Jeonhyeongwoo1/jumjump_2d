# AGENTS.md

## Highest Priority: Required Dependency Null Checks

- Required dependencies resolved through DI, constructors, `Construct`, `Bind`, `Awake`, or `Initialize` must be validated once at that boundary and then used directly.
- Do not add repeated defensive null checks for required dependencies in gameplay logic, event handlers, `Update`, `FixedUpdate`, or `Tick`.
- If a required dependency is missing, fail fast in the initialization boundary with `Debug.LogError` and disable/abort that component or service setup.
- Keep null checks only for runtime state that can legitimately be absent, such as the current player before spawn, an optional camera, nullable event payload objects, or pooled objects returned from a factory.
- Before finishing code changes, search touched files for repeated required dependency checks such as `_eventBus == null`, `_configData == null`, `_playerRegistry == null`, `_platformFactory == null`, and replace them with initialization-time validation unless the dependency is explicitly optional.

Recommended check:

```powershell
rg -n "_eventBus == null|_configData == null|_playerRegistry == null|_platformFactory == null" Assets\Game\02.Scripts
```

## JumJump Mobile WebGL 작업 기준

- JumJump의 최종 목표 플랫폼은 **모바일 브라우저용 Unity WebGL 빌드**다.
- Codex는 구현, 리팩터링, 리소스 추가, UI 변경 시 Mobile WebGL 제약을 우선 고려한다.
- 기능이 동작하더라도 모바일 WebGL에서 프레임, 메모리, 빌드 용량, 로딩 시간이 과도하면 완료로 보지 않는다.
- 반복 생성되는 오브젝트는 `Destroy` 대신 풀링을 우선 사용한다.
- `Update` 계열 메서드에서는 GC allocation, LINQ, 반복 문자열 생성, 임시 컬렉션 생성을 피한다.
- 스프라이트, 오디오, 배경, 스킨 리소스는 MVP 단계에서 최소 세트만 포함하고, 사용하지 않는 리소스는 빌드에 들어가지 않게 관리한다.
- Addressables 또는 리소스 로딩 구조를 사용할 때는 로딩 시점과 해제 시점을 함께 설계한다.
- 모바일 터치 입력, 세로 화면, Safe Area, 브라우저 주소창 높이 변화를 기본 UX 조건으로 본다.
- BGM/효과음은 모바일 브라우저 자동 재생 정책을 고려해 사용자 첫 입력 이후 활성화한다.
- 광고, IAP, 분석 SDK는 WebGL 지원 여부와 번들 크기를 확인한 뒤 도입한다.
- 중요한 변경 후에는 가능하면 Unity Editor 테스트뿐 아니라 WebGL 빌드 또는 브라우저 실행 검증을 수행한다.

This file provides guidance to OpenAI Codex when working with code in this repository.

프로젝트 아키텍처, 코드 스타일, 컨벤션 전체는 **[AI_GUIDELINES.md](./AI_GUIDELINES.md)** 를 참조한다.  
아래는 Codex 전용 보충 사항이다.

---

## Codex 전용 참고사항

### SubAgent 운영 문서
- 기능 구현, 리팩터링, 버그 수정 작업 전에는 **[SUBAGENTS_GUIDE.md](./SUBAGENTS_GUIDE.md)** 를 먼저 확인한다.
- 사용자가 `구현해줘`, `만들어줘`, `리팩터링해줘`, `고쳐줘` 같은 요청을 했을 때는 기본적으로 `SUBAGENTS_GUIDE.md`의 작업 루프를 따른다.
- 현재 세션/도구 제약으로 실제 SubAgent를 사용할 수 없는 경우에도, 메인 에이전트는 동일한 단계(분석 -> 구현 -> 테스트/검증 -> 수정 -> 재검증)를 직접 수행한다.
- 사용자가 `subagent`, `agent`, `병렬`, `delegation` 같은 표현을 함께 쓰면 실제 SubAgent 활용을 우선 검토한다.

### 작업 전 필수 파악 파일
1. `AI_GUIDELINES.md` — 아키텍처·스타일 전체
2. `Assets/Game/02.Scripts/GameSceneLifeScope.cs` — 전체 DI 배선
3. `Assets/Game/02.Scripts/Enum.cs` — 모든 열거형
4. `Assets/Game/02.Scripts/Event/GameEvents.cs` — 모든 이벤트 타입

### 코드 생성 규칙
- **네이밍**: private 필드 `_camelCase`, 클래스·메서드·프로퍼티 `PascalCase`, 인터페이스 `I` 접두사
- **직접 `new` 금지**: 오브젝트 생성은 반드시 해당 Factory 클래스 경유
- **`async void` 금지**: 비동기 메서드 반환 타입은 `async UniTask`
- **불필요한 `static` 금지**: `static class`가 아닌 이상 메서드·필드·프로퍼티에 `static`을 추가하지 않는다
- **이벤트 타입은 `struct`**: `where T : struct` 제약 준수
- **`[SerializeField]` 필드는 `private`**: `public` 선언 금지
- **주입 의존성은 `private readonly`**: 생성 후 재할당 금지
- **필수 컴포넌트 / 의존성 null 체크 반복 금지**: `_animator == null` 같은 방어 코드를 각 메서드마다 반복하지 않는다. `Awake`, `Initialize`, `Construct`, `Bind` 단계에서 한 번 resolve / validate 하고, 실패 시 `Debug.LogError`로 fail-fast 한다.
- **`AddComponent` / `GetComponent` 남발 금지**: `AddComponent`, `GetComponent`, `GetComponentInChildren`, `GetComponentInParent` 호출을 로직 곳곳에서 반복하지 않는다. 컴포넌트 참조는 `Awake`, `Initialize`, `Construct`, `Bind` 단계에서 한 번만 resolve / cache 하고 재사용한다. 런타임 중 반복 탐색이나 조건부 `AddComponent`는 예외적인 경우에만 허용하며, 왜 필요한지 코드상 책임이 분명해야 한다.
- **일반 클래스의 `static` 사용 제한**: `Utils`, `Helper`처럼 타입 자체가 `static class`인 경우가 아니면 `static` 메서드, `static` 필드, `static` 프로퍼티를 추가하지 않는다. 공용 동작이 필요하면 전용 `static class`로 분리하거나, 그렇지 않으면 인스턴스 책임으로 유지한다.
- **행동별 예외 규칙은 행동 객체가 책임진다**: 공용 상태 전이 함수(`SetState`, `ChangeCondition`, `RefreshCrowdControlState`)에는 모든 개체에 공통인 기본 규칙만 둔다. 특정 행동(`Charge`, `Dash`, 패턴 캐스팅 등) 동안에만 달라지는 상태 면역, 군중제어 무시, 입력 잠금, 슈퍼아머 같은 예외는 해당 행동을 구현한 Behaviour/Pattern이 직접 활성화·해제하고, 종료 시 공용 상태를 재평가한다.

### 파일 생성 규칙
- 파일명 = 클래스명 (예: `WeaponService.cs`)
- 파일 1개 = 클래스 1개
- 새 인터페이스는 `Assets/Game/02.Scripts/Interface/` 에 별도 파일로 생성
- 새 이벤트 struct는 기존 파일 `Event/GameEvents.cs` 에 추가 (새 파일 생성 금지)
- 새 열거형 값은 기존 `Enum.cs` 에 추가

### 금지 패턴
```csharp
// 금지: 정적 싱글턴
public static MyService Instance { get; private set; }

// 금지: FindObjectOfType
var player = FindObjectOfType<PlayerController>();

// 금지: async void
private async void LoadAsync() { }

// 금지: static class가 아닌데 static 멤버 사용
public class DamageService
{
    public static int Count;
}

// 금지: Destroy (풀링 오브젝트)
Destroy(projectile.gameObject);

// 금지: public SerializeField
[SerializeField] public Transform _target;
```
