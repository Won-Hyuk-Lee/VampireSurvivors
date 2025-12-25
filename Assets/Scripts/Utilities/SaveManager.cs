using System;
using System.IO;
using UnityEngine;

namespace VampireSurvivors.Utilities
{
    /// <summary>
    /// 게임 저장 데이터 클래스.
    /// JSON으로 직렬화되어 파일에 저장됩니다.
    /// 필요에 따라 필드를 추가/수정하세요.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>최고 점수</summary>
        public int highScore;

        /// <summary>최장 생존 시간 (초)</summary>
        public float bestTime;

        /// <summary>누적 처치 수</summary>
        public int totalKills;

        /// <summary>누적 골드</summary>
        public int totalGold;

        /// <summary>해금된 캐릭터 ID 배열</summary>
        public int[] unlockedCharacters;

        /// <summary>해금된 무기 ID 배열</summary>
        public int[] unlockedWeapons;

        /// <summary>마스터 볼륨 설정</summary>
        public float masterVolume;

        /// <summary>BGM 볼륨 설정</summary>
        public float bgmVolume;

        /// <summary>SFX 볼륨 설정</summary>
        public float sfxVolume;

        /// <summary>
        /// 기본값으로 초기화된 SaveData 생성자.
        /// 첫 실행 시 또는 저장 파일이 없을 때 사용됩니다.
        /// </summary>
        public SaveData()
        {
            highScore = 0;
            bestTime = 0f;
            totalKills = 0;
            totalGold = 0;
            unlockedCharacters = new int[] { 0 }; // 기본 캐릭터 해금
            unlockedWeapons = new int[] { 0 };    // 기본 무기 해금
            masterVolume = 1f;
            bgmVolume = 1f;
            sfxVolume = 1f;
        }
    }

    /// <summary>
    /// 게임 데이터 저장/불러오기를 관리하는 싱글톤 매니저.
    /// JSON 파일 저장을 기본으로 하고, 실패 시 PlayerPrefs를 백업으로 사용합니다.
    /// </summary>
    /// <example>
    /// // 데이터 읽기
    /// int highScore = SaveManager.Instance.CurrentData.highScore;
    ///
    /// // 데이터 업데이트 및 저장
    /// SaveManager.Instance.UpdateHighScore(1000);
    /// SaveManager.Instance.AddGold(50);
    ///
    /// // 수동 저장
    /// SaveManager.Instance.Save();
    ///
    /// // 저장 데이터 삭제
    /// SaveManager.Instance.DeleteSave();
    /// </example>
    public class SaveManager : Singleton<SaveManager>
    {
        /// <summary>저장 파일 이름</summary>
        private const string SAVE_FILE_NAME = "save_data.json";

        /// <summary>PlayerPrefs 백업용 키</summary>
        private const string PREFS_KEY = "SaveData";

        /// <summary>현재 로드된 저장 데이터</summary>
        public SaveData CurrentData { get; private set; }

        /// <summary>저장 파일 전체 경로 (Application.persistentDataPath 사용)</summary>
        private string SaveFilePath => Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);

        /// <summary>
        /// 싱글톤 초기화.
        /// 저장 데이터를 자동으로 로드합니다.
        /// </summary>
        protected override void OnSingletonAwake()
        {
            Load();
        }

        #region Save/Load with File

        /// <summary>
        /// 현재 데이터를 JSON 파일로 저장합니다.
        /// 파일 저장 실패 시 PlayerPrefs에 백업합니다.
        /// </summary>
        public void Save()
        {
            try
            {
                string json = JsonUtility.ToJson(CurrentData, true);
                File.WriteAllText(SaveFilePath, json);
                Debug.Log($"[SaveManager] Data saved to {SaveFilePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to save: {e.Message}");
                // 파일 저장 실패 시 PlayerPrefs 백업
                SaveToPlayerPrefs();
            }
        }

        /// <summary>
        /// 저장 파일에서 데이터를 로드합니다.
        /// 파일이 없거나 로드 실패 시 PlayerPrefs에서 로드를 시도합니다.
        /// 모두 실패하면 새 SaveData를 생성합니다.
        /// </summary>
        public void Load()
        {
            try
            {
                if (File.Exists(SaveFilePath))
                {
                    string json = File.ReadAllText(SaveFilePath);
                    CurrentData = JsonUtility.FromJson<SaveData>(json);
                    Debug.Log("[SaveManager] Data loaded from file");
                }
                else
                {
                    // 파일이 없으면 PlayerPrefs 시도
                    LoadFromPlayerPrefs();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to load: {e.Message}");
                LoadFromPlayerPrefs();
            }

            // 로드 실패 시 새 데이터 생성
            if (CurrentData == null)
            {
                CurrentData = new SaveData();
                Debug.Log("[SaveManager] Created new save data");
            }
        }

        #endregion

        #region PlayerPrefs Backup

        /// <summary>
        /// PlayerPrefs에 데이터를 백업합니다.
        /// 파일 저장 실패 시 폴백으로 사용됩니다.
        /// </summary>
        private void SaveToPlayerPrefs()
        {
            try
            {
                string json = JsonUtility.ToJson(CurrentData);
                PlayerPrefs.SetString(PREFS_KEY, json);
                PlayerPrefs.Save();
                Debug.Log("[SaveManager] Data saved to PlayerPrefs");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to save to PlayerPrefs: {e.Message}");
            }
        }

        /// <summary>
        /// PlayerPrefs에서 데이터를 로드합니다.
        /// </summary>
        private void LoadFromPlayerPrefs()
        {
            if (PlayerPrefs.HasKey(PREFS_KEY))
            {
                string json = PlayerPrefs.GetString(PREFS_KEY);
                CurrentData = JsonUtility.FromJson<SaveData>(json);
                Debug.Log("[SaveManager] Data loaded from PlayerPrefs");
            }
        }

        #endregion

        #region Data Operations

        /// <summary>
        /// 최고 점수를 업데이트합니다.
        /// 기존 기록보다 높은 경우에만 저장됩니다.
        /// </summary>
        /// <param name="score">새로운 점수</param>
        public void UpdateHighScore(int score)
        {
            if (score > CurrentData.highScore)
            {
                CurrentData.highScore = score;
                Save();
            }
        }

        /// <summary>
        /// 최장 생존 시간을 업데이트합니다.
        /// 기존 기록보다 긴 경우에만 저장됩니다.
        /// </summary>
        /// <param name="time">새로운 생존 시간</param>
        public void UpdateBestTime(float time)
        {
            if (time > CurrentData.bestTime)
            {
                CurrentData.bestTime = time;
                Save();
            }
        }

        /// <summary>
        /// 누적 처치 수를 추가합니다.
        /// </summary>
        /// <param name="kills">추가할 처치 수</param>
        public void AddKills(int kills)
        {
            CurrentData.totalKills += kills;
            Save();
        }

        /// <summary>
        /// 누적 골드를 추가합니다.
        /// </summary>
        /// <param name="gold">추가할 골드</param>
        public void AddGold(int gold)
        {
            CurrentData.totalGold += gold;
            Save();
        }

        /// <summary>
        /// 캐릭터를 해금합니다.
        /// 이미 해금된 경우 무시됩니다.
        /// </summary>
        /// <param name="characterId">해금할 캐릭터 ID</param>
        public void UnlockCharacter(int characterId)
        {
            if (!IsCharacterUnlocked(characterId))
            {
                var list = new System.Collections.Generic.List<int>(CurrentData.unlockedCharacters);
                list.Add(characterId);
                CurrentData.unlockedCharacters = list.ToArray();
                Save();
            }
        }

        /// <summary>
        /// 무기를 해금합니다.
        /// 이미 해금된 경우 무시됩니다.
        /// </summary>
        /// <param name="weaponId">해금할 무기 ID</param>
        public void UnlockWeapon(int weaponId)
        {
            if (!IsWeaponUnlocked(weaponId))
            {
                var list = new System.Collections.Generic.List<int>(CurrentData.unlockedWeapons);
                list.Add(weaponId);
                CurrentData.unlockedWeapons = list.ToArray();
                Save();
            }
        }

        /// <summary>
        /// 캐릭터 해금 여부를 확인합니다.
        /// </summary>
        /// <param name="characterId">확인할 캐릭터 ID</param>
        /// <returns>해금 여부</returns>
        public bool IsCharacterUnlocked(int characterId)
        {
            return Array.Exists(CurrentData.unlockedCharacters, id => id == characterId);
        }

        /// <summary>
        /// 무기 해금 여부를 확인합니다.
        /// </summary>
        /// <param name="weaponId">확인할 무기 ID</param>
        /// <returns>해금 여부</returns>
        public bool IsWeaponUnlocked(int weaponId)
        {
            return Array.Exists(CurrentData.unlockedWeapons, id => id == weaponId);
        }

        /// <summary>
        /// 오디오 설정을 저장합니다.
        /// </summary>
        /// <param name="master">마스터 볼륨</param>
        /// <param name="bgm">BGM 볼륨</param>
        /// <param name="sfx">SFX 볼륨</param>
        public void SaveAudioSettings(float master, float bgm, float sfx)
        {
            CurrentData.masterVolume = master;
            CurrentData.bgmVolume = bgm;
            CurrentData.sfxVolume = sfx;
            Save();
        }

        #endregion

        /// <summary>
        /// 모든 저장 데이터를 삭제합니다.
        /// 파일과 PlayerPrefs 모두 삭제하고 새 SaveData로 초기화합니다.
        /// </summary>
        public void DeleteSave()
        {
            // 파일 삭제
            if (File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
            }

            // PlayerPrefs 삭제
            PlayerPrefs.DeleteKey(PREFS_KEY);

            // 새 데이터로 초기화
            CurrentData = new SaveData();
            Debug.Log("[SaveManager] Save data deleted");
        }
    }
}
