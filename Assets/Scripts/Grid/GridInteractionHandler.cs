using System.Collections.Generic;
using UnityEngine;

namespace ChessMonsters.Grid
{
    /// <summary>
    /// 마우스 입력 및 체스판 상호작용 (선택, 이동/사거리 하이라이트, 타일 파괴/복구 테스트)
    /// New Input System과 Legacy Input 모두 완벽 호환
    /// </summary>
    public class GridInteractionHandler : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [SerializeField] private LayerMask _tileLayer = ~0;
        [SerializeField] private bool _showDebugGUI = true;

        [Header("Test Range Settings")]
        [SerializeField] private int _testMoveRange = 3;
        [SerializeField] private int _testAttackRange = 2;
        [SerializeField] private bool _testFlyingUnit = false;

        private Camera _mainCamera;
        private Tile _hoveredTile;
        private Tile _selectedTile;
        private List<Tile> _activeHighlightTiles = new List<Tile>();

        private void Start()
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                _mainCamera = FindFirstObjectByType<Camera>();
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

            // 숫자 1~4 키: 기획서 "정해진 지형" 프리셋 즉시 전환
            if (WasKeyJustPressed(KeyCode.Alpha1)) { GridManager.Instance.ApplyFieldPreset(FieldPresetType.DefaultPlain); ClearSelection(); }
            if (WasKeyJustPressed(KeyCode.Alpha2)) { GridManager.Instance.ApplyFieldPreset(FieldPresetType.RiverField); ClearSelection(); }
            if (WasKeyJustPressed(KeyCode.Alpha3)) { GridManager.Instance.ApplyFieldPreset(FieldPresetType.HighlandField); ClearSelection(); }
            if (WasKeyJustPressed(KeyCode.Alpha4)) { GridManager.Instance.ApplyFieldPreset(FieldPresetType.MarshField); ClearSelection(); }

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
            ClearSelection();
            _selectedTile = tile;
            _selectedTile.HandleMouseClick();
            _selectedTile.SetHighlight(TileHighlightState.Selected);

            ShowRangeHighlights(tile);
        }

        private void ShowRangeHighlights(Tile centerTile)
        {
            // 기존 하이라이트 목록 해제
            foreach (var t in _activeHighlightTiles)
            {
                if (t != null && t != _selectedTile)
                {
                    t.SetHighlight(TileHighlightState.None);
                }
            }
            _activeHighlightTiles.Clear();

            // 1. 기동력(이동 범위) 계산 및 하이라이트 (하늘색)
            List<Tile> moveTiles = GridManager.Instance.GetReachableTiles(centerTile.Coord, _testMoveRange, _testFlyingUnit);
            foreach (var t in moveTiles)
            {
                if (t != centerTile && !t.IsDestroyed)
                {
                    t.SetHighlight(TileHighlightState.Movable);
                    _activeHighlightTiles.Add(t);
                }
            }

            // 2. 공격 사거리 계산 및 하이라이트 (빨간색 - 이동 범위 밖 사거리)
            List<Tile> attackTiles = GridManager.Instance.GetAttackRangeTiles(centerTile.Coord, _testAttackRange);
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
            if (_selectedTile != null)
            {
                _selectedTile.SetHighlight(TileHighlightState.None);
                _selectedTile = null;
            }

            foreach (var t in _activeHighlightTiles)
            {
                if (t != null)
                {
                    t.SetHighlight(TileHighlightState.None);
                }
            }
            _activeHighlightTiles.Clear();
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
                    case KeyCode.O:
                        return UnityEngine.InputSystem.Keyboard.current.oKey.wasPressedThisFrame;
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
                }
            }
#endif
            return Input.GetKeyDown(key);
        }

        // ================= Debug GUI Overlay =================
        private void OnGUI()
        {
            if (!_showDebugGUI) return;

            GUI.Box(new Rect(15, 15, 360, 260), "♟ 체스 몬스터즈 그리드 시스템 테스터");

            GUILayout.BeginArea(new Rect(25, 45, 340, 220));
            GUILayout.Label("• <b>좌클릭</b>: 타일 선택 (이동/사거리 범위 표시)");
            GUILayout.Label("• <b>우클릭</b>: 타일 파괴(추락 연출)");
            GUILayout.Label("• <b>W 키</b>: '물' 칸 토글 | <b>O 키</b>: '장애물' 토글");
            GUILayout.Label($"• <b>F 키</b>: 비행 모드 (현재: {(_testFlyingUnit ? "<color=cyan>비행(물 통과)</color>" : "지상")})");
            GUILayout.Label("• <b>숫자 1~4</b>: 기획서 지형 프리셋 전환");
            GUILayout.Label("  (1:기본평원, 2:강/호수, 3:고지대단차, 4:습지)");
            GUILayout.Label("• <b>Space 키</b>: 모든 타일 복구");

            if (GridManager.Instance != null)
            {
                GUILayout.Label($"<b>현재 지형:</b> <color=yellow>{GridManager.Instance.CurrentPreset}</color>");
            }

            if (_hoveredTile != null)
            {
                GUILayout.Space(4);
                GUILayout.Label($"<b>호버 타일:</b> {_hoveredTile.Coord} | 타입: {_hoveredTile.Type} | Y: {_hoveredTile.Height:F1}");
            }

            GUILayout.EndArea();
        }
    }
}
