# AGENTS.md

이 문서는 이 저장소에서 작업하는 자동화 에이전트와 개발자를 위한 프로젝트 규칙이다. 저장소 전체에 적용되며, 더 하위 디렉터리에 별도 `AGENTS.md`가 생기면 해당 범위에서는 하위 문서를 우선한다.

## 프로젝트 목표

Sweeper Client는 Unity 기반 세로형 2D 벽돌 제거 게임이다. 변경 시 다음 특성을 유지한다.

- 터치와 마우스에서 동일하게 작동하는 스와이프 조작
- 해상도 및 안전 영역에 대응하는 세로 UI
- 한 발리의 모든 공이 귀환한 뒤 다음 라운드로 넘어가는 결정적 흐름
- 명시적인 `Playing` / `GameOver` 상태 전환
- 인증 토큰과 민감정보의 안전한 처리

## 기술 기준

- Unity: `6000.3.11f1`
- 언어: C#
- 렌더링: URP 2D
- 입력: Unity Input System
- UI: uGUI
- 네트워크: `UnityWebRequest` + 코루틴
- 직렬화: Unity `JsonUtility`

Unity 및 패키지 버전을 임의로 올리지 않는다. 변경이 필요한 작업에서는 영향 범위와 마이그레이션 결과를 함께 기록한다.

## 주요 진입점

- 시작 씬: `Assets/Scenes/Main.unity`
- 플레이 씬: `Assets/Sweeper/Scenes/Play.unity`
- Play 구성 루트: `Assets/Sweeper/Scripts/Core/PlaySceneBootstrap.cs`
- 게임 상태 연결: `Assets/Sweeper/Scripts/Core/GameComponentMap.cs`
- 인증 진입점: `Assets/Sweeper/Scripts/Networking/AuthManager.cs`
- 공통 HTTP: `Assets/Sweeper/Scripts/Networking/ApiClient.cs`
- 게임 종료: `Assets/Sweeper/Scripts/UI/GameOverController.cs`

시스템 설명은 `README.md`, 네트워크 상세는 `Docs/NETWORKING.md`를 먼저 확인한다.

## 작업 원칙

1. 요청 범위와 관련된 코드, 씬, 프리팹, 테스트를 먼저 확인한다.
2. 사용자의 기존 변경을 보존하고 관련 없는 파일을 포맷하거나 수정하지 않는다.
3. 가장 작은 변경으로 기존 컴포넌트 경계와 이벤트 흐름을 유지한다.
4. 런타임에 자동 탐색할 수 있다는 이유로 Inspector 참조 누락을 숨기지 않는다. 기존 코드가 명시적 참조를 쓰면 동일한 방식을 따른다.
5. 에셋을 추가하거나 이동할 때 `.meta` 파일도 함께 관리한다.
6. 씬 및 프리팹 YAML을 직접 대량 편집하지 않는다. 가능한 경우 Unity Editor나 기존 Builder 도구를 사용한다.
7. 자동 생성 도구로 씬을 재구축했다면 의도하지 않은 오브젝트 삭제나 직렬화 변경이 없는지 diff를 확인한다.

## 코드 스타일

- 기존 네임스페이스 구조를 따른다: `Sweeper.Core`, `Sweeper.Input`, `Sweeper.Gameplay.*`, `Sweeper.Networking`, `Sweeper.UI`.
- 타입과 public 멤버는 PascalCase, private 필드는 `_camelCase`, 직렬화 필드는 기존 파일의 스타일을 따른다.
- Unity 생명주기 메서드는 `Awake`, `OnEnable`, `Start`, `Update`, `OnDisable`, `OnDestroy` 흐름을 명확히 유지한다.
- 이벤트를 구독한 컴포넌트는 대칭되는 생명주기에서 반드시 해제한다.
- Inspector 값에는 의미 있는 `[Header]`, `[Min]`, `[Range]`를 사용한다.
- 프레임마다 불필요한 오브젝트 탐색이나 할당을 추가하지 않는다.
- 게임 규칙을 UI 컴포넌트에 넣지 않고 Core 또는 Gameplay 계층에 둔다.
- 공개 동작이 불분명한 경우에만 짧은 XML 문서나 이유를 설명하는 주석을 추가한다.

## 아키텍처 경계

### Core

씬 구성과 상위 게임 상태를 담당한다. Gameplay 세부 구현을 직접 복제하지 않고 컴포넌트를 연결한다.

### Input

장치 입력을 게임에서 사용할 수 있는 값과 이벤트로 변환한다. Gameplay가 `Touchscreen`이나 `Mouse`를 직접 읽지 않도록 한다.

### Gameplay

- `Ball`: 발사, 이동, 반사, 귀환과 발리 수명주기
- `Bricks`: 벽돌 체력, 행 생성, 난이도 진행, 게임 오버 판정
- `Board`: 카메라에 맞춘 경기장 경계와 귀환 영역
- `CameraEffects`: 게임 카메라 효과

시스템 간 통지는 가능한 경우 기존 이벤트를 사용한다. 예: `SwipeReleased`, `VolleyCompleted`, `GameFailed`, `ScoreChanged`.

### UI

상태를 표시하고 사용자 명령을 상위 시스템에 전달한다. 점수 계산, 벽돌 배치, 인증 토큰 조작을 UI에 구현하지 않는다.

### Networking

HTTP 전송과 인증 상태를 담당한다. DTO는 `Networking/DTO`에 두고 화면 코드가 원시 JSON을 직접 분석하지 않도록 한다.

## 네트워크 변경 규칙

- 새 API는 공통 `ApiClient`의 `Get` 또는 `Post`를 우선 사용한다.
- 인증이 필요하면 `AuthManager.Instance.Api`와 `authenticated: true`를 사용한다.
- 요청과 응답 DTO는 `[Serializable]` public 필드로 정의하고 JSON 이름과 대소문자를 맞춘다.
- 최상위 배열은 `JsonUtility` 제약이 있으므로 서버 응답 계약을 확인한 뒤 wrapper DTO를 사용한다.
- `401` 자동 갱신과 1회 재시도 동작을 우회하거나 중복 구현하지 않는다.
- 비밀번호와 모든 종류의 토큰을 일반 로그에 남기지 않는다. 새 민감 필드가 생기면 `ApiClient`의 로그 마스킹 목록도 갱신한다.
- UI 오류 문구는 `ErrorCode`, HTTP 상태, 서버 메시지의 우선순위를 기존 방식과 일치시킨다.
- API 계약을 추측해 DTO를 만들지 않는다. 응답 예시, 인증 여부, pagination 기준을 확인한다.

현재 미연결 라우트:

```http
GET /api/result/ranking?page={page}&pageSize={pageSize}
```

## 씬과 UI 변경 규칙

- `Main`과 `Play`라는 씬 이름은 코드 상수와 Build Settings에서 사용되므로 변경 시 모든 참조를 함께 갱신한다.
- 모바일 safe area를 유지하고 기준 해상도 `1080x1920`에서 레이아웃을 확인한다.
- 설정 패널은 Play 씬에서 `Time.timeScale`을 일시 중지하며 닫힐 때 이전 값을 복원해야 한다.
- 게임 오버 화면은 중복 표시와 점수 중복 전송을 방지해야 한다.
- 런타임 생성 UI를 변경할 때 sorting order와 입력 차단 범위를 확인한다.
- 사용자 문자열은 현재 한국어 UI와 영문 게임 상태 문구의 기존 톤을 따른다.

## 검증

변경 규모에 맞춰 아래를 수행한다.

- C# 컴파일 오류와 Unity Console 오류 확인
- 관련 EditMode / PlayMode 테스트 실행
- Main → 로그인 → Play 씬 전환 확인
- 스와이프, 발리 종료, 새 행 생성, 게임 오버 흐름 확인
- UI 변경 시 portrait 기준과 서로 다른 화면 비율 확인
- 네트워크 변경 시 성공, 서버 오류, 연결 실패, `401` 갱신 성공/실패 확인
- 씬 또는 프리팹 변경 시 누락된 serialized reference 확인

Unity를 실행할 수 없는 환경에서는 수행하지 못한 검증을 완료한 것처럼 표현하지 말고, 정적 확인 결과와 남은 수동 검증을 구분해 기록한다.

## 완료 조건

- 요청한 동작이 구현되어 있다.
- 관련 없는 사용자 변경이 보존되어 있다.
- 새 파일의 `.meta` 필요 여부를 확인했다.
- 가능한 검증이 통과했다.
- 남은 제한이나 서버 계약 미확정 사항이 문서화되어 있다.
- 구조가 바뀌었다면 `README.md` 또는 `Docs/NETWORKING.md`도 함께 갱신되어 있다.
