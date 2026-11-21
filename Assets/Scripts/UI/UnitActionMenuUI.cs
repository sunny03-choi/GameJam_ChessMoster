using System;
using System.Collections;
using UnityEngine;
using ChessMonsters.Unit;

namespace ChessMonsters.UI
{
    public enum UnitActionMode
    {
        None,
        ChoiceMenu, // 3가지 선택지 대기 상태 (1. 공격, 2. 이동, 3. 효과 발동)
        MoveMode,   // 이동 모드 (+1칸 이동, 기동력 표시 및 차감)
        AttackMode  // 공격 모드 (사거리 표시, 적 유닛 클릭 시 전투 진입)
    }

    /// <summary>
    /// 쿠키런 킹덤 스타일의 3대 선택지 (1. 공격, 2. 이동, 3. 효과 발동) UI 컨트롤러
    /// - 유닛 선택 시 머리 위에 도톰하고 귀여운 젤리/알약 형태의 3개 액션 버튼 팝업
    /// - 1: 공격 (사거리 표시 및 적 선택 시 전투 진입)
    /// - 2: 이동 (유닛 위 남은 기동력 표시 및 십자 1칸 범위 이동 시 기동력 1 감소)
    /// - 3: 효과 발동 (쿠키런 스킬 토스트 연출)
    /// - 마우스 클릭 및 숫자키 [1], [2], [3], [ESC] 완전 지원
    /// - "전투 진입 (BATTLE START!)" 다이내믹 컷인 배너 연출 내장
    /// </summary>
    public class UnitActionMenuUI : MonoBehaviour
    {
        public static UnitActionMenuUI Instance { get; private set; }

        [Header("UI Textures")]
        [SerializeField] private Texture2D _attackBtnTex;
        [SerializeField] private Texture2D _moveBtnTex;
        [SerializeField] private Texture2D _skillBtnTex;
        [SerializeField] private Texture2D _battleBannerTex;
        [SerializeField] private Texture2D _mobilityBadgeTex;

        [Header("State")]
        private Unit.Unit _targetUnit;
        private UnitActionMode _currentMode = UnitActionMode.None;
        private Camera _mainCamera;

        // "전투 진입" 컷인 배너 연출용
        private bool _isShowingBattleBanner = false;
        private float _battleBannerTimer = 0f;
        private string _battleAttackerName = "";
        private string _battleDefenderName = "";
        private int _battleDamage = 0;

        // 스킬 토스트 연출용
        private string _skillToastMessage = "";
        private float _skillToastTimer = 0f;

        // 액션 콜백
        public event Action<Unit.Unit> OnAttackSelected;
        public event Action<Unit.Unit> OnMoveSelected;
        public event Action<Unit.Unit> OnSkillSelected;
        public event Action OnCancelSelected;

        public UnitActionMode CurrentMode => _currentMode;
        public Unit.Unit TargetUnit => _targetUnit;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            LoadTexturesIfNull();
        }

        private void Start()
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null) _mainCamera = FindFirstObjectByType<Camera>();
        }

        private void LoadTexturesIfNull()
        {
#if UNITY_EDITOR
            if (_attackBtnTex == null)
                _attackBtnTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/UI/Btn_Cookie_Attack.png");
            if (_moveBtnTex == null)
                _moveBtnTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/UI/Btn_Cookie_Move.png");
            if (_skillBtnTex == null)
                _skillBtnTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/UI/Btn_Cookie_Skill.png");
            if (_battleBannerTex == null)
                _battleBannerTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/UI/Banner_Battle_Start.png");
            if (_mobilityBadgeTex == null)
                _mobilityBadgeTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/UI/Badge_Mobility_Bubble.png");
#endif
        }

        private void Update()
        {
            // 배너 타이머
            if (_isShowingBattleBanner)
            {
                _battleBannerTimer -= Time.deltaTime;
                if (_battleBannerTimer <= 0f)
                {
                    _isShowingBattleBanner = false;
                }
            }

            // 스킬 토스트 타이머
            if (_skillToastTimer > 0f)
            {
                _skillToastTimer -= Time.deltaTime;
            }

            // 단축키 처리
            if (_targetUnit != null && !_targetUnit.IsMoving && !_targetUnit.IsDead)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
                {
                    SelectAttack();
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
                {
                    SelectMove();
                }
                else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
                {
                    SelectSkill();
                }
                else if (Input.GetKeyDown(KeyCode.Escape))
                {
                    CancelMenu();
                }
            }
        }

        /// <summary>
        /// 유닛 선택 시 쿠키런 3종 액션 메뉴 열기
        /// </summary>
        public void OpenMenu(Unit.Unit unit)
        {
            _targetUnit = unit;
            _currentMode = UnitActionMode.ChoiceMenu;
            if (_targetUnit != null)
            {
                _targetUnit.ShowMobilityBadge(false);
            }
        }

        /// <summary>
        /// 메뉴 닫기 및 모드 초기화
        /// </summary>
        public void CloseMenu()
        {
            if (_targetUnit != null)
            {
                _targetUnit.ShowMobilityBadge(false);
            }
            _targetUnit = null;
            _currentMode = UnitActionMode.None;
        }

        public void SelectAttack()
        {
            if (_targetUnit == null) return;
            _currentMode = UnitActionMode.AttackMode;
            _targetUnit.ShowMobilityBadge(false);
            OnAttackSelected?.Invoke(_targetUnit);
            Debug.Log($"[ActionMenu] 유닛 {_targetUnit.UnitName}: 1. 공격 모드 진입 (사거리: {_targetUnit.AttackRange})");
        }

        public void SelectMove()
        {
            if (_targetUnit == null) return;
            _currentMode = UnitActionMode.MoveMode;
            _targetUnit.ShowMobilityBadge(true);
            OnMoveSelected?.Invoke(_targetUnit);
            Debug.Log($"[ActionMenu] 유닛 {_targetUnit.UnitName}: 2. 이동 모드 진입 (남은 기동력: {_targetUnit.CurrentMobility}/{_targetUnit.MaxMobility})");
        }

        public void SelectSkill()
        {
            if (_targetUnit == null) return;
            _skillToastMessage = $"✨ [{_targetUnit.UnitName}] 효과 발동! (스킬 쿨다운 중)";
            _skillToastTimer = 2.0f;
            OnSkillSelected?.Invoke(_targetUnit);
            Debug.Log($"[ActionMenu] 유닛 {_targetUnit.UnitName}: 3. 효과 발동!");
        }

        public void CancelMenu()
        {
            CloseMenu();
            OnCancelSelected?.Invoke();
        }

        /// <summary>
        /// "전투 진입 (BATTLE START)" 연출 발동
        /// </summary>
        public void TriggerBattleStartCutIn(Unit.Unit attacker, Unit.Unit defender)
        {
            _isShowingBattleBanner = true;
            _battleBannerTimer = 1.6f;
            _battleAttackerName = attacker != null ? attacker.UnitName : "Attacker";
            _battleDefenderName = defender != null ? defender.UnitName : "Defender";
            _battleDamage = attacker != null ? attacker.AttackPower : 3;
        }

        private void OnGUI()
        {
            // 1. 전투 진입 컷인 배너 연출
            DrawBattleStartBanner();

            // 2. 스킬 토스트 연출
            DrawSkillToast();

            // 3. 기동력 상단 HUD (이동 모드일 때)
            if (_targetUnit != null && _currentMode == UnitActionMode.MoveMode)
            {
                DrawMoveModeHUD();
            }

            // 4. 쿠키런 3종 선택지 메뉴 (ChoiceMenu 상태일 때)
            if (_targetUnit != null && _currentMode == UnitActionMode.ChoiceMenu && !_targetUnit.IsMoving && !_targetUnit.IsDead)
            {
                DrawCookieRunChoiceMenu();
            }

            // 5. 공격 모드 안내 가이드
            if (_targetUnit != null && _currentMode == UnitActionMode.AttackMode)
            {
                DrawAttackModeHUD();
            }
        }

        /// <summary>
        /// 선택된 유닛 머리 위 화면 위치에 쿠키런 킹덤 스타일 3종 선택지 렌더링
        /// </summary>
        private void DrawCookieRunChoiceMenu()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null || _targetUnit == null) return;

            Vector3 worldPos = _targetUnit.transform.position + Vector3.up * 1.6f;
            Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPos);

            if (screenPos.z < 0) return; // 카메라 뒤면 그리지 않음

            float guiX = screenPos.x;
            float guiY = Screen.height - screenPos.y;

            // 버튼 크기
            float btnW = 160f;
            float btnH = 55f;
            float spacing = 12f;
            float totalW = btnW * 3 + spacing * 2;

            // 중앙 정렬 및 화면 클램프 (화면 밖으로 나가지 않도록 보호)
            float startX = Mathf.Clamp(guiX - totalW * 0.5f, 20f, Screen.width - totalW - 20f);
            float startY = Mathf.Clamp(guiY - btnH - 15f, 50f, Screen.height - btnH - 80f);

            // 타이틀 미니 배지: [유닛 이름] 행동 선택
            Rect titleRect = new Rect(startX + totalW * 0.5f - 90f, startY - 32f, 180f, 26f);
            GUI.Box(titleRect, $"<b>{_targetUnit.UnitName}</b> 행동 선택");

            // 1. 공격 버튼 [1]
            Rect btn1Rect = new Rect(startX, startY, btnW, btnH);
            DrawCookieButton(btn1Rect, _attackBtnTex, "<b>공격</b> [1]", new Color(1.0f, 0.35f, 0.35f), () =>
            {
                SelectAttack();
            });

            // 2. 이동 버튼 [2]
            Rect btn2Rect = new Rect(startX + btnW + spacing, startY, btnW, btnH);
            DrawCookieButton(btn2Rect, _moveBtnTex, "<b>이동</b> [2]", new Color(0.35f, 0.95f, 0.55f), () =>
            {
                SelectMove();
            });

            // 3. 효과 발동 버튼 [3]
            Rect btn3Rect = new Rect(startX + (btnW + spacing) * 2, startY, btnW, btnH);
            DrawCookieButton(btn3Rect, _skillBtnTex, "<b>효과 발동</b> [3]", new Color(0.85f, 0.55f, 1.0f), () =>
            {
                SelectSkill();
            });
        }

        private void DrawCookieButton(Rect rect, Texture2D texture, string label, Color fallbackColor, Action onClick)
        {
            Vector2 mousePos = Event.current.mousePosition;
            bool isHovered = rect.Contains(mousePos);

            // 호버 시 살짝 팽창(Scale up) 연출
            Rect drawRect = isHovered ? new Rect(rect.x - 3, rect.y - 3, rect.width + 6, rect.height + 6) : rect;

            if (texture != null)
            {
                Color prevCol = GUI.color;
                GUI.color = isHovered ? new Color(1.15f, 1.15f, 1.15f, 1f) : Color.white;
                GUI.DrawTexture(drawRect, texture, ScaleMode.StretchToFill);
                GUI.color = prevCol;
            }
            else
            {
                // 텍스처 폴백
                Color prevCol = GUI.backgroundColor;
                GUI.backgroundColor = isHovered ? fallbackColor * 1.2f : fallbackColor;
                GUI.Box(drawRect, "");
                GUI.backgroundColor = prevCol;
            }

            // 라벨 텍스트
            GUIStyle textStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 17,
                fontStyle = FontStyle.Bold
            };
            textStyle.normal.textColor = Color.white;

            // 텍스트 그림자
            GUI.Label(new Rect(drawRect.x + 36, drawRect.y + 2, drawRect.width - 36, drawRect.height), label, new GUIStyle(textStyle) { normal = { textColor = new Color(0.12f, 0.08f, 0.06f, 0.9f) } });
            GUI.Label(new Rect(drawRect.x + 35, drawRect.y, drawRect.width - 35, drawRect.height), label, textStyle);

            // 클릭 감지
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(mousePos))
            {
                Event.current.Use();
                onClick?.Invoke();
            }
        }

        /// <summary>
        /// 이동 모드 안내 및 기동력 카운터 상단 표시
        /// </summary>
        private void DrawMoveModeHUD()
        {
            float w = 420f;
            float h = 64f;
            float x = (Screen.width - w) * 0.5f;
            float y = 20f;

            Rect hudRect = new Rect(x, y, w, h);
            GUI.Box(hudRect, "");

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = new Color(0.3f, 0.95f, 0.5f);

            GUIStyle subStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13
            };
            subStyle.normal.textColor = Color.white;

            GUI.Label(new Rect(x, y + 6, w, 26), $"👟 <b>이동 모드</b> (남은 기동력: <color=yellow>{_targetUnit.CurrentMobility}</color> / {_targetUnit.MaxMobility})", titleStyle);
            GUI.Label(new Rect(x, y + 32, w, 24), "앞/뒤/좌/우(+) 1칸 클릭 시 이동 및 기동력 1 감소 | 취소: ESC", subStyle);
        }

        /// <summary>
        /// 공격 모드 안내
        /// </summary>
        private void DrawAttackModeHUD()
        {
            float w = 420f;
            float h = 64f;
            float x = (Screen.width - w) * 0.5f;
            float y = 20f;

            Rect hudRect = new Rect(x, y, w, h);
            GUI.Box(hudRect, "");

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = new Color(1.0f, 0.4f, 0.4f);

            GUIStyle subStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13
            };
            subStyle.normal.textColor = Color.white;

            GUI.Label(new Rect(x, y + 6, w, 26), $"⚔️ <b>공격 모드</b> (사거리: {_targetUnit.AttackRange}칸)", titleStyle);
            GUI.Label(new Rect(x, y + 32, w, 24), "붉은 사거리 내 상대 유닛을 클릭하면 [전투 진입]! | 취소: ESC", subStyle);
        }

        /// <summary>
        /// "⚔️ 전투 진입 (BATTLE START!)" 다이내믹 컷인 배너
        /// </summary>
        private void DrawBattleStartBanner()
        {
            if (!_isShowingBattleBanner) return;

            float w = Mathf.Min(600f, Screen.width * 0.8f);
            float h = 130f;
            float x = (Screen.width - w) * 0.5f;
            float y = Screen.height * 0.35f;

            Rect bannerRect = new Rect(x, y, w, h);

            if (_battleBannerTex != null)
            {
                GUI.DrawTexture(bannerRect, _battleBannerTex, ScaleMode.StretchToFill);
            }
            else
            {
                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.9f, 0.2f, 0.2f, 0.95f);
                GUI.Box(bannerRect, "");
                GUI.backgroundColor = prev;
            }

            GUIStyle bannerTitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 26,
                fontStyle = FontStyle.Bold
            };
            bannerTitleStyle.normal.textColor = new Color(1.0f, 0.95f, 0.3f);

            GUIStyle vsStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 19,
                fontStyle = FontStyle.Bold
            };
            vsStyle.normal.textColor = Color.white;

            GUI.Label(new Rect(x, y + 14, w, 36), "⚔️ <b>전 투 진 입 !</b> ⚔️", bannerTitleStyle);
            GUI.Label(new Rect(x, y + 54, w, 32), $"<color=cyan>[아군] {_battleAttackerName}</color>  <b>VS</b>  <color=red>[적군] {_battleDefenderName}</color>", vsStyle);
            GUI.Label(new Rect(x, y + 88, w, 24), $"💥 공격력 {_battleDamage}의 기습 타격 개시!", new GUIStyle(vsStyle) { fontSize = 14 });
        }

        /// <summary>
        /// 효과 발동(스킬) 토스트 연출
        /// </summary>
        private void DrawSkillToast()
        {
            if (_skillToastTimer <= 0f) return;

            float w = 380f;
            float h = 48f;
            float x = (Screen.width - w) * 0.5f;
            float y = Screen.height * 0.45f;

            GUI.Box(new Rect(x, y, w, h), "");

            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
            style.normal.textColor = new Color(0.9f, 0.65f, 1.0f);

            GUI.Label(new Rect(x, y + 10, w, 28), _skillToastMessage, style);
        }
    }
}
