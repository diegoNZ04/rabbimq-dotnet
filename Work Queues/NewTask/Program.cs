using RabbitMQ.Client;
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

var message = GetMessage(args);
var body = Encoding.UTF8.GetBytes(message);

await channel.BasicPublishAsync
(
    exchange: string.Empty,
    routingKey: "hello",
    body: body
);


Console.WriteLine($" [X] Sent {message}");

static string GetMessage(string[] args)
{
    return ((args.Length > 0) ? string.Join(" ", args) : "Hello World");
}