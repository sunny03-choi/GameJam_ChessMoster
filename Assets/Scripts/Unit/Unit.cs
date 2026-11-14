using System.Collections;
using UnityEngine;
using ChessMonsters.Grid;

namespace ChessMonsters.Unit
{
    /// <summary>
    /// 체스판 필드 위에 소환되어 이동, 공격, 추락 등의 동작을 수행하는 유닛 컴포넌트
    /// 3D 공간에서 2D 스프라이트 빌보드(SpriteBillboard)를 기반으로 렌더링됩니다.
    /// </summary>
    [SelectionBase]
    public class Unit : MonoBehaviour
    {
        [Header("Unit Status")]
        [SerializeField] private string _unitName = "Pawn";
        [SerializeField] private int _maxHp = 10;
        [SerializeField] private int _currentHp = 10;
        [SerializeField] private int _attackPower = 3;
        [SerializeField] private int _moveRange = 3;
        [SerializeField] private int _attackRange = 2;
        [SerializeField] private bool _isFlying = false;

        [Header("Grid Binding")]
        [SerializeField] private Tile _currentTile;

        [Header("Visual Components")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private SpriteRenderer _shadowRenderer;
        [SerializeField] private GameObject _selectionIndicator;
        [SerializeField] private SpriteBillboard _billboard;

        private Coroutine _moveCoroutine;
        private Coroutine _damageFlashCoroutine;
        private bool _isMoving = false;
        private bool _isDead = false;

        public string UnitName => _unitName;
        public int MaxHp => _maxHp;
        public int CurrentHp => _currentHp;
        public int AttackPower => _attackPower;
        public int MoveRange => _moveRange;
        public int AttackRange => _attackRange;
        public bool IsFlying => _isFlying;
        public Tile CurrentTile => _currentTile;
        public Vector2Int Coord => _currentTile != null ? _currentTile.Coord : Vector2Int.zero;
        public bool IsMoving => _isMoving;
        public bool IsDead => _isDead;

        private void Awake()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
            if (_billboard == null)
            {
                _billboard = GetComponentInChildren<SpriteBillboard>();
            }
            if (_currentHp <= 0) _currentHp = _maxHp;
        }

        /// <summary>
        /// 타일 위에 유닛을 처음 소환하거나 즉시 배치
        /// </summary>
        public void PlaceOnTile(Tile tile)
        {
            if (_currentTile != null && _currentTile.OccupyingUnit == gameObject)
            {
                _currentTile.SetOccupyingUnit(null);
            }

            _currentTile = tile;
            if (tile != null)
            {
                tile.SetOccupyingUnit(gameObject);
                transform.position = GetTopSurfaceOfTile(tile);
            }
        }

        /// <summary>
        /// 목적지 타일로 부드러운 포물선 점프(호핑) 이동
        /// </summary>
        public void MoveToTile(Tile targetTile, System.Action onComplete = null)
        {
            if (targetTile == null || _isMoving || _isDead) return;

            if (_currentTile != null && _currentTile.OccupyingUnit == gameObject)
            {
                _currentTile.SetOccupyingUnit(null);
            }

            _currentTile = targetTile;
            targetTile.SetOccupyingUnit(gameObject);

            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
            _moveCoroutine = StartCoroutine(AnimateHopMove(targetTile, onComplete));
        }

        private IEnumerator AnimateHopMove(Tile targetTile, System.Action onComplete)
        {
            _isMoving = true;
            Vector3 startPos = transform.position;
            Vector3 endPos = GetTopSurfaceOfTile(targetTile);

            float duration = 0.35f;
            float elapsed = 0f;
            float jumpHeight = 0.6f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // 수평 이동 보간
                Vector3 current = Vector3.Lerp(startPos, endPos, t);

                // 수직 포물선 점프 (호핑 효과)
                float arc = Mathf.Sin(t * Mathf.PI) * jumpHeight;
                current.y += arc;

                transform.position = current;
                yield return null;
            }

            transform.position = endPos;
            _isMoving = false;
            onComplete?.Invoke();
        }

        /// <summary>
        /// 데미지 입기 및 피격 깜빡임 연출
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (_isDead) return;

            _currentHp = Mathf.Max(0, _currentHp - damage);
            Debug.Log($"[Unit {_unitName}] 피격! 데미지: {damage}, 남은 HP: {_currentHp}/{_maxHp}");

            if (_damageFlashCoroutine != null) StopCoroutine(_damageFlashCoroutine);
            _damageFlashCoroutine = StartCoroutine(AnimateDamageFlash());

            if (_currentHp <= 0)
            {
                Die();
            }
        }

        private IEnumerator AnimateDamageFlash()
        {
            if (_spriteRenderer == null) yield break;

            Color originalColor = _spriteRenderer.color;
            _spriteRenderer.color = new Color(1.0f, 0.3f, 0.3f, originalColor.a);
            yield return new WaitForSeconds(0.12f);
            _spriteRenderer.color = originalColor;
        }

        /// <summary>
        /// 기획서 명세: "필드가 파괴되었을 때 필드 위에 존재하던 유닛은 지면에 착지할 때까지 추락하며
        /// 착지할 때 떨어진 높이에 비례한 피해를 입는다."
        /// </summary>
        public void Fall(float floorY)
        {
            if (_isDead) return;

            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
            StartCoroutine(AnimateFall(floorY));
        }

        private IEnumerator AnimateFall(float floorY)
        {
            _isMoving = true;
            Vector3 startPos = transform.position;
            float fallDistance = Mathf.Max(0f, startPos.y - floorY);
            int damage = Mathf.RoundToInt(fallDistance * 2f);

            Vector3 endPos = new Vector3(startPos.x, floorY, startPos.z);
            float duration = 0.5f;
            float elapsed = 0f;

            // 가속 추락
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float easeInT = t * t;
                transform.position = Vector3.Lerp(startPos, endPos, easeInT);
                yield return null;
            }

            transform.position = endPos;
            _isMoving = false;

            TakeDamage(damage);
        }

        public void SetSelected(bool isSelected)
        {
            if (_selectionIndicator != null)
            {
                _selectionIndicator.SetActive(isSelected);
            }
            else if (_spriteRenderer != null)
            {
                _spriteRenderer.color = isSelected ? new Color(1.0f, 0.95f, 0.4f, 1f) : Color.white;
            }
        }

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;

            if (_currentTile != null && _currentTile.OccupyingUnit == gameObject)
            {
                _currentTile.SetOccupyingUnit(null);
            }

            Debug.Log($"[Unit {_unitName}] 사망했습니다.");
            StartCoroutine(AnimateDeathAndDestroy());
        }

        private IEnumerator AnimateDeathAndDestroy()
        {
            float duration = 0.3f;
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                if (_spriteRenderer != null)
                {
                    Color c = _spriteRenderer.color;
                    c.a = 1f - t;
                    _spriteRenderer.color = c;
                }
                yield return null;
            }

            Destroy(gameObject);
        }

        /// <summary>
        /// 타일의 윗면 정중앙 좌표 계산 (단차 및 반칸 블록 높이 완전 반영)
        /// </summary>
        public static Vector3 GetTopSurfaceOfTile(Tile tile)
        {
            if (tile == null) return Vector3.zero;
            return tile.TopSurfacePosition;
        }
    }
}
