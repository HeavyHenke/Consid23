﻿using Considition2023_Cs;

namespace Consid23;

public static class Helpers
{
    public static void RandomizeLocationOrder(this MapData mapData)
    {
        var allLocations = new List<StoreLocation>(mapData.locations.Values);
        mapData.locations.Clear();
        var rnd = new Random();
        while (allLocations.Count > 0)
        {
            var randIx = rnd.Next(0, allLocations.Count);
            mapData.locations.Add(allLocations[randIx].LocationName, allLocations[randIx]);
            allLocations.RemoveAt(randIx);
        }
    }
}