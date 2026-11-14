namespace ChessMonsters.Grid
{
    /// <summary>
    /// 기획서에 명시된 특수 필드칸 및 타일 유형
    /// </summary>
    public enum TileType
    {
        Normal,     // 일반 타일 (체스판 기본 칸)
        HalfBlock,  // 반칸 높이 블록 (일반 블록의 절반 높이 디딤돌/계단 칸)
        Water,      // 물 칸 : 수면 높이 0.6, 통상 유닛 이동 불가 (비행 유닛 통과 가능)
        Bridge,     // 나무 다리 칸 : 두께 0.3, 물 위를 건널 수 있는 통행 가능 목재 다리
        Obstacle,   // 장애물 칸 : 이동 불가
        Void        // 공허/낙하 구역 (타일이 파괴되었거나 없는 칸)
    }

    /// <summary>
    /// 타일 하이라이트 상태 (이동 범위, 공격 범위, 소환 범위 등)
    /// </summary>
    public enum TileHighlightState
    {
        None,
        Hovered,        // 마우스 커서 호버
        Selected,       // 선택된 타일
        Movable,        // 이동 가능한 범위 (파란색 계열)
        Attackable,     // 공격 가능한 사거리 (빨간색 계열)
        Spawnable       // 소환 가능한 위치 (노란색/시안색 계열)
    }

    /// <summary>
    /// 기획서 명세: "7×7의 공간을 가지고 있으며 정해진 지형이 존재한다."
    /// 턴제 전략 보드에서 사용 가능한 지형 프리셋 종류
    /// </summary>
    public enum FieldPresetType
    {
        DefaultPlain,   // 1. 기본 평원 (9x9 표준 체스판)
        RiverField,     // 2. 강/호수 지형 (중앙 강줄기 물 칸 + 여울목 다리)
        HighlandField,  // 3. 고지대 지형 (중앙 고지대 단차 + 반칸 디딤돌)
        MarshField,     // 4. 습지 지형 (분산된 물 웅덩이 지형)
        BattleIsland    // 5. 9x9 대격돌 섬 (참고 사진 지형: 대각선/중앙 강줄기 + 3개 나무 다리 + 고지대 언덕 + 외곽 석벽 + 진영 깃발)
    }
}
