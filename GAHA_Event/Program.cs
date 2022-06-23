using Newtonsoft.Json;
using TiltifyApi;
using TiltifyApi.Models;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Models;

namespace GAHA_Event;

internal static class Program
{
    private const string channelName = "gaha_event";
    private static TwitchClient twitchClient = null!;

    private static readonly TiltifyClient tiltifyClient =
        new("api_key");

    private static bool isConnected;

    private static void Main()
    {
        Console.WriteLine("F9 to exit");

        if (!Directory.Exists("donos"))
            Directory.CreateDirectory("donos");

        var credentials = new ConnectionCredentials("felicityone", "twitch_token");
        var clientOptions = new ClientOptions
        {
            MessagesAllowedInPeriod = 750,
            ThrottlingPeriod = TimeSpan.FromSeconds(30)
        };

        twitchClient = new TwitchClient(new WebSocketClient(clientOptions));
        twitchClient.Initialize(credentials, channelName);

        twitchClient.OnConnected += Client_OnConnected;
        twitchClient.OnDisconnected += Client_OnDisconnected;
        twitchClient.OnMessageReceived += Client_OnMessageReceived;
        twitchClient.AddChatCommandIdentifier('?');
        twitchClient.OnChatCommandReceived += Client_OnChatCommandRecieved;

        twitchClient.Connect();

        Helpers.PopulateBlocks();

        var blockList = new Dictionary<DayOfWeek, List<Donation>>
        {
            {DayOfWeek.Monday, new List<Donation>()},
            {DayOfWeek.Tuesday, new List<Donation>()},
            {DayOfWeek.Wednesday, new List<Donation>()},
            {DayOfWeek.Thursday, new List<Donation>()},
            {DayOfWeek.Friday, new List<Donation>()},
            {DayOfWeek.Saturday, new List<Donation>()}
        };

        foreach (var dono in JsonConvert.DeserializeObject<List<Donation>>(File.ReadAllText("totalDonos.json"))!)
        {
            var time = Helpers.UnixTimeStampToDateTime(dono.CompletedAt);

            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (time.DayOfWeek)
            {
                case DayOfWeek.Tuesday:
                    if (time.Hour < 4)
                    {
                        blockList[DayOfWeek.Monday].Add(dono);
                        continue;
                    }
                    break;
                case DayOfWeek.Wednesday:
                    if (time.Hour < 4)
                    {
                        blockList[DayOfWeek.Tuesday].Add(dono);
                        continue;
                    }
                    break;
                case DayOfWeek.Thursday:
                    if (time.Hour < 4)
                    {
                        blockList[DayOfWeek.Wednesday].Add(dono);
                        continue;
                    }
                    break;
                case DayOfWeek.Friday:
                    if (time.Hour < 4)
                    {
                        blockList[DayOfWeek.Thursday].Add(dono);
                        continue;
                    }
                    break;
                case DayOfWeek.Saturday:
                    if (time.Hour < 4)
                    {
                        blockList[DayOfWeek.Friday].Add(dono);
                        continue;
                    }
                    break;
            }

            blockList[time.DayOfWeek].Add(dono);
        }

        

        foreach (var pair in blockList)
        {
            decimal mBlock1 = 0;
            decimal mBlock2 = 0;
            decimal mBlock3 = 0;
            decimal mBlock4 = 0;
            decimal mBlock5 = 0;
            decimal mBlock6 = 0;

            foreach (var dono in pair.Value)
            {
                var time = Helpers.UnixTimeStampToDateTime(dono.CompletedAt);

                switch (time.Hour)
                {
                    case >= 14 and < 16:
                        mBlock1 += dono.Amount;
                        break;
                    case >= 16 and < 18:
                        mBlock2 += dono.Amount;
                        break;
                    case >= 18 and < 20:
                        mBlock3 += dono.Amount;
                        break;
                    case >= 20 and < 22:
                        mBlock4 += dono.Amount;
                        break;
                    case >= 22 and < 24:
                        mBlock5 += dono.Amount;
                        break;
                    case < 4:
                        mBlock6 += dono.Amount;
                        break;
                }
            }

            Console.WriteLine($"{pair.Key}:");
            Console.WriteLine($"Block 1: ${mBlock1:n}");
            Console.WriteLine($"Block 2: ${mBlock2:n}");
            Console.WriteLine($"Block 3: ${mBlock3:n}");
            Console.WriteLine($"Block 4: ${mBlock4:n}");
            Console.WriteLine($"Block 5: ${mBlock5:n}");
            Console.WriteLine($"Block 6: ${mBlock6:n}");
        }
        
        while (true)
        {
            var ret = Console.ReadKey();
            if (ret.Key == ConsoleKey.F9)
                return;

            Console.WriteLine("wrong key");
        }
    }

    private static void Client_OnChatCommandRecieved(object? sender, OnChatCommandReceivedArgs e)
    {
        switch (e.Command.CommandText)
        {
            case "rdono":
                if (e.Command.ChatMessage.IsMod())
                    twitchClient.SendReply(channelName, e.Command.ChatMessage.Id,
                        $"Random donator from this block: {Commands.RandomDono()}");
                break;
            case "blocktotal":
                if (e.Command.ChatMessage.IsMod())
                    twitchClient.SendReply(channelName, e.Command.ChatMessage.Id, Commands.BlockTotal());
                break;
            case "total":
                if (e.Command.ChatMessage.IsMod())
                    twitchClient.SendReply(channelName, e.Command.ChatMessage.Id, Commands.Total());
                break;
        }
    }

    private static void Client_OnDisconnected(object? sender, OnDisconnectedEventArgs e)
    {
        isConnected = false;
    }

    private static void Client_OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        Console.WriteLine($"{e.ChatMessage.Username}: {e.ChatMessage.Message}");
    }

    private static void Client_OnConnected(object? sender, OnConnectedArgs e)
    {
        Console.WriteLine("Twitch client connected.");
        isConnected = true;

        new Thread(DonoTimer).Start();
    }

    private static void DonoTimer()
    {
        Thread.Sleep(1000);

        do
        {
            Console.WriteLine("Checking Donations...");

            var donos = tiltifyClient.GetCampaignDonations(166768).Result;
            foreach (var dono in donos.Data)
            {
                var path = $"donos/{dono.Id}.json";
                if (File.Exists(path)) continue;

                var donation = $"{dono.Name} donated ${dono.Amount:#.00}";
                if (!string.IsNullOrEmpty(dono.Comment))
                    donation += $": {dono.Comment}";
                else
                    donation += " with no comment.";
                Console.WriteLine(donation);

                twitchClient.SendMessage(channelName, $"/announce {donation}");
                File.WriteAllText(path, JsonConvert.SerializeObject(dono));

                var totalJson = Directory.EnumerateFiles("donos")
                    .Select(enumerateFile => JsonConvert.DeserializeObject<Donation>(File.ReadAllText(enumerateFile)))
                    .Where(json => json != null).ToList();

                File.WriteAllText("totalDonos.json", JsonConvert.SerializeObject(totalJson));
            }

            Thread.Sleep(30 * 1000);
        } while (isConnected);
    }
}