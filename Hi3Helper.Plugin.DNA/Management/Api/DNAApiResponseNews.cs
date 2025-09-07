using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable InconsistentNaming

namespace Hi3Helper.Plugin.DNA.Management.Api;

public class DNAApiResponseNews
{
    public struct DNAAnnouncement
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public string Date { get; set; }
    }

    public List<DNAAnnouncement>? ResponseData { get; set; }

    public static DNAApiResponseNews ParseFrom(Stream stream, bool isDisposeStream = false)
        => ParseFromAsync(stream, isDisposeStream).Result;

    public static async Task<DNAApiResponseNews> ParseFromAsync(Stream stream, bool isDisposeStream = false,
        CancellationToken token = default)
    {
        try
        {
            return await Task.Factory.StartNew(() => ParseFrom(stream), token);
        }
        finally
        {
            if (isDisposeStream)
            {
                await stream.DisposeAsync();
            }
        }
    }

    public static DNAApiResponseNews ParseFrom(Stream stream)
    {
        List<DNAAnnouncement> innerValue = new();

        using (var reader = new StreamReader(stream, leaveOpen: true))
        {
            var lines = new List<string>();

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                lines.Add(line);

                if (lines.Count == 4)
                {
                    var entry = new DNAAnnouncement
                    {
                        Title = lines[0],
                        Description = lines[1],
                        Url = lines[2],
                        Date = lines[3],
                    };

                    innerValue.Add(entry);
                    lines.Clear();
                }
            }
        }

        return new DNAApiResponseNews
        {
            ResponseData  = innerValue
        };
    }
}
