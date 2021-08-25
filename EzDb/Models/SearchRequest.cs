using System;

namespace MPSHouse.EzDb.Models
{
    public class SearchRequest
    {
        public string[] SearchTerms { get; set; }
        public string[] SearchFields { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public string OrderBy { get; set; }
        public bool? IncludeDeleted { get; set; }
        public bool? IncludeActive { get; set; }

        public SearchRequest() { }

        public SearchRequest(string SearchTerms, string SearchFields, int PageIndex, int PageSize, string OrderBy, bool? IncludeDeleted, bool? IncludeActive)        {
            this.SearchTerms = string.IsNullOrEmpty(SearchTerms) ? (string[])null : SearchTerms.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            this.SearchFields = string.IsNullOrEmpty(SearchFields) ? (string[])null : SearchFields.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            this.PageIndex = PageIndex;
            this.PageSize = PageSize;
            this.OrderBy = OrderBy;
            this.IncludeDeleted = IncludeDeleted;
            this.IncludeActive = IncludeActive;
        }
    }
}