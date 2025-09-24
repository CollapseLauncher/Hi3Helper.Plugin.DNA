using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable InconsistentNaming

namespace Hi3Helper.Plugin.DNA.Management.Api;

public class DNAApiResponseCarousel
{
    public struct DNATurn
    {
        public string? ImageUrl { get; set; }
        public string? ClickUrl { get; set; }
    }

    public List<DNATurn>? ResponseData { get; set; }

    public static DNAApiResponseCarousel ParseFrom(Stream stream, bool isDisposeStream = false)
        => ParseFromAsync(stream, isDisposeStream).Result;

    public static async Task<DNAApiResponseCarousel> ParseFromAsync(Stream stream, bool isDisposeStream = false,
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

    public static DNAApiResponseCarousel ParseFrom(Stream stream)
    {
        List<DNATurn> innerValue = [];

        using (var reader = new StreamReader(stream, leaveOpen: true))
        {
            if (reader.EndOfStream)
                return new DNAApiResponseCarousel
                {
                    ResponseData = innerValue,
                };

            int count = int.Parse(reader.ReadLine() ?? "0");

            for (int i = 0; i < count; i++)
            {
                if (reader.EndOfStream)
                    throw new InvalidDataException("Unexpected end of stream");

                string? image = reader.ReadLine();
                if (reader.EndOfStream)
                    throw new InvalidDataException("Unexpected end of stream");

                string? link = reader.ReadLine();

                innerValue.Add(new DNATurn { ImageUrl = image, ClickUrl = link });
            }
        }

        return new DNAApiResponseCarousel
        {
            ResponseData  = innerValue
        };
    }
}
