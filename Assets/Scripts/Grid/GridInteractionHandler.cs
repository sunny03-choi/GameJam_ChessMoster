using System.Collections.Generic;
using UnityEngine;
using ChessMonsters.Unit;
using ChessMonsters.UI;

namespace ChessMonsters.Grid
{
    /// <summary>
    /// 마우스 입력 및 체스판 상호작용 컨트롤러
    /// - 쿠키런 킹덤 스타일 3종 선택지(1. 공격, 2. 이동, 3. 효과 발동) 연동
    /// - 이동 시: 유닛 머리 위 남은 기동력 표시 및 십자(+) 1칸 이동, 클릭 시 기동력 1 감소
    /// - 공격 시: 사거리 내 범위 표시, 사거리 내 상대 유닛 클릭 시 [전투 진입]
    /// - 유닛 소환: S 키 (아군 폰), E 키 (적군 몬스터 폰)
    /// - New Input System 및 Legacy Input 완벽 호환
    /// </summary>
    public class GridInteractionHandler : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [SerializeField] private LayerMask _tileLayer = ~0;
        [SerializeField] private bool _showDebugGUI = true;

        [Header("Unit Spawning (2D Sprite in 3D World)")]
        [Tooltip("아군 유닛 프리팹 (Assets/Prefabs/Unit.prefab)")]
        [SerializeField] private GameObject _unitPrefab;

        [Header("Boundary & Target Indicators")]
        [Tooltip("이동 가능 타일 네온 경계선 스프라이트")]
        [SerializeField] private Sprite _moveBorderSprite;

        [Tooltip("공격 사거리 내 적 조준선 스프라이트")]
        [SerializeField] private Sprite _targetReticleSprite;

        [Header("Test Settings")]
        [SerializeField] private bool _testFlyingUnit = false;

        private Camera _mainCamera;
        private Tile _hoveredTile;
        private Tile _selectedTile;
        private Unit.Unit _selectedUnit;
        private UnitActionMode _currentMode = UnitActionMode.None;

        private List<Tile> _activeHighlightTiles = new List<Tile>();
        private List<Tile> _movableTiles = new List<Tile>();
        private List<Tile> _attackableTiles = new List<Tile>();
        private List<Unit.Unit> _targetableEnemies = new List<Unit.Unit>();

        private List<GameObject> _activeBorderIndicators = new List<GameObject>();
        private List<GameObject> _activeReticleIndicators = new List<GameObject>();
        private Transform _indicatorContainer;

        private void Start()
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null) _mainCamera = FindFirstObjectByType<Camera>();

            // UI 매니저 자동 부착 확인
            EnsureActionMenuUI();

            LoadSpritesIfNull();
        }

        private void EnsureActionMenuUI()
        {
            if (UnitActionMenuUI.Instance == null)
            {
                UnitActionMenuUI ui = FindFirstObjectByType<UnitActionMenuUI>();
                if (ui == null)
                {
                    GameObject uiObj = new GameObject("UnitActionMenuUI");
                    ui = uiObj.AddComponent<UnitActionMenuUI>();
                }
            }

            if (UnitActionMenuUI.Instance != null)
            {
                UnitActionMenuUI.Instance.OnAttackSelected += HandleAttackModeSelected;
                UnitActionMenuUI.Instance.OnMoveSelected += HandleMoveModeSelected;
                UnitActionMenuUI.Instance.OnSkillSelected += HandleSkillModeSelected;
                UnitActionMenuUI.Instance.OnCancelSelected += HandleCancelSelected;
            }
        }

        private void OnDestroy()
        {
            if (UnitActionMenuUI.Instance != null)
            {
                UnitActionMenuUI.Instance.OnAttackSelected -= HandleAttackModeSelected;
                UnitActionMenuUI.Instance.OnMoveSelected -= HandleMoveModeSelected;
                UnitActionMenuUI.Instance.OnSkillSelected -= HandleSkillModeSelected;
                UnitActionMenuUI.Instance.OnCancelSelected -= HandleCancelSelected;
            }
        }

        private void LoadSpritesIfNull()
        {
#if UNITY_EDITOR
            if (_unitPrefab == null)
                _unitPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Unit.prefab");
            if (_moveBorderSprite == null)
                _moveBorderSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/Sprite_Move_Border.png");
            if (_targetReticleSprite == null)
                _targetReticleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/Sprite_Target_Reticle.png");
#endif
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
                // 유닛 콜라이더 또는 타일 콜라이더 감지
                Tile tile = hit.collider.GetComponentInParent<Tile>();
                if (tile == null)
                {
                    Unit.Unit unitOnCol = hit.collider.GetComponentInParent<Unit.Unit>();
                    if (unitOnCol != null) tile = unitOnCol.CurrentTile;
                }

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
            // 좌클릭 상호작용
            if (WasLeftMouseClicked())
            {
                HandleLeftClick();
            }

            // 우클릭: 타일 파괴 / 복구 토글 테스트
            if (WasRightMouseClicked() && _hoveredTile != null)
            {
                _hoveredTile.DestroyTile(true, GridManager.Instance.FallFloorY);
                ClearHovered();
            }

            // S 키: 아군 유닛 소환 (Player Pawn)
            if (WasKeyJustPressed(KeyCode.S))
            {
                if (_hoveredTile != null) SpawnUnitAt(_hoveredTile, UnitTeam.Player);
            }

            // E 키: 적군 유닛 소환 (Enemy Pawn)
            if (WasKeyJustPressed(KeyCode.E))
            {
                if (_hoveredTile != null) SpawnUnitAt(_hoveredTile, UnitTeam.Enemy);
            }

            // R 키: 선택된 유닛의 기동력 풀 충전(리셋)
            if (WasKeyJustPressed(KeyCode.R))
            {
                if (_selectedUnit != null)
                {
                    _selectedUnit.ResetMobility();
                    Debug.Log($"[GridTester] 유닛 {_selectedUnit.UnitName} 기동력 리셋 완료 ({_selectedUnit.CurrentMobility}/{_selectedUnit.MaxMobility})");
                    if (_currentMode == UnitActionMode.MoveMode)
                    {
                        ShowMoveStepHighlightsForUnit(_selectedUnit);
                    }
                }
            }

            // W 키: 물(Water) 칸 토글
            if (WasKeyJustPressed(KeyCode.W))
            {
                if (_hoveredTile != null)
                {
                    TileType nextType = _hoveredTile.Type == TileType.Water ? TileType.Normal : TileType.Water;
                    _hoveredTile.SetTileType(nextType);
                }
            }

            // B 키: 나무 다리(Bridge) 칸 토글
            if (WasKeyJustPressed(KeyCode.B))
            {
                if (_hoveredTile != null)
                {
                    TileType nextType = _hoveredTile.Type == TileType.Bridge ? TileType.Normal : TileType.Bridge;
                    _hoveredTile.SetTileType(nextType);
                }
            }

            // H 키: 반칸 디딤돌 블록 토글 (0.5h)
            if (WasKeyJustPressed(KeyCode.H))
            {
                if (_hoveredTile != null)
                {
                    bool nextHalf = !_hoveredTile.IsHalfBlock;
                    _hoveredTile.SetHalfBlock(nextHalf, 1.0f);
                    if (_selectedUnit != null && _currentMode == UnitActionMode.MoveMode)
                    {
                        ShowMoveStepHighlightsForUnit(_selectedUnit);
                    }
                }
            }

            // O 키: 장애물 토글
            if (WasKeyJustPressed(KeyCode.O))
            {
                if (_hoveredTile != null)
                {
                    TileType nextType = _hoveredTile.Type == TileType.Obstacle ? TileType.Normal : TileType.Obstacle;
                    _hoveredTile.SetTileType(nextType);
                }
            }

            // F 키: 비행 유닛 모드 토글
            if (WasKeyJustPressed(KeyCode.F))
            {
                _testFlyingUnit = !_testFlyingUnit;
                if (_selectedUnit != null && _currentMode == UnitActionMode.MoveMode)
                {
                    ShowMoveStepHighlightsForUnit(_selectedUnit);
                }
            }

            // ESC 키: 선택 해제 / 메뉴 닫기
            if (WasKeyJustPressed(KeyCode.Escape))
            {
                ClearSelection();
            }

            // 숫자 1~5 키 (유닛 미선택 시 지형 프리셋 전환)
            if (_selectedUnit == null)
            {
                if (WasKeyJustPressed(KeyCode.Alpha1)) { GridManager.Instance.ApplyFieldPreset(FieldPresetType.DefaultPlain); ClearSelection(); }
                if (WasKeyJustPressed(KeyCode.Alpha2)) { GridManager.Instance.ApplyFieldPreset(FieldPresetType.RiverField); ClearSelection(); }
                if (WasKeyJustPressed(KeyCode.Alpha3)) { GridManager.Instance.ApplyFieldPreset(FieldPresetType.HighlandField); ClearSelection(); }
                if (WasKeyJustPressed(KeyCode.Alpha4)) { GridManager.Instance.ApplyFieldPreset(FieldPresetType.MarshField); ClearSelection(); }
                if (WasKeyJustPressed(KeyCode.Alpha5)) { GridManager.Instance.ApplyFieldPreset(FieldPresetType.BattleIsland); ClearSelection(); }
            }

            // Space 키: 모든 타일 리셋
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

        private void HandleLeftClick()
        {
            if (_hoveredTile == null)
            {
                ClearSelection();
                return;
            }

            // ================= 1. 이동 모드 (MoveMode) =================
            // 기동력이 남아있고, 이동 가능한 십자 1칸 타일을 클릭한 경우 -> 이동 및 기동력 1 차감!
            if (_currentMode == UnitActionMode.MoveMode && _selectedUnit != null && !_selectedUnit.IsMoving)
            {
                if (_movableTiles.Contains(_hoveredTile) && _hoveredTile.OccupyingUnit == null)
                {
                    ExecuteUnitMoveStep(_selectedUnit, _hoveredTile);
                    return;
                }
            }

            // ================= 2. 공격 모드 (AttackMode) =================
            // 공격 사거리 내에 위치한 타겟 유닛(상대 유닛)을 클릭한 경우 -> [전투 진입]!
            if (_currentMode == UnitActionMode.AttackMode && _selectedUnit != null && !_selectedUnit.IsMoving)
            {
                if (_attackableTiles.Contains(_hoveredTile))
                {
                    Unit.Unit targetUnit = _hoveredTile.OccupyingUnit != null ? _hoveredTile.OccupyingUnit.GetComponent<Unit.Unit>() : null;
                    if (targetUnit != null && targetUnit != _selectedUnit)
                    {
                        ExecuteCombatEngagement(_selectedUnit, targetUnit);
                        return;
                    }
                }
            }

            // ================= 3. 새 유닛 선택 =================
            if (_hoveredTile.OccupyingUnit != null)
            {
                Unit.Unit clickedUnit = _hoveredTile.OccupyingUnit.GetComponent<Unit.Unit>();
                if (clickedUnit != null)
                {
                    SelectUnit(clickedUnit, _hoveredTile);
                    return;
                }
            }

            // 빈 타일 클릭 시 선택 해제
            ClearSelection();
        }

        /// <summary>
        /// 유닛 선택 및 쿠키런 3종 선택지 팝업
        /// </summary>
        private void SelectUnit(Unit.Unit unit, Tile tile)
        {
            ClearSelection();

            _selectedUnit = unit;
            _selectedTile = tile;

            _selectedUnit.SetSelected(true);
            _selectedTile.HandleMouseClick();
            _selectedTile.SetHighlight(TileHighlightState.Selected);

            _currentMode = UnitActionMode.ChoiceMenu;
            if (UnitActionMenuUI.Instance != null)
            {
                UnitActionMenuUI.Instance.OpenMenu(_selectedUnit);
            }

            Debug.Log($"[GridTester] 유닛 '{unit.UnitName}'(팀: {unit.Team}, 기동력: {unit.CurrentMobility}/{unit.MaxMobility}) 선택됨 -> 쿠키런 3종 선택지 오픈");
        }

        // ================= 쿠키런 액션 선택 핸들러 =================

        /// <summary>
        /// 선택지 1: 공격 (사거리 표시 및 타겟 조준선 활성화)
        /// </summary>
        private void HandleAttackModeSelected(Unit.Unit unit)
        {
            _currentMode = UnitActionMode.AttackMode;
            ClearVisualsOnly();

            if (unit == null || unit.CurrentTile == null) return;

            // 공격 사거리 계산 (맨해튼 거리 기준)
            List<Tile> inRangeTiles = GridManager.Instance.GetAttackRangeTiles(unit.CurrentTile.Coord, unit.AttackRange);
            foreach (var t in inRangeTiles)
            {
                if (t != unit.CurrentTile && !t.IsDestroyed)
                {
                    t.SetHighlight(TileHighlightState.Attackable);
                    _activeHighlightTiles.Add(t);
                    _attackableTiles.Add(t);

                    // 범위 내에 상대 유닛이 있는 경우 붉은 타겟팅 조준선(Reticle) 표시
                    if (t.OccupyingUnit != null)
                    {
                        Unit.Unit otherUnit = t.OccupyingUnit.GetComponent<Unit.Unit>();
                        if (otherUnit != null && otherUnit.Team != unit.Team)
                        {
                            _targetableEnemies.Add(otherUnit);
                            ShowTargetReticleOnTile(t);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 선택지 2: 이동 (유닛 머리 위 기동력 표시 및 앞/뒤/좌/우 1칸 선택 가능 범위 표시)
        /// </summary>
        private void HandleMoveModeSelected(Unit.Unit unit)
        {
            _currentMode = UnitActionMode.MoveMode;
            ShowMoveStepHighlightsForUnit(unit);
        }

        private void ShowMoveStepHighlightsForUnit(Unit.Unit unit)
        {
            ClearVisualsOnly();
            if (unit == null || unit.CurrentTile == null) return;

            // 머리 위 기동력 뱃지 켜기
            unit.ShowMobilityBadge(true);

            // 기동력이 0인 경우 이동 불가 안내
            if (unit.CurrentMobility <= 0)
            {
                Debug.Log($"[GridTester] 유닛 '{unit.UnitName}'의 기동력이 소진되었습니다 (0/{unit.MaxMobility}). R 키로 충전 가능.");
                return;
            }

            // 앞, 뒤, 좌, 우 (+ 십자 모양) 1칸 이동 가능 타일 탐색
            // 단차 1칸 이상 블록은 직접 등판 불가(0.55f 이하만 통과 가능), 반칸 디딤돌을 통해서만 등판
            bool isFlying = unit.IsFlying || _testFlyingUnit;
            List<Tile> stepNeighbors = GridManager.Instance.GetMovableStepNeighbors(unit.CurrentTile.Coord, isFlying, 0.55f);

            foreach (var t in stepNeighbors)
            {
                t.SetHighlight(TileHighlightState.Movable);
                _activeHighlightTiles.Add(t);
                _movableTiles.Add(t);
                ShowBorderIndicatorOnTile(t); // 네온 경계선 표시
            }
        }

        /// <summary>
        /// 유닛의 1칸 이동 실행 및 기동력 1 차감
        /// </summary>
        private void ExecuteUnitMoveStep(Unit.Unit unit, Tile targetTile)
        {
            if (unit == null || targetTile == null || unit.CurrentMobility <= 0) return;

            // 기동력 1 차감
            unit.ConsumeMobility(1);
            Debug.Log($"[GridTester] 유닛 '{unit.UnitName}' 1칸 이동! 남은 기동력: {unit.CurrentMobility}/{unit.MaxMobility}");

            ClearVisualsOnly();
            _selectedTile = targetTile;

            unit.MoveToTile(targetTile, () =>
            {
                // 이동 완료 후, 여전히 유닛이 살아있고 이동 모드인 경우
                if (unit != null && !unit.IsDead && _selectedUnit == unit)
                {
                    targetTile.SetHighlight(TileHighlightState.Selected);

                    // 기동력이 아직 남아있다면 다음 +1칸 이동 범위 즉시 갱신
                    if (unit.CurrentMobility > 0)
                    {
                        ShowMoveStepHighlightsForUnit(unit);
                    }
                    else
                    {
                        // 기동력 모두 소진 시 뱃지 업데이트 후 완료 상태 유지
                        unit.UpdateMobilityBadge();
                        Debug.Log($"[GridTester] 유닛 '{unit.UnitName}' 기동력 0 소진 완료.");
                    }
                }
            });
        }

        /// <summary>
        /// 선택지 1: 공격 사거리 내 적 선택 시 [전투 진입] 실행!
        /// </summary>
        private void ExecuteCombatEngagement(Unit.Unit attacker, Unit.Unit defender)
        {
            if (attacker == null || defender == null || attacker.IsMoving || defender.IsDead) return;

            Debug.Log($"[GridTester] ⚔️ [전투 진입] 공격자 '{attacker.UnitName}'(공격력 {attacker.AttackPower}) -> 방어자 '{defender.UnitName}'(HP {defender.CurrentHp})");

            // 1. "전투 진입 (BATTLE START)" 다이내믹 컷인 배너 연출
            if (UnitActionMenuUI.Instance != null)
            {
                UnitActionMenuUI.Instance.TriggerBattleStartCutIn(attacker, defender);
            }

            ClearVisualsOnly();

            // 2. 공격 돌진 타격 애니메이션 실행
            attacker.ExecuteAttack(defender, () =>
            {
                Debug.Log($"[GridTester] ⚔️ 전투 종료. 피격 유닛 HP: {defender.CurrentHp}/{defender.MaxHp}");
                if (defender.IsDead)
                {
                    Debug.Log($"[GridTester] 🏆 적 유닛 '{defender.UnitName}' 격파 완료!");
                }

                // 전투 완료 후 메뉴 복귀
                if (attacker != null && !attacker.IsDead)
                {
                    SelectUnit(attacker, attacker.CurrentTile);
                }
                else
                {
                    ClearSelection();
                }
            });
        }

        /// <summary>
        /// 선택지 3: 효과 발동 핸들러
        /// </summary>
        private void HandleSkillModeSelected(Unit.Unit unit)
        {
            _currentMode = UnitActionMode.ChoiceMenu;
            Debug.Log($"[GridTester] 유닛 '{unit.UnitName}' 효과 발동 연출 실행.");
        }

        private void HandleCancelSelected()
        {
            ClearSelection();
        }

        /// <summary>
        /// 특정 타일에 유닛 소환 (팀 구분: Player 또는 Enemy)
        /// </summary>
        public void SpawnUnitAt(Tile tile, UnitTeam team)
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

            if (tile.OccupyingUnit != null)
            {
                Destroy(tile.OccupyingUnit);
                tile.SetOccupyingUnit(null);
            }

            GameObject unitObj = Instantiate(_unitPrefab);
            Unit.Unit unitComp = unitObj.GetComponent<Unit.Unit>();
            if (unitComp != null)
            {
                unitComp.Team = team;
                unitComp.UnitName = (team == UnitTeam.Player) ? "Pawn" : "Enemy Pawn";
                unitComp.PlaceOnTile(tile);
                SelectUnit(unitComp, tile);
                Debug.Log($"[GridTester] 타일 {tile.Coord}에 {team} 유닛 '{unitComp.UnitName}' 소환 완료!");
            }
        }

        private void ClearSelection()
        {
            if (_selectedUnit != null)
            {
                _selectedUnit.SetSelected(false);
                _selectedUnit.ShowMobilityBadge(false);
                _selectedUnit = null;
            }

            ClearVisualsOnly();

            if (_selectedTile != null)
            {
                _selectedTile.SetHighlight(TileHighlightState.None);
                _selectedTile = null;
            }

            _currentMode = UnitActionMode.None;
            if (UnitActionMenuUI.Instance != null)
            {
                UnitActionMenuUI.Instance.CloseMenu();
            }
        }

        private void ClearVisualsOnly()
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
            _attackableTiles.Clear();
            _targetableEnemies.Clear();

            ClearBorderIndicators();
            ClearReticleIndicators();
        }

        private void ClearBorderIndicators()
        {
            foreach (var ind in _activeBorderIndicators)
            {
                if (ind != null) ind.SetActive(false);
            }
        }

        private void ClearReticleIndicators()
        {
            foreach (var ind in _activeReticleIndicators)
            {
                if (ind != null) ind.SetActive(false);
            }
        }

        private void ShowBorderIndicatorOnTile(Tile tile)
        {
            if (tile == null) return;
            LoadSpritesIfNull();
            if (_moveBorderSprite == null) return;

            EnsureIndicatorContainer();

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
                indicator.transform.SetParent(_indicatorContainer, false);
                SpriteRenderer sr = indicator.AddComponent<SpriteRenderer>();
                sr.sprite = _moveBorderSprite;
                sr.color = new Color(0.25f, 0.95f, 1.0f, 0.95f);
                sr.sortingOrder = 5;
                _activeBorderIndicators.Add(indicator);
            }

            indicator.transform.position = tile.TopSurfacePosition + Vector3.up * 0.015f;
            indicator.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            indicator.transform.localScale = Vector3.one * 0.98f;
            indicator.SetActive(true);
        }

        private void ShowTargetReticleOnTile(Tile tile)
        {
            if (tile == null) return;
            LoadSpritesIfNull();
            if (_targetReticleSprite == null) return;

            EnsureIndicatorContainer();

            GameObject indicator = null;
            for (int i = 0; i < _activeReticleIndicators.Count; i++)
            {
                if (_activeReticleIndicators[i] != null && !_activeReticleIndicators[i].activeSelf)
                {
                    indicator = _activeReticleIndicators[i];
                    break;
                }
            }

            if (indicator == null)
            {
                indicator = new GameObject("TargetReticleIndicator");
                indicator.transform.SetParent(_indicatorContainer, false);
                SpriteRenderer sr = indicator.AddComponent<SpriteRenderer>();
                sr.sprite = _targetReticleSprite;
                sr.color = new Color(1.0f, 0.2f, 0.2f, 0.95f);
                sr.sortingOrder = 6;
                _activeReticleIndicators.Add(indicator);
            }

            indicator.transform.position = tile.TopSurfacePosition + Vector3.up * 0.025f;
            indicator.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            indicator.transform.localScale = Vector3.one * 0.95f;
            indicator.SetActive(true);
        }

        private void EnsureIndicatorContainer()
        {
            if (_indicatorContainer == null)
            {
                GameObject containerObj = new GameObject("_IndicatorsContainer");
                containerObj.transform.SetParent(transform, false);
                _indicatorContainer = containerObj.transform;
            }
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
                    case KeyCode.S: return UnityEngine.InputSystem.Keyboard.current.sKey.wasPressedThisFrame;
                    case KeyCode.E: return UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame;
                    case KeyCode.R: return UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame;
                    case KeyCode.W: return UnityEngine.InputSystem.Keyboard.current.wKey.wasPressedThisFrame;
                    case KeyCode.B: return UnityEngine.InputSystem.Keyboard.current.bKey.wasPressedThisFrame;
                    case KeyCode.O: return UnityEngine.InputSystem.Keyboard.current.oKey.wasPressedThisFrame;
                    case KeyCode.H: return UnityEngine.InputSystem.Keyboard.current.hKey.wasPressedThisFrame;
                    case KeyCode.F: return UnityEngine.InputSystem.Keyboard.current.fKey.wasPressedThisFrame;
                    case KeyCode.Space: return UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame;
                    case KeyCode.Escape: return UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame;
                    case KeyCode.Alpha1: return UnityEngine.InputSystem.Keyboard.current.digit1Key.wasPressedThisFrame;
                    case KeyCode.Alpha2: return UnityEngine.InputSystem.Keyboard.current.digit2Key.wasPressedThisFrame;
                    case KeyCode.Alpha3: return UnityEngine.InputSystem.Keyboard.current.digit3Key.wasPressedThisFrame;
                    case KeyCode.Alpha4: return UnityEngine.InputSystem.Keyboard.current.digit4Key.wasPressedThisFrame;
                    case KeyCode.Alpha5: return UnityEngine.InputSystem.Keyboard.current.digit5Key.wasPressedThisFrame;
                }
            }
#endif
            return Input.GetKeyDown(key);
        }

        // ================= Debug GUI Overlay =================
        private void OnGUI()
        {
            if (!_showDebugGUI) return;

            GUI.Box(new Rect(15, 15, 430, 390), "♟ 체스 몬스터즈 9x9 전장 시스템 테스터");

            GUILayout.BeginArea(new Rect(25, 42, 410, 355));
            GUILayout.Label("• <b>유닛 클릭</b>: 쿠키런 킹덤 스타일 <b>3대 선택지 팝업</b>");
            GUILayout.Label("   - <b>1. 공격</b>: 사거리 내 표시 & 상대 유닛 클릭 시 <b>전투 진입!</b>");
            GUILayout.Label("   - <b>2. 이동</b>: <b>기동력 카운터</b> 표시 & 앞/뒤/좌/우 1칸 이동 시 1 감소");
            GUILayout.Label("   - <b>3. 효과 발동</b>: 쿠키런 스킬 발동 연출");
            GUILayout.Label("• <b>소환</b>: <b>S 키</b> 아군 폰(White) | <b>E 키</b> 적군 몬스터(Red)");
            GUILayout.Label("• <b>R 키</b>: 선택된 유닛 기동력 리셋 | <b>ESC</b>: 선택 취소");
            GUILayout.Label("• <b>B 키</b> 나무 다리(0.3h) | <b>W 키</b> 물(0.6h) | <b>H 키</b> 반칸 디딤돌(0.5h)");
            GUILayout.Label("• <b>단차 이동 규칙</b>: 1칸 높이 블록은 반칸 디딤돌을 통해서만 등판");
            GUILayout.Label("• <b>Space</b>: 전체 타일 복구 | <b>우클릭</b>: 타일 파괴");

            if (_selectedUnit != null)
            {
                GUILayout.Space(2);
                string teamColor = _selectedUnit.Team == UnitTeam.Player ? "cyan" : "red";
                GUILayout.Label($"<b>선택된 유닛:</b> <color={teamColor}>[{_selectedUnit.Team}] {_selectedUnit.UnitName}</color> (HP: {_selectedUnit.CurrentHp}/{_selectedUnit.MaxHp}, 남은 기동력: <color=yellow>{_selectedUnit.CurrentMobility}/{_selectedUnit.MaxMobility}</color>, 사거리: {_selectedUnit.AttackRange})");
                GUILayout.Label($"<b>현재 모드:</b> <color=yellow>{_currentMode}</color>");
            }
            else if (_hoveredTile != null)
            {
                GUILayout.Space(2);
                string unitInfo = _hoveredTile.OccupyingUnit != null ? " | 유닛 탑승 중" : "";
                GUILayout.Label($"<b>호버 타일:</b> {_hoveredTile.Coord} | 타입: {_hoveredTile.Type} | Y: {_hoveredTile.Height:F1}{unitInfo}");
            }

            GUILayout.Space(4);
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("아군 소환 (S)", GUILayout.Height(24)))
                {
                    Tile t = _hoveredTile ?? GridManager.Instance.GetTile(4, 3);
                    if (t != null) SpawnUnitAt(t, UnitTeam.Player);
                }

                if (GUILayout.Button("적군 소환 (E)", GUILayout.Height(24)))
                {
                    Tile t = _hoveredTile ?? GridManager.Instance.GetTile(4, 5);
                    if (t != null) SpawnUnitAt(t, UnitTeam.Enemy);
                }

                if (GUILayout.Button("선택 해제 (ESC)", GUILayout.Height(24)))
                {
                    ClearSelection();
                }
            }

            GUILayout.EndArea();
        }
    }
}
