using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class BoxDetectionHandler : MonoBehaviour
{
    [Header("Network Settings")]
    private string serverUrl = "http://localhost:5000";
    private float pollInterval = 0.5f;

    [Header("UI Settings")]
    [SerializeField] private float messageDuration = 3f;
    [SerializeField] private int maxMessages = 5;
    [SerializeField] private Vector2 messagesPanelOffset = new Vector2(20, 20);
    
    private GameObject messagesPanel;
    private Queue<GameObject> activeMessages;

    [System.Serializable]
    private class DetectionData
    {
        public int agentId;
        public int numBoxes;
        public float[] position;
        public float confidence;
        public Vector2 Position => new Vector2(position[0], position[1]);
    }

    private void Awake()
    {
        CreateUIElements();
        activeMessages = new Queue<GameObject>();
    }

    void Start()
    {
        StartCoroutine(PollDetections());
    }

    private void CreateUIElements()
    {
        messagesPanel = new GameObject("MessagesPanel");
        messagesPanel.transform.SetParent(GameObject.Find("Canvas").transform, false);
        
        RectTransform panelRect = messagesPanel.AddComponent<RectTransform>();
        VerticalLayoutGroup verticalLayout = messagesPanel.AddComponent<VerticalLayoutGroup>();
        ContentSizeFitter sizeFitter = messagesPanel.AddComponent<ContentSizeFitter>();
        
        verticalLayout.spacing = 5;
        verticalLayout.padding = new RectOffset(10, 10, 10, 10);
        verticalLayout.childAlignment = TextAnchor.UpperLeft;
        verticalLayout.childControlHeight = true;
        verticalLayout.childControlWidth = true;
        
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        panelRect.anchorMin = new Vector2(0, 1);
        panelRect.anchorMax = new Vector2(0, 1);
        panelRect.pivot = new Vector2(0, 1);
        panelRect.anchoredPosition = messagesPanelOffset;
    }

    private GameObject CreateMessagePrefab()
    {
        GameObject messagePrefab = new GameObject("Message");
        messagePrefab.transform.SetParent(messagesPanel.transform, false);
        
        RectTransform rect = messagePrefab.AddComponent<RectTransform>();
        TextMeshProUGUI tmp = messagePrefab.AddComponent<TextMeshProUGUI>();
        
        tmp.fontSize = 16;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Left;
        
        rect.sizeDelta = new Vector2(200, 30);
        
        return messagePrefab;
    }

    IEnumerator PollDetections()
    {
        while (true)
        {
            using (UnityWebRequest request = UnityWebRequest.Get($"{serverUrl}/detections"))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string jsonResponse = request.downloadHandler.text;
                    ProcessDetections(jsonResponse);
                }
                else
                {
                    Debug.LogError($"Error fetching detections: {request.error}");
                }
            }

            yield return new WaitForSeconds(pollInterval);
        }
    }

    private void ProcessDetections(string jsonData)
    {
        try
        {
            DetectionData[] detections = JsonConvert.DeserializeObject<DetectionData[]>(jsonData);
            
            foreach (var detection in detections)
            {
                if (detection.numBoxes > 0)
                {
                    Debug.Log($"Agent {detection.agentId} found {detection.numBoxes} box(es)");
                    Debug.Log($"Position: ({detection.Position.x}, {detection.Position.y})");
                    Debug.Log($"Confidence: {detection.confidence:F2}%");
                    
                    ShowMessage(detection);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error processing detection data: {e.Message}");
        }
    }

    private void ShowMessage(DetectionData detection)
    {
        GameObject messageObj = CreateMessagePrefab();
        TextMeshProUGUI tmp = messageObj.GetComponent<TextMeshProUGUI>();
        tmp.text = $"Agent {detection.agentId} found {detection.numBoxes} box{(detection.numBoxes > 1 ? "es" : "")}";

        activeMessages.Enqueue(messageObj);
        if (activeMessages.Count > maxMessages)
        {
            GameObject oldestMessage = activeMessages.Dequeue();
            Destroy(oldestMessage);
        }

        StartCoroutine(FadeOutMessage(messageObj));
    }

    private IEnumerator FadeOutMessage(GameObject messageObj)
    {
        TextMeshProUGUI tmp = messageObj.GetComponent<TextMeshProUGUI>();
        float elapsedTime = 0;
        Color initialColor = tmp.color;

        yield return new WaitForSeconds(messageDuration - 1f);

        while (elapsedTime < 1f)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1, 0, elapsedTime);
            tmp.color = new Color(initialColor.r, initialColor.g, initialColor.b, alpha);
            yield return null;
        }

        activeMessages.Dequeue();
        Destroy(messageObj);
    }
}