using System.Collections.Generic;
using UnityEngine;

namespace ChessMonsters.Grid
{
    public enum TilePlacementMode
    {
        [Tooltip("체스판 교차 패턴 (밝은 칸: 잔디, 어두운 칸: 흙)")]
        Checkerboard,

        [Tooltip("자연스러운 무작위 잔디 배치 (비율 및 시드 기반)")]
        RandomGrass
    }

    /// <summary>
    /// 7x7 체스판 그리드를 생성하고 관리하는 매니저 클래스
    /// </summary>
    [ExecuteAlways]
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance { get; private set; }

        public const int BoardWidth = 7;
        public const int BoardHeight = 7;

        [Header("Grid Layout Settings")]
        [SerializeField] private float _tileSize = 1.0f;
        [Tooltip("타일 높이/두께 (1.0 = 완벽한 정육면체 큐브, 0.3 = 납작한 슬랩 타일)")]
        [SerializeField] private float _tileThickness = 1.0f;
        [SerializeField] private float _tileSpacing = 0.04f;

        [Header("Tile Placement (잔디 및 지형 배치 설정)")]
        [Tooltip("타일 배치 모드: 체스판 교차 배치 또는 랜덤 잔디 배치")]
        [SerializeField] private TilePlacementMode _placementMode = TilePlacementMode.RandomGrass;

        [Tooltip("랜덤 시드 (0이면 생성 시 무작위 시드 사용)")]
        [SerializeField] private int _randomSeed = 0;

        [Tooltip("랜덤 배치 시 잔디 타일의 비율 (0 ~ 1)")]
        [Range(0f, 1f)]
        [SerializeField] private float _grassRatio = 0.65f;

        [Tooltip("잔디 타일 중 꽃이 핀 잔디의 비율 (0 ~ 1)")]
        [Range(0f, 1f)]
        [SerializeField] private float _flowerGrassRatio = 0.35f;

        [Tooltip("흙길 타일 중 풀 테두리 흙길의 비율 (0 ~ 1)")]
        [Range(0f, 1f)]
        [SerializeField] private float _dirtGrassRatio = 0.40f;

        [Tooltip("타일을 90도 단위로 무작위 회전하여 텍스처 반복 느낌 완화")]
        [SerializeField] private bool _randomizeRotation = true;

        [Header("Tile Prefab (Optional)")]
        [Tooltip("지정하지 않으면 윗면 잔디 + 5면 흙 2-Submesh 3D 큐브 타일이 자동으로 절차적 생성됩니다.")]
        [SerializeField] private GameObject _tilePrefab;

        [Header("Tile Materials (URP Textures)")]
        [Tooltip("풀밭 기본 머티리얼 (Tile_Grass_04)")]
        [SerializeField] private Material _lightTileMaterial;

        [Tooltip("꽃이 핀 풀밭 머티리얼 (Tile_GrassFlowers_03)")]
        [SerializeField] private Material _lightTileVariationMaterial;

        [Tooltip("흙길 기본 머티리얼 (Tile_Dirt_01)")]
        [SerializeField] private Material _darkTileMaterial;

        [Tooltip("잔디 테두리 흙길 머티리얼 (Tile_DirtGrass_02)")]
        [SerializeField] private Material _darkTileVariationMaterial;

        [Tooltip("물 타일 머티리얼")]
        [SerializeField] private Material _waterMaterial;

        [Tooltip("장애물 타일 머티리얼")]
        [SerializeField] private Material _obstacleMaterial;

        [Header("Default Materials (Fallback)")]
        [SerializeField] private Material _boardMaterial;

        [Header("Fall Floor Y")]
        [SerializeField] private float _fallFloorY = -5.0f;

        [Header("Field Preset (정해진 지형)")]
        [SerializeField] private FieldPresetType _currentPreset = FieldPresetType.DefaultPlain;

        [SerializeField, HideInInspector]
        private Tile[] _serializedTiles = new Tile[BoardWidth * BoardHeight];

        private Tile[,] _grid = new Tile[BoardWidth, BoardHeight];

        public float TileSize => _tileSize;
        public float TileSpacing => _tileSpacing;
        public float FallFloorY => _fallFloorY;
        public FieldPresetType CurrentPreset => _currentPreset;

        public Material LightTileMaterial => _lightTileMaterial;
        public Material LightTileVariationMaterial => _lightTileVariationMaterial;
        public Material DarkTileMaterial => _darkTileMaterial;
        public Material DarkTileVariationMaterial => _darkTileVariationMaterial;
        public Material WaterMaterial => _waterMaterial;
        public Material ObstacleMaterial => _obstacleMaterial;

        public float TileThickness
        {
            get => _tileThickness;
            set => _tileThickness = value;
        }

        public TilePlacementMode PlacementMode
        {
            get => _placementMode;
            set => _placementMode = value;
        }

        public int RandomSeed
        {
            get => _randomSeed;
            set => _randomSeed = value;
        }

        /// <summary>
        /// 타일 두께(높이) 변경 및 그리드 재생성 (1.0f = 정육면체 큐브, 0.3f = 슬랩)
        /// </summary>
        public void SetTileThickness(float thickness)
        {
            _tileThickness = thickness;
            GenerateGrid();
        }

        /// <summary>
        /// 잔디를 자연스럽게 랜덤 재배치 (새로운 시드 생성)
        /// </summary>
        public void RandomizeGrassPlacement(int newSeed = 0)
        {
            _placementMode = TilePlacementMode.RandomGrass;
            _randomSeed = (newSeed != 0) ? newSeed : UnityEngine.Random.Range(1, 999999);
            GenerateGrid();
        }

        /// <summary>
        /// 체스판 체크무늬 교차 배치로 복원
        /// </summary>
        public void SetCheckerboardPlacement()
        {
            _placementMode = TilePlacementMode.Checkerboard;
            GenerateGrid();
        }

        private void Awake()
        {
            if (Application.isPlaying)
            {
                if (Instance != null && Instance != this)
                {
                    Destroy(gameObject);
                    return;
                }
                Instance = this;
            }

            RebuildGridFromChildren();
        }

        private void Start()
        {
            if (Application.isPlaying && GetTileCount() == 0)
            {
                GenerateGrid();
            }

            if (Application.isPlaying && _currentPreset != FieldPresetType.DefaultPlain)
            {
                ApplyFieldPreset(_currentPreset);
            }
        }

        /// <summary>
        /// 씬에 이미 존재하는 타일 자식 오브젝트들을 2차원 배열에 매핑
        /// </summary>
        public void RebuildGridFromChildren()
        {
            _grid = new Tile[BoardWidth, BoardHeight];
            Tile[] childTiles = GetComponentsInChildren<Tile>(true);

            foreach (var tile in childTiles)
            {
                Vector2Int c = tile.Coord;
                if (IsValidCoord(c.x, c.y))
                {
                    _grid[c.x, c.y] = tile;
                }
            }
        }

        /// <summary>
        /// 7x7 체스판 타일 오브젝트 일괄 생성
        /// </summary>
        [ContextMenu("7x7 그리드 생성 (Generate Grid)")]
        public void GenerateGrid()
        {
#if UNITY_EDITOR
            SetupDefaultMaterialsIfNull();
#endif
            ClearGrid();
            _grid = new Tile[BoardWidth, BoardHeight];

            int activeSeed = _randomSeed != 0 ? _randomSeed : UnityEngine.Random.Range(1, 999999);
            System.Random rnd = new System.Random(activeSeed);

            // 7x7 중앙 정렬 오프셋
            float step = _tileSize + _tileSpacing;
            float offsetX = (BoardWidth - 1) * step * 0.5f;
            float offsetZ = (BoardHeight - 1) * step * 0.5f;

            for (int z = 0; z < BoardHeight; z++)
            {
                for (int x = 0; x < BoardWidth; x++)
                {
                    Vector3 localPos = new Vector3(
                        x * step - offsetX,
                        0f,
                        z * step - offsetZ
                    );

                    Tile tile = CreateTileObject(x, z, localPos, rnd);
                    _grid[x, z] = tile;
                    _serializedTiles[z * BoardWidth + x] = tile;
                }
            }

            Debug.Log($"[GridManager] 7x7 체스판 그리드 오브젝트(총 49칸) 생성 완료. (모드: {_placementMode}, 두께: {_tileThickness:F1}, 시드: {activeSeed})");
        }

        /// <summary>
        /// 기획서 명세: "7×7의 공간을 가지고 있으며 정해진 지형이 존재한다."
        /// 정해진 지형 프리셋(기본 평원, 강/호수, 고지대 단차, 습지)을 필드에 적용합니다.
        /// </summary>
        public void ApplyFieldPreset(FieldPresetType preset)
        {
            _currentPreset = preset;
            if (_grid == null || _grid.GetLength(0) != BoardWidth) RebuildGridFromChildren();

            // 1. 모든 타일을 기본 일반 상태(높이 0, 파괴 복구)로 초기화
            for (int z = 0; z < BoardHeight; z++)
            {
                for (int x = 0; x < BoardWidth; x++)
                {
                    Tile tile = GetTile(x, z);
                    if (tile != null)
                    {
                        tile.RestoreTile(TileType.Normal, false);
                        tile.SetHeight(0f);
                    }
                }
            }

            // 2. 프리셋별 특수 지형 적용
            switch (preset)
            {
                case FieldPresetType.RiverField:
                    // 강/호수 지형: 중앙(Z = 3)이 물로 채워지고, 특정 칸(X = 1, X = 5)에 건널 수 있는 여울/다리 형성
                    for (int x = 0; x < BoardWidth; x++)
                    {
                        if (x != 1 && x != 5)
                        {
                            Tile t = GetTile(x, 3);
                            if (t != null) t.SetTileType(TileType.Water);
                        }
                    }
                    break;

                case FieldPresetType.HighlandField:
                    // 고지대 지형: 중앙 3x3 영역(X: 2~4, Z: 2~4)이 단차(높이 1.0f)로 솟아 있음
                    // 기획서의 필드 파괴 시 낙하 높이에 비례한 추락 피해 및 비행 유닛의 고저차 무시 특성을 체감할 수 있는 3D 입체 전장
                    for (int z = 2; z <= 4; z++)
                    {
                        for (int x = 2; x <= 4; x++)
                        {
                            Tile t = GetTile(x, z);
                            if (t != null)
                            {
                                t.SetHeight(1.0f);
                            }
                        }
                    }
                    Tile obs1 = GetTile(0, 0); if (obs1 != null) obs1.SetTileType(TileType.Obstacle);
                    Tile obs2 = GetTile(6, 6); if (obs2 != null) obs2.SetTileType(TileType.Obstacle);
                    break;

                case FieldPresetType.MarshField:
                    // 습지 지형: 징검다리 형태로 물 칸들이 곳곳에 분산 배치됨
                    Vector2Int[] waterCoords = new Vector2Int[]
                    {
                        new Vector2Int(1, 1), new Vector2Int(5, 1),
                        new Vector2Int(2, 3), new Vector2Int(3, 3), new Vector2Int(4, 3),
                        new Vector2Int(1, 5), new Vector2Int(5, 5)
                    };
                    foreach (var wc in waterCoords)
                    {
                        Tile t = GetTile(wc.x, wc.y);
                        if (t != null) t.SetTileType(TileType.Water);
                    }
                    break;

                case FieldPresetType.DefaultPlain:
                default:
                    // 기본 평평한 7x7 체스판
                    break;
            }

            Debug.Log($"[GridManager] 지형 프리셋 적용 완료: {preset}");
        }

        private Tile CreateTileObject(int x, int z, Vector3 localPos, System.Random rnd = null)
        {
            GameObject tileObj;

            if (_tilePrefab != null)
            {
#if UNITY_EDITOR
                tileObj = UnityEditor.PrefabUtility.InstantiatePrefab(_tilePrefab, transform) as GameObject;
#else
                tileObj = Instantiate(_tilePrefab, transform);
#endif
            }
            else
            {
                // 윗면(잔디)과 나머지 5면(흙)을 분리 렌더링하기 위한 2-Submesh 3D 정육면체 큐브 생성
                tileObj = new GameObject($"Tile_{x}_{z}");
                tileObj.transform.SetParent(transform);

                MeshFilter filter = tileObj.AddComponent<MeshFilter>();
                filter.sharedMesh = TileMeshUtility.GetOrCreateTwoSubmeshCubeMesh();

                tileObj.AddComponent<MeshRenderer>();

                BoxCollider bc = tileObj.AddComponent<BoxCollider>();
                bc.size = Vector3.one;
            }

            tileObj.name = $"Tile_{x}_{z}";
            tileObj.transform.localPosition = localPos;

            // 텍스처 반복 느낌을 완화하기 위해 90도 단위 무작위 회전
            if (_randomizeRotation && rnd != null)
            {
                int rotY = rnd.Next(0, 4) * 90;
                tileObj.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
            }
            else
            {
                tileObj.transform.localRotation = Quaternion.identity;
            }

            // 1.0 x 1.0 x 1.0 완벽한 정육면체 큐브 스케일
            tileObj.transform.localScale = new Vector3(_tileSize, _tileThickness, _tileSize);

            Tile tile = tileObj.GetComponent<Tile>();
            if (tile == null)
            {
                tile = tileObj.AddComponent<Tile>();
            }

            // 1. 윗면(Top Face) 머티리얼 결정
            Material topMat = null;
            bool isDark = (x + z) % 2 == 1;

            if (_placementMode == TilePlacementMode.RandomGrass && rnd != null)
            {
                // 랜덤 잔디 배치: _grassRatio 확률로 잔디, 나머지 확률로 흙길
                bool isGrass = (float)rnd.NextDouble() < _grassRatio;
                if (isGrass)
                {
                    bool isFlower = (float)rnd.NextDouble() < _flowerGrassRatio;
                    topMat = (isFlower && _lightTileVariationMaterial != null)
                        ? _lightTileVariationMaterial
                        : _lightTileMaterial;
                }
                else
                {
                    bool isBorderDirt = (float)rnd.NextDouble() < _dirtGrassRatio;
                    topMat = (isBorderDirt && _darkTileVariationMaterial != null)
                        ? _darkTileVariationMaterial
                        : _darkTileMaterial;
                }
            }
            else
            {
                // 체스판 체크무늬 배치
                if (isDark)
                {
                    topMat = ((x + 2 * z) % 3 == 0 && _darkTileVariationMaterial != null)
                        ? _darkTileVariationMaterial
                        : _darkTileMaterial;
                }
                else
                {
                    topMat = ((x * 3 + z * 5) % 4 == 0 && _lightTileVariationMaterial != null)
                        ? _lightTileVariationMaterial
                        : _lightTileMaterial;
                }
            }

            if (topMat == null) topMat = _boardMaterial;

            // 2. 나머지 5면(옆면 4면 + 밑면)은 항상 일반 흙(Tile_Dirt_01) 머티리얼 적용
            Material sideMat = _darkTileMaterial ?? _boardMaterial;

            // URP Lit 안전 폴백 (분홍색 셰이더 에러 방지)
            if (topMat == null || sideMat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                             ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                             ?? Shader.Find("Universal Render Pipeline/Unlit")
                             ?? Shader.Find("Standard");
                Material fallbackMat = new Material(shader) { name = "Tile_Runtime_Fallback_Mat" };
                if (topMat == null) topMat = fallbackMat;
                if (sideMat == null) sideMat = fallbackMat;
            }

            MeshRenderer mr = tileObj.GetComponent<MeshRenderer>();
            MeshFilter mf = tileObj.GetComponent<MeshFilter>();
            if (mr != null)
            {
                // 2-Submesh 메시일 경우 [Submesh 0 = 흙, Submesh 1 = 잔디] 슬롯에 각 머티리얼 바인딩
                if (mf != null && mf.sharedMesh != null && mf.sharedMesh.subMeshCount >= 2)
                {
                    mr.sharedMaterials = new Material[] { sideMat, topMat };
                }
                else
                {
                    mr.sharedMaterial = topMat;
                }
            }

            tile.Initialize(new Vector2Int(x, z), isDark, 0f, TileType.Normal, topMat, sideMat);

            return tile;
        }

#if UNITY_EDITOR
        public void SetupDefaultMaterialsIfNull()
        {
            if (_lightTileMaterial == null)
                _lightTileMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Tiles/Mat_Tile_Grass_04.mat");
            if (_lightTileVariationMaterial == null)
                _lightTileVariationMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Tiles/Mat_Tile_GrassFlowers_03.mat");
            if (_darkTileMaterial == null)
                _darkTileMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Tiles/Mat_Tile_Dirt_01.mat");
            if (_darkTileVariationMaterial == null)
                _darkTileVariationMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Tiles/Mat_Tile_DirtGrass_02.mat");
            if (_waterMaterial == null)
                _waterMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Tiles/Mat_Tile_Water.mat");
            if (_obstacleMaterial == null)
                _obstacleMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Tiles/Mat_Tile_Obstacle.mat");
        }
#endif

        /// <summary>
        /// 보드의 모든 타일 제거
        /// </summary>
        [ContextMenu("그리드 초기화 (Clear Grid)")]
        public void ClearGrid()
        {
            List<GameObject> children = new List<GameObject>();
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                children.Add(transform.GetChild(i).gameObject);
            }

            foreach (var child in children)
            {
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }

            _grid = new Tile[BoardWidth, BoardHeight];
            _serializedTiles = new Tile[BoardWidth * BoardHeight];
        }

        public bool IsValidCoord(int x, int z)
        {
            return x >= 0 && x < BoardWidth && z >= 0 && z < BoardHeight;
        }

        public Tile GetTile(int x, int z)
        {
            if (!IsValidCoord(x, z)) return null;
            if (_grid == null || _grid.GetLength(0) != BoardWidth) RebuildGridFromChildren();
            return _grid[x, z];
        }

        public Tile GetTile(Vector2Int coord)
        {
            return GetTile(coord.x, coord.y);
        }

        /// <summary>
        /// 그리드 좌표(x, z)를 유니티 월드 좌표로 변환
        /// </summary>
        public Vector3 GridToWorldPosition(int x, int z, float height = 0f)
        {
            Tile tile = GetTile(x, z);
            if (tile != null)
            {
                Vector3 p = tile.transform.position;
                p.y += _tileThickness * 0.5f; // 타일 윗면
                return p;
            }

            float step = _tileSize + _tileSpacing;
            float offsetX = (BoardWidth - 1) * step * 0.5f;
            float offsetZ = (BoardHeight - 1) * step * 0.5f;
            Vector3 localPos = new Vector3(x * step - offsetX, height, z * step - offsetZ);
            return transform.TransformPoint(localPos);
        }

        /// <summary>
        /// 월드 좌표에서 가장 가까운 그리드 좌표 탐색
        /// </summary>
        public Vector2Int WorldToGridPosition(Vector3 worldPos)
        {
            Vector3 localPos = transform.InverseTransformPoint(worldPos);
            float step = _tileSize + _tileSpacing;
            float offsetX = (BoardWidth - 1) * step * 0.5f;
            float offsetZ = (BoardHeight - 1) * step * 0.5f;

            int x = Mathf.RoundToInt((localPos.x + offsetX) / step);
            int z = Mathf.RoundToInt((localPos.z + offsetZ) / step);

            x = Mathf.Clamp(x, 0, BoardWidth - 1);
            z = Mathf.Clamp(z, 0, BoardHeight - 1);

            return new Vector2Int(x, z);
        }

        /// <summary>
        /// 기획서 기능: 특정 칸 파괴 (유닛 추락 처리 포함)
        /// </summary>
        public void DestroyTile(int x, int z, bool animate = true)
        {
            Tile tile = GetTile(x, z);
            if (tile != null && !tile.IsDestroyed)
            {
                tile.DestroyTile(animate, _fallFloorY);
            }
        }

        /// <summary>
        /// 기획서 기능: 특정 칸 복구 / 생성
        /// </summary>
        public void CreateTile(int x, int z, TileType type = TileType.Normal, bool animate = true)
        {
            Tile tile = GetTile(x, z);
            if (tile != null)
            {
                tile.RestoreTile(type, animate);
            }
        }

        /// <summary>
        /// 인접 칸 목록 반환 (상하좌우 또는 대각선 포함)
        /// </summary>
        public List<Tile> GetNeighbors(Vector2Int coord, bool includeDiagonals = false)
        {
            List<Tile> neighbors = new List<Tile>();
            Vector2Int[] dirs = includeDiagonals ?
                new Vector2Int[] {
                    Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
                    new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
                } :
                new Vector2Int[] {
                    Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
                };

            foreach (var dir in dirs)
            {
                Vector2Int nCoord = coord + dir;
                Tile nTile = GetTile(nCoord);
                if (nTile != null && !nTile.IsDestroyed)
                {
                    neighbors.Add(nTile);
                }
            }

            return neighbors;
        }

        /// <summary>
        /// 유닛 기동력(이동 범위) 또는 사거리에 따른 타일 탐색 (BFS)
        /// </summary>
        /// <param name="start">시작 좌표</param>
        /// <param name="range">이동력 또는 사거리</param>
        /// <param name="isFlying">기획서 명세: 비행 유닛은 물/높이 제약 무시</param>
        public List<Tile> GetReachableTiles(Vector2Int start, int range, bool isFlying = false)
        {
            List<Tile> reachable = new List<Tile>();
            if (!IsValidCoord(start.x, start.y)) return reachable;

            Dictionary<Vector2Int, int> visited = new Dictionary<Vector2Int, int>();
            Queue<(Vector2Int coord, int dist)> queue = new Queue<(Vector2Int, int)>();

            queue.Enqueue((start, 0));
            visited[start] = 0;

            while (queue.Count > 0)
            {
                var (current, dist) = queue.Dequeue();
                if (dist > 0)
                {
                    Tile t = GetTile(current);
                    if (t != null) reachable.Add(t);
                }

                if (dist >= range) continue;

                foreach (var neighbor in GetNeighbors(current, false))
                {
                    // 비행 유닛이 아니면 통과 불가 타일(물, 장애물 등) 체크
                    if (!isFlying)
                    {
                        if (neighbor.Type == TileType.Water || neighbor.Type == TileType.Obstacle)
                            continue;
                    }

                    Vector2Int nCoord = neighbor.Coord;
                    int newDist = dist + 1;

                    if (!visited.ContainsKey(nCoord) || visited[nCoord] > newDist)
                    {
                        visited[nCoord] = newDist;
                        queue.Enqueue((nCoord, newDist));
                    }
                }
            }

            return reachable;
        }

        /// <summary>
        /// 공격 사거리 내 타일 탐색 (맨해튼 거리 기준)
        /// </summary>
        public List<Tile> GetAttackRangeTiles(Vector2Int center, int attackRange)
        {
            List<Tile> inRange = new List<Tile>();

            for (int z = 0; z < BoardHeight; z++)
            {
                for (int x = 0; x < BoardWidth; x++)
                {
                    if (x == center.x && z == center.y) continue;
                    int dist = Mathf.Abs(x - center.x) + Mathf.Abs(z - center.y);
                    if (dist <= attackRange)
                    {
                        Tile t = GetTile(x, z);
                        if (t != null && !t.IsDestroyed)
                        {
                            inRange.Add(t);
                        }
                    }
                }
            }

            return inRange;
        }

        /// <summary>
        /// 전체 타일의 하이라이트 해제
        /// </summary>
        public void ClearAllHighlights()
        {
            if (_grid == null) return;
            for (int z = 0; z < BoardHeight; z++)
            {
                for (int x = 0; x < BoardWidth; x++)
                {
                    if (_grid[x, z] != null)
                    {
                        _grid[x, z].SetHighlight(TileHighlightState.None);
                    }
                }
            }
        }

        private int GetTileCount()
        {
            int count = 0;
            if (_grid == null) return 0;
            for (int z = 0; z < BoardHeight; z++)
            {
                for (int x = 0; x < BoardWidth; x++)
                {
                    if (_grid[x, z] != null) count++;
                }
            }
            return count;
        }
    }
}
