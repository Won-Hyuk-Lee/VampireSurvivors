using System;
using UnityEngine;
using VampireSurvivors.Utilities;

namespace VampireSurvivors.Character
{
    /// <summary>
    /// 플레이어 캐릭터 클래스.
    /// 사용자 입력을 처리하고 캐릭터를 이동시킵니다.
    /// </summary>
    /// <remarks>
    /// CharacterBase를 상속받아 플레이어 고유의 기능을 구현합니다.
    /// 경험치 시스템, 레벨업, 아이템 수집 등을 담당합니다.
    /// </remarks>
    public class Player : CharacterBase
    {
        #region 싱글톤 (선택적)

        /// <summary>
        /// 현재 활성화된 플레이어 인스턴스.
        /// 다른 시스템에서 플레이어에 쉽게 접근할 수 있도록 합니다.
        /// </summary>
        public static Player Instance { get; private set; }

        #endregion

        #region 경험치 및 레벨

        [Header("경험치/레벨")]
        [Tooltip("현재 레벨")]
        [SerializeField]
        private int _level = 1;

        [Tooltip("현재 경험치")]
        [SerializeField]
        private float _currentExp = 0f;

        [Tooltip("레벨업에 필요한 기본 경험치")]
        [SerializeField]
        private float _baseExpToLevelUp = 100f;

        [Tooltip("레벨당 필요 경험치 증가율")]
        [SerializeField]
        private float _expScalingFactor = 1.2f;

        /// <summary>
        /// 현재 레벨
        /// </summary>
        public int Level => _level;

        /// <summary>
        /// 현재 경험치
        /// </summary>
        public float CurrentExp => _currentExp;

        /// <summary>
        /// 다음 레벨업에 필요한 경험치
        /// </summary>
        public float ExpToNextLevel => _baseExpToLevelUp * Mathf.Pow(_expScalingFactor, _level - 1);

        /// <summary>
        /// 경험치 비율 (0~1)
        /// </summary>
        public float ExpRatio => ExpToNextLevel > 0 ? _currentExp / ExpToNextLevel : 0f;

        #endregion

        #region 이벤트

        /// <summary>
        /// 경험치 변경 시 발생 (현재 경험치, 필요 경험치)
        /// </summary>
        public event Action<float, float> OnExpChanged;

        /// <summary>
        /// 레벨업 시 발생 (새로운 레벨)
        /// </summary>
        public event Action<int> OnLevelUp;

        /// <summary>
        /// 아이템 수집 시 발생 (수집한 아이템)
        /// </summary>
        public event Action<GameObject> OnItemCollected;

        #endregion

        #region 입력 관련

        /// <summary>
        /// 마지막 입력 시간 (AFK 감지용)
        /// </summary>
        private float _lastInputTime;

        /// <summary>
        /// 입력 활성화 여부
        /// </summary>
        private bool _inputEnabled = true;

        #endregion

        #region Unity 생명주기

        protected override void Awake()
        {
            base.Awake();

            // 싱글톤 설정 (중복 방지)
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Debug.LogWarning("[Player] 중복 플레이어 인스턴스 감지. 파괴합니다.");
                Destroy(gameObject);
                return;
            }
        }

        protected override void Start()
        {
            base.Start();

            // 경험치 초기화 이벤트 발생
            OnExpChanged?.Invoke(_currentExp, ExpToNextLevel);

            // 게임 매니저에 플레이어 등록
            EventManager.TriggerEvent(GameEvents.PLAYER_SPAWNED, gameObject);
        }

        protected override void Update()
        {
            base.Update();

            if (_isDead) return;

            // 입력 처리
            if (_inputEnabled)
            {
                HandleInput();
            }
        }

        private void OnDestroy()
        {
            // 싱글톤 정리
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region 입력 처리

        /// <summary>
        /// 사용자 입력을 처리합니다.
        /// </summary>
        private void HandleInput()
        {
            // WASD 또는 화살표 키로 이동
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            Vector2 inputDirection = new Vector2(horizontal, vertical);

            // 입력이 있으면 마지막 입력 시간 갱신
            if (inputDirection.sqrMagnitude > 0.01f)
            {
                _lastInputTime = Time.time;
            }

            // 이동 방향 설정 (CharacterBase의 Move에서 처리)
            SetMoveDirection(inputDirection);
        }

        /// <summary>
        /// 입력을 활성화하거나 비활성화합니다.
        /// </summary>
        /// <param name="enabled">활성화 여부</param>
        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;

            // 입력 비활성화 시 이동 중지
            if (!enabled)
            {
                SetMoveDirection(Vector2.zero);
            }
        }

        #endregion

        #region 경험치 및 레벨업

        /// <summary>
        /// 경험치를 획득합니다.
        /// </summary>
        /// <param name="amount">획득할 경험치량</param>
        public void GainExp(float amount)
        {
            if (_isDead || amount <= 0) return;

            // 경험치 배율 적용
            float expMultiplier = _characterData != null ? _characterData.ExpMultiplier : 1f;
            float actualExp = amount * expMultiplier;

            _currentExp += actualExp;

            // 레벨업 체크
            while (_currentExp >= ExpToNextLevel)
            {
                LevelUp();
            }

            // 경험치 변경 이벤트
            OnExpChanged?.Invoke(_currentExp, ExpToNextLevel);
        }

        /// <summary>
        /// 레벨업을 처리합니다.
        /// </summary>
        private void LevelUp()
        {
            // 초과 경험치 이월
            _currentExp -= ExpToNextLevel;
            _level++;

            Debug.Log($"[Player] 레벨업! 현재 레벨: {_level}");

            // 레벨업 이벤트
            OnLevelUp?.Invoke(_level);
            EventManager.TriggerEvent(GameEvents.PLAYER_LEVEL_UP, _level);

            // 체력 완전 회복 (선택적)
            Heal(MaxHealth);
        }

        #endregion

        #region 아이템 수집

        /// <summary>
        /// 아이템 수집 범위를 반환합니다.
        /// </summary>
        public float PickupRange => _characterData != null ? _characterData.PickupRange : 1.5f;

        /// <summary>
        /// 아이템을 수집했을 때 호출됩니다.
        /// </summary>
        /// <param name="item">수집한 아이템</param>
        public void CollectItem(GameObject item)
        {
            if (item == null) return;

            OnItemCollected?.Invoke(item);
            EventManager.TriggerEvent(GameEvents.ITEM_COLLECTED, item);
        }

        #endregion

        #region 피격 및 사망 오버라이드

        protected override void OnTakeDamage(float damage, GameObject attacker)
        {
            base.OnTakeDamage(damage, attacker);

            // 플레이어 피격 이벤트
            EventManager.TriggerEvent(GameEvents.PLAYER_DAMAGED, damage);

            // 카메라 흔들림 등 추가 효과를 여기에 구현
        }

        protected override void OnDie()
        {
            Debug.Log("[Player] 플레이어 사망!");

            // 게임 오버 이벤트 발생
            EventManager.TriggerEvent(GameEvents.PLAYER_DIED);

            // 게임 매니저에 게임 오버 알림
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetGameState(GameState.GameOver);
            }

            // 부모 클래스의 OnDie는 호출하지 않음 (오브젝트 비활성화 방지)
            // 대신 사망 애니메이션 재생 등을 구현
        }

        #endregion

        #region 유틸리티

        /// <summary>
        /// 플레이어를 초기 상태로 리셋합니다.
        /// </summary>
        public void ResetPlayer()
        {
            _level = 1;
            _currentExp = 0f;
            _isDead = false;
            _isInvincible = false;
            _inputEnabled = true;

            InitializeStats();
            gameObject.SetActive(true);

            OnExpChanged?.Invoke(_currentExp, ExpToNextLevel);
        }

        /// <summary>
        /// 마지막 입력 이후 경과 시간을 반환합니다.
        /// </summary>
        public float TimeSinceLastInput => Time.time - _lastInputTime;

        #endregion
    }
}
