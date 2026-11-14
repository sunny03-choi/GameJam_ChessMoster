using UnityEngine;

namespace ChessMonsters.Unit
{
    /// <summary>
    /// 3D 월드에서 2D 스프라이트가 카메라를 항상 자연스럽게 바라보도록 회전시키는 빌보드 컴포넌트
    /// 참고 영상: Reddit r/unity_tutorials - 2D sprites in 3D world: Sprite Billboarding
    /// (https://www.reddit.com/r/unity_tutorials/comments/x7b9br/2d_sprites_in_3d_world_sprite_billboarding/)
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class SpriteBillboard : MonoBehaviour
    {
        public enum BillboardMode
        {
            [Tooltip("카메라의 전체 회전과 완벽히 일치 (쿼터뷰/아이소메트릭 체스판에서 캐릭터가 찌그러짐 없이 화면 정면으로 똑바로 투영됨)")]
            CameraAligned,

            [Tooltip("Y축 수직 회전만 카메라를 향함 (지면과 항상 90도 수직을 유지하는 원통형 빌보드)")]
            YAxisOnly,

            [Tooltip("카메라 렌즈 중심점을 직접 바라봄")]
            LookAtCamera
        }

        [Header("Billboard Settings")]
        [Tooltip("빌보드 동작 모드")]
        [SerializeField] private BillboardMode _mode = BillboardMode.CameraAligned;

        [Tooltip("특정 카메라를 지정하지 않으면 Camera.main 자동 사용")]
        [SerializeField] private Camera _targetCamera;

        [Tooltip("추가 회전 오프셋 (각도 보정이 필요할 때 사용)")]
        [SerializeField] private Vector3 _rotationOffset = Vector3.zero;

        [Header("Smoothing (선택적 부드러운 회전)")]
        [Tooltip("카메라 회전 시 즉각 회전 대신 부드러운 보간 적용 여부")]
        [SerializeField] private bool _useSmoothing = false;
        [SerializeField] private float _smoothSpeed = 15f;

        private Transform _camTransform;

        public BillboardMode Mode
        {
            get => _mode;
            set => _mode = value;
        }

        public Camera TargetCamera
        {
            get => _targetCamera;
            set
            {
                _targetCamera = value;
                if (_targetCamera != null) _camTransform = _targetCamera.transform;
            }
        }

        private void OnEnable()
        {
            FindTargetCamera();
            UpdateBillboardRotation(instant: true);
        }

        private void Start()
        {
            FindTargetCamera();
            UpdateBillboardRotation(instant: true);
        }

        /// <summary>
        /// 카메라 이동 및 캐릭터 이동이 Update에서 완료된 후, 지터링(떨림) 현상을 방지하기 위해
        /// 반드시 LateUpdate에서 빌보드 회전을 갱신합니다. (Reddit 튜토리얼 핵심 기법)
        /// </summary>
        private void LateUpdate()
        {
            if (_camTransform == null)
            {
                FindTargetCamera();
                if (_camTransform == null) return;
            }

            UpdateBillboardRotation(instant: !_useSmoothing || !Application.isPlaying);
        }

        private void FindTargetCamera()
        {
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
                if (_targetCamera == null)
                {
                    _targetCamera = Object.FindFirstObjectByType<Camera>();
                }
            }

            if (_targetCamera != null)
            {
                _camTransform = _targetCamera.transform;
            }
        }

        private void UpdateBillboardRotation(bool instant)
        {
            if (_camTransform == null) return;

            Quaternion targetRotation;

            switch (_mode)
            {
                case BillboardMode.CameraAligned:
                    // 카메라와 완전히 동일한 회전 적용 (쿼터뷰 아이소메트릭 체스판에 최적)
                    targetRotation = _camTransform.rotation;
                    break;

                case BillboardMode.YAxisOnly:
                    // Y축(수직축) 회전만 카메라의 Y회전과 맞추고 X/Z는 고정 (지면에 수직인 원통형 빌보드)
                    Vector3 camEuler = _camTransform.rotation.eulerAngles;
                    targetRotation = Quaternion.Euler(0f, camEuler.y, 0f);
                    break;

                case BillboardMode.LookAtCamera:
                default:
                    // 카메라 위치를 직접 바라봄
                    Vector3 dirToCam = _camTransform.position - transform.position;
                    if (dirToCam.sqrMagnitude > 0.001f)
                    {
                        targetRotation = Quaternion.LookRotation(-dirToCam.normalized, Vector3.up);
                    }
                    else
                    {
                        targetRotation = transform.rotation;
                    }
                    break;
            }

            if (_rotationOffset != Vector3.zero)
            {
                targetRotation *= Quaternion.Euler(_rotationOffset);
            }

            if (instant)
            {
                transform.rotation = targetRotation;
            }
            else
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * _smoothSpeed);
            }
        }
    }
}
