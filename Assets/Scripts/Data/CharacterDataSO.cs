using UnityEngine;

namespace VampireSurvivors.Data
{
    /// <summary>
    /// 캐릭터의 기본 스탯과 설정을 정의하는 ScriptableObject.
    /// 플레이어와 몬스터 모두 이 데이터를 기반으로 스탯을 적용받습니다.
    /// </summary>
    /// <remarks>
    /// Unity 에디터에서 Create > VampireSurvivors > Character Data 메뉴로 생성할 수 있습니다.
    /// </remarks>
    [CreateAssetMenu(fileName = "NewCharacterData", menuName = "VampireSurvivors/Character Data", order = 0)]
    public class CharacterDataSO : ScriptableObject
    {
        [Header("기본 정보")]
        [Tooltip("캐릭터의 표시 이름")]
        [SerializeField]
        private string _displayName;

        [Tooltip("캐릭터 설명")]
        [TextArea(2, 4)]
        [SerializeField]
        private string _description;

        [Tooltip("캐릭터 아이콘 (UI용)")]
        [SerializeField]
        private Sprite _icon;

        [Header("체력")]
        [Tooltip("최대 체력")]
        [Min(1)]
        [SerializeField]
        private float _maxHealth = 100f;

        [Tooltip("초당 체력 회복량")]
        [Min(0)]
        [SerializeField]
        private float _healthRegen = 0f;

        [Header("이동")]
        [Tooltip("기본 이동 속도")]
        [Min(0)]
        [SerializeField]
        private float _moveSpeed = 5f;

        [Header("공격")]
        [Tooltip("기본 공격력 (데미지 배율)")]
        [Min(0)]
        [SerializeField]
        private float _attackPower = 1f;

        [Tooltip("공격 속도 배율 (1 = 기본, 2 = 2배 빠름)")]
        [Min(0.1f)]
        [SerializeField]
        private float _attackSpeed = 1f;

        [Tooltip("치명타 확률 (0~1, 0.1 = 10%)")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _criticalChance = 0.05f;

        [Tooltip("치명타 데미지 배율 (1.5 = 150% 데미지)")]
        [Min(1f)]
        [SerializeField]
        private float _criticalMultiplier = 1.5f;

        [Header("방어")]
        [Tooltip("방어력 (받는 데미지 감소량)")]
        [Min(0)]
        [SerializeField]
        private float _defense = 0f;

        [Tooltip("피격 후 무적 시간 (초)")]
        [Min(0)]
        [SerializeField]
        private float _invincibilityDuration = 0.5f;

        [Header("기타")]
        [Tooltip("획득 경험치 배율")]
        [Min(0)]
        [SerializeField]
        private float _expMultiplier = 1f;

        [Tooltip("아이템 획득 범위")]
        [Min(0)]
        [SerializeField]
        private float _pickupRange = 1.5f;

        [Tooltip("투사체 개수 추가")]
        [Min(0)]
        [SerializeField]
        private int _projectileCountBonus = 0;

        #region Properties (읽기 전용 프로퍼티)

        /// <summary>
        /// 캐릭터의 표시 이름
        /// </summary>
        public string DisplayName => _displayName;

        /// <summary>
        /// 캐릭터 설명
        /// </summary>
        public string Description => _description;

        /// <summary>
        /// 캐릭터 아이콘
        /// </summary>
        public Sprite Icon => _icon;

        /// <summary>
        /// 최대 체력
        /// </summary>
        public float MaxHealth => _maxHealth;

        /// <summary>
        /// 초당 체력 회복량
        /// </summary>
        public float HealthRegen => _healthRegen;

        /// <summary>
        /// 기본 이동 속도
        /// </summary>
        public float MoveSpeed => _moveSpeed;

        /// <summary>
        /// 기본 공격력
        /// </summary>
        public float AttackPower => _attackPower;

        /// <summary>
        /// 공격 속도 배율
        /// </summary>
        public float AttackSpeed => _attackSpeed;

        /// <summary>
        /// 치명타 확률
        /// </summary>
        public float CriticalChance => _criticalChance;

        /// <summary>
        /// 치명타 데미지 배율
        /// </summary>
        public float CriticalMultiplier => _criticalMultiplier;

        /// <summary>
        /// 방어력
        /// </summary>
        public float Defense => _defense;

        /// <summary>
        /// 피격 후 무적 시간
        /// </summary>
        public float InvincibilityDuration => _invincibilityDuration;

        /// <summary>
        /// 경험치 획득 배율
        /// </summary>
        public float ExpMultiplier => _expMultiplier;

        /// <summary>
        /// 아이템 획득 범위
        /// </summary>
        public float PickupRange => _pickupRange;

        /// <summary>
        /// 투사체 개수 추가
        /// </summary>
        public int ProjectileCountBonus => _projectileCountBonus;

        #endregion
    }
}
