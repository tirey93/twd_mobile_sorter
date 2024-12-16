
namespace twd_to_pc_sorter.Settings
{
    public class MainSettings
    {
        public Mode Mode { get; set; }
        public string PCEngDirectory { get; set; }
        public string PCPlDirectory { get; set; }
        public string MobileEngDirectory { get; set; }
        public string MobilePlDirectory { get; set; }
    }

    public enum Mode
    {
        Sort
    }
}
