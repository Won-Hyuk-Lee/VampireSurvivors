using UnityEngine;

namespace VampireSurvivors.Utilities
{
    /// <summary>
    /// Unity 타입들에 대한 확장 메서드 모음.
    /// Vector2, Vector3, Transform, GameObject 등의 기능을 확장합니다.
    /// 코드 가독성을 높이고 자주 사용되는 연산을 간편하게 호출할 수 있습니다.
    /// </summary>
    public static class ExtensionMethods
    {
        #region Vector3 Extensions

        /// <summary>
        /// Vector3의 X값만 변경한 새 벡터를 반환합니다.
        /// </summary>
        /// <param name="v">원본 벡터</param>
        /// <param name="x">새로운 X값</param>
        /// <returns>X값이 변경된 새 Vector3</returns>
        public static Vector3 WithX(this Vector3 v, float x) => new Vector3(x, v.y, v.z);

        /// <summary>
        /// Vector3의 Y값만 변경한 새 벡터를 반환합니다.
        /// </summary>
        /// <param name="v">원본 벡터</param>
        /// <param name="y">새로운 Y값</param>
        /// <returns>Y값이 변경된 새 Vector3</returns>
        public static Vector3 WithY(this Vector3 v, float y) => new Vector3(v.x, y, v.z);

        /// <summary>
        /// Vector3의 Z값만 변경한 새 벡터를 반환합니다.
        /// </summary>
        /// <param name="v">원본 벡터</param>
        /// <param name="z">새로운 Z값</param>
        /// <returns>Z값이 변경된 새 Vector3</returns>
        public static Vector3 WithZ(this Vector3 v, float z) => new Vector3(v.x, v.y, z);

        /// <summary>
        /// Vector3를 Vector2로 변환합니다 (X, Y 사용).
        /// 2D 게임에서 3D 위치를 2D로 변환할 때 유용합니다.
        /// </summary>
        /// <param name="v">원본 Vector3</param>
        /// <returns>X, Y 값을 가진 Vector2</returns>
        public static Vector2 ToVector2XY(this Vector3 v) => new Vector2(v.x, v.y);

        /// <summary>
        /// Vector3를 Vector2로 변환합니다 (X, Z 사용).
        /// 탑다운 3D 게임에서 수평면 좌표 추출 시 유용합니다.
        /// </summary>
        /// <param name="v">원본 Vector3</param>
        /// <returns>X, Z 값을 가진 Vector2</returns>
        public static Vector2 ToVector2XZ(this Vector3 v) => new Vector2(v.x, v.z);

        /// <summary>
        /// 랜덤한 2D 방향 벡터를 생성합니다 (XY 평면).
        /// 360도 중 무작위 각도의 정규화된 방향 벡터를 반환합니다.
        /// </summary>
        /// <returns>정규화된 랜덤 방향 Vector3 (Z = 0)</returns>
        public static Vector3 RandomDirection2D()
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
        }

        /// <summary>
        /// 현재 위치에서 목표 위치까지의 정규화된 방향 벡터를 반환합니다.
        /// </summary>
        /// <param name="from">시작 위치</param>
        /// <param name="to">목표 위치</param>
        /// <returns>정규화된 방향 벡터</returns>
        public static Vector3 DirectionTo(this Vector3 from, Vector3 to) => (to - from).normalized;

        /// <summary>
        /// 두 위치 사이의 거리를 반환합니다.
        /// </summary>
        /// <param name="from">시작 위치</param>
        /// <param name="to">목표 위치</param>
        /// <returns>두 점 사이의 거리</returns>
        public static float DistanceTo(this Vector3 from, Vector3 to) => Vector3.Distance(from, to);

        #endregion

        #region Vector2 Extensions

        /// <summary>
        /// Vector2의 X값만 변경한 새 벡터를 반환합니다.
        /// </summary>
        /// <param name="v">원본 벡터</param>
        /// <param name="x">새로운 X값</param>
        /// <returns>X값이 변경된 새 Vector2</returns>
        public static Vector2 WithX(this Vector2 v, float x) => new Vector2(x, v.y);

        /// <summary>
        /// Vector2의 Y값만 변경한 새 벡터를 반환합니다.
        /// </summary>
        /// <param name="v">원본 벡터</param>
        /// <param name="y">새로운 Y값</param>
        /// <returns>Y값이 변경된 새 Vector2</returns>
        public static Vector2 WithY(this Vector2 v, float y) => new Vector2(v.x, y);

        /// <summary>
        /// Vector2를 Vector3로 변환합니다 (XY 평면).
        /// </summary>
        /// <param name="v">원본 Vector2</param>
        /// <param name="z">Z값 (기본값: 0)</param>
        /// <returns>Vector3 (x, y, z)</returns>
        public static Vector3 ToVector3XY(this Vector2 v, float z = 0f) => new Vector3(v.x, v.y, z);

        /// <summary>
        /// Vector2를 Vector3로 변환합니다 (XZ 평면).
        /// 탑다운 3D 게임에서 2D 입력을 3D 이동으로 변환할 때 유용합니다.
        /// </summary>
        /// <param name="v">원본 Vector2</param>
        /// <param name="y">Y값 (기본값: 0)</param>
        /// <returns>Vector3 (x, y, z) - v.x는 x로, v.y는 z로 매핑</returns>
        public static Vector3 ToVector3XZ(this Vector2 v, float y = 0f) => new Vector3(v.x, y, v.y);

        /// <summary>
        /// 랜덤한 2D 방향 벡터를 생성합니다.
        /// 360도 중 무작위 각도의 정규화된 방향 벡터를 반환합니다.
        /// </summary>
        /// <returns>정규화된 랜덤 방향 Vector2</returns>
        public static Vector2 RandomDirection()
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        /// <summary>
        /// 현재 위치에서 목표 위치까지의 정규화된 방향 벡터를 반환합니다.
        /// </summary>
        /// <param name="from">시작 위치</param>
        /// <param name="to">목표 위치</param>
        /// <returns>정규화된 방향 벡터</returns>
        public static Vector2 DirectionTo(this Vector2 from, Vector2 to) => (to - from).normalized;

        /// <summary>
        /// 두 위치 사이의 거리를 반환합니다.
        /// </summary>
        /// <param name="from">시작 위치</param>
        /// <param name="to">목표 위치</param>
        /// <returns>두 점 사이의 거리</returns>
        public static float DistanceTo(this Vector2 from, Vector2 to) => Vector2.Distance(from, to);

        #endregion

        #region Transform Extensions

        /// <summary>
        /// 2D 게임에서 Transform이 목표 위치를 바라보도록 회전합니다.
        /// Z축 회전만 사용하며, 스프라이트가 오른쪽을 기본 방향으로 가정합니다.
        /// </summary>
        /// <param name="transform">회전할 Transform</param>
        /// <param name="target">바라볼 목표 위치</param>
        public static void LookAt2D(this Transform transform, Vector3 target)
        {
            Vector3 direction = target - transform.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        /// <summary>
        /// 2D 게임에서 Transform이 목표 Transform을 바라보도록 회전합니다.
        /// </summary>
        /// <param name="transform">회전할 Transform</param>
        /// <param name="target">바라볼 목표 Transform</param>
        public static void LookAt2D(this Transform transform, Transform target)
        {
            transform.LookAt2D(target.position);
        }

        /// <summary>
        /// 현재 Transform에서 목표 Transform까지의 정규화된 방향 벡터를 반환합니다.
        /// </summary>
        /// <param name="from">시작 Transform</param>
        /// <param name="to">목표 Transform</param>
        /// <returns>정규화된 방향 벡터</returns>
        public static Vector3 DirectionTo(this Transform from, Transform to)
            => from.position.DirectionTo(to.position);

        /// <summary>
        /// 두 Transform 사이의 거리를 반환합니다.
        /// </summary>
        /// <param name="from">시작 Transform</param>
        /// <param name="to">목표 Transform</param>
        /// <returns>두 Transform 사이의 거리</returns>
        public static float DistanceTo(this Transform from, Transform to)
            => from.position.DistanceTo(to.position);

        /// <summary>
        /// Transform의 로컬 위치, 회전, 스케일을 기본값으로 초기화합니다.
        /// 오브젝트 풀링에서 재사용 전 초기화 시 유용합니다.
        /// </summary>
        /// <param name="transform">초기화할 Transform</param>
        public static void ResetLocal(this Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        #endregion

        #region GameObject Extensions

        /// <summary>
        /// GameObject에서 컴포넌트를 가져오거나, 없으면 추가합니다.
        /// 컴포넌트 존재 여부를 확인하지 않고 안전하게 사용할 수 있습니다.
        /// </summary>
        /// <typeparam name="T">가져올 또는 추가할 컴포넌트 타입</typeparam>
        /// <param name="go">대상 GameObject</param>
        /// <returns>기존 또는 새로 추가된 컴포넌트</returns>
        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            if (component == null)
                component = go.AddComponent<T>();
            return component;
        }

        /// <summary>
        /// GameObject의 활성화 상태를 최적화하여 설정합니다.
        /// 현재 상태와 동일하면 SetActive 호출을 건너뜁니다 (성능 최적화).
        /// </summary>
        /// <param name="go">대상 GameObject</param>
        /// <param name="active">활성화 여부</param>
        public static void SetActiveOptimized(this GameObject go, bool active)
        {
            if (go.activeSelf != active)
                go.SetActive(active);
        }

        #endregion

        #region Component Extensions

        /// <summary>
        /// Component가 부착된 GameObject에서 컴포넌트를 가져오거나, 없으면 추가합니다.
        /// </summary>
        /// <typeparam name="T">가져올 또는 추가할 컴포넌트 타입</typeparam>
        /// <param name="component">대상 Component</param>
        /// <returns>기존 또는 새로 추가된 컴포넌트</returns>
        public static T GetOrAddComponent<T>(this Component component) where T : Component
        {
            return component.gameObject.GetOrAddComponent<T>();
        }

        #endregion
    }
}
