using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using ShopifyLib.Models;
using ShopifyLib.Services;

namespace ShopifyLib.Tests
{
    /// <summary>
    /// Unit tests for ImageTransformationService
    /// </summary>
    public class ImageTransformationTests
    {
        private readonly ImageTransformationService _service;
        private const string BaseCdnUrl = "https://cdn.shopify.com/s/files/1/1234/5678/files/sample.jpg";

        public ImageTransformationTests()
        {
            _service = new ImageTransformationService();
        }

        [Fact]
        public void BuildTransformedUrl_WithNullTransformations_ReturnsOriginalUrl()
        {
            // Act
            var result = _service.BuildTransformedUrl(BaseCdnUrl, null);

            // Assert
            Assert.Equal(BaseCdnUrl, result);
        }

        [Fact]
        public void BuildTransformedUrl_WithEmptyTransformations_ReturnsOriginalUrl()
        {
            // Arrange
            var transformations = new ImageTransformations();

            // Act
            var result = _service.BuildTransformedUrl(BaseCdnUrl, transformations);

            // Assert
            Assert.Equal(BaseCdnUrl, result);
        }

        [Fact]
        public void BuildTransformedUrl_WithWidthOnly_ReturnsCorrectUrl()
        {
            // Arrange
            var transformations = new ImageTransformations { Width = 800 };

            // Act
            var result = _service.BuildTransformedUrl(BaseCdnUrl, transformations);

            // Assert
            Assert.Equal($"{BaseCdnUrl}?width=800", result);
        }

        [Fact]
        public void BuildTransformedUrl_WithHeightOnly_ReturnsCorrectUrl()
        {
            // Arrange
            var transformations = new ImageTransformations { Height = 600 };

            // Act
            var result = _service.BuildTransformedUrl(BaseCdnUrl, transformations);

            // Assert
            Assert.Equal($"{BaseCdnUrl}?height=600", result);
        }

        [Fact]
        public void BuildTransformedUrl_WithWidthAndHeight_ReturnsCorrectUrl()
        {
            // Arrange
            var transformations = new ImageTransformations 
            { 
                Width = 800, 
                Height = 600 
            };

            // Act
            var result = _service.BuildTransformedUrl(BaseCdnUrl, transformations);

            // Assert
            Assert.Equal($"{BaseCdnUrl}?width=800&height=600", result);
        }

        [Fact]
        public void BuildTransformedUrl_WithCrop_ReturnsCorrectUrl()
        {
            // Arrange
            var transformations = new ImageTransformations 
            { 
                Crop = CropMode.Center 
            };

            // Act
            var result = _service.BuildTransformedUrl(BaseCdnUrl, transformations);

            // Assert
            Assert.Equal($"{BaseCdnUrl}?crop=center", result);
        }

        [Fact]
        public void BuildTransformedUrl_WithFormat_ReturnsCorrectUrl()
        {
            // Arrange
            var transformations = new ImageTransformations 
            { 
                Format = ImageFormat.WebP 
            };

            // Act
            var result = _service.BuildTransformedUrl(BaseCdnUrl, transformations);

            // Assert
            Assert.Equal($"{BaseCdnUrl}?format=webp", result);
        }

        [Fact]
        public void BuildTransformedUrl_WithQuality_ReturnsCorrectUrl()
        {
            // Arrange
            var transformations = new ImageTransformations 
            { 
                Quality = 85 
            };

            // Act
            var result = _service.BuildTransformedUrl(BaseCdnUrl, transformations);

            // Assert
            Assert.Equal($"{BaseCdnUrl}?quality=85", result);
        }

        [Fact]
        public void BuildTransformedUrl_WithScale_ReturnsCorrectUrl()
        {
            // Arrange
            var transformations = new ImageTransformations 
            { 
                Scale = 2.0 
            };

            // Act
            var result = _service.BuildTransformedUrl(BaseCdnUrl, transformations);

            // Assert
            Assert.Equal($"{BaseCdnUrl}?scale=2", result);
        }

        [Fact]
        public void BuildTransformedUrl_WithAllParameters_ReturnsCorrectUrl()
        {
            // Arrange
            var transformations = new ImageTransformations 
            { 
                Width = 800,
                Height = 600,
                Crop = CropMode.Center,
                Format = ImageFormat.WebP,
                Quality = 85,
                Scale = 1.5
            };

            // Act
            var result = _service.BuildTransformedUrl(BaseCdnUrl, transformations);

            // Assert
            Assert.Equal($"{BaseCdnUrl}?width=800&height=600&crop=center&format=webp&quality=85&scale=1.5", result);
        }

        [Fact]
        public void BuildTransformedUrl_WithQualityOutOfRange_ClampsToValidRange()
        {
            // Arrange
            var transformations = new ImageTransformations { Quality = 150 }; // Above 100

            // Act
            var result = _service.BuildTransformedUrl(BaseCdnUrl, transformations);

            // Assert
            Assert.Equal($"{BaseCdnUrl}?quality=100", result);
        }

        [Fact]
        public void BuildTransformedUrl_WithNegativeQuality_ClampsToValidRange()
        {
            // Arrange
            var transformations = new ImageTransformations { Quality = -10 }; // Below 1

            // Act
            var result = _service.BuildTransformedUrl(BaseCdnUrl, transformations);

            // Assert
            Assert.Equal($"{BaseCdnUrl}?quality=1", result);
        }

        [Fact]
        public void BuildTransformedUrl_WithExistingQueryParameters_AppendsCorrectly()
        {
            // Arrange
            var urlWithParams = $"{BaseCdnUrl}?existing=value";
            var transformations = new ImageTransformations { Width = 800 };

            // Act
            var result = _service.BuildTransformedUrl(urlWithParams, transformations);

            // Assert
            Assert.Equal($"{BaseCdnUrl}?existing=value&width=800", result);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void BuildTransformedUrl_WithInvalidBaseUrl_ThrowsArgumentException(string invalidUrl)
        {
            // Arrange
            var transformations = new ImageTransformations { Width = 800 };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _service.BuildTransformedUrl(invalidUrl, transformations));
        }

        [Fact]
        public void CreateThumbnailUrl_WithDefaultParameters_ReturnsCorrectUrl()
        {
            // Act
            var result = _service.CreateThumbnailUrl(BaseCdnUrl);

            // Assert
            Assert.Equal($"{BaseCdnUrl}?width=150&height=150&crop=center", result);
        }

        [Fact]
        public void CreateThumbnailUrl_WithCustomSize_ReturnsCorrectUrl()
        {
            // Act
            var result = _service.CreateThumbnailUrl(BaseCdnUrl, 300);

            // Assert
            Assert.Equal($"{BaseCdnUrl}?width=300&height=300&crop=center", result);
        }

        [Fact]
        public void CreateThumbnailUrl_WithCustomCrop_ReturnsCorrectUrl()
        {
            // Act
            var result = _service.CreateThumbnailUrl(BaseCdnUrl, 150, CropMode.Top);

            // Assert
            Assert.Equal($"{BaseCdnUrl}?width=150&height=150&crop=top", result);
        }

        [Fact]
        public void CreateMediumUrl_WithDefaultParameters_ReturnsCorrectUrl()
        {
            // Act
            var result = _service.CreateMediumUrl(BaseCdnUrl);

            // Assert
            Assert.Equal($"{BaseCdnUrl}?width=800&height=600&crop=center", result);
        }

        [Fact]
        public void CreateMediumUrl_WithCustomParameters_ReturnsCorrectUrl()
        {
            // Act
            var result = _service.CreateMediumUrl(BaseCdnUrl, 1024, 768, CropMode.Bottom);

            // Assert
            Assert.Equal($"{BaseCdnUrl}?width=1024&height=768&crop=bottom", result);
        }

        [Fact]
        public void CreateLargeUrl_WithDefaultParameters_ReturnsCorrectUrl()
        {
            // Act
            var result = _service.CreateLargeUrl(BaseCdnUrl);

            // Assert
            Assert.Equal($"{BaseCdnUrl}?width=1200&height=800&crop=center", result);
        }

        [Fact]
        public void CreateLargeUrl_WithCustomParameters_ReturnsCorrectUrl()
        {
            // Act
            var result = _service.CreateLargeUrl(BaseCdnUrl, 1920, 1080, CropMode.Left);

            // Assert
            Assert.Equal($"{BaseCdnUrl}?width=1920&height=1080&crop=left", result);
        }

        [Fact]
        public void CreateWebPUrl_WithDefaultQuality_ReturnsCorrectUrl()
        {
            // Act
            var result = _service.CreateWebPUrl(BaseCdnUrl);

            // Assert
            Assert.Equal($"{BaseCdnUrl}?format=webp&quality=85", result);
        }

        [Fact]
        public void CreateWebPUrl_WithCustomQuality_ReturnsCorrectUrl()
        {
            // Act
            var result = _service.CreateWebPUrl(BaseCdnUrl, 95);

            // Assert
            Assert.Equal($"{BaseCdnUrl}?format=webp&quality=95", result);
        }

        [Fact]
        public void CreateHighQualityUrl_WithDefaultQuality_ReturnsCorrectUrl()
        {
            // Act
            var result = _service.CreateHighQualityUrl(BaseCdnUrl);

            // Assert
            Assert.Equal($"{BaseCdnUrl}?quality=100", result);
        }

        [Fact]
        public void CreateHighQualityUrl_WithCustomQuality_ReturnsCorrectUrl()
        {
            // Act
            var result = _service.CreateHighQualityUrl(BaseCdnUrl, 90);

            // Assert
            Assert.Equal($"{BaseCdnUrl}?quality=90", result);
        }

        [Fact]
        public void CreateResponsiveUrls_ReturnsAllExpectedUrls()
        {
            // Act
            var result = _service.CreateResponsiveUrls(BaseCdnUrl);

            // Assert
            Assert.Equal(6, result.Count);
            Assert.Contains("thumbnail", result.Keys);
            Assert.Contains("small", result.Keys);
            Assert.Contains("medium", result.Keys);
            Assert.Contains("large", result.Keys);
            Assert.Contains("webp", result.Keys);
            Assert.Contains("original", result.Keys);

            // Verify specific URLs
            Assert.Equal($"{BaseCdnUrl}?width=150&height=150&crop=center", result["thumbnail"]);
            Assert.Equal($"{BaseCdnUrl}?width=300&height=300&crop=center", result["small"]);
            Assert.Equal($"{BaseCdnUrl}?width=800&height=600&crop=center", result["medium"]);
            Assert.Equal($"{BaseCdnUrl}?width=1200&height=800&crop=center", result["large"]);
            Assert.Equal($"{BaseCdnUrl}?format=webp&quality=85", result["webp"]);
            Assert.Equal(BaseCdnUrl, result["original"]);
        }

        [Theory]
        [InlineData(CropMode.Center, "center")]
        [InlineData(CropMode.Top, "top")]
        [InlineData(CropMode.Bottom, "bottom")]
        [InlineData(CropMode.Left, "left")]
        [InlineData(CropMode.Right, "right")]
        public void BuildTransformedUrl_WithDifferentCropModes_ReturnsCorrectValues(CropMode cropMode, string expectedValue)
        {
            // Arrange
            var transformations = new ImageTransformations { Crop = cropMode };

            // Act
            var result = _service.BuildTransformedUrl(BaseCdnUrl, transformations);

            // Assert
            Assert.Equal($"{BaseCdnUrl}?crop={expectedValue}", result);
        }

        [Theory]
        [InlineData(ImageFormat.Jpg, "jpg")]
        [InlineData(ImageFormat.Jpeg, "jpeg")]
        [InlineData(ImageFormat.Png, "png")]
        [InlineData(ImageFormat.WebP, "webp")]
        [InlineData(ImageFormat.Gif, "gif")]
        public void BuildTransformedUrl_WithDifferentFormats_ReturnsCorrectValues(ImageFormat format, string expectedValue)
        {
            // Arrange
            var transformations = new ImageTransformations { Format = format };

            // Act
            var result = _service.BuildTransformedUrl(BaseCdnUrl, transformations);

            // Assert
            Assert.Equal($"{BaseCdnUrl}?format={expectedValue}", result);
        }

        [Fact]
        public void ImageTransformations_Clone_ReturnsIdenticalCopy()
        {
            // Arrange
            var original = new ImageTransformations
            {
                Width = 800,
                Height = 600,
                Crop = CropMode.Center,
                Format = ImageFormat.WebP,
                Quality = 85,
                Scale = 1.5,
                Alt = "Test Image"
            };

            // Act
            var clone = original.Clone();

            // Assert
            Assert.Equal(original.Width, clone.Width);
            Assert.Equal(original.Height, clone.Height);
            Assert.Equal(original.Crop, clone.Crop);
            Assert.Equal(original.Format, clone.Format);
            Assert.Equal(original.Quality, clone.Quality);
            Assert.Equal(original.Scale, clone.Scale);
            Assert.Equal(original.Alt, clone.Alt);
        }
    }
} 