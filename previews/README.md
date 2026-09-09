# Browser Playable Preview

`index.html`을 더블클릭하면 Preview Hub가 열린다. 모든 Preview는 별도 설치, 로컬 서버 또는 외부 CDN 없이 실행된다.

- `index.html`: Preview 선택, 설명과 상태만 제공하는 Hub
- `RTS_CORE_02A_Economy.html`: 현재 Sector Income + Economy Preview
- `RTS_CORE_01C_Territory.html`: 완료된 Territory Graph + Control Anchor Preview

## 02A 현재 시나리오

- Blue 초기 Balance 0, 다음 Collection +65
- `수입 회수 시험` 뒤 Balance 65
- Anchor-C 점령 뒤 Center Red→Blue, 다음 Collection +125
- 다시 회수하면 Balance 190
- Frontline과 Event Log도 같은 조작에서 갱신
- Reset으로 전체 시작 상태 복원

## 책임 경계

- 순수 C# Core와 `ManagedPcChecks`가 기술 정본이다.
- Browser Preview는 사람의 조작·가독성·플레이 감각 확인용 표현 계층이다.
- Preview PASS는 Unity Compile, Scene, 입력, 물리, 렌더링 또는 Play Mode PASS를 뜻하지 않는다.
- 새 규칙을 Preview에 반영할 때는 같은 시작 상태, 조작과 기대 결과를 `ManagedPcChecks`에도 유지한다.
