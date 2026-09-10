# Browser Playable Preview

`index.html`을 더블클릭하면 Preview Hub가 열린다. 모든 Preview는 별도 설치, 로컬 서버 또는 외부 CDN 없이 실행된다.

- `index.html`: Preview 선택, 설명과 상태만 제공하는 Hub
- `RTS_CORE_03E_ProductionCaptureQueue.html`: 현재 Production Facility Capture + Queue Resolution 기술 Preview
- `RTS_CORE_03D_RouteThreatInterdiction.html`: 이전 Route Threat + Interdiction + Isolation Semantics 기술 Preview
- `RTS_CORE_03C_ProductionReinforcement.html`: 이전 Production + Reinforcement Source Integration 기술 Preview
- `RTS_CORE_03B_TerrainRoadReinforcement.html`: 이전 Terrain Movement + Road Network + Physical Reinforcement 기술 Preview
- `RTS_PLAYTEST_03A_R1_TerrainRoadInterdiction.html`: 사람 Gameplay Validation을 통과한 Terrain + Road + Physical Reinforcement Microgame
- `RTS_PLAYTEST_03A_SupplyStrategy.html`: `[대체됨 — R1]` 단순 보급 차단 Gameplay Microgame
- `RTS_CORE_03A_SupplyConnectivity.html`: 완료된 전략 보급 규칙/연결 판독 Preview
- `RTS_CORE_02C_ConstructionOwnership.html`: 완료된 건설 완공 + 일반 건물 지역 소유권 Preview
- `RTS_CORE_02B_FreeConstruction.html`: 완료된 Sector-Scoped Free Construction Preview
- `RTS_CORE_02A_Economy.html`: 완료된 Sector Income + Economy Preview
- `RTS_CORE_01C_Territory.html`: 완료된 Territory Graph + Control Anchor Preview

## RTS-CORE-03E 현재 기술 시나리오

- 기존 Territory Building Capture 결과를 사용해 완공 생산시설의 새 소유 진영 이전을 표시
- `[시험 규칙]` 점령된 생산시설의 진행 중·대기 중 생산 작업을 모두 취소하고 진행도를 소실
- 기존 소유 진영 환불 없음, 새 소유 진영에 기존 대기열·생산물 자동 이전 없음
- 점령 전 생산 완료된 출발 대기 병력과 이미 이동 중인 병력은 기존 진영과 객체를 유지
- 점령 뒤 새 소유 진영은 빈 대기열에서 정상 비용을 지불하고 새 생산을 즉시 시작 가능
- Q-015는 사람 확인 전까지 `[미정]`, 완료 병력의 최종 처리는 Q-018 `[미정]`

## RTS-CORE-03D 이전 기술 시나리오

- 적 이동형 위협과 고착형 위협은 도로를 삭제하거나 통행 불가로 만들지 않고 경로 위험 점수에만 반영
- 북쪽 빠른 길과 남쪽 안전한 길의 이동 시간·위험 점수를 함께 비교해 현재 시험 Profile에서 더 나은 경로 선택
- 대체 경로가 없으면 적 병력 또는 전초기지의 위협이 있어도 해당 경로로 강행 파견
- Road가 없으면 Offroad fallback, Threat가 있어도 Strategic 연결은 별도로 유지
- 아군 Territory 연결 자체가 없을 때만 기술 `CutOff`, 사용자 화면에서는 `완전 고립`으로 표시하고 새 Dispatch 차단
- 완전 고립에서도 출발 대기와 이미 이동 중인 Reinforcement를 삭제·Teleport하지 않음
- 위협은 Dispatch 시점 Route Planning 입력이며 자동 피해나 이동 중 재탐색을 만들지 않음

## RTS-CORE-03C 이전 기술 시나리오

- 단일 시험 경제 자원으로 생산 요청 비용을 지불하고 생산 시설별 FIFO Queue에 등록
- 명시적 시간 진행과 초과 시간 소비로 Queue 선두부터 생산 완료
- 생산 완료 병력은 전선이 아니라 실제 생산 시설의 Position/Sector에서 `Ready At Source`로 전환
- `Ready At Source`를 기존 03B Dispatch에 연결해 Road 우선·Offroad fallback의 실제 `EnRoute → Arrived` 이동 수행
- Cut Off 중에도 생산은 계속되지만 신규 Dispatch는 차단되며 Ready 객체와 ID는 보존
- 연결 복구 뒤 같은 Ready 객체를 파견하고, 이동 중 파괴는 기존 `DestroyedEnRoute` 계약을 재사용
- 생산 중 시설 소유권이 바뀌면 Queue를 이전·삭제하지 않고 `OwnershipConflict`로 안전하게 정지

## RTS-CORE-03B 이전 기술 시나리오

- TerrainMovementProfile과 Unity 비의존 Resolver 계약으로 Road 밖 이동 배율 판독
- 자유 WorldPoint endpoint와 여러 TraversedSectorIds를 가진 RoadSegment, 고정 Road Slot 없음
- RoadPlacementService는 통과 Sector가 모두 건설 진영 소유일 때만 RoadNetwork를 변경
- Road 위에서는 Road Profile을 사용하고 밖에서는 underlying Terrain Profile을 사용하며 둘을 중첩하지 않음
- Supplied Front는 Rear에서 Reinforcement를 Dispatch하고 실제 `EnRoute → Arrived` Transit을 거침
- 연결 Road가 있으면 Road Route 우선, 없으면 Strategic 연결이 살아 있는 동안 Offroad fallback
- `DestroyedEnRoute`는 Physical Interdiction이며 Strategic Supply Snapshot을 변경하지 않음
- Cut Off는 신규 Dispatch를 막지만 이미 EnRoute인 Reinforcement를 삭제·Teleport하지 않음

## Gameplay 검증 완료 — RTS-PLAYTEST-03A-R1

- Open Ground, Forest, Rocky Ground, Mud와 Road Overlay에서 현재 위치 기반 이동 배율 적용
- Road는 1.40x 이동 보너스를 주지만 자체 Cover/Concealment가 없는 `[현재 방향]`을 표현
- 자유 Road Segment: 도로 건설 → 아군 시작점 → 아군 끝점 → Validation → 확정; 고정 Slot 없음
- Red Reinforcement는 Rear에서 실제 Squad로 Spawn해 활성 North/South Road waypoint를 따라 Front로 이동
- Strategic Connection이 남아 있어도 이동 중 증원을 공격하면 `INTERDICTED`; Corridor 두 곳을 점령해야 `CUT OFF`
- Spawned / Reached Front / Destroyed En Route 통계를 분리하고, 출발한 증원은 Cut Off 때 삭제·Teleport하지 않음
- Blue가 점령한 Corridor로 Road를 연장해 실제 분대 재배치 속도를 시험 가능

## `[대체됨 — R1]` RTS-PLAYTEST-03A 시나리오

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
- `RTS_CORE_03E_ProductionCaptureQueue.html`은 완공 생산시설 점령, 기존 Queue 취소·환불 없음과 Ready/EnRoute 보존을 확인하는 현재 기술 Preview다.
- `RTS_CORE_03D_RouteThreatInterdiction.html`은 실제 C# Route Threat 평가, 안전 경로 선호, 위험 경로 강행과 완전 고립을 구분하는 이전 기술 Preview다.
- `RTS_CORE_03C_ProductionReinforcement.html`은 Production→Ready At Source→기존 Dispatch 계약을 단계별로 확인하는 이전 기술 Preview다.
- `RTS_CORE_03B_TerrainRoadReinforcement.html`은 Terrain/Road/Reinforcement 계약을 단계별로 확인하는 이전 기술 Preview다.
- `RTS_PLAYTEST_03A_SupplyStrategy.html`은 단순 Supply Cut을 시험했던 이전 비정본 Gameplay Layer다.
- `RTS_PLAYTEST_03A_R1_TerrainRoadInterdiction.html`은 Terrain, Road Infrastructure, 실제 Reinforcement 이동·요격과 Strategic Cut Off를 함께 검증해 사람 PASS를 받은 비정본 Gameplay Layer다.
- Browser Preview와 Microgame은 사람의 조작·가독성·플레이 감각 확인용 표현 계층이다.
- 보급 연결은 현재 소유권·인접 관계·보급원에서 파생하며 Preview JavaScript를 기술 정본으로 사용하지 않는다.
- Microgame의 `Cut Off = 적 전방 Reinforcement 중단`은 사람 Playtest 전용 `[시험 규칙]`이며 C# Core나 최종 보급 규칙이 아니다.
- R1/03B Fixture의 Terrain·Road 배율과 R1의 Forest/Rocky 전투 보조, 무료·즉시 Road 건설 및 Reinforcement 수치는 모두 `[시험값/시험 규칙]`이며 최종 Balance가 아니다.
- `[확정] HTML 사용자 인터페이스 언어 규칙`: 모든 Browser Preview와 Microgame의 사용자 화면·버튼·상태·조작 안내는 한국어를 기본으로 한다.
- 코드 식별자와 Result Bundle 번호에는 영어를 유지할 수 있지만, 사용자 화면을 영어만으로 제공하지 않는다. 병기할 때는 한국어를 먼저 쓴다.
- 기술 `SectorSupplyStatus.CutOff`는 사용자 UI에서 `완전 고립`으로 표시한다. 경로 주변 적 전투 요소는 `보급로 위협` 또는 `강한 보급로 위협`이며 이동 불가와 동의어가 아니다.
- 사각형 Sector와 원형 Footprint는 Preview/검증용 시험 표현이며 최종 맵 Geometry나 Collider 확정이 아니다.
- Preview PASS는 Unity Compile, Scene, 입력, 물리, 렌더링 또는 Play Mode PASS를 뜻하지 않는다.
- 새 규칙을 Preview에 반영할 때는 같은 시작 상태, 조작과 기대 결과를 `ManagedPcChecks`에도 유지한다.
