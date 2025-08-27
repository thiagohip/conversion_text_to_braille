using BrailleMaker;
using BrailleMaker.src;
using ImageMagick;
using System.Text;
using System.Text.Json;
using BrailleMaker;
using BrailleMaker.src;
using static System.Net.Mime.MediaTypeNames;

namespace BrailleMaker.src
{
    internal class Conversion
    {
        public static Dictionary<string, int[][][]> characters = new();

        private static readonly double diameterSvg = 4;
        private static readonly int dotSpacingSvg = (int)Math.Floor(1.8 * diameterSvg);
        private static readonly int symbolSpacingSvg = (int)Math.Floor(6 * diameterSvg);
        private static readonly int lineSpacingSvg = (int)Math.Floor(10 * diameterSvg);

        private static readonly double diameterPng = 16;
        private static readonly int dotSpacingPng = (int)Math.Floor(1.85 * diameterPng);
        private static readonly int symbolSpacingPng = (int)Math.Floor(2.97 * diameterPng);
        private static readonly int lineSpacingPng = (int)Math.Floor(9 * diameterPng);
        private static readonly int symbolWidth = (int)diameterPng + dotSpacingPng;
        private static readonly int symbolHeight = (int)diameterPng + 2 * dotSpacingPng;



        public static void InitializeCharacters(ref Dictionary<string, int[][][]> characters)
        {
            string json = File.ReadAllText(Path.Combine(Program.DATA, "Resources", "char.json"));
            JsonElement root = JsonDocument.Parse(json).RootElement;
            characters = JsonSerializer.Deserialize<Dictionary<string, int[][][]>>(root) ?? new Dictionary<string, int[][][]>();
        }

        public static string CreateSymbolOnSvg(int[][] symbol, int cursor, int line)
        {
            var svgSymbol = new StringBuilder();

            int startX = (int)Math.Floor(diameterSvg / 2 + ((cursor - 1) * symbolSpacingSvg));
            int startY = (int)Math.Floor(diameterSvg / 2 + ((line - 1) * lineSpacingSvg));

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    int x = startX + (j * dotSpacingSvg);
                    int y = startY + (i * dotSpacingSvg);


                    if (symbol[i][j] == 1)
                    {
                        svgSymbol.AppendLine($"<circle cx=\"{x}\" cy=\"{y}\" r=\"{diameterSvg / 2}\" stroke-width=\"0\" stroke=\"none\" fill=\"#000000\" fill-opacity=\"1\"/>");
                    }
                }
            }
            return svgSymbol.ToString();
        }

        public static MagickImage CreateSymbolOnPng(int[][] symbol)
        {
            MagickImage onImage = new MagickImage(Path.Combine(Program.DATA, "Resources", "assets", "on.png"));
            MagickImage offImage = new MagickImage(Path.Combine(Program.DATA, "Resources", "assets", "off.png"));
            MagickImage charImage = new MagickImage(MagickColors.Transparent, (uint)symbolWidth, (uint)symbolHeight);

            for (int i = 0; i < symbol.Length; i++)
            {
                for (int j = 0; j < symbol[i].Length; j++)
                {
                    int x = j * dotSpacingPng;
                    int y = i * dotSpacingPng;
                    if (symbol[i][j] == 1)
                    {
                        charImage.Composite(onImage, x, y, CompositeOperator.Over);
                    }
                    else
                    {
                        charImage.Composite(offImage, x, y, CompositeOperator.Over);
                    }
                }
            }



            return charImage;
        }

        public static string ConvertCharacterToSvgImage(ref int cursor, int line, string ch = " ", bool isCap = false, bool isUpper = false)
        {
            var svgChar = new StringBuilder();
            int[][] cap = [[0, 0], [0, 0], [0, 1]];
            int[][] upper = [[0, 1], [0, 0], [0, 1]];
            if (isCap)
            {
                svgChar.AppendLine(CreateSymbolOnSvg(cap, cursor, line));
            }
            else if (isUpper)
            {
                svgChar.AppendLine(CreateSymbolOnSvg(upper, cursor, line));
            }
            else if (ch != " ")
            {
                if (!characters.TryGetValue(ch, out int[][][] charData))
                {
                    throw new ArgumentException($"Character '{ch}' not found in character map.");
                }
                foreach (int[][] symbol in charData)
                {
                    svgChar.AppendLine(CreateSymbolOnSvg(symbol, cursor, line));
                    cursor++;
                }
            }

            return svgChar.ToString();
        }

        public static MagickImage ConvertCharacterToPngImage(ref int cursor, string ch = " ", bool isCap = false, bool isUpper = false)
        {
            int len = Util.GetCharacterMatrizLenght(ch, characters);
            uint width = (uint)((symbolWidth * len) + ((len - 1) * symbolSpacingPng));
            uint height = (uint)symbolHeight;
            var pngChar = new MagickImage(MagickColors.Transparent, width, height);

            int[][] cap = [[0, 0], [0, 0], [0, 1]];
            int[][] upper = [[0, 1], [0, 0], [0, 1]];
            if (isCap || isUpper)
            {
                cursor++;
                var image = isCap ? CreateSymbolOnPng(cap) : CreateSymbolOnPng(upper);
                return image;
            }

            characters.TryGetValue(ch, out int[][][] charData);

            for (int i = 0; i < charData.Length; i++)
            {
                int x = (i * symbolWidth) + (i * symbolSpacingPng);
                int y = 0;

                pngChar.Composite(CreateSymbolOnPng(charData[i]), x, y, CompositeOperator.Over);
                cursor++;
            }
            return pngChar;
        }

        public static MagickImage ConvertTextToPngImage(string text)
        {

            int line, cursor, columns, rows;
            columns = 0;
            rows = 1;
            cursor = line = 1;


            MagickImage textImage;
            string[] words = text.Replace("\n", " \n ").Split(' ');

            foreach (string word in words)
            {
                if (word[0] == '\n')
                {
                    rows++;
                }
                else
                {
                    bool isCapitalize = Util.isCapitalize(word);
                    bool isUpper = Util.isUpper(word);
                    string word_aux = word.ToLower();
                    if (isCapitalize || isUpper)
                    {
                        columns++;
                    }
                    foreach (char ch in word_aux)
                    {
                        columns += Util.GetCharacterMatrizLenght(ch.ToString(), characters);
                    }
                    columns++;
                }
                if (!(word == words.LastOrDefault()))
                {
                    columns++;
                }
                
            }

            uint width = (uint)((columns * symbolWidth) + ((columns - 1) * symbolSpacingPng));
            uint height = (uint)((rows * symbolHeight) + ((rows - 1) * lineSpacingPng));

            textImage = new MagickImage(MagickColors.Transparent, width, height);

            foreach (string word in words)
            {
                if (word == "\n")
                {
                    cursor = 1;
                    line++;
                }
                else
                {
                    bool isCapitalize = Util.isCapitalize(word);
                    bool isUpper = Util.isUpper(word);
                    string word_aux = word.ToLower();
                    if (isCapitalize || isUpper)
                    {
                        int x = ((cursor - 1) * symbolWidth) + (cursor - 1 >= 0 ? cursor - 1 : 0) * symbolSpacingPng;
                        int y = ((line - 1) * symbolHeight) + (line - 1 >= 0 ? line - 1 : 0) * symbolHeight;
                        var charImage = ConvertCharacterToPngImage(ref cursor, isCap: isCapitalize, isUpper: isUpper);
                        textImage.Composite(charImage, x, y, CompositeOperator.Over);
                    }
                    foreach (char ch in word_aux)
                    {
                        int x = ((cursor - 1) * symbolWidth) + (cursor - 1 >= 0 ? cursor - 1 : 0) * symbolSpacingPng;
                        int y = ((line - 1) * symbolHeight) + (line - 1 >= 0 ? line - 1 : 0) * symbolHeight;
                        var charImage = ConvertCharacterToPngImage(ref cursor, ch.ToString());
                        textImage.Composite(charImage, x, y, CompositeOperator.Over);

                    }
                    cursor++;
                }  
            }
            return textImage;

        }

        public static string ConvertTextToSvgImage(string text)
        {
            int line, cursor, width;
            line = cursor = 1;
            width = 0;

            StringBuilder svgParts = new StringBuilder();

            string[] words = text.Replace("\n", " \n ").Split(' ');

            foreach (string word in words)
            {
                if (word[0] == '\n')
                {
                    cursor = 1;
                    line++;
                }
                else
                {
                    bool isCapitalize = Util.isCapitalize(word);
                    bool isUpper = Util.isUpper(word);
                    string word_aux = word.ToLower();
                    if (isCapitalize || isUpper)
                    {
                        svgParts.AppendLine(ConvertCharacterToSvgImage(cursor: ref cursor, line: line, isCap: isCapitalize, isUpper: isUpper));
                        cursor++;
                        width = cursor > width ? cursor : width;
                    }
                    foreach (char ch in word_aux)
                    {
                        svgParts.AppendLine(ConvertCharacterToSvgImage(ref cursor, line, ch.ToString()));
                        width = cursor > width ? cursor : width;
                        cursor++;
                    }
                }
                
            }

            var svgText = new StringBuilder();

            svgText.AppendLine("<?xml version=\"1.0\" standalone=\"no\"?>");
            svgText.AppendLine("<!DOCTYPE svg PUBLIC \"-//W3C//DTD SVG 20010904//EN\" \"http://www.w3.org/TR/2001/REC-SVG-20010904/DTD/svg10.dtd\">");
            svgText.AppendLine($"<svg width=\"{width * symbolSpacingSvg}px\" height=\"{line * lineSpacingSvg}px\" xmlns=\"http://www.w3.org/2000/svg\">");
            svgText.AppendLine("<desc>SVG Output</desc>");

            svgText.AppendLine(svgParts.ToString());

            svgText.AppendLine("</svg>");

            return svgText.ToString();
        }
    }
}
