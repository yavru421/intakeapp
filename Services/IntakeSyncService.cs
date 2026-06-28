using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using IntakeApp.Models;

namespace IntakeApp.Services
{
    public class IntakeSyncService : IAsyncDisposable
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly HttpClient _httpClient;
        private DotNetObjectReference<IntakeSyncService>? _dotNetHelper;
        private const string StorageKey = "intake_requests";

        public bool IsOnline { get; private set; } = true;
        public List<IntakeRequest> CacheQueue { get; private set; } = new();

        public event Action? OnSyncStatusChanged;

        public IntakeSyncService(IJSRuntime jsRuntime, HttpClient httpClient)
        {
            _jsRuntime = jsRuntime;
            _httpClient = httpClient;
        }

        public async Task InitializeAsync()
        {
            _dotNetHelper = DotNetObjectReference.Create(this);
            
            // Load from cache
            try
            {
                var stored = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
                if (!string.IsNullOrEmpty(stored))
                {
                    try
                    {
                        CacheQueue = JsonSerializer.Deserialize<List<IntakeRequest>>(stored) ?? new();
                    }
                    catch
                    {
                        CacheQueue = new();
                    }
                }
            }
            catch
            {
                // localStorage.getItem might fail if not available in prerendering
            }

            // Register JS interop connection status callback
            try 
            {
                IsOnline = await _jsRuntime.InvokeAsync<bool>("iosPwa.registerOnlineCallback", _dotNetHelper);
            }
            catch 
            {
                IsOnline = true;
            }
            
            if (IsOnline)
            {
                await SyncQueueAsync();
            }
        }

        [JSInvokable]
        public async Task OnConnectionChanged(bool isOnline)
        {
            IsOnline = isOnline;
            NotifyStateChanged();

            if (isOnline)
            {
                // Active-Window Syncing (No Background Sync)
                // When coming back online, run sync automatically
                await SyncQueueAsync();
            }
        }

        public async Task AddEntryAsync(IntakeRequest entry)
        {
            CacheQueue.Insert(0, entry);
            await SaveToStorageAsync();
            NotifyStateChanged();

            if (IsOnline)
            {
                await SyncQueueAsync();
            }
        }

        public async Task SyncQueueAsync()
        {
            if (!IsOnline) return;

            bool changed = false;
            foreach (var entry in CacheQueue.Where(e => !e.IsSynced && !e.SyncSkip))
            {
                // Process only non-synced and non-skipped entries
                bool success = await UploadToServerAsync(entry);
                if (success)
                {
                    entry.IsSynced = true;
                    entry.SyncFailed = false;
                    entry.ErrorMessage = null;
                    changed = true;
                }
                else
                {
                    entry.SyncFailed = true;
                    entry.ErrorMessage = "Server upload failed (Check connection)";
                    changed = true;
                }
            }

            if (changed)
            {
                await SaveToStorageAsync();
                NotifyStateChanged();
            }
        }

        public async Task RetryEntryAsync(string id)
        {
            var entry = CacheQueue.FirstOrDefault(e => e.Id == id);
            if (entry != null)
            {
                entry.SyncFailed = false;
                entry.SyncSkip = false;
                entry.ErrorMessage = null;
                await SaveToStorageAsync();
                NotifyStateChanged();

                if (IsOnline)
                {
                    // Force upload of this specific entry
                    bool success = await UploadToServerAsync(entry);
                    if (success)
                    {
                        entry.IsSynced = true;
                        entry.SyncFailed = false;
                    }
                    else
                    {
                        entry.SyncFailed = true;
                        entry.ErrorMessage = "Upload failed during retry.";
                    }
                    await SaveToStorageAsync();
                    NotifyStateChanged();
                }
            }
        }

        public async Task SkipEntryAsync(string id)
        {
            var entry = CacheQueue.FirstOrDefault(e => e.Id == id);
            if (entry != null)
            {
                entry.SyncSkip = true;
                entry.ErrorMessage = "Skipped by user";
                await SaveToStorageAsync();
                NotifyStateChanged();
            }
        }

        private async Task<bool> UploadToServerAsync(IntakeRequest entry)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/submissions", entry);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UploadToServerAsync failed: {ex.Message}");
                return false;
            }
        }

        private async Task SaveToStorageAsync()
        {
            var serialized = JsonSerializer.Serialize(CacheQueue);
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, serialized);
            }
            catch
            {
                // Ignore errors
            }
        }

        private void NotifyStateChanged()
        {
            OnSyncStatusChanged?.Invoke();
        }

        public async ValueTask DisposeAsync()
        {
            _dotNetHelper?.Dispose();
            await Task.CompletedTask;
        }
    }
}
