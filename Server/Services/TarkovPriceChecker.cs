using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public class Item
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int? Avg24hPrice { get; set; }

    public override string ToString()
    {
        return $"ID: {Id} | Name: {Name} | Price: {(Avg24hPrice.HasValue ? Avg24hPrice.Value.ToString() + "₽" : "No Data")}";
    }
}

public class ApiResponse
{
    public ItemData Data { get; set; }
}

public class ItemData
{
    public List<Item> Items { get; set; }
}

public class User
{
    public string Username { get; set; }
    public IPEndPoint IPAddress { get; set; }
    public List<Item> TrackedItems { get; set; } = new List<Item>();

    public User(string username, IPEndPoint ip)
    {
        Username = username;
        IPAddress = ip;
    }

    public void AddTrackedItem(Item item)
    {
        lock (TrackedItems)
        {
            TrackedItems.Add(item);
        }
        Console.WriteLine($"[{Username}] Item added: {item.Name}");
    }

    public void ShowUserInfo()
    {
        Console.WriteLine($">>> Nickname: {Username} | IP: {IPAddress}");
        lock (TrackedItems)
        {
            if (TrackedItems.Count == 0)
            {
                Console.WriteLine("  No items tracked.");
                return;
            }
            Console.WriteLine("  Tracked items:");
            foreach (var item in TrackedItems)
            {
                Console.WriteLine("   - " + item);
            }
        }
    }
}

class Program
{
    static int port = 5582;
    static ConcurrentDictionary<string, User> clients = new ConcurrentDictionary<string, User>();

    public static async Task<Item> GetFirstItem(string searchName)
    {
        string query = $"{{\"query\":\"{{ items(name: \\\"{searchName}\\\") {{ id name avg24hPrice }} }}\"}}";

        using var httpClient = new HttpClient();
        var content = new StringContent(query, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync("https://api.tarkov.dev/graphql", content);
        var json = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<ApiResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return result?.Data?.Items?.FirstOrDefault();
    }

    static async Task Main(string[] args)
    {
        Console.WriteLine("Server started on port " + port);
        UdpClient server = new UdpClient(port);
        CancellationTokenSource cts = new CancellationTokenSource();

        _ = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var result = await server.ReceiveAsync();
                var message = Encoding.UTF8.GetString(result.Buffer);
                var ip = result.RemoteEndPoint;

                Task.Run(async () =>
                {
                    string clientId = ip.ToString();
                    var user = clients.GetOrAdd(clientId, _ => new User("User@" + clientId, ip));

                    if (message.StartsWith("track:"))
                    {
                        string itemName = message.Substring(6).Trim();
                        var item = await GetFirstItem(itemName);
                        if (item != null)
                            user.AddTrackedItem(item);
                        else
                            Console.WriteLine($"Item not found: {itemName}");
                    }
                    else if (message == "info")
                    {
                        user.ShowUserInfo();
                    }
                });
            }
        });

        Console.WriteLine("Press Enter to stop the server...");
        Console.ReadLine();
        cts.Cancel();
    }
}