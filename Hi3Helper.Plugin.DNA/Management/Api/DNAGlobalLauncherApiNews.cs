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

    protected override string ApiResponseBaseUrl { get; } = apiResponseBaseUrl + "LauncherInfo/CBT2Publish_Pub/";

    private DNAApiResponseNews? NewsApiResponse { get; set; }
    private DNAApiResponseCarousel? CarouselApiResponse { get; set; }

    protected override async Task<int> InitAsync(CancellationToken token)
    {
        using HttpResponseMessage announcements = await ApiResponseHttpClient
            .GetAsync(ApiResponseBaseUrl + "AnnouncementInfo_en.txt", HttpCompletionOption.ResponseHeadersRead, token);
        announcements.EnsureSuccessStatusCode();

        using HttpResponseMessage carousel = await ApiResponseHttpClient
            .GetAsync(ApiResponseBaseUrl + "TurnsImageInfo_en.txt", HttpCompletionOption.ResponseHeadersRead, token);
        carousel.EnsureSuccessStatusCode();

        NewsApiResponse = DNAApiResponseNews
            .ParseFrom(announcements.Content.ReadAsStream(token));

        CarouselApiResponse = DNAApiResponseCarousel
            .ParseFrom(carousel.Content.ReadAsStream(token));

        // Initialize embedded Icon data
        //await DNAIconData.Initialize(token);

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
                unmanagedEntry.Write(title, description, url, date);
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
                string imageUrl = ApiResponseBaseUrl + validEntries[i].ImageUrl!;
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

        /*try
        {
            if (SocialApiResponse?.ResponseData?.SocialMediaEntries == null ||
                SocialApiResponse.ResponseData.SocialMediaEntries.Count == 0)
            {
                SharedStatic.InstanceLogger.LogTrace("[HBRGlobalLauncherApiNews::GetSocialMediaEntries] API provides no Social Media entries!");
                InitializeEmpty(out handle, out count, out isDisposable, out isAllocated);
                return;
            }

            List<HBRApiResponseSocialResponse> validEntries = [..SocialApiResponse.ResponseData.SocialMediaEntries
                .Where(x => !string.IsNullOrEmpty(x.SocialMediaName) &&
                            !string.IsNullOrEmpty(x.ClickUrl) &&
                            DNAIconData.EmbeddedDataDictionary.ContainsKey(x.SocialMediaName)
                )];

            int entryCount = validEntries.Count;
            PluginDisposableMemory<LauncherSocialMediaEntry> memory = PluginDisposableMemory<LauncherSocialMediaEntry>.Alloc(entryCount);

            handle = memory.AsSafePointer();
            count = entryCount;
            isDisposable = true;

            SharedStatic.InstanceLogger.LogTrace("[HBRGlobalLauncherApiNews::GetSocialMediaEntries] {EntryCount} entries are allocated at: 0x{Address:x8}", entryCount, handle);

            for (int i = 0; i < entryCount; i++)
            {
                string socialMediaName = validEntries[i].SocialMediaName!;
                string clickUrl = validEntries[i].ClickUrl!;
                string? qrImageUrl = validEntries[i].QrImageUrl;

                byte[]? iconData = DNAIconData.GetEmbeddedData(socialMediaName);
                if (iconData == null)
                {
                    continue;
                }

                ref LauncherSocialMediaEntry unmanagedEntry = ref memory[i];
                if (!string.IsNullOrEmpty(qrImageUrl))
                {
                    unmanagedEntry.WriteQrImage(qrImageUrl);
                }

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
        }*/
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
