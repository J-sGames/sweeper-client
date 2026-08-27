# 통신 구조 요약

## 1. 개요

클라이언트의 HTTP 통신은 Unity의 `UnityWebRequest`와 코루틴을 기반으로 동작한다. 공통 요청 처리는 `ApiClient`, 인증 상태 관리는 `AuthManager`, 토큰 보관은 `TokenStorage`, 서버 DTO는 `Networking/DTO` 아래 클래스들이 담당한다.

현재 구현된 서버 통신은 크게 두 흐름으로 나뉜다.

- 인증: 회원가입, 로그인, Google 로그인, 토큰 갱신, 내 정보 조회, 로그아웃
- 게임 결과: 게임 종료 후 점수 등록

## 2. 주요 구성 요소

### `ApiClient`

모든 HTTP 요청의 생성, 전송, 응답 변환을 담당한다.

- `Get<T>(path, done, authenticated)`
- `Post<TRequest, TResponse>(path, body, done, authenticated)`
- 기준 URL과 상대 경로를 `/`로 연결해 최종 URL을 만든다.
- POST 본문은 `JsonUtility.ToJson`으로 직렬화하고 `Content-Type: application/json`을 설정한다.
- `authenticated`가 `true`이면 `Authorization: Bearer {accessToken}` 헤더를 추가한다.
- 응답은 `ApiResult<T>`로 정규화해 콜백에 전달한다.
- 인증 요청이 `401`을 받으면 토큰을 한 번 갱신한 뒤 원래 요청을 한 번만 재시도한다.

통신은 비동기 코루틴 형태이므로 호출 측에서는 `StartCoroutine(...)` 또는 `yield return`으로 실행한다.

```csharp
StartCoroutine(api.Get<ResponseDto>(
    "api/example?page=1&pageSize=10",
    result => { /* 결과 처리 */ },
    authenticated: true));
```

### `ApiResult<T>`

호출 측에서 HTTP 및 서버 처리 결과를 동일한 형태로 다루기 위한 컨테이너다.

| 필드 | 의미 |
| --- | --- |
| `IsSuccess` | 최종 성공 여부 |
| `StatusCode` | HTTP 상태 코드 |
| `Response` | 역직렬화된 응답 데이터 |
| `Error` | 사용자 표시 또는 진단에 사용할 오류 메시지 |
| `ErrorCode` | 서버가 반환한 오류 코드 |

### `AuthManager`

인증 기능의 진입점이며 씬 전환 후에도 유지되는 싱글턴 `MonoBehaviour`다.

- 시작 시 저장된 refresh token이 있으면 자동 로그인을 시도한다.
- 로그인과 회원가입 전 클라이언트 입력값을 검증한다.
- 인증 성공 시 토큰을 저장하고 `SignedIn` 상태로 전환한다.
- 자동 로그인 시 토큰 갱신 후 `/api/auth/me`로 사용자 정보를 확인한다.
- `StateChanged` 이벤트를 통해 `AuthUI`에 상태와 오류 메시지를 알린다.

인증 상태는 다음 네 가지다.

| 상태 | 의미 |
| --- | --- |
| `Checking` | 자동 로그인 확인 중 |
| `SignedOut` | 로그아웃 상태 |
| `Busy` | 로그인, 가입 또는 로그아웃 요청 처리 중 |
| `SignedIn` | 인증 완료 |

### `TokenStorage`

- access token과 사용자 정보는 메모리에 보관한다.
- refresh token은 Windows DPAPI로 암호화해 `Application.persistentDataPath` 아래에 보관한다.
- 토큰 교체 시 refresh token 저장과 재확인까지 성공해야 전체 교체가 성공한 것으로 처리한다.
- 갱신 실패나 로그아웃 시 메모리 정보와 저장된 refresh token을 함께 삭제한다.
- 현재 안전한 영구 저장 구현은 Windows/Windows Editor에 한정되어 있다. 다른 플랫폼에서는 별도 네이티브 저장소 구현이 필요하다.

## 3. 요청 및 응답 형식

DTO는 Unity `JsonUtility`가 처리할 수 있도록 `[Serializable]` 클래스와 public 필드로 정의한다. JSON 필드명과 C# 필드명은 일치해야 한다.

성공 응답은 두 형식을 모두 지원한다.

### Envelope 응답

최상위 JSON에 `success` 속성이 있으면 아래 구조로 해석한다.

```json
{
  "success": true,
  "data": {},
  "errorCode": null,
  "message": null
}
```

이 경우 실제 응답 DTO는 `data`에서 읽는다. HTTP가 2xx여도 `success`가 `false`이면 최종 결과는 실패다.

### 직접 응답

최상위 JSON에 `success` 속성이 없으면 응답 전체를 `T`로 역직렬화한다.

```json
{
  "name": "Player",
  "score": 1000
}
```

실패 응답에서는 `errorCode`, `message`를 우선 읽는다. ASP.NET 형식의 validation problem 응답은 `detail`, `errors`의 첫 메시지, `title` 순서로 사용자 메시지를 추출한다.

## 4. 인증 및 토큰 갱신 흐름

```text
UI
 └─ AuthManager.Login/Register/GoogleLogin
     └─ ApiClient.Post
         └─ 성공: TokenStorage.Replace
             └─ AuthState.SignedIn

인증이 필요한 일반 요청
 └─ ApiClient.Get/Post(authenticated: true)
     ├─ 2xx: 응답 반환
     └─ 401: RefreshSingleFlight
         ├─ 갱신 성공: 원래 요청 1회 재시도
         └─ 갱신 실패: 토큰 삭제 후 실패 반환
```

동시에 여러 요청이 `401`을 받아도 refresh 요청은 하나만 실행된다. 나머지 요청은 진행 중인 갱신이 끝날 때까지 기다린 뒤 같은 결과를 사용한다.

## 5. 현재 API 목록

| Method | Path | 인증 헤더 | 요청 DTO | 응답 DTO | 호출 위치 |
| --- | --- | --- | --- | --- | --- |
| POST | `/api/auth/register` | 없음 | `RegisterRequest` | `AuthTokensResponse` | `AuthManager` |
| POST | `/api/auth/login` | 없음 | `LoginRequest` | `AuthTokensResponse` | `AuthManager` |
| POST | `/api/auth/google` | 없음 | `GoogleLoginRequest` | `AuthTokensResponse` | `AuthManager` |
| POST | `/api/auth/refresh` | 없음 | `RefreshRequest` | `AuthTokensResponse` | `ApiClient` |
| GET | `/api/auth/me` | 필요 | 없음 | `UserInfo` | `AuthManager` |
| POST | `/api/auth/logout` | 없음 | `LogoutRequest` | `ApiErrorResponse` | `AuthManager` |
| POST | `/api/result/achieve` | 현재 없음 | `ScoreRequest` | `ScoreResponse` | `GameOverView` |
| GET | `/api/result/ranking?page={page}&pageSize={pageSize}` | 없음 | 없음 | `RankingPageResponse` | `RankingUI` |

랭킹은 페이지 번호가 1부터 시작하며 현재 UI는 페이지당 10개를 요청한다.

```http
GET /api/result/ranking?page={page}&pageSize={pageSize}
```

응답의 `hasNext`로 다음 페이지 버튼을 활성화하고, 현재 페이지가 1보다 크면 이전 페이지 버튼을 활성화한다.

## 6. 게임 결과 등록의 별도 구조

`GameOverView`는 `AuthManager.Api`를 사용하지 않고 Inspector의 `baseUrl`로 별도의 `ApiClient`를 생성한다. 따라서 현재 점수 등록 요청에는 access token이 전달되지 않으며, 인증 API와 기준 URL 설정도 분리되어 있다.

처리 순서는 다음과 같다.

1. 게임 종료 점수와 시작/종료 시각으로 `ScoreRequest`를 만든다.
2. `/api/result/achieve`에 POST한다.
3. 성공하면 `SCORE SENT`, 실패하면 서버 메시지 또는 HTTP 상태를 표시한다.

`IResultView<T>`는 이 화면에서 요청 시작, 성공, 실패 처리를 나누기 위해 사용하는 UI 인터페이스다. 공통 네트워크 계층 자체의 필수 요소는 아니다.

## 7. 로깅과 민감정보 처리

요청마다 임의의 요청 ID, 메서드, 경로, 인증 여부, 소요 시간, 상태 코드를 Unity 로그에 기록한다. 요청과 응답 JSON도 기록하지만 다음 필드는 `[REDACTED]`로 치환한다.

- password 및 passwordConfirmation
- access token 및 refresh token
- Google ID token

본문 로그는 최대 4,000자로 제한된다.

## 8. 새 API 연결 시 확인 사항

- 서버 JSON과 정확히 일치하는 `[Serializable]` DTO를 정의한다.
- 응답이 envelope인지 직접 응답인지 확인한다.
- 배열 응답이라면 최상위 배열을 직접 사용하지 않고 객체 필드에 담긴 형태인지 확인한다. `JsonUtility`는 최상위 배열 처리에 제약이 있다.
- 인증이 필요하면 `AuthManager.Instance.Api`를 사용하고 `authenticated: true`를 전달한다.
- query string 값은 현재 별도 빌더가 없으므로 path에 포함한다. 사용자 입력 문자열이 들어간다면 URL 인코딩이 필요하다.
- 페이지 번호가 0부터인지 1부터인지, `pageSize`의 최소·최대값, 빈 페이지의 표현을 서버 계약에서 확인한다.
- 사용자에게 표시할 오류는 `ErrorCode`, `StatusCode`, `Error`를 함께 고려한다.

## 9. 관련 파일

- `Assets/Sweeper/Scripts/Networking/ApiClient.cs`
- `Assets/Sweeper/Scripts/Networking/ApiResult.cs`
- `Assets/Sweeper/Scripts/Networking/AuthManager.cs`
- `Assets/Sweeper/Scripts/Networking/TokenStorage.cs`
- `Assets/Sweeper/Scripts/Networking/AuthValidation.cs`
- `Assets/Sweeper/Scripts/Networking/DTO/AuthDTOs.cs`
- `Assets/Sweeper/Scripts/Networking/DTO/ScoreDTOs.cs`
- `Assets/Sweeper/Scripts/Networking/IResultView.cs`
- `Assets/Sweeper/Scripts/UI/AuthUI.cs`
- `Assets/Sweeper/Scripts/UI/GameOverView.cs`
