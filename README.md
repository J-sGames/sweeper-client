# Sweeper Client

Sweeper는 세로 화면에서 스와이프로 공을 발사해 벽돌을 제거하는 Unity 2D 게임 클라이언트다. 로그인과 자동 로그인, 점수 등록, 라운드 진행, 게임 오버, 오디오 설정을 포함한다.

## 개발 환경

- Unity `6000.3.11f1`
- Universal Render Pipeline 17
- Input System 1.19
- uGUI 2.0
- 기본 방향: Portrait
- 목표 프레임: 60 FPS

정확한 패키지 버전은 `Packages/manifest.json`을 기준으로 한다.

## 실행 방법

1. Unity Hub에서 저장소 루트를 프로젝트로 연다.
2. Unity 버전 `6000.3.11f1`을 사용한다.
3. `Assets/Scenes/Main.unity`를 연다.
4. Play Mode를 실행한다.

빌드에 포함된 씬 순서는 다음과 같다.

1. `Assets/Scenes/Main.unity` — 로그인, 회원가입, 게임 진입
2. `Assets/Sweeper/Scenes/Play.unity` — 실제 게임 플레이

`Assets/Scenes/GameplayTest.unity`는 개발 및 테스트용 씬이며 현재 빌드 목록에는 없다.

## 게임 흐름

```text
앱 시작
 └─ 저장된 refresh token 확인
     ├─ 유효: 자동 로그인 → Main의 인증 완료 화면
     └─ 없음/만료: 로그인 화면
         └─ 로그인 또는 회원가입
             └─ 게임 시작 → Play
                 └─ 스와이프 발사
                     └─ 모든 공 귀환 → 다음 벽돌 행 생성
                         ├─ 계속 플레이
                         └─ 벽돌이 종료선을 넘음 → 게임 오버 → 점수 등록
```

## 조작법

- 터치 또는 마우스 드래그: 발사 방향과 세기 지정
- 손가락 또는 마우스 버튼 놓기: 공 발사
- `Esc`: Play 씬에서 설정 열기, 열린 설정 닫기
- 설정 메뉴: 전체 음량, 음소거, 메인 메뉴 이동, 게임 종료

발사는 최소 거리와 위쪽 방향 조건을 만족해야 한다. 한 번 발사된 모든 공이 돌아오기 전에는 다음 발사를 할 수 없다.

## 핵심 시스템

### 게임 플레이

- `PlaySceneBootstrap`: Play 씬의 구성 진입점. 카메라, 플레이필드, 발사 시스템과 게임 오버 콜백을 연결한다.
- `SwipeLaunchInput`: 터치와 마우스 입력을 해상도 독립적인 스와이프 값으로 변환한다.
- `BallVolleyController`: 여러 공을 순차 발사하고, 전부 귀환하면 다음 라운드를 시작한다.
- `BrickRowSpawner`: 라운드마다 기존 행을 내리고 새 벽돌 행과 공 증가 아이템을 생성한다.
- `BrickBlock`: 벽돌 체력, 피격, 파괴를 처리한다.
- `GameScore`: 벽돌 피격 `+1`, 파괴 `+100`, 플레이 중 매초 `-1` 규칙으로 점수를 관리하며 0 미만으로 내려가지 않는다.
- `GameComponentMap`: `Playing`과 `GameOver` 상태에 따라 입력, 점수, 게임 오버 UI를 전환한다.

### 인증과 네트워크

- `ApiClient`: `UnityWebRequest` 기반 GET/POST, JSON 변환, 오류 정규화, 401 재시도를 담당한다.
- `AuthManager`: 로그인 상태와 자동 로그인을 관리하는 영속 싱글턴이다.
- `TokenStorage`: access token은 메모리에, refresh token은 Windows DPAPI 보호 저장소에 보관한다.
- `GameOverView`: 게임 종료 시 `/api/result/achieve`로 점수를 전송한다.

상세한 API, 응답 envelope, 토큰 갱신 흐름은 [통신 구조 문서](Docs/NETWORKING.md)를 참고한다.

모든 씬에서 유지되는 랭킹 버튼으로 페이지당 10개의 순위와 점수를 조회할 수 있다.

```http
GET /api/result/ranking?page={page}&pageSize={pageSize}
```

### UI와 설정

- `AuthUI`: 로그인, 회원가입, 로그아웃 및 서버 응답 팝업
- `ScoreUI`: 점수 변경 이벤트를 구독해 화면 갱신
- `GameOverController` / `GameOverView`: 최종 점수, 등록 상태, 재시작 및 메인 메뉴 이동
- `SceneControlsUI`: 씬과 무관하게 유지되는 설정 오버레이
- `SafeAreaFitter`: 모바일 기기의 안전 영역 반영

플레이어 이름과 오디오 설정은 `PlayerPrefs`에 저장된다.

## 디렉터리 구조

```text
Assets/
├─ Scenes/                    # Main 및 개발용 씬
└─ Sweeper/
   ├─ Art/                    # 스프라이트, 머티리얼, 시각 효과
   ├─ Audio/                  # 효과음과 음악
   ├─ Prefabs/                # 재사용 게임 오브젝트
   ├─ Scenes/                 # Play 씬
   ├─ Scripts/
   │  ├─ Core/                # 부트스트랩, 점수, 게임 상태
   │  ├─ Input/               # 스와이프 입력
   │  ├─ Gameplay/            # 공, 보드, 벽돌, 카메라 효과
   │  ├─ Networking/          # HTTP, 인증, 토큰, DTO
   │  ├─ UI/                  # 런타임 UI
   │  ├─ Editor/              # 씬 및 프리팹 생성 도구
   │  └─ Debug/               # 입력 및 궤적 진단
   └─ Tests/                  # EditMode 및 PlayMode 테스트
Docs/                         # 설계 및 개발 문서
Packages/                     # Unity 패키지 선언
ProjectSettings/              # Unity 프로젝트 설정
```

## Editor 도구

Unity 상단 `Sweeper` 메뉴에서 프로젝트용 생성·정비 도구를 사용할 수 있다.

- Main 메뉴 씬 재구축
- Play 씬 구성 보조
- 공 및 벽돌 프리팹 설정
- HTTP 연결 확인 창

씬 재구축 도구는 씬 오브젝트를 다시 만들 수 있으므로 실행 전에 변경 사항을 저장하고 diff를 확인한다.

## 개발 시 유의사항

- 씬과 프리팹 참조는 `[SerializeField]`로 연결되므로 스크립트 변경 후 Inspector의 누락 참조를 확인한다.
- Unity가 생성하는 `.meta` 파일을 대응 에셋과 함께 커밋한다.
- DTO는 `[Serializable]`과 public 필드를 사용하며 서버 JSON 필드명과 정확히 맞춘다.
- 현재 결과 등록은 인증용 `AuthManager.Api`가 아닌 별도 `ApiClient`를 사용한다.
- Windows 외 플랫폼을 배포하려면 refresh token의 안전한 저장 구현이 필요하다.
- `Library`, `Temp`, `Logs`, `obj` 같은 Unity 생성물은 소스 변경으로 취급하지 않는다.

기여 및 자동화 작업 규칙은 [AGENTS.md](AGENTS.md)를 참고한다.
