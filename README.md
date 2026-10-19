# 26_SA_GameJam_ChessMoster
26_SA_GameJam_ChessMoster
Unity version 6.3 LTS

파일들 중
Library
Temp
Obj
Build
Builds
Logs
UserSettings
*.log

같은 건 제외하고 commit해주세요.

아래 .gitignore, collabignore, gitattributes 들은 깃허브 설정들입니다. 그냥 다운받아서 파일 안에 두시면 됩니다.
아 그리고 메인에 브랜치를 적용하는 pull&request 해주실 때에는 디코든 카톡이든, 깃헙 이슈에서든 김민지를 멘션해주세요, (충돌 여부 확인과 버전을 관리해야함)

---

## ♟ 7x7 체스판 그리드 시스템 (Chess Monsters Grid System)

기획서의 [필드] 명세를 완벽히 반영한 7x7 오브젝트 기반 3D 체스판 시스템입니다.

### 🎨 비주얼 및 텍스처 시스템 (URP)
- **1x1x1 완벽한 정육면체 큐브**: 기존 0.3 슬랩 형태에서 완전한 1.0x1.0x1.0 정육면체 큐브 블록으로 업그레이드 (인스펙터에서 큐브/슬랩 모드 원클릭 토글 가능)
- **2-Submesh 메시 분리 구조**: 
  - **윗면(Top Face)**: 푸른 잔디(`Tile_Grass_04`) 및 꽃 핀 잔디(`Tile_GrassFlowers_03`)
  - **나머지 5면(측면 4면 + 밑면)**: 마인크래프트/택틱스 RPG 스타일의 **일반 흙(`Tile_Dirt_01`)**으로 렌더링
- **랜덤 잔디 배치(Random Grass Mode)**:
  - 체스판 체크무늬 교차 배치 외에도 자연스러운 무작위 잔디 지형 생성 지원
  - 잔디/흙 비율(`_grassRatio`), 꽃잔디 비율(`_flowerGrassRatio`), 시드(`_randomSeed`), 90도 단위 무작위 텍스처 회전(`_randomizeRotation`) 지원
- **URP Lit 셰이더 적용**: 분홍색(Magenta) 셰이더 에러를 원천 차단하고 텍스처 원본 색감 유지 + 부드러운 하이라이트 오버레이

### 📁 주요 파일 및 구조
- **`Assets/Prefabs/ChessBoard.prefab`**: 씬 충돌 없이 어느 씬이든 바로 끌어다 놓아 사용할 수 있는 완성형 체스판 프리팹
- **`Assets/Scripts/Grid/GridManager.cs`**: 7x7 그리드 총괄 매니저, 경로 탐색(BFS), 지형 프리셋, 랜덤 잔디 배치 관리
- **`Assets/Scripts/Grid/Tile.cs`**: 개별 타일 제어, 2-Submesh 머티리얼 바인딩, 동적 파괴/복구 애니메이션, 낙하 피해 공식 계산, 하이라이트
- **`Assets/Scripts/Grid/TileMeshUtility.cs`**: 윗면(잔디)과 5면(흙)을 분리한 2-Submesh 큐브 메시 절차적 생성 및 캐싱
- **`Assets/Scripts/Grid/TileType.cs`**: 타일 종류(Normal, Water, Obstacle, Void) 및 지형 프리셋 정의
- **`Assets/Scripts/Grid/GridInteractionHandler.cs`**: 마우스 인터랙션, 사거리 시각화, 테스터 OnGUI
- **`Assets/Scripts/Grid/Editor/`**:
  - `GridManagerEditor.cs`: 인스펙터 버튼(랜덤 재배치, 체스판 패턴 복원, 큐브/슬랩 전환, 지형 프리셋)
  - `SetupTileMaterials.cs`: 원클릭 URP 머티리얼 자동 생성 및 씬 일괄 셋업

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
