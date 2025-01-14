using RabbitMQ.Client;
using System.Text;


var factory = new ConnectionFactory { HostName = "localhost" };
using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

// Declarando fila como duravel
await channel.QueueDeclareAsync
(
    queue: "task_queue",
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: null
);

// Var recebe mensagem da CLI
var message = GetMessage(args);
var body = Encoding.UTF8.GetBytes(message);

// Definir persistencia como true
var properties = new BasicProperties
{
    Persistent = true
};

await channel.BasicPublishAsync
(
    exchange: string.Empty,
    routingKey: "task_queue",
    body: body
);


Console.WriteLine($" [X] Sent {message}");

// Ler mensagem na CLI
static string GetMessage(string[] args)
{
    return ((args.Length > 0) ? string.Join(" ", args) : "Hello World");
}