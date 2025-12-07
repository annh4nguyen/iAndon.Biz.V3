using iAndon.Biz.Logic;
using System;
using System.Configuration;
using System.Windows.Forms;

namespace iAndon.Biz.Test
{
    public partial class Form1 : Form
    {
        MainApp Service = new MainApp();
        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            txtCustomerID.Text = ConfigurationManager.AppSettings["CustomerId"];
            txtWebSocketURL.Text = ConfigurationManager.AppSettings["Websocket_Url"];
            txtRabbitHost.Text = ConfigurationManager.AppSettings["MQTT.Host"];
            txtRabbitVHost.Text = ConfigurationManager.AppSettings["MQTT.Port"];
            txtRabbitUser.Text = ConfigurationManager.AppSettings["MQTT.User"];
            txtRabbitPassword.Text = ConfigurationManager.AppSettings["MQTT.Gateways"];
            txtRabbitInterval.Text = ConfigurationManager.AppSettings["MQTT.Topic"];
            txtArchiveInterval.Text = ConfigurationManager.AppSettings["archive_interval"];
            txtLiveInterval.Text = ConfigurationManager.AppSettings["data_live_interval"];
            txtLiveTime.Text = ConfigurationManager.AppSettings["data_live_time"];
            txtLLogPath.Text = ConfigurationManager.AppSettings["log_path"];
            txtLogLevel.Text = ConfigurationManager.AppSettings["log_level"];
        }
        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                btnStart.Visible = false;
                btnStop.Visible = true;
                Service.Start();
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}");
                btnStart.Visible = true;
                btnStop.Visible = false;
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            try
            {
                Service.Stop();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}");
            }
            finally
            {
                btnStart.Visible = true;
                btnStop.Visible = false;
            }
        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }
    }
}
