using System;
using System.Collections.Generic;
using UnityEngine;

namespace VampireSurvivors.Utilities
{
    /// <summary>
    /// 코루틴 없이 시간을 관리하는 타이머 클래스.
    /// Update 루프에서 Tick()을 호출하여 작동합니다.
    /// 일회성 또는 반복 타이머로 사용할 수 있습니다.
    /// </summary>
    /// <example>
    /// // 직접 사용
    /// var timer = new Timer(3f, () => Debug.Log("완료!"), loop: false);
    /// timer.Start();
    /// // Update에서: timer.Tick(Time.deltaTime);
    ///
    /// // TimerManager 사용 (권장)
    /// TimerManager.Instance.CreateTimer(3f, () => Debug.Log("완료!"));
    /// </example>
    public class Timer
    {
        /// <summary>타이머 지속 시간 (초)</summary>
        public float Duration { get; private set; }

        /// <summary>남은 시간 (초)</summary>
        public float RemainingTime { get; private set; }

        /// <summary>타이머 실행 중 여부</summary>
        public bool IsRunning { get; private set; }

        /// <summary>타이머 완료 여부</summary>
        public bool IsCompleted => RemainingTime <= 0f;

        /// <summary>진행률 (0.0 ~ 1.0, 완료 시 1.0)</summary>
        public float Progress => 1f - (RemainingTime / Duration);

        /// <summary>타이머 완료 시 호출될 콜백</summary>
        private Action _onComplete;

        /// <summary>반복 여부 (true면 완료 후 자동으로 재시작)</summary>
        private bool _loop;

        /// <summary>
        /// 타이머 생성자.
        /// </summary>
        /// <param name="duration">지속 시간 (초)</param>
        /// <param name="onComplete">완료 시 콜백 (선택)</param>
        /// <param name="loop">반복 여부 (기본: false)</param>
        public Timer(float duration, Action onComplete = null, bool loop = false)
        {
            Duration = duration;
            RemainingTime = duration;
            _onComplete = onComplete;
            _loop = loop;
            IsRunning = false;
        }

        /// <summary>
        /// 타이머를 시작합니다.
        /// 남은 시간이 Duration으로 초기화됩니다.
        /// </summary>
        public void Start()
        {
            IsRunning = true;
            RemainingTime = Duration;
        }

        /// <summary>
        /// 타이머를 정지합니다.
        /// 남은 시간은 유지됩니다.
        /// </summary>
        public void Stop()
        {
            IsRunning = false;
        }

        /// <summary>
        /// 타이머를 초기 상태로 리셋합니다.
        /// 실행 상태는 변경되지 않습니다.
        /// </summary>
        public void Reset()
        {
            RemainingTime = Duration;
        }

        /// <summary>
        /// 타이머를 업데이트합니다.
        /// MonoBehaviour의 Update에서 호출하거나 TimerManager를 사용하세요.
        /// </summary>
        /// <param name="deltaTime">경과 시간 (일반적으로 Time.deltaTime)</param>
        public void Tick(float deltaTime)
        {
            if (!IsRunning) return;

            RemainingTime -= deltaTime;

            // 타이머 완료 처리
            if (RemainingTime <= 0f)
            {
                _onComplete?.Invoke();

                if (_loop)
                {
                    // 반복 타이머: 재시작
                    RemainingTime = Duration;
                }
                else
                {
                    // 일회성 타이머: 정지
                    IsRunning = false;
                    RemainingTime = 0f;
                }
            }
        }

        /// <summary>
        /// 타이머 지속 시간을 변경합니다.
        /// </summary>
        /// <param name="newDuration">새로운 지속 시간</param>
        public void SetDuration(float newDuration)
        {
            Duration = newDuration;
        }

        /// <summary>
        /// 완료 콜백을 변경합니다.
        /// </summary>
        /// <param name="onComplete">새로운 콜백</param>
        public void SetOnComplete(Action onComplete)
        {
            _onComplete = onComplete;
        }
    }

    /// <summary>
    /// 여러 타이머를 중앙에서 관리하는 싱글톤 매니저.
    /// 타이머를 생성하고 자동으로 업데이트합니다.
    /// 개별 MonoBehaviour에서 타이머를 관리할 필요가 없어집니다.
    /// </summary>
    /// <example>
    /// // 3초 후 한 번 실행
    /// var timer = TimerManager.Instance.CreateTimer(3f, () => Debug.Log("완료!"));
    ///
    /// // 1초마다 반복 실행
    /// var repeatTimer = TimerManager.Instance.CreateTimer(1f, () => Debug.Log("Tick!"), loop: true);
    ///
    /// // 타이머 제거
    /// TimerManager.Instance.RemoveTimer(timer);
    /// </example>
    public class TimerManager : Singleton<TimerManager>
    {
        /// <summary>관리 중인 타이머 목록</summary>
        private List<Timer> _timers = new List<Timer>();

        /// <summary>이번 프레임에 추가될 타이머 (동시 수정 방지)</summary>
        private List<Timer> _timersToAdd = new List<Timer>();

        /// <summary>이번 프레임에 제거될 타이머 (동시 수정 방지)</summary>
        private List<Timer> _timersToRemove = new List<Timer>();

        /// <summary>
        /// 새 타이머를 생성하고 관리 목록에 추가합니다.
        /// </summary>
        /// <param name="duration">지속 시간 (초)</param>
        /// <param name="onComplete">완료 시 콜백 (선택)</param>
        /// <param name="loop">반복 여부 (기본: false)</param>
        /// <param name="autoStart">자동 시작 여부 (기본: true)</param>
        /// <returns>생성된 Timer 인스턴스</returns>
        public Timer CreateTimer(float duration, Action onComplete = null, bool loop = false, bool autoStart = true)
        {
            var timer = new Timer(duration, onComplete, loop);
            _timersToAdd.Add(timer);

            if (autoStart)
                timer.Start();

            return timer;
        }

        /// <summary>
        /// 타이머를 관리 목록에서 제거합니다.
        /// </summary>
        /// <param name="timer">제거할 타이머</param>
        public void RemoveTimer(Timer timer)
        {
            _timersToRemove.Add(timer);
        }

        /// <summary>
        /// Unity Update 콜백.
        /// 모든 관리 중인 타이머를 업데이트합니다.
        /// </summary>
        private void Update()
        {
            // 대기 중인 타이머 추가
            foreach (var timer in _timersToAdd)
            {
                _timers.Add(timer);
            }
            _timersToAdd.Clear();

            // 모든 타이머 업데이트
            foreach (var timer in _timers)
            {
                timer.Tick(Time.deltaTime);
            }

            // 제거 대기 중인 타이머 제거
            foreach (var timer in _timersToRemove)
            {
                _timers.Remove(timer);
            }
            _timersToRemove.Clear();
        }

        /// <summary>
        /// 모든 타이머를 제거합니다.
        /// 씬 전환 시 호출하면 좋습니다.
        /// </summary>
        public void ClearAll()
        {
            _timers.Clear();
            _timersToAdd.Clear();
            _timersToRemove.Clear();
        }
    }
}
