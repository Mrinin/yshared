#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

using System;
using System.Collections;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine;

namespace YShared.Console
{
    

    public static class ScreenshotClipboard
    {
        static int Width, Height;
        static float Size;

        private const uint CF_DIB = 8;
        private const uint GMEM_MOVEABLE = 0x0002;

        [DllImport("user32.dll")]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll")]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll")]
        private static extern bool EmptyClipboard();

        [DllImport("user32.dll")]
        private static extern IntPtr SetClipboardData(
            uint uFormat,
            IntPtr hMem);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalAlloc(
            uint uFlags,
            UIntPtr dwBytes);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        private static extern bool GlobalUnlock(IntPtr hMem);

        public static IEnumerator CopyScreenshot()
        {
            DevConsoleUI.Instance?.transform.GetChild(0)?.gameObject.SetActive(false);

            yield return new WaitForEndOfFrame();

            Width = Screen.width;
            Height = Screen.height;

            
            Texture2D texture = new Texture2D(
                Width,
                Height,
                TextureFormat.RGBA32,
                false
            );

            texture.ReadPixels(
                new Rect(0, 0, Width, Height),
                0,
                0
            );

            Size = (texture.GetRawTextureData().Length / 1000f) / 1000f;

            CopyTextureToClipboard(texture);

            UnityEngine.Object.Destroy(texture);
            string decimalmb = Size.ToString("F2", CultureInfo.InvariantCulture);
            DevConsole.Log($"Screenshot taken. {Width}x{Height}, {decimalmb} MB. Paste it anywhere using Ctrl + V.");

            
            DevConsoleUI.Instance?.transform.GetChild(0)?.gameObject.SetActive(true);
        }

        private static void CopyTextureToClipboard(Texture2D texture)
        {
            byte[] pixels = texture.GetRawTextureData();

            int width = texture.width;
            int height = texture.height;

            // Windows CF_DIB uses a BITMAPINFOHEADER followed by BGR pixels.
            int rowSize = width * 3;
            rowSize = (rowSize + 3) & ~3;

            int pixelDataSize = rowSize * height;
            int headerSize = 40;
            int totalSize = headerSize + pixelDataSize;

            IntPtr hGlobal = GlobalAlloc(
                GMEM_MOVEABLE,
                (UIntPtr)totalSize);

            if (hGlobal == IntPtr.Zero)
                return;

            IntPtr ptr = GlobalLock(hGlobal);

            if (ptr == IntPtr.Zero)
                return;

            try
            {
                // BITMAPINFOHEADER
                Marshal.WriteInt32(ptr, 0, 40);       // biSize
                Marshal.WriteInt32(ptr, 4, width);    // biWidth
                Marshal.WriteInt32(ptr, 8, height);   // biHeight
                Marshal.WriteInt16(ptr, 12, 1);       // biPlanes
                Marshal.WriteInt16(ptr, 14, 24);      // biBitCount
                Marshal.WriteInt32(ptr, 16, 0);       // biCompression
                Marshal.WriteInt32(ptr, 20, pixelDataSize);
                Marshal.WriteInt32(ptr, 24, 0);       // biXPelsPerMeter
                Marshal.WriteInt32(ptr, 28, 0);       // biYPelsPerMeter
                Marshal.WriteInt32(ptr, 32, 0);       // biClrUsed
                Marshal.WriteInt32(ptr, 36, 0);       // biClrImportant

                IntPtr pixelPtr = ptr + headerSize;

                // Unity's texture data is RGBA.
                // Windows expects BGR and bottom-to-top rows.
                byte[] bgr = new byte[pixelDataSize];

                for (int y = 0; y < height; y++)
                {
                    int sourceY = y;

                    for (int x = 0; x < width; x++)
                    {
                        int source = (sourceY * width + x) * 4;
                        int destination = y * rowSize + x * 3;

                        bgr[destination + 0] = pixels[source + 2]; // B
                        bgr[destination + 1] = pixels[source + 1]; // G
                        bgr[destination + 2] = pixels[source + 0]; // R
                    }
                }

                Marshal.Copy(bgr, 0, pixelPtr, bgr.Length);
            }
            finally
            {
                GlobalUnlock(hGlobal);
            }

            if (!OpenClipboard(IntPtr.Zero))
                return;

            EmptyClipboard();

            // Clipboard now owns hGlobal.
            SetClipboardData(CF_DIB, hGlobal);

            CloseClipboard();
        }

        [YCommand("screenshot", "Take a screenshot and save it the clipboard. The Console UI is disabled briefly.")]
        public static void TakeScreenshot()
        {
            DevConsoleUI.Instance.StartCoroutine(ScreenshotClipboard.CopyScreenshot());
        }
    }
}

#endif