using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

class User
{
    public string Username { get; set; }
    public IPEndPoint IPAddress { get; set; }
    public List<Item> TracItems { get; set; } = new List<Item>(); 

    public User(string username, IPEndPoint iPAddress)
    {
        Username = username;
        IPAddress = iPAddress;
    }

    public void ShowUserInfo()
    {
        Console.WriteLine($"Nickname: {Username} | IP: {IPAddress}");

        if (TracItems.Count > 0)
        {
            Console.WriteLine("Traced items:");
            foreach (var item in TracItems)
            {
                Console.WriteLine("  - " + item); 
            }
        }
        else
        {
            Console.WriteLine("Error.");
        }
    }

    public void AddTrackedItem(Item item)
    {

        TracItems.Add(item);
        Console.WriteLine($"Item added: {item.name}");
    }
}


public class Item
{
    public string id { get; set; }
    public string name { get; set; }
    public int? avg24hPrice { get; set; }

    public override string ToString()
    {
        return $"ID: {id} | Name: {name} | Price: {(avg24hPrice.HasValue ? avg24hPrice.Value.ToString() + "P" :"No Data")}";
    }
}

public class ApiResponse
{
    public ItemData data { get; set; }
}

public class ItemData
{
    public List<Item> items { get; set; }
}

class Program
{
    static int port = 5582;
    static List<User> clients = new List<User>();

    public static async Task GetAll()
    {
        var data = new Dictionary<string, string>()
        {
            {"query", "{items {id name shortName}}" }
        };
        using (var httpClient = new HttpClient())
        {
            var httpResponse = await httpClient.PostAsJsonAsync("https://api.tarkov.dev/graphql", data);
            var responseContent = await httpResponse.Content.ReadAsStringAsync();
            Console.WriteLine(responseContent);
        }
    }

    public static async Task<string> GetItemInfo(string itemName)
    {
        string query = $"{{\"query\":\"{{ items(name: \\\"{itemName}\\\") {{ id name avg24hPrice }} }}\"}}";

        using var httpClient = new HttpClient();
        var content = new StringContent(query, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync("https://api.tarkov.dev/graphql", content);
        var json = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<ApiResponse>(json);

        if (result?.data?.items == null  result.data.items.Count == 0)
            return "Item not found";

        return string.Join("\n", result.data.items.Select(i => i.ToString()));
    }
    /*public static async Task AddFirstMatchingItemToUser(User user ,string searchName)
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

        if (result?.data?.items == null  result.data.items.Count == 0)
        {
            Console.WriteLine("Ничего не найдено.");
            return;
        }

        var firstItem = result.data.items.FirstOrDefault();
        if (firstItem != null)
        {
            user.AddTrackedItem(firstItem);
        }
    }*/


    static async Task Main(string[] args)
    {
        Console.WriteLine("Server started...");
        UdpClient server = new UdpClient(port);
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, port);
        await GetAll();
        var info = await GetItemInfo("AR-15 Colt charging handle");
        Console.WriteLine(info);
        /*await GetAll();
        Console.WriteLine("\n\n\n");
        string info = await GetItemInfo("");
        Console.WriteLine(info);*/
    }
}