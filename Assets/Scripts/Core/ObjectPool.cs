using System;
using System.Collections.Generic;
using UnityEngine;

namespace VampireSurvivors.Core
{
    /// <summary>
    /// 제네릭 오브젝트 풀 클래스.
    /// MonoBehaviour 컴포넌트를 가진 오브젝트들을 효율적으로 재사용할 수 있도록 관리합니다.
    /// </summary>
    /// <typeparam name="T">풀링할 MonoBehaviour 타입. IPoolable 인터페이스 구현 권장</typeparam>
    /// <example>
    /// // 풀 생성
    /// var bulletPool = new ObjectPool&lt;Bullet&gt;(bulletPrefab, 20);
    ///
    /// // 오브젝트 가져오기
    /// Bullet bullet = bulletPool.Get();
    ///
    /// // 오브젝트 반환
    /// bulletPool.Return(bullet);
    /// </example>
    public class ObjectPool<T> where T : Component
    {
        /// <summary>
        /// 비활성화된 오브젝트들을 저장하는 스택
        /// </summary>
        private readonly Stack<T> _pool;

        /// <summary>
        /// 풀에서 생성한 모든 오브젝트 목록 (활성 + 비활성)
        /// </summary>
        private readonly List<T> _allObjects;

        /// <summary>
        /// 오브젝트 생성에 사용할 프리팹
        /// </summary>
        private readonly T _prefab;

        /// <summary>
        /// 풀링된 오브젝트들의 부모 Transform (정리용)
        /// </summary>
        private readonly Transform _parent;

        /// <summary>
        /// 풀이 비었을 때 자동으로 확장할지 여부
        /// </summary>
        private readonly bool _autoExpand;

        /// <summary>
        /// 자동 확장 시 한 번에 생성할 오브젝트 수
        /// </summary>
        private readonly int _expandCount;

        /// <summary>
        /// 현재 풀에서 대기 중인 오브젝트 수
        /// </summary>
        public int AvailableCount => _pool.Count;

        /// <summary>
        /// 풀이 생성한 총 오브젝트 수
        /// </summary>
        public int TotalCount => _allObjects.Count;

        /// <summary>
        /// 현재 활성화되어 사용 중인 오브젝트 수
        /// </summary>
        public int ActiveCount => TotalCount - AvailableCount;

        /// <summary>
        /// 오브젝트 풀을 생성합니다.
        /// </summary>
        /// <param name="prefab">오브젝트 생성에 사용할 프리팹</param>
        /// <param name="initialSize">초기 풀 크기</param>
        /// <param name="parent">풀 오브젝트들의 부모 Transform (null이면 새로 생성)</param>
        /// <param name="autoExpand">풀이 비었을 때 자동 확장 여부 (기본값: true)</param>
        /// <param name="expandCount">자동 확장 시 생성할 오브젝트 수 (기본값: 5)</param>
        /// <exception cref="ArgumentNullException">prefab이 null인 경우</exception>
        public ObjectPool(T prefab, int initialSize, Transform parent = null, bool autoExpand = true, int expandCount = 5)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab), "풀 프리팹이 null입니다.");
            }

            _prefab = prefab;
            _autoExpand = autoExpand;
            _expandCount = Mathf.Max(1, expandCount);
            _pool = new Stack<T>(initialSize);
            _allObjects = new List<T>(initialSize);

            // 부모 Transform 설정 또는 생성
            if (parent != null)
            {
                _parent = parent;
            }
            else
            {
                var parentObject = new GameObject($"Pool_{typeof(T).Name}");
                _parent = parentObject.transform;
            }

            // 초기 오브젝트 생성
            Prewarm(initialSize);
        }

        /// <summary>
        /// 지정된 개수만큼 오브젝트를 미리 생성하여 풀에 추가합니다.
        /// </summary>
        /// <param name="count">생성할 오브젝트 수</param>
        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                CreateNewObject();
            }
        }

        /// <summary>
        /// 풀에서 오브젝트를 가져옵니다.
        /// 풀이 비어있고 자동 확장이 비활성화된 경우 null을 반환합니다.
        /// </summary>
        /// <returns>풀에서 꺼낸 오브젝트 또는 null</returns>
        public T Get()
        {
            T obj;

            // 풀에 사용 가능한 오브젝트가 있는지 확인
            if (_pool.Count > 0)
            {
                obj = _pool.Pop();
            }
            else if (_autoExpand)
            {
                // 풀이 비었으면 확장
                Debug.Log($"[ObjectPool] {typeof(T).Name} 풀 확장: {_expandCount}개 추가 생성");
                Prewarm(_expandCount);
                obj = _pool.Pop();
            }
            else
            {
                // 자동 확장 비활성화 시 null 반환
                Debug.LogWarning($"[ObjectPool] {typeof(T).Name} 풀이 비어있습니다.");
                return null;
            }

            // IPoolable 인터페이스 구현 시 OnSpawn 호출
            if (obj is IPoolable poolable)
            {
                poolable.OnSpawn();
            }
            else
            {
                // 기본 동작: GameObject 활성화
                obj.gameObject.SetActive(true);
            }

            return obj;
        }

        /// <summary>
        /// 풀에서 오브젝트를 가져와 지정된 위치와 회전으로 설정합니다.
        /// </summary>
        /// <param name="position">설정할 위치</param>
        /// <param name="rotation">설정할 회전</param>
        /// <returns>풀에서 꺼낸 오브젝트</returns>
        public T Get(Vector3 position, Quaternion rotation)
        {
            T obj = Get();
            if (obj != null)
            {
                obj.transform.SetPositionAndRotation(position, rotation);
            }
            return obj;
        }

        /// <summary>
        /// 오브젝트를 풀로 반환합니다.
        /// </summary>
        /// <param name="obj">반환할 오브젝트</param>
        public void Return(T obj)
        {
            if (obj == null)
            {
                Debug.LogWarning($"[ObjectPool] null 오브젝트를 반환하려고 했습니다.");
                return;
            }

            // 이미 풀에 있는지 확인 (중복 반환 방지)
            if (_pool.Contains(obj))
            {
                Debug.LogWarning($"[ObjectPool] {obj.name}은(는) 이미 풀에 있습니다.");
                return;
            }

            // IPoolable 인터페이스 구현 시 OnDespawn 호출
            if (obj is IPoolable poolable)
            {
                poolable.OnDespawn();
            }
            else
            {
                // 기본 동작: GameObject 비활성화
                obj.gameObject.SetActive(false);
            }

            // 부모 Transform 설정 후 풀에 추가
            obj.transform.SetParent(_parent);
            _pool.Push(obj);
        }

        /// <summary>
        /// 모든 활성 오브젝트를 풀로 반환합니다.
        /// </summary>
        public void ReturnAll()
        {
            foreach (var obj in _allObjects)
            {
                if (obj != null && obj.gameObject.activeInHierarchy && !_pool.Contains(obj))
                {
                    Return(obj);
                }
            }
        }

        /// <summary>
        /// 풀의 모든 오브젝트를 파괴하고 풀을 비웁니다.
        /// </summary>
        public void Clear()
        {
            foreach (var obj in _allObjects)
            {
                if (obj != null)
                {
                    UnityEngine.Object.Destroy(obj.gameObject);
                }
            }

            _pool.Clear();
            _allObjects.Clear();
        }

        /// <summary>
        /// 새 오브젝트를 생성하여 풀에 추가합니다.
        /// </summary>
        /// <returns>생성된 오브젝트</returns>
        private T CreateNewObject()
        {
            T obj = UnityEngine.Object.Instantiate(_prefab, _parent);
            obj.gameObject.SetActive(false);
            obj.name = $"{_prefab.name}_{_allObjects.Count}";

            _allObjects.Add(obj);
            _pool.Push(obj);

            return obj;
        }
    }
}
