using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

var factory = new ConnectionFactory { HostName = "localhost" };
using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

await channel.QueueDeclareAsync
(
    queue: "hello",
    durable: false,
    exclusive: false,
    autoDelete: false,
    arguments: null
);

// Solicita a entrega das mensagens e fornece retorno de chamada
var consumer = new AsyncEventingBasicConsumer(channel);
// Recebe a mensagem da fila
consumer.ReceivedAsync += (model, ea) =>
{
    var body = ea.Body.ToArray();
    // Converte para string
    var message = Encoding.UTF8.GetString(body);
    // Imprime a mensagem
    Console.WriteLine($" [x] Received {message}");
    return Task.CompletedTask;
};

// Indica o consumo da mensagem
await channel.BasicConsumeAsync
(
    "hello",
    autoAck: true,
    consumer: consumer
);

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();