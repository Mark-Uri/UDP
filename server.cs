using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;



public class RestaurantServer
{
    private readonly int port;
    private UdpClient? server;
    private ConcurrentQueue<(IPEndPoint client, string dish)> orderQueue = new();
    private ConcurrentDictionary<IPEndPoint, (string name, int color)> clients = new();



    private Dictionary<string, int> dishPrepTime = new()
    {
        {"Борщ", 5}, {"Пельмени", 3}, {"Омлет", 2}, {"Блины", 4}, {"Котлета", 3},
        {"Салат", 1}, {"Суп", 4}, {"Пюре", 2}, {"Шашлык", 5}, {"Паста", 3}
    };

    public RestaurantServer(int port)
    {
        this.port = port;
    }

    public async Task StartAsync()
    {
        server = new UdpClient(port);
        Console.WriteLine($"Сервер ресторана запущен на порту {port}.");

        _ = Task.Run(ProcessOrdersAsync);

        while (true)
        {
            try
            {


                var result = await server.ReceiveAsync();
                var message = Encoding.UTF8.GetString(result.Buffer);


                if (!clients.ContainsKey(result.RemoteEndPoint))
                {
                    var parts = message.Split('|');


                    if (parts.Length == 2 && int.TryParse(parts[1], out int color))
                    {

                        clients[result.RemoteEndPoint] = (parts[0], color);
                        Console.WriteLine($"Подключился {parts[0]} с цветом {color} ({result.RemoteEndPoint})");
                    }
                    continue;
                }

                var (name, _) = clients[result.RemoteEndPoint];
                Console.WriteLine($"Гость {name} заказал: {message}");
                orderQueue.Enqueue((result.RemoteEndPoint, message));




                string notifyMsg = $"[{DateTime.Now:HH:mm:ss}]|{name}|{clients[result.RemoteEndPoint].color}|заказал блюдо: {message}";
                await BroadcastMessageAsync(notifyMsg);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка приема данных: " + ex.Message);
            }
        }
    }




    private async Task ProcessOrdersAsync()
    {
        while (true)
        {

            if (orderQueue.TryDequeue(out var order))
            {
                var (client, dish) = order;
                var (name, color) = clients[client];



                if (!dishPrepTime.TryGetValue(dish, out int time))
                    time = 3;




                Console.WriteLine($"Готовим: {dish} для {name} ({time} сек)...");
                await Task.Delay(time * 1000);




                var response = Encoding.UTF8.GetBytes($"[{DateTime.Now:HH:mm:ss}]|{name}|{color}|Ваша еда готова: {dish}. Приятного аппетита!");
                await server!.SendAsync(response, response.Length, client);
            }
            else
            {
                await Task.Delay(300);
            }
        }
    }




    private async Task BroadcastMessageAsync(string message)
    {

        var data = Encoding.UTF8.GetBytes(message);
        foreach (var client in clients.Keys)
        {
            await server!.SendAsync(data, data.Length, client);
        }
    }

    static async Task Main() => await new RestaurantServer(9000).StartAsync();
}

