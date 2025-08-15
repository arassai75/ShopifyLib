using System.ComponentModel;

namespace ShopifyLib.Models
{
    /// <summary>
    /// Represents image transformation parameters for Shopify CDN URLs
    /// </summary>
    public class ImageTransformations
    {
        /// <summary>
        /// Width of the transformed image in pixels
        /// </summary>
        public int? Width { get; set; }

        /// <summary>
        /// Height of the transformed image in pixels
        /// </summary>
        public int? Height { get; set; }

        /// <summary>
        /// Crop mode for the image transformation
        /// </summary>
        public CropMode? Crop { get; set; }

        /// <summary>
        /// Output format for the transformed image
        /// </summary>
        public ImageFormat? Format { get; set; }

        /// <summary>
        /// Quality of the transformed image (1-100)
        /// </summary>
        public int? Quality { get; set; }

        /// <summary>
        /// Scale factor for the image
        /// </summary>
        public double? Scale { get; set; }

        /// <summary>
        /// Alt text for the image
        /// </summary>
        public string? Alt { get; set; }

        /// <summary>
        /// Creates a copy of the current transformations
        /// </summary>
        /// <returns>A new ImageTransformations instance with the same values</returns>
        public ImageTransformations Clone()
        {
            return new ImageTransformations
            {
                Width = this.Width,
                Height = this.Height,
                Crop = this.Crop,
                Format = this.Format,
                Quality = this.Quality,
                Scale = this.Scale,
                Alt = this.Alt
            };
        }
    }

    /// <summary>
    /// Crop modes supported by Shopify CDN
    /// </summary>
    public enum CropMode
    {
        [Description("center")]
        Center,
        
        [Description("top")]
        Top,
        
        [Description("bottom")]
        Bottom,
        
        [Description("left")]
        Left,
        
        [Description("right")]
        Right
    }

    /// <summary>
    /// Image formats supported by Shopify CDN
    /// </summary>
    public enum ImageFormat
    {
        [Description("jpg")]
        Jpg,
        
        [Description("jpeg")]
        Jpeg,
        
        [Description("png")]
        Png,
        
        [Description("webp")]
        WebP,
        
        [Description("gif")]
        Gif
    }
} 