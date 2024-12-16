namespace twd_to_pc_sorter.Dtos
{
    public class Line
    {
        public int Number { get; set; }
        public string Author { get; set; }
        public string Content { get; set; }

        public Line(string line)
        {
            var splitted = line.Split(")");
            if (splitted.Length < 2)
                return;
            Number = int.Parse(splitted[0]);

            var author = splitted[1].Replace(" ", "");
            if (author.Length > 0)
                Author = author;
        }

        public void AddContent(string content)
        {
            if (string.IsNullOrEmpty(Content))
            {
                Content = content;
            }
            else
            {
                Content += "\n" + content;
            }
        }

        public override string ToString()
        {
            return $"{Number} {Content}";
        }
    }
}
