namespace VampireSurvivors.Core
{
    /// <summary>
    /// 오브젝트 풀링을 지원하는 오브젝트가 구현해야 하는 인터페이스.
    /// 풀에서 꺼내거나 반환할 때 호출되는 콜백을 정의합니다.
    /// </summary>
    /// <example>
    /// public class Bullet : MonoBehaviour, IPoolable
    /// {
    ///     public void OnSpawn()
    ///     {
    ///         // 풀에서 꺼낼 때 초기화 로직
    ///         gameObject.SetActive(true);
    ///     }
    ///
    ///     public void OnDespawn()
    ///     {
    ///         // 풀로 반환할 때 정리 로직
    ///         gameObject.SetActive(false);
    ///     }
    /// }
    /// </example>
    public interface IPoolable
    {
        /// <summary>
        /// 오브젝트가 풀에서 꺼내져서 활성화될 때 호출됩니다.
        /// 오브젝트의 초기 상태 설정, 컴포넌트 활성화 등의 로직을 구현합니다.
        /// </summary>
        void OnSpawn();

        /// <summary>
        /// 오브젝트가 풀로 반환되어 비활성화될 때 호출됩니다.
        /// 상태 초기화, 이벤트 구독 해제, 컴포넌트 비활성화 등의 정리 로직을 구현합니다.
        /// </summary>
        void OnDespawn();
    }
}
