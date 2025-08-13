using App;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using fastJSON;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressablesService : IDisposable
{
    private const string DEFAULT_CDN_URL = "https://cdn4.snipe.dev/bingo/";
    private const int DOWNLOADING_DELAY = 60;
    private const int RETRY_REQUEST_DELAY = 1;
    public static string BundlesPath { get; private set; } = ""; // Must be public static
    private string _defaultBundlesPath = ""; // Path that was set on Init and can not be changed
    private int _urlIndex;
    
    public AddressablesContainer AddressablesContainer => _addressablesContainer;
    public bool IsDownloaded { get; private set; }
    public bool IsDownloadRetrying { get; private set; }
    public bool IsDownloading => _downloadOperationHandle.IsValid();
    public Action<float> ProgressUpdateCallback;
    private CancellationTokenSource _delayCancellationTokenSource = new ();
    private AsyncOperationHandle _downloadOperationHandle;
    private readonly AddressablesContainer _addressablesContainer;
    
    public void Dispose()
    {
        EventController.RemoveListener(EventMessage.OnHousingAssetsDownloadFailed,() => OnHousingAssetsDownloadFailed().Forget());
    }
    
    public AddressablesService(AddressablesContainer addressablesContainer)
    {
        _addressablesContainer = addressablesContainer;
        AppConfig.Config.AddOnInitializedCallback(OnConfigInitialized);
        EventController.AddListener(EventMessage.OnHousingAssetsDownloadFailed, () => OnHousingAssetsDownloadFailed().Forget());
    }

    private void OnConfigInitialized(Config config)
    {
        SetupAddressables().Forget();
    }
    
    private async UniTaskVoid OnHousingAssetsDownloadFailed()
    {
        await UniTask.Delay(TimeSpan.FromSeconds(RETRY_REQUEST_DELAY));

        IsDownloadRetrying = true;
        _urlIndex++;
        SetBundlesPath();
        DownloadAddressablesAsync().Forget();
    }

    private async UniTask SetupAddressables()
    {
        SetBundlesPath();
        _defaultBundlesPath = BundlesPath;
        var initHandle = Addressables.InitializeAsync();
        
        await initHandle;
        
        Debug.Log($"[AddressablesService] - Catalog load status: {initHandle.Status}");
        foreach (var locator in Addressables.ResourceLocators)
        {
            Debug.Log($"[AddressablesService] - Found locator: {locator.LocatorId}");
        }
        
        Addressables.Release(initHandle);
        Addressables.WebRequestOverride = OverrideAddressablesWebRequest;
        ResourceManager.ExceptionHandler = CustomExceptionHandler;

        var downloadSize = await GetAddressablesDownloadSize();
        if (downloadSize > 0)
        {
            StartDownloadingDelay().Forget();
        }
        else
        {
           IsDownloaded = true;
        }
    }
    
    private void SetBundlesPath()
    {
        var platform = Application.platform switch
        {
            RuntimePlatform.Android => "Android",
            RuntimePlatform.IPhonePlayer => "iOS",
            RuntimePlatform.WSAPlayerX64 => "UWP",
            _ => "Android"
        };
        
        var urls = GetBundlesUrls();
        if (urls.Count == 0 || _urlIndex > urls.Count - 1)
        {
            _urlIndex = 0;
            Debug.Log($"[AddressablesService] Can't set BundlesPath. Retrying");
        }

        var pathString = urls[_urlIndex];
        BundlesPath = Path.Combine(pathString, platform, Application.version);
        Debug.Log($"[AddressablesService] SetBundlesPath: {BundlesPath}");
    }

    private void OverrideAddressablesWebRequest(UnityWebRequest request)
    {
        if (!string.Equals(_defaultBundlesPath, BundlesPath))
        {
            request.url = request.url.Replace(_defaultBundlesPath, BundlesPath);
            Debug.Log($"[AddressablesService] OverrideAddressablesWebRequest. Override result: {request.url}");
        }
    }
    
    private void CustomExceptionHandler(AsyncOperationHandle operationHandle, Exception exception)
    {
       Debug.LogError($"[AddressablesService] Error: {exception}. Current BundlesPath: {BundlesPath}");
    }

    
    private List<string> GetBundlesUrls()
    {
        var urls =  AppConfig.GetBundlesPath();
        return ParseUrls(urls) ?? new List<string> {DEFAULT_CDN_URL};
    }
        
    private List<string> ParseUrls(string urls)
    {
        if (!string.IsNullOrEmpty(urls))
        {
            var lowerUrl = urls.ToLower();

            if (lowerUrl.StartsWith('['))
            {
                List<string> list;
                try
                {
                    list = JSON.ToObject<List<string>>(urls);
                }
                catch (Exception)
                {
                    list = null;
                }

                if (list != null && list.Count > 0)
                {
                    return list;
                }
            }
        }

        return null;
    }
    
    private async UniTask StartDownloadingDelay()
    {
        try
        {
            Debug.Log($"[AddressablesService] Start delay before download addressables");
            await UniTask.Delay(TimeSpan.FromSeconds(DOWNLOADING_DELAY), cancellationToken: _delayCancellationTokenSource.Token)
                .ContinueWith(() =>
                {
                    // Will be executed only on delay success
                    Debug.Log($"[AddressablesService] Delay before download addressables has finished");
                    DownloadAddressablesAsync().Forget();
                });
        }
        catch (Exception e)
        {
            Debug.Log($"[AddressablesService] Cancel delay before download addressables");
        }
    }

    public async UniTask DownloadAddressablesAsync()
    {
        if (IsDownloading)
        {
            Debug.Log($"[AddressablesService] Download addressables already started");
            return;
        }

        Debug.Log($"[AddressablesService] DownloadAddressablesAsync");

        _downloadOperationHandle = Addressables.DownloadDependenciesAsync(AddressablesContainer.HOUSING_ADDRESSABLES_LABEL);
        float progress = 0;
        
        while (_downloadOperationHandle.Status == AsyncOperationStatus.None)
        {
            var percent = _downloadOperationHandle.GetDownloadStatus().Percent;
            if (percent > progress + 0.1)
            {
                progress = percent;
                Debug.Log($"[AddressablesService] Download progress: {progress * 100}%");
            }
            
            ProgressUpdateCallback?.Invoke(percent);
            await UniTask.Yield();
        }
        
        if (_downloadOperationHandle.Status == AsyncOperationStatus.Succeeded)
        {
            IsDownloaded = true;
            IsDownloadRetrying = false;
            Addressables.Release(_downloadOperationHandle);
            Debug.Log($"[AddressablesService] Download succeeded");
            
            EventController.Invoke(EventMessage.OnHousingAssetsDownloadSucceeded);
        }
        else if (_downloadOperationHandle.Status == AsyncOperationStatus.Failed)
        {
            Services.AnalyticsService.TrackError("Addressables download failed", 
                _downloadOperationHandle.OperationException,
                new Dictionary<string, object> {{"url", BundlesPath}});
            
            Addressables.Release(_downloadOperationHandle);
            Debug.Log($"[AddressablesService] Download failed");
            
            EventController.Invoke(EventMessage.OnHousingAssetsDownloadFailed);
        }
    }

    public async UniTask<long> GetAddressablesDownloadSize()
    {
        var sizeOperationHandle = Addressables.GetDownloadSizeAsync(AddressablesContainer.HOUSING_ADDRESSABLES_LABEL);
        await sizeOperationHandle;
        long downloadSize = 0;

        if (sizeOperationHandle.Status == AsyncOperationStatus.Succeeded)
        {
            downloadSize = sizeOperationHandle.Result;
            Debug.Log($"[AddressablesService] Download size: {downloadSize}");
        }
        
        Addressables.Release(sizeOperationHandle);
        return downloadSize;
    }

    public void CancelDelay()
    {
        _delayCancellationTokenSource.Cancel();
    }
}