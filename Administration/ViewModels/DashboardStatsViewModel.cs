namespace Administration.ViewModels
{
    public class DashboardStatsViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalRH { get; set; }
        public int TotalDirecteurs { get; set; }
        public int TotalOffres { get; set; }
        public int TotalCvs { get; set; }
<<<<<<< HEAD
       

        // Dynamic Chart Data
        public List<int> OffersPerMonth { get; set; } = new();
        public List<string> MonthLabels { get; set; } = new();

        public int PendingCvs { get; set; }
        public int AcceptedCvs { get; set; }
        public int RejectedCvs { get; set; }
=======
        public int TotalMatches { get; set; }
>>>>>>> 364b0a3c7128899c3df393e4339086a493d07ccc
    }
}