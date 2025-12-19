using AssignmentC_.Models;

namespace AssignmentC_.Models
{
    public class HomeViewModel
    {
        public List<Movie> NowShowing { get; set; } = new List<Movie>();
        public List<Movie> ComingSoon { get; set; } = new List<Movie>();
    }
}