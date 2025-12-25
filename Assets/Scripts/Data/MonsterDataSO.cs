using UnityEngine;

namespace VampireSurvivors.Data
{
    /// <summary>
    /// 몬스터의 기본 스탯과 설정을 정의하는 ScriptableObject.
    /// 각 몬스터 종류마다 하나의 SO를 생성하여 사용합니다.
    /// </summary>
    /// <remarks>
    /// Unity 에디터에서 Create > VampireSurvivors > Monster Data 메뉴로 생성할 수 있습니다.
    /// </remarks>
    [CreateAssetMenu(fileName = "NewMonsterData", menuName = "VampireSurvivors/Monster Data", order = 1)]
    public class MonsterDataSO : ScriptableObject
    {
        [Header("기본 정보")]
        [Tooltip("몬스터의 고유 ID")]
        [SerializeField]
        private string _monsterId;

        [Tooltip("몬스터의 표시 이름")]
        [SerializeField]
        private string _displayName;

        [Tooltip("몬스터 설명")]
        [TextArea(2, 4)]
        [SerializeField]
        private string _description;

        [Header("외형")]
        [Tooltip("몬스터 스프라이트")]
        [SerializeField]
        private Sprite _sprite;

        [Tooltip("몬스터 프리팹 (스폰 시 사용)")]
        [SerializeField]
        private GameObject _prefab;

        [Tooltip("몬스터 크기 배율")]
        [Min(0.1f)]
        [SerializeField]
        private float _scale = 1f;

        [Header("체력")]
        [Tooltip("최대 체력")]
        [Min(1)]
        [SerializeField]
        private float _maxHealth = 10f;

        [Header("이동")]
        [Tooltip("기본 이동 속도")]
        [Min(0)]
        [SerializeField]
        private float _moveSpeed = 2f;

        [Header("공격")]
        [Tooltip("접촉 시 데미지")]
        [Min(0)]
        [SerializeField]
        private float _contactDamage = 10f;

        [Tooltip("공격 쿨다운 (연속 피해 방지)")]
        [Min(0)]
        [SerializeField]
        private float _attackCooldown = 1f;

        [Header("보상")]
        [Tooltip("처치 시 드롭하는 경험치")]
        [Min(0)]
        [SerializeField]
        private float _expReward = 10f;

        [Tooltip("골드 드롭 확률 (0~1)")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _goldDropChance = 0.1f;

        [Tooltip("드롭 골드량 (최소)")]
        [Min(0)]
        [SerializeField]
        private int _minGoldDrop = 1;

        [Tooltip("드롭 골드량 (최대)")]
        [Min(0)]
        [SerializeField]
        private int _maxGoldDrop = 5;

        [Header("특수 속성")]
        [Tooltip("넉백 저항력 (0 = 넉백 없음, 1 = 일반)")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _knockbackResistance = 1f;

        [Tooltip("엘리트 몬스터 여부")]
        [SerializeField]
        private bool _isElite = false;

        [Tooltip("보스 몬스터 여부")]
        [SerializeField]
        private bool _isBoss = false;

        #region Properties (읽기 전용 프로퍼티)

        /// <summary>
        /// 몬스터 고유 ID
        /// </summary>
        public string MonsterId => _monsterId;

        /// <summary>
        /// 몬스터 표시 이름
        /// </summary>
        public string DisplayName => _displayName;

        /// <summary>
        /// 몬스터 설명
        /// </summary>
        public string Description => _description;

        /// <summary>
        /// 몬스터 스프라이트
        /// </summary>
        public Sprite Sprite => _sprite;

        /// <summary>
        /// 몬스터 프리팹
        /// </summary>
        public GameObject Prefab => _prefab;

        /// <summary>
        /// 몬스터 크기 배율
        /// </summary>
        public float Scale => _scale;

        /// <summary>
        /// 최대 체력
        /// </summary>
        public float MaxHealth => _maxHealth;

        /// <summary>
        /// 기본 이동 속도
        /// </summary>
        public float MoveSpeed => _moveSpeed;

        /// <summary>
        /// 접촉 데미지
        /// </summary>
        public float ContactDamage => _contactDamage;

        /// <summary>
        /// 공격 쿨다운
        /// </summary>
        public float AttackCooldown => _attackCooldown;

        /// <summary>
        /// 경험치 보상
        /// </summary>
        public float ExpReward => _expReward;

        /// <summary>
        /// 골드 드롭 확률
        /// </summary>
        public float GoldDropChance => _goldDropChance;

        /// <summary>
        /// 최소 골드 드롭량
        /// </summary>
        public int MinGoldDrop => _minGoldDrop;

        /// <summary>
        /// 최대 골드 드롭량
        /// </summary>
        public int MaxGoldDrop => _maxGoldDrop;

        /// <summary>
        /// 넉백 저항력
        /// </summary>
        public float KnockbackResistance => _knockbackResistance;

        /// <summary>
        /// 엘리트 몬스터 여부
        /// </summary>
        public bool IsElite => _isElite;

        /// <summary>
        /// 보스 몬스터 여부
        /// </summary>
        public bool IsBoss => _isBoss;

        #endregion

        #region 유틸리티 메서드

        /// <summary>
        /// 랜덤 골드 드롭량을 계산합니다.
        /// </summary>
        /// <returns>드롭할 골드량 (0이면 드롭 안 함)</returns>
        public int CalculateGoldDrop()
        {
            if (Random.value > _goldDropChance)
            {
                return 0;
            }

            return Random.Range(_minGoldDrop, _maxGoldDrop + 1);
        }

        #endregion
    }
}
