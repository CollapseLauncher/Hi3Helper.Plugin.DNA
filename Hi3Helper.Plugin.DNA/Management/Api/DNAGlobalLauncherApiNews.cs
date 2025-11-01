using Hi3Helper.Plugin.Core;
using Hi3Helper.Plugin.Core.Management.Api;
using Hi3Helper.Plugin.DNA.Utility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using System.Threading.Tasks;
using Hi3Helper.Plugin.Core.Management;
using Hi3Helper.Plugin.DNA.Management.Api.Response;
using System.ComponentModel;


// ReSharper disable InconsistentNaming

#if USELIGHTWEIGHTJSONPARSER
using System.IO;
#else
using System.Text.Json;
#endif

namespace Hi3Helper.Plugin.DNA.Management.Api;

[GeneratedComClass]
internal partial class DNAGlobalLauncherApiNews(string apiResponseBaseUrl) : LauncherApiNewsBase
{
    [field: AllowNull, MaybeNull]
    protected override HttpClient ApiResponseHttpClient
    {
        get => field ??= DNAUtility.CreateApiHttpClient();
        set;
    }

    [field: AllowNull, MaybeNull]
    protected HttpClient ApiDownloadHttpClient
    {
        get => field ??= DNAUtility.CreateApiHttpClient();
        set;
    }

    protected override string ApiResponseBaseUrl { get; } = apiResponseBaseUrl;

    private DNAApiResponseNotices? NoticesApiResponse { get; set; }

    private DNAApiResponseCarousel? CarouselApiResponse { get; set; }

    private DNAApiResponseSocials? SocialApiResponse { get; set; }

    protected override async Task<int> InitAsync(CancellationToken token)
    {
        using HttpResponseMessage notices = await ApiResponseHttpClient
            .GetAsync(ApiResponseBaseUrl + "/OperationLauncherNotice/OperationLauncherNoticeProductionGlobalonline.json", HttpCompletionOption.ResponseHeadersRead, token);
        notices.EnsureSuccessStatusCode();

        string noticesJsonResponse = await notices.Content.ReadAsStringAsync(token);
        NoticesApiResponse = JsonSerializer.Deserialize(noticesJsonResponse, DNAApiResponseContext.Default.DNAApiResponseNotices);

        using HttpResponseMessage carousel = await ApiResponseHttpClient
            .GetAsync(ApiResponseBaseUrl + "/OperationLauncherHeadImage/OperationLauncherHeadImageProductionGlobalonline.json", HttpCompletionOption.ResponseHeadersRead, token);
        carousel.EnsureSuccessStatusCode();

        string carouselJsonResponse = await carousel.Content.ReadAsStringAsync(token);
        CarouselApiResponse = JsonSerializer.Deserialize(carouselJsonResponse, DNAApiResponseContext.Default.DNAApiResponseCarousel);

        using HttpResponseMessage socials = await ApiResponseHttpClient
            .GetAsync(ApiResponseBaseUrl + "/OperationLauncherSocialMedia/OperationLauncherSocialMediaProductionGlobalonline.json", HttpCompletionOption.ResponseHeadersRead, token);
        socials.EnsureSuccessStatusCode();

        string socialsJsonResponse = await socials.Content.ReadAsStringAsync(token);
        SocialApiResponse = JsonSerializer.Deserialize(socialsJsonResponse, DNAApiResponseContext.Default.DNAApiResponseSocials);

        // Initialize embedded Icon data
        await DNAImageData.Initialize(token);

        return 0;
    }

    public override void GetNewsEntries(out nint handle, out int count, out bool isDisposable, out bool isAllocated)
    {
        try
        {
            if (NoticesApiResponse == null || NoticesApiResponse.Count == 0)
            {
                SharedStatic.InstanceLogger.LogTrace("[DNAGlobalLauncherApiNews::GetNewsEntries] API provides no News entries!");
                InitializeEmpty(out handle, out count, out isDisposable, out isAllocated);
                return;
            }

            List<DNAApiResponseNoticesEntry> validEntries = [..NoticesApiResponse.Values
                .Where(x => x.Event != null &&
                            !string.IsNullOrEmpty(x.Event.Name) &&
                            !string.IsNullOrEmpty(x.Event.Type) &&
                            !string.IsNullOrEmpty(x.Event.Date) &&
                            x.Event.Content != null && x.Event.Content.Count > 0
                )];

            int entryCount = validEntries.Count;
            PluginDisposableMemory<LauncherNewsEntry> memory = PluginDisposableMemory<LauncherNewsEntry>.Alloc(entryCount);

            handle = memory.AsSafePointer();
            count = entryCount;
            isDisposable = true;

            SharedStatic.InstanceLogger.LogTrace("[DNAGlobalLauncherApiNews::GetNewsEntries] {EntryCount} entries are allocated at: 0x{Address:x8}", entryCount, handle);

            for (int i = 0; i < entryCount; i++)
            {
                var vEvent = validEntries[i].Event;
                var content = vEvent.Content
                    .Where(x => x.Language == "EN").First();

                string title = content.Title!;
                string description = content.Description!;
                string url = content.ClickUrl!;

                long timestamp = long.Parse(vEvent.Date!);
                DateTime time = DateTimeOffset.FromUnixTimeSeconds(timestamp)
                    .ToLocalTime().DateTime;
                string formattedTime = time.ToString("dd/MM/yyyy");

                var type = vEvent.Type switch
                {
                    "1" => LauncherNewsEntryType.Notice,
                    "0" => LauncherNewsEntryType.Info,
                    "2" => LauncherNewsEntryType.Event,
                    _ => LauncherNewsEntryType.Notice,
                };

                ref LauncherNewsEntry unmanagedEntry = ref memory[i];
                unmanagedEntry.Write(
                    title,
                    description,
                    url,
                    formattedTime,
                    type);
            }

            isAllocated = true;

        }
        catch (Exception ex)
        {
            SharedStatic.InstanceLogger.LogError(ex, "Failed to get news entries");
            InitializeEmpty(out handle, out count, out isDisposable, out isAllocated);
        }
    }

    public override void GetCarouselEntries(out nint handle, out int count, out bool isDisposable, out bool isAllocated)
    {
        try
        {
            if (CarouselApiResponse == null || CarouselApiResponse.Count == 0)
            {
                SharedStatic.InstanceLogger.LogTrace("[DNAGlobalLauncherApiNews::GetCarouselEntries] API provides no Carousel entries!");
                InitializeEmpty(out handle, out count, out isDisposable, out isAllocated);
                return;
            }

            List<DNAApiResponseCarouselEntry> validEntries = [..CarouselApiResponse.Values
                .Where(x => !string.IsNullOrEmpty(x.Name) &&
                            x.Content != null && x.Content.Count > 0
                )];

            int entryCount = validEntries.Count;
            PluginDisposableMemory<LauncherCarouselEntry> memory = PluginDisposableMemory<LauncherCarouselEntry>.Alloc(entryCount);

            handle = memory.AsSafePointer();
            count = entryCount;
            isDisposable = true;

            SharedStatic.InstanceLogger.LogTrace("[DNAGlobalLauncherApiNews::GetCarouselEntries] {EntryCount} entries are allocated at: 0x{Address:x8}", entryCount, handle);

            for (int i = 0; i < entryCount; i++)
            {
                var contentList = validEntries[i].Content;
                if (contentList == null)
                    continue;

                var content = contentList
                    .Where(x => x.Language == "EN")
                    .FirstOrDefault();
                if (content == null)
                    continue;

                var image = content.HeadImages.FirstOrDefault();
                if (image == null)
                    continue;

                string description = image.Description!;
                string imageUrl = image.ImageUrl!;
                string url = image.ClickUrl!;

                ref LauncherCarouselEntry unmanagedEntry = ref memory[i];
                unmanagedEntry.Write(
                    description,
                    imageUrl,
                    url);
            }

            isAllocated = true;

        }
        catch (Exception ex)
        {
            SharedStatic.InstanceLogger.LogError(ex, "Failed to get carousel entries");
            InitializeEmpty(out handle, out count, out isDisposable, out isAllocated);
        }
    }

    public override void GetSocialMediaEntries(out nint handle, out int count, out bool isDisposable, out bool isAllocated)
    {
        try
        {
            if (SocialApiResponse?.MediumList == null ||
                SocialApiResponse.MediumList.Count == 0)
            {
                SharedStatic.InstanceLogger.LogTrace("[DNAGlobalLauncherApiNews::GetSocialMediaEntries] API provides no Social Media entries!");
                InitializeEmpty(out handle, out count, out isDisposable, out isAllocated);
                return;
            }

            List<DNAApiResponseMedium> validEntries = [..SocialApiResponse.MediumList
                .Where(x => x.Content != null &&
                    x.Content.Any(y => y.Language?.Code == "EN" && y.Inlets != null && y.Inlets.Count > 0) &&
                    x.Medium != null &&
                    x.Medium?.Code != null &&
                    DNAImageData.EmbeddedDataDictionary.ContainsKey(x.Medium!.Code)
                )];

            int entryCount = validEntries.Count;
            PluginDisposableMemory<LauncherSocialMediaEntry> memory = PluginDisposableMemory<LauncherSocialMediaEntry>.Alloc(entryCount);

            handle = memory.AsSafePointer();
            count = entryCount;
            isDisposable = true;

            SharedStatic.InstanceLogger.LogTrace("[DNAGlobalLauncherApiNews::GetSocialMediaEntries] {EntryCount} entries are allocated at: 0x{Address:x8}", entryCount, handle);

            for (int i = 0; i < entryCount; i++)
            {
                var content = validEntries[i].Content!
                    .First(x => x.Language != null && x.Language?.Code == "EN");

                var inlet = content.Inlets!.First();

                string socialMediaName = validEntries[i].Medium?.Code!;
                string clickUrl = inlet.Url!;

                byte[]? iconData = DNAImageData.GetEmbeddedData(socialMediaName);
                if (iconData == null)
                {
                    continue;
                }

                ref LauncherSocialMediaEntry unmanagedEntry = ref memory[i];

                unmanagedEntry.WriteIcon(iconData);
                unmanagedEntry.WriteDescription(socialMediaName);
                unmanagedEntry.WriteClickUrl(clickUrl);
            }

            isAllocated = true;
        }
        catch (Exception ex)
        {
            SharedStatic.InstanceLogger.LogError(ex, "Failed to get social media entries");
            InitializeEmpty(out handle, out count, out isDisposable, out isAllocated);
        }
    }

    private static void InitializeEmpty(out nint handle, out int count, out bool isDisposable, out bool isAllocated)
    {
        handle = nint.Zero;
        count = 0;
        isDisposable = false;
        isAllocated = false;
    }

    public override void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        using (ThisInstanceLock.EnterScope())
        {
            ApiDownloadHttpClient.Dispose();
            ApiDownloadHttpClient = null!;

            NoticesApiResponse = null;
            base.Dispose();
        }
    }
}
