using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProniaProject.DAL;
using ProniaProject.Models;
using ProniaProject.Utils.Enums;
using ProniaProject.Utils.Exceptions;
using ProniaProject.ViewModels;
namespace ProniaProject.Conrollers
{
    public class ShopController : Controller
    {
        private readonly AppDbContext _context; 
        public ShopController(AppDbContext context)
        {
            _context = context;
        }

        
        public async Task<IActionResult> Index(string? search, int? categoryId, int key=1, int page = 1)
        {
            IQueryable<Product> query = _context.Products.Include(p=>p.Images.Where(pi=>pi.IsPrime!=null));

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(q => q.Name.ToLower().Contains(search.ToLower()));
                
            }

            if(categoryId != null && categoryId > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId);
            }

            switch (key)
            {
                case (int)SortType.Name:
                    query = query.OrderBy(p => p.Name);
                    break;
                case (int)SortType.Price:
                    query = query.OrderByDescending(p => p.Price);
                    break;
                case (int)SortType.Date:
                    query = query.OrderByDescending(p => p.CreatedAt);
                    break;
            }

            int count = query.Count();
            double totalPage = Math.Ceiling((double)count / 3);
            query = query.Skip((page-1)*3).Take(3);

            ShopVM shopVM = new ShopVM()
            {
                Products = await query.Select(p => new GetProductVM
                {
                    Id = p.Id,
                    Name = p.Name,
                    Image = p.Images.FirstOrDefault(pi => pi.IsPrime == true).Image,
                    SecondaryImage = p.Images.FirstOrDefault(pi => pi.IsPrime == false).Image,
                    Price = p.Price,
                }).ToListAsync(),
                Categories = await _context.Categories.Select(c => new GetCategoryVM
                {
                    Id = c.Id,
                    Name = c.Name,
                    Count = c.Products.Count
                }).ToListAsync(),
                SearchValue = search,
                CategoryId = categoryId,
                Key = key,
                TotalPage = totalPage,
                CurrentPage = page
            };

            return View(shopVM);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || id < 1) throw new BadRequestException();

            Product? product = await _context.Products.
                Include(p=>p.Images.OrderByDescending(pi=>pi.IsPrime)).Include(p=>p.ProductSizes).ThenInclude(ps=>ps.Size)
                .Include(p=>p.Category).Include(p => p.ColorProducts).ThenInclude(p=>p.Color).Include(p=>p.ProductTags).
                ThenInclude(pt=>pt.Tag).FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) throw new NotFoundException();

            DetailVM detailVM = new DetailVM
            {
                Product = product,
                RelatedProducts = await _context.Products.Where(p=> p.CategoryId == product.CategoryId && p.Id != id).Include(p=>p.Images.Where(pi=>pi.IsPrime != null))
                .ToListAsync(),

            };
            return View(detailVM);

        }
    }
}
