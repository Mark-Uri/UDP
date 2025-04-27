using System.Net;
using System.Net.Sockets;
using System.Text;



class RestaurantClient
{

    private const int serverPort = 9000;
    private static UdpClient? client;
    private static IPEndPoint? serverEndpoint;
    private static int colorCode = 15;
    private static string nickname = "Гость";


    static async Task Main()
    {

        Console.Title = "КЛИЕНТ РЕСТОРАНА";
        Console.OutputEncoding = Encoding.UTF8;


        Console.Write("Введите ваше имя: ");
        nickname = Console.ReadLine()?.Trim() ?? "Гость";


        ShowColorList();
        Console.Write("Введите номер цвета (1–15): ");
        int.TryParse(Console.ReadLine(), out colorCode);
        colorCode = Math.Clamp(colorCode, 1, 15);



        client = new UdpClient(0);
        serverEndpoint = new IPEndPoint(IPAddress.Loopback, serverPort);
        client.Connect(serverEndpoint);

        

        var initData = Encoding.UTF8.GetBytes($"{nickname}|{colorCode}");
        await client.SendAsync(initData, initData.Length);

        _ = Task.Run(ReceiveMessagesAsync);

        while (true)
        {


            var dishes = new[] { "Борщ", "Пельмени", "Омлет", "Блины", "Котлета", "Салат", "Суп", "Пюре", "Шашлык", "Паста" };


            Console.WriteLine("\nВыберите блюдо:");
            for (int i = 0; i < dishes.Length; i++)
            {
                Console.WriteLine($"{i + 1}. {dishes[i]}");
            }


            Console.Write("Ваш выбор (номер, или '0' для выхода): ");

            int.TryParse(Console.ReadLine(), out int choice);

            if (choice == 0) break;


            choice = Math.Clamp(choice, 1, dishes.Length);
            string selectedDish = dishes[choice - 1];

            var orderData = Encoding.UTF8.GetBytes(selectedDish);

            await client.SendAsync(orderData, orderData.Length);

            Console.WriteLine($"Заказ отправлен: {selectedDish}. Ожидайте...");
        }

        client?.Close();

        Console.WriteLine("Вы вышли из ресторана До свидания!");
    }


    private static async Task ReceiveMessagesAsync()
    {
        while (true)
        {


            var result = await client!.ReceiveAsync();
            var message = Encoding.UTF8.GetString(result.Buffer);



            if (TryParseMessage(message, out string time, out string name, out int color, out string text))
            {
                Console.ForegroundColor = (ConsoleColor)(color % 16);
                Console.WriteLine($"[{time}] {name}: {text}");
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine(message);
            }
        }
    }

    private static bool TryParseMessage(string raw, out string time, out string name, out int color, out string text)
    {

        time = name = text = "";

        color = 15;

        var parts = raw.Split('|', 4);

        if (parts.Length == 4)
        {
            time = parts[0];
            name = parts[1];


            int.TryParse(parts[2], out color);
            text = parts[3];
            return true;
        }
        return false;
    }

    private static void ShowColorList()
    {


        Console.WriteLine("Доступные цвета:");
        for (int i = 1; i <= 15; i++)
        {

            Console.ForegroundColor = (ConsoleColor)i;
            Console.WriteLine($"{i} - {((ConsoleColor)i).ToString()}");


        }
        Console.ResetColor();
    }
}

