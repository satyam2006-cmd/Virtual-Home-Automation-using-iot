#include <ESP8266WiFi.h>
#include <ESP8266WebServer.h>
#include <DHT.h>

// ---------------- PIN CONFIGURATION ----------------

#define LDR_PIN A0
#define LED_PIN D5

#define DHTPIN D2
#define DHTTYPE DHT11

// ---------------- WIFI CONFIGURATION ----------------

const char* ssid = "YOUR_WIFI_SSID";
const char* password = "YOUR_WIFI_PASSWORD";

// ---------------- SERVER ----------------

ESP8266WebServer server(80);

// ---------------- LDR CONFIGURATION ----------------

int DARK_THRESHOLD = 400;
int BRIGHT_THRESHOLD = 700;

int ldrValue = 0;
bool lightState = false;

// ---------------- DHT11 CONFIGURATION ----------------

DHT dht(DHTPIN, DHTTYPE);

float temperature = 0.0;
float humidity = 0.0;

// ---------------- FAN AUTOMATION ----------------

#define FAN_ON_TEMP 30.0
#define FAN_OFF_TEMP 28.0

bool fanState = false;

// ---------------- SETUP ----------------

void setup()
{
    Serial.begin(115200);
    delay(1000);

    pinMode(LED_PIN, OUTPUT);
    digitalWrite(LED_PIN, LOW);

    dht.begin();

    Serial.println();
    Serial.println("================================");
    Serial.println(" SMART HOME DIGITAL TWIN");
    Serial.println("================================");

    // Connect to WiFi
    Serial.println("Connecting to WiFi...");

    WiFi.begin(ssid, password);

    while (WiFi.status() != WL_CONNECTED)
    {
        delay(500);
        Serial.print(".");
    }

    Serial.println();
    Serial.println("WiFi Connected!");

    Serial.print("ESP8266 IP Address: ");
    Serial.println(WiFi.localIP());

    // ---------------- JSON API ----------------

    server.on("/data", []()
    {
        String json = "{";

        json += "\"ldr\":" + String(ldrValue) + ",";
        json += "\"dark\":" + String(lightState ? "true" : "false") + ",";
        json += "\"lightState\":" + String(lightState ? "true" : "false") + ",";
        json += "\"temperature\":" + String(temperature, 1) + ",";
        json += "\"humidity\":" + String(humidity, 1) + ",";
        json += "\"fanState\":" + String(fanState ? "true" : "false");

        json += "}";

        server.send(200, "application/json", json);
    });

    // Root endpoint
    server.on("/", []()
    {
        server.send(200, "text/plain",
                    "Smart Home Digital Twin ESP8266 Server Running. Open /data for sensor values.");
    });

    server.begin();

    Serial.println("Smart Home Web Server Started!");
    Serial.println("Open /data to view sensor values.");
}

// ---------------- READ LDR ----------------

void readLDR()
{
    ldrValue = analogRead(LDR_PIN);

    // Hysteresis logic
    if (ldrValue < DARK_THRESHOLD)
    {
        lightState = true;
    }
    else if (ldrValue > BRIGHT_THRESHOLD)
    {
        lightState = false;
    }

    // Physical LED control
    digitalWrite(LED_PIN, lightState ? HIGH : LOW);

    Serial.print("LDR: ");
    Serial.print(ldrValue);

    Serial.print(" | Light: ");
    Serial.println(lightState ? "ON" : "OFF");
}

// ---------------- READ DHT11 ----------------

void readTemperature()
{
    float t = dht.readTemperature();
    float h = dht.readHumidity();

    if (isnan(t) || isnan(h))
    {
        Serial.println("DHT11 reading failed! Check sensor wiring.");
        return;
    }

    temperature = t;
    humidity = h;

    // Fan automation with hysteresis
    if (temperature >= FAN_ON_TEMP)
    {
        fanState = true;
    }
    else if (temperature <= FAN_OFF_TEMP)
    {
        fanState = false;
    }

    Serial.print("Temperature: ");
    Serial.print(temperature);
    Serial.print(" C | Humidity: ");
    Serial.print(humidity);
    Serial.print(" % | Fan: ");
    Serial.println(fanState ? "ON" : "OFF");
}

// ---------------- MAIN LOOP ----------------

void loop()
{
    server.handleClient();

    static unsigned long previousLDRRead = 0;
    static unsigned long previousDHTRead = 0;

    unsigned long currentMillis = millis();

    // Read LDR every 500 milliseconds
    if (currentMillis - previousLDRRead >= 500)
    {
        previousLDRRead = currentMillis;
        readLDR();
    }

    // Read DHT11 every 2 seconds
    if (currentMillis - previousDHTRead >= 2000)
    {
        previousDHTRead = currentMillis;
        readTemperature();
    }
}
