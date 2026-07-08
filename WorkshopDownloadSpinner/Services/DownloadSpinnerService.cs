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

        public PublishedFileId_t WorkshopItem { get; private set; } = PublishedFileId_t.Invalid;

        private static readonly char[] SpinnerChars = [ '-', '\\', '|', '/' ];
        private int SpinnerStep { get; set; } = 0;

        private const char ProgressEmptyChar = ' ';
        private const char ProgressFilledChar = '#';

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

        private IEnumerator UpdateSpinner(string downloadMessage)
        {
            yield return new WaitForEndOfFrame();
            // Prevent Appearing for Very Fast Downloads
            yield return new WaitForSeconds(DELAY_SECONDS);

            Console.WriteLine();
            SetConsoleInputEnabled(false);

            WaitForSeconds updateDelay = new(0.1f);
            while (SteamGameServerUGC.GetItemDownloadInfo(WorkshopItem, out ulong bytesDownloaded, out ulong bytesTotal))
            {
                if (Console.CursorLeft != 0 && Console.BufferWidth > 0)
                    Console.CursorLeft = 0;

                Console.Write(downloadMessage);
                Console.Write(' ');
                Console.Write(NextSpinnerChar());
                Console.Write(' ');
                Console.Write(DownloadProgressBar(bytesDownloaded, bytesTotal));
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
