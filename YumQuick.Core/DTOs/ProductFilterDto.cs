namespace YumQuick.Core.DTOs
{
    public class ProductFilterDto
    {
        // 1. Pagination Parameters
        private const int MaxPageSize = 50;
        public int PageNumber { get; set; } = 1;

        private int _pageSize = 10;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
        }

        // 2. Filtering Parameters
        public int? MainCategoryId { get; set; } 
        public List<int>? SubCategoryIds { get; set; }

        public double? MinRating { get; set; } 
        public decimal? MaxPrice { get; set; } 

        // 3. Sorting Parameter
        public string? SortBy { get; set; } 
    }
}