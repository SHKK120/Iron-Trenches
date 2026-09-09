# Browser Playable Preview

`index.html`을 더블클릭하면 Preview Hub가 열린다. 모든 Preview는 별도 설치, 로컬 서버 또는 외부 CDN 없이 실행된다.

- `index.html`: Preview 선택, 설명과 상태만 제공하는 Hub
- `RTS_CORE_02B_FreeConstruction.html`: 현재 Sector-Scoped Free Construction Preview
- `RTS_CORE_02A_Economy.html`: 완료된 Sector Income + Economy Preview
- `RTS_CORE_01C_Territory.html`: 완료된 Territory Graph + Control Anchor Preview

## 02B 현재 시나리오

- Sector를 먼저 선택하고 Barracks/Depot를 선택한 뒤 해당 Sector 안의 자유 위치에 Ghost를 이동
- Green Ghost는 배치 가능, 다른 Sector·경계 침범·충돌·자금 부족은 Red와 이유로 표시
- Blue 초기 수입 +65를 회수하고 West 자유 위치에 Barracks Site를 배치하면 Balance 65→15
- Center는 Red일 때 건설 불가이며 Anchor 점령 뒤 새 Blue 건설 영역과 다음 수입 +125로 전환
- 각 Construction Site에서 Type, Owner와 정확히 하나의 Sector 소속을 확인
- Reset으로 전체 시작 상태 복원

## 책임 경계

- 순수 C# Core와 `ManagedPcChecks`가 기술 정본이다.
- Browser Preview는 사람의 조작·가독성·플레이 감각 확인용 표현 계층이다.
- 사각형 Sector와 원형 Footprint는 Preview/검증용 시험 표현이며 최종 맵 Geometry나 Collider 확정이 아니다.
- Preview PASS는 Unity Compile, Scene, 입력, 물리, 렌더링 또는 Play Mode PASS를 뜻하지 않는다.
- 새 규칙을 Preview에 반영할 때는 같은 시작 상태, 조작과 기대 결과를 `ManagedPcChecks`에도 유지한다.
