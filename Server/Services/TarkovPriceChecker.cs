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
using Microsoft.EntityFrameworkCore;
using Server.Models;

public class Item
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int? Avg24hPrice { get; set; }

    public override string ToString()
    {
        return $"ID: {Id} | Name: {Name} | Price: {(Avg24hPrice.HasValue ? Avg24hPrice.Value + "₽" : "No Data")}";
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
    static ConcurrentDictionary<string, User> clients = new();

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
        using var server = new UdpClient(port);
        CancellationTokenSource cts = new();

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseMySql(
            "server=localhost;port=3306;database=tarkovdb;user=root;password=",
            ServerVersion.AutoDetect("server=localhost;port=3306;database=tarkovdb;user=root;password=")
        );
        using var dbContext = new AppDbContext(optionsBuilder.Options);

        Console.WriteLine("Press Ctrl+C to stop the server...");

        while (!cts.Token.IsCancellationRequested)
        {
            var result = await server.ReceiveAsync();
            var message = Encoding.UTF8.GetString(result.Buffer);
            var ip = result.RemoteEndPoint;

            string clientId = ip.ToString();
            var user = clients.GetOrAdd(clientId, _ => new User("User@" + clientId, ip));

            if (message.StartsWith("track:"))
            {
                string itemName = message.Substring(6).Trim();
                var item = await GetFirstItem(itemName);
                if (item != null)
                {
                    user.AddTrackedItem(item);

                    if (item.Avg24hPrice.HasValue)
                    {
                        dbContext.PriceHistory.Add(new PriceHistory
                        {
                            ItemId = item.Id,
                            ItemName = item.Name,
                            Price = item.Avg24hPrice.Value,
                            Timestamp = DateTime.Now
                        });
                        await dbContext.SaveChangesAsync();
                        Console.WriteLine($"[DB] Saved price for {item.Name}: {item.Avg24hPrice.Value}₽");
                    }
                }
                else
                {
                    Console.WriteLine($"Item not found: {itemName}");
                }
            }
            else if (message == "info")
            {
                user.ShowUserInfo();
            }
        }
    }
}
