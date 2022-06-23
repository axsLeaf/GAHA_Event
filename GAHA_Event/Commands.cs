using Newtonsoft.Json;
using TiltifyApi.Models;

namespace GAHA_Event;

internal static class Commands
{
    private static readonly Random rng = new();
    private static readonly List<Donation> donationList = new();

    public static string RandomDono()
    {
        if (PopulateDonoList())
            return donationList.Count > 0
                ? donationList[rng.Next(donationList.Count)].Name
                : "no donations found during current block.";

        return "current block not found.";
    }

    public static string BlockTotal()
    {
        if (!PopulateDonoList())
            return "Current block not found.";

        var sum = donationList.Sum(donation => donation.Amount);

        return $"During this block, you have helped raise ${sum:0.##}!";
    }

    private static bool PopulateDonoList()
    {
        var time = DateTime.UtcNow;
        var currentBlock = Helpers.Blocks.FirstOrDefault(block => time.Hour >= block.Start && time.Hour < block.End);

        if (currentBlock == null!)
            return false;

        donationList.Clear();

        foreach (var enumerateFile in Directory.EnumerateFiles("donos"))
        {
            var donationJson = JsonConvert.DeserializeObject<Donation>(File.ReadAllText(enumerateFile));
            if (donationJson == null) continue;

            var donoHour = Helpers.UnixTimeStampToDateTime(donationJson.CompletedAt);
            if (donoHour.Day != DateTime.UtcNow.Day)
                continue;

            if (donoHour.Hour < currentBlock.Start || donoHour.Hour >= currentBlock.End)
                continue;

            if (!donationList.Contains(donationJson))
                donationList.Add(donationJson);
        }

        return true;
    }

    public static string Total()
    {
        var total = Directory.EnumerateFiles("donos")
            .Select(enumerateFile => JsonConvert.DeserializeObject<Donation>(File.ReadAllText(enumerateFile)))
            .Where(donationJson => donationJson != null).Sum(donationJson => donationJson?.Amount ?? 0);

        return $"In total, you have helped raise ${total:0.##}!";
    }
}