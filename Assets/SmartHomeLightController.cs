using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;

public class SmartHomeLightController : MonoBehaviour
{
    [Header("ESP8266 Connection")]
    public string espURL = "http://192.168.1.100/data";
    public float updateInterval = 1f;

    [Header("Room Lights")]
    public Light virtualLight;
    [Tooltip("All room lights controlled by the ESP8266 lightState value.")]
    public Light[] sensorControlledLights;
    public Renderer bulbRenderer;

    [Header("Sensor Displays")]
    public TMP_Text sensorValueText;
    public TMP_Text statusText;

    [Header("Sensor State")]
    public int ldrValue;
    public bool lightsAreOn;
    public bool sensorConnected;

    [System.Serializable]
    public class SensorData
    {
        public int ldr;
        public bool dark;
        public bool lightState;
    }

    void Start()
    {
        if ((sensorControlledLights == null || sensorControlledLights.Length == 0) && virtualLight != null)
            sensorControlledLights = new[] { virtualLight };

        if (sensorControlledLights == null || sensorControlledLights.Length == 0)
            Debug.LogError("No controlled lights are assigned.");

        if (sensorControlledLights != null)
        {
            foreach (Light controlledLight in sensorControlledLights)
            {
                if (controlledLight == null) continue;
                controlledLight.gameObject.SetActive(true);
                controlledLight.intensity = 2f;
                controlledLight.range = 10f;
            }
        }

        StartCoroutine(ReadSensorData());
    }

    IEnumerator ReadSensorData()
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
                        UpdateVirtualHouse(data);
                    }
                    else
                    {
                        sensorConnected = false;
                        if (statusText != null)
                            statusText.text = "INVALID SENSOR DATA";
                    }
                }
                else
                {
                    sensorConnected = false;
                    Debug.LogWarning("ESP8266 light data unavailable: " + request.error);
                    if (statusText != null)
                        statusText.text = "ESP OFFLINE";
                }
            }

            yield return new WaitForSeconds(updateInterval);
        }
    }

    void UpdateVirtualHouse(SensorData data)
    {
        ldrValue = data.ldr;
        lightsAreOn = data.lightState;

        if (sensorControlledLights != null)
        {
            foreach (Light controlledLight in sensorControlledLights)
            {
                if (controlledLight != null)
                    controlledLight.enabled = data.lightState;
            }
        }

        if (bulbRenderer != null)
            bulbRenderer.material.color = data.lightState ? Color.yellow : Color.gray;

        if (sensorValueText != null)
            sensorValueText.text = "LDR: " + data.ldr;

        if (statusText != null)
            statusText.text = data.lightState ? "LIGHT ON" : "LIGHT OFF";
    }
}
