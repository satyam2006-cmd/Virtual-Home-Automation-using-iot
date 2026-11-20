using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;

public class SmartHomeFanController : MonoBehaviour
{
    [Header("ESP8266 Connection")]
    public string espURL = "http://192.168.1.100/data";
    public float updateInterval = 2f;

    [Header("Fan Configuration")]
    public Transform fanBlades;
    public float rotationSpeed = 900f;
    // The fan model is mounted so its local Z axis is the spin axis.
    public Vector3 rotationAxis = Vector3.forward;

    [Header("Sensor Rules (display only; controlled by firmware)")]
    public float fanOnTemperature = 30f;
    public float fanOffTemperature = 28f;

    [Header("Sensor Displays")]
    public TMP_Text temperatureText;
    public TMP_Text fanStatusText;

    [Header("Fan Status")]
    public bool fanIsRunning;

    public bool sensorConnected;

    private float currentTemperature;
    private float currentHumidity;
    private SmartHomeLightController lightController;
    [System.Serializable]
    public class SensorData
    {
        public int ldr;
        public bool dark;
        public bool lightState;
        public float temperature;
        public float humidity;
        public bool fanState;
    }

    void Start()
    {
        lightController = FindFirstObjectByType<SmartHomeLightController>();

        if (fanBlades == null)
            Debug.LogError("Fan blades are not assigned.");

        StartCoroutine(ReadESPData());
    }

    IEnumerator ReadESPData()
    {
        while (true)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(espURL))
            {
                request.timeout = 5;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    SensorData data = JsonUtility.FromJson<SensorData>(request.downloadHandler.text);
                    if (data != null)
                    {
                        sensorConnected = true;
                        UpdateFan(data);
                    }
                    else
                    {
                        sensorConnected = false;
                        if (fanStatusText != null)
                            fanStatusText.text = "INVALID SENSOR DATA";
                    }
                }
                else
                {
                    sensorConnected = false;
                    Debug.LogWarning("ESP8266 fan data unavailable: " + request.error);
                    if (fanStatusText != null)
                        fanStatusText.text = "ESP OFFLINE";
                }
            }

            yield return new WaitForSeconds(updateInterval);
        }
    }

    void UpdateFan(SensorData data)
    {
        fanIsRunning = data.fanState;
        currentTemperature = data.temperature;
        currentHumidity = data.humidity;

        if (temperatureText != null)
            temperatureText.text = "Temperature: " + currentTemperature.ToString("F1") + " °C  |  Humidity: " + currentHumidity.ToString("F1") + " %";

        if (fanStatusText != null)
            fanStatusText.text = fanIsRunning ? "FAN ON" : "FAN OFF";
    }

    void Update()
    {
        if (fanIsRunning && fanBlades != null && rotationAxis.sqrMagnitude > 0f)
            fanBlades.Rotate(rotationAxis.normalized, rotationSpeed * Time.deltaTime, Space.Self);
    }

    void OnGUI()
    {
        GUI.skin.label.fontSize = 20;
        GUI.skin.box.fontSize = 24;

        GUI.Box(new Rect(24, 24, 620, 360), "SMART HOME DIGITAL TWIN");
        GUI.Label(new Rect(48, 78, 570, 34), "ESP8266: " + (sensorConnected ? "CONNECTED" : "WAITING FOR SENSOR"));
        GUI.Label(new Rect(48, 116, 570, 34), "Temperature: " + (sensorConnected ? currentTemperature.ToString("F1") + " °C" : "--"));
        GUI.Label(new Rect(48, 154, 570, 34), "Humidity: " + (sensorConnected ? currentHumidity.ToString("F1") + " %" : "--"));
        GUI.Label(new Rect(48, 192, 570, 34), "Fan: " + (sensorConnected ? (fanIsRunning ? "ON" : "OFF") : "--"));
        GUI.Label(new Rect(48, 230, 570, 34), "Fan ON >= " + fanOnTemperature.ToString("F1") + " °C  |  OFF <= " + fanOffTemperature.ToString("F1") + " °C");

        bool lightConnected = lightController != null && lightController.sensorConnected;
        GUI.Label(new Rect(48, 268, 570, 34), "LDR: " + (lightConnected ? lightController.ldrValue.ToString() : "--") +
            "  |  Light: " + (lightConnected ? (lightController.lightsAreOn ? "ON" : "OFF") : "--"));
        GUI.Label(new Rect(48, 306, 570, 34), "Light thresholds: Dark < 400  |  Bright > 700");
        GUI.Label(new Rect(48, 344, 570, 28), "LDR poll: 500 ms  |  DHT11 poll: 2000 ms");
    }
}
