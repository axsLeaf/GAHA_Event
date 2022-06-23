using TwitchLib.Client.Models;

namespace GAHA_Event;

internal static class Helpers
{
    public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
    {
        var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddMilliseconds(unixTimeStamp);
        return dateTime;
    }

    public static bool IsMod(this ChatMessage msg) =>
        msg.IsBroadcaster || msg.IsModerator || msg.Username.ToLower() == "blossomleafy";

    internal class Block
    {
        public int Start;
        public int End;
    }

    public static List<Block> Blocks = new();

    public static void PopulateBlocks()
    {
        Blocks = new List<Block>
        {
            new() { Start = 14, End = 16 },
            new() { Start = 16, End = 18 },
            new() { Start = 18, End = 20 },
            new() { Start = 20, End = 22 },
            new() { Start = 22, End = 24 },
            new() { Start = 00, End = 02 }
        };
    }
}