# RabbitMQ-DotNet

O RabbitMQ é um servidor de mensageria *open source* desenvolvido em *Erlang*, implementado para suportar mensagens no protocolo *AMQP - Advanced Message Queuring Protocol*.
Ele lida com o tráfego de mensagens de forma rápida e confiável, é compatível com diversas linguagens de programação, possui uma interface de administração nativa e é multiplataforma.

## RabbitMQ - Conceitos

**Message** - É um bloco de dados binários que podem conter dados em diferentes formatos, como texto, JSON, XML, etc. É dividida em *Payload* que representa o corpo de dados que serão transmitidos e *Label* que descreve o *Payload*.

**Producer/Publish** - Responsável por incluir uma mensagem na fila.

**Exchange** - É uma entidade *AMQP* para onde as mensagens são enviadas. Ele recebe uma mensagem e encaminha para as filas.

**Bindings** - Estabelece um relacionamento entre um **Exchange** e um **Queue**.

**Queue** - Local onde ficam armazenadas as mensagens até que sejam retiradas/consumidas.

## Como Funciona?

1. Uma aplicação deseja enviar mensagens para outra aplicação (**Producer/Publisher**)
2. Ele pode fazer isso através de um **broker** ou agende de mensagens ou um servidor de mensageria (**RabbitMQ**)
3. O **broker** recebe a mensagem por meio de um **Exchange** que faz o roteamento da mensagem usando chaves de roteamento e regras chamadas **Bindings**
4. E coloca a mensagem nas **Queues** (filas)
5. A mensagem é então recebida por outro aplicativo que se inscreve na fila fornecida (**Consumer**)

## Fluxo da mensagem no modelo AMQP

![amqpflow-diagram](https://github.com/user-attachments/assets/e36cfbdb-8182-44dd-85a2-4e9779dfceb4)

1. As mensagens são publicadas nos **Exchanges** que funcionam como caixas de correio
2. Em seguida as **Exchanges** distribuem cópias das mensagens para as **Queue** usndo regras chamadas **Bindings**
3. A seguir o **broker** entrega as mensagens aos consumidores inscritos nas filas ou os consumidores buscas as mensagens sob demanda

## "Hello World" (usando .NET/C# Client)

Nesta parte do tutorial escreveremos dois programas em C#; um produtor que envia uma única mensagem e um consumidor que recebe mensagens e as imprime. Iremos encobrir alguns detalhes da API do cliente .NET, concentrando-nos nesta coisa muito simples apenas para começar. É o “Olá Mundo” das mensagens.

### Pré-Requisitos
Este tutorial pressupõe que o RabbitMQ esteja instalado e em execução no host local na porta padrão (5672). Caso você use um host, porta ou credenciais diferentes, as configurações de conexões precisarão de ajustes.

### Setup

Crie dois repositórios, com um projeto em cada um. `Send` vai ser nosso *Publisher* e `Receive` nosso *Consumer*.

```
dotnet new console -o Send --name Send
dotnet new console -o Receive --name Receive
```

Adicione as dependências:

```
cd Send
dotnet add package RabbitMQ.Client
cd ../Receive
dotnet add package RabbitMQ.Client
```

### Publisher

```
// Send/Program.cs

using RabbitMQ.Client;
using System.Text;

// Conexão ao nó RabbitMQ
var factory = new ConnectionFactory { HostName = "localhost" };

// Abre conexão com o nó
using var connection = await factory.CreateConnectionAsync();

// Cria canal onde a fila será definida
using var channel = await connection.CreateChannelAsync();
```

Aqui nos conectamos a um nó RabbitMQ na máquina local - daí o localhost. Se quiséssemos nos conectar a um nó em uma máquina diferente, simplesmente especificaríamos seu nome de host ou endereço IP aqui.

Em seguida, criamos um canal, que é onde reside a maior parte da API para realizar as tarefas.

Para enviar, devemos declarar uma fila para enviarmos; então podemos publicar uma mensagem na fila:

```
// Send/Program.cs

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
```

Declarar uma fila é idempotente - ela só será criada se ainda não existir. 
O conteúdo da mensagem é uma matriz de bytes, então você pode codificar o que quiser lá.

Quando o código acima terminar de ser executado, o canal e a conexão serão descartados.
### Consumer

```
// Receive/Program.cs

using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
```

A configuração é igual à do editor; abrimos uma conexão e um canal e declaramos a fila da qual iremos consumir. Observe que isso corresponde à fila na qual `Send` publica.

```
// Receive/Program.cs

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
```

Observe que declaramos a fila aqui também. Como podemos iniciar o consumidor antes do editor, queremos ter certeza de que a fila existe antes de tentarmos consumir mensagens dela.

Estamos prestes a dizer ao servidor para nos entregar as mensagens da fila. Como isso nos enviará mensagens de forma assíncrona, forneceremos um retorno de chamada. Isso é o que o manipulador de eventos `AsyncEventingBasicConsumer.Received` faz.

```
// Receive/Program.cs

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
```
### Executando Projetos

Abra dois terminais.

Você pode executar os clientes em qualquer ordem, pois ambos declaram a fila. Executaremos primeiro o *Consumer* para que você possa vê-lo aguardando e depois recebendo a mensagem:

```
cd Receive
dotnet run
```

Em seguida, execute o *Publisher*:

```
cd Receive
dotnet run
```

O consumidor imprimirá a mensagem recebida do editor via RabbitMQ. O *Consumer* continuará rodando, aguardando mensagens, então tente reiniciar o publicador diversas vezes.

## Trabalhando Com Filas

Neste tutorial criamos uma fila (queue) de trabalho que será usada para distribuir tarefas demoradas entre vários trabalhadores.

A ideia principal das **Work Queues** (filas de trabalho, aka: *Task Queues*) é evitar executar imediatamente uma tarefa que consome muitos recursos e ter que esperar que ela seja concluída. 

Em vez disso, agendamos a tarefa para ser realizada mais tarde. Encapsulamos uma tarefa como uma mensagem e a enviamos para uma fila. 

Um processo de trabalho em execução em segundo plano irá exibir as tarefas e, eventualmente, executar o trabalho.

Quando você executa muitos trabalhadores, as tarefas serão compartilhadas entre eles.

Este conceito é especialmente útil em aplicações web onde é impossível lidar com uma tarefa complexa durante uma curta janela de solicitação HTTP.
### Setup

Vamos criar dois projetos e adicionar o pacote `RabbitMQ.Client` em ambos:

```
dotnet new console -o NewTask --name NewTask
dotnet new console -o Worker --name Worker
cd NewTask  
dotnet add package RabbitMQ.Client  
cd ../Worker  
dotnet add package RabbitMQ.Client
```

Copie o código do arquivo `Program.cs` de *Send* (tutorial anterior) e para *NewTask* e faça as modificações a seguir.

Atualiza a inicialização da variável `message`:

```
// NewTask

var message = GetMessage(args);
```

Adicione o método `GetMessage` no final do arquivo `Program.cs` de *NewTask*:

```
// NewTask

static string GetMessage(string[] args)
{
    return ((args.Length > 0) ? string.Join(" ", args) : "Hello World!");
}
```

Agora copio o antigo script de *Receive* do tutorial anterior e cole no script de *Worker*, este script também vai receber algumas modificações. Ele irá manipular as mensagens entregues pelo RabbitMQ e executar a tarefa.

Primeiro, modifique o manipulador de eventos lambda para ser assíncrono e, depois de nosso `WriteLine` existente para receber a mensagem, adicione a tarefa falsa para simular o tempo de execução: 

```
var consumer = new AsyncEventingBasicConsumer(channel);
consumer.Received += async (model, ea) =>
{
  Console.WriteLine($" [x] Received {message}");

  int dots = message.Split('.').Length - 1;
  await Task.Delay(dots * 1000);

  Console.WriteLine(" [x] Done");
}
```
### Round-robin Dispatching (Despacho Round-Robin)

Uma das vantagens de usar *Task Queue* é a capacidade de paralelizar facilmente o trabalho. Se estivermos acumulando um amontoado de trabalho, podemos simplesmente adicionar mais trabalhadores e, dessa forma, escalar facilmente. 

Primeiros, vamos executar duas instâncias de *Worker* ao mesmo tempo. Ambos receberão mensagens da fila. 

Você precisa de três terminais abertos. Dois executarão *Worker*. Esses terminais serão nossos dois consumidores: C1 e C2.

```
# shell 1
cd Worker
dotnet run
# => Press [enter] to exit.
```

```
# shell 2
cd Worker
dotnet run
# => Press [enter] to exit.
```

Na terceira publicaremos novas tarefas. Depois de iniciar os consumidores, você pode publicar as mensagens: 

```
# shell 3
cd NewTask
dotnet run "First message."
dotnet run "Second message.."
dotnet run "Third message..."
dotnet run "Fourth message...."
dotnet run "Fifth message....."
```

Resultado dos terminais:

![result-round-dispatching](https://github.com/user-attachments/assets/7352b5af-ae3a-472a-b9bf-80cf5c352280)

