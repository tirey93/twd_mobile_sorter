
using Microsoft.Extensions.Options;
using System.Text;
using twd_to_pc_so.Utils;
using twd_to_pc_sorter.Dtos;
using twd_to_pc_sorter.Settings;

namespace twd_to_pc_sorter.Commands
{
    public class SortCommand
    {
        private readonly MainSettings _settings;
        private readonly Dictionary<int, Line> _dictEngPC;
        private readonly int _maxLineNumberPC;
        private readonly List<Line> _linesEngMobile;
        private readonly Dictionary<int, Line> _dictPl;

        public bool HasErrors { get; set; }

        public SortCommand(IOptions<MainSettings> options) 
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            _settings = options.Value;

            var errors = string.Empty;
            if (!File.Exists(_settings.PCPlFileLocation))
                errors += "Error: PCPlFile was not found in given path\n";
            if (!File.Exists(_settings.PCEngFileLocation))
                errors += "Error: PCEngFile was not found in given path\n";
            if (!File.Exists(_settings.MobileEngFileLocation))
                errors += "Error: MobileEngFile was not found in given path\n";
            if (!string.IsNullOrEmpty(errors))
            {
                Console.WriteLine(errors);
                HasErrors = true;
                return;
            }

            _dictEngPC = LineUtils.LoadLines(_settings.PCEngFileLocation)
                .ToDictionary(x => x.Number, y => y);
            _maxLineNumberPC = _dictEngPC.Keys.Max();
            _linesEngMobile = LineUtils.LoadLines(_settings.MobileEngFileLocation)
                .OrderBy(x => x.Number).ToList();
            _dictPl = LineUtils.LoadLines(_settings.PCPlFileLocation)
                .ToDictionary(x => x.Number, y => y);
        }

        public void Execute()
        {
            var shift = 0;
            var translations = new List<Translation>();
            foreach (var lineEngMobile in _linesEngMobile)
            {
                if(!_dictEngPC.ContainsKey(lineEngMobile.Number + shift))
                {   
                    var newShift = FindInPC(lineEngMobile, shift, _dictEngPC);
                    if (newShift != null)
                    {
                        translations.AddRange(AddLinesBeforeShift(shift, newShift, lineEngMobile));
                        shift = newShift.Value;
                    }
                    else
                    {
                        translations.Add(new Translation
                        {
                            PCEngLine = null,
                            PlLine = lineEngMobile,
                            MobileEngLine = lineEngMobile
                        });
                        continue;
                    }
                }
                if (lineEngMobile.Content != _dictEngPC[lineEngMobile.Number + shift].Content)
                {
                    var newShift = FindInPC(lineEngMobile, shift, _dictEngPC);
                    if (newShift == null)
                    {
                        translations.Add(new Translation
                        {
                            PCEngLine = null,
                            PlLine = lineEngMobile,
                            MobileEngLine = lineEngMobile
                        });
                        continue;
                    }
                    translations.AddRange(AddLinesBeforeShift(shift, newShift, lineEngMobile));
                    shift = newShift.Value;
                }
                translations.Add(new Translation
                {
                    PCEngLine = _dictEngPC[lineEngMobile.Number + shift],
                    PlLine = _dictPl[lineEngMobile.Number + shift],
                    MobileEngLine = lineEngMobile
                });
            }

            var mobileResult = new StringBuilder();
            foreach (var translation in translations)
            {
                int number = 0;
                try
                {
                    var toAppendMobilePl = $"{translation.PlLine.Number}) {translation.PlLine.Author ?? ""}\n{translation.PlLine.Content}\n";

                    mobileResult.Append(toAppendMobilePl);
                }
                catch
                {
                    Console.WriteLine($"Exception in line: {number}");
                    throw;
                }
            }
            File.WriteAllText(_settings.MobilePlFileLocation, mobileResult.ToString());
        }

        private List<Translation> AddLinesBeforeShift(int shift, int? newShift, Line lineEngMobile)
        {
            var result = new List<Translation>();
            for (int i = 0; i < newShift - shift; i++)
            {
                if (!_dictEngPC.ContainsKey(lineEngMobile.Number + shift + i))
                    continue;

                var pcTranslation = new Translation
                {
                    PCEngLine = _dictEngPC[lineEngMobile.Number + shift + i],
                    PlLine = _dictPl[lineEngMobile.Number + shift + i],
                    MobileEngLine = null
                };
                result.Add(pcTranslation);
            }
            return result;
        }

        private int? FindInPC(Line mobileLine, int shift, Dictionary<int, Line> dictEngPC)
        {
            var result = shift - 25;
            for (int i = 0; i < _maxLineNumberPC - mobileLine.Number + shift + 5; i++)
            {
                result = result + 1;
                if (!dictEngPC.ContainsKey(mobileLine.Number + result))
                    continue;
                if (dictEngPC[mobileLine.Number + result].Content == mobileLine.Content)
                {
                    return result;
                }
            }

            return null;
        }
        private static string SetMarkup(Translation translation, int i)
        {
            if (translation.PCEngLine != null && translation.MobileEngLine != null)
            {
                return $"{translation.PCEngLine.Number}_{translation.MobileEngLine.Number}_{i}_{translation.PCEngLine.Author}";
            }
            else if (translation.PCEngLine != null && translation.MobileEngLine == null)
            {
                return $"{translation.PCEngLine.Number}__{i}_{translation.PCEngLine.Author}";
            }
            else if (translation.PCEngLine == null && translation.MobileEngLine != null)
            {
                return $"_{translation.MobileEngLine.Number}_{i}_{translation.MobileEngLine.Author}";
            }
            return string.Empty;
        }
    }
}
