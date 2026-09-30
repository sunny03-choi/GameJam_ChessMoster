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

        // 색상 프리셋 (에디터/런타임에서 자동 초기화)
        private Color _baseColor;
        private static readonly Color ColorLight = new Color(0.92f, 0.90f, 0.85f);     // 체스판 밝은 칸
        private static readonly Color ColorDark = new Color(0.45f, 0.40f, 0.35f);      // 체스판 어두운 칸
        private static readonly Color ColorWater = new Color(0.20f, 0.55f, 0.85f);     // 물 칸
        private static readonly Color ColorObstacle = new Color(0.30f, 0.30f, 0.30f);  // 장애물 칸
        private static readonly Color ColorHover = new Color(1.0f, 0.95f, 0.5f);       // 마우스 호버 (연노랑)
        private static readonly Color ColorSelected = new Color(1.0f, 0.8f, 0.0f);     // 선택됨 (골드)
        private static readonly Color ColorMovable = new Color(0.3f, 0.85f, 1.0f);     // 이동 가능 범위 (하늘색)
        private static readonly Color ColorAttackable = new Color(1.0f, 0.3f, 0.3f);   // 공격 사거리 (빨강)
        private static readonly Color ColorSpawnable = new Color(0.4f, 1.0f, 0.5f);    // 소환 가능 (초록)

        private MaterialPropertyBlock _propBlock;
        private TileHighlightState _currentHighlight = TileHighlightState.None;
        private Coroutine _animCoroutine;
        private Vector3 _originalLocalPos;

        public Vector2Int Coord => _coord;
        public TileType Type => _type;
        public float Height => _height;
        public bool IsDarkSquare => _isDarkSquare;
        public bool IsDestroyed => _isDestroyed;
        public bool IsPassable => !_isDestroyed && _type != TileType.Water && _type != TileType.Obstacle && _occupyingUnit == null;
        public GameObject OccupyingUnit => _occupyingUnit;

        public event Action<Tile> OnClicked;
        public event Action<Tile> OnHoverEnter;
        public event Action<Tile> OnHoverExit;

        private void Awake()
        {
            if (_meshRenderer == null) _meshRenderer = GetComponentInChildren<MeshRenderer>();
            if (_boxCollider == null) _boxCollider = GetComponentInChildren<BoxCollider>();
            _propBlock = new MaterialPropertyBlock();
            _originalLocalPos = transform.localPosition;
        }

        /// <summary>
        /// 타일 기본 설정 초기화
        /// </summary>
        public void Initialize(Vector2Int coord, bool isDark, float height = 0f, TileType initialType = TileType.Normal)
        {
            _coord = coord;
            _isDarkSquare = isDark;
            _height = height;
            _type = initialType;
            _isDestroyed = false;
            _originalLocalPos = transform.localPosition;

            UpdateBaseColor();
            ApplyHighlightColor();
        }

        private void UpdateBaseColor()
        {
            switch (_type)
            {
                case TileType.Water:
                    _baseColor = ColorWater;
                    break;
                case TileType.Obstacle:
                    _baseColor = ColorObstacle;
                    break;
                case TileType.Normal:
                default:
                    _baseColor = _isDarkSquare ? ColorDark : ColorLight;
                    break;
            }
        }

        /// <summary>
        /// 타일 속성 변경 (일반, 물, 장애물 등)
        /// </summary>
        public void SetTileType(TileType newType)
        {
            _type = newType;
            UpdateBaseColor();
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
                    targetColor = Color.Lerp(_baseColor, ColorHover, 0.6f);
                    break;
                case TileHighlightState.Selected:
                    targetColor = ColorSelected;
                    break;
                case TileHighlightState.Movable:
                    targetColor = Color.Lerp(_baseColor, ColorMovable, 0.65f);
                    break;
                case TileHighlightState.Attackable:
                    targetColor = Color.Lerp(_baseColor, ColorAttackable, 0.7f);
                    break;
                case TileHighlightState.Spawnable:
                    targetColor = Color.Lerp(_baseColor, ColorSpawnable, 0.65f);
                    break;
            }

            _meshRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor("_Color", targetColor);
            _propBlock.SetColor("_BaseColor", targetColor); // URP 호환
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
            _type = type;
            gameObject.SetActive(true);

            if (_boxCollider != null) _boxCollider.enabled = true;

            UpdateBaseColor();
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
            float fallDistance = Mathf.Max(0f, transform.position.y - floorY);
            // 기획서: 낙하 높이에 비례한 피해
            int damage = Mathf.RoundToInt(fallDistance * 2f);
            Debug.Log($"[Tile {_coord}] 유닛 추락 발생! 낙하 거리: {fallDistance:F1}m, 낙하 피해량: {damage}");

            // 추락 연출 또는 유닛 파괴 로직 연결 지점
            // (유닛 스크립트 연결 시 unit.TakeDamage(damage) 호출 가능)
            _occupyingUnit = null;
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
