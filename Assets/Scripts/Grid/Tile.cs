using System;
using System.Collections;
using UnityEngine;

namespace ChessMonsters.Grid
{
    /// <summary>
    /// 체스판의 개별 칸(타일) 오브젝트를 관리하는 컴포넌트
    /// </summary>
    [SelectionBase]
    public class Tile : MonoBehaviour
    {
        [Header("Grid Info")]
        [SerializeField] private Vector2Int _coord;
        [SerializeField] private TileType _type = TileType.Normal;
        [SerializeField] private float _height = 0f;
        [SerializeField] private bool _isDarkSquare = false;
        [SerializeField] private bool _isDestroyed = false;

        [Header("Occupancy")]
        [SerializeField] private GameObject _occupyingUnit;

        [Header("Visual Components")]
        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private BoxCollider _boxCollider;
        [SerializeField] private Material _topMaterial;
        [SerializeField] private Material _sideMaterial;

        // 색상 프리셋 (에디터/런타임에서 자동 초기화)
        private Color _baseColor = Color.white;
        private static readonly Color ColorHover = new Color(1.0f, 0.95f, 0.4f);       // 마우스 호버 (연노랑)
        private static readonly Color ColorSelected = new Color(1.0f, 0.82f, 0.1f);    // 선택됨 (골드)
        private static readonly Color ColorMovable = new Color(0.35f, 0.85f, 1.0f);    // 이동 가능 범위 (하늘색)
        private static readonly Color ColorAttackable = new Color(1.0f, 0.35f, 0.35f);  // 공격 사거리 (빨강)
        private static readonly Color ColorSpawnable = new Color(0.45f, 1.0f, 0.55f);   // 소환 가능 (초록)

        private MaterialPropertyBlock _propBlock;
        private TileHighlightState _currentHighlight = TileHighlightState.None;
        private Coroutine _animCoroutine;
        private Vector3 _originalLocalPos;

        [SerializeField] private bool _isHalfBlock = false;

        public Vector2Int Coord => _coord;
        public TileType Type => _type;
        public float Height => _height;
        public bool IsDarkSquare => _isDarkSquare;
        public bool IsDestroyed => _isDestroyed;
        public bool IsHalfBlock => _isHalfBlock;
        public bool IsBridge => _type == TileType.Bridge;
        public bool IsWater => _type == TileType.Water;
        public bool IsPassable => !_isDestroyed && _type != TileType.Water && _type != TileType.Obstacle && _occupyingUnit == null;
        public GameObject OccupyingUnit => _occupyingUnit;
        public Vector3 TopSurfacePosition
        {
            get
            {
                float halfThickness = transform.localScale.y * 0.5f;
                return transform.position + Vector3.up * (halfThickness + 0.02f);
            }
        }

        public event Action<Tile> OnClicked;
        public event Action<Tile> OnHoverEnter;
        public event Action<Tile> OnHoverExit;

        private void Awake()
        {
            if (_meshRenderer == null) _meshRenderer = GetComponentInChildren<MeshRenderer>();
            if (_boxCollider == null) _boxCollider = GetComponentInChildren<BoxCollider>();
            if (_meshRenderer != null)
            {
                Material[] mats = _meshRenderer.sharedMaterials;
                if (mats != null && mats.Length >= 2)
                {
                    if (_sideMaterial == null) _sideMaterial = mats[0];
                    if (_topMaterial == null) _topMaterial = mats[1];
                }
                else if (mats != null && mats.Length == 1 && _topMaterial == null)
                {
                    _topMaterial = mats[0];
                }
            }
            _propBlock = new MaterialPropertyBlock();
            _originalLocalPos = transform.localPosition;
        }

        /// <summary>
        /// 타일 기본 설정 초기화 (윗면 머티리얼과 측면/밑면 머티리얼 지원)
        /// </summary>
        public void Initialize(Vector2Int coord, bool isDark, float height = 0f, TileType initialType = TileType.Normal, Material topMat = null, Material sideMat = null)
        {
            if (_meshRenderer == null) _meshRenderer = GetComponentInChildren<MeshRenderer>();
            if (_boxCollider == null) _boxCollider = GetComponentInChildren<BoxCollider>();

            _coord = coord;
            _isDarkSquare = isDark;
            _height = height;
            _isDestroyed = false;
            _originalLocalPos = transform.localPosition;

            if (topMat != null) _topMaterial = topMat;
            if (sideMat != null) _sideMaterial = sideMat;

            SetTileType(initialType);
        }

        private void UpdateVisualsForType()
        {
            if (_meshRenderer == null) return;

            Material currentSide = _sideMaterial;
            Material currentTop = _topMaterial;

            switch (_type)
            {
                case TileType.Water:
                    if (GridManager.Instance != null && GridManager.Instance.WaterMaterial != null)
                    {
                        currentTop = GridManager.Instance.WaterMaterial;
                    }
                    _baseColor = Color.white;
                    break;

                case TileType.Bridge:
                    if (GridManager.Instance != null && GridManager.Instance.BridgeMaterial != null)
                    {
                        currentTop = GridManager.Instance.BridgeMaterial;
                        currentSide = GridManager.Instance.BridgeMaterial;
                    }
                    _baseColor = Color.white;
                    break;

                case TileType.Obstacle:
                    if (GridManager.Instance != null && GridManager.Instance.ObstacleMaterial != null)
                    {
                        currentTop = GridManager.Instance.ObstacleMaterial;
                    }
                    _baseColor = Color.white;
                    break;

                case TileType.Normal:
                case TileType.HalfBlock:
                default:
                    currentTop = _topMaterial;
                    currentSide = _sideMaterial;
                    _baseColor = Color.white;
                    break;
            }

            // 2-Submesh 큐브 지원: Submesh 0(측면/밑면) = 흙/목재, Submesh 1(윗면) = 잔디/물/목재
            MeshFilter mf = GetComponentInChildren<MeshFilter>();
            if (currentSide != null && currentTop != null && (mf == null || mf.sharedMesh == null || mf.sharedMesh.subMeshCount >= 2))
            {
                _meshRenderer.sharedMaterials = new Material[] { currentSide, currentTop };
            }
            else if (currentTop != null)
            {
                _meshRenderer.sharedMaterial = currentTop;
            }
        }

        /// <summary>
        /// 타일 속성 변경 (일반, 물, 나무 다리, 장애물 등)
        /// 피드백 명세: 물 높이는 0.6(수로), 다리 높이는 0.3(목재 슬랩) 반영
        /// </summary>
        public void SetTileType(TileType newType)
        {
            _type = newType;
            _isHalfBlock = (_type == TileType.HalfBlock);

            float baseThickness = GridManager.Instance != null ? GridManager.Instance.TileThickness : 1.0f;
            float tileSize = GridManager.Instance != null ? GridManager.Instance.TileSize : 1.0f;

            switch (_type)
            {
                case TileType.Water:
                    // 피드백: 일반 칸(1.0) 기준 물 높이는 0.6 (수면이 0.4 낮게 파여 있는 수로 연출)
                    transform.localScale = new Vector3(tileSize, 0.6f, tileSize);
                    transform.localPosition = new Vector3(_originalLocalPos.x, _height - 0.2f, _originalLocalPos.z);
                    break;

                case TileType.Bridge:
                    // 피드백: 나무 다리 높이(두께)는 0.3 (수로 위에 걸쳐 양쪽 잔디 둑을 잇는 목재 다리)
                    transform.localScale = new Vector3(tileSize, 0.3f, tileSize);
                    transform.localPosition = new Vector3(_originalLocalPos.x, _height + 0.35f, _originalLocalPos.z);
                    break;

                case TileType.HalfBlock:
                    // 반칸 높이 디딤돌 (두께 0.5)
                    transform.localScale = new Vector3(tileSize, 0.5f, tileSize);
                    transform.localPosition = new Vector3(_originalLocalPos.x, _height + 0.25f, _originalLocalPos.z);
                    break;

                case TileType.Normal:
                case TileType.Obstacle:
                default:
                    transform.localScale = new Vector3(tileSize, baseThickness, tileSize);
                    transform.localPosition = new Vector3(_originalLocalPos.x, _height, _originalLocalPos.z);
                    break;
            }

            UpdateVisualsForType();
            ApplyHighlightColor();
        }

        /// <summary>
        /// 타일 높이(단차) 설정
        /// </summary>
        public void SetHeight(float newHeight, bool updatePosition = true)
        {
            _height = newHeight;
            if (updatePosition)
            {
                Vector3 pos = transform.position;
                pos.y = _height;
                transform.position = pos;
                _originalLocalPos = transform.localPosition;
            }
        }

        /// <summary>
        /// 일반 블록의 절반 높이의 블록 (반칸 디딤돌/계단 블록) 설정
        /// </summary>
        /// <param name="isHalf">반칸 블록 활성화 여부</param>
        /// <param name="surfaceHeight">상단 표면 목표 높이 (기본: 평지 0.5보다 반칸 높은 1.0f)</param>
        public void SetHalfBlock(bool isHalf, float surfaceHeight = 1.0f)
        {
            _isHalfBlock = isHalf;
            float baseThickness = GridManager.Instance != null ? GridManager.Instance.TileThickness : 1.0f;
            float tileSize = GridManager.Instance != null ? GridManager.Instance.TileSize : 1.0f;

            if (_isHalfBlock)
            {
                _type = TileType.HalfBlock;
                float halfThick = baseThickness * 0.5f;
                transform.localScale = new Vector3(tileSize, halfThick, tileSize);

                Vector3 pos = transform.position;
                pos.y = surfaceHeight - halfThick * 0.5f;
                transform.position = pos;
                _height = surfaceHeight - 0.5f;
                _originalLocalPos = transform.localPosition;
            }
            else
            {
                if (_type == TileType.HalfBlock) _type = TileType.Normal;
                transform.localScale = new Vector3(tileSize, baseThickness, tileSize);

                Vector3 pos = transform.position;
                pos.y = _height;
                transform.position = pos;
                _originalLocalPos = transform.localPosition;
            }

            UpdateVisualsForType();
            ApplyHighlightColor();
        }

        /// <summary>
        /// 하이라이트 상태 적용
        /// </summary>
        public void SetHighlight(TileHighlightState state)
        {
            if (_isDestroyed) return;
            _currentHighlight = state;
            ApplyHighlightColor();
        }

        private void ApplyHighlightColor()
        {
            if (_meshRenderer == null) return;
            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();

            Color targetColor = _baseColor;

            switch (_currentHighlight)
            {
                case TileHighlightState.Hovered:
                    targetColor = Color.Lerp(_baseColor, ColorHover, 0.45f);
                    break;
                case TileHighlightState.Selected:
                    targetColor = Color.Lerp(_baseColor, ColorSelected, 0.65f);
                    break;
                case TileHighlightState.Movable:
                    targetColor = Color.Lerp(_baseColor, ColorMovable, 0.55f);
                    break;
                case TileHighlightState.Attackable:
                    targetColor = Color.Lerp(_baseColor, ColorAttackable, 0.65f);
                    break;
                case TileHighlightState.Spawnable:
                    targetColor = Color.Lerp(_baseColor, ColorSpawnable, 0.55f);
                    break;
                case TileHighlightState.None:
                default:
                    targetColor = _baseColor;
                    break;
            }

            _meshRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor("_Color", targetColor);
            _propBlock.SetColor("_BaseColor", targetColor); // URP Lit 지원
            _meshRenderer.SetPropertyBlock(_propBlock);
        }

        /// <summary>
        /// 기획서 명세: "필드는 카드 효과에 따라 파괴되거나 생성될 수 있다.
        /// 필드가 파괴되었을 때 필드 위에 존재하던 유닛은 지면에 착지할 때까지 추락하며
        /// 착지할 때 떨어진 높이에 비례한 피해를 입는다."
        /// </summary>
        public void DestroyTile(bool animate = true, float fallFloorY = -5f)
        {
            if (_isDestroyed) return;
            _isDestroyed = true;
            _currentHighlight = TileHighlightState.None;

            if (_boxCollider != null) _boxCollider.enabled = false;

            // 유닛 추락 처리
            if (_occupyingUnit != null)
            {
                HandleUnitFall(fallFloorY);
            }

            if (animate && gameObject.activeInHierarchy)
            {
                if (_animCoroutine != null) StopCoroutine(_animCoroutine);
                _animCoroutine = StartCoroutine(AnimateFallAndDeactivate());
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 파괴된 타일 복구/생성
        /// </summary>
        public void RestoreTile(TileType type = TileType.Normal, bool animate = true)
        {
            if (!_isDestroyed) return;
            _isDestroyed = false;
            if (type == TileType.HalfBlock)
            {
                SetHalfBlock(true);
            }
            else
            {
                _isHalfBlock = false;
                _type = type;
                float baseThickness = GridManager.Instance != null ? GridManager.Instance.TileThickness : 1.0f;
                float tileSize = GridManager.Instance != null ? GridManager.Instance.TileSize : 1.0f;
                transform.localScale = new Vector3(tileSize, baseThickness, tileSize);
            }
            gameObject.SetActive(true);

            if (_boxCollider != null) _boxCollider.enabled = true;

            UpdateVisualsForType();
            ApplyHighlightColor();

            if (animate && gameObject.activeInHierarchy)
            {
                if (_animCoroutine != null) StopCoroutine(_animCoroutine);
                _animCoroutine = StartCoroutine(AnimateRise());
            }
            else
            {
                transform.localPosition = _originalLocalPos;
            }
        }

        private void HandleUnitFall(float floorY)
        {
            if (_occupyingUnit != null)
            {
                var unit = _occupyingUnit.GetComponent<ChessMonsters.Unit.Unit>();
                if (unit != null)
                {
                    unit.Fall(floorY);
                }
                else
                {
                    float fallDistance = Mathf.Max(0f, transform.position.y - floorY);
                    int damage = Mathf.RoundToInt(fallDistance * 2f);
                    Debug.Log($"[Tile {_coord}] 유닛 추락 발생! 낙하 거리: {fallDistance:F1}m, 낙하 피해량: {damage}");
                }
                _occupyingUnit = null;
            }
        }

        private IEnumerator AnimateFallAndDeactivate()
        {
            Vector3 startPos = transform.localPosition;
            Vector3 endPos = startPos + Vector3.down * 4f;
            float duration = 0.45f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // 가속 추락 이징 (EaseInQuad)
                float easeT = t * t;
                transform.localPosition = Vector3.Lerp(startPos, endPos, easeT);
                yield return null;
            }

            gameObject.SetActive(false);
            transform.localPosition = startPos; // 다음 복구를 위해 원위치
        }

        private IEnumerator AnimateRise()
        {
            Vector3 endPos = _originalLocalPos;
            Vector3 startPos = endPos + Vector3.down * 4f;
            transform.localPosition = startPos;

            float duration = 0.35f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // 반동 이징 (EaseOutBack)
                float c1 = 1.70158f;
                float c3 = c1 + 1f;
                float easeT = 1f + c3 * Mathf.Pow(t - 1f, 3) + c1 * Mathf.Pow(t - 1f, 2);
                transform.localPosition = Vector3.LerpUnclamped(startPos, endPos, easeT);
                yield return null;
            }

            transform.localPosition = endPos;
        }

        public void SetOccupyingUnit(GameObject unit)
        {
            _occupyingUnit = unit;
        }

        // 마우스 인터랙션 이벤트 핸들러
        public void HandleMouseEnter()
        {
            if (_isDestroyed) return;
            OnHoverEnter?.Invoke(this);
        }

        public void HandleMouseExit()
        {
            if (_isDestroyed) return;
            OnHoverExit?.Invoke(this);
        }

        public void HandleMouseClick()
        {
            if (_isDestroyed) return;
            OnClicked?.Invoke(this);
        }
    }
}
