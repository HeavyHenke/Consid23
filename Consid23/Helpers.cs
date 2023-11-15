using Considition2023_Cs;

namespace Consid23;

public static class Helpers
{
    public static void RandomizeLocationOrder(this MapData mapData, int seed = 1337)
    {
        var rnd = new Random(seed);
        var locations = mapData.locations.Values.ToArray();
        rnd.Shuffle(locations);
        mapData.locations.Clear();
        foreach(var l in locations)
            mapData.locations.Add(l.LocationName, l);
    }
}