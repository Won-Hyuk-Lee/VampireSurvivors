using UnityEngine;

namespace VampireSurvivors.Utilities
{
    /// <summary>
    /// 제네릭 싱글톤 베이스 클래스.
    /// MonoBehaviour를 상속받는 클래스에서 싱글톤 패턴을 쉽게 구현할 수 있도록 지원합니다.
    /// 씬 전환 시에도 파괴되지 않으며(DontDestroyOnLoad), 스레드 안전합니다.
    /// </summary>
    /// <typeparam name="T">싱글톤으로 만들 MonoBehaviour 타입</typeparam>
    /// <example>
    /// public class GameManager : Singleton&lt;GameManager&gt;
    /// {
    ///     protected override void OnSingletonAwake()
    ///     {
    ///         // 초기화 로직
    ///     }
    /// }
    /// // 사용: GameManager.Instance.SomeMethod();
    /// </example>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        /// <summary>
        /// 싱글톤 인스턴스를 저장하는 정적 변수
        /// </summary>
        private static T _instance;

        /// <summary>
        /// 멀티스레드 환경에서 인스턴스 생성 시 동기화를 위한 락 객체
        /// </summary>
        private static readonly object _lock = new object();

        /// <summary>
        /// 애플리케이션 종료 중인지 확인하는 플래그.
        /// OnApplicationQuit 이후 Instance 접근 시 새 인스턴스 생성을 방지합니다.
        /// </summary>
        private static bool _isQuitting = false;

        /// <summary>
        /// 싱글톤 인스턴스에 접근하는 프로퍼티.
        /// 인스턴스가 없으면 씬에서 찾거나 새로 생성합니다.
        /// </summary>
        public static T Instance
        {
            get
            {
                // 애플리케이션 종료 중이면 null 반환 (새 인스턴스 생성 방지)
                if (_isQuitting)
                {
                    Debug.LogWarning($"[Singleton] Instance of {typeof(T)} already destroyed. Returning null.");
                    return null;
                }

                // 스레드 안전을 위한 락
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        // 씬에서 기존 인스턴스 검색
                        _instance = FindFirstObjectByType<T>();

                        // 씬에 없으면 새 GameObject 생성
                        if (_instance == null)
                        {
                            var singletonObject = new GameObject($"{typeof(T).Name} (Singleton)");
                            _instance = singletonObject.AddComponent<T>();
                            DontDestroyOnLoad(singletonObject);
                        }
                    }

                    return _instance;
                }
            }
        }

        /// <summary>
        /// Unity Awake 콜백.
        /// 싱글톤 인스턴스 설정 및 중복 인스턴스 제거를 처리합니다.
        /// </summary>
        protected virtual void Awake()
        {
            if (_instance == null)
            {
                // 첫 번째 인스턴스를 싱글톤으로 설정
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
                OnSingletonAwake();
            }
            else if (_instance != this)
            {
                // 중복 인스턴스는 즉시 파괴
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 싱글톤 초기화 시 호출되는 가상 메서드.
        /// 자식 클래스에서 오버라이드하여 초기화 로직을 구현합니다.
        /// Awake 대신 이 메서드를 사용하세요.
        /// </summary>
        protected virtual void OnSingletonAwake() { }

        /// <summary>
        /// 애플리케이션 종료 시 호출.
        /// 종료 플래그를 설정하여 종료 중 새 인스턴스 생성을 방지합니다.
        /// </summary>
        protected virtual void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        /// <summary>
        /// 오브젝트 파괴 시 호출.
        /// 현재 인스턴스가 싱글톤이면 참조를 null로 설정합니다.
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
