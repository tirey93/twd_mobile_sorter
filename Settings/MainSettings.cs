
namespace twd_to_mobile_sorter.Settings
{
    public class MainSettings
    {
        public Mode Mode { get; set; }
        public string MobileEngFileLocation { get; set; }
        public string MobilePlFileLocation { get; set; }
        public string PCEngFileLocation { get; set; }
        public string PCPlFileLocation { get; set; }
    }

    public enum Mode
    {
        Sort
    }
}
