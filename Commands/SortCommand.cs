using Microsoft.Extensions.Options;
using System.Text;
using twd_to_pc_sorter.Commands;
using twd_to_pc_sorter.Settings;

namespace twd_to_mobile_sorter.Commands
{
    public class SortCommand
    {
        public bool HasErrors { get; set; }

        private readonly MainSettings _settings;

        public SortCommand(IOptions<MainSettings> options)
        {
            _settings = options.Value;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var errors = string.Empty;
            if (!Directory.Exists(_settings.PCPlDirectory))
                errors += "Error: PCPlDirectory was not found in given path\n";
            if (!Directory.Exists(_settings.PCEngDirectory))
                errors += "Error: PCEngDirectory was not found in given path\n";
            if (!Directory.Exists(_settings.MobileEngDirectory))
                errors += "Error: MobileEngDirectory was not found in given path\n";
            if (!string.IsNullOrEmpty(errors))
            {
                Console.WriteLine(errors);
                HasErrors = true;
                return;
            }
        }

        public void Execute()
        {
            if (!Directory.Exists(_settings.MobilePlDirectory))
                Directory.CreateDirectory(_settings.MobilePlDirectory);
            foreach (var file in Directory.GetFiles(_settings.MobileEngDirectory))
            {
                var name = Path.GetFileName(file);
                Console.WriteLine($"Handling file: {name}");
                var command = new SortFileCommand($"{_settings.PCPlDirectory}/{name}",
                    $"{_settings.PCEngDirectory}/{name}",
                    $"{_settings.MobileEngDirectory}/{name}",
                    $"{_settings.MobilePlDirectory}/{name}");
                command.Execute();
            }
        }
    }
}
