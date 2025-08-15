using System;
using System.Text.Json;
using Xunit;
using ShopifyLib.Models;

namespace ShopifyLib.Tests
{
    public class ModelTests
    {
        [Fact]
        public void Product_CanBeSerializedAndDeserialized()
        {
            // Arrange
            var product = new Product
            {
                Id = 123,
                Title = "Test Product",
                BodyHtml = "<p>Test description</p>",
                Vendor = "Test Vendor",
                ProductType = "Test Type",
                Handle = "test-product",
                Status = "active",
                Tags = "test, sample",
                Published = true,
                PublishedScope = "web",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Act
            var json = JsonSerializer.Serialize(product);
            var deserializedProduct = JsonSerializer.Deserialize<Product>(json);

            // Assert
            Assert.NotNull(deserializedProduct);
            Assert.Equal(product.Id, deserializedProduct.Id);
            Assert.Equal(product.Title, deserializedProduct.Title);
            Assert.Equal(product.BodyHtml, deserializedProduct.BodyHtml);
            Assert.Equal(product.Vendor, deserializedProduct.Vendor);
            Assert.Equal(product.ProductType, deserializedProduct.ProductType);
            Assert.Equal(product.Handle, deserializedProduct.Handle);
            Assert.Equal(product.Status, deserializedProduct.Status);
            Assert.Equal(product.Tags, deserializedProduct.Tags);
            Assert.Equal(product.Published, deserializedProduct.Published);
            Assert.Equal(product.PublishedScope, deserializedProduct.PublishedScope);
        }

        [Fact]
        public void Product_WithImagesAndVariants_CanBeSerialized()
        {
            // Arrange
            var product = new Product
            {
                Id = 123,
                Title = "Test Product",
                Images = new System.Collections.Generic.List<ProductImage>
                {
                    new ProductImage
                    {
                        Id = 1,
                        ProductId = 123,
                        Src = "https://example.com/image.jpg",
                        Alt = "Test image",
                        Width = 800,
                        Height = 600
                    }
                },
                Variants = new System.Collections.Generic.List<ProductVariant>
                {
                    new ProductVariant
                    {
                        Id = 1,
                        ProductId = 123,
                        Title = "Default Title",
                        Price = "19.99",
                        Sku = "TEST-SKU-001",
                        InventoryQuantity = 10
                    }
                }
            };

            // Act
            var json = JsonSerializer.Serialize(product);
            var deserializedProduct = JsonSerializer.Deserialize<Product>(json);

            // Assert
            Assert.NotNull(deserializedProduct);
            Assert.Single(deserializedProduct.Images);
            Assert.Single(deserializedProduct.Variants);
            Assert.Equal("https://example.com/image.jpg", deserializedProduct.Images[0].Src);
            Assert.Equal("19.99", deserializedProduct.Variants[0].Price);
        }

        [Fact]
        public void Metafield_CanBeSerializedAndDeserialized()
        {
            // Arrange
            var metafield = new Metafield
            {
                Id = "gid://shopify/Metafield/456",
                Namespace = "custom",
                Key = "size",
                Value = "Large",
                Type = "single_line_text_field"
            };

            // Act
            var json = JsonSerializer.Serialize(metafield);
            var deserializedMetafield = JsonSerializer.Deserialize<Metafield>(json);

            // Assert
            Assert.NotNull(deserializedMetafield);
            Assert.Equal(metafield.Id, deserializedMetafield.Id);
            Assert.Equal(metafield.Namespace, deserializedMetafield.Namespace);
            Assert.Equal(metafield.Key, deserializedMetafield.Key);
            Assert.Equal(metafield.Value, deserializedMetafield.Value);
            Assert.Equal(metafield.Type, deserializedMetafield.Type);
        }

        [Fact]
        public void MetafieldDefinition_CanBeSerializedAndDeserialized()
        {
            // Arrange
            var definition = new MetafieldDefinition
            {
                Id = "gid://shopify/MetafieldDefinition/789",
                Namespace = "custom",
                Key = "size",
                Name = "Size",
                Type = "single_line_text_field"
            };

            // Act
            var json = JsonSerializer.Serialize(definition);
            var deserializedDefinition = JsonSerializer.Deserialize<MetafieldDefinition>(json);

            // Assert
            Assert.NotNull(deserializedDefinition);
            Assert.Equal(definition.Id, deserializedDefinition.Id);
            Assert.Equal(definition.Namespace, deserializedDefinition.Namespace);
            Assert.Equal(definition.Key, deserializedDefinition.Key);
            Assert.Equal(definition.Name, deserializedDefinition.Name);
            Assert.Equal(definition.Type, deserializedDefinition.Type);
        }

        [Fact]
        public void ProductImage_CanBeSerializedAndDeserialized()
        {
            // Arrange
            var image = new ProductImage
            {
                Id = 1,
                ProductId = 123,
                Position = 1,
                Src = "https://example.com/image.jpg",
                Width = 800,
                Height = 600,
                Alt = "Test product image",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Act
            var json = JsonSerializer.Serialize(image);
            var deserializedImage = JsonSerializer.Deserialize<ProductImage>(json);

            // Assert
            Assert.NotNull(deserializedImage);
            Assert.Equal(image.Id, deserializedImage.Id);
            Assert.Equal(image.ProductId, deserializedImage.ProductId);
            Assert.Equal(image.Position, deserializedImage.Position);
            Assert.Equal(image.Src, deserializedImage.Src);
            Assert.Equal(image.Width, deserializedImage.Width);
            Assert.Equal(image.Height, deserializedImage.Height);
            Assert.Equal(image.Alt, deserializedImage.Alt);
        }

        [Fact]
        public void ProductVariant_CanBeSerializedAndDeserialized()
        {
            // Arrange
            var variant = new ProductVariant
            {
                Id = 1,
                ProductId = 123,
                Title = "Default Title",
                Price = "19.99",
                Sku = "TEST-SKU-001",
                Barcode = "123456789",
                Weight = 0.5m,
                WeightUnit = "kg",
                InventoryQuantity = 10,
                InventoryManagement = "shopify",
                InventoryPolicy = "deny",
                RequiresShipping = true,
                Taxable = true,
                Option1 = "Red",
                Option2 = "Large",
                Option3 = "Cotton"
            };

            // Act
            var json = JsonSerializer.Serialize(variant);
            var deserializedVariant = JsonSerializer.Deserialize<ProductVariant>(json);

            // Assert
            Assert.NotNull(deserializedVariant);
            Assert.Equal(variant.Id, deserializedVariant.Id);
            Assert.Equal(variant.ProductId, deserializedVariant.ProductId);
            Assert.Equal(variant.Title, deserializedVariant.Title);
            Assert.Equal(variant.Price, deserializedVariant.Price);
            Assert.Equal(variant.Sku, deserializedVariant.Sku);
            Assert.Equal(variant.Barcode, deserializedVariant.Barcode);
            Assert.Equal(variant.Weight, deserializedVariant.Weight);
            Assert.Equal(variant.WeightUnit, deserializedVariant.WeightUnit);
            Assert.Equal(variant.InventoryQuantity, deserializedVariant.InventoryQuantity);
            Assert.Equal(variant.InventoryManagement, deserializedVariant.InventoryManagement);
            Assert.Equal(variant.InventoryPolicy, deserializedVariant.InventoryPolicy);
            Assert.Equal(variant.RequiresShipping, deserializedVariant.RequiresShipping);
            Assert.Equal(variant.Taxable, deserializedVariant.Taxable);
            Assert.Equal(variant.Option1, deserializedVariant.Option1);
            Assert.Equal(variant.Option2, deserializedVariant.Option2);
            Assert.Equal(variant.Option3, deserializedVariant.Option3);
        }
    }
} 