using UnityEngine;

namespace VampireSurvivors.Monster
{
    /// <summary>
    /// 기본 몬스터 클래스.
    /// 단순히 플레이어를 추적하고 접촉 데미지를 주는 일반적인 몬스터입니다.
    /// </summary>
    /// <remarks>
    /// MonsterBase를 상속받아 기본 동작을 그대로 사용합니다.
    /// 특수한 동작이 필요한 몬스터는 이 클래스를 상속받거나
    /// MonsterBase를 직접 상속받아 구현합니다.
    /// </remarks>
    public class BasicMonster : MonsterBase
    {
        #region 인스펙터 필드

        [Header("기본 몬스터 설정")]
        [Tooltip("사망 시 파티클 효과 프리팹")]
        [SerializeField]
        private GameObject _deathEffectPrefab;

        [Tooltip("피격 시 사운드 효과")]
        [SerializeField]
        private AudioClip _hitSound;

        [Tooltip("사망 시 사운드 효과")]
        [SerializeField]
        private AudioClip _deathSound;

        #endregion

        #region 오버라이드 메서드

        /// <summary>
        /// 피격 시 추가 효과를 적용합니다.
        /// </summary>
        protected override void OnTakeDamage(float damage, GameObject attacker)
        {
            base.OnTakeDamage(damage, attacker);

            // 피격 사운드 재생
            if (_hitSound != null)
            {
                PlaySound(_hitSound);
            }
        }

        /// <summary>
        /// 사망 시 추가 효과를 적용합니다.
        /// </summary>
        protected override void OnDie()
        {
            // 사망 이펙트 생성
            if (_deathEffectPrefab != null)
            {
                Instantiate(_deathEffectPrefab, transform.position, Quaternion.identity);
            }

            // 사망 사운드 재생
            if (_deathSound != null)
            {
                PlaySound(_deathSound);
            }

            base.OnDie();
        }

        #endregion

        #region 유틸리티

        /// <summary>
        /// 사운드를 재생합니다.
        /// AudioManager가 있으면 사용하고, 없으면 AudioSource.PlayClipAtPoint 사용
        /// </summary>
        /// <param name="clip">재생할 오디오 클립</param>
        private void PlaySound(AudioClip clip)
        {
            if (clip == null) return;

            // AudioManager 사용 (있는 경우)
            if (Utilities.AudioManager.Instance != null)
            {
                Utilities.AudioManager.Instance.PlaySFX(clip);
            }
            else
            {
                // 폴백: 위치 기반 사운드 재생
                AudioSource.PlayClipAtPoint(clip, transform.position);
            }
        }

        #endregion
    }
}
