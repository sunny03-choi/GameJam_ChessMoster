using System.Collections.Generic;
using UnityEngine;

namespace ChessMonsters.Grid
{
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
        [SerializeField] private float _tileThickness = 0.3f;
        [SerializeField] private float _tileSpacing = 0.04f;

        [Header("Tile Prefab (Optional)")]
        [Tooltip("지정하지 않으면 3D 큐브 타일이 자동으로 절차적 생성됩니다.")]
        [SerializeField] private GameObject _tilePrefab;

        [Header("Default Materials (Optional)")]
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
            ClearGrid();
            _grid = new Tile[BoardWidth, BoardHeight];

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

                    Tile tile = CreateTileObject(x, z, localPos);
                    bool isDark = (x + z) % 2 == 1; // 체스판 체크무늬
                    tile.Initialize(new Vector2Int(x, z), isDark, 0f, TileType.Normal);

                    _grid[x, z] = tile;
                    _serializedTiles[z * BoardWidth + x] = tile;
                }
            }

            Debug.Log("[GridManager] 7x7 체스판 그리드 오브젝트(총 49칸) 생성 완료.");
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

        private Tile CreateTileObject(int x, int z, Vector3 localPos)
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
                // 프리팹이 없을 경우 기본 3D 큐브로 자동 생성
                tileObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tileObj.transform.SetParent(transform);
            }

            tileObj.name = $"Tile_{x}_{z}";
            tileObj.transform.localPosition = localPos;
            tileObj.transform.localRotation = Quaternion.identity;
            tileObj.transform.localScale = new Vector3(_tileSize, _tileThickness, _tileSize);

            Tile tile = tileObj.GetComponent<Tile>();
            if (tile == null)
            {
                tile = tileObj.AddComponent<Tile>();
            }

            // 머티리얼 설정
            MeshRenderer mr = tileObj.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                if (_boardMaterial != null)
                {
                    mr.sharedMaterial = _boardMaterial;
                }
                else
                {
                    // 기본 스탠다드 머티리얼 적용
                    Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default");
                    if (shader != null)
                    {
                        Material mat = new Material(shader);
                        mat.name = "Tile_Default_Mat";
                        mr.sharedMaterial = mat;
                    }
                }
            }

            return tile;
        }

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
