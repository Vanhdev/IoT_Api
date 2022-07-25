using uPLibrary.Networking.M2Mqtt;

namespace IoT_Api
{
    public class Mqtt
    {
        static MqttClient _client;
        static public MqttClient Start(string hostName, int port)
        {
            string cid = Guid.NewGuid().ToString();
            _client = new MqttClient(hostName, port, false, MqttSslProtocols.None, null, null);
            _client.MqttMsgPublishReceived += (s, e) => {
                var msg = e.Message.ASCII();
                var doc = Document.Parse(msg);

                Models.Device.Process(doc);
            };
            
            _client.Connect(cid);
            _client.Subscribe(new string[] { "va_2022_device" }, new byte[] { 0 });

            return _client;
        }
    }
}
