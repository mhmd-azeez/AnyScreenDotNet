using System;
using System.IO;

namespace ScreenShare
{
    public static class StreamExtensions
    {
        // https://stackoverflow.com/a/60110009/7003797
        public static string ConvertToBase64(this Stream stream)
        {
            var bytes = new byte[(int)stream.Length];

            stream.Seek(0, SeekOrigin.Begin);
            stream.Read(bytes, 0, (int)stream.Length);

            return Convert.ToBase64String(bytes);
        }
    }
}
