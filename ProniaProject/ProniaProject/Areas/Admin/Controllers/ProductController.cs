﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProniaProject.Areas.Admin.ViewModels;
using ProniaProject.DAL;
using ProniaProject.Models;
using ProniaProject.Utils;

namespace ProniaProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    //[Authorize(Roles = "Admin,Moderator")]
    [AutoValidateAntiforgeryToken]
    public class ProductController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;

        }
        public async Task<IActionResult> Index()
        {
            List<GetProductAdminVM> productsVM = await _context.Products.Include(p => p.Category).Include(p => p.ColorProducts).ThenInclude(pi => pi.Color).Include(p => p.Images.Where(pi => pi.IsPrime == true)).
                  Select(
                     p => new GetProductAdminVM
                     {
                         Id = p.Id,
                         Name = p.Name,
                         CategoryName = p.Category.Name,
                         Price = p.Price,
                         Image = p.Images[0].Image,


                     }
                  )
                  .ToListAsync();

            return View(productsVM);
        }

        public async Task<IActionResult> Create()
        {
            CreateProductVM productVM = new()
            {
                Tags = await _context.Tags.ToListAsync(),
                Categories = await _context.Categories.ToListAsync(),
                Colors = await _context.Color.ToListAsync(),
                Sizes = await _context.Sizes.ToListAsync()

            };

            return View(productVM);
        }


        [HttpPost]
        public async Task<IActionResult> Create(CreateProductVM productVM)
        {
            productVM.Categories = await _context.Categories.ToListAsync();
            productVM.Tags = await _context.Tags.ToListAsync();
            productVM.Colors = await _context.Color.ToListAsync();
            productVM.Sizes = await _context.Sizes.ToListAsync();



            if (!ModelState.IsValid)
            {
                return View(productVM);
            }

            if (!productVM.MainPhoto.ValidateType("image/"))
            {
                ModelState.AddModelError(nameof(productVM.MainPhoto), "Photo Type is invalid");
                return View(productVM);
            }

            if (!productVM.MainPhoto.ValidateSize(Utils.Enums.FileSize.Mb, 1))
            {
                ModelState.AddModelError(nameof(productVM.MainPhoto), "Photo size is invalid");
                return View(productVM);
            }

            if (!productVM.HoverPhoto.ValidateType("image/"))
            {
                ModelState.AddModelError(nameof(productVM.HoverPhoto), "Photo Type is invalid");
                return View(productVM);
            }

            if (!productVM.HoverPhoto.ValidateSize(Utils.Enums.FileSize.Mb, 1))
            {
                ModelState.AddModelError(nameof(productVM.HoverPhoto), "Photo size is invalid");
                return View(productVM);
            }

           

            bool result = productVM.Categories.Any(c => c.Id == productVM.CategoryId);

            if (!result)
            {
                ModelState.AddModelError(nameof(CreateProductVM), "Category does not exist");
                productVM.Categories = await _context.Categories.ToListAsync();
                return View(productVM);
            }

            if (productVM.TagIds is null)
            {
                productVM.TagIds = new List<int>();
            }


            bool tagResult = productVM.TagIds.Any(tId => !productVM.Tags.Exists(t => t.Id == tId));

            if (tagResult)
            {
                ModelState.AddModelError(nameof(CreateProductVM.TagIds), "not exists");
                return View(productVM);
            }



            if (productVM.Colors is not null)
            {
                bool colorResult = productVM.ColorIds.Any(cId => !productVM.Colors.Exists(c => c.Id == cId));

                if (colorResult)
                {
                    ModelState.AddModelError(nameof(CreateProductVM.ColorIds), "Colors are wrong");
                    return View(productVM);
                }
            }

            if (productVM.Sizes is not null)
            {
                bool sizeresult = productVM.SizeIds.Any(cId => !productVM.Sizes.Exists(c => c.Id == cId));

                if (sizeresult)
                {
                    ModelState.AddModelError(nameof(CreateProductVM.SizeIds), "Sizes are wrong");
                    return View(productVM);
                }
            }

            ProductImage mainImage = new()
            {
                Image = await productVM.MainPhoto.CreateFileAsync(_env.WebRootPath, "assets", "images", "website-images"),
                IsPrime = true,
                CreatedAt = DateTime.Now,
                IsDeleted = false

            };

            ProductImage hoverImage = new()
            {
                Image = await productVM.HoverPhoto.CreateFileAsync(_env.WebRootPath, "assets", "images", "website-images"),
                IsPrime = false,
                CreatedAt = DateTime.Now,
                IsDeleted = false
            };

            Product product = new()
            {
                Name = productVM.Name,
                SKU = productVM.SKU,
                CategoryId = productVM.CategoryId,
                Price = productVM.Price,
                Description = productVM.Description,
                CreatedAt = DateTime.Now,
                IsDeleted = false,
                ProductTags = productVM.TagIds.Select(tId => new ProductTag { TagId = tId }).ToList(),
                Images = new List<ProductImage> { mainImage, hoverImage },


            };

            if (productVM.ColorIds is not null)
            {
                product.ColorProducts = productVM.ColorIds.Select(cId => new ColorProduct { ColorId = cId }).ToList();
            }

            if (productVM.SizeIds is not null)
            {
                product.ProductSizes = productVM.SizeIds.Select(sId => new ProductSize { SizeId = sId }).ToList();
            }

            if(productVM.AdditionalPhotos is not null)
            {
                string text = string.Empty;
                foreach (IFormFile addPhoto in productVM.AdditionalPhotos)
                {
                    if (!addPhoto.ValidateType("image/"))
                    {
                        text += $"<div class=\"alert alert-warning\" role=\"alert\">\r\n  {addPhoto.FileName} is not valid type\r\n</div>";
                        continue;
                    }

                    if (!addPhoto.ValidateSize(Utils.Enums.FileSize.Mb, 1))
                    {
                        text += $"<div class=\"alert alert-warning\" role=\"alert\">\r\n  {addPhoto.FileName} is not valid size\r\n</div>";
                        continue;
                    }

                    product.Images.Add(new ProductImage
                    {
                        Image = await addPhoto.CreateFileAsync(_env.WebRootPath, "assets", "images", "website-images"),
                        CreatedAt = DateTime.Now,
                        IsDeleted = false,
                        IsPrime = null

                    });

                }
                TempData["FileWarning"] = text;
            }
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles ="Admin")]

        public async Task<IActionResult> Update(int? id)
        {
            if (id == null || id < 1) return BadRequest();

            Product product = await _context.Products.Include(p => p.ProductTags).Include(p=>p.Images).Include(p => p.ColorProducts).ThenInclude(cp => cp.Color).FirstOrDefaultAsync(p => p.Id == id);

            if (product is null) return NotFound();


            UpdateProductVM productVM = new()
            {
                Name = product.Name,
                SKU = product.SKU,
                CategoryId = product.CategoryId,
                Price = product.Price,
                Description = product.Description,
                Categories = await _context.Categories.ToListAsync(),
                Tags = await _context.Tags.ToListAsync(),
                TagIds = product.ProductTags.Select(pt => pt.TagId).ToList(),
                Colors = await _context.Color.ToListAsync(),
                Images = product.Images


            };

            return View(productVM);
        }

        [HttpPost]
        public async Task<IActionResult> Update(int? id, UpdateProductVM productVM)
        {

            Product existed = await _context.Products.Include(p => p.ProductTags).Include(p=>p.Images).FirstOrDefaultAsync(p => p.Id == id);

            productVM.Categories = await _context.Categories.ToListAsync();
            productVM.Tags = await _context.Tags.ToListAsync();
            productVM.Images = existed.Images;

            if (!ModelState.IsValid)
            {
                return View(productVM);
            }
            if (productVM.MainPhoto is not null)
            {
                if (!productVM.MainPhoto.ValidateType("image/"))
                {
                    ModelState.AddModelError(nameof(productVM.MainPhoto), "Photo Type is invalid");
                    return View(productVM);
                }

                if (!productVM.MainPhoto.ValidateSize(Utils.Enums.FileSize.Mb, 1))
                {
                    ModelState.AddModelError(nameof(productVM.MainPhoto), "Photo size is invalid");
                    return View(productVM);
                }

            }

            if (productVM.HoverPhoto is not null) {
                if (!productVM.HoverPhoto.ValidateType("image/"))
                {
                    ModelState.AddModelError(nameof(productVM.HoverPhoto), "Photo Type is invalid");
                    return View(productVM);
                }

                if (!productVM.HoverPhoto.ValidateSize(Utils.Enums.FileSize.Mb, 1))
                {
                    ModelState.AddModelError(nameof(productVM.HoverPhoto), "Photo size is invalid");
                    return View(productVM);
                }


            }

            

            if (existed.CategoryId != productVM.CategoryId)
            {
                bool result = _context.Products.Any(c => c.Id == productVM.CategoryId);

                if (!result)
                {
                    productVM.Categories = await _context.Categories.ToListAsync();
                    return View(productVM);
                }
            }

            if (productVM.TagIds is not null)
            {
                bool tagResult = productVM.TagIds.Any(tId => !productVM.Tags.Exists(t => t.Id == tId));

                if (tagResult)
                {
                    ModelState.AddModelError(nameof(UpdateProductVM.TagIds), "not exists");
                    return View(productVM);
                }
            }
            if (productVM.TagIds is null)
            {
                productVM.TagIds = new();
            }
            _context.ProductTags
                .RemoveRange(existed.ProductTags
                .Where(pTag => !productVM.TagIds
                .Exists(tId => tId == pTag.TagId))
                .ToList());

            _context.ProductTags.AddRange(
         productVM.TagIds
         .Where(tId => !existed.ProductTags.Exists(pTag => pTag.TagId == tId))
         .Select(tId => new ProductTag { TagId = tId, ProductId = existed.Id })
         .ToList());

            if(productVM.MainPhoto is not null)
            {
                string fileName = await productVM.MainPhoto.CreateFileAsync(_env.WebRootPath, "assets", "images", "website-images");
                ProductImage mainImg =  existed.Images.FirstOrDefault(p=>p.IsPrime == true);
                mainImg.Image.DeleteImage(_env.WebRootPath, "assets", "images", "website-images");
                existed.Images.Remove(mainImg);
                existed.Images.Add(new ProductImage
                {
                    Image = fileName,
                    CreatedAt = DateTime.Now,
                    IsDeleted = false,
                    IsPrime = true
                });
            }

            if (productVM.HoverPhoto is not null)
            {
                string fileName = await productVM.MainPhoto.CreateFileAsync(_env.WebRootPath, "assets", "images", "website-images");
                ProductImage hoverImg = existed.Images.FirstOrDefault(p => p.IsPrime == true);
                hoverImg.Image.DeleteImage(_env.WebRootPath, "assets", "images", "website-images");
                existed.Images.Remove(hoverImg);
                existed.Images.Add(new ProductImage
                {
                    Image = fileName,
                    CreatedAt = DateTime.Now,
                    IsDeleted = false,
                    IsPrime = false
                });
            }
            if(productVM.ImageIds is null)
            {
                productVM.ImageIds = new List<int>();
            }

            var deletedImages = existed.Images.Where(p => productVM.ImageIds.Exists(imgId=>imgId == p.Id && p.IsPrime==null)).ToList();

            deletedImages.ForEach(p => p.Image.DeleteImage(_env.WebRootPath, "assets", "images", "website-images"));

            _context.ProductImages.RemoveRange(deletedImages);


            existed.CategoryId = productVM.CategoryId.Value;
            existed.Price = productVM.Price;
            existed.Description = productVM.Description;
            existed.Name = productVM.Name;
            existed.SKU = productVM.SKU;
            
            _context.Products.Update(existed);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || id < 1) return BadRequest();

            Product product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();



            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));

        }
    }
}
