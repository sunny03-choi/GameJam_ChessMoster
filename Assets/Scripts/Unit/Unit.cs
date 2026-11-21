using System;
using System.Collections;
using UnityEngine;
using ChessMonsters.Grid;

namespace ChessMonsters.Unit
{
    public enum UnitTeam
    {
        Player, // 아군 (Blue / White)
        Enemy   // 적군 (Red / Dark)
    }

    /// <summary>
    /// 체스판 필드 위에 소환되어 이동, 공격, 추락 등의 동작을 수행하는 유닛 컴포넌트
    /// 3D 공간에서 2D 스프라이트 빌보드(SpriteBillboard)를 기반으로 렌더링됩니다.
    /// 기동력(Mobility) 시스템과 공격/전투(Combat) 연출을 지원합니다.
    /// </summary>
    [SelectionBase]
    public class Unit : MonoBehaviour
    {
        [Header("Unit Status")]
        [SerializeField] private string _unitName = "Pawn";
        [SerializeField] private UnitTeam _team = UnitTeam.Player;
        [SerializeField] private int _maxHp = 10;
        [SerializeField] private int _currentHp = 10;
        [SerializeField] private int _attackPower = 3;
        [SerializeField] private int _moveRange = 3;
        [SerializeField] private int _maxMobility = 3;
        [SerializeField] private int _currentMobility = 3;
        [SerializeField] private int _attackRange = 2;
        [SerializeField] private bool _isFlying = false;

        [Header("Grid Binding")]
        [SerializeField] private Tile _currentTile;

        [Header("Visual Components")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private SpriteRenderer _shadowRenderer;
        [SerializeField] private GameObject _selectionIndicator;
        [SerializeField] private SpriteBillboard _billboard;

        [Header("Floating HUD Components")]
        [SerializeField] private GameObject _mobilityBadgeObj;
        [SerializeField] private TextMesh _mobilityTextMesh;
        [SerializeField] private SpriteRenderer _mobilityBadgeRenderer;

        private Coroutine _moveCoroutine;
        private Coroutine _damageFlashCoroutine;
        private bool _isMoving = false;
        private bool _isDead = false;

        public string UnitName { get => _unitName; set => _unitName = value; }
        public UnitTeam Team
        {
            get => _team;
            set
            {
                _team = value;
                UpdateTeamVisuals();
            }
        }
        public int MaxHp => _maxHp;
        public int CurrentHp => _currentHp;
        public int AttackPower => _attackPower;
        public int MoveRange => _moveRange;
        public int MaxMobility => _maxMobility;
        public int CurrentMobility => _currentMobility;
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
            if (_currentMobility <= 0) _currentMobility = _maxMobility;

            EnsureMobilityBadge();
            UpdateTeamVisuals();
        }

        private void EnsureMobilityBadge()
        {
            if (_mobilityBadgeObj == null)
            {
                // 머리 위 기동력 뱃지 동적 생성
                _mobilityBadgeObj = new GameObject("MobilityBadge");
                _mobilityBadgeObj.transform.SetParent(transform, false);
                _mobilityBadgeObj.transform.localPosition = new Vector3(0f, 1.65f, 0f);

                // 카메라를 향하는 빌보드
                SpriteBillboard bb = _mobilityBadgeObj.AddComponent<SpriteBillboard>();
                bb.Mode = SpriteBillboard.BillboardMode.CameraAligned;

                // 뱃지 배경 스프라이트
                _mobilityBadgeRenderer = _mobilityBadgeObj.AddComponent<SpriteRenderer>();
                Sprite badgeSprite = null;
#if UNITY_EDITOR
                badgeSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/Badge_Mobility_Bubble.png");
#endif
                if (badgeSprite != null)
                {
                    _mobilityBadgeRenderer.sprite = badgeSprite;
                }
                _mobilityBadgeRenderer.sortingOrder = 10;
                _mobilityBadgeObj.transform.localScale = new Vector3(0.55f, 0.55f, 1f);

                // 뱃지 텍스트 (숫자 표시)
                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(_mobilityBadgeObj.transform, false);
                textObj.transform.localPosition = new Vector3(0.22f, 0.02f, -0.02f);
                _mobilityTextMesh = textObj.AddComponent<TextMesh>();
                _mobilityTextMesh.characterSize = 0.16f;
                _mobilityTextMesh.fontSize = 32;
                _mobilityTextMesh.fontStyle = FontStyle.Bold;
                _mobilityTextMesh.alignment = TextAlignment.Center;
                _mobilityTextMesh.anchor = TextAnchor.MiddleCenter;
                _mobilityTextMesh.color = new Color(0.2f, 0.12f, 0.08f, 1f); // 짙은 초콜릿색 텍스트
                _mobilityTextMesh.text = $"{_currentMobility}";

                _mobilityBadgeObj.SetActive(false);
            }
        }

        public void UpdateTeamVisuals()
        {
            if (_spriteRenderer == null) return;

#if UNITY_EDITOR
            if (_team == UnitTeam.Enemy)
            {
                Sprite enemySprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Units/Sprite_Unit_Pawn_Red.png");
                if (enemySprite != null)
                {
                    _spriteRenderer.sprite = enemySprite;
                }
                else
                {
                    _spriteRenderer.color = new Color(0.95f, 0.35f, 0.35f, 1f);
                }
            }
            else
            {
                Sprite playerSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Units/Sprite_Unit_Pawn_White.png");
                if (playerSprite != null)
                {
                    _spriteRenderer.sprite = playerSprite;
                }
                _spriteRenderer.color = Color.white;
            }
#endif
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
        /// 유닛의 기동력 리셋 (새 턴 시작 시 호출)
        /// </summary>
        public void ResetMobility()
        {
            _currentMobility = _maxMobility;
            UpdateMobilityBadge();
        }

        /// <summary>
        /// 기동력 1 소모
        /// </summary>
        public bool ConsumeMobility(int amount = 1)
        {
            if (_currentMobility >= amount)
            {
                _currentMobility -= amount;
                UpdateMobilityBadge();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 머리 위 기동력 뱃지 표시 여부 설정
        /// </summary>
        public void ShowMobilityBadge(bool show)
        {
            EnsureMobilityBadge();
            if (_mobilityBadgeObj != null)
            {
                _mobilityBadgeObj.SetActive(show);
                if (show) UpdateMobilityBadge();
            }
        }

        public void UpdateMobilityBadge()
        {
            if (_mobilityTextMesh != null)
            {
                _mobilityTextMesh.text = $"{_currentMobility}";
                if (_currentMobility <= 0)
                {
                    _mobilityTextMesh.color = new Color(0.6f, 0.2f, 0.2f, 1f);
                }
                else
                {
                    _mobilityTextMesh.color = new Color(0.2f, 0.12f, 0.08f, 1f);
                }
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

            float duration = 0.32f;
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
        /// 적 유닛을 향해 돌진 타격하는 공격 애니메이션 실행
        /// </summary>
        public void ExecuteAttack(Unit targetUnit, System.Action onComplete = null)
        {
            if (targetUnit == null || _isMoving || _isDead) return;
            StartCoroutine(AnimateAttack(targetUnit, onComplete));
        }

        private IEnumerator AnimateAttack(Unit targetUnit, System.Action onComplete)
        {
            _isMoving = true;
            Vector3 originalPos = transform.position;
            Vector3 targetPos = targetUnit.transform.position;
            Vector3 lungePos = Vector3.Lerp(originalPos, targetPos, 0.55f);

            // 1. 적을 향해 빠른 돌진 (Lunge)
            float elapsed = 0f;
            float lungeDuration = 0.14f;
            while (elapsed < lungeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / lungeDuration);
                transform.position = Vector3.Lerp(originalPos, lungePos, Mathf.Sin(t * Mathf.PI * 0.5f));
                yield return null;
            }

            // 2. 타격 데미지 적용
            targetUnit.TakeDamage(_attackPower);
            yield return new WaitForSeconds(0.12f);

            // 3. 원래 자리로 복귀
            elapsed = 0f;
            float returnDuration = 0.16f;
            while (elapsed < returnDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / returnDuration);
                transform.position = Vector3.Lerp(lungePos, originalPos, t);
                yield return null;
            }

            transform.position = originalPos;
            _isMoving = false;
            onComplete?.Invoke();
        }

        /// <summary>
        /// 데미지 입기 및 피격 깜빡임 연출 + 플로팅 데미지 텍스트
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (_isDead) return;

            _currentHp = Mathf.Max(0, _currentHp - damage);
            Debug.Log($"[Unit {_unitName}] 피격! 데미지: {damage}, 남은 HP: {_currentHp}/{_maxHp}");

            SpawnFloatingDamageText(damage);

            if (_damageFlashCoroutine != null) StopCoroutine(_damageFlashCoroutine);
            _damageFlashCoroutine = StartCoroutine(AnimateDamageFlash());

            if (_currentHp <= 0)
            {
                Die();
            }
        }

        private void SpawnFloatingDamageText(int damage)
        {
            GameObject floatObj = new GameObject("FloatingDamageText");
            floatObj.transform.position = transform.position + Vector3.up * 1.4f;

            SpriteBillboard bb = floatObj.AddComponent<SpriteBillboard>();
            bb.Mode = SpriteBillboard.BillboardMode.CameraAligned;

            TextMesh tm = floatObj.AddComponent<TextMesh>();
            tm.characterSize = 0.22f;
            tm.fontSize = 36;
            tm.fontStyle = FontStyle.Bold;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = new Color(1.0f, 0.2f, 0.2f, 1f); // 강렬한 레드
            tm.text = $"-{damage} HP";

            StartCoroutine(AnimateFloatingText(floatObj, tm));
        }

        private IEnumerator AnimateFloatingText(GameObject textObj, TextMesh textMesh)
        {
            float duration = 0.85f;
            float elapsed = 0f;
            Vector3 startPos = textObj.transform.position;
            Vector3 endPos = startPos + Vector3.up * 0.9f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                if (textObj != null)
                {
                    textObj.transform.position = Vector3.Lerp(startPos, endPos, t);
                    if (textMesh != null)
                    {
                        Color c = textMesh.color;
                        c.a = Mathf.Clamp01(1.2f - t * 1.2f);
                        textMesh.color = c;
                    }
                }
                yield return null;
            }

            if (textObj != null) Destroy(textObj);
        }

        private IEnumerator AnimateDamageFlash()
        {
            if (_spriteRenderer == null) yield break;

            Color originalColor = _spriteRenderer.color;
            _spriteRenderer.color = new Color(1.0f, 0.2f, 0.2f, originalColor.a);
            yield return new WaitForSeconds(0.12f);
            _spriteRenderer.color = originalColor;
        }

        /// <summary>
        /// 낙하 피해
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
            float duration = 0.35f;
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
        /// 타일의 윗면 정중앙 좌표 계산
        /// </summary>
        public static Vector3 GetTopSurfaceOfTile(Tile tile)
        {
            if (tile == null) return Vector3.zero;
            return tile.TopSurfacePosition;
        }
    }
}
