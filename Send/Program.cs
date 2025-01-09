using RabbitMQ.Client;
using System.Text;

// Conexão ao nó RabbitMQ
var factory = new ConnectionFactory { HostName = "localhost" };
// Abre conexão com o nó
using var connection = await factory.CreateConnectionAsync();
// Cria canal onde a fila será definida
using var channel = await connection.CreateChannelAsync();

// Cria fila 
await channel.QueueDeclareAsync
(
    queue: "hello",
    durable: false,
    exclusive: false,
    autoDelete: false,
    arguments: null
);

// Cria mensagem a ser posta na fila
const string message = "Hello World!";
// Codifica a mensagem em um array de bytes
var body = Encoding.UTF8.GetBytes(message);

// Publica a mesagem informando a fila e o corpo da mensagem
await channel.BasicPublishAsync
(
    exchange: string.Empty,
    routingKey: "hello",
    body: body
);

Console.WriteLine($" [X] Sent {message}");

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();