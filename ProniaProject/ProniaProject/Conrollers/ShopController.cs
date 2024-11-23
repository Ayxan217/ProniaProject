﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProniaProject.DAL;
using ProniaProject.Models;
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
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Details(int? id)
        {
            if (id == null || id < 1) return BadRequest();

            Product? product = _context.Products.
                Include(p=>p.Images.OrderByDescending(pi=>pi.IsPrime))
                .Include(p=>p.Category).FirstOrDefault(p => p.Id == id);
            if (product == null) return NotFound();

            DetailVM detailVM = new DetailVM
            {
                Product = product,
                RelatedProducts = _context.Products.Where(p=> p.CategoryId == product.CategoryId && p.Id != id).Include(p=>p.Images.Where(pi=>pi.IsPrime != null))
                .ToList(),

            };
            return View(detailVM);

        }
    }
}
