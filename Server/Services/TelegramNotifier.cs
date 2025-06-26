using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Server.Services
{
    public class TelegramNotifier
    {
        private readonly string _botToken = "7597015096:AAEbN-GxFC6IjDCXpoMpiMHKfF71dSReLeA";
        private readonly string _chatId = "YOUR_CHAT_ID";

        public async Task SendAlertAsync(string message)
        {
            var url = $"https://api.telegram.org/bot{7597015096:AAEbN-GxFC6IjDCXpoMpiMHKfF71dSReLeA}/sendMessage";
            var parameters = new Dictionary<string, string>
            {
                ["chat_id"] = _chatId,
                ["text"] = message
            };

            using var client = new HttpClient();
            await client.PostAsync(url, new FormUrlEncodedContent(parameters));
        }
    }
}