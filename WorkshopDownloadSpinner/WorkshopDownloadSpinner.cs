using HarmonyLib;
using SDG.Framework.Modules;
using SDG.Unturned;
using Steamworks;
using System;
using System.Reflection;
using UnityEngine;
using WorkshopDownloadSpinner.Services;

namespace WorkshopDownloadSpinner
{
    [HarmonyPatch]
    public class WorkshopDownloadSpinner : IModuleNexus
    {
        public static WorkshopDownloadSpinner Instance { get; private set; } = null!;
        private Harmony Harmony { get; set; } = null!;
        private DownloadSpinnerService? SpinnerService { get; set; }

        public void initialize()
        {
            Instance = this;

            Harmony = new Harmony("WorkshopDownloadSpinner");
            Harmony.PatchAll();

            SpinnerService = new GameObject("WorkshopDownloadSpinner").AddComponent<DownloadSpinnerService>();

            CommandWindow.Log($"WorkshopDownloadSpinner {Assembly.GetExecutingAssembly().GetName().Version} by Gamingtoday093 has been Initialized");
        }

        public void shutdown()
        {
            Harmony.UnpatchAll(Harmony.Id);

            if (SpinnerService != null)
            {
                GameObject.Destroy(SpinnerService.gameObject);
                SpinnerService = null;
            }
        }

        [HarmonyPatch(typeof(DedicatedUGC), "installNextItem")]
        [HarmonyPostfix]
        private static void InstallNextItemPostfix(PublishedFileId_t ___currentDownload)
        {
            if (___currentDownload == PublishedFileId_t.Invalid) return;
            if (Instance.SpinnerService == null) return;

            Instance.SpinnerService.StopSpinner();
            Instance.SpinnerService.StartSpinner(___currentDownload);
        }

        [HarmonyPatch(typeof(DedicatedUGC), "installDownloadedItem")]
        [HarmonyPostfix]
        private static void InstallDownloadedItem(PublishedFileId_t fileId, string path)
        {
            Instance.SpinnerService?.StopSpinner();
        }

        [HarmonyPatch(typeof(DedicatedUGC), "OnFinishedDownloadingItems")]
        [HarmonyPostfix]
        private static void OnFinishedDownloadingItemsPostfix()
        {
            if (Instance.SpinnerService == null) return;

            Instance.SpinnerService.StopSpinner();
            GameObject.Destroy(Instance.SpinnerService.gameObject);
            Instance.SpinnerService = null;
        }
    }
}
