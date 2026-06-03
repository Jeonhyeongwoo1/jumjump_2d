# CLAUDE.md

This file provides guidance to Claude Code (claude.com/claude-code) when working with code in this repository.

프로젝트 아키텍처, 코드 스타일, 컨벤션, Mobile WebGL 제약 전체는 **[AI_GUIDELINES.md](./AI_GUIDELINES.md)** 를 참조한다.
아래는 Claude Code 전용 보충 사항이다.

---

## JumJump Mobile WebGL 작업 기준 (요약)

상세 기준은 `AI_GUIDELINES.md` 의 *Target Platform: Mobile WebGL* 섹션을 따른다. 핵심만:

- JumJump의 최종 목표 플랫폼은 **모바일 브라우저용 Unity WebGL 빌드**다.
- 구현·리팩터링·리소스 추가·UI 변경 시 Mobile WebGL 제약(프레임, 메모리, 빌드 용량, 로딩 시간)을 우선 기준으로 본다.
- 기능이 동작하더라도 모바일 WebGL 비용이 과도하면 완료로 보지 않는다.
- 반복 생성 오브젝트는 `Destroy` 대신 풀링을 우선 사용한다.
- `Update` 계열에서 GC allocation, LINQ, 반복 문자열 생성, 임시 컬렉션 생성을 피한다.
- BGM/효과음은 모바일 브라우저 자동 재생 정책상 사용자 첫 입력 이후 활성화한다.
- 중요한 변경 후에는 Unity Editor 테스트뿐 아니라 가능하면 WebGL 빌드 또는 브라우저 실행 검증을 수행한다.

---

## Claude Code 전용 참고사항

### 작업 루프
- 사용자가 `구현해줘`, `만들어줘`, `리팩터링해줘`, `고쳐줘` 같은 요청을 하면 **분석 → 구현 → 테스트/검증 → 수정 → 재검증** 루프를 따른다.
- 검색·탐색 범위가 넓거나(여러 디렉터리·네이밍 컨벤션 스윕), 독립적으로 병렬화 가능한 작업은 `Agent`(서브에이전트) 도구 활용을 검토한다. 단순·단일 경로 작업은 메인 에이전트가 직접 수행한다.

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
- **`AddComponent` / `GetComponent` 남발 금지**: 컴포넌트 참조는 `Awake`, `Initialize`, `Construct`, `Bind` 단계에서 한 번만 resolve / cache 하고 재사용한다. 런타임 중 반복 탐색이나 조건부 `AddComponent`는 책임이 분명한 예외적인 경우에만 허용한다.
- **일반 클래스의 `static` 사용 제한**: `Utils`, `Helper`처럼 타입 자체가 `static class`인 경우가 아니면 `static` 멤버를 추가하지 않는다. 공용 동작이 필요하면 전용 `static class`로 분리하거나 인스턴스 책임으로 유지한다.
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
