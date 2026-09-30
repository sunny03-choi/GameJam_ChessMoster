namespace ChessMonsters.Grid
{
    /// <summary>
    /// 기획서에 명시된 특수 필드칸 및 타일 유형
    /// </summary>
    public enum TileType
    {
        Normal,     // 일반 타일 (체스판 기본 칸)
        Water,      // 물 칸 : 기획서상 "통상적인 유닛은 해당 칸 위를 지날 수 없다"
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
}
