# Browser Playable Preview

`index.html`을 더블클릭하면 Preview Hub가 열린다. 모든 Preview는 별도 설치, 로컬 서버 또는 외부 CDN 없이 실행된다.

- `index.html`: Preview 선택, 설명과 상태만 제공하는 Hub
- `RTS_CORE_02C_ConstructionOwnership.html`: 현재 건설 완공 + 일반 건물 지역 소유권 Preview
- `RTS_CORE_02B_FreeConstruction.html`: 완료된 Sector-Scoped Free Construction Preview
- `RTS_CORE_02A_Economy.html`: 완료된 Sector Income + Economy Preview
- `RTS_CORE_01C_Territory.html`: 완료된 Territory Graph + Control Anchor Preview

## 02C 현재 시나리오

- 중앙에는 적군의 완공 보급창, 동부에는 적군의 완공 병영이 초기 배치됨
- 청군 수입 +65를 회수하고 서부 자유 위치에 병영 건설 현장을 배치
- 선택한 현장을 명시적 버튼으로 완공하고 Owner/Sector/Position이 유지되는지 확인
- 중앙 점령 시 중앙 보급창만 청군으로 이전되고 소속 지역은 중앙로 유지
- 동부 병영은 중앙 점령의 영향을 받지 않고 적군 소유 유지
- Reset으로 전체 시작 상태 복원

## 책임 경계

- 순수 C# Core와 `ManagedPcChecks`가 기술 정본이다.
- Browser Preview는 사람의 조작·가독성·플레이 감각 확인용 표현 계층이다.
- 사용자 화면 문구는 한글을 기본으로 하고, 코드 식별자와 Result Bundle 번호만 필요한 범위에서 영어를 유지한다.
- 사각형 Sector와 원형 Footprint는 Preview/검증용 시험 표현이며 최종 맵 Geometry나 Collider 확정이 아니다.
- Preview PASS는 Unity Compile, Scene, 입력, 물리, 렌더링 또는 Play Mode PASS를 뜻하지 않는다.
- 새 규칙을 Preview에 반영할 때는 같은 시작 상태, 조작과 기대 결과를 `ManagedPcChecks`에도 유지한다.
