﻿using System.ComponentModel.DataAnnotations;

namespace ProniaProject.Models
{
    public class Category : BaseEntity
    {
        
      
        public int Id { get; set; }
        public string Name { get; set; }
        public List<Product>? Products { get; set; }
    }
}

