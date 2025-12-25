using System.Collections.Generic;
using UnityEngine;
using VampireSurvivors.Character;
using VampireSurvivors.Core;
using VampireSurvivors.Data;
using VampireSurvivors.Monster;
using VampireSurvivors.Utilities;

namespace VampireSurvivors.Spawner
{
    /// <summary>
    /// 몬스터 스폰을 담당하는 클래스.
    /// 시간에 따라 난이도가 증가하며, 웨이브 기반 스폰과 보스 등장을 관리합니다.
    /// </summary>
    /// <remarks>
    /// PoolManager와 연동하여 몬스터를 풀에서 가져옵니다.
    /// GameManager의 게임 상태에 따라 스폰을 제어합니다.
    /// </remarks>
    public class MonsterSpawner : MonoBehaviour
    {
        #region 인스펙터 필드

        [Header("스폰 데이터")]
        [Tooltip("스폰 설정 데이터")]
        [SerializeField]
        private SpawnDataSO _spawnData;

        [Header("기본 몬스터")]
        [Tooltip("기본 몬스터 프리팹 (웨이브가 없을 때 사용)")]
        [SerializeField]
        private MonsterDataSO _defaultMonsterData;

        [Tooltip("기본 몬스터 풀 키")]
        [SerializeField]
        private string _defaultMonsterPoolKey = "BasicMonster";

        [Tooltip("기본 몬스터 풀 초기 크기")]
        [SerializeField]
        private int _defaultPoolSize = 50;

        [Header("디버그")]
        [Tooltip("스폰 로그 출력 여부")]
        [SerializeField]
        private bool _debugLog = false;

        #endregion

        #region 런타임 변수

        /// <summary>
        /// 스폰 활성화 여부
        /// </summary>
        private bool _isSpawning;

        /// <summary>
        /// 다음 스폰까지의 타이머
        /// </summary>
        private float _spawnTimer;

        /// <summary>
        /// 현재 활성화된 몬스터 수
        /// </summary>
        private int _activeMonsterCount;

        /// <summary>
        /// 마지막 보스 체크 시간
        /// </summary>
        private float _lastBossCheckTime;

        /// <summary>
        /// 보스 스폰으로 인해 일반 스폰이 일시 정지되었는지 여부
        /// </summary>
        private bool _normalSpawnPaused;

        /// <summary>
        /// 웨이브별 스폰 타이머
        /// </summary>
        private Dictionary<SpawnWaveEntry, float> _waveTimers = new Dictionary<SpawnWaveEntry, float>();

        /// <summary>
        /// 활성화된 몬스터 목록
        /// </summary>
        private List<MonsterBase> _activeMonsters = new List<MonsterBase>();

        /// <summary>
        /// 풀 초기화 여부
        /// </summary>
        private bool _poolInitialized;

        #endregion

        #region Properties

        /// <summary>
        /// 현재 활성화된 몬스터 수
        /// </summary>
        public int ActiveMonsterCount => _activeMonsterCount;

        /// <summary>
        /// 스폰 활성화 여부
        /// </summary>
        public bool IsSpawning => _isSpawning;

        #endregion

        #region Unity 생명주기

        private void Start()
        {
            // 풀 초기화
            InitializePool();

            // 이벤트 구독
            EventManager.Instance.Subscribe(GameEvents.OnGameStart, OnGameStart);
            EventManager.Instance.Subscribe(GameEvents.OnGameOver, OnGameOver);
            EventManager.Instance.Subscribe<GameObject>(GameEvents.OnMonsterDeath, OnMonsterKilled);
        }

        private void OnDestroy()
        {
            // 이벤트 구독 해제
            if (EventManager.Instance != null)
            {
                EventManager.Instance.Unsubscribe(GameEvents.OnGameStart, OnGameStart);
                EventManager.Instance.Unsubscribe(GameEvents.OnGameOver, OnGameOver);
                EventManager.Instance.Unsubscribe<GameObject>(GameEvents.OnMonsterDeath, OnMonsterKilled);
            }
        }

        private void Update()
        {
            if (!_isSpawning || _spawnData == null) return;

            // 게임 상태 체크
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                return;
            }

            float gameTime = GetGameTime();

            // 보스 체크
            CheckBossSpawn(gameTime);

            // 일반 스폰이 일시 정지 상태면 리턴
            if (_normalSpawnPaused) return;

            // 웨이브 기반 스폰
            UpdateWaveSpawns(gameTime);

            // 기본 스폰 (웨이브가 없을 때)
            UpdateDefaultSpawn(gameTime);
        }

        private void OnDrawGizmosSelected()
        {
            if (_spawnData == null) return;

            // 스폰 범위 시각화
            Vector3 center = Player.Instance != null ? Player.Instance.transform.position : transform.position;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(center, _spawnData.MinSpawnDistance);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(center, _spawnData.MaxSpawnDistance);
        }

        #endregion

        #region 초기화

        /// <summary>
        /// 몬스터 풀을 초기화합니다.
        /// </summary>
        private void InitializePool()
        {
            if (_poolInitialized) return;

            if (PoolManager.Instance == null)
            {
                Debug.LogWarning("[MonsterSpawner] PoolManager를 찾을 수 없습니다.");
                return;
            }

            // 기본 몬스터 풀 생성
            if (_defaultMonsterData != null && _defaultMonsterData.Prefab != null)
            {
                if (!PoolManager.Instance.HasPool(_defaultMonsterPoolKey))
                {
                    PoolManager.Instance.CreatePool(
                        _defaultMonsterPoolKey,
                        _defaultMonsterData.Prefab,
                        _defaultPoolSize
                    );
                }
            }

            // 웨이브 몬스터 풀 생성
            if (_spawnData != null)
            {
                foreach (var wave in _spawnData.SpawnWaves)
                {
                    CreateMonsterPool(wave.monsterData);
                }

                // 보스 풀 생성
                foreach (var boss in _spawnData.BossSpawns)
                {
                    CreateMonsterPool(boss.bossData);
                }
            }

            _poolInitialized = true;
        }

        /// <summary>
        /// 특정 몬스터 데이터에 대한 풀을 생성합니다.
        /// </summary>
        private void CreateMonsterPool(MonsterDataSO monsterData)
        {
            if (monsterData == null || monsterData.Prefab == null) return;

            string poolKey = GetPoolKey(monsterData);

            if (!PoolManager.Instance.HasPool(poolKey))
            {
                int poolSize = monsterData.IsBoss ? 2 : 30;
                PoolManager.Instance.CreatePool(poolKey, monsterData.Prefab, poolSize);
            }
        }

        /// <summary>
        /// 몬스터 데이터에 해당하는 풀 키를 반환합니다.
        /// </summary>
        private string GetPoolKey(MonsterDataSO monsterData)
        {
            return string.IsNullOrEmpty(monsterData.MonsterId)
                ? monsterData.name
                : monsterData.MonsterId;
        }

        #endregion

        #region 스폰 제어

        /// <summary>
        /// 스폰을 시작합니다.
        /// </summary>
        public void StartSpawning()
        {
            _isSpawning = true;
            _spawnTimer = 0f;
            _lastBossCheckTime = 0f;
            _normalSpawnPaused = false;
            _waveTimers.Clear();

            if (_debugLog)
            {
                Debug.Log("[MonsterSpawner] 스폰 시작");
            }
        }

        /// <summary>
        /// 스폰을 중지합니다.
        /// </summary>
        public void StopSpawning()
        {
            _isSpawning = false;

            if (_debugLog)
            {
                Debug.Log("[MonsterSpawner] 스폰 중지");
            }
        }

        /// <summary>
        /// 모든 활성 몬스터를 제거합니다.
        /// </summary>
        public void DespawnAllMonsters()
        {
            foreach (var monster in _activeMonsters)
            {
                if (monster != null && monster.gameObject.activeInHierarchy)
                {
                    monster.ReturnToPool();
                }
            }

            _activeMonsters.Clear();
            _activeMonsterCount = 0;
        }

        #endregion

        #region 스폰 로직

        /// <summary>
        /// 웨이브 기반 스폰을 업데이트합니다.
        /// </summary>
        private void UpdateWaveSpawns(float gameTime)
        {
            if (_spawnData == null) return;

            var activeWaves = _spawnData.GetActiveWaves(gameTime);

            foreach (var wave in activeWaves)
            {
                // 웨이브 타이머 초기화
                if (!_waveTimers.ContainsKey(wave))
                {
                    _waveTimers[wave] = 0f;
                }

                // 타이머 업데이트
                _waveTimers[wave] -= Time.deltaTime;

                if (_waveTimers[wave] <= 0f)
                {
                    // 스폰
                    SpawnMonsterFromWave(wave, gameTime);
                    _waveTimers[wave] = wave.spawnInterval;
                }
            }
        }

        /// <summary>
        /// 기본 몬스터 스폰을 업데이트합니다.
        /// </summary>
        private void UpdateDefaultSpawn(float gameTime)
        {
            // 웨이브가 있으면 기본 스폰 건너뛰기
            if (_spawnData != null && _spawnData.SpawnWaves.Count > 0)
            {
                var activeWaves = _spawnData.GetActiveWaves(gameTime);
                if (activeWaves.Count > 0) return;
            }

            // 기본 몬스터 스폰
            _spawnTimer -= Time.deltaTime;

            if (_spawnTimer <= 0f)
            {
                float interval = _spawnData != null
                    ? _spawnData.GetSpawnInterval(gameTime)
                    : 2f;

                int count = _spawnData != null
                    ? _spawnData.GetSpawnCount(gameTime)
                    : 1;

                for (int i = 0; i < count; i++)
                {
                    SpawnDefaultMonster(gameTime);
                }

                _spawnTimer = interval;
            }
        }

        /// <summary>
        /// 웨이브에서 몬스터를 스폰합니다.
        /// </summary>
        private void SpawnMonsterFromWave(SpawnWaveEntry wave, float gameTime)
        {
            if (wave.monsterData == null) return;

            // 최대 몬스터 수 체크
            if (_spawnData != null && _activeMonsterCount >= _spawnData.MaxMonsterCount)
            {
                return;
            }

            for (int i = 0; i < wave.spawnCount; i++)
            {
                Vector2 spawnPos = GetSpawnPosition();
                SpawnMonster(wave.monsterData, spawnPos, wave.healthMultiplier, wave.speedMultiplier);
            }
        }

        /// <summary>
        /// 기본 몬스터를 스폰합니다.
        /// </summary>
        private void SpawnDefaultMonster(float gameTime)
        {
            if (_defaultMonsterData == null) return;

            // 최대 몬스터 수 체크
            if (_spawnData != null && _activeMonsterCount >= _spawnData.MaxMonsterCount)
            {
                return;
            }

            Vector2 spawnPos = GetSpawnPosition();

            float healthMult = _spawnData != null ? _spawnData.GetHealthMultiplier(gameTime) : 1f;
            float speedMult = _spawnData != null ? _spawnData.GetSpeedMultiplier(gameTime) : 1f;

            SpawnMonster(_defaultMonsterData, spawnPos, healthMult, speedMult);
        }

        /// <summary>
        /// 몬스터를 스폰합니다.
        /// </summary>
        private void SpawnMonster(MonsterDataSO monsterData, Vector2 position, float healthMult = 1f, float speedMult = 1f)
        {
            if (monsterData == null || monsterData.Prefab == null) return;

            string poolKey = GetPoolKey(monsterData);

            // 풀이 없으면 생성
            if (!PoolManager.Instance.HasPool(poolKey))
            {
                CreateMonsterPool(monsterData);
            }

            // 풀에서 가져오기
            var monster = PoolManager.Instance.Get<MonsterBase>(poolKey, position, Quaternion.identity);

            if (monster != null)
            {
                // 몬스터 데이터 설정
                monster.SetMonsterData(monsterData);

                // TODO: 난이도 배율 적용 (MonsterBase에 메서드 추가 필요)

                // 풀 키 설정
                var poolable = monster.GetComponent<PoolableObject>();
                if (poolable != null)
                {
                    poolable.PoolKey = poolKey;
                }

                _activeMonsters.Add(monster);
                _activeMonsterCount++;

                if (_debugLog)
                {
                    Debug.Log($"[MonsterSpawner] {monsterData.DisplayName} 스폰 at {position}");
                }
            }
        }

        /// <summary>
        /// 보스 스폰을 체크합니다.
        /// </summary>
        private void CheckBossSpawn(float gameTime)
        {
            if (_spawnData == null) return;

            var bossToSpawn = _spawnData.GetBossToSpawn(gameTime, _lastBossCheckTime);

            if (bossToSpawn != null)
            {
                SpawnBoss(bossToSpawn);
            }

            _lastBossCheckTime = gameTime;
        }

        /// <summary>
        /// 보스를 스폰합니다.
        /// </summary>
        private void SpawnBoss(BossSpawnEntry bossEntry)
        {
            if (bossEntry.bossData == null) return;

            // 일반 스폰 일시 정지
            if (bossEntry.pauseNormalSpawns)
            {
                _normalSpawnPaused = true;
            }

            Vector2 spawnPos = GetSpawnPosition();
            SpawnMonster(bossEntry.bossData, spawnPos, bossEntry.healthMultiplier, 1f);

            // 보스 등장 이벤트
            EventManager.Instance.Publish(GameEvents.OnBossSpawned, bossEntry.bossData);

            if (_debugLog)
            {
                Debug.Log($"[MonsterSpawner] 보스 스폰: {bossEntry.bossData.DisplayName}");
            }
        }

        #endregion

        #region 유틸리티

        /// <summary>
        /// 스폰 위치를 계산합니다.
        /// </summary>
        private Vector2 GetSpawnPosition()
        {
            Vector2 playerPos = Player.Instance != null
                ? (Vector2)Player.Instance.transform.position
                : Vector2.zero;

            if (_spawnData != null)
            {
                return _spawnData.GetRandomSpawnPosition(playerPos);
            }

            // 기본 스폰 위치 계산
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(8f, 12f);
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

            return playerPos + offset;
        }

        /// <summary>
        /// 현재 게임 시간을 반환합니다.
        /// </summary>
        private float GetGameTime()
        {
            if (GameManager.Instance != null)
            {
                return GameManager.Instance.ElapsedTime;
            }

            return Time.time;
        }

        #endregion

        #region 이벤트 핸들러

        private void OnGameStart()
        {
            StartSpawning();
        }

        private void OnGameOver()
        {
            StopSpawning();
        }

        private void OnMonsterKilled(GameObject monsterObj)
        {
            _activeMonsterCount = Mathf.Max(0, _activeMonsterCount - 1);

            // 리스트에서 제거
            if (monsterObj != null)
            {
                var monster = monsterObj.GetComponent<MonsterBase>();
                if (monster != null)
                {
                    _activeMonsters.Remove(monster);
                }

                // 보스 처치 시 일반 스폰 재개
                if (_normalSpawnPaused && monster != null && monster.MonsterData != null && monster.MonsterData.IsBoss)
                {
                    _normalSpawnPaused = false;
                    EventManager.Instance.Publish(GameEvents.OnBossDefeated, monster.MonsterData);
                }
            }
        }

        #endregion
    }
}
