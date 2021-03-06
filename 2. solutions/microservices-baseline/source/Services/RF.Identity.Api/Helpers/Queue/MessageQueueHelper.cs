using RabbitMQ.Client;
using RF.Identity.Api.Helpers.Base;
using RF.Identity.Api.Helpers.KeyVault;
using RF.Identity.Domain.Entities.KeyVault;
using System;
using System.Text;
using System.Threading.Tasks;

namespace RF.Identity.Api.Helpers.Queue
{
    public class MessageQueueHelper : BaseHelper
    {
        public async Task QueueMessageAsync<T>(T model, string queue, KeyVaultConnectionInfo keyVaultConnection)
        {
            ConnectionFactory factory = new ConnectionFactory();
            factory.UserName = Settings.RabbitMQUsername;
            factory.Password = Settings.RabbitMQPassword;
            factory.HostName = Settings.RabbitMQHostname;
            factory.Port = Settings.RabbitMQPort;

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(model);
            var encrypted = string.Empty;

            using (KeyVaultHelper keyVaultHelper = new KeyVaultHelper(keyVaultConnection))
            {
                string secret = await keyVaultHelper.GetVaultKeyAsync(Settings.KeyVaultEncryptionKey);
                encrypted = NETCore.Encrypt.EncryptProvider.AESEncrypt(json, secret);
            }

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: queue,
                                    durable: true,
                                    exclusive: false,
                                    autoDelete: false,
                                    arguments: null);

                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;

                string message = encrypted;
                var body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish(exchange: "",
                                    routingKey: queue,
                                    basicProperties: properties,
                                    body: body);

                Console.WriteLine("Sent: {0}", message);
            }
        }
    }
}