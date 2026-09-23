using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YumQuick.Core.DTOs;
using YumQuick.Core.Entities;
using YumQuick.Core.Interfaces;
using YumQuick.Data;

namespace YumQuick.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IImageService _imageService;

        public ProductsController(ApplicationDbContext context, IImageService imageService)
        {
            _context = context;
            _imageService = imageService;
        }

        // GET: api/Products
        [HttpGet]
        public async Task<IActionResult> GetProducts([FromQuery] int? categoryId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            pageSize = pageSize > 50 ? 50 : pageSize;
            var query = _context.Products
                .Include(p => p.Variants)
                .Include(p => p.Category)
                .AsQueryable();
            if (categoryId != null)
            {
                 query = query
                    .Where(p => p.CategoryId == categoryId || p.Category.ParentCategoryId == categoryId)
                    .AsQueryable();
            }


            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var products = await query
                .OrderByDescending(p => p.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    p.ImageUrl,
                    p.OriginalPrice,
                    p.DiscountPercent,
                    FinalPrice = p.OriginalPrice - (p.OriginalPrice * (p.DiscountPercent / 100m)),
                    p.RatingAvg,
                    p.IsAvailable,
                    p.IsBestSeller,
                    p.IsNew,
                    p.CategoryId,
                    CategoryName = p.Category.Name,
                    Variants = p.Variants.Select(v => new { v.Id, v.Name, v.ExtraPrice })
                }).ToListAsync();

            return Ok(new
            {
                Data = products,
                Pagination = new
                {
                    CurrentPage = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    TotalItems = totalItems,
                    HasPrevious = pageNumber > 1,
                    HasNext = pageNumber < totalPages
                }
            });
        }

        // GET: api/Products/best-sellers
        [HttpGet("best-sellers")]
        public async Task<IActionResult> GetBestSellers()
        {
            var products = await _context.Products
                .Include(p => p.Variants)
                .Where(p => p.IsBestSeller && p.IsAvailable)
                .OrderByDescending(p => p.Id)
                .Take(10)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.ImageUrl,
                    p.OriginalPrice,
                    p.DiscountPercent,
                    FinalPrice = p.OriginalPrice - (p.OriginalPrice * (p.DiscountPercent / 100m)),
                    p.RatingAvg,
                    p.IsBestSeller,
                    Variants = p.Variants.Select(v => new { v.Id, v.Name, v.ExtraPrice })
                }).ToListAsync();

            return Ok(products);
        }

        // GET: api/Products/recommended
        [HttpGet("recommended")]
        public async Task<IActionResult> GetRecommended()
        {
            var products = await _context.Products
                .Include(p => p.Variants)
                .Where(p => p.IsAvailable && (p.RatingAvg >= 4 || p.IsNew))
                .OrderByDescending(p => p.RatingAvg)
                .Take(10)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.ImageUrl,
                    p.OriginalPrice,
                    p.DiscountPercent,
                    FinalPrice = p.OriginalPrice - (p.OriginalPrice * (p.DiscountPercent / 100m)),
                    p.RatingAvg,
                    p.IsNew,
                    Variants = p.Variants.Select(v => new { v.Id, v.Name, v.ExtraPrice })
                }).ToListAsync();

            return Ok(products);
        }

        // GET: api/Products/filter
        [HttpGet("filter")]
        public async Task<IActionResult> GetFilteredProducts([FromQuery] ProductFilterDto filter)
        {
            var query = _context.Products
                .Include(p => p.Variants)
                .Include(p => p.Category)
                .AsQueryable();

            
            if (filter.SubCategoryIds != null && filter.SubCategoryIds.Any())
            {
                query = query.Where(p => filter.SubCategoryIds.Contains(p.CategoryId));
            }
            
            else if (filter.MainCategoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == filter.MainCategoryId.Value ||
                                         p.Category.ParentCategoryId == filter.MainCategoryId.Value);
            }

            if (filter.MinRating.HasValue)
            {
                query = query.Where(p => p.RatingAvg >= Convert.ToDecimal(filter.MinRating.Value));
            }

            if (filter.MaxPrice.HasValue)
            {
                query = query.Where(p =>
                    (p.OriginalPrice - (p.OriginalPrice * (p.DiscountPercent / 100m))) <= filter.MaxPrice.Value);
            }

            if (!string.IsNullOrEmpty(filter.SortBy) && filter.SortBy.ToLower() == "rating")
            {
                query = query.OrderByDescending(p => p.RatingAvg);
            }
            else
            {
                query = query.OrderByDescending(p => p.Id);
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)filter.PageSize);

            var products = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    p.ImageUrl,
                    p.OriginalPrice,
                    p.DiscountPercent,
                    FinalPrice = p.OriginalPrice - (p.OriginalPrice * (p.DiscountPercent / 100m)),
                    p.RatingAvg,
                    p.IsAvailable,
                    p.IsBestSeller,
                    p.CategoryId,
                    CategoryName = p.Category.Name,
                    Variants = p.Variants.Select(v => new { v.Id, v.Name, v.ExtraPrice })
                })
                .ToListAsync();

            var response = new
            {
                Data = products,
                Pagination = new
                {
                    CurrentPage = filter.PageNumber,
                    PageSize = filter.PageSize,
                    TotalPages = totalPages,
                    TotalItems = totalItems,
                    HasPrevious = filter.PageNumber > 1,
                    HasNext = filter.PageNumber < totalPages
                }
            };

            return Ok(response);
        }

        // GET: api/Products/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Variants)
                .Where(p => p.Id == id)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    p.ImageUrl,
                    p.OriginalPrice,
                    p.DiscountPercent,
                    FinalPrice = p.OriginalPrice - (p.OriginalPrice * (p.DiscountPercent / 100)),
                    p.RatingAvg,
                    p.IsAvailable,
                    p.CategoryId,
                    Variants = p.Variants.Select(v => new { v.Id, v.Name, v.ExtraPrice })
                }).FirstOrDefaultAsync();

            if (product == null) return NotFound(new { Message = "Product not found" });

            return Ok(product);
        }

        // POST: api/Products
        [HttpPost]
        [Authorize(Roles = "RestaurantManager")]
        public async Task<IActionResult> CreateProduct([FromForm] CreateProductDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId);
            if (!categoryExists) return BadRequest(new { Message = "Category does not exist." });

            string imageUrl = null;
            if (dto.Image != null)
            {
                imageUrl = await _imageService.UploadImageAsync(dto.Image);
            }

            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                OriginalPrice = dto.OriginalPrice,
                DiscountPercent = dto.DiscountPercent,
                IsAvailable = dto.IsAvailable,
                IsBestSeller = dto.IsBestSeller,
                IsNew = dto.IsNew,
                CategoryId = dto.CategoryId,
                ImageUrl = imageUrl ?? "https://res.cloudinary.com/your-cloud/image/upload/v123/default_food.png"
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Product created successfully", ProductId = product.Id });
        }


        // PUT: api/Products/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "RestaurantManager")]
        public async Task<IActionResult> UpdateProduct(int id, [FromForm] UpdateProductDto dto)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound("Product not found");

            if (dto.CategoryId.HasValue)
            {
                var categoryExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId.Value);
                if (!categoryExists) return BadRequest("Category does not exist.");
                product.CategoryId = dto.CategoryId.Value;
            }

            if (!string.IsNullOrEmpty(dto.Name)) product.Name = dto.Name;
            if (!string.IsNullOrEmpty(dto.Description)) product.Description = dto.Description;
            if (dto.OriginalPrice.HasValue) product.OriginalPrice = dto.OriginalPrice.Value;
            if (dto.DiscountPercent.HasValue) product.DiscountPercent = dto.DiscountPercent.Value;
            if (dto.IsAvailable.HasValue) product.IsAvailable = dto.IsAvailable.Value;
            if (dto.IsBestSeller.HasValue) product.IsBestSeller = dto.IsBestSeller.Value;
            if (dto.IsNew.HasValue) product.IsNew = dto.IsNew.Value;

            if (dto.Image != null)
            {
                var imageUrl = await _imageService.UploadImageAsync(dto.Image);
                if (!string.IsNullOrEmpty(imageUrl)) product.ImageUrl = imageUrl;
            }

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Product updated successfully" });
        }

        // DELETE: api/Products/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "RestaurantManager")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound("Product not found");

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Product deleted successfully" });
        }

        // POST: api/Products/{productId}/variants
        [HttpPost("{productId}/variants")]
        [Authorize(Roles = "RestaurantManager")]
        public async Task<IActionResult> AddVariant(int productId, [FromBody] CreateVariantDto dto)
        {
            var productExists = await _context.Products.AnyAsync(p => p.Id == productId);
            if (!productExists) return NotFound(new { Message = "Product not found" });

            var variant = new ProductVariant
            {
                ProductId = productId,
                Name = dto.Name,
                ExtraPrice = dto.ExtraPrice
            };

            _context.ProductVariants.Add(variant);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Variant added successfully", VariantId = variant.Id });
        }

        // PUT: api/Products/variants/{variantId}
        [HttpPut("variants/{variantId}")]
        [Authorize(Roles = "RestaurantManager")]
        public async Task<IActionResult> UpdateVariant(int variantId, [FromBody] UpdateVariantDto dto)
        {
            var variant = await _context.ProductVariants.FindAsync(variantId);
            if (variant == null) return NotFound("Variant not found");

            if (!string.IsNullOrEmpty(dto.Name)) variant.Name = dto.Name;
            if (dto.ExtraPrice.HasValue) variant.ExtraPrice = dto.ExtraPrice.Value;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Variant updated successfully" });
        }

        // DELETE: api/Products/variants/{variantId}
        [HttpDelete("variants/{variantId}")]
        [Authorize(Roles = "RestaurantManager")]
        public async Task<IActionResult> DeleteVariant(int variantId)
        {
            var variant = await _context.ProductVariants.FindAsync(variantId);
            if (variant == null) return NotFound("Variant not found");

            _context.ProductVariants.Remove(variant);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Variant deleted successfully" });
        }

    }
}