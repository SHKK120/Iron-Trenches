# Browser Playable Preview

`index.html`을 더블클릭하면 Preview Hub가 열린다. 모든 Preview는 별도 설치, 로컬 서버 또는 외부 CDN 없이 실행된다.

- `index.html`: Preview 선택, 설명과 상태만 제공하는 Hub
- `RTS_PLAYTEST_03A_SupplyStrategy.html`: 현재 보급 차단 전략 Gameplay Microgame
- `RTS_CORE_03A_SupplyConnectivity.html`: 완료된 전략 보급 규칙/연결 판독 Preview
- `RTS_CORE_02C_ConstructionOwnership.html`: 완료된 건설 완공 + 일반 건물 지역 소유권 Preview
- `RTS_CORE_02B_FreeConstruction.html`: 완료된 Sector-Scoped Free Construction Preview
- `RTS_CORE_02A_Economy.html`: 완료된 Sector Income + Economy Preview
- `RTS_CORE_01C_Territory.html`: 완료된 Territory Graph + Control Anchor Preview

## RTS-PLAYTEST-03A 현재 시나리오

- 청군 5개 Squad Token을 클릭/드래그/Shift로 선택하고 우클릭 이동 또는 공격 이동
- 적 전방을 바로 공격하는 정면 돌파와 북부·남부 보급로 점령 후 공격을 모두 허용
- 적 전방은 보급로 하나만 살아 있어도 `SUPPLIED`, 두 보급로가 모두 끊기면 `CUT OFF`
- `[시험 규칙]` Cut Off는 이미 배치된 적 병력의 능력을 바꾸지 않고 새 전방 증원만 중단
- 단순 자동 전투, Control Anchor 점령, 방어 Red AI, 승리/패배, Reset과 결과 통계 제공
- 전투·점령·증원 수치는 모두 Browser Gameplay 검증용 `[시험값]`

## RTS-CORE-03A 규칙 시나리오

- 서부 후방 보급원에서 중앙·남부 두 경로를 따라 동부 전방까지 청군 보급 연결
- 적군이 중앙을 점령해 첫 경로를 끊어도 남부 우회로로 동부 보급 유지
- 적군이 남부까지 점령하면 동부는 청군 소유를 유지한 채 보급 차단 상태
- 동부 전방 보급창은 별도 상태 저장 없이 소속 지역을 통해 같은 보급 상태를 표시
- 청군이 남부를 재점령하면 우회 경로와 동부 보급이 다시 연결
- 전체 초기화로 소유권, 전선, 보급 연결, 건물 상태와 기록 복원

## 책임 경계

- 순수 C# Core와 `ManagedPcChecks`가 기술 정본이다.
- `RTS_CORE_03A_SupplyConnectivity.html`은 Supply 규칙과 연결 판독용 Preview다.
- `RTS_PLAYTEST_03A_SupplyStrategy.html`은 보급 차단 전략의 실제 RTS 조작/재미 검증용 비정본 Gameplay Validation Layer다.
- Browser Preview와 Microgame은 사람의 조작·가독성·플레이 감각 확인용 표현 계층이다.
- 보급 연결은 현재 소유권·인접 관계·보급원에서 파생하며 Preview JavaScript를 기술 정본으로 사용하지 않는다.
- Microgame의 `Cut Off = 적 전방 Reinforcement 중단`은 사람 Playtest 전용 `[시험 규칙]`이며 C# Core나 최종 보급 규칙이 아니다.
- 사용자 화면 문구는 한글을 기본으로 하고, 코드 식별자와 Result Bundle 번호만 필요한 범위에서 영어를 유지한다.
- 사각형 Sector와 원형 Footprint는 Preview/검증용 시험 표현이며 최종 맵 Geometry나 Collider 확정이 아니다.
- Preview PASS는 Unity Compile, Scene, 입력, 물리, 렌더링 또는 Play Mode PASS를 뜻하지 않는다.
- 새 규칙을 Preview에 반영할 때는 같은 시작 상태, 조작과 기대 결과를 `ManagedPcChecks`에도 유지한다.
