using UnityEngine;
using VampireSurvivors.Core;

namespace VampireSurvivors.Character
{
    /// <summary>
    /// 투사체 클래스.
    /// 발사 후 직선으로 이동하며 적과 충돌 시 데미지를 입힙니다.
    /// PoolableObject를 상속받아 오브젝트 풀링을 지원합니다.
    /// </summary>
    /// <remarks>
    /// PlayerAttack에서 생성되며, 적 또는 벽에 충돌하거나
    /// 수명이 다하면 자동으로 풀로 반환됩니다.
    /// </remarks>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Projectile : PoolableObject
    {
        #region 인스펙터 필드

        [Header("이동")]
        [Tooltip("투사체 이동 속도")]
        [SerializeField]
        private float _speed = 15f;

        [Header("수명")]
        [Tooltip("최대 수명 (초). 이 시간이 지나면 자동으로 풀로 반환됩니다.")]
        [SerializeField]
        private float _maxLifetime = 5f;

        [Header("관통")]
        [Tooltip("적을 관통하는 횟수. 0이면 첫 충돌 시 사라집니다.")]
        [SerializeField]
        private int _pierceCount = 0;

        [Header("충돌")]
        [Tooltip("벽/장애물 레이어 마스크")]
        [SerializeField]
        private LayerMask _obstacleLayerMask;

        [Tooltip("적 레이어 마스크")]
        [SerializeField]
        private LayerMask _enemyLayerMask;

        [Header("시각 효과")]
        [Tooltip("SpriteRenderer (스프라이트 방향 전환용)")]
        [SerializeField]
        private SpriteRenderer _spriteRenderer;

        [Tooltip("TrailRenderer (궤적 효과)")]
        [SerializeField]
        private TrailRenderer _trailRenderer;

        #endregion

        #region 런타임 변수

        /// <summary>
        /// Rigidbody2D 컴포넌트
        /// </summary>
        private Rigidbody2D _rigidbody;

        /// <summary>
        /// 이동 방향
        /// </summary>
        private Vector2 _direction;

        /// <summary>
        /// 현재 데미지
        /// </summary>
        private float _damage;

        /// <summary>
        /// 치명타 여부
        /// </summary>
        private bool _isCritical;

        /// <summary>
        /// 발사한 오브젝트 (자기 자신에게 데미지 방지용)
        /// </summary>
        private GameObject _owner;

        /// <summary>
        /// 남은 관통 횟수
        /// </summary>
        private int _remainingPierces;

        /// <summary>
        /// 남은 수명
        /// </summary>
        private float _remainingLifetime;

        /// <summary>
        /// 초기화 완료 여부
        /// </summary>
        private bool _isInitialized;

        #endregion

        #region Properties

        /// <summary>
        /// 현재 데미지
        /// </summary>
        public float Damage => _damage;

        /// <summary>
        /// 치명타 여부
        /// </summary>
        public bool IsCritical => _isCritical;

        /// <summary>
        /// 발사자
        /// </summary>
        public GameObject Owner => _owner;

        /// <summary>
        /// 이동 방향
        /// </summary>
        public Vector2 Direction => _direction;

        #endregion

        #region Unity 생명주기

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();

            // Rigidbody2D 설정
            _rigidbody.gravityScale = 0f;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // SpriteRenderer 자동 탐색
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            // TrailRenderer 자동 탐색
            if (_trailRenderer == null)
            {
                _trailRenderer = GetComponentInChildren<TrailRenderer>();
            }
        }

        private void Update()
        {
            if (!_isInitialized) return;

            // 수명 감소
            _remainingLifetime -= Time.deltaTime;
            if (_remainingLifetime <= 0)
            {
                ReturnToPool();
            }
        }

        private void FixedUpdate()
        {
            if (!_isInitialized) return;

            // 이동
            _rigidbody.linearVelocity = _direction * _speed;
        }

        #endregion

        #region 초기화

        /// <summary>
        /// 투사체를 초기화합니다.
        /// </summary>
        /// <param name="direction">이동 방향 (정규화됨)</param>
        /// <param name="damage">데미지</param>
        /// <param name="isCritical">치명타 여부</param>
        /// <param name="owner">발사자 GameObject</param>
        public void Initialize(Vector2 direction, float damage, bool isCritical = false, GameObject owner = null)
        {
            _direction = direction.normalized;
            _damage = damage;
            _isCritical = isCritical;
            _owner = owner;
            _remainingPierces = _pierceCount;
            _remainingLifetime = _maxLifetime;
            _isInitialized = true;

            // 이동 시작
            _rigidbody.linearVelocity = _direction * _speed;

            // 스프라이트 회전 (방향에 맞게)
            UpdateRotation();

            // 치명타 시각 효과 (선택적)
            if (_isCritical)
            {
                ApplyCriticalVisual();
            }
        }

        /// <summary>
        /// 투사체를 초기화합니다 (간단 버전).
        /// </summary>
        /// <param name="direction">이동 방향</param>
        /// <param name="damage">데미지</param>
        public void Initialize(Vector2 direction, float damage)
        {
            Initialize(direction, damage, false, null);
        }

        #endregion

        #region 풀링 오버라이드

        /// <summary>
        /// 풀에서 꺼내질 때 호출됩니다.
        /// </summary>
        protected override void OnPoolSpawn()
        {
            base.OnPoolSpawn();

            // 상태 초기화
            _isInitialized = false;
            _rigidbody.linearVelocity = Vector2.zero;

            // Trail 초기화
            if (_trailRenderer != null)
            {
                _trailRenderer.Clear();
            }

            // 스프라이트 색상 초기화
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = Color.white;
            }
        }

        /// <summary>
        /// 풀로 반환될 때 호출됩니다.
        /// </summary>
        protected override void OnPoolDespawn()
        {
            // 정리
            _isInitialized = false;
            _rigidbody.linearVelocity = Vector2.zero;
            _owner = null;

            base.OnPoolDespawn();
        }

        #endregion

        #region 충돌 처리

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isInitialized) return;

            // 발사자 자신은 무시
            if (_owner != null && other.gameObject == _owner) return;

            // 벽/장애물 충돌
            if (IsInLayerMask(other.gameObject.layer, _obstacleLayerMask))
            {
                OnHitObstacle(other);
                return;
            }

            // 적 충돌
            if (IsInLayerMask(other.gameObject.layer, _enemyLayerMask))
            {
                OnHitEnemy(other);
            }
        }

        /// <summary>
        /// 적과 충돌했을 때 호출됩니다.
        /// </summary>
        /// <param name="enemyCollider">적 콜라이더</param>
        private void OnHitEnemy(Collider2D enemyCollider)
        {
            // CharacterBase가 있으면 데미지 적용
            var character = enemyCollider.GetComponent<CharacterBase>();
            if (character != null)
            {
                character.TakeDamage(_damage, _owner);
            }

            // 관통 처리
            if (_remainingPierces > 0)
            {
                _remainingPierces--;
                // 관통하므로 계속 이동
            }
            else
            {
                // 관통 횟수 소진 - 풀로 반환
                OnProjectileDestroy();
            }
        }

        /// <summary>
        /// 장애물과 충돌했을 때 호출됩니다.
        /// </summary>
        /// <param name="obstacleCollider">장애물 콜라이더</param>
        private void OnHitObstacle(Collider2D obstacleCollider)
        {
            // 벽에 닿으면 바로 사라짐
            OnProjectileDestroy();
        }

        /// <summary>
        /// 투사체가 파괴될 때 호출됩니다.
        /// </summary>
        private void OnProjectileDestroy()
        {
            // 파괴 이펙트 재생 (선택적)
            // ParticleManager.Instance?.PlayEffect("ProjectileHit", transform.position);

            ReturnToPool();
        }

        #endregion

        #region 유틸리티

        /// <summary>
        /// 이동 방향에 맞게 투사체를 회전시킵니다.
        /// </summary>
        private void UpdateRotation()
        {
            if (_direction.sqrMagnitude < 0.01f) return;

            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        /// <summary>
        /// 치명타 시각 효과를 적용합니다.
        /// </summary>
        private void ApplyCriticalVisual()
        {
            // 스프라이트 색상 변경 (빨간색 계열)
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = new Color(1f, 0.5f, 0.5f);
            }

            // 크기 증가
            transform.localScale = Vector3.one * 1.2f;
        }

        /// <summary>
        /// 레이어가 레이어마스크에 포함되는지 확인합니다.
        /// </summary>
        /// <param name="layer">확인할 레이어</param>
        /// <param name="layerMask">레이어 마스크</param>
        /// <returns>포함 여부</returns>
        private bool IsInLayerMask(int layer, LayerMask layerMask)
        {
            return (layerMask.value & (1 << layer)) != 0;
        }

        #endregion

        #region 공용 메서드

        /// <summary>
        /// 투사체의 속도를 설정합니다.
        /// </summary>
        /// <param name="speed">새 속도</param>
        public void SetSpeed(float speed)
        {
            _speed = Mathf.Max(0, speed);
        }

        /// <summary>
        /// 투사체의 관통 횟수를 설정합니다.
        /// </summary>
        /// <param name="pierceCount">관통 횟수</param>
        public void SetPierceCount(int pierceCount)
        {
            _pierceCount = Mathf.Max(0, pierceCount);
            _remainingPierces = _pierceCount;
        }

        /// <summary>
        /// 투사체의 수명을 설정합니다.
        /// </summary>
        /// <param name="lifetime">수명 (초)</param>
        public void SetLifetime(float lifetime)
        {
            _maxLifetime = Mathf.Max(0, lifetime);
            _remainingLifetime = _maxLifetime;
        }

        #endregion
    }
}
