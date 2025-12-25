using System;
using System.Collections.Generic;
using UnityEngine;
using VampireSurvivors.Utilities;

namespace VampireSurvivors.Core
{
    /// <summary>
    /// 오브젝트 풀 설정 데이터.
    /// Inspector에서 풀 설정을 편집할 때 사용합니다.
    /// </summary>
    [Serializable]
    public class PoolConfig
    {
        /// <summary>
        /// 풀을 식별하는 고유 키
        /// </summary>
        [Tooltip("풀을 식별하는 고유 키")]
        public string key;

        /// <summary>
        /// 풀링할 프리팹
        /// </summary>
        [Tooltip("풀링할 프리팹")]
        public GameObject prefab;

        /// <summary>
        /// 초기 풀 크기
        /// </summary>
        [Tooltip("게임 시작 시 미리 생성할 오브젝트 수")]
        [Min(0)]
        public int initialSize = 10;

        /// <summary>
        /// 풀이 비었을 때 자동 확장 여부
        /// </summary>
        [Tooltip("풀이 비었을 때 자동으로 확장할지 여부")]
        public bool autoExpand = true;

        /// <summary>
        /// 자동 확장 시 한 번에 생성할 오브젝트 수
        /// </summary>
        [Tooltip("자동 확장 시 한 번에 생성할 오브젝트 수")]
        [Min(1)]
        public int expandCount = 5;
    }

    /// <summary>
    /// 여러 오브젝트 풀을 중앙에서 관리하는 싱글톤 매니저.
    /// 풀을 키로 접근하고, 런타임에 동적으로 풀을 생성/삭제할 수 있습니다.
    /// </summary>
    /// <example>
    /// // Inspector에서 설정하거나 런타임에 풀 생성
    /// PoolManager.Instance.CreatePool("Bullet", bulletPrefab, 20);
    ///
    /// // 풀에서 오브젝트 가져오기
    /// GameObject bullet = PoolManager.Instance.Get("Bullet");
    ///
    /// // 풀로 오브젝트 반환
    /// PoolManager.Instance.Return("Bullet", bullet);
    /// </example>
    public class PoolManager : Singleton<PoolManager>
    {
        /// <summary>
        /// Inspector에서 설정할 풀 목록
        /// </summary>
        [Header("풀 설정")]
        [Tooltip("게임 시작 시 생성할 풀 목록")]
        [SerializeField]
        private List<PoolConfig> _poolConfigs = new List<PoolConfig>();

        /// <summary>
        /// 키로 풀을 관리하는 딕셔너리
        /// </summary>
        private readonly Dictionary<string, ObjectPool<PoolableObject>> _pools = new Dictionary<string, ObjectPool<PoolableObject>>();

        /// <summary>
        /// 풀 오브젝트들을 담을 부모 Transform
        /// </summary>
        private Transform _poolContainer;

        /// <summary>
        /// 싱글톤 초기화 시 호출됩니다.
        /// Inspector에서 설정된 풀들을 생성합니다.
        /// </summary>
        protected override void OnSingletonAwake()
        {
            // 풀 컨테이너 생성
            var containerObject = new GameObject("PoolContainer");
            containerObject.transform.SetParent(transform);
            _poolContainer = containerObject.transform;

            // Inspector에서 설정된 풀들 초기화
            InitializePools();
        }

        /// <summary>
        /// Inspector에서 설정된 풀들을 초기화합니다.
        /// </summary>
        private void InitializePools()
        {
            foreach (var config in _poolConfigs)
            {
                if (string.IsNullOrEmpty(config.key))
                {
                    Debug.LogError("[PoolManager] 풀 키가 비어있습니다.");
                    continue;
                }

                if (config.prefab == null)
                {
                    Debug.LogError($"[PoolManager] '{config.key}' 풀의 프리팹이 null입니다.");
                    continue;
                }

                CreatePool(config.key, config.prefab, config.initialSize, config.autoExpand, config.expandCount);
            }
        }

        /// <summary>
        /// 새 오브젝트 풀을 생성합니다.
        /// </summary>
        /// <param name="key">풀을 식별할 고유 키</param>
        /// <param name="prefab">풀링할 프리팹</param>
        /// <param name="initialSize">초기 풀 크기</param>
        /// <param name="autoExpand">자동 확장 여부</param>
        /// <param name="expandCount">확장 시 생성할 개수</param>
        /// <returns>풀 생성 성공 여부</returns>
        public bool CreatePool(string key, GameObject prefab, int initialSize = 10, bool autoExpand = true, int expandCount = 5)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("[PoolManager] 풀 키가 비어있습니다.");
                return false;
            }

            if (_pools.ContainsKey(key))
            {
                Debug.LogWarning($"[PoolManager] '{key}' 풀이 이미 존재합니다.");
                return false;
            }

            if (prefab == null)
            {
                Debug.LogError($"[PoolManager] '{key}' 풀의 프리팹이 null입니다.");
                return false;
            }

            // PoolableObject 컴포넌트 확인 및 추가
            var poolableComponent = prefab.GetComponent<PoolableObject>();
            if (poolableComponent == null)
            {
                Debug.LogWarning($"[PoolManager] '{key}' 프리팹에 PoolableObject 컴포넌트가 없어 자동 추가합니다.");
                poolableComponent = prefab.AddComponent<PoolableObject>();
            }

            // 풀 부모 오브젝트 생성
            var poolParent = new GameObject($"Pool_{key}");
            poolParent.transform.SetParent(_poolContainer);

            // 풀 생성
            var pool = new ObjectPool<PoolableObject>(
                poolableComponent,
                initialSize,
                poolParent.transform,
                autoExpand,
                expandCount
            );

            _pools.Add(key, pool);
            Debug.Log($"[PoolManager] '{key}' 풀 생성 완료 (초기 크기: {initialSize})");

            return true;
        }

        /// <summary>
        /// 풀에서 오브젝트를 가져옵니다.
        /// </summary>
        /// <param name="key">풀 키</param>
        /// <returns>풀에서 꺼낸 GameObject 또는 null</returns>
        public GameObject Get(string key)
        {
            if (!TryGetPool(key, out var pool))
            {
                return null;
            }

            var obj = pool.Get();
            return obj != null ? obj.gameObject : null;
        }

        /// <summary>
        /// 풀에서 오브젝트를 가져와 지정된 위치와 회전으로 설정합니다.
        /// </summary>
        /// <param name="key">풀 키</param>
        /// <param name="position">설정할 위치</param>
        /// <param name="rotation">설정할 회전</param>
        /// <returns>풀에서 꺼낸 GameObject 또는 null</returns>
        public GameObject Get(string key, Vector3 position, Quaternion rotation)
        {
            if (!TryGetPool(key, out var pool))
            {
                return null;
            }

            var obj = pool.Get(position, rotation);
            return obj != null ? obj.gameObject : null;
        }

        /// <summary>
        /// 풀에서 오브젝트를 가져와 특정 컴포넌트를 반환합니다.
        /// </summary>
        /// <typeparam name="T">가져올 컴포넌트 타입</typeparam>
        /// <param name="key">풀 키</param>
        /// <returns>요청한 컴포넌트 또는 null</returns>
        public T Get<T>(string key) where T : Component
        {
            var obj = Get(key);
            return obj != null ? obj.GetComponent<T>() : null;
        }

        /// <summary>
        /// 풀에서 오브젝트를 가져와 특정 컴포넌트를 위치/회전과 함께 반환합니다.
        /// </summary>
        /// <typeparam name="T">가져올 컴포넌트 타입</typeparam>
        /// <param name="key">풀 키</param>
        /// <param name="position">설정할 위치</param>
        /// <param name="rotation">설정할 회전</param>
        /// <returns>요청한 컴포넌트 또는 null</returns>
        public T Get<T>(string key, Vector3 position, Quaternion rotation) where T : Component
        {
            var obj = Get(key, position, rotation);
            return obj != null ? obj.GetComponent<T>() : null;
        }

        /// <summary>
        /// 오브젝트를 풀로 반환합니다.
        /// </summary>
        /// <param name="key">풀 키</param>
        /// <param name="obj">반환할 GameObject</param>
        public void Return(string key, GameObject obj)
        {
            if (obj == null)
            {
                Debug.LogWarning("[PoolManager] null 오브젝트를 반환하려고 했습니다.");
                return;
            }

            if (!TryGetPool(key, out var pool))
            {
                return;
            }

            var poolable = obj.GetComponent<PoolableObject>();
            if (poolable == null)
            {
                Debug.LogWarning($"[PoolManager] {obj.name}에 PoolableObject 컴포넌트가 없습니다.");
                return;
            }

            pool.Return(poolable);
        }

        /// <summary>
        /// PoolableObject를 사용하여 오브젝트를 풀로 반환합니다.
        /// PoolableObject가 자신의 풀 키를 알고 있을 때 사용합니다.
        /// </summary>
        /// <param name="poolable">반환할 PoolableObject</param>
        public void Return(PoolableObject poolable)
        {
            if (poolable == null)
            {
                Debug.LogWarning("[PoolManager] null PoolableObject를 반환하려고 했습니다.");
                return;
            }

            Return(poolable.PoolKey, poolable.gameObject);
        }

        /// <summary>
        /// 특정 풀의 모든 활성 오브젝트를 반환합니다.
        /// </summary>
        /// <param name="key">풀 키</param>
        public void ReturnAll(string key)
        {
            if (TryGetPool(key, out var pool))
            {
                pool.ReturnAll();
            }
        }

        /// <summary>
        /// 모든 풀의 모든 활성 오브젝트를 반환합니다.
        /// </summary>
        public void ReturnAllPools()
        {
            foreach (var pool in _pools.Values)
            {
                pool.ReturnAll();
            }
        }

        /// <summary>
        /// 특정 풀을 삭제합니다.
        /// </summary>
        /// <param name="key">풀 키</param>
        public void RemovePool(string key)
        {
            if (TryGetPool(key, out var pool))
            {
                pool.Clear();
                _pools.Remove(key);
                Debug.Log($"[PoolManager] '{key}' 풀 삭제 완료");
            }
        }

        /// <summary>
        /// 모든 풀을 삭제합니다.
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var pool in _pools.Values)
            {
                pool.Clear();
            }
            _pools.Clear();
            Debug.Log("[PoolManager] 모든 풀 삭제 완료");
        }

        /// <summary>
        /// 풀이 존재하는지 확인합니다.
        /// </summary>
        /// <param name="key">풀 키</param>
        /// <returns>풀 존재 여부</returns>
        public bool HasPool(string key)
        {
            return _pools.ContainsKey(key);
        }

        /// <summary>
        /// 특정 풀의 정보를 가져옵니다.
        /// </summary>
        /// <param name="key">풀 키</param>
        /// <param name="available">사용 가능한 오브젝트 수</param>
        /// <param name="total">전체 오브젝트 수</param>
        /// <param name="active">활성 오브젝트 수</param>
        /// <returns>풀 존재 여부</returns>
        public bool GetPoolInfo(string key, out int available, out int total, out int active)
        {
            if (TryGetPool(key, out var pool))
            {
                available = pool.AvailableCount;
                total = pool.TotalCount;
                active = pool.ActiveCount;
                return true;
            }

            available = 0;
            total = 0;
            active = 0;
            return false;
        }

        /// <summary>
        /// 풀을 가져오려 시도합니다.
        /// </summary>
        /// <param name="key">풀 키</param>
        /// <param name="pool">찾은 풀</param>
        /// <returns>풀 존재 여부</returns>
        private bool TryGetPool(string key, out ObjectPool<PoolableObject> pool)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("[PoolManager] 풀 키가 비어있습니다.");
                pool = null;
                return false;
            }

            if (!_pools.TryGetValue(key, out pool))
            {
                Debug.LogError($"[PoolManager] '{key}' 풀을 찾을 수 없습니다.");
                return false;
            }

            return true;
        }
    }
}
