using UnityEngine;
using UnityEngine.Events;

namespace VampireSurvivors.Core
{
    /// <summary>
    /// 오브젝트 풀링을 지원하는 기본 MonoBehaviour 클래스.
    /// IPoolable 인터페이스를 구현하며, 풀에서 오브젝트를 관리하는 데 필요한 기능을 제공합니다.
    /// </summary>
    /// <remarks>
    /// 이 클래스를 상속받아 풀링이 필요한 오브젝트를 구현하거나,
    /// 직접 이 컴포넌트를 추가하여 사용할 수 있습니다.
    /// </remarks>
    /// <example>
    /// // 상속하여 사용
    /// public class Bullet : PoolableObject
    /// {
    ///     protected override void OnPoolSpawn()
    ///     {
    ///         base.OnPoolSpawn();
    ///         // 총알 초기화 로직
    ///     }
    /// }
    ///
    /// // 또는 컴포넌트로 추가하여 이벤트 사용
    /// poolableObject.OnSpawnEvent.AddListener(OnBulletSpawn);
    /// </example>
    public class PoolableObject : MonoBehaviour, IPoolable
    {
        /// <summary>
        /// 이 오브젝트가 속한 풀의 키.
        /// PoolManager에서 자동으로 설정됩니다.
        /// </summary>
        [Header("풀 설정")]
        [Tooltip("이 오브젝트가 속한 풀의 키 (자동 설정됨)")]
        [SerializeField]
        private string _poolKey;

        /// <summary>
        /// 오브젝트가 풀에서 꺼내질 때 발생하는 이벤트
        /// </summary>
        [Header("이벤트")]
        [Tooltip("오브젝트가 풀에서 꺼내질 때 호출됩니다")]
        public UnityEvent OnSpawnEvent;

        /// <summary>
        /// 오브젝트가 풀로 반환될 때 발생하는 이벤트
        /// </summary>
        [Tooltip("오브젝트가 풀로 반환될 때 호출됩니다")]
        public UnityEvent OnDespawnEvent;

        /// <summary>
        /// 오브젝트가 현재 활성 상태인지 (풀에서 사용 중인지)
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// 이 오브젝트가 속한 풀의 키
        /// </summary>
        public string PoolKey
        {
            get => _poolKey;
            set => _poolKey = value;
        }

        /// <summary>
        /// IPoolable.OnSpawn 구현.
        /// 오브젝트가 풀에서 꺼내질 때 호출됩니다.
        /// </summary>
        public void OnSpawn()
        {
            IsActive = true;
            gameObject.SetActive(true);

            // 가상 메서드 호출 (자식 클래스에서 오버라이드 가능)
            OnPoolSpawn();

            // Unity 이벤트 발생
            OnSpawnEvent?.Invoke();
        }

        /// <summary>
        /// IPoolable.OnDespawn 구현.
        /// 오브젝트가 풀로 반환될 때 호출됩니다.
        /// </summary>
        public void OnDespawn()
        {
            IsActive = false;

            // 가상 메서드 호출 (자식 클래스에서 오버라이드 가능)
            OnPoolDespawn();

            // Unity 이벤트 발생
            OnDespawnEvent?.Invoke();

            gameObject.SetActive(false);
        }

        /// <summary>
        /// 오브젝트가 풀에서 꺼내질 때 호출되는 가상 메서드.
        /// 자식 클래스에서 오버라이드하여 초기화 로직을 구현합니다.
        /// </summary>
        protected virtual void OnPoolSpawn()
        {
            // 자식 클래스에서 구현
        }

        /// <summary>
        /// 오브젝트가 풀로 반환될 때 호출되는 가상 메서드.
        /// 자식 클래스에서 오버라이드하여 정리 로직을 구현합니다.
        /// </summary>
        protected virtual void OnPoolDespawn()
        {
            // 자식 클래스에서 구현
        }

        /// <summary>
        /// 이 오브젝트를 풀로 반환합니다.
        /// PoolManager를 통해 간편하게 반환할 수 있는 헬퍼 메서드입니다.
        /// </summary>
        public void ReturnToPool()
        {
            if (string.IsNullOrEmpty(_poolKey))
            {
                Debug.LogWarning($"[PoolableObject] {name}의 PoolKey가 설정되지 않았습니다.");
                gameObject.SetActive(false);
                return;
            }

            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Return(this);
            }
            else
            {
                Debug.LogWarning("[PoolableObject] PoolManager 인스턴스를 찾을 수 없습니다.");
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 지정된 시간 후에 오브젝트를 풀로 반환합니다.
        /// </summary>
        /// <param name="delay">반환까지의 지연 시간 (초)</param>
        public void ReturnToPoolAfterDelay(float delay)
        {
            if (delay <= 0)
            {
                ReturnToPool();
                return;
            }

            // Coroutine 대신 Timer 사용 (코루틴 없는 방식)
            StartCoroutine(ReturnAfterDelayCoroutine(delay));
        }

        /// <summary>
        /// 지연 반환을 위한 코루틴
        /// </summary>
        private System.Collections.IEnumerator ReturnAfterDelayCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (IsActive) // 아직 활성 상태일 때만 반환
            {
                ReturnToPool();
            }
        }
    }
}
