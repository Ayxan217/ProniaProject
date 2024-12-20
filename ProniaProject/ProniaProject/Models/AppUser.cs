﻿using Microsoft.AspNetCore.Identity;

namespace ProniaProject.Models
{
    public class AppUser : IdentityUser
    {
        public string Name { get; set; }
        public string Surname { get; set; }
        public List<BasketItem> BasketItems { get; set; }
    }
}
