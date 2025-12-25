using System;
using System.Collections.Generic;
using UnityEngine;

namespace VampireSurvivors.Utilities
{
    /// <summary>
    /// 게임 이벤트 발행/구독 시스템을 관리하는 싱글톤 클래스.
    /// Observer 패턴을 구현하여 컴포넌트 간 결합도를 낮춥니다.
    /// 문자열 기반 이벤트 이름을 사용하며, 파라미터 없는 이벤트와 파라미터가 있는 이벤트를 모두 지원합니다.
    /// </summary>
    /// <example>
    /// // 구독
    /// EventManager.Instance.Subscribe(GameEvents.OnPlayerDeath, OnPlayerDeathHandler);
    /// EventManager.Instance.Subscribe&lt;int&gt;(GameEvents.OnPlayerExpGain, OnExpGain);
    ///
    /// // 발행
    /// EventManager.Instance.Publish(GameEvents.OnPlayerDeath);
    /// EventManager.Instance.Publish(GameEvents.OnPlayerExpGain, 100);
    ///
    /// // 구독 해제 (OnDestroy에서 호출 권장)
    /// EventManager.Instance.Unsubscribe(GameEvents.OnPlayerDeath, OnPlayerDeathHandler);
    /// </example>
    public class EventManager : Singleton<EventManager>
    {
        /// <summary>
        /// 파라미터가 없는 이벤트를 저장하는 딕셔너리.
        /// Key: 이벤트 이름, Value: 콜백 Action
        /// </summary>
        private Dictionary<string, Action> _eventDictionary = new Dictionary<string, Action>();

        /// <summary>
        /// 파라미터가 있는 이벤트를 저장하는 딕셔너리.
        /// Key: 이벤트 이름, Value: 제네릭 Delegate
        /// </summary>
        private Dictionary<string, Delegate> _eventDictionaryWithParam = new Dictionary<string, Delegate>();

        #region No Parameter Events

        /// <summary>
        /// 파라미터가 없는 이벤트를 구독합니다.
        /// </summary>
        /// <param name="eventName">구독할 이벤트 이름 (GameEvents 상수 사용 권장)</param>
        /// <param name="listener">이벤트 발생 시 호출될 콜백 메서드</param>
        public void Subscribe(string eventName, Action listener)
        {
            if (_eventDictionary.TryGetValue(eventName, out Action existingEvent))
            {
                // 기존 이벤트에 리스너 추가 (멀티캐스트 델리게이트)
                _eventDictionary[eventName] = existingEvent + listener;
            }
            else
            {
                // 새 이벤트 등록
                _eventDictionary[eventName] = listener;
            }
        }

        /// <summary>
        /// 파라미터가 없는 이벤트 구독을 해제합니다.
        /// 메모리 누수 방지를 위해 OnDestroy에서 반드시 호출하세요.
        /// </summary>
        /// <param name="eventName">구독 해제할 이벤트 이름</param>
        /// <param name="listener">제거할 콜백 메서드</param>
        public void Unsubscribe(string eventName, Action listener)
        {
            if (_eventDictionary.TryGetValue(eventName, out Action existingEvent))
            {
                existingEvent -= listener;
                if (existingEvent == null)
                    _eventDictionary.Remove(eventName);
                else
                    _eventDictionary[eventName] = existingEvent;
            }
        }

        /// <summary>
        /// 파라미터가 없는 이벤트를 발행합니다.
        /// 해당 이벤트를 구독한 모든 리스너가 호출됩니다.
        /// </summary>
        /// <param name="eventName">발행할 이벤트 이름</param>
        public void Publish(string eventName)
        {
            if (_eventDictionary.TryGetValue(eventName, out Action eventAction))
            {
                eventAction?.Invoke();
            }
        }

        #endregion

        #region Single Parameter Events

        /// <summary>
        /// 파라미터 1개가 있는 이벤트를 구독합니다.
        /// </summary>
        /// <typeparam name="T">파라미터 타입</typeparam>
        /// <param name="eventName">구독할 이벤트 이름</param>
        /// <param name="listener">이벤트 발생 시 호출될 콜백 메서드</param>
        public void Subscribe<T>(string eventName, Action<T> listener)
        {
            if (_eventDictionaryWithParam.TryGetValue(eventName, out Delegate existingEvent))
            {
                _eventDictionaryWithParam[eventName] = Delegate.Combine(existingEvent, listener);
            }
            else
            {
                _eventDictionaryWithParam[eventName] = listener;
            }
        }

        /// <summary>
        /// 파라미터 1개가 있는 이벤트 구독을 해제합니다.
        /// </summary>
        /// <typeparam name="T">파라미터 타입</typeparam>
        /// <param name="eventName">구독 해제할 이벤트 이름</param>
        /// <param name="listener">제거할 콜백 메서드</param>
        public void Unsubscribe<T>(string eventName, Action<T> listener)
        {
            if (_eventDictionaryWithParam.TryGetValue(eventName, out Delegate existingEvent))
            {
                var newEvent = Delegate.Remove(existingEvent, listener);
                if (newEvent == null)
                    _eventDictionaryWithParam.Remove(eventName);
                else
                    _eventDictionaryWithParam[eventName] = newEvent;
            }
        }

        /// <summary>
        /// 파라미터 1개가 있는 이벤트를 발행합니다.
        /// </summary>
        /// <typeparam name="T">파라미터 타입</typeparam>
        /// <param name="eventName">발행할 이벤트 이름</param>
        /// <param name="param">전달할 파라미터</param>
        public void Publish<T>(string eventName, T param)
        {
            if (_eventDictionaryWithParam.TryGetValue(eventName, out Delegate eventDelegate))
            {
                (eventDelegate as Action<T>)?.Invoke(param);
            }
        }

        #endregion

        #region Two Parameter Events

        /// <summary>
        /// 파라미터 2개가 있는 이벤트를 구독합니다.
        /// </summary>
        /// <typeparam name="T1">첫 번째 파라미터 타입</typeparam>
        /// <typeparam name="T2">두 번째 파라미터 타입</typeparam>
        /// <param name="eventName">구독할 이벤트 이름</param>
        /// <param name="listener">이벤트 발생 시 호출될 콜백 메서드</param>
        public void Subscribe<T1, T2>(string eventName, Action<T1, T2> listener)
        {
            if (_eventDictionaryWithParam.TryGetValue(eventName, out Delegate existingEvent))
            {
                _eventDictionaryWithParam[eventName] = Delegate.Combine(existingEvent, listener);
            }
            else
            {
                _eventDictionaryWithParam[eventName] = listener;
            }
        }

        /// <summary>
        /// 파라미터 2개가 있는 이벤트 구독을 해제합니다.
        /// </summary>
        /// <typeparam name="T1">첫 번째 파라미터 타입</typeparam>
        /// <typeparam name="T2">두 번째 파라미터 타입</typeparam>
        /// <param name="eventName">구독 해제할 이벤트 이름</param>
        /// <param name="listener">제거할 콜백 메서드</param>
        public void Unsubscribe<T1, T2>(string eventName, Action<T1, T2> listener)
        {
            if (_eventDictionaryWithParam.TryGetValue(eventName, out Delegate existingEvent))
            {
                var newEvent = Delegate.Remove(existingEvent, listener);
                if (newEvent == null)
                    _eventDictionaryWithParam.Remove(eventName);
                else
                    _eventDictionaryWithParam[eventName] = newEvent;
            }
        }

        /// <summary>
        /// 파라미터 2개가 있는 이벤트를 발행합니다.
        /// </summary>
        /// <typeparam name="T1">첫 번째 파라미터 타입</typeparam>
        /// <typeparam name="T2">두 번째 파라미터 타입</typeparam>
        /// <param name="eventName">발행할 이벤트 이름</param>
        /// <param name="param1">첫 번째 파라미터</param>
        /// <param name="param2">두 번째 파라미터</param>
        public void Publish<T1, T2>(string eventName, T1 param1, T2 param2)
        {
            if (_eventDictionaryWithParam.TryGetValue(eventName, out Delegate eventDelegate))
            {
                (eventDelegate as Action<T1, T2>)?.Invoke(param1, param2);
            }
        }

        #endregion

        /// <summary>
        /// 모든 이벤트 구독을 해제합니다.
        /// 씬 전환 시 호출하면 메모리 누수를 방지할 수 있습니다.
        /// </summary>
        public void ClearAll()
        {
            _eventDictionary.Clear();
            _eventDictionaryWithParam.Clear();
        }

        /// <summary>
        /// 특정 이벤트의 모든 구독을 해제합니다.
        /// </summary>
        /// <param name="eventName">구독을 해제할 이벤트 이름</param>
        public void Clear(string eventName)
        {
            _eventDictionary.Remove(eventName);
            _eventDictionaryWithParam.Remove(eventName);
        }
    }

    /// <summary>
    /// 게임에서 사용되는 이벤트 이름 상수 모음.
    /// EventManager 사용 시 문자열 하드코딩 대신 이 상수를 사용하세요.
    /// </summary>
    public static class GameEvents
    {
        /// <summary>플레이어 사망 이벤트</summary>
        public const string OnPlayerDeath = "OnPlayerDeath";

        /// <summary>플레이어 레벨업 이벤트 (파라미터: int newLevel)</summary>
        public const string OnPlayerLevelUp = "OnPlayerLevelUp";

        /// <summary>플레이어 경험치 획득 이벤트 (파라미터: int expAmount)</summary>
        public const string OnPlayerExpGain = "OnPlayerExpGain";

        /// <summary>몬스터 사망 이벤트 (파라미터: MonsterBase monster)</summary>
        public const string OnMonsterDeath = "OnMonsterDeath";

        /// <summary>게임 시작 이벤트</summary>
        public const string OnGameStart = "OnGameStart";

        /// <summary>게임 일시정지 이벤트</summary>
        public const string OnGamePause = "OnGamePause";

        /// <summary>게임 재개 이벤트</summary>
        public const string OnGameResume = "OnGameResume";

        /// <summary>게임 오버 이벤트</summary>
        public const string OnGameOver = "OnGameOver";

        /// <summary>무기 업그레이드 이벤트 (파라미터: WeaponData weapon, int newLevel)</summary>
        public const string OnWeaponUpgrade = "OnWeaponUpgrade";

        /// <summary>플레이어 스폰 이벤트 (파라미터: GameObject player)</summary>
        public const string OnPlayerSpawned = "OnPlayerSpawned";

        /// <summary>플레이어 피격 이벤트 (파라미터: float damage)</summary>
        public const string OnPlayerDamaged = "OnPlayerDamaged";

        /// <summary>아이템 수집 이벤트 (파라미터: GameObject item)</summary>
        public const string OnItemCollected = "OnItemCollected";

        /// <summary>골드 드롭 이벤트 (파라미터: int amount)</summary>
        public const string OnGoldDropped = "OnGoldDropped";

        /// <summary>보스 스폰 이벤트 (파라미터: MonsterDataSO bossData)</summary>
        public const string OnBossSpawned = "OnBossSpawned";

        /// <summary>보스 처치 이벤트 (파라미터: MonsterDataSO bossData)</summary>
        public const string OnBossDefeated = "OnBossDefeated";
    }
}
