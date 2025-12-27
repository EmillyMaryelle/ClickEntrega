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
              
                channel.QueueDeclare(queue: _queueName,
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

           
                var mensagem = new { Id = idProduto, Nome = nomeProduto };
                var mensagemJson = Newtonsoft.Json.JsonConvert.SerializeObject(mensagem);
                var body = Encoding.UTF8.GetBytes(mensagemJson);

                channel.BasicPublish(exchange: "",
                                     routingKey: _queueName,
                                     basicProperties: null,
                                     body: body);
            }
        }
      }

}
