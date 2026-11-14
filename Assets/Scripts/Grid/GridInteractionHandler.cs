using System.Collections.Generic;
using UnityEngine;
using ChessMonsters.Unit;

namespace ChessMonsters.Grid
{
    /// <summary>
    /// 마우스 입력 및 체스판 상호작용 (선택, 이동/사거리 하이라이트, 유닛 소환 및 이동, 타일 파괴/복구 테스트)
    /// New Input System과 Legacy Input 모두 완벽 호환
    /// </summary>
    public class GridInteractionHandler : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [SerializeField] private LayerMask _tileLayer = ~0;
        [SerializeField] private bool _showDebugGUI = true;

        [Header("Unit Spawning (2D Sprite in 3D World)")]
        [Tooltip("소환할 2D 빌보드 유닛 프리팹 (미지정 시 Assets/Prefabs/Unit.prefab 자동 로드)")]
        [SerializeField] private GameObject _unitPrefab;

        [Header("Movable Boundary Indicator")]
        [Tooltip("이동 가능한 타일에 표시되는 네온 경계선 스프라이트 (미지정 시 Assets/Textures/UI/Sprite_Move_Border.png 자동 로드)")]
        [SerializeField] private Sprite _moveBorderSprite;

        [Header("Test Range Settings")]
        [SerializeField] private int _testMoveRange = 3;
        [SerializeField] private int _testAttackRange = 2;
        [SerializeField] private bool _testFlyingUnit = false;

        private Camera _mainCamera;
        private Tile _hoveredTile;
        private Tile _selectedTile;
        private Unit.Unit _selectedUnit;
        private List<Tile> _activeHighlightTiles = new List<Tile>();
        private List<Tile> _movableTiles = new List<Tile>();
        private List<GameObject> _activeBorderIndicators = new List<GameObject>();
        private Transform _borderIndicatorContainer;

        private void Start()
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                _mainCamera = FindFirstObjectByType<Camera>();
            }

            if (_unitPrefab == null)
            {
#if UNITY_EDITOR
                _unitPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Unit.prefab");
#endif
            }

            if (_moveBorderSprite == null)
            {
#if UNITY_EDITOR
                _moveBorderSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/Sprite_Move_Border.png");
#endif
            }
        }

        private void Update()
        {
            HandleMouseRaycast();
            HandleInputs();
        }

        private void HandleMouseRaycast()
        {
            if (_mainCamera == null) return;

            Vector2 mousePos = GetMousePosition();
            Ray ray = _mainCamera.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, _tileLayer))
            {
                Tile tile = hit.collider.GetComponentInParent<Tile>();
                if (tile != null && !tile.IsDestroyed)
                {
                    if (_hoveredTile != tile)
                    {
                        ClearHovered();
                        _hoveredTile = tile;
                        _hoveredTile.HandleMouseEnter();

                        if (_hoveredTile != _selectedTile && !_activeHighlightTiles.Contains(_hoveredTile))
                        {
                            _hoveredTile.SetHighlight(TileHighlightState.Hovered);
                        }
                    }
                    return;
                }
            }

            ClearHovered();
        }

        private void ClearHovered()
        {
            if (_hoveredTile != null)
            {
                _hoveredTile.HandleMouseExit();
                if (_hoveredTile != _selectedTile && !_activeHighlightTiles.Contains(_hoveredTile))
                {
                    _hoveredTile.SetHighlight(TileHighlightState.None);
                }
                _hoveredTile = null;
            }
        }

        private void HandleInputs()
        {
            // 좌클릭: 타일 선택 및 이동/사거리 범위 표시
            if (WasLeftMouseClicked())
            {
                if (_hoveredTile != null)
                {
                    SelectTile(_hoveredTile);
                }
                else
                {
                    ClearSelection();
                }
            }

            // 우클릭: 타일 파괴 / 복구 토글 테스트
            if (WasRightMouseClicked() && _hoveredTile != null)
            {
                _hoveredTile.DestroyTile(true, GridManager.Instance.FallFloorY);
                ClearHovered();
            }

            // W 키 또는 휠클릭: 물(Water) 칸 토글
            if (WasKeyJustPressed(KeyCode.W))
            {
                if (_hoveredTile != null)
                {
                    TileType nextType = _hoveredTile.Type == TileType.Water ? TileType.Normal : TileType.Water;
                    _hoveredTile.SetTileType(nextType);
                    Debug.Log($"[GridTester] 타일 {_hoveredTile.Coord} 속성 변경: {nextType}");
                }
            }

            // B 키: 나무 다리(Bridge) 칸 토글 (피드백: 두께 0.3의 건널 수 있는 목재 다리)
            if (WasKeyJustPressed(KeyCode.B))
            {
                if (_hoveredTile != null)
                {
                    TileType nextType = _hoveredTile.Type == TileType.Bridge ? TileType.Normal : TileType.Bridge;
                    _hoveredTile.SetTileType(nextType);
                    Debug.Log($"[GridTester] 타일 {_hoveredTile.Coord} 속성 변경: {nextType}");
                }
            }

            // S 키: 마우스가 위치한 타일에 2D 빌보드 유닛 소환
            if (WasKeyJustPressed(KeyCode.S))
            {
                if (_hoveredTile != null)
                {
                    SpawnUnitAt(_hoveredTile);
                }
            }

            // O 키: 장애물(Obstacle) 칸 토글
            if (WasKeyJustPressed(KeyCode.O))
            {
                if (_hoveredTile != null)
                {
                    TileType nextType = _hoveredTile.Type == TileType.Obstacle ? TileType.Normal : TileType.Obstacle;
                    _hoveredTile.SetTileType(nextType);
                    Debug.Log($"[GridTester] 타일 {_hoveredTile.Coord} 속성 변경: {nextType}");
                }
            }

            // H 키: 반칸 높이 디딤돌 블록(Half Block) 토글 (단차 0.5h 등판 디딤돌)
            if (WasKeyJustPressed(KeyCode.H))
            {
                if (_hoveredTile != null)
                {
                    bool nextHalf = !_hoveredTile.IsHalfBlock;
                    _hoveredTile.SetHalfBlock(nextHalf, 1.0f);
                    Debug.Log($"[GridTester] 타일 {_hoveredTile.Coord} 반칸 블록(HalfBlock): {(nextHalf ? "ON (0.5h 단차 디딤돌)" : "OFF")}");
                    if (_selectedUnit != null)
                    {
                        ShowRangeHighlightsForUnit(_selectedUnit);
                    }
                }
            }

            // F 키: 비행 유닛 모드 토글 (기획서: 비행 유닛은 물과 높이를 무시)
            if (WasKeyJustPressed(KeyCode.F))
            {
                _testFlyingUnit = !_testFlyingUnit;
                Debug.Log($"[GridTester] 비행 유닛 모드: {(_testFlyingUnit ? "ON (물 무시 가능)" : "OFF (물 통과 불가)")}");
                if (_selectedTile != null)
                {
                    ShowRangeHighlights(_selectedTile);
                }
            }

            // 숫자 1~5 키: 지형 프리셋 즉시 전환 (5번: 9x9 사진 전장 대격돌 섬)
            if (WasKeyJustPressed(KeyCode.Alpha1)) { GridManager.Instance.ApplyFieldPreset(FieldPresetType.DefaultPlain); ClearSelection(); }
            if (WasKeyJustPressed(KeyCode.Alpha2)) { GridManager.Instance.ApplyFieldPreset(FieldPresetType.RiverField); ClearSelection(); }
            if (WasKeyJustPressed(KeyCode.Alpha3)) { GridManager.Instance.ApplyFieldPreset(FieldPresetType.HighlandField); ClearSelection(); }
            if (WasKeyJustPressed(KeyCode.Alpha4)) { GridManager.Instance.ApplyFieldPreset(FieldPresetType.MarshField); ClearSelection(); }
            if (WasKeyJustPressed(KeyCode.Alpha5)) { GridManager.Instance.ApplyFieldPreset(FieldPresetType.BattleIsland); ClearSelection(); }

            // Space 키: 모든 타일 리셋 / 복구
            if (WasKeyJustPressed(KeyCode.Space))
            {
                for (int z = 0; z < GridManager.BoardHeight; z++)
                {
                    for (int x = 0; x < GridManager.BoardWidth; x++)
                    {
                        GridManager.Instance.CreateTile(x, z, TileType.Normal, true);
                    }
                }
                ClearSelection();
                Debug.Log("[GridTester] 모든 타일 복구 완료.");
            }
        }

        private void SelectTile(Tile tile)
        {
            // 1. 이미 유닛이 선택되어 있고, 해당 유닛의 이동 가능 범위(+ 십자 1칸) 내 빈 타일을 클릭한 경우 -> 호핑 이동!
            if (_selectedUnit != null && _movableTiles.Contains(tile) && tile.OccupyingUnit == null && !_selectedUnit.IsMoving)
            {
                Unit.Unit movingUnit = _selectedUnit;
                ClearSelectionVisualsOnly();
                _selectedTile = null;

                movingUnit.MoveToTile(tile, () =>
                {
                    // 이동 완료 후 새 위치에서 유닛 재선택 (+ 모양 다음 1칸 이동 준비)
                    if (movingUnit != null && !movingUnit.IsDead)
                    {
                        SelectTile(tile);
                    }
                });
                return;
            }

            ClearSelection();
            _selectedTile = tile;
            _selectedTile.HandleMouseClick();
            _selectedTile.SetHighlight(TileHighlightState.Selected);

            // 2. 타일에 유닛이 올라와 있는 경우 -> 유닛 선택 및 해당 유닛의 이동(+ 모양 1칸)/공격 사거리 표시
            if (tile.OccupyingUnit != null)
            {
                _selectedUnit = tile.OccupyingUnit.GetComponent<Unit.Unit>();
                if (_selectedUnit != null)
                {
                    _selectedUnit.SetSelected(true);
                    ShowRangeHighlightsForUnit(_selectedUnit);
                    return;
                }
            }

            // 3. 빈 타일인 경우 -> 기본 테스트 사거리 표시
            ShowRangeHighlights(tile);
        }

        /// <summary>
        /// 특정 타일에 2D 빌보드 유닛 소환
        /// </summary>
        public void SpawnUnitAt(Tile tile)
        {
            if (tile == null || tile.IsDestroyed || tile.Type == TileType.Obstacle)
            {
                Debug.LogWarning("[GridTester] 파괴되었거나 장애물 타일에는 유닛을 소환할 수 없습니다.");
                return;
            }

            if (_unitPrefab == null)
            {
#if UNITY_EDITOR
                _unitPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Unit.prefab");
#endif
            }

            if (_unitPrefab == null)
            {
                Debug.LogError("[GridTester] 유닛 프리팹(Assets/Prefabs/Unit.prefab)을 찾을 수 없습니다!");
                return;
            }

            // 이미 유닛이 있으면 기존 유닛 제거 후 교체
            if (tile.OccupyingUnit != null)
            {
                Destroy(tile.OccupyingUnit);
                tile.SetOccupyingUnit(null);
            }

            GameObject unitObj = Instantiate(_unitPrefab);
            Unit.Unit unitComp = unitObj.GetComponent<Unit.Unit>();
            if (unitComp != null)
            {
                unitComp.PlaceOnTile(tile);
                SelectTile(tile);
                Debug.Log($"[GridTester] 타일 {tile.Coord}에 2D 빌보드 유닛 '{unitComp.UnitName}' 소환 완료!");
            }
        }

        private void ShowRangeHighlightsForUnit(Unit.Unit unit)
        {
            if (unit == null || unit.CurrentTile == null) return;

            ClearSelectionVisualsOnly();

            // 1. 유닛 이동: 앞/뒤/좌/우 (+ 모양) 1칸 이동 가능 타일 계산
            // 단차 제약: 1칸(1.0f) 이상 높은 블록은 직접 등판 불가 (0.55f 이하), 반칸 블록(HalfBlock) 디딤돌을 통해서만 등판 가능 (비행 유닛 제외)
            List<Tile> stepNeighbors = GridManager.Instance.GetMovableStepNeighbors(unit.CurrentTile.Coord, unit.IsFlying, 0.55f);
            foreach (var t in stepNeighbors)
            {
                t.SetHighlight(TileHighlightState.Movable);
                _activeHighlightTiles.Add(t);
                _movableTiles.Add(t);
                ShowBorderIndicatorOnTile(t); // 이동 가능 지점 경계 표시!
            }

            // 2. 공격 사거리 계산 및 하이라이트 (빨간색 - 이동 범위 밖 사거리)
            List<Tile> attackTiles = GridManager.Instance.GetAttackRangeTiles(unit.CurrentTile.Coord, unit.AttackRange);
            foreach (var t in attackTiles)
            {
                if (t != unit.CurrentTile && !t.IsDestroyed && !_activeHighlightTiles.Contains(t))
                {
                    t.SetHighlight(TileHighlightState.Attackable);
                    _activeHighlightTiles.Add(t);
                }
            }
        }

        private void ShowRangeHighlights(Tile centerTile)
        {
            ShowRangeHighlights(centerTile, _testMoveRange, _testAttackRange, _testFlyingUnit);
        }

        private void ShowRangeHighlights(Tile centerTile, int moveRange, int attackRange, bool isFlying)
        {
            ClearSelectionVisualsOnly();

            // 1. 기동력(이동 범위) 계산 및 하이라이트 (하늘색)
            List<Tile> moveTiles = GridManager.Instance.GetReachableTiles(centerTile.Coord, moveRange, isFlying);
            foreach (var t in moveTiles)
            {
                if (t != centerTile && !t.IsDestroyed && t.OccupyingUnit == null)
                {
                    t.SetHighlight(TileHighlightState.Movable);
                    _activeHighlightTiles.Add(t);
                    _movableTiles.Add(t);
                    ShowBorderIndicatorOnTile(t);
                }
            }

            // 2. 공격 사거리 계산 및 하이라이트 (빨간색 - 이동 범위 밖 사거리)
            List<Tile> attackTiles = GridManager.Instance.GetAttackRangeTiles(centerTile.Coord, attackRange);
            foreach (var t in attackTiles)
            {
                if (t != centerTile && !t.IsDestroyed && !_activeHighlightTiles.Contains(t))
                {
                    t.SetHighlight(TileHighlightState.Attackable);
                    _activeHighlightTiles.Add(t);
                }
            }
        }

        private void ClearSelection()
        {
            if (_selectedUnit != null)
            {
                _selectedUnit.SetSelected(false);
                _selectedUnit = null;
            }

            ClearSelectionVisualsOnly();

            if (_selectedTile != null)
            {
                _selectedTile.SetHighlight(TileHighlightState.None);
                _selectedTile = null;
            }
        }

        private void ClearSelectionVisualsOnly()
        {
            foreach (var t in _activeHighlightTiles)
            {
                if (t != null && t != _selectedTile)
                {
                    t.SetHighlight(TileHighlightState.None);
                }
            }
            _activeHighlightTiles.Clear();
            _movableTiles.Clear();
            ClearBorderIndicators();
        }

        private void ClearBorderIndicators()
        {
            foreach (var ind in _activeBorderIndicators)
            {
                if (ind != null) ind.SetActive(false);
            }
        }

        private void ShowBorderIndicatorOnTile(Tile tile)
        {
            if (tile == null) return;

            if (_moveBorderSprite == null)
            {
#if UNITY_EDITOR
                _moveBorderSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/Sprite_Move_Border.png");
#endif
            }

            if (_moveBorderSprite == null) return;

            if (_borderIndicatorContainer == null)
            {
                GameObject containerObj = new GameObject("_MoveBorderIndicators");
                containerObj.transform.SetParent(transform, false);
                _borderIndicatorContainer = containerObj.transform;
            }

            GameObject indicator = null;
            for (int i = 0; i < _activeBorderIndicators.Count; i++)
            {
                if (_activeBorderIndicators[i] != null && !_activeBorderIndicators[i].activeSelf)
                {
                    indicator = _activeBorderIndicators[i];
                    break;
                }
            }

            if (indicator == null)
            {
                indicator = new GameObject("MoveBorderIndicator");
                indicator.transform.SetParent(_borderIndicatorContainer, false);
                SpriteRenderer sr = indicator.AddComponent<SpriteRenderer>();
                sr.sprite = _moveBorderSprite;
                sr.color = new Color(0.2f, 0.95f, 1.0f, 0.95f);
                sr.sortingOrder = 5;
                _activeBorderIndicators.Add(indicator);
            }

            indicator.transform.position = tile.TopSurfacePosition + Vector3.up * 0.015f;
            indicator.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            indicator.transform.localScale = Vector3.one * 0.98f;
            indicator.SetActive(true);
        }

        // ================= Input Helpers (New & Legacy 지원) =================
        private Vector2 GetMousePosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null)
                return UnityEngine.InputSystem.Mouse.current.position.ReadValue();
#endif
            return Input.mousePosition;
        }

        private bool WasLeftMouseClicked()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null)
                return UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;
#endif
            return Input.GetMouseButtonDown(0);
        }

        private bool WasRightMouseClicked()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null)
                return UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame;
#endif
            return Input.GetMouseButtonDown(1);
        }

        private bool WasKeyJustPressed(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                switch (key)
                {
                    case KeyCode.W:
                        return UnityEngine.InputSystem.Keyboard.current.wKey.wasPressedThisFrame;
                    case KeyCode.B:
                        return UnityEngine.InputSystem.Keyboard.current.bKey.wasPressedThisFrame;
                    case KeyCode.S:
                        return UnityEngine.InputSystem.Keyboard.current.sKey.wasPressedThisFrame;
                    case KeyCode.O:
                        return UnityEngine.InputSystem.Keyboard.current.oKey.wasPressedThisFrame;
                    case KeyCode.H:
                        return UnityEngine.InputSystem.Keyboard.current.hKey.wasPressedThisFrame;
                    case KeyCode.F:
                        return UnityEngine.InputSystem.Keyboard.current.fKey.wasPressedThisFrame;
                    case KeyCode.Space:
                        return UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame;
                    case KeyCode.Alpha1:
                        return UnityEngine.InputSystem.Keyboard.current.digit1Key.wasPressedThisFrame;
                    case KeyCode.Alpha2:
                        return UnityEngine.InputSystem.Keyboard.current.digit2Key.wasPressedThisFrame;
                    case KeyCode.Alpha3:
                        return UnityEngine.InputSystem.Keyboard.current.digit3Key.wasPressedThisFrame;
                    case KeyCode.Alpha4:
                        return UnityEngine.InputSystem.Keyboard.current.digit4Key.wasPressedThisFrame;
                    case KeyCode.Alpha5:
                        return UnityEngine.InputSystem.Keyboard.current.digit5Key.wasPressedThisFrame;
                }
            }
#endif
            return Input.GetKeyDown(key);
        }

        // ================= Debug GUI Overlay =================
        private void OnGUI()
        {
            if (!_showDebugGUI) return;

            GUI.Box(new Rect(15, 15, 430, 370), "♟ 체스 몬스터즈 9x9 전장 시스템 테스터");

            GUILayout.BeginArea(new Rect(25, 42, 410, 335));
            GUILayout.Label("• <b>좌클릭</b>: 유닛 선택 및 <b>앞/뒤/좌/우(+) 1칸 이동</b> (네온 경계선 표시)");
            GUILayout.Label("• <b>신규 리소스</b>: <b>B 키</b> 나무 다리(0.3h 통행 가능) | <b>W 키</b> 물(0.6h 수로, 3프레임 애니)");
            GUILayout.Label("• <b>H 키</b>: 마우스 위치 <b>반칸 블록(Half Block) 디딤돌</b> 토글");
            GUILayout.Label("• <b>단차 이동 규칙</b>: 1칸 높이 블록은 직접 등판 불가\n   -> <b>반칸 블록(0.5h)을 디딤돌 삼아 등판 가능</b>");
            GUILayout.Label("• <b>O 키</b>: 장애물 | <b>S 키</b>: 유닛 소환 | <b>F 키</b>: 비행 모드 (물/단차 무시)");
            GUILayout.Label("• <b>숫자 1~5</b>: 지형 프리셋 (<b>5번: 참고 사진 9x9 대격돌 섬 전장</b>)");
            GUILayout.Label("• <b>Space 키</b>: 모든 타일 복구 | <b>우클릭</b>: 타일 파괴");

            if (_selectedUnit != null)
            {
                GUILayout.Space(2);
                GUILayout.Label($"<b>선택된 유닛:</b> <color=cyan>{_selectedUnit.UnitName}</color> (HP: {_selectedUnit.CurrentHp}/{_selectedUnit.MaxHp}, 이동: +1칸, 공격사거리: {_selectedUnit.AttackRange})");
            }
            else if (_hoveredTile != null)
            {
                GUILayout.Space(2);
                string unitInfo = _hoveredTile.OccupyingUnit != null ? " | 유닛 탑승 중" : "";
                string halfInfo = _hoveredTile.IsHalfBlock ? " (반칸 디딤돌 0.5h)" : "";
                string bridgeInfo = _hoveredTile.Type == TileType.Bridge ? " (나무 다리 0.3h)" : "";
                string waterInfo = _hoveredTile.Type == TileType.Water ? " (수로 0.6h)" : "";
                GUILayout.Label($"<b>호버 타일:</b> {_hoveredTile.Coord} | 타입: {_hoveredTile.Type}{halfInfo}{bridgeInfo}{waterInfo} | Y: {_hoveredTile.Height:F1}{unitInfo}");
            }

            GUILayout.Space(4);
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("유닛 소환 (S)", GUILayout.Height(24)))
                {
                    if (_hoveredTile != null)
                    {
                        SpawnUnitAt(_hoveredTile);
                    }
                    else
                    {
                        // 중앙 타일에 스폰 (레드 진영 중앙 다리 앞)
                        Tile centerTile = GridManager.Instance != null ? GridManager.Instance.GetTile(4, 3) : null;
                        if (centerTile != null) SpawnUnitAt(centerTile);
                    }
                }

                if (GUILayout.Button("선택 해제", GUILayout.Height(24)))
                {
                    ClearSelection();
                }
            }

            GUILayout.EndArea();
        }
    }
}
