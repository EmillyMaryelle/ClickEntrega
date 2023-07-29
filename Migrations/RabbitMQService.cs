using System;
using System.Text;
using RabbitMQ.Client;


namespace ApiProductsRest.Migrations
{
      public class RabbitMQService
      {
        private readonly string _rabbitMQConnectionString;
        private readonly string _queueName;

        public RabbitMQService(string connectionString, string queueName)
        {
            _rabbitMQConnectionString = connectionString;
            _queueName = queueName;
        }

        public void EnviarMensagemParaFila(int idProduto, string nomeProduto)
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(_rabbitMQConnectionString)
            };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                // Declara a fila para onde a mensagem será enviada
                channel.QueueDeclare(queue: _queueName,
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                // Cria a mensagem em formato JSON
                var mensagem = new { Id = idProduto, Nome = nomeProduto };
                var mensagemJson = Newtonsoft.Json.JsonConvert.SerializeObject(mensagem);
                var body = Encoding.UTF8.GetBytes(mensagemJson);

                // Envia a mensagem para a fila
                channel.BasicPublish(exchange: "",
                                     routingKey: _queueName,
                                     basicProperties: null,
                                     body: body);
            }
        }
      }

}
