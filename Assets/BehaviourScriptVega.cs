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

    private Dictionary<int, GameObject> agents = new Dictionary<int, GameObject>();
    private Dictionary<int, GameObject> boxes = new Dictionary<int, GameObject>();
    private List<GameObject> shelves = new List<GameObject>();

    private float updateInterval = 0.1f;
    private string initUrl = "http://localhost:5000/init";
    private string stateUrl = "http://localhost:5000/state";
    private string acknowledgeUrl = "http://localhost:5000/acknowledge";

    private int totalSteps = 0;

    void Start()
    {
        StartCoroutine(InitializeSimulation());
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
            using (UnityWebRequest request = UnityWebRequest.Get(stateUrl))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    ProcessServerData(request.downloadHandler.text, false);

                    // Enviar confirmación al servidor
                    StartCoroutine(SendAcknowledgment(totalSteps));
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
            // Update agents
            foreach (JObject agentData in data["agents"])
            {
                int agentId = agentData["id"].Value<int>();
                Vector3 position = new Vector3(agentData["position"][0].Value<float>(), 0, agentData["position"][1].Value<float>());
                if (agents.ContainsKey(agentId))
                {
                    GameObject agent = agents[agentId];
                    Vector3 previousPosition = agent.transform.position;

                    agent.transform.position = position;

                    Vector3 direction = position - previousPosition;
                    if (direction != Vector3.zero)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(direction);
                        agent.transform.rotation = Quaternion.Lerp(agent.transform.rotation, targetRotation, 0.5f);
                    }
                }
            }

            // Update boxes
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

            // Update shelves
            foreach (JObject shelveData in data["shelves"])
            {
                Vector3 position = new Vector3(
                    shelveData["position"][0].Value<float>(),
                    0,
                    shelveData["position"][1].Value<float>()
                );
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

            // Update simulation info
            totalSteps++;
            if (simulationInfoText != null)
            {
                simulationInfoText.text = $"Steps: {totalSteps}\nActive Boxes: {boxes.Count}\nAgents: {agents.Count}";
            }
        }
    }

    IEnumerator SendAcknowledgment(int step)
    {
        JObject acknowledgment = new JObject();
        acknowledgment["step"] = step;

        using (UnityWebRequest request = UnityWebRequest.PostWwwForm(acknowledgeUrl, acknowledgment.ToString()))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(acknowledgment.ToString());
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error sending acknowledgment: " + request.error);
            }
            else
            {
                Debug.Log($"Acknowledgment sent for step {step}");
            }
        }
    }
}
