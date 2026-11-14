using UnityEngine;

namespace ChessMonsters.Grid
{
    /// <summary>
    /// 물(Water) 타일의 3프레임 순환 애니메이션을 실시간으로 제어하는 컴포넌트
    /// 3가지 수면 텍스처를 번갈아 표기하여 잔잔하게 일렁이는 물결 애니메이션을 연출합니다.
    /// 에디터 및 인게임에서 모두 동작 ([ExecuteAlways])
    /// </summary>
    [ExecuteAlways]
    public class WaterTileAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        [Tooltip("애니메이션을 적용할 물 머티리얼")]
        [SerializeField] private Material _waterMaterial;

        [Tooltip("순환할 3가지 물 텍스처 프레임")]
        [SerializeField] private Texture2D[] _frames = new Texture2D[3];

        [Tooltip("프레임 전환 주기 (초 단위, 권장 0.2 ~ 0.3초)")]
        [SerializeField] private float _animationInterval = 0.25f;

        private int _currentFrame = 0;
        private float _timer = 0f;
        private double _lastEditorTime = 0.0;

        public Texture2D[] Frames => _frames;
        public float AnimationInterval
        {
            get => _animationInterval;
            set => _animationInterval = Mathf.Max(0.05f, value);
        }

        private void OnEnable()
        {
            InitializeAssetsIfNull();
            _timer = 0f;
#if UNITY_EDITOR
            _lastEditorTime = UnityEditor.EditorApplication.timeSinceStartup;
#endif
        }

        private void Update()
        {
            if (_waterMaterial == null || _frames == null || _frames.Length < 2)
            {
                InitializeAssetsIfNull();
                if (_waterMaterial == null || _frames == null || _frames.Length < 2) return;
            }

            float dt = 0f;
            if (Application.isPlaying)
            {
                dt = Time.deltaTime;
            }
            else
            {
#if UNITY_EDITOR
                double now = UnityEditor.EditorApplication.timeSinceStartup;
                dt = (float)(now - _lastEditorTime);
                _lastEditorTime = now;
#endif
            }

            _timer += dt;
            if (_timer >= _animationInterval)
            {
                _timer = 0f;
                _currentFrame = (_currentFrame + 1) % _frames.Length;

                Texture2D currentTex = _frames[_currentFrame];
                if (currentTex != null)
                {
                    _waterMaterial.SetTexture("_BaseMap", currentTex);
                    _waterMaterial.SetTexture("_MainTex", currentTex);
                }
            }
        }

        public void InitializeAssetsIfNull()
        {
            if (_waterMaterial == null)
            {
                if (GridManager.Instance != null && GridManager.Instance.WaterMaterial != null)
                {
                    _waterMaterial = GridManager.Instance.WaterMaterial;
                }
#if UNITY_EDITOR
                else
                {
                    _waterMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Tiles/Mat_Tile_Water.mat");
                }
#endif
            }

            if (_frames == null || _frames.Length < 3 || _frames[0] == null)
            {
#if UNITY_EDITOR
                _frames = new Texture2D[3];
                _frames[0] = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Tiles/Tile_Water_01.png");
                _frames[1] = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Tiles/Tile_Water_02.png");
                _frames[2] = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Tiles/Tile_Water_03.png");
#endif
            }
        }
    }
}