using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Networking;

public class Timer : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI timerText;
    float elapsedTime;
    bool isTimerRunning = true;
    string serverUrl = "http://localhost:5000";
    float checkInterval = 1f; 

    void Start()
    {
        // Start the server check coroutine once
        StartCoroutine(CheckShelvesPeriodically());
    }

    void Update()
    {
        if (isTimerRunning)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimerDisplay();
        }
    }

    void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(elapsedTime / 60);
        int seconds = Mathf.FloorToInt(elapsedTime % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    string FormatTimeForLog()
    {
        int minutes = Mathf.FloorToInt(elapsedTime / 60);
        int seconds = Mathf.FloorToInt(elapsedTime % 60);
        int milliseconds = Mathf.FloorToInt((elapsedTime * 1000) % 1000);
        return string.Format("{0:00}:{1:00}.{2:000}", minutes, seconds, milliseconds);
    }

    IEnumerator CheckShelvesPeriodically()
    {
        while (isTimerRunning)
        {
            yield return new WaitForSeconds(checkInterval);
            
            UnityWebRequest www = UnityWebRequest.Get($"{serverUrl}/state");
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = www.downloadHandler.text;
                StateResponse state = JsonUtility.FromJson<StateResponse>(jsonResponse);
                
                bool allShelvesFull = true;
                foreach (Shelf shelf in state.shelves)
                {
                    if (shelf.box_count < 5)
                    {
                        allShelvesFull = false;
                        break;
                    }
                }

                if (allShelvesFull)
                {
                    isTimerRunning = false;
                    string finalTime = FormatTimeForLog();
                    Debug.Log($"Timer stopped - All shelves are full! Time spent: {finalTime}");
                }
            }
        }
    }
}

[System.Serializable]
public class StateResponse
{
    public List<Shelf> shelves;
}

[System.Serializable]
public class Shelf
{
    public Vector2 position;
    public int box_count;
}