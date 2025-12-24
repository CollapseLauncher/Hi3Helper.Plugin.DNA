using System;
using System.Collections.Generic;
using System.Linq;

namespace Hi3Helper.Plugin.DNA.Management.Api;

public class DNAApiResponseDetails
{
    public List<string> BaseUrls { get; init; } = null!;

    public string BaseUrl {
        get
        {
            int index = (int)Math.Floor((DateTime.Now.Day - 1) / 8.0);
            if (index >= BaseUrls.Count) return BaseUrls.First();

            return BaseUrls[index];
        }
    }

    public string Region { get; init; } = null!;
    public string RegionLong { get; init; } = null!;
    public string Tag { get; init; } = null!;
}
