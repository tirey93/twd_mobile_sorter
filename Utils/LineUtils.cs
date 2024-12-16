using twd_to_pc_sorter.Dtos;
using System.Text;
using System.Text.RegularExpressions;

namespace twd_to_pc_so  .Utils
{
    public class LineUtils
    {
        public static List<Line> LoadLines(string fileLocation)
        {
            var lines = File.ReadAllLines(fileLocation, Encoding.GetEncoding("windows-1250"));
            var regex = new Regex("\\d+\\)");

            var matchedLines = new List<Line>();

            var currentLine = new Line(lines[0]);
            for (int i = 1; i < lines.Length; i++)
            {
                var strLine = lines[i];

                if (regex.Match(strLine).Success)
                {
                    matchedLines.Add(currentLine);
                    currentLine = new Line(strLine);
                }
                else
                {
                    currentLine.AddContent(strLine);
                }
            }

            matchedLines.Add(currentLine);
            return matchedLines;
        }
    }
}
