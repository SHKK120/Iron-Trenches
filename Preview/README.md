# Browser Playable Preview

`index.html`을 브라우저로 직접 열면 별도 설치, 로컬 서버 또는 외부 CDN 없이 현재 Source-Only 규칙의 플레이 감각을 확인할 수 있다.

## 현재 시나리오

- 시작: Blue West — Red Center — Red East
- 연결: West ↔ Center ↔ East
- 시작 전선: West-Center
- `Center 점령 완료` 뒤 전선: Center-East
- `초기화`로 시작 상태 복원

## 책임 경계

- 순수 C# Core와 `ManagedPcChecks`가 기술 정본이다.
- Browser Preview는 사람의 조작·가독성·플레이 감각 확인용 표현 계층이다.
- Preview PASS는 Unity Compile, Scene, 입력, 물리, 렌더링 또는 Play Mode PASS를 뜻하지 않는다.
- 새 규칙을 Preview에 반영할 때는 같은 시작 상태, 조작과 기대 결과를 `ManagedPcChecks`에도 유지한다.
