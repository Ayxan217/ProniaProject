using ProniaProject.Models;

namespace ProniaProject.ViewModels
{
    public class ShopVM
    {
        public List<GetProductVM> Products { get; set; }
        public List<GetCategoryVM> Categories { get; set; }
        public string SearchValue { get; set; }
        public int? CategoryId { get; set; }
        public int Key {  get; set; }
        public double TotalPage { get; set; }
        public int CurrentPage { get; set; }

    }
}
