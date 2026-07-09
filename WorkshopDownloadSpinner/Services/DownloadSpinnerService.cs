using SDG.Provider;
using Steamworks;
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using WorkshopDownloadSpinner.Helpers;

namespace WorkshopDownloadSpinner.Services
{
    public class DownloadSpinnerService : MonoBehaviour
    {
        private const float DELAY_SECONDS = 1.5f;
        private const int PROGRESS_BAR_LENGTH = 16;
        private const float UPDATE_ESTIMATE_RATE_SECONDS = 2.5f;

        public PublishedFileId_t WorkshopItem { get; private set; } = PublishedFileId_t.Invalid;

        private static readonly char[] SpinnerChars = [ '-', '\\', '|', '/' ];
        private int SpinnerStep { get; set; } = 0;

        private const char ProgressEmptyChar = ' ';
        private const char ProgressFilledChar = '#';

        private float BytesPerSecond { get; set; }
        private ulong LastDownloadedBytes { get; set; }
        private float LastEstimate { get; set; } = Time.realtimeSinceStartup;

        public bool SpinnerRunning => UpdateSpinnerCoroutine != null;
        private Coroutine? UpdateSpinnerCoroutine { get; set; }

        private void SetConsoleInputEnabled(bool enabled)
        {
            Console.CursorVisible = enabled;
            ConsoleHelper.DiscardConsoleInput = !enabled;
        }

        public void StartSpinner(PublishedFileId_t workshopItem)
        {
            if (SpinnerRunning)
                StopSpinner();

            WorkshopItem = workshopItem;

            LastDownloadedBytes = 0;
            LastEstimate = Time.realtimeSinceStartup;

            TempSteamworksWorkshop.getCachedDetails(WorkshopItem, out CachedUGCDetails cachedDetails);
            UpdateSpinnerCoroutine = StartCoroutine(UpdateSpinner($"Downloading \"{cachedDetails.GetTitle()}\" ({WorkshopItem})"));
        }

        private char NextSpinnerChar()
        {
            SpinnerStep++;
            if (SpinnerStep >= SpinnerChars.Length) SpinnerStep = 0;
            return SpinnerChars[SpinnerStep];
        }

        private string DownloadProgressBar(ulong bytesDownloaded, ulong bytesTotal)
        {
            float progress = bytesTotal > 0 ? Mathf.Clamp01((float)bytesDownloaded / (float)bytesTotal) : 0f;
            int progressSteps = Math.Clamp((int)(PROGRESS_BAR_LENGTH * progress), 0, PROGRESS_BAR_LENGTH);

            StringBuilder progressBar = new();
            progressBar.Append('[');
            progressBar.Append(ProgressFilledChar, progressSteps);
            progressBar.Append(ProgressEmptyChar, PROGRESS_BAR_LENGTH - progressSteps);
            progressBar.Append(']');
            progressBar.Append(' ');
            progressBar.Append(progress.ToString("P1"));
            progressBar.Append(' ');
            progressBar.Append('(');
            progressBar.Append(bytesDownloaded);
            progressBar.Append('/');
            progressBar.Append(bytesTotal);
            progressBar.Append(')');

            return progressBar.ToString();
        }

        private void UpdateEstimate(ulong bytesDownloaded, ulong bytesTotal)
        {
            if (bytesDownloaded == bytesTotal)
            {
                BytesPerSecond = 0f;
                return;
            }

            if (BytesPerSecond != 0f && Time.realtimeSinceStartup - LastEstimate < UPDATE_ESTIMATE_RATE_SECONDS) return;
            
            BytesPerSecond = (float)((double)(bytesDownloaded - LastDownloadedBytes) / Time.realtimeSinceStartup - LastEstimate);
            if (BytesPerSecond < 0f) BytesPerSecond = 0f;

            LastDownloadedBytes = bytesDownloaded;
            LastEstimate = Time.realtimeSinceStartup;
        }

        private string DownloadEstimate()
        {
            return $"{BytesPerSecond.ToBytesString()}/s";
        }

        private IEnumerator UpdateSpinner(string downloadMessage)
        {
            yield return new WaitForEndOfFrame();

            if (SteamGameServerUGC.GetItemDownloadInfo(WorkshopItem, out ulong bytesDownloaded, out ulong bytesTotal))
                UpdateEstimate(bytesDownloaded, bytesTotal);
            else
                UpdateEstimate(0, 0);

            // Prevent Appearing for Very Fast Downloads
            yield return new WaitForSeconds(DELAY_SECONDS);

            Console.WriteLine();
            SetConsoleInputEnabled(false);

            WaitForSeconds updateDelay = new(0.1f);
            while (SteamGameServerUGC.GetItemDownloadInfo(WorkshopItem, out bytesDownloaded, out bytesTotal))
            {
                UpdateEstimate(bytesDownloaded, bytesTotal);

                if (Console.CursorLeft != 0 && Console.BufferWidth > 0)
                    Console.CursorLeft = 0;

                Console.Write(downloadMessage);
                Console.Write(' ');
                Console.Write(NextSpinnerChar());
                Console.Write(' ');
                Console.Write(DownloadProgressBar(bytesDownloaded, bytesTotal));
                Console.Write(' ');
                Console.Write(DownloadEstimate());
                Console.Write("       "); // Download Estimate changes quite a bit in size, this is to prevent "0 B/s/sssss"
                yield return updateDelay;
            }

            StopSpinner();
        }

        public void StopSpinner()
        {
            if (!SpinnerRunning) return;

            Console.WriteLine();
            SetConsoleInputEnabled(true);

            StopCoroutine(UpdateSpinnerCoroutine);
            UpdateSpinnerCoroutine = null;
        }
    }
}
