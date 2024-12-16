
using Microsoft.Extensions.Options;
using System.Text;
using twd_to_mobile_so.Utils;
using twd_to_mobile_sorter.Dtos;
using twd_to_mobile_sorter.Settings;

namespace twd_to_mobile_sorter.Commands
{
    public class SortCommand
    {
        private readonly MainSettings _settings;
        private readonly Dictionary<int, Line> _dictEngMobile;
        private readonly int _maxLineNumberMobile;
        private readonly List<Line> _linesEngPC;
        private readonly Dictionary<int, Line> _dictPl;

        public bool HasErrors { get; set; }

        public SortCommand(IOptions<MainSettings> options) 
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            _settings = options.Value;

            var errors = string.Empty;
            if (!File.Exists(_settings.MobilePlFileLocation))
                errors += "Error: MobilePlFile was not found in given path\n";
            if (!File.Exists(_settings.PCPlFileLocation))
                errors += "Error: PCPlFile was not found in given path\n";
            if (!File.Exists(_settings.MobileEngFileLocation))
                errors += "Error: MobileEngFile was not found in given path\n";
            if (!File.Exists(_settings.PCEngFileLocation))
                errors += "Error: PCEngFile was not found in given path\n";
            if (!string.IsNullOrEmpty(errors))
            {
                Console.WriteLine(errors);
                HasErrors = true;
                return;
            }

            _dictEngMobile = LineUtils.LoadLines(_settings.MobileEngFileLocation)
                .ToDictionary(x => x.Number, y => y);
            _maxLineNumberMobile = _dictEngMobile.Keys.Max();
            _linesEngPC = LineUtils.LoadLines(_settings.PCEngFileLocation)
                .OrderBy(x => x.Number).ToList();
            _dictPl = LineUtils.LoadLines(_settings.MobilePlFileLocation)
                .ToDictionary(x => x.Number, y => y);
        }

        public void Execute()
        {
            var shift = 0;
            var translations = new List<Translation>();
            foreach (var lineEngPC in _linesEngPC)
            {
                if(!_dictEngMobile.ContainsKey(lineEngPC.Number + shift))
                {
                    var newShift = FindInMobile(lineEngPC, shift, _dictEngMobile);
                    if (newShift != null)
                    {
                        translations.AddRange(AddLinesBeforeShift(shift, newShift, lineEngPC));
                        shift = newShift.Value;
                    }
                    else
                    {
                        translations.Add(new Translation
                        {
                            MobileEngLine = null,
                            PlLine = lineEngPC,
                            PCEngLine = lineEngPC
                        });
                        continue;
                    }
                }
                if (lineEngPC.Content != _dictEngMobile[lineEngPC.Number + shift].Content)
                {
                    var newShift = FindInMobile(lineEngPC, shift, _dictEngMobile);
                    if (newShift == null)
                    {
                        translations.Add(new Translation
                        {
                            MobileEngLine = null,
                            PlLine = lineEngPC,
                            PCEngLine = lineEngPC
                        });
                        continue;
                    }
                    translations.AddRange(AddLinesBeforeShift(shift, newShift, lineEngPC));
                    shift = newShift.Value;
                }
                translations.Add(new Translation
                {
                    MobileEngLine = _dictEngMobile[lineEngPC.Number + shift],
                    PlLine = _dictPl[lineEngPC.Number + shift],
                    PCEngLine = lineEngPC
                });
            }

            var poResult = new StringBuilder();
            foreach (var translation in translations)
            {
                int number = 0;
                try
                {
                    string[] splittedEng;
                    string[] splittedPl;
                    if (translation.MobileEngLine != null)
                    {
                        splittedEng = translation.MobileEngLine.Content.Split('\n');
                        number = translation.MobileEngLine.Number;
                    }
                    else
                    {
                        splittedEng = translation.PCEngLine.Content.Split('\n');
                        number = translation.PCEngLine.Number;
                    }
                    splittedPl = translation.PlLine.Content.Split('\n');


                    int i = 0;

                    foreach (var splittedLine in splittedEng)
                    {
                        var markup = SetMarkup(translation, i);

                        poResult.Append(Sort(markup, splittedEng[i], splittedPl[i]));
                        i = i + 1;
                    }
                }
                catch
                {
                    Console.WriteLine($"Exception in line: {number}");
                    throw;
                }
            }
            File.WriteAllText(_settings.MobilePlFileLocation, poResult.ToString());
        }

        private List<Translation> AddLinesBeforeShift(int shift, int? newShift, Line lineEngPC)
        {
            var result = new List<Translation>();
            for (int i = 0; i < newShift - shift; i++)
            {
                if (!_dictEngMobile.ContainsKey(lineEngPC.Number + shift + i))
                    continue;

                var mobileTranslation = new Translation
                {
                    MobileEngLine = _dictEngMobile[lineEngPC.Number + shift + i],
                    PlLine = _dictPl[lineEngPC.Number + shift + i],
                    PCEngLine = null
                };
                result.Add(mobileTranslation);
            }
            return result;
        }

        private int? FindInMobile(Line pcLine, int shift, Dictionary<int, Line> dictEngMobile)
        {
            var result = shift - 5;
            for (int i = 0; i < _maxLineNumberMobile - pcLine.Number + shift + 5; i++)
            {
                result = result + 1;
                if (!dictEngMobile.ContainsKey(pcLine.Number + result))
                    continue;
                if (dictEngMobile[pcLine.Number + result].Content == pcLine.Content)
                {
                    return result;
                }
            }

            return null;
        }
        private static string SetMarkup(Translation translation, int i)
        {
            if (translation.MobileEngLine != null && translation.PCEngLine != null)
            {
                return $"{translation.MobileEngLine.Number}_{translation.PCEngLine.Number}_{i}_{translation.MobileEngLine.Author}";
            }
            else if (translation.MobileEngLine != null && translation.PCEngLine == null)
            {
                return $"{translation.MobileEngLine.Number}__{i}_{translation.MobileEngLine.Author}";
            }
            else if (translation.MobileEngLine == null && translation.PCEngLine != null)
            {
                return $"_{translation.PCEngLine.Number}_{i}_{translation.PCEngLine.Author}";
            }
            return string.Empty;
        }

        private static string Sort(string markup, string engStr, string plStr)
        {
            var result = $"msgctxt \"{markup}\"\n";
            result += $"msgid \"{engStr}\"\n";
            result += $"msgstr \"{plStr}\"\n\n";

            return result;
        }
    }
}
