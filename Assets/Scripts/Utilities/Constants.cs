namespace VampireSurvivors.Utilities
{
    /// <summary>
    /// 게임 전체에서 사용되는 상수 값들을 정의하는 정적 클래스.
    /// 문자열 하드코딩을 방지하고 오타로 인한 버그를 예방합니다.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Unity 태그(Tag) 문자열 상수.
        /// Edit > Project Settings > Tags and Layers에서 태그를 먼저 등록해야 합니다.
        /// </summary>
        public static class Tags
        {
            /// <summary>플레이어 캐릭터 태그</summary>
            public const string Player = "Player";

            /// <summary>적(몬스터) 태그</summary>
            public const string Enemy = "Enemy";

            /// <summary>투사체(총알, 마법 등) 태그</summary>
            public const string Projectile = "Projectile";

            /// <summary>획득 가능한 아이템 태그</summary>
            public const string PickUp = "PickUp";

            /// <summary>경험치 오브젝트 태그</summary>
            public const string Experience = "Experience";
        }

        /// <summary>
        /// Unity 레이어(Layer) 문자열 상수.
        /// Edit > Project Settings > Tags and Layers에서 레이어를 먼저 등록해야 합니다.
        /// 물리 충돌 매트릭스 설정 시 사용됩니다.
        /// </summary>
        public static class Layers
        {
            /// <summary>기본 레이어</summary>
            public const string Default = "Default";

            /// <summary>플레이어 레이어 (플레이어 전용 충돌 처리)</summary>
            public const string Player = "Player";

            /// <summary>적 레이어 (적 전용 충돌 처리)</summary>
            public const string Enemy = "Enemy";

            /// <summary>투사체 레이어 (투사체 충돌 최적화)</summary>
            public const string Projectile = "Projectile";

            /// <summary>획득 아이템 레이어</summary>
            public const string PickUp = "PickUp";
        }

        /// <summary>
        /// 씬 이름 상수.
        /// Build Settings에 등록된 씬 이름과 일치해야 합니다.
        /// SceneManager.LoadScene() 호출 시 사용됩니다.
        /// </summary>
        public static class Scenes
        {
            /// <summary>타이틀/메인 메뉴 씬</summary>
            public const string Title = "TitleScene";

            /// <summary>인게임(플레이) 씬</summary>
            public const string InGame = "InGameScene";

            /// <summary>결과 화면 씬</summary>
            public const string Result = "ResultScene";
        }

        /// <summary>
        /// Animator 파라미터 이름 상수.
        /// Animator Controller에서 정의한 파라미터 이름과 일치해야 합니다.
        /// Animator.SetBool(), SetTrigger() 등에서 사용됩니다.
        /// </summary>
        public static class AnimParams
        {
            /// <summary>이동 중 여부 (bool)</summary>
            public const string IsMoving = "IsMoving";

            /// <summary>공격 트리거 (trigger)</summary>
            public const string Attack = "Attack";

            /// <summary>사망 트리거 (trigger)</summary>
            public const string Die = "Die";

            /// <summary>피격 트리거 (trigger)</summary>
            public const string Hit = "Hit";
        }
    }
}
