# AIT Build Config Policy

JumJump의 AIT dev build와 production build는 빌드 산출물 호환성에 영향을 주는 설정을 동일하게 유지한다.
현재 목표는 모바일 브라우저용 Unity WebGL을 Apps in Toss WebView에서 실행하는 것이므로, dev와 production이 서로 다른 WebGL 런타임 조건을 가지면 로컬에서 잡은 문제가 배포 후 다시 발생할 수 있다.

## 원칙

- Dev와 production의 WebGL build config는 기본적으로 같아야 한다.
- 차이는 빌드 산출물 자체가 아니라 실행 환경, 배포 메타데이터, 인증 모드, 로그 수준처럼 운영 단계의 차이로 제한한다.
- Production profile에서 `-1`처럼 Unity ProjectSettings를 암묵적으로 상속하는 값은 피하고, dev와 같은 권장값을 명시한다.
- 설정을 다르게 해야 한다면 이유와 검증 방법을 이 문서에 먼저 기록한다.

## 동일해야 하는 설정

| 항목 | 기준 |
|---|---|
| WebGL Template | `AITTemplate` |
| Compression Format | Disabled / none |
| Decompression Fallback | Disabled |
| Threads Support | Disabled |
| WebGL Memory | dev/prod 동일, 현재 기준 1024 MB |
| Data Caching | Disabled |
| Addressables Player Build | Build Addressables with Player Build enabled |
| Addressables Data Builder | Player/WebGL 빌드는 Packed Build / Default Build Script 기준 |
| Vite/Web Server Header | 실제 압축된 파일에만 `Content-Encoding` 설정 |
| WASM Header | `.wasm`은 `Content-Type: application/wasm` |

이 설정들이 달라지면 다음 문제가 다시 발생할 수 있다.

- `.unityweb` 파일이 생성되거나, 압축되지 않은 파일에 `Content-Encoding`이 붙어 WebGL 로딩 실패
- `Unable to parse *.framework.js.unityweb` 같은 압축/헤더 불일치 에러
- Addressables catalog 또는 bundle 경로가 dev와 production에서 달라져 key lookup 실패
- dev server에서는 통과했지만 Toss WebView production에서만 로딩이 멈추는 문제
- 메모리, 스레드, 캐싱 차이로 모바일 WebGL에서만 재현되는 문제

## 달라도 되는 항목

아래 항목은 dev와 production이 다를 수 있다. 단, 빌드 산출물 호환성에 영향을 주지 않는 범위여야 한다.

| 항목 | 설명 |
|---|---|
| `IS_PRODUCTION` | production에서는 true. 로컬 fallback auth를 막고 Toss SDK 인증 경로를 사용한다. |
| App metadata | app name, display name, icon, deployment id 등 배포 메타데이터 |
| Host/port | dev server의 `localhost`, LAN IP, Vite port 등 |
| Auth fallback | 로컬 dev에서만 mock/dev tossHash 허용 가능. production에는 포함하지 않는다. |
| Debug logs | 개발 편의를 위한 로그는 dev에서 더 자세할 수 있다. |

특히 production 빌드에서 일반 PC 브라우저나 모바일 브라우저로 직접 접속하면 `ReactNativeWebView`와 Apps in Toss native bridge가 없기 때문에 인증 실패가 정상일 수 있다. 이 경우는 build config 문제가 아니라 실행 환경 차이다.

## Production 검증 체크리스트

1. Unity에서 AIT Clean 후 Publish를 실행한다.
2. `webgl/Build`와 `ait-build/dist/web/Build`에 `.unityweb` 파일이 남아 있지 않은지 확인한다.
3. `.wasm`, `.data`, `.framework.js`, `.loader.js`가 압축 확장자 없이 생성됐는지 확인한다.
4. 로컬 dev server에서는 리소스, Addressables, 레이아웃만 1차 검증한다.
5. 인증, Toss SDK, `ReactNativeWebView`, Safe Area 최종 검증은 Toss Apps in Toss 환경에 업로드해서 확인한다.

## 결론

JumJump에서는 dev와 production의 build config를 다르게 가져갈 실익이 거의 없다. 오히려 설정 차이가 WebGL 압축, Addressables, SDK 초기화 문제를 숨기거나 새로 만들 수 있다.

따라서 기본 정책은 다음과 같다.

```text
Dev build config == Production build config
Production difference == runtime/deployment/auth environment only
```
