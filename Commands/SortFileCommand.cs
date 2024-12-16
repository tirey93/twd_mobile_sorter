
using Microsoft.Extensions.Options;
using System.Text;
using twd_to_pc_so.Utils;
using twd_to_pc_sorter.Dtos;
using twd_to_pc_sorter.Settings;

namespace twd_to_pc_sorter.Commands
{
    public class SortFileCommand
    {
        private Dictionary<int, Line> _dictEngPC;
        private int _maxLineNumberPC;
        private List<Line> _linesEngMobile;
        private Dictionary<int, Line> _dictPl;
        private readonly string _mobilePlPath;
        private readonly int _reverseSearch;

        public bool HasErrors { get; set; }

        public SortFileCommand(string pcPlFile, string pcEngFile, string mobileEngFile, string mobilePlPath, int reverseSearch) 
        {
            var errors = string.Empty;
            if (!File.Exists(pcPlFile))
                errors += "Error: PCPlFile was not found in given path\n";
            if (!File.Exists(pcEngFile))
                errors += "Error: PCEngFile was not found in given path\n";
            if (!File.Exists(mobileEngFile))
                errors += "Error: MobileEngFile was not found in given path\n";
            if (!string.IsNullOrEmpty(errors))
            {
                Console.WriteLine(errors);
                HasErrors = true;
                return;
            }

            if (pcPlFile.Contains("env_dairybarninterior_escape_english"))
            {

            }
            _dictEngPC = LineUtils.LoadLines(pcEngFile)
                .ToDictionary(x => x.Number, y => y);
            _maxLineNumberPC = _dictEngPC.Keys.Max();
            _linesEngMobile = LineUtils.LoadLines(mobileEngFile)
                .OrderBy(x => x.Number).ToList();
            _dictPl = LineUtils.LoadLines(pcPlFile)
                .ToDictionary(x => x.Number, y => y);
            _mobilePlPath = mobilePlPath;
            _reverseSearch = reverseSearch;
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
            File.WriteAllText(_mobilePlPath, mobileResult.ToString());
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
            var result = shift - _reverseSearch;
            for (int i = 0; i < _maxLineNumberPC - mobileLine.Number + shift + _reverseSearch; i++)
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
