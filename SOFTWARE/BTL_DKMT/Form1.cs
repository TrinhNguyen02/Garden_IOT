using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Forms;
using BTL_DKMT.Model;
using Guna.UI2.WinForms;

using Newtonsoft.Json;
using static Guna.UI2.Native.WinApi;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Receiving;
using System.Windows.Documents;

namespace BTL_DKMT
{

    public partial class Form1 : Form
    {
       
        
        public string StatusMode { set; get; }
        public string StatusFan { set; get; }
        public string StatusValve { set; get; }
       public string Temp_ref { set; get; }
        public string Humi_ref { set; get; }

        // public int Deg { get => Do; set { Do = value; OnPropertyChanged(); } }

        private MqttFactory mqttFactory;
        private IManagedMqttClient mqttClient;


        public Form1()
        {
            InitializeComponent();
            mqttFactory = new MqttFactory();
        }
       

        private void OnConnected(MqttClientConnectedEventArgs eventArgs)
        {
            // Khi kết nối thành công, đăng ký đọc dữ liệu từ một topic trong HiveMQ
            string topic = "/data-dkmt";
            mqttClient.SubscribeAsync(topic);
        }

        private void OnMessageReceived(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            // Khi nhận được tin nhắn từ HiveMQ, hiển thị nội dung lên TextBox
            string message = Encoding.UTF8.GetString(eventArgs.ApplicationMessage.Payload);
              
           
            DataReiceive dataReiceive = JsonConvert.DeserializeObject<DataReiceive>(message);
           // textBox1.Invoke(new Action(() => textBox1.Text = dataReiceive.mode));
            guna2CircleButton1.Invoke(new Action(() => guna2CircleButton1.Text = dataReiceive.temperature + "°C"));
            guna2CircleProgressBar1.Invoke(new Action(()=> guna2CircleProgressBar1.Value = (int)double.Parse(dataReiceive.temperature)));
           
            guna2CircleButton2.Invoke(new Action(() => guna2CircleButton2.Text = dataReiceive.humidity + "%"));
            guna2CircleProgressBar2.Invoke(new Action(() => guna2CircleProgressBar2.Value = (int)double.Parse(dataReiceive.humidity)));
           
            guna2ToggleSwitch3.Invoke(new Action(() => {
                if (dataReiceive.mode == "1")
                {
                    guna2ToggleSwitch3.Checked = true;
                    label6.Text = "Auto";
                    StatusMode = "1";
                }
                else
                {
                    guna2ToggleSwitch3.Checked = false;
                    label6.Text = "Manual";
                    StatusMode = "0";
                }
                }));
           
            guna2ToggleSwitch1.Invoke(new Action(() => {
                if (dataReiceive.valve =="1")
                {
                    guna2ToggleSwitch1.Checked = true;
                    label2.Text = "On";
                    guna2Panel3.FillColor = Color.Blue;
                    StatusValve = "1";

                }
                else
                {
                    guna2ToggleSwitch1.Checked = false;
                    label2.Text = "Off";
                    guna2Panel3.FillColor = Color.Silver;
                    StatusValve = "0";
                }
            }
                ));
            guna2ToggleSwitch2.Invoke(new Action(() =>
            {
                if (dataReiceive.fan =="1")
                {
                    guna2ToggleSwitch2.Checked = true;
                    label5.Text = "On";
                    guna2Panel4.FillColor = Color.Blue;
                    StatusFan = "1";

                }
                else
                {
                    guna2ToggleSwitch2.Checked = false;
                    label5.Text = "Off";
                    guna2Panel4.FillColor = Color.Silver;
                    StatusFan = "0";

                }
            }
                ));
        }
        private async void OnPublishData()
        {
            if (mqttClient != null && mqttClient.IsConnected)
            {
                string topic = "/control-dkmt";
                string message = $"{{\"fan\":\"{StatusFan}\",\"valve\":\"{StatusValve}\",\"mode\":\"{StatusMode}\",\"setTemp\":\"{Temp_ref}\",\"setHumid\":\"{Humi_ref}\"}}";

                var mqttMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(message)
                    .WithExactlyOnceQoS()
                    .WithRetainFlag()
                    .Build();

                await mqttClient.PublishAsync(mqttMessage);
            }
        }

        private void guna2ToggleSwitch3_Click(object sender, EventArgs e)
        {
            
            if (guna2ToggleSwitch3.Checked)
            {
                label6.Text = "Auto";
                StatusMode = "1";
                
            }
            else
            {
                
                label6.Text = "Manual";
                StatusMode = "0";
                
            }
            OnPublishData();
        }

        private void guna2ToggleSwitch2_Click(object sender, EventArgs e)
        {
            if (guna2ToggleSwitch3.Checked)
            {
                guna2ToggleSwitch2.Checked = false;
                System.Windows.MessageBox.Show("Vui lòng chuyển sang chế độ Manual để sử dụng chức năng này!",
                    "Thông báo", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            }
            else
            {
                if (guna2ToggleSwitch2.Checked)
                {
                    label5.Text = "On";
                    guna2Panel4.FillColor = Color.Blue;
                    StatusFan = "1";

                }
                else
                {
                    label5.Text = "Off";
                    guna2Panel4.FillColor = Color.Silver;
                    StatusFan = "0";

                }
                OnPublishData();
            }
        }

        private void guna2ToggleSwitch1_Click(object sender, EventArgs e)
        {
            if (guna2ToggleSwitch3.Checked)
            {
                guna2ToggleSwitch1.Checked = false;
                System.Windows.MessageBox.Show("Vui lòng chuyển sang chế độ Manual để sử dụng chức năng này!", "Thông báo", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            }
            else
            {
                if (guna2ToggleSwitch1.Checked)
                {
                    label2.Text = "On";
                    guna2Panel3.FillColor = Color.Blue;
                    StatusValve = "1";
                    
                }
                else
                {
                    label2.Text = "Off";
                    guna2Panel3.FillColor = Color.Silver;
                    StatusValve = "0";
                    
                }

                OnPublishData();
            }
        }
      
        private void Form1_Load(object sender, EventArgs e)
        {
            timer1.Enabled = true;
            StatusFan = "0";
            StatusValve = "0";
            Humi_ref = "30";
            Temp_ref = "27";
            textBox1.Text = "27";
            textBox2.Text = "30";
        }
        int cnt = 0;
        private async void timer1_Tick(object sender, EventArgs e)
        {
            string brokerHostname = "broker.hivemq.com";
            int brokerPort = 1883;

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(brokerHostname, brokerPort)
                .Build();

            mqttClient = mqttFactory.CreateManagedMqttClient();
            mqttClient.ConnectedHandler = new MqttClientConnectedHandlerDelegate(OnConnected);
            mqttClient.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(OnMessageReceived);

            await mqttClient.StartAsync(new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(options)
                .Build());
           
            timer1.Enabled = false;
            OnPublishData();
        }

        private void guna2Panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            
        }

        private void textBox1_KeyUp(object sender, KeyEventArgs e)
        {

            
        }

        private void textBox1_MouseEnter(object sender, EventArgs e)
        {
           
        }

        private void textBox2_MouseUp(object sender, MouseEventArgs e)
        {
            Humi_ref = textBox2.Text;
            OnPublishData();
        }

        private void textBox1_MouseUp(object sender, MouseEventArgs e)
        {
            Temp_ref = textBox1.Text;
            OnPublishData();
        }

        private void guna2ImageButton1_Click(object sender, EventArgs e)
        {

        }
    }
}
