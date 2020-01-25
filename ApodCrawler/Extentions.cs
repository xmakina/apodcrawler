using System;
using System.Drawing;
using System.IO;

namespace ApodCrawler
{
    public static class Extentions
    {
        public static Image ToImage(this byte[] value)
        {
            var memStream = new MemoryStream(value); // if you dispose of the memory stream, you can't save the image
            try
            {
                return Image.FromStream(memStream);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}