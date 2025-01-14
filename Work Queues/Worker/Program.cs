using RabbitMQ.Client;
using RabbitMQ.Client.Events;
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

// Nao dar mais de uma mensagem a um worker por vez
await channel.BasicQosAsync
(
    prefetchSize: 0,
    prefetchCount: 1,
    global: false
);

Console.WriteLine(" [*] Waiting for messages.");

var consumer = new AsyncEventingBasicConsumer(channel);
// Manipulador de eventos lambda em async
consumer.ReceivedAsync += async (model, ea) =>
{
    byte[] body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    Console.WriteLine($" [x] Received {message}");

    // Tarefa falsa para simular tempo de execucao
    int dots = message.Split('.').Length - 1;
    await Task.Delay(dots * 1000);

    Console.WriteLine(" [x] Done");

    // Aqui o cannal pode ser acessado como ((AsyncEventingBasicConsumer)sender).Channel
    await channel.BasicAckAsync
    (
        deliveryTag: ea.DeliveryTag,
        multiple: false
    );
};

// Desativando reconhecimento automatico
await channel.BasicConsumeAsync
(
    "task_queue",
    autoAck: true,
    consumer: consumer
);

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();