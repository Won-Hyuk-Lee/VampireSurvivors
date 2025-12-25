using System;
using UnityEngine;
using VampireSurvivors.Character;
using VampireSurvivors.Core;
using VampireSurvivors.Data;
using VampireSurvivors.Utilities;

namespace VampireSurvivors.Monster
{
    /// <summary>
    /// 모든 몬스터의 기본 추상 클래스.
    /// 플레이어 추적, 접촉 데미지, 사망 시 보상 등의 공통 로직을 제공합니다.
    /// </summary>
    /// <remarks>
    /// PoolableObject를 상속받아 오브젝트 풀링을 지원합니다.
    /// CharacterBase와 유사한 구조이지만, 몬스터 전용 기능을 포함합니다.
    /// </remarks>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public abstract class MonsterBase : PoolableObject
    {
        #region 인스펙터 필드

        [Header("몬스터 데이터")]
        [Tooltip("몬스터의 기본 스탯 데이터")]
        [SerializeField]
        protected MonsterDataSO _monsterData;

        [Header("컴포넌트 참조")]
        [Tooltip("SpriteRenderer (스프라이트 방향 전환용)")]
        [SerializeField]
        protected SpriteRenderer _spriteRenderer;

        [Tooltip("Animator (애니메이션 제어용)")]
        [SerializeField]
        protected Animator _animator;

        [Header("타겟팅")]
        [Tooltip("플레이어 레이어 마스크")]
        [SerializeField]
        protected LayerMask _playerLayerMask;

        #endregion

        #region 런타임 스탯

        /// <summary>
        /// 현재 체력
        /// </summary>
        protected float _currentHealth;

        /// <summary>
        /// 현재 이동 속도
        /// </summary>
        protected float _currentMoveSpeed;

        /// <summary>
        /// 사망 상태 여부
        /// </summary>
        protected bool _isDead;

        /// <summary>
        /// 추적 대상 (플레이어)
        /// </summary>
        protected Transform _target;

        /// <summary>
        /// 마지막 공격 시간 (쿨다운 계산용)
        /// </summary>
        protected float _lastAttackTime;

        /// <summary>
        /// 넉백 중인지 여부
        /// </summary>
        protected bool _isKnockedBack;

        /// <summary>
        /// 넉백 타이머
        /// </summary>
        protected float _knockbackTimer;

        #endregion

        #region 컴포넌트 캐시

        /// <summary>
        /// Rigidbody2D 컴포넌트
        /// </summary>
        protected Rigidbody2D _rigidbody;

        /// <summary>
        /// Collider2D 컴포넌트
        /// </summary>
        protected Collider2D _collider;

        #endregion

        #region 이벤트

        /// <summary>
        /// 체력 변경 시 발생 (현재 체력, 최대 체력)
        /// </summary>
        public event Action<float, float> OnHealthChanged;

        /// <summary>
        /// 피격 시 발생 (받은 데미지)
        /// </summary>
        public event Action<float> OnDamaged;

        /// <summary>
        /// 사망 시 발생
        /// </summary>
        public event Action OnDeath;

        #endregion

        #region Properties

        /// <summary>
        /// 몬스터 데이터 SO
        /// </summary>
        public MonsterDataSO MonsterData => _monsterData;

        /// <summary>
        /// 현재 체력
        /// </summary>
        public float CurrentHealth => _currentHealth;

        /// <summary>
        /// 최대 체력
        /// </summary>
        public float MaxHealth => _monsterData != null ? _monsterData.MaxHealth : 10f;

        /// <summary>
        /// 체력 비율 (0~1)
        /// </summary>
        public float HealthRatio => MaxHealth > 0 ? _currentHealth / MaxHealth : 0f;

        /// <summary>
        /// 사망 상태 여부
        /// </summary>
        public bool IsDead => _isDead;

        /// <summary>
        /// 추적 대상
        /// </summary>
        public Transform Target => _target;

        #endregion

        #region Unity 생명주기

        protected virtual void Awake()
        {
            // 필수 컴포넌트 캐싱
            _rigidbody = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();

            // Rigidbody2D 설정
            _rigidbody.gravityScale = 0f;
            _rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;

            // SpriteRenderer 자동 탐색
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            // Animator 자동 탐색
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }
        }

        protected virtual void Update()
        {
            if (_isDead) return;

            // 넉백 타이머 처리
            if (_isKnockedBack)
            {
                _knockbackTimer -= Time.deltaTime;
                if (_knockbackTimer <= 0)
                {
                    _isKnockedBack = false;
                }
            }

            // 타겟 갱신 (플레이어가 사망했을 수 있음)
            UpdateTarget();
        }

        protected virtual void FixedUpdate()
        {
            if (_isDead || _isKnockedBack) return;

            // 플레이어 추적
            ChaseTarget();
        }

        #endregion

        #region 풀링 오버라이드

        /// <summary>
        /// 풀에서 꺼내질 때 호출됩니다.
        /// </summary>
        protected override void OnPoolSpawn()
        {
            base.OnPoolSpawn();

            // 스탯 초기화
            InitializeStats();

            // 타겟 설정
            FindTarget();

            // 콜라이더 활성화
            if (_collider != null)
            {
                _collider.enabled = true;
            }
        }

        /// <summary>
        /// 풀로 반환될 때 호출됩니다.
        /// </summary>
        protected override void OnPoolDespawn()
        {
            // 정리
            _target = null;
            _rigidbody.linearVelocity = Vector2.zero;

            base.OnPoolDespawn();
        }

        #endregion

        #region 초기화

        /// <summary>
        /// 몬스터 스탯을 초기화합니다.
        /// </summary>
        protected virtual void InitializeStats()
        {
            if (_monsterData == null)
            {
                Debug.LogWarning($"[MonsterBase] {name}에 MonsterData가 할당되지 않았습니다.");
                _currentHealth = 10f;
                _currentMoveSpeed = 2f;
                return;
            }

            _currentHealth = _monsterData.MaxHealth;
            _currentMoveSpeed = _monsterData.MoveSpeed;
            _isDead = false;
            _isKnockedBack = false;
            _lastAttackTime = -999f; // 즉시 공격 가능

            // 크기 적용
            transform.localScale = Vector3.one * _monsterData.Scale;

            // 체력 초기화 이벤트 발생
            OnHealthChanged?.Invoke(_currentHealth, MaxHealth);
        }

        /// <summary>
        /// 몬스터 데이터를 설정하고 스탯을 초기화합니다.
        /// </summary>
        /// <param name="data">적용할 몬스터 데이터</param>
        public virtual void SetMonsterData(MonsterDataSO data)
        {
            _monsterData = data;
            InitializeStats();

            // 스프라이트 변경
            if (_spriteRenderer != null && data.Sprite != null)
            {
                _spriteRenderer.sprite = data.Sprite;
            }
        }

        #endregion

        #region 타겟팅

        /// <summary>
        /// 플레이어를 찾아 타겟으로 설정합니다.
        /// </summary>
        protected virtual void FindTarget()
        {
            // Player 싱글톤 사용
            if (Player.Instance != null && !Player.Instance.IsDead)
            {
                _target = Player.Instance.transform;
            }
            else
            {
                _target = null;
            }
        }

        /// <summary>
        /// 타겟 상태를 갱신합니다.
        /// </summary>
        protected virtual void UpdateTarget()
        {
            // 타겟이 없거나 비활성화된 경우 다시 탐색
            if (_target == null || !_target.gameObject.activeInHierarchy)
            {
                FindTarget();
            }

            // 플레이어가 사망한 경우
            if (Player.Instance != null && Player.Instance.IsDead)
            {
                _target = null;
            }
        }

        #endregion

        #region 이동 (플레이어 추적)

        /// <summary>
        /// 타겟(플레이어)을 추적합니다.
        /// </summary>
        protected virtual void ChaseTarget()
        {
            if (_target == null)
            {
                _rigidbody.linearVelocity = Vector2.zero;
                return;
            }

            // 플레이어 방향 계산
            Vector2 direction = (_target.position - transform.position).normalized;

            // 이동
            _rigidbody.linearVelocity = direction * _currentMoveSpeed;

            // 스프라이트 방향 전환
            UpdateSpriteDirection(direction);
        }

        /// <summary>
        /// 스프라이트의 좌우 방향을 업데이트합니다.
        /// </summary>
        /// <param name="direction">이동 방향</param>
        protected virtual void UpdateSpriteDirection(Vector2 direction)
        {
            if (_spriteRenderer == null) return;

            if (direction.x < -0.01f)
            {
                _spriteRenderer.flipX = true;
            }
            else if (direction.x > 0.01f)
            {
                _spriteRenderer.flipX = false;
            }
        }

        #endregion

        #region 피격 및 체력

        /// <summary>
        /// 데미지를 받습니다.
        /// </summary>
        /// <param name="damage">받을 데미지량</param>
        /// <param name="attacker">공격자 GameObject</param>
        /// <param name="knockbackForce">넉백 힘 (0이면 넉백 없음)</param>
        /// <returns>실제로 받은 데미지량</returns>
        public virtual float TakeDamage(float damage, GameObject attacker = null, float knockbackForce = 0f)
        {
            if (_isDead) return 0f;

            // 데미지 적용
            float actualDamage = Mathf.Max(0, damage);
            _currentHealth = Mathf.Max(0, _currentHealth - actualDamage);

            // 이벤트 발생
            OnHealthChanged?.Invoke(_currentHealth, MaxHealth);
            OnDamaged?.Invoke(actualDamage);

            // 피격 효과
            OnTakeDamage(actualDamage, attacker);

            // 넉백 적용
            if (knockbackForce > 0 && attacker != null)
            {
                ApplyKnockback(attacker.transform.position, knockbackForce);
            }

            // 사망 체크
            if (_currentHealth <= 0)
            {
                Die();
            }

            return actualDamage;
        }

        /// <summary>
        /// 피격 시 호출되는 가상 메서드.
        /// </summary>
        /// <param name="damage">받은 데미지</param>
        /// <param name="attacker">공격자</param>
        protected virtual void OnTakeDamage(float damage, GameObject attacker)
        {
            // 피격 시각 효과 (깜빡임)
            if (_spriteRenderer != null)
            {
                StartCoroutine(DamageFlashCoroutine());
            }
        }

        /// <summary>
        /// 피격 시 깜빡임 효과 코루틴
        /// </summary>
        private System.Collections.IEnumerator DamageFlashCoroutine()
        {
            Color originalColor = _spriteRenderer.color;
            _spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = originalColor;
            }
        }

        /// <summary>
        /// 넉백을 적용합니다.
        /// </summary>
        /// <param name="sourcePosition">넉백 원점</param>
        /// <param name="force">넉백 힘</param>
        public virtual void ApplyKnockback(Vector3 sourcePosition, float force)
        {
            if (_monsterData == null || _monsterData.KnockbackResistance <= 0) return;

            // 넉백 방향 계산
            Vector2 knockbackDirection = (transform.position - sourcePosition).normalized;

            // 저항력 적용
            float actualForce = force * _monsterData.KnockbackResistance;

            // 넉백 적용
            _rigidbody.linearVelocity = knockbackDirection * actualForce;
            _isKnockedBack = true;
            _knockbackTimer = 0.2f; // 넉백 지속 시간
        }

        #endregion

        #region 사망

        /// <summary>
        /// 몬스터가 사망합니다.
        /// </summary>
        protected virtual void Die()
        {
            if (_isDead) return;

            _isDead = true;
            _rigidbody.linearVelocity = Vector2.zero;

            // 콜라이더 비활성화 (추가 충돌 방지)
            if (_collider != null)
            {
                _collider.enabled = false;
            }

            // 보상 드롭
            DropRewards();

            // 사망 이벤트 발생
            OnDeath?.Invoke();
            EventManager.TriggerEvent(GameEvents.MONSTER_KILLED, gameObject);

            // 사망 처리
            OnDie();
        }

        /// <summary>
        /// 사망 시 호출되는 가상 메서드.
        /// </summary>
        protected virtual void OnDie()
        {
            // 풀로 반환 (약간의 딜레이 후)
            ReturnToPoolAfterDelay(0.1f);
        }

        /// <summary>
        /// 보상을 드롭합니다.
        /// </summary>
        protected virtual void DropRewards()
        {
            if (_monsterData == null) return;

            // 경험치 드롭
            if (_monsterData.ExpReward > 0)
            {
                // 경험치 오브 생성 또는 직접 플레이어에게 전달
                if (Player.Instance != null)
                {
                    Player.Instance.GainExp(_monsterData.ExpReward);
                }

                // 또는 경험치 오브 스폰 이벤트
                // EventManager.TriggerEvent(GameEvents.SPAWN_EXP_ORB, new ExpOrbData { position = transform.position, amount = _monsterData.ExpReward });
            }

            // 골드 드롭
            int goldAmount = _monsterData.CalculateGoldDrop();
            if (goldAmount > 0)
            {
                // 골드 드롭 이벤트
                EventManager.TriggerEvent(GameEvents.GOLD_DROPPED, goldAmount);
            }
        }

        #endregion

        #region 충돌 처리 (접촉 데미지)

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (_isDead) return;

            // 플레이어와 충돌 시 데미지
            if (IsInLayerMask(collision.gameObject.layer, _playerLayerMask))
            {
                TryDealContactDamage(collision.gameObject);
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (_isDead) return;

            // 플레이어와 충돌 시 데미지 (트리거 콜라이더 사용 시)
            if (IsInLayerMask(other.gameObject.layer, _playerLayerMask))
            {
                TryDealContactDamage(other.gameObject);
            }
        }

        /// <summary>
        /// 접촉 데미지를 시도합니다.
        /// </summary>
        /// <param name="target">대상 GameObject</param>
        protected virtual void TryDealContactDamage(GameObject target)
        {
            if (_monsterData == null) return;

            // 쿨다운 체크
            if (Time.time - _lastAttackTime < _monsterData.AttackCooldown)
            {
                return;
            }

            // 플레이어에게 데미지
            var player = target.GetComponent<CharacterBase>();
            if (player != null && !player.IsDead)
            {
                player.TakeDamage(_monsterData.ContactDamage, gameObject);
                _lastAttackTime = Time.time;
            }
        }

        #endregion

        #region 유틸리티

        /// <summary>
        /// 레이어가 레이어마스크에 포함되는지 확인합니다.
        /// </summary>
        private bool IsInLayerMask(int layer, LayerMask layerMask)
        {
            return (layerMask.value & (1 << layer)) != 0;
        }

        #endregion
    }
}
