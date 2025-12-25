using System;
using UnityEngine;
using VampireSurvivors.Data;
using VampireSurvivors.Utilities;

namespace VampireSurvivors.Character
{
    /// <summary>
    /// 모든 캐릭터(플레이어, 몬스터)의 기본 추상 클래스.
    /// 체력, 이동, 피격, 사망 등 공통 로직을 제공합니다.
    /// </summary>
    /// <remarks>
    /// 이 클래스를 상속받아 Player, Monster 등을 구현합니다.
    /// </remarks>
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class CharacterBase : MonoBehaviour
    {
        #region 인스펙터 필드

        [Header("캐릭터 데이터")]
        [Tooltip("캐릭터의 기본 스탯 데이터")]
        [SerializeField]
        protected CharacterDataSO _characterData;

        [Header("컴포넌트 참조")]
        [Tooltip("SpriteRenderer (스프라이트 방향 전환용)")]
        [SerializeField]
        protected SpriteRenderer _spriteRenderer;

        [Tooltip("Animator (애니메이션 제어용)")]
        [SerializeField]
        protected Animator _animator;

        #endregion

        #region 런타임 스탯

        /// <summary>
        /// 현재 체력
        /// </summary>
        protected float _currentHealth;

        /// <summary>
        /// 현재 이동 속도 (버프/디버프 적용)
        /// </summary>
        protected float _currentMoveSpeed;

        /// <summary>
        /// 현재 공격력 (버프/디버프 적용)
        /// </summary>
        protected float _currentAttackPower;

        /// <summary>
        /// 무적 상태 여부
        /// </summary>
        protected bool _isInvincible;

        /// <summary>
        /// 사망 상태 여부
        /// </summary>
        protected bool _isDead;

        /// <summary>
        /// 이동 방향 벡터
        /// </summary>
        protected Vector2 _moveDirection;

        /// <summary>
        /// 마지막으로 바라본 방향 (공격 방향 결정용)
        /// </summary>
        protected Vector2 _facingDirection = Vector2.right;

        #endregion

        #region 컴포넌트 캐시

        /// <summary>
        /// Rigidbody2D 컴포넌트
        /// </summary>
        protected Rigidbody2D _rigidbody;

        #endregion

        #region 이벤트

        /// <summary>
        /// 체력 변경 시 발생 (현재 체력, 최대 체력)
        /// </summary>
        public event Action<float, float> OnHealthChanged;

        /// <summary>
        /// 피격 시 발생 (받은 데미지, 공격자)
        /// </summary>
        public event Action<float, GameObject> OnDamaged;

        /// <summary>
        /// 사망 시 발생
        /// </summary>
        public event Action OnDeath;

        #endregion

        #region Properties

        /// <summary>
        /// 캐릭터 데이터 SO
        /// </summary>
        public CharacterDataSO CharacterData => _characterData;

        /// <summary>
        /// 현재 체력
        /// </summary>
        public float CurrentHealth => _currentHealth;

        /// <summary>
        /// 최대 체력
        /// </summary>
        public float MaxHealth => _characterData != null ? _characterData.MaxHealth : 100f;

        /// <summary>
        /// 체력 비율 (0~1)
        /// </summary>
        public float HealthRatio => MaxHealth > 0 ? _currentHealth / MaxHealth : 0f;

        /// <summary>
        /// 사망 상태 여부
        /// </summary>
        public bool IsDead => _isDead;

        /// <summary>
        /// 무적 상태 여부
        /// </summary>
        public bool IsInvincible => _isInvincible;

        /// <summary>
        /// 현재 이동 속도
        /// </summary>
        public float CurrentMoveSpeed => _currentMoveSpeed;

        /// <summary>
        /// 현재 공격력
        /// </summary>
        public float CurrentAttackPower => _currentAttackPower;

        /// <summary>
        /// 마지막으로 바라본 방향
        /// </summary>
        public Vector2 FacingDirection => _facingDirection;

        #endregion

        #region Unity 생명주기

        protected virtual void Awake()
        {
            // 필수 컴포넌트 캐싱
            _rigidbody = GetComponent<Rigidbody2D>();

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

        protected virtual void Start()
        {
            // 스탯 초기화
            InitializeStats();
        }

        protected virtual void Update()
        {
            // 사망 상태면 업데이트 중단
            if (_isDead) return;

            // 체력 재생
            if (_characterData != null && _characterData.HealthRegen > 0)
            {
                Heal(_characterData.HealthRegen * Time.deltaTime);
            }
        }

        protected virtual void FixedUpdate()
        {
            // 사망 상태면 이동 중단
            if (_isDead) return;

            // 이동 처리
            Move();
        }

        #endregion

        #region 초기화

        /// <summary>
        /// 캐릭터 스탯을 초기화합니다.
        /// </summary>
        protected virtual void InitializeStats()
        {
            if (_characterData == null)
            {
                Debug.LogWarning($"[CharacterBase] {name}에 CharacterData가 할당되지 않았습니다.");
                _currentHealth = 100f;
                _currentMoveSpeed = 5f;
                _currentAttackPower = 1f;
                return;
            }

            _currentHealth = _characterData.MaxHealth;
            _currentMoveSpeed = _characterData.MoveSpeed;
            _currentAttackPower = _characterData.AttackPower;
            _isDead = false;
            _isInvincible = false;

            // 체력 초기화 이벤트 발생
            OnHealthChanged?.Invoke(_currentHealth, MaxHealth);
        }

        /// <summary>
        /// 캐릭터 데이터를 설정하고 스탯을 초기화합니다.
        /// </summary>
        /// <param name="data">적용할 캐릭터 데이터</param>
        public virtual void SetCharacterData(CharacterDataSO data)
        {
            _characterData = data;
            InitializeStats();
        }

        #endregion

        #region 이동

        /// <summary>
        /// 캐릭터를 이동시킵니다. FixedUpdate에서 호출됩니다.
        /// </summary>
        protected virtual void Move()
        {
            if (_moveDirection.sqrMagnitude > 0.01f)
            {
                // Rigidbody2D를 사용한 이동
                Vector2 velocity = _moveDirection.normalized * _currentMoveSpeed;
                _rigidbody.linearVelocity = velocity;

                // 바라보는 방향 업데이트
                _facingDirection = _moveDirection.normalized;

                // 스프라이트 방향 전환
                UpdateSpriteDirection();
            }
            else
            {
                // 정지
                _rigidbody.linearVelocity = Vector2.zero;
            }
        }

        /// <summary>
        /// 이동 방향을 설정합니다.
        /// </summary>
        /// <param name="direction">이동 방향 벡터</param>
        public virtual void SetMoveDirection(Vector2 direction)
        {
            _moveDirection = direction;
        }

        /// <summary>
        /// 스프라이트의 좌우 방향을 업데이트합니다.
        /// </summary>
        protected virtual void UpdateSpriteDirection()
        {
            if (_spriteRenderer == null) return;

            // 왼쪽으로 이동 시 스프라이트 뒤집기
            if (_facingDirection.x < -0.01f)
            {
                _spriteRenderer.flipX = true;
            }
            else if (_facingDirection.x > 0.01f)
            {
                _spriteRenderer.flipX = false;
            }
        }

        #endregion

        #region 체력 관리

        /// <summary>
        /// 데미지를 받습니다.
        /// </summary>
        /// <param name="damage">받을 데미지량</param>
        /// <param name="attacker">공격자 GameObject (null 가능)</param>
        /// <returns>실제로 받은 데미지량</returns>
        public virtual float TakeDamage(float damage, GameObject attacker = null)
        {
            // 사망 또는 무적 상태면 데미지 무시
            if (_isDead || _isInvincible) return 0f;

            // 방어력 적용
            float defense = _characterData != null ? _characterData.Defense : 0f;
            float actualDamage = Mathf.Max(0, damage - defense);

            // 체력 감소
            _currentHealth = Mathf.Max(0, _currentHealth - actualDamage);

            // 이벤트 발생
            OnHealthChanged?.Invoke(_currentHealth, MaxHealth);
            OnDamaged?.Invoke(actualDamage, attacker);

            // 피격 효과 (무적 시간, 시각적 피드백 등)
            OnTakeDamage(actualDamage, attacker);

            // 사망 체크
            if (_currentHealth <= 0)
            {
                Die();
            }

            return actualDamage;
        }

        /// <summary>
        /// 피격 시 호출되는 가상 메서드.
        /// 자식 클래스에서 피격 효과를 구현합니다.
        /// </summary>
        /// <param name="damage">받은 데미지</param>
        /// <param name="attacker">공격자</param>
        protected virtual void OnTakeDamage(float damage, GameObject attacker)
        {
            // 무적 시간 적용
            if (_characterData != null && _characterData.InvincibilityDuration > 0)
            {
                StartInvincibility(_characterData.InvincibilityDuration);
            }
        }

        /// <summary>
        /// 체력을 회복합니다.
        /// </summary>
        /// <param name="amount">회복량</param>
        /// <returns>실제 회복량</returns>
        public virtual float Heal(float amount)
        {
            if (_isDead || amount <= 0) return 0f;

            float previousHealth = _currentHealth;
            _currentHealth = Mathf.Min(_currentHealth + amount, MaxHealth);
            float actualHeal = _currentHealth - previousHealth;

            if (actualHeal > 0)
            {
                OnHealthChanged?.Invoke(_currentHealth, MaxHealth);
            }

            return actualHeal;
        }

        /// <summary>
        /// 무적 상태를 시작합니다.
        /// </summary>
        /// <param name="duration">무적 지속 시간</param>
        public virtual void StartInvincibility(float duration)
        {
            if (duration <= 0) return;

            _isInvincible = true;
            StartCoroutine(InvincibilityCoroutine(duration));
        }

        /// <summary>
        /// 무적 시간을 처리하는 코루틴
        /// </summary>
        private System.Collections.IEnumerator InvincibilityCoroutine(float duration)
        {
            // 깜빡임 효과
            float elapsed = 0f;
            float blinkInterval = 0.1f;

            while (elapsed < duration)
            {
                if (_spriteRenderer != null)
                {
                    _spriteRenderer.enabled = !_spriteRenderer.enabled;
                }
                yield return new WaitForSeconds(blinkInterval);
                elapsed += blinkInterval;
            }

            // 무적 종료
            _isInvincible = false;
            if (_spriteRenderer != null)
            {
                _spriteRenderer.enabled = true;
            }
        }

        #endregion

        #region 사망

        /// <summary>
        /// 캐릭터가 사망합니다.
        /// </summary>
        protected virtual void Die()
        {
            if (_isDead) return;

            _isDead = true;
            _rigidbody.linearVelocity = Vector2.zero;

            // 사망 이벤트 발생
            OnDeath?.Invoke();

            // 사망 처리 (자식 클래스에서 구현)
            OnDie();
        }

        /// <summary>
        /// 사망 시 호출되는 가상 메서드.
        /// 자식 클래스에서 사망 효과를 구현합니다.
        /// </summary>
        protected virtual void OnDie()
        {
            // 기본 구현: 오브젝트 비활성화
            gameObject.SetActive(false);
        }

        #endregion

        #region 스탯 수정

        /// <summary>
        /// 이동 속도 배율을 적용합니다.
        /// </summary>
        /// <param name="multiplier">속도 배율</param>
        public virtual void ApplySpeedMultiplier(float multiplier)
        {
            if (_characterData != null)
            {
                _currentMoveSpeed = _characterData.MoveSpeed * multiplier;
            }
        }

        /// <summary>
        /// 공격력 배율을 적용합니다.
        /// </summary>
        /// <param name="multiplier">공격력 배율</param>
        public virtual void ApplyAttackMultiplier(float multiplier)
        {
            if (_characterData != null)
            {
                _currentAttackPower = _characterData.AttackPower * multiplier;
            }
        }

        /// <summary>
        /// 모든 스탯을 기본값으로 리셋합니다.
        /// </summary>
        public virtual void ResetStats()
        {
            InitializeStats();
        }

        #endregion
    }
}
