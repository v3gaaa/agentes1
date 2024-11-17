using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;
using System.Linq;
using UnityEngine.UI;

public class SimulationController : MonoBehaviour
{
    public GameObject agentPrefab;
    public GameObject boxPrefab;
    public GameObject shelvePrefab;
    public Text simulationInfoText;

    private float cameraHeight = 1.6f;
    private float cameraNearClip = 0.1f;
    private float cameraFOV = 60f;

    private Dictionary<int, GameObject> agents = new Dictionary<int, GameObject>();
    private Dictionary<int, UnityEngine.Camera> agentCameras = new Dictionary<int, UnityEngine.Camera>();
    private Dictionary<int, GameObject> boxes = new Dictionary<int, GameObject>();
    private List<GameObject> shelves = new List<GameObject>();
    private Dictionary<int, bool> agentCarryingStatus = new Dictionary<int, bool>();
    private int stepCounter = 0;

    private float updateInterval = 0.1f;
    private string initUrl = "http://localhost:5000/init";
    private string stateUrl = "http://localhost:5000/state";
    private string uploadImageUrl = "http://localhost:5000/upload-image";

    private int totalSteps = 0;

    void Start()
    {
        StartCoroutine(InitializeSimulation());
    }

    private void SetupAgentCamera(GameObject agentObject, int agentId)
    {
        GameObject cameraObject = new GameObject($"AgentCamera_{agentId}");
        cameraObject.transform.SetParent(agentObject.transform);
        
        cameraObject.transform.localPosition = new Vector3(0, cameraHeight, 0.1f);
        cameraObject.transform.localRotation = Quaternion.identity;

        UnityEngine.Camera agentCamera = cameraObject.AddComponent<UnityEngine.Camera>();
        agentCamera.orthographic = false;
        agentCamera.fieldOfView = cameraFOV;
        agentCamera.nearClipPlane = cameraNearClip;
        agentCamera.farClipPlane = 1000f;
        
        agentCameras[agentId] = agentCamera;
        agentCarryingStatus[agentId] = false;
    }

    IEnumerator InitializeSimulation()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(initUrl))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                ProcessServerData(request.downloadHandler.text, true);
                StartCoroutine(UpdateSimulation());
            }
            else
            {
                Debug.LogError("Error initializing simulation: " + request.error);
            }
        }
    }

    IEnumerator UpdateSimulation()
    {
        while (true)
        {
            stepCounter++;

            if (stepCounter % 5 == 0)
            {
                foreach (var agentCamera in agentCameras)
                {
                    int agentId = agentCamera.Key;
                    
                    if (!agentCarryingStatus[agentId])
                    {
                        UnityEngine.Camera camera = agentCamera.Value;

                        RenderTexture renderTexture = new RenderTexture(256, 256, 24);
                        camera.targetTexture = renderTexture;
                        
                        Texture2D texture = new Texture2D(256, 256, TextureFormat.RGB24, false);
                        camera.Render();
                        RenderTexture.active = renderTexture;
                        texture.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
                        texture.Apply();
                        
                        camera.targetTexture = null;
                        RenderTexture.active = null;
                        Destroy(renderTexture);

                        byte[] imageBytes = texture.EncodeToPNG();
                        Destroy(texture);

                        yield return StartCoroutine(SendImageToServer(agentId, imageBytes));
                    }
                }
            }

            using (UnityWebRequest request = UnityWebRequest.Get(stateUrl))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    ProcessServerData(request.downloadHandler.text, false);
                }
                else
                {
                    Debug.LogError("Error getting state from server: " + request.error);
                    yield break;
                }
            }

            yield return new WaitForSeconds(updateInterval);
        }
    }

    IEnumerator SendImageToServer(int agentId, byte[] imageBytes)
    {
        UnityWebRequest request = UnityWebRequest.Put(uploadImageUrl, imageBytes);
        request.SetRequestHeader("Content-Type", "application/octet-stream");
        request.SetRequestHeader("Agent-Id", agentId.ToString());

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Error sending image for agent {agentId}: {request.error}");
        }
    }

    void ProcessServerData(string jsonData, bool initializing)
    {
        JObject data = JObject.Parse(jsonData);

        if (initializing)
        {
            foreach (JObject agentData in data["agents"])
            {
                int agentId = agentData["id"].Value<int>();
                Vector3 position = new Vector3(agentData["position"][0].Value<float>(), 0, agentData["position"][1].Value<float>());
                GameObject agentObject = Instantiate(agentPrefab, position, Quaternion.identity);
                agents[agentId] = agentObject;

                SetupAgentCamera(agentObject, agentId);
            }

            int boxId = 0;
            foreach (JObject boxData in data["boxes"])
            {
                Vector3 position = new Vector3(boxData["position"][0].Value<float>(), 0.5f, boxData["position"][1].Value<float>());
                GameObject boxObject = Instantiate(boxPrefab, position, Quaternion.identity);
                boxes[boxId] = boxObject;
                boxId++;
            }

            foreach (JObject shelveData in data["shelves"])
            {
                Vector3 position = new Vector3(shelveData["position"][0].Value<float>(), 0, shelveData["position"][1].Value<float>());
                GameObject shelveObject = Instantiate(shelvePrefab, position, Quaternion.identity);
                shelves.Add(shelveObject);
            }
        }
        else
        {
            foreach (JObject agentData in data["agents"])
            {
                int agentId = agentData["id"].Value<int>();
                Vector3 position = new Vector3(agentData["position"][0].Value<float>(), 0, agentData["position"][1].Value<float>());
                bool isCarrying = agentData["carrying_box"].Value<bool>();
                
                agentCarryingStatus[agentId] = isCarrying;

                if (agents.ContainsKey(agentId))
                {
                    GameObject agent = agents[agentId];
                    Vector3 previousPosition = agent.transform.position;

                    agent.transform.position = position;

                    Vector3 direction = position - previousPosition;
                    if (direction != Vector3.zero)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
                        agent.transform.rotation = Quaternion.Lerp(agent.transform.rotation, targetRotation, 0.5f);
                    }
                }
            }

            List<int> activeBoxIds = new List<int>();
            int boxIndex = 0;
            foreach (JObject boxData in data["boxes"])
            {
                Vector3 position = new Vector3(boxData["position"][0].Value<float>(), 0.5f, boxData["position"][1].Value<float>());
                if (boxes.ContainsKey(boxIndex))
                {
                    boxes[boxIndex].transform.position = position;
                }
                else
                {
                    GameObject boxObject = Instantiate(boxPrefab, position, Quaternion.identity);
                    boxes[boxIndex] = boxObject;
                }
                activeBoxIds.Add(boxIndex);
                boxIndex++;
            }

            List<int> keysToRemove = boxes.Keys.Except(activeBoxIds).ToList();
            foreach (int key in keysToRemove)
            {
                Destroy(boxes[key]);
                boxes.Remove(key);
            }

            foreach (JObject shelveData in data["shelves"])
            {
                Vector3 position = new Vector3(shelveData["position"][0].Value<float>(), 0, shelveData["position"][1].Value<float>());
                int boxCount = shelveData["box_count"].Value<int>();

                GameObject shelf = shelves.Find(s => s.transform.position.x == position.x && s.transform.position.z == position.z);
                if (shelf != null)
                {
                    foreach (Transform child in shelf.transform)
                    {
                        Destroy(child.gameObject);
                    }

                    for (int i = 1; i <= boxCount; i++)
                    {
                        Vector3 stackedPosition = new Vector3(position.x, i * 0.5f, position.z);
                        GameObject stackedBox = Instantiate(boxPrefab, stackedPosition, Quaternion.identity);
                        stackedBox.transform.SetParent(shelf.transform);
                    }
                }
            }

            totalSteps++;
            if (simulationInfoText != null)
            {
                simulationInfoText.text = $"Steps: {totalSteps}\nActive Boxes: {boxes.Count}\nAgents: {agents.Count}";
            }
        }
    }
}