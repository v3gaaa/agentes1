using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;
using System.Linq;

public class SimulationController : MonoBehaviour
{
    public GameObject agentPrefab;
    public GameObject boxPrefab;
    public GameObject shelvePrefab;

    private Dictionary<int, GameObject> agents = new Dictionary<int, GameObject>();
    private Dictionary<int, GameObject> boxes = new Dictionary<int, GameObject>();
    private List<GameObject> shelves = new List<GameObject>();

    private float updateInterval = 0.1f;
    private string initUrl = "http://localhost:5000/init";
    private string stateUrl = "http://localhost:5000/state";

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
            foreach (JObject agentData in data["agents"])
            {
                int agentId = agentData["id"].Value<int>();
                Vector3 position = new Vector3(agentData["position"][0].Value<float>(), 0, agentData["position"][1].Value<float>());
                if (agents.ContainsKey(agentId))
                {
                    agents[agentId].transform.position = position;
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

            // Remover cajas visuales que ya no existen en la simulación
            List<int> keysToRemove = boxes.Keys.Except(activeBoxIds).ToList();
            foreach (int key in keysToRemove)
            {
                Destroy(boxes[key]);
                boxes.Remove(key);
            }

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
        }
    }
}
