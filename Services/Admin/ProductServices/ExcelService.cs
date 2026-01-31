using ExcelDataReader;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tech_Store.Models;
using Tech_Store.Models.DTO;
using Tech_Store.Models.Error;
using Tech_Store.Models.Transfer;

namespace Tech_Store.Services.Admin.ProductServices
{
    public class ExcelService
    {
        private readonly ApplicationDbContext _context;
        private static readonly Random _random = new Random();

        public ExcelService(ApplicationDbContext context)
        {
            _context = context;
        }

        //Nhập excel
        public (List<Product> products, List<ImportError> errors) ImportExcel(IFormFile file)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using var stream = file.OpenReadStream();

            IExcelDataReader reader;

            var ext = Path.GetExtension(file.FileName).ToLower();

            if (ext == ".csv")
            {
                reader = ExcelReaderFactory.CreateCsvReader(stream);
            }
            else if (ext == ".xlsx" || ext == ".xls")
            {
                reader = ExcelReaderFactory.CreateReader(stream);
            }
            else
            {
                throw new Exception("File không hỗ trợ. Chỉ nhận CSV / XLS / XLSX");
            }


            var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration
                {
                    UseHeaderRow = true
                }
            });

            var table = dataSet.Tables[0];

            var products = new List<Product>();
            var errors = new List<ImportError>();

            for (int i = 0; i < table.Rows.Count; i++)
            {
                var row = table.Rows[i];

                using var transaction = _context.Database.BeginTransaction();
                try
                {
                    var brandName = row["brand"]?.ToString()?.Trim();
                    var categoryName = row["category"]?.ToString()?.Trim();

                    // BRAND
                    var brand = _context.Brands.FirstOrDefault(x => x.Name == brandName)
                                ?? new Brand { Name = brandName };

                    if (brand.BrandId == 0)
                        _context.Brands.Add(brand);

                    // CATEGORY
                    var category = _context.Categories
                        .FirstOrDefault(x => x.Name.ToLower() == categoryName.ToLower())
                        ?? new Category { Name = categoryName };

                    if (category.CategoryId == 0)
                        _context.Categories.Add(category);

                    _context.SaveChanges();

                    var basePrice = TryDecimal(row["base_price"]);
                    var sellPrice = TryDecimal(row["sell_price"]);

                    var productSysIdFromCsv = row["product_id"]?.ToString()?.Trim();
                    var product_check_unique = _context.Products.FirstOrDefault(x => x.Name.ToLower() == row["name_product"].ToString().ToLower()
                    && x.SellPrice == sellPrice);
                    if (product_check_unique != null)
                    {
                        if (string.IsNullOrEmpty(product_check_unique.ProductSysId))
                        {
                            product_check_unique.ProductSysId = productSysIdFromCsv;
                            _context.SaveChanges();
                          
                        }

                        transaction.Commit();
                        continue;
                    }
                    var product = new Product
                    {
                        ProductSysId = row["product_id"]?.ToString(),
                        Name = row["name_product"]?.ToString(),
                        Brand = brand,
                        Category = category,
                        CostPrice = sellPrice * 0.8m,
                        OriginalPrice = basePrice,
                        SellPrice = sellPrice,
                        Color = row["current_color"]?.ToString(),
                        DiscountPercentage = basePrice > 0
                            ? (short)(((basePrice - sellPrice) / basePrice) * 100)
                            : (short)0,
                        WarrantyPeriod = new[] { "12", "24", "36" }[_random.Next(3)],
                        Image = row["main_image"]?.ToString(),
                        UrlYoutube = row["youtube_url"]?.ToString(),
                        Description = row["description_raw"]?.ToString(),
                        Status = "available",
                        Stock = _random.Next(10, 140),
                        Visible = true,
                        Sku = Guid.NewGuid().ToString("N")[..8].ToUpper(),

                        Slug = GenerateSlug(row["name_product"]?.ToString()),
                        CreatedAt = DateTime.Now
                    };

                    _context.Products.Add(product);
                    _context.SaveChanges();

                    // ===================== SPECS =====================
                    if (row["specs"] != null)
                    {
                        var specs = ConvertSpec(row["specs"].ToString());

                        foreach (var item in specs)
                        {
                            var specEntity = _context.Species.FirstOrDefault(x => x.Name == item.Key);

                            if (specEntity == null)
                            {
                                specEntity = new Specs { Name = item.Key };
                                _context.Species.Add(specEntity);
                                _context.SaveChanges();
                            }

                            _context.SpecValues.Add(new SpecValue
                            {
                                SpecId = specEntity.SpecId,
                                ProductId = product.ProductId,
                                Value = item.Value
                            });
                        }

                        _context.SaveChanges();
                    }

                    // ===================== GALLERY =====================
                    var imagesRaw = row["images"]?.ToString();
                    if (!string.IsNullOrEmpty(imagesRaw))
                    {
                        var imageList = imagesRaw.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var img in imageList)
                        {
                            _context.Galleries.Add(new Gallery
                            {
                                ProductId = product.ProductId,
                                Path = img.Trim()
                            });
                        }

                        _context.SaveChanges();
                    }

                    // ===================== VARIANTS =====================
                    var colorVariantsJson = row["color_variants"]?.ToString();

                    if (!string.IsNullOrEmpty(colorVariantsJson))
                    {
                        var variants = ConvertVariant(colorVariantsJson);
                        var attribute = _context.Attributes.First(x => x.Code == "color");

                        foreach (var v in variants)
                        {
                            // 1️⃣ AttributeValue (color: Trắng)
                            var attrValue = _context.AttributeValues
                                .FirstOrDefault(x =>
                                    x.Value == v.color &&
                                    x.AttributeId == attribute.AttributeId);

                            if (attrValue == null)
                            {
                                attrValue = new AttributeValue
                                {
                                    Value = v.color,
                                    AttributeId = attribute.AttributeId
                                };
                                _context.AttributeValues.Add(attrValue);
                                _context.SaveChanges();
                            }

                            var variantEntity = new VarientProduct
                            {
                                ProductId = product.ProductId,
                                Price = v.price,
                                ImageUrl = v.img,
                                Stock = product.Stock / Math.Max(1, variants.Count),
                                Sku = product.Sku + "-" + RemoveDiacritics(v.color),
                                CreatedAt = DateTime.Now,
                                Attributes = $"color:{v.color}"
                            };

                            _context.VarientProducts.Add(variantEntity);
                            _context.SaveChanges();

                            _context.VariantAttributes.Add(new VariantAttribute
                            {
                                ProductVariantId = variantEntity.VarientId,
                                AttributeValueId = attrValue.AttributeValueId
                            });
                        }

                        _context.SaveChanges();
                    }


                    transaction.Commit();
                    products.Add(product);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    errors.Add(new ImportError
                    {
                        Row = i + 2, // + header
                        ProductName = row["name_product"]?.ToString(),
                        Error = ex.Message
                    });
                }
            }

            return (products, errors);
        }


        private static Dictionary<string, string> ConvertSpec(string spec)
        {
            if (string.IsNullOrWhiteSpace(spec))
                return new Dictionary<string, string>();

            return JsonSerializer.Deserialize<Dictionary<string, string>>(spec)
                   ?? new Dictionary<string, string>();
        }

        private static List<Variant_TF> ConvertVariant(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<Variant_TF>();

            return JsonSerializer.Deserialize<List<Variant_TF>>(json)
                   ?? new List<Variant_TF>();
        }


        public static string GenerateSlug(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            // Chuyển đổi chữ thường và bỏ dấu
            string slug = RemoveDiacritics(name.ToLower());

            // Loại bỏ ký tự đặc biệt, chỉ giữ lại chữ cái, số và khoảng trắng
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");

            // Thay khoảng trắng và dấu gạch ngang liên tiếp thành một dấu gạch ngang
            slug = Regex.Replace(slug, @"\s+", "-").Trim();

            // Loại bỏ dấu gạch ngang dư thừa ở đầu và cuối
            slug = slug.Trim('-');

            return slug;
        }

        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        private int TryInt(object value)
            => int.TryParse(value?.ToString(), out var v) ? v : 0;

        private decimal TryDecimal(object value)
            => decimal.TryParse(value?.ToString(), out var v) ? v : 0;
    }
}
