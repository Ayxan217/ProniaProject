﻿namespace ProniaProject.ViewModels
{
    public class BasketItemVM
    {
        public int Id { get; set; }
        public decimal Price { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public int Count { get; set; }
        public decimal SubTotal { get; set; }
    }
}
