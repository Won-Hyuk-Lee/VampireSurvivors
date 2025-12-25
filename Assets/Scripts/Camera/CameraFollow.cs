using UnityEngine;
using VampireSurvivors.Character;

namespace VampireSurvivors.CameraSystem
{
    /// <summary>
    /// 카메라가 플레이어를 부드럽게 추적하도록 하는 클래스.
    /// 스무스 이동, 오프셋, 경계 제한 등의 기능을 제공합니다.
    /// </summary>
    /// <remarks>
    /// 메인 카메라에 부착하여 사용합니다.
    /// Player.Instance를 자동으로 추적합니다.
    /// </remarks>
    public class CameraFollow : MonoBehaviour
    {
        #region 인스펙터 필드

        [Header("추적 대상")]
        [Tooltip("추적할 대상 Transform (null이면 Player.Instance 자동 탐색)")]
        [SerializeField]
        private Transform _target;

        [Tooltip("대상 자동 탐색 여부")]
        [SerializeField]
        private bool _autoFindPlayer = true;

        [Header("이동 설정")]
        [Tooltip("카메라 이동 스무스니스 (낮을수록 빠르게 따라감)")]
        [Range(0.01f, 1f)]
        [SerializeField]
        private float _smoothTime = 0.15f;

        [Tooltip("대상과의 오프셋")]
        [SerializeField]
        private Vector3 _offset = new Vector3(0, 0, -10);

        [Tooltip("이동 방향에 따른 추가 오프셋 (카메라가 진행 방향을 미리 보여줌)")]
        [SerializeField]
        private float _lookAheadAmount = 0f;

        [Tooltip("Look Ahead 스무스니스")]
        [Range(0.01f, 1f)]
        [SerializeField]
        private float _lookAheadSmoothTime = 0.3f;

        [Header("경계 제한")]
        [Tooltip("카메라 이동 경계 사용 여부")]
        [SerializeField]
        private bool _useBounds = false;

        [Tooltip("카메라 이동 최소 경계")]
        [SerializeField]
        private Vector2 _minBounds = new Vector2(-50, -50);

        [Tooltip("카메라 이동 최대 경계")]
        [SerializeField]
        private Vector2 _maxBounds = new Vector2(50, 50);

        [Header("화면 흔들림")]
        [Tooltip("흔들림 강도")]
        [SerializeField]
        private float _shakeIntensity = 0.3f;

        [Tooltip("흔들림 감쇠 속도")]
        [SerializeField]
        private float _shakeDampingSpeed = 1f;

        #endregion

        #region 런타임 변수

        /// <summary>
        /// 현재 속도 (SmoothDamp용)
        /// </summary>
        private Vector3 _currentVelocity;

        /// <summary>
        /// Look Ahead 속도
        /// </summary>
        private Vector3 _lookAheadVelocity;

        /// <summary>
        /// 현재 Look Ahead 오프셋
        /// </summary>
        private Vector3 _currentLookAhead;

        /// <summary>
        /// 이전 프레임 대상 위치
        /// </summary>
        private Vector3 _previousTargetPosition;

        /// <summary>
        /// 현재 흔들림 강도
        /// </summary>
        private float _currentShakeAmount;

        /// <summary>
        /// 카메라 컴포넌트
        /// </summary>
        private Camera _camera;

        /// <summary>
        /// 초기화 완료 여부
        /// </summary>
        private bool _initialized;

        #endregion

        #region Properties

        /// <summary>
        /// 추적 대상
        /// </summary>
        public Transform Target
        {
            get => _target;
            set => _target = value;
        }

        /// <summary>
        /// 카메라 오프셋
        /// </summary>
        public Vector3 Offset
        {
            get => _offset;
            set => _offset = value;
        }

        /// <summary>
        /// 스무스 시간
        /// </summary>
        public float SmoothTime
        {
            get => _smoothTime;
            set => _smoothTime = Mathf.Max(0.01f, value);
        }

        #endregion

        #region Unity 생명주기

        private void Awake()
        {
            _camera = GetComponent<Camera>();

            // 타겟 초기화
            if (_target == null && _autoFindPlayer)
            {
                FindPlayer();
            }
        }

        private void Start()
        {
            // 초기 위치 설정
            if (_target != null)
            {
                Vector3 targetPos = _target.position + _offset;
                transform.position = ClampToBounds(targetPos);
                _previousTargetPosition = _target.position;
            }

            _initialized = true;
        }

        private void LateUpdate()
        {
            // 대상이 없으면 자동 탐색
            if (_target == null)
            {
                if (_autoFindPlayer)
                {
                    FindPlayer();
                }
                return;
            }

            // 카메라 이동
            FollowTarget();

            // 화면 흔들림 적용
            ApplyShake();
        }

        private void OnDrawGizmosSelected()
        {
            if (!_useBounds) return;

            // 경계 시각화
            Gizmos.color = Color.cyan;
            Vector3 center = new Vector3(
                (_minBounds.x + _maxBounds.x) / 2,
                (_minBounds.y + _maxBounds.y) / 2,
                0
            );
            Vector3 size = new Vector3(
                _maxBounds.x - _minBounds.x,
                _maxBounds.y - _minBounds.y,
                1
            );
            Gizmos.DrawWireCube(center, size);
        }

        #endregion

        #region 카메라 추적

        /// <summary>
        /// 대상을 부드럽게 추적합니다.
        /// </summary>
        private void FollowTarget()
        {
            // 목표 위치 계산
            Vector3 targetPosition = _target.position + _offset;

            // Look Ahead 계산
            if (_lookAheadAmount > 0)
            {
                Vector3 moveDirection = (_target.position - _previousTargetPosition) / Time.deltaTime;
                Vector3 targetLookAhead = new Vector3(moveDirection.x, moveDirection.y, 0).normalized * _lookAheadAmount;

                _currentLookAhead = Vector3.SmoothDamp(
                    _currentLookAhead,
                    targetLookAhead,
                    ref _lookAheadVelocity,
                    _lookAheadSmoothTime
                );

                targetPosition += _currentLookAhead;
                _previousTargetPosition = _target.position;
            }

            // 스무스 이동
            Vector3 smoothedPosition = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref _currentVelocity,
                _smoothTime
            );

            // 경계 제한 적용
            smoothedPosition = ClampToBounds(smoothedPosition);

            // 위치 적용
            transform.position = smoothedPosition;
        }

        /// <summary>
        /// 플레이어를 찾아 대상으로 설정합니다.
        /// </summary>
        private void FindPlayer()
        {
            if (Player.Instance != null)
            {
                _target = Player.Instance.transform;
                _previousTargetPosition = _target.position;

                // 초기화 전이면 즉시 위치 이동
                if (!_initialized)
                {
                    transform.position = _target.position + _offset;
                }
            }
        }

        /// <summary>
        /// 위치를 경계 내로 제한합니다.
        /// </summary>
        private Vector3 ClampToBounds(Vector3 position)
        {
            if (!_useBounds) return position;

            // 카메라 크기 계산 (오쏘그래픽 카메라 기준)
            float halfHeight = 0f;
            float halfWidth = 0f;

            if (_camera != null && _camera.orthographic)
            {
                halfHeight = _camera.orthographicSize;
                halfWidth = halfHeight * _camera.aspect;
            }

            // 경계 적용
            float clampedX = Mathf.Clamp(position.x, _minBounds.x + halfWidth, _maxBounds.x - halfWidth);
            float clampedY = Mathf.Clamp(position.y, _minBounds.y + halfHeight, _maxBounds.y - halfHeight);

            return new Vector3(clampedX, clampedY, position.z);
        }

        #endregion

        #region 화면 흔들림

        /// <summary>
        /// 화면 흔들림을 시작합니다.
        /// </summary>
        /// <param name="intensity">흔들림 강도 (null이면 기본값 사용)</param>
        public void Shake(float? intensity = null)
        {
            _currentShakeAmount = intensity ?? _shakeIntensity;
        }

        /// <summary>
        /// 화면 흔들림을 적용합니다.
        /// </summary>
        private void ApplyShake()
        {
            if (_currentShakeAmount <= 0) return;

            // 랜덤 오프셋 계산
            Vector3 shakeOffset = new Vector3(
                Random.Range(-1f, 1f) * _currentShakeAmount,
                Random.Range(-1f, 1f) * _currentShakeAmount,
                0
            );

            // 흔들림 적용
            transform.position += shakeOffset;

            // 감쇠
            _currentShakeAmount = Mathf.Lerp(_currentShakeAmount, 0, _shakeDampingSpeed * Time.deltaTime);

            if (_currentShakeAmount < 0.01f)
            {
                _currentShakeAmount = 0;
            }
        }

        #endregion

        #region 공용 메서드

        /// <summary>
        /// 대상을 즉시 추적합니다 (스무스 없음).
        /// </summary>
        public void SnapToTarget()
        {
            if (_target == null) return;

            Vector3 targetPos = _target.position + _offset;
            transform.position = ClampToBounds(targetPos);
            _currentVelocity = Vector3.zero;
        }

        /// <summary>
        /// 경계를 설정합니다.
        /// </summary>
        /// <param name="min">최소 경계</param>
        /// <param name="max">최대 경계</param>
        public void SetBounds(Vector2 min, Vector2 max)
        {
            _minBounds = min;
            _maxBounds = max;
            _useBounds = true;
        }

        /// <summary>
        /// 경계를 비활성화합니다.
        /// </summary>
        public void DisableBounds()
        {
            _useBounds = false;
        }

        /// <summary>
        /// 특정 위치로 부드럽게 이동합니다.
        /// </summary>
        /// <param name="position">목표 위치</param>
        /// <param name="duration">이동 시간</param>
        public void MoveTo(Vector3 position, float duration = 1f)
        {
            StartCoroutine(MoveToCoroutine(position, duration));
        }

        private System.Collections.IEnumerator MoveToCoroutine(Vector3 targetPosition, float duration)
        {
            Vector3 startPosition = transform.position;
            float elapsed = 0f;

            // 추적 일시 중지
            Transform previousTarget = _target;
            _target = null;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = t * t * (3f - 2f * t); // SmoothStep

                transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                yield return null;
            }

            transform.position = targetPosition;

            // 추적 재개
            _target = previousTarget;
        }

        #endregion
    }
}
