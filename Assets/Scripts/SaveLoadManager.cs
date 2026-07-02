using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class SaveLoadManager : MonoBehaviour
{
    const string HIGH_SCORE_KEY = "BestWaveValue";
    const string ZOMBIES_KILLED_KEY = "TotalZombiesKilled";
    const string WORLDS_SAVED_KEY = "TotalWorldsSaved";
    const string TOTAL_PLAYTIME_KEY = "TotalPlaytimeSeconds";
    const string API_URL = "https://undeadcourier.hridya.tech/api/scores";

    public static SaveLoadManager Instance;
    private string playerId;
    public TextMeshProUGUI verificationCode;

    private float sessionStartTime;
    private float currentSessionTime;
    private bool isSessionActive = false;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        playerId = SystemInfo.deviceUniqueIdentifier;
        Debug.Log($"Player ID initialized: {playerId}");

        StartSession();
    }

    void OnApplicationQuit()
    {
        EndSession();
    }

    void OnApplicationPause(bool isPaused)
    {
        if (isPaused)
        {
            PauseSession();
        }
        else
        {
            ResumeSession();
        }
    }

    public void StartSession()
    {
        if (!isSessionActive)
        {
            sessionStartTime = Time.realtimeSinceStartup;
            isSessionActive = true;
            Debug.Log("Playtime tracking started");
        }
    }

    public void PauseSession()
    {
        if (isSessionActive)
        {
            currentSessionTime += Time.realtimeSinceStartup - sessionStartTime;
            isSessionActive = false;
            Debug.Log($"Session paused. Current session time: {currentSessionTime} seconds");
        }
    }

    public void ResumeSession()
    {
        if (!isSessionActive)
        {
            sessionStartTime = Time.realtimeSinceStartup;
            isSessionActive = true;
            Debug.Log("Session resumed");
        }
    }

    public void EndSession()
    {
        if (isSessionActive)
        {
            currentSessionTime += Time.realtimeSinceStartup - sessionStartTime;
            isSessionActive = false;
        }

        int totalPlaytime = PlayerPrefs.GetInt(TOTAL_PLAYTIME_KEY, 0);
        totalPlaytime += Mathf.RoundToInt(currentSessionTime);
        PlayerPrefs.SetInt(TOTAL_PLAYTIME_KEY, totalPlaytime);
        PlayerPrefs.Save();

        Debug.Log($"Session ended. Total playtime: {totalPlaytime} seconds");

        currentSessionTime = 0;
    }

    public int GetCurrentSessionTime()
    {
        float sessionTime = currentSessionTime;

        if (isSessionActive)
        {
            sessionTime += Time.realtimeSinceStartup - sessionStartTime;
        }

        return Mathf.RoundToInt(sessionTime);
    }

    public int GetTotalPlaytime()
    {
        int savedPlaytime = PlayerPrefs.GetInt(TOTAL_PLAYTIME_KEY, 0);
        int currentSession = GetCurrentSessionTime();
        return savedPlaytime + currentSession;
    }

    public void SaveScore(int wavesCompleted, int zombiesKilled)
    {
        Debug.Log($"SaveScore called with waves: {wavesCompleted}, zombies: {zombiesKilled}");

        int sessionDuration = GetCurrentSessionTime();

        StartCoroutine(PostScore(wavesCompleted, zombiesKilled, sessionDuration));

        int currentHighScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
        if (wavesCompleted > currentHighScore)
        {
            Debug.Log($"New local high score! Saving {wavesCompleted}");
            PlayerPrefs.SetInt(HIGH_SCORE_KEY, wavesCompleted);
            PlayerPrefs.Save();
        }

        int totalZombiesKilled = PlayerPrefs.GetInt(ZOMBIES_KILLED_KEY, 0);
        totalZombiesKilled += zombiesKilled;
        PlayerPrefs.SetInt(ZOMBIES_KILLED_KEY, totalZombiesKilled);
        PlayerPrefs.Save();
        Debug.Log($"Total zombies killed updated: {totalZombiesKilled}");
    }

    public void SaveHighScore(int score)
    {
        Debug.Log($"SaveHighScore called with score: {score}");
        int currentHighScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
        Debug.Log($"Current high score: {currentHighScore}");

        if (score > currentHighScore)
        {
            Debug.Log($"New local high score! Saving {score}");
            PlayerPrefs.SetInt(HIGH_SCORE_KEY, score);
            PlayerPrefs.Save();
        }
        else
        {
            Debug.Log("Score not higher than current local high score.");
        }
    }   

    public void IncrementWorldsSaved()
    {
        int worldsSaved = PlayerPrefs.GetInt(WORLDS_SAVED_KEY, 0);
        worldsSaved++;
        PlayerPrefs.SetInt(WORLDS_SAVED_KEY, worldsSaved);
        PlayerPrefs.Save();
        Debug.Log($"Worlds saved incremented to: {worldsSaved}");

        StartCoroutine(PostWorldSaved());
    }

    public int LoadWorldsSaved() => PlayerPrefs.GetInt(WORLDS_SAVED_KEY, 0);

    IEnumerator PostScore(int wavesCompleted, int zombiesKilled, int matchDuration)
    {
        Debug.Log($"PostScore called with waves: {wavesCompleted}, zombies: {zombiesKilled}, matchDuration: {matchDuration}");
        Debug.Log($"API URL: {API_URL}");

        int totalPlaytime = GetTotalPlaytime();

        var json = $"{{\"playerId\":\"{playerId}\",\"score\":{wavesCompleted},\"zombiesKilled\":{zombiesKilled},\"matchDuration\":{matchDuration},\"totalPlaytime\":{totalPlaytime}}}";
        Debug.Log($"Sending JSON: {json}");

        var request = new UnityEngine.Networking.UnityWebRequest(API_URL, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        Debug.Log("Sending web request...");
        yield return request.SendWebRequest();
        Debug.Log($"Request complete. Response code: {request.responseCode}");

        if (request.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Save failed: {request.error}");
            Debug.LogError($"Response: {request.downloadHandler?.text}");
        }
        else
        {
            Debug.Log("Save successful!");
            Debug.Log($"Response: {request.downloadHandler?.text}");

            try
            {
                var response = JsonUtility.FromJson<ScoreSaveResponse>(request.downloadHandler.text);
                if (response.updated)
                {
                    Debug.Log("New high score confirmed by server!");
                }
                else
                {
                    Debug.Log("Score saved but wasn't a new high score.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error parsing response: {e.Message}");
            }
        }
    }

    IEnumerator PostWorldSaved()
    {
        int worldsSaved = PlayerPrefs.GetInt(WORLDS_SAVED_KEY, 0);
        int currentHighScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
        int totalZombies = PlayerPrefs.GetInt(ZOMBIES_KILLED_KEY, 0);
        int totalPlaytime = GetTotalPlaytime();

        Debug.Log($"PostWorldSaved called with worlds saved: {worldsSaved}");

        var json = $"{{\"playerId\":\"{playerId}\",\"score\":{currentHighScore},\"zombiesKilled\":{totalZombies},\"worldsSaved\":{worldsSaved},\"totalPlaytime\":{totalPlaytime}}}";
        Debug.Log($"Sending JSON: {json}");

        var request = new UnityEngine.Networking.UnityWebRequest(API_URL, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            Debug.LogError($"World saved update failed: {request.error}");
            Debug.LogError($"Response: {request.downloadHandler?.text}");
        }
        else
        {
            Debug.Log("World saved update successful!");
            Debug.Log($"Response: {request.downloadHandler?.text}");
        }
    }

    public int LoadHighScore() => PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);

    public int LoadTotalZombiesKilled() => PlayerPrefs.GetInt(ZOMBIES_KILLED_KEY, 0);

    public void OpenPlayerProfile()
    {
        string url = $"https://undeadcourier.hridya.tech/player?player_id={playerId}";
        Debug.Log($"Opening player profile at: {url}");
        Application.OpenURL(url);
    }

    public void GenerateVerificationCode()
    {
        StartCoroutine(GetVerificationCode());
    }

    IEnumerator GetVerificationCode()
    {
        var url = "https://undeadcourier.hridya.tech/api/verification";
        var json = $"{{\"playerId\":\"{playerId}\"}}";

        var request = new UnityEngine.Networking.UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Verification code generation failed: {request.error}");
        }
        else
        {
            try
            {
                var response = JsonUtility.FromJson<VerificationResponse>(request.downloadHandler.text);
                verificationCode.gameObject.SetActive(true);
                verificationCode.text = $"Code: {response.code}";
                string verifyurl = $"https://undeadcourier.hridya.tech/verify";
                Application.OpenURL(verifyurl);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error parsing verification response: {e.Message}");
            }
        }
    }

    [System.Serializable]
    public class VerificationResponse
    {
        public string code;
    }

    [System.Serializable]
    public class ScoreResponse
    {
        public int waves_killed;
        public int zombies_killed;
        public int worlds_saved;
        public int total_playtime_seconds;
        public string updated_at;
    }

    [System.Serializable]
    public class ScoreSaveResponse
    {
        public bool success;
        public bool updated;
    }

    [System.Serializable]
    public class MatchData
    {
        public int waves_survived;
        public int zombies_killed;
        public int worlds_saved;
        public int match_duration_seconds;
        public string played_at;
    }

    [System.Serializable]
    public class PlayerDataResponse
    {
        public int waves_killed;
        public int zombies_killed;
        public int worlds_saved;
        public int total_playtime_seconds;
        public string updated_at;
        public List<MatchData> recent_matches;
    }
}