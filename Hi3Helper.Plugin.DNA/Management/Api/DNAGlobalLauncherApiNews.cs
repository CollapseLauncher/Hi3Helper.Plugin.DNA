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

    private DNAApiResponseNews? NewsApiResponse { get; set; }
    
    private DNAApiResponseCarousel? CarouselApiResponse { get; set; }

    private DNAApiResponseMediumList? SocialApiResponse { get; set; }

    protected override async Task<int> InitAsync(CancellationToken token)
    {
        using HttpResponseMessage announcements = await ApiResponseHttpClient
            .GetAsync(ApiResponseBaseUrl + "LauncherInfo/CBT2Publish_Pub/AnnouncementInfo_en.txt", HttpCompletionOption.ResponseHeadersRead, token);
        announcements.EnsureSuccessStatusCode();

        using HttpResponseMessage carousel = await ApiResponseHttpClient
            .GetAsync(ApiResponseBaseUrl + "LauncherInfo/CBT2Publish_Pub/TurnsImageInfo_en.txt", HttpCompletionOption.ResponseHeadersRead, token);
        carousel.EnsureSuccessStatusCode();

        NewsApiResponse = DNAApiResponseNews
            .ParseFrom(announcements.Content.ReadAsStream(token));

        CarouselApiResponse = DNAApiResponseCarousel
            .ParseFrom(carousel.Content.ReadAsStream(token));

        using HttpResponseMessage socials = await ApiResponseHttpClient
            .GetAsync(ApiResponseBaseUrl + "/OperationLauncherSocialMedia/OperationLauncherSocialMediaProductionGlobalonline.json", HttpCompletionOption.ResponseHeadersRead, token);
        socials.EnsureSuccessStatusCode();

        string jsonResponse = await socials.Content.ReadAsStringAsync(token);
        SocialApiResponse = JsonSerializer.Deserialize(jsonResponse, DNAApiResponseContext.Default.DNAApiResponseMediumList);

        // Initialize embedded Icon data
        await DNAImageData.Initialize(token);

        return 0;
    }

    public override void GetNewsEntries(out nint handle, out int count, out bool isDisposable, out bool isAllocated)
    {
        try
        {
            if (NewsApiResponse?.ResponseData == null || NewsApiResponse?.ResponseData.Count == 0)
            {
                SharedStatic.InstanceLogger.LogTrace("[DNAGlobalLauncherApiNews::GetSocialMediaEntries] API provides no News entries!");
                InitializeEmpty(out handle, out count, out isDisposable, out isAllocated);
                return;
            }

            List<DNAApiResponseNews.DNAAnnouncement> validEntries = [..NewsApiResponse!.ResponseData
                .Where(x => !string.IsNullOrEmpty(x.Title) &&
                            !string.IsNullOrEmpty(x.Description) &&
                            !string.IsNullOrEmpty(x.Url) &&
                            !string.IsNullOrEmpty(x.Date)
                )];

            int entryCount = validEntries.Count;
            PluginDisposableMemory<LauncherNewsEntry> memory = PluginDisposableMemory<LauncherNewsEntry>.Alloc(entryCount);

            handle = memory.AsSafePointer();
            count = entryCount;
            isDisposable = true;

            SharedStatic.InstanceLogger.LogTrace("[DNAGlobalLauncherApiNews::GetNewsEntries] {EntryCount} entries are allocated at: 0x{Address:x8}", entryCount, handle);

            for (int i = 0; i < entryCount; i++)
            {
                string title = validEntries[i].Title!;
                string description = validEntries[i].Description!;
                string url = validEntries[i].Url!;
                string date = validEntries[i].Date;

                ref LauncherNewsEntry unmanagedEntry = ref memory[i];
                unmanagedEntry.Write(title, description, url, date, LauncherNewsEntryType.Notice);
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
            if (CarouselApiResponse?.ResponseData == null || CarouselApiResponse?.ResponseData.Count == 0)
            {
                SharedStatic.InstanceLogger.LogTrace("[DNAGlobalLauncherApiNews::GetCarouselEntries] API provides no Carousel entries!");
                InitializeEmpty(out handle, out count, out isDisposable, out isAllocated);
                return;
            }

            List<DNAApiResponseCarousel.DNATurn> validEntries = [..CarouselApiResponse!.ResponseData
                .Where(x => !string.IsNullOrEmpty(x.ImageUrl) &&
                            !string.IsNullOrEmpty(x.ClickUrl)
                )];

            int entryCount = validEntries.Count;
            PluginDisposableMemory<LauncherCarouselEntry> memory = PluginDisposableMemory<LauncherCarouselEntry>.Alloc(entryCount);

            handle = memory.AsSafePointer();
            count = entryCount;
            isDisposable = true;

            SharedStatic.InstanceLogger.LogTrace("[DNAGlobalLauncherApiNews::GetCarouselEntries] {EntryCount} entries are allocated at: 0x{Address:x8}", entryCount, handle);

            for (int i = 0; i < entryCount; i++)
            {
                string imageUrl = ApiResponseBaseUrl + "LauncherInfo/CBT2Publish_Pub/" + validEntries[i].ImageUrl!;
                string clickUrl = validEntries[i].ClickUrl!;

                ref LauncherCarouselEntry unmanagedEntry = ref memory[i];
                unmanagedEntry.Write(null, imageUrl, clickUrl);
            }

            isAllocated = true;

        }
        catch (Exception ex)
        {
            SharedStatic.InstanceLogger.LogError(ex, "Failed to get news entries");
            InitializeEmpty(out handle, out count, out isDisposable, out isAllocated);
        }
    }

    public override void GetSocialMediaEntries(out nint handle, out int count, out bool isDisposable, out bool isAllocated)
    {
        InitializeEmpty(out handle, out count, out isDisposable, out isAllocated);

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
                DNAApiResponseMedium.MediumContent content = validEntries[i].Content!
                    .First(x => x.Language != null && x.Language?.Code == "EN");

                DNAApiResponseMedium.MediumInlet inlet = content.Inlets!.First();

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

            NewsApiResponse = null;
            CarouselApiResponse = null;
            base.Dispose();
        }
    }
}
