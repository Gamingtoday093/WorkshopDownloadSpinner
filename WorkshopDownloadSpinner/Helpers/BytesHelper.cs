using System;
using System.Collections.Generic;
using System.Text;

namespace WorkshopDownloadSpinner.Helpers
{
    public static class BytesHelper
    {
        private static readonly string[] ByteUnits = ["B", "KB", "MB", "GB", "TB"];

        public static string ToBytesString(this float bytes)
        {
            int unit = 0;
            while (bytes > 1024f && unit < ByteUnits.Length - 1)
            {
                bytes /= 1024f;
                unit++;
            }

            return $"{bytes:0.#} {ByteUnits[unit]}";
        }
    }
}
