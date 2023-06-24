//-------Khai bao thu vien----------------
#include <PubSubClient.h>
#include <WiFi.h>
#include <MQTT.h>
#include <string.h>
#include <stdlib.h>
#include <ArduinoJson.h>
#include <arduino-timer.h>
#include <DHT.h>

//--------Khai bao chan ngoai vi---------
#define DHTPIN 2
#define DHTTYPE DHT11
#define valvePin 14
#define fanPin 13
#define MQTT_USER "dkmt_btl"
#define MQTT_PASSWORD "dkmt_btl"
int smokeA0 = 34;


//-------Khai bao cac bien dieu khien cho he thong-----
float temperature = 0, humidity = 0;
float setTemp = 40.0, setHumid = 80.0 ;
// float setTemperature =40.0 ; setConcentration = 60.0;
int operation_mode = 1, valveStatus = 0, fanStatus = 0;

//mqtt broker config
char broker[] = "broker.hivemq.com";
int port = 1883;

//wifi config
char ssid[] = "my wifi";
char password[] = "123456788";

//client sub topics
char sub_topic[] = "/control-dkmt";

//client pub topic
char pub_topic[] = "/data-dkmt";

int serialBaudrate = 9600;

//----------Khai bao doi tuong MQTT Client và WifiClient
WiFiClient net;
MQTTClient client;
auto timer = timer_create_default();
DHT dht(DHTPIN, DHTTYPE);

////////////////////////////////////////////////////////////////////////////
//////Define functions
////////////////////////////////////////////////////////////////////////////

void connect() {
  while (!client.connected()) {
    Serial.print("Attempting MQTT connection...");
    String clientId = "ESP_dkmtBTL";
    clientId += String(random(0xffff), HEX);
    if (client.connect(clientId.c_str())) {
      Serial.println("connected \n");
      Serial.println(clientId);
      client.subscribe(sub_topic);
    } else {
      Serial.print("failed, rc=");
      // Serial.print(client.state());
      Serial.println(" try again in 2 seconds");
      delay(2000);
    }
  }
  Serial.println("\nConnected!");

}


void messageReceived(String &topic, String &payload) {
  Serial.println(payload);
  DynamicJsonDocument control(256);
  deserializeJson(control, payload);
  int control_mode = control["mode"];
  operation_mode = control_mode;
  if (operation_mode == 0) {
    int valve = control["valve"];
    int fan = control["fan"];
    if (valve == 1) {
      digitalWrite(valvePin, HIGH);
      valveStatus = 1;
      Serial.println("valve on");
    } else {
      digitalWrite(valvePin, LOW);
      valveStatus = 0;
      Serial.println("valve off");
    }

    if (fan == 1) {
      digitalWrite(fanPin, HIGH);
      fanStatus = 1;
      Serial.println("fan on");
    } else {
      digitalWrite(fanPin, LOW);
      fanStatus = 0;
      Serial.println("fan off");
    }
  } 
  else 
   {
       float t_sv = control["setTemp"];
       float h_sv = control["setHumid"];
       setTemp = t_sv;
       setHumid = h_sv;
   } 
}

bool sendData(void *) {
  char value[160] = "";
  char temperature_string[50];
  char humidity_string[50];
  char mode_string[20];
  char fan_string[20];
  char valvePin_string[20];


  sprintf(temperature_string, "\"temperature\":\"%0.2f\",", temperature);
  sprintf(humidity_string, "\"humidity\":\"%0.2f\",", humidity);
  sprintf(mode_string, "\"mode\":\"%d\",", operation_mode);
  sprintf(fan_string, "\"fan\":\"%d\",", fanStatus);
  sprintf(valvePin_string, "\"valve\":\"%d\"", valveStatus);


  strcat(value, "{");
  strcat(value, temperature_string);
  strcat(value, humidity_string);
  strcat(value, mode_string);
  strcat(value, fan_string);
  strcat(value, valvePin_string);
  strcat(value, "}");

  client.publish(pub_topic, value);
  Serial.println(value);
  return true;
}

bool readDHT(void *) {
  float h = dht.readHumidity();
  float t = dht.readTemperature();
  float f = dht.readTemperature(true);
  if (isnan(t)) {
    temperature = 0;
  }
  temperature = t;
  humidity = h ;
  return true;
}



  void control()
  {
    if (temperature > setTemp){
      digitalWrite(fanPin, HIGH);
      fanStatus = 1;
    }
    else{
      digitalWrite(fanPin, LOW);
      fanStatus = 0;
    }

    if (humidity < setHumid) {
      digitalWrite(valvePin, HIGH);
      valveStatus = 1;
    }
    else{
      digitalWrite(valvePin, LOW);
      valveStatus = 0 ;      
    }



  }

///////////////////////////////////////////////////////////
/////////Main functions
///////////////////////////////////////////////////////////
void setup() {
  pinMode(valvePin, OUTPUT);
  pinMode(fanPin, OUTPUT);
  dht.begin();

  Serial.begin(serialBaudrate);
  Serial.print("Connecting to ");
  Serial.println(ssid);
  WiFi.begin(ssid, password);

  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }
  Serial.println("");
  Serial.println("WiFi connected");
  Serial.println("IP address: ");
  Serial.println(WiFi.localIP());

  client.begin(broker, port, net);
  client.onMessage(messageReceived);
  connect();
  timer.every(2000, sendData);
  timer.every(500, readDHT);
}

void loop() {
  timer.tick();
  client.loop();
  delay(10);

  if (!client.connected()) {
    connect();
  }

  if (operation_mode == 1) {
    control();
  }
  }

