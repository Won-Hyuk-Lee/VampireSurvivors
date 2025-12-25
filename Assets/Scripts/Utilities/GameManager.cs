using UnityEngine;

namespace VampireSurvivors.Utilities
{
    /// <summary>
    /// 게임 상태를 정의하는 열거형.
    /// </summary>
    public enum GameState
    {
        /// <summary>게임 시작 대기 상태</summary>
        Ready,

        /// <summary>게임 진행 중</summary>
        Playing,

        /// <summary>일시 정지 상태</summary>
        Paused,

        /// <summary>게임 오버 상태</summary>
        GameOver
    }

    /// <summary>
    /// 게임의 전반적인 상태와 흐름을 관리하는 싱글톤 매니저.
    /// 게임 시작, 일시정지, 재개, 게임오버 등의 상태 전환을 처리합니다.
    /// 게임 경과 시간도 추적합니다.
    /// </summary>
    /// <example>
    /// // 게임 시작
    /// GameManager.Instance.StartGame();
    ///
    /// // 일시정지 토글
    /// if (Input.GetKeyDown(KeyCode.Escape))
    /// {
    ///     if (GameManager.Instance.IsPlaying)
    ///         GameManager.Instance.PauseGame();
    ///     else
    ///         GameManager.Instance.ResumeGame();
    /// }
    ///
    /// // 경과 시간 표시
    /// timeText.text = GameManager.Instance.GetFormattedTime();
    /// </example>
    public class GameManager : Singleton<GameManager>
    {
        /// <summary>현재 게임 상태</summary>
        public GameState CurrentState { get; private set; } = GameState.Ready;

        /// <summary>게임 시작 후 경과 시간 (초)</summary>
        public float ElapsedTime { get; private set; } = 0f;

        /// <summary>현재 게임이 진행 중인지 여부 (Playing 상태)</summary>
        public bool IsPlaying => CurrentState == GameState.Playing;

        /// <summary>
        /// 싱글톤 초기화.
        /// 타겟 프레임레이트를 60으로 설정합니다.
        /// </summary>
        protected override void OnSingletonAwake()
        {
            Application.targetFrameRate = 60;
        }

        /// <summary>
        /// Unity Update 콜백.
        /// Playing 상태에서만 경과 시간을 업데이트합니다.
        /// </summary>
        private void Update()
        {
            if (CurrentState == GameState.Playing)
            {
                ElapsedTime += Time.deltaTime;
            }
        }

        /// <summary>
        /// 게임을 시작합니다.
        /// 경과 시간을 초기화하고 상태를 Playing으로 변경합니다.
        /// OnGameStart 이벤트를 발행합니다.
        /// </summary>
        public void StartGame()
        {
            ElapsedTime = 0f;
            CurrentState = GameState.Playing;
            Time.timeScale = 1f;
            EventManager.Instance.Publish(GameEvents.OnGameStart);
        }

        /// <summary>
        /// 게임을 일시정지합니다.
        /// Time.timeScale을 0으로 설정하여 물리/애니메이션을 멈춥니다.
        /// OnGamePause 이벤트를 발행합니다.
        /// </summary>
        public void PauseGame()
        {
            // Playing 상태에서만 일시정지 가능
            if (CurrentState != GameState.Playing) return;

            CurrentState = GameState.Paused;
            Time.timeScale = 0f;
            EventManager.Instance.Publish(GameEvents.OnGamePause);
        }

        /// <summary>
        /// 일시정지된 게임을 재개합니다.
        /// Time.timeScale을 1로 복원합니다.
        /// OnGameResume 이벤트를 발행합니다.
        /// </summary>
        public void ResumeGame()
        {
            // Paused 상태에서만 재개 가능
            if (CurrentState != GameState.Paused) return;

            CurrentState = GameState.Playing;
            Time.timeScale = 1f;
            EventManager.Instance.Publish(GameEvents.OnGameResume);
        }

        /// <summary>
        /// 게임 오버 처리를 합니다.
        /// Time.timeScale을 0으로 설정합니다.
        /// OnGameOver 이벤트를 발행합니다.
        /// </summary>
        public void GameOver()
        {
            // 이미 게임오버 상태면 무시
            if (CurrentState == GameState.GameOver) return;

            CurrentState = GameState.GameOver;
            Time.timeScale = 0f;
            EventManager.Instance.Publish(GameEvents.OnGameOver);
        }

        /// <summary>
        /// 현재 씬을 다시 로드하여 게임을 재시작합니다.
        /// </summary>
        public void RestartGame()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        /// <summary>
        /// 지정된 씬을 로드합니다.
        /// </summary>
        /// <param name="sceneName">로드할 씬 이름 (Constants.Scenes 사용 권장)</param>
        public void LoadScene(string sceneName)
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }

        /// <summary>
        /// 경과 시간을 "MM:SS" 형식의 문자열로 반환합니다.
        /// </summary>
        /// <returns>포맷된 시간 문자열 (예: "05:30")</returns>
        public string GetFormattedTime()
        {
            int minutes = Mathf.FloorToInt(ElapsedTime / 60f);
            int seconds = Mathf.FloorToInt(ElapsedTime % 60f);
            return $"{minutes:00}:{seconds:00}";
        }
    }
}
