using System.Collections.Generic;
using UnityEngine;
using VampireSurvivors.Core;
using VampireSurvivors.Data;

namespace VampireSurvivors.Character
{
    /// <summary>
    /// 플레이어의 자동 공격을 담당하는 클래스.
    /// 뱀파이어 서바이버스 스타일로 가장 가까운 적을 자동으로 공격합니다.
    /// </summary>
    /// <remarks>
    /// Player 오브젝트에 함께 부착하여 사용합니다.
    /// 투사체는 오브젝트 풀링을 통해 관리됩니다.
    /// </remarks>
    [RequireComponent(typeof(Player))]
    public class PlayerAttack : MonoBehaviour
    {
        #region 인스펙터 필드

        [Header("투사체 설정")]
        [Tooltip("투사체 프리팹")]
        [SerializeField]
        private Projectile _projectilePrefab;

        [Tooltip("투사체 풀 키 (PoolManager에서 사용)")]
        [SerializeField]
        private string _projectilePoolKey = "PlayerProjectile";

        [Tooltip("투사체 풀 초기 크기")]
        [SerializeField]
        private int _poolInitialSize = 20;

        [Header("공격 설정")]
        [Tooltip("기본 공격 쿨다운 (초)")]
        [SerializeField]
        private float _baseAttackCooldown = 1f;

        [Tooltip("기본 투사체 개수")]
        [SerializeField]
        private int _baseProjectileCount = 1;

        [Tooltip("다중 투사체 발사 시 각도 간격")]
        [SerializeField]
        private float _multiShotAngleSpread = 15f;

        [Header("타겟팅")]
        [Tooltip("적 탐지 범위")]
        [SerializeField]
        private float _detectionRange = 10f;

        [Tooltip("적 레이어 마스크")]
        [SerializeField]
        private LayerMask _enemyLayerMask;

        [Header("발사 위치")]
        [Tooltip("투사체 발사 시작 위치 (null이면 플레이어 위치)")]
        [SerializeField]
        private Transform _firePoint;

        #endregion

        #region 런타임 변수

        /// <summary>
        /// 플레이어 컴포넌트 참조
        /// </summary>
        private Player _player;

        /// <summary>
        /// 현재 공격 쿨다운 타이머
        /// </summary>
        private float _attackTimer;

        /// <summary>
        /// 현재 타겟 (가장 가까운 적)
        /// </summary>
        private Transform _currentTarget;

        /// <summary>
        /// 공격 활성화 여부
        /// </summary>
        private bool _attackEnabled = true;

        /// <summary>
        /// 투사체 풀 초기화 여부
        /// </summary>
        private bool _poolInitialized = false;

        /// <summary>
        /// 범위 내 적 탐색을 위한 재사용 가능한 배열
        /// </summary>
        private readonly Collider2D[] _enemyBuffer = new Collider2D[50];

        #endregion

        #region Properties

        /// <summary>
        /// 현재 공격 쿨다운 (공격 속도 적용)
        /// </summary>
        public float CurrentAttackCooldown
        {
            get
            {
                float attackSpeed = 1f;
                if (_player != null && _player.CharacterData != null)
                {
                    attackSpeed = _player.CharacterData.AttackSpeed;
                }
                return _baseAttackCooldown / Mathf.Max(0.1f, attackSpeed);
            }
        }

        /// <summary>
        /// 현재 투사체 개수 (보너스 포함)
        /// </summary>
        public int CurrentProjectileCount
        {
            get
            {
                int bonus = 0;
                if (_player != null && _player.CharacterData != null)
                {
                    bonus = _player.CharacterData.ProjectileCountBonus;
                }
                return _baseProjectileCount + bonus;
            }
        }

        /// <summary>
        /// 현재 타겟
        /// </summary>
        public Transform CurrentTarget => _currentTarget;

        #endregion

        #region Unity 생명주기

        private void Awake()
        {
            _player = GetComponent<Player>();

            // 발사 위치가 설정되지 않았으면 자신의 Transform 사용
            if (_firePoint == null)
            {
                _firePoint = transform;
            }
        }

        private void Start()
        {
            // 투사체 풀 초기화
            InitializeProjectilePool();
        }

        private void Update()
        {
            // 공격 비활성화 또는 플레이어 사망 시 중단
            if (!_attackEnabled || _player == null || _player.IsDead) return;

            // 쿨다운 감소
            if (_attackTimer > 0)
            {
                _attackTimer -= Time.deltaTime;
            }

            // 타겟 탐색
            FindNearestEnemy();

            // 공격 시도
            if (_attackTimer <= 0 && _currentTarget != null)
            {
                Attack();
                _attackTimer = CurrentAttackCooldown;
            }
        }

        private void OnDrawGizmosSelected()
        {
            // 탐지 범위 시각화
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _detectionRange);

            // 현재 타겟 표시
            if (_currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, _currentTarget.position);
            }
        }

        #endregion

        #region 풀 초기화

        /// <summary>
        /// 투사체 오브젝트 풀을 초기화합니다.
        /// </summary>
        private void InitializeProjectilePool()
        {
            if (_poolInitialized || _projectilePrefab == null) return;

            // PoolManager가 있으면 풀 생성
            if (PoolManager.Instance != null)
            {
                if (!PoolManager.Instance.HasPool(_projectilePoolKey))
                {
                    PoolManager.Instance.CreatePool(
                        _projectilePoolKey,
                        _projectilePrefab.gameObject,
                        _poolInitialSize
                    );
                }
                _poolInitialized = true;
            }
            else
            {
                Debug.LogWarning("[PlayerAttack] PoolManager를 찾을 수 없습니다. 투사체 풀링이 비활성화됩니다.");
            }
        }

        #endregion

        #region 타겟팅

        /// <summary>
        /// 가장 가까운 적을 찾습니다.
        /// </summary>
        private void FindNearestEnemy()
        {
            _currentTarget = null;
            float nearestDistance = float.MaxValue;

            // OverlapCircleNonAlloc으로 GC 최소화
            int count = Physics2D.OverlapCircleNonAlloc(
                transform.position,
                _detectionRange,
                _enemyBuffer,
                _enemyLayerMask
            );

            for (int i = 0; i < count; i++)
            {
                Collider2D enemy = _enemyBuffer[i];
                if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;

                float distance = Vector2.Distance(transform.position, enemy.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    _currentTarget = enemy.transform;
                }
            }
        }

        /// <summary>
        /// 특정 위치에서 가장 가까운 적을 찾습니다.
        /// </summary>
        /// <param name="position">탐색 시작 위치</param>
        /// <param name="range">탐색 범위</param>
        /// <returns>가장 가까운 적의 Transform 또는 null</returns>
        public Transform FindNearestEnemyFrom(Vector2 position, float range)
        {
            Transform nearest = null;
            float nearestDistance = float.MaxValue;

            int count = Physics2D.OverlapCircleNonAlloc(
                position,
                range,
                _enemyBuffer,
                _enemyLayerMask
            );

            for (int i = 0; i < count; i++)
            {
                Collider2D enemy = _enemyBuffer[i];
                if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;

                float distance = Vector2.Distance(position, enemy.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = enemy.transform;
                }
            }

            return nearest;
        }

        #endregion

        #region 공격

        /// <summary>
        /// 공격을 실행합니다.
        /// </summary>
        private void Attack()
        {
            if (_currentTarget == null) return;

            // 타겟 방향 계산
            Vector2 direction = (_currentTarget.position - _firePoint.position).normalized;

            // 투사체 개수에 따라 발사
            int projectileCount = CurrentProjectileCount;

            if (projectileCount == 1)
            {
                // 단일 투사체
                FireProjectile(direction);
            }
            else
            {
                // 다중 투사체 (부채꼴 형태)
                float totalSpread = _multiShotAngleSpread * (projectileCount - 1);
                float startAngle = -totalSpread / 2f;

                for (int i = 0; i < projectileCount; i++)
                {
                    float angle = startAngle + (_multiShotAngleSpread * i);
                    Vector2 spreadDirection = RotateVector(direction, angle);
                    FireProjectile(spreadDirection);
                }
            }
        }

        /// <summary>
        /// 투사체를 발사합니다.
        /// </summary>
        /// <param name="direction">발사 방향</param>
        private void FireProjectile(Vector2 direction)
        {
            Projectile projectile = null;

            // 풀에서 투사체 가져오기
            if (_poolInitialized && PoolManager.Instance != null)
            {
                projectile = PoolManager.Instance.Get<Projectile>(
                    _projectilePoolKey,
                    _firePoint.position,
                    Quaternion.identity
                );
            }

            // 풀링 실패 시 직접 생성 (폴백)
            if (projectile == null && _projectilePrefab != null)
            {
                projectile = Instantiate(_projectilePrefab, _firePoint.position, Quaternion.identity);
            }

            if (projectile == null)
            {
                Debug.LogWarning("[PlayerAttack] 투사체 생성 실패");
                return;
            }

            // 투사체 초기화
            float damage = _player != null ? _player.CurrentAttackPower : 1f;
            bool isCritical = CheckCritical();

            if (isCritical && _player != null && _player.CharacterData != null)
            {
                damage *= _player.CharacterData.CriticalMultiplier;
            }

            projectile.Initialize(direction, damage, isCritical, gameObject);
        }

        /// <summary>
        /// 치명타 여부를 판정합니다.
        /// </summary>
        /// <returns>치명타 여부</returns>
        private bool CheckCritical()
        {
            if (_player == null || _player.CharacterData == null) return false;

            float critChance = _player.CharacterData.CriticalChance;
            return Random.value < critChance;
        }

        /// <summary>
        /// 벡터를 특정 각도만큼 회전합니다.
        /// </summary>
        /// <param name="vector">원본 벡터</param>
        /// <param name="degrees">회전 각도 (도)</param>
        /// <returns>회전된 벡터</returns>
        private Vector2 RotateVector(Vector2 vector, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);

            return new Vector2(
                vector.x * cos - vector.y * sin,
                vector.x * sin + vector.y * cos
            );
        }

        #endregion

        #region 공용 메서드

        /// <summary>
        /// 공격을 활성화하거나 비활성화합니다.
        /// </summary>
        /// <param name="enabled">활성화 여부</param>
        public void SetAttackEnabled(bool enabled)
        {
            _attackEnabled = enabled;
        }

        /// <summary>
        /// 공격 쿨다운을 즉시 리셋합니다.
        /// </summary>
        public void ResetCooldown()
        {
            _attackTimer = 0f;
        }

        /// <summary>
        /// 탐지 범위를 설정합니다.
        /// </summary>
        /// <param name="range">새 탐지 범위</param>
        public void SetDetectionRange(float range)
        {
            _detectionRange = Mathf.Max(0, range);
        }

        #endregion
    }
}
