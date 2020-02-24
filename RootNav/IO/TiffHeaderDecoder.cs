using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RootNav.IO
{
    public static class TiffHeaderDecoder
    {
        // Tags used by ImageJ
        private const int NEW_SUBFILE_TYPE = 254;
        private const int IMAGE_WIDTH = 256;
        private const int IMAGE_LENGTH = 257;
        private const int BITS_PER_SAMPLE = 258;
        private const int COMPRESSION = 259;
        private const int PHOTO_INTERP = 262;
        private const int IMAGE_DESCRIPTION = 270;
        private const int STRIP_OFFSETS = 273;
        private const int ORIENTATION = 274;
        private const int SAMPLES_PER_PIXEL = 277;
        private const int ROWS_PER_STRIP = 278;
        private const int STRIP_BYTE_COUNT = 279;
        private const int X_RESOLUTION = 282;
        private const int Y_RESOLUTION = 283;
        private const int PLANAR_CONFIGURATION = 284;
        private const int RESOLUTION_UNIT = 296;
        private const int SOFTWARE = 305;
        private const int DATE_TIME = 306;
        private const int ARTEST = 315;
        private const int HOST_COMPUTER = 316;
        private const int PREDICTOR = 317;
        private const int COLOR_MAP = 320;
        private const int TILE_WIDTH = 322;
        private const int SAMPLE_FORMAT = 339;
        private const int JPEG_TABLES = 347;
        private const int METAMORPH1 = 33628;
        private const int METAMORPH2 = 33629;
        private const int IPLAB = 34122;
        private const int NIH_IMAGE_HDR = 43314;
        private const int META_DATA_BYTE_COUNTS = 50838; // Adobe Photoshop tag
        private const int META_DATA = 50839; // Adobe Photoshop tag

        private class RawMetadataItem
        {
            public int location;
            public Object value;
        }

        private static void CaptureMetadata(ImageMetadata imageMetadata, string query, List<RawMetadataItem> dataItems)
        {
            if (dataItems == null)
            {
                dataItems = new List<RawMetadataItem>();
            }

            BitmapMetadata bitmapMetadata = imageMetadata as BitmapMetadata;
            if (bitmapMetadata != null)
            {
                foreach (string relativeQuery in bitmapMetadata)
                {
                    string fullQuery = query + relativeQuery;
                    object metadataQueryReader = bitmapMetadata.GetQuery(relativeQuery);

                    if (relativeQuery != "/ifd" && relativeQuery.IndexOf("/{ushort=") == 0)
                    {
                        RawMetadataItem metadataItem = new RawMetadataItem();
                        Int32.TryParse(relativeQuery.Substring(9).TrimEnd('}'), out metadataItem.location);
                        metadataItem.value = metadataQueryReader;
                        dataItems.Add(metadataItem);
                    }

                    BitmapMetadata innerBitmapMetadata = metadataQueryReader as BitmapMetadata;
                    if (innerBitmapMetadata != null)
                    {
                        CaptureMetadata(innerBitmapMetadata, fullQuery, dataItems);
                    }
                }
            }
        }
        public static TiffHeaderInfo ReadHeaderInfo(String filePath)
        {
            using (Stream fs = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                try
                {
                    TiffHeaderInfo headerInfo = new TiffHeaderInfo();
                    BitmapDecoder decoder = TiffBitmapDecoder.Create(fs, BitmapCreateOptions.None, BitmapCacheOption.Default);
                    BitmapMetadata metadata = decoder.Frames[0].Metadata as BitmapMetadata;

                    if (metadata != null)
                    {
                        List<RawMetadataItem> dataItems = new List<RawMetadataItem>();
                        CaptureMetadata(metadata, "", dataItems);
                        

                        try
                        {
                            foreach (RawMetadataItem item in dataItems)
                            {
                                switch (item.location)
                                {
                                    case NEW_SUBFILE_TYPE:
                                        break;
                                    case IMAGE_WIDTH:
                                        headerInfo.ImageWidth = Convert.ToInt32(item.value);
                                        break;
                                    case IMAGE_LENGTH:
                                        headerInfo.ImageLength = Convert.ToInt32(item.value);
                                        break;
                                    case BITS_PER_SAMPLE:
                                        headerInfo.BitsPerSample = Convert.ToInt32(item.value);
                                        break;
                                    case PHOTO_INTERP:
                                        headerInfo.PhotoInterpolation = Convert.ToInt32(item.value);
                                        break;
                                    case IMAGE_DESCRIPTION:
                                        headerInfo.Description = item.value.ToString();
                                        break;
                                    case STRIP_OFFSETS:
                                        headerInfo.StripOffsets = Convert.ToInt32(item.value);
                                        break;
                                    case SAMPLES_PER_PIXEL:
                                        headerInfo.SamplesPerPixel = Convert.ToInt32(item.value);
                                        break;
                                    case ROWS_PER_STRIP:
                                        headerInfo.RowsPerStrip = Convert.ToInt32(item.value);
                                        break;
                                    case STRIP_BYTE_COUNT:
                                        headerInfo.StripByteCount = Convert.ToInt32(item.value);
                                        break;
                                    case X_RESOLUTION:
                                        headerInfo.XResolution = ToRationalNumber((ulong)item.value);
                                        break;
                                    case Y_RESOLUTION:
                                        headerInfo.YResolution = ToRationalNumber((ulong)item.value);
                                        break;
                                    case RESOLUTION_UNIT:
                                        headerInfo.ResolutionUnit = Convert.ToInt32(item.value);
                                        break;
                                    case COLOR_MAP:
                                        ushort[] values = (ushort[])item.value;
                                        List<int> colors = new List<int>();
                                        foreach (ushort u in values)
                                        {
                                            colors.Add((int)u);
                                        }
                                        headerInfo.ColorMap = colors.ToArray();
                                        break;
                                }
                            }
                        }
                        catch (FormatException) { } // Ignore an unsuccessful cast, default values will remain
                        catch (InvalidCastException) { }     
                    }

                    return headerInfo;
                }
                finally
                {
                    fs.Close();
                }
            }
           
        }  

        private static double ToRationalNumber(ulong value)
        {
            return (double)(value & 0xFFFFFFFF) / (value >> 32);
        }
    }

    public class TiffHeaderInfo
    {
        public int SubFileType { get; set; }
        public int ImageWidth { get; set; }
        public int ImageLength { get; set; }
        public int BitsPerSample { get; set; }
        public int PhotoInterpolation { get; set; }
        public string Description { get; set; }
        public int StripOffsets { get; set; }
        public int SamplesPerPixel { get; set; }
        public int RowsPerStrip { get; set; }
        public int StripByteCount { get; set; }
        public double XResolution { get; set; }
        public double YResolution { get; set; }
        public int ResolutionUnit { get; set; }
        public int[] ColorMap { get; set; }
    }
}
