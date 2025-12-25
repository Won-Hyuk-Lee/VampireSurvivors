using System;
using System.Collections.Generic;
using UnityEngine;

namespace VampireSurvivors.Data
{
    /// <summary>
    /// 특정 시간대에 스폰할 몬스터 정보
    /// </summary>
    [Serializable]
    public class SpawnWaveEntry
    {
        [Tooltip("이 웨이브가 시작되는 게임 시간 (초)")]
        public float startTime;

        [Tooltip("스폰할 몬스터 데이터")]
        public MonsterDataSO monsterData;

        [Tooltip("이 몬스터의 스폰 간격 (초)")]
        [Min(0.1f)]
        public float spawnInterval = 2f;

        [Tooltip("한 번에 스폰할 몬스터 수")]
        [Min(1)]
        public int spawnCount = 1;

        [Tooltip("체력 배율 (난이도 조절)")]
        [Min(0.1f)]
        public float healthMultiplier = 1f;

        [Tooltip("이동속도 배율 (난이도 조절)")]
        [Min(0.1f)]
        public float speedMultiplier = 1f;
    }

    /// <summary>
    /// 보스 스폰 정보
    /// </summary>
    [Serializable]
    public class BossSpawnEntry
    {
        [Tooltip("보스가 등장하는 게임 시간 (초)")]
        public float spawnTime;

        [Tooltip("보스 몬스터 데이터")]
        public MonsterDataSO bossData;

        [Tooltip("보스 등장 시 일반 몬스터 스폰 중단 여부")]
        public bool pauseNormalSpawns = true;

        [Tooltip("체력 배율")]
        [Min(1f)]
        public float healthMultiplier = 1f;
    }

    /// <summary>
    /// 몬스터 스폰 설정 데이터.
    /// 웨이브 기반 스폰, 난이도 곡선, 보스 등장 시간 등을 정의합니다.
    /// </summary>
    /// <remarks>
    /// Unity 에디터에서 Create > VampireSurvivors > Spawn Data 메뉴로 생성할 수 있습니다.
    /// </remarks>
    [CreateAssetMenu(fileName = "NewSpawnData", menuName = "VampireSurvivors/Spawn Data", order = 2)]
    public class SpawnDataSO : ScriptableObject
    {
        [Header("기본 스폰 설정")]
        [Tooltip("기본 스폰 간격 (초)")]
        [Min(0.1f)]
        [SerializeField]
        private float _baseSpawnInterval = 2f;

        [Tooltip("최소 스폰 간격 (초) - 이 이하로는 빨라지지 않음")]
        [Min(0.1f)]
        [SerializeField]
        private float _minSpawnInterval = 0.3f;

        [Tooltip("초당 스폰 간격 감소율")]
        [Range(0f, 0.1f)]
        [SerializeField]
        private float _spawnIntervalDecreaseRate = 0.005f;

        [Header("스폰 범위")]
        [Tooltip("플레이어로부터 최소 스폰 거리")]
        [Min(1f)]
        [SerializeField]
        private float _minSpawnDistance = 8f;

        [Tooltip("플레이어로부터 최대 스폰 거리")]
        [Min(1f)]
        [SerializeField]
        private float _maxSpawnDistance = 12f;

        [Header("동시 스폰")]
        [Tooltip("화면에 존재할 수 있는 최대 몬스터 수")]
        [Min(1)]
        [SerializeField]
        private int _maxMonsterCount = 100;

        [Tooltip("한 번에 스폰할 기본 몬스터 수")]
        [Min(1)]
        [SerializeField]
        private int _baseSpawnCount = 1;

        [Tooltip("시간에 따른 동시 스폰 증가량 (분당)")]
        [Min(0)]
        [SerializeField]
        private float _spawnCountIncreasePerMinute = 0.5f;

        [Header("난이도 곡선")]
        [Tooltip("시간에 따른 체력 증가 배율 (분당)")]
        [Min(0)]
        [SerializeField]
        private float _healthIncreasePerMinute = 0.1f;

        [Tooltip("시간에 따른 이동속도 증가 배율 (분당)")]
        [Min(0)]
        [SerializeField]
        private float _speedIncreasePerMinute = 0.02f;

        [Header("웨이브 설정")]
        [Tooltip("시간대별 스폰 웨이브 목록")]
        [SerializeField]
        private List<SpawnWaveEntry> _spawnWaves = new List<SpawnWaveEntry>();

        [Header("보스 설정")]
        [Tooltip("보스 등장 정보")]
        [SerializeField]
        private List<BossSpawnEntry> _bossSpawns = new List<BossSpawnEntry>();

        #region Properties

        /// <summary>
        /// 기본 스폰 간격
        /// </summary>
        public float BaseSpawnInterval => _baseSpawnInterval;

        /// <summary>
        /// 최소 스폰 간격
        /// </summary>
        public float MinSpawnInterval => _minSpawnInterval;

        /// <summary>
        /// 스폰 간격 감소율
        /// </summary>
        public float SpawnIntervalDecreaseRate => _spawnIntervalDecreaseRate;

        /// <summary>
        /// 최소 스폰 거리
        /// </summary>
        public float MinSpawnDistance => _minSpawnDistance;

        /// <summary>
        /// 최대 스폰 거리
        /// </summary>
        public float MaxSpawnDistance => _maxSpawnDistance;

        /// <summary>
        /// 최대 몬스터 수
        /// </summary>
        public int MaxMonsterCount => _maxMonsterCount;

        /// <summary>
        /// 기본 스폰 수
        /// </summary>
        public int BaseSpawnCount => _baseSpawnCount;

        /// <summary>
        /// 스폰 웨이브 목록
        /// </summary>
        public IReadOnlyList<SpawnWaveEntry> SpawnWaves => _spawnWaves;

        /// <summary>
        /// 보스 스폰 목록
        /// </summary>
        public IReadOnlyList<BossSpawnEntry> BossSpawns => _bossSpawns;

        #endregion

        #region 계산 메서드

        /// <summary>
        /// 현재 게임 시간에 맞는 스폰 간격을 계산합니다.
        /// </summary>
        /// <param name="gameTime">현재 게임 경과 시간 (초)</param>
        /// <returns>계산된 스폰 간격</returns>
        public float GetSpawnInterval(float gameTime)
        {
            float interval = _baseSpawnInterval - (gameTime * _spawnIntervalDecreaseRate);
            return Mathf.Max(interval, _minSpawnInterval);
        }

        /// <summary>
        /// 현재 게임 시간에 맞는 동시 스폰 수를 계산합니다.
        /// </summary>
        /// <param name="gameTime">현재 게임 경과 시간 (초)</param>
        /// <returns>계산된 스폰 수</returns>
        public int GetSpawnCount(float gameTime)
        {
            float minutes = gameTime / 60f;
            int count = _baseSpawnCount + Mathf.FloorToInt(minutes * _spawnCountIncreasePerMinute);
            return count;
        }

        /// <summary>
        /// 현재 게임 시간에 맞는 체력 배율을 계산합니다.
        /// </summary>
        /// <param name="gameTime">현재 게임 경과 시간 (초)</param>
        /// <returns>체력 배율</returns>
        public float GetHealthMultiplier(float gameTime)
        {
            float minutes = gameTime / 60f;
            return 1f + (minutes * _healthIncreasePerMinute);
        }

        /// <summary>
        /// 현재 게임 시간에 맞는 이동속도 배율을 계산합니다.
        /// </summary>
        /// <param name="gameTime">현재 게임 경과 시간 (초)</param>
        /// <returns>이동속도 배율</returns>
        public float GetSpeedMultiplier(float gameTime)
        {
            float minutes = gameTime / 60f;
            return 1f + (minutes * _speedIncreasePerMinute);
        }

        /// <summary>
        /// 현재 게임 시간에 활성화된 스폰 웨이브들을 가져옵니다.
        /// </summary>
        /// <param name="gameTime">현재 게임 경과 시간 (초)</param>
        /// <returns>활성화된 웨이브 목록</returns>
        public List<SpawnWaveEntry> GetActiveWaves(float gameTime)
        {
            var activeWaves = new List<SpawnWaveEntry>();

            foreach (var wave in _spawnWaves)
            {
                if (gameTime >= wave.startTime)
                {
                    activeWaves.Add(wave);
                }
            }

            return activeWaves;
        }

        /// <summary>
        /// 특정 시간에 스폰해야 할 보스가 있는지 확인합니다.
        /// </summary>
        /// <param name="gameTime">현재 게임 경과 시간 (초)</param>
        /// <param name="lastCheckTime">마지막으로 확인한 시간</param>
        /// <returns>스폰할 보스 정보 또는 null</returns>
        public BossSpawnEntry GetBossToSpawn(float gameTime, float lastCheckTime)
        {
            foreach (var boss in _bossSpawns)
            {
                if (boss.spawnTime > lastCheckTime && boss.spawnTime <= gameTime)
                {
                    return boss;
                }
            }

            return null;
        }

        /// <summary>
        /// 랜덤 스폰 위치를 계산합니다.
        /// </summary>
        /// <param name="playerPosition">플레이어 위치</param>
        /// <returns>스폰 위치</returns>
        public Vector2 GetRandomSpawnPosition(Vector2 playerPosition)
        {
            // 랜덤 각도
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;

            // 랜덤 거리
            float distance = UnityEngine.Random.Range(_minSpawnDistance, _maxSpawnDistance);

            // 위치 계산
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

            return playerPosition + offset;
        }

        #endregion
    }
}
