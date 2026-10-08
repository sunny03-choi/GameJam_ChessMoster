# 26_SA_GameJam_ChessMoster
Unity version 6.3 LTS

---

## ♟ 7x7 체스판 그리드 시스템 (Chess Monsters Grid System)

기획서의 [필드] 명세를 완벽히 반영한 7x7 오브젝트 기반 체스판 시스템입니다.

### 📁 주요 파일 및 구조
- **`Assets/Prefabs/ChessBoard.prefab`**: 씬 충돌 없이 어떤 씬이든 바로 끌어다 놓아 사용할 수 있는 완성형 체스판 프리팹
- **`Assets/Scripts/Grid/GridManager.cs`**: 7x7 그리드 총괄 매니저, 경로 탐색(BFS), 지형 프리셋 관리
- **`Assets/Scripts/Grid/Tile.cs`**: 개별 타일 제어, 동적 파괴/복구 애니메이션, 낙하 피해 공식 계산, 하이라이트
- **`Assets/Scripts/Grid/TileType.cs`**: 타일 종류(Normal, Water, Obstacle, Void) 및 지형 프리셋 정의
- **`Assets/Scripts/Grid/GridInteractionHandler.cs`**: 마우스 인터랙션, 사거리 시각화, 테스터 OnGUI
- **`Assets/Scripts/Grid/Editor/`**: 인스펙터 버튼 한 번으로 그리드 생성 및 지형 프리셋 변경 도구

### 🗺 지형 프리셋 (Field Presets)
- **1. 기본 평원 (Default Plain)**: 표준 7×7 평평한 체스판
- **2. 강/호수 지형 (River Field)**: 중앙 강줄기(물 칸) + 여울목 다리 형성 (비행 유닛만 강 통과)
- **3. 고지대 단차 지형 (Highland Field)**: 중앙 3×3 영역 1m 고지대 단차 + 필드 파괴 시 실제 낙하 피해 발생
- **4. 습지 지형 (Marsh Field)**: 곳곳에 분산된 물 웅덩이 늪지대 전술 지형

### 🎮 인게임 테스트 조작키 (Play 모드)
- **마우스 좌클릭**: 타일 선택 (이동 범위 3칸: 하늘색, 공격 사거리 2칸: 빨간색)
- **마우스 우클릭**: 타일 파괴 (블록 추락 연출)
- **W 키**: '물' 칸 토글 / **O 키**: '장애물' 칸 토글
- **F 키**: 비행 유닛 모드 (물 통과 가능 여부 테스트)
- **숫자 1~4 키**: 4가지 지형 프리셋 실시간 전환
- **Space 키**: 파괴된 모든 타일 복구
