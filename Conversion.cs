using ImageMagick;
using System.Text;
using System.Text.Json;
using BrailleMaker.src;
using BrailleMaker;

namespace BrailleMaker.Controls
{
    internal class Conversion
    {
        public static Dictionary<string, int[][][]> characters = new();

        private const int symbolHorizontalSpacing = 50;
        private const int symbolVerticalSpacing = 70;
        private const int symbolHeight = 55;
        private const int symbolWidth = 35;
        private const int dotSpacing = 10;
        private const int pngFactorHorizontalPosition = 19;
        private const int svgFactorHorizontalPosition = 25;


        /// <summary>
        /// Loads the characters from the 'char.json' file into the variable in memory.
        /// </summary>
        /// <param name="characters">Dictionary where the Braille information of each character will be stored.</param>
        public static void InitializeCharacters(ref Dictionary<string, int[][][]> characters)
        {
            string json = File.ReadAllText(Path.Combine(Program.DATA, "Resources", "char.json"));
            JsonElement root = JsonDocument.Parse(json).RootElement;
            characters = JsonSerializer.Deserialize<Dictionary<string, int[][][]>>(root) ?? new Dictionary<string, int[][][]>();
        }

        /// <summary>
        /// Counts how many symbols are needed to display that character in Braille.
        /// </summary>
        /// <param name="ch">Character that will be searched in the dictionary.</param>
        /// <returns>Number of symbols needed to display the character in Braille.</returns>
        

        /// <summary>
        /// Converts a character into a Braille image.
        /// </summary>
        /// <param name="key">Character that will be converted.</param>
        /// <returns>Returns a <see cref="MagickImage"/> object containing the graphical representation of the character.</returns>
        /// <exception cref="ArgumentException">Thrown if the given character is not present in the character map and is not a space.</exception>
        public static MagickImage ConvertCharacterToPngImage(string key)
        {
            if (!characters.TryGetValue(key, out int[][][] character) && !(key == " "))
            {
                throw new ArgumentException($"Character '{key}' not found in character map.", nameof(key));
            }
            MagickImage charImage = new MagickImage(MagickColors.Transparent, symbolWidth, symbolHeight);

            if (key == " ")
            {
                return charImage;
            }

            MagickImage onImage = new MagickImage(Path.Combine(Program.DATA, "Resources", "assets", "on.png"));
            MagickImage offImage = new MagickImage(Path.Combine(Program.DATA, "Resources", "assets", "off.png"));

            int len = character.Length;
            int x = 0;

            if (len > 0)
            {
                charImage = new MagickImage(MagickColors.Transparent, (uint)(len * symbolHorizontalSpacing), symbolHeight);
            }

            foreach (int[][] signal in character)
            {

                for (int i = 0; i < signal.Length; i++)
                {
                    for (int j = 0; j < signal[i].Length; j++)
                    {
                        charImage.Composite(signal[i][j] == 1 ? onImage : offImage, x + j * pngFactorHorizontalPosition, i * pngFactorHorizontalPosition, CompositeOperator.Over);
                    }
                }
                x += symbolHorizontalSpacing;
            }

            return charImage;
        }

        /// <summary>
        /// Converts a given text string into a composite PNG image representation using Braille character images.
        /// </summary>
        /// <param name="text">The input text string to be converted into an image. Supports newline characters ('\n') to create multiple lines.</param>
        /// <returns>
        /// A <see cref="MagickImage"/> object containing the rendered image of the entire input text composed of individual character images.
        /// </returns>
        public static MagickImage ConvertTextToPngImage(string text)
        {
            MagickImage textImage = new MagickImage(MagickColors.Transparent, 1, 1);
            MagickImage tempImage;
            int width = 1;
            int height = 1;
            int cursor = 0;

            foreach (char i in text)
            {
                if (i != '\n')
                {
                    tempImage = new MagickImage(MagickColors.Transparent, (uint)width * (uint)(symbolHorizontalSpacing * Util.GetCharacterMatrizLenght(i.ToString(), characters)), (uint)height * symbolVerticalSpacing);
                    tempImage.Composite(textImage, 0, 0, CompositeOperator.Over);
                    textImage = tempImage;
                    MagickImage charImage = ConvertCharacterToPngImage(i.ToString());
                    tempImage.Composite(charImage, cursor * symbolHorizontalSpacing, ((height - 1) * symbolVerticalSpacing), CompositeOperator.Over);
                    cursor += 1 * Util.GetCharacterMatrizLenght(i.ToString(), characters);
                    if (cursor + 1 >= width)
                    {
                        width += 1 * Util.GetCharacterMatrizLenght(i.ToString(), characters);
                    }
                }
                else
                {
                    height += 1;
                    cursor = 0;
                }
                    
            }
            
            

            
            return textImage;
        }

        /// <summary>
        /// Converts a single character into an SVG representation of its Braille dots starting at the specified coordinates.
        /// </summary>
        /// <param name="ch">The character to be converted into an SVG image.</param>
        /// <param name="startX">The starting X coordinate for the SVG dots placement.</param>
        /// <param name="startY">The starting Y coordinate for the SVG dots placement.</param>
        /// <returns>
        /// A string containing SVG markup representing the Braille dots of the character.
        /// Filled dots are drawn as circles with radius 3 and black fill; empty dots are smaller circles with radius 1 and no stroke.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the character is not found in the character map and is not a space.
        /// </exception>
        public static string ConvertCharacterToSvgImage(string ch, int startX, int startY)
        {
            var svgParts = new StringBuilder();
            if (!characters.TryGetValue(ch, out int[][][] charData) && !(ch == " "))
            {
                throw new ArgumentException($"Character '{ch}' not found in character map.");
            }
            foreach (var matrix in charData)
            {
                for (int row = 0; row < 3; row++)
                {
                    for (int col = 0; col < 2; col++)
                    {
                        int x = startX + (col * dotSpacing);
                        int y = startY + (row * dotSpacing) + dotSpacing;

                        if (matrix[row][col] == 1)
                        {
                            svgParts.AppendLine($"<circle cx=\"{x}\" cy=\"{y}\" r=\"3\" stroke-width=\"1\" stroke=\"#000000\" fill=\"#000000\" fill-opacity=\"1\"/>");
                        }
                        else
                        {
                            svgParts.AppendLine($"<circle cx=\"{x}\" cy=\"{y}\" r=\"1\" stroke-width=\"1\" stroke=\"none\" fill=\"#000000\" fill-opacity=\"1\"/>");
                        }
                    }
                }
                startX += 30;
            }
            return svgParts.ToString();
        }

        /// <summary>
        /// Converts a multi-line text string into an SVG image representation composed of Braille character patterns.
        /// </summary>
        /// <param name="text">The input text to convert, supporting newline characters ('\n') to create multiple lines.</param>
        /// <returns>
        /// A string containing the full SVG markup representing the entire input text as Braille dot patterns.
        /// </returns>
        public static string ConvertTextToSvgImage(string text)
        {
            int line = 0;
            int cx = dotSpacing;
            int cy = 0;
            int height = 1;
            int currentWidth = 0;
            int maxWidth = 0;

            List<int> lineWidths = new List<int>();

            for (int i = 0; i < text.Length; i++)
            {
                string ch = text[i].ToString();
                int chLen = Util.GetCharacterMatrizLenght(ch, characters);
                if (ch == "\n")
                {
                    height++;
                    lineWidths.Add(currentWidth);
                    currentWidth = 0;
                }
                else if (ch != " ")
                {
                    currentWidth += (chLen * symbolWidth) + dotSpacing;
                }
                else
                {
                    currentWidth += chLen * dotSpacing;
                }
            }
            lineWidths.Add(currentWidth);

            foreach (var w in lineWidths)
                if (w > maxWidth) maxWidth = w;

            // Construção do SVG
            var svgParts = new StringBuilder();
            
            svgParts.AppendLine("<?xml version=\"1.0\" standalone=\"no\"?>");
            svgParts.AppendLine("<!DOCTYPE svg PUBLIC \"-//W3C//DTD SVG 20010904//EN\" \"http://www.w3.org/TR/2001/REC-SVG-20010904/DTD/svg10.dtd\">");
            svgParts.AppendLine($"<svg width=\"{maxWidth}px\" height=\"{height * symbolHorizontalSpacing}px\" xmlns=\"http://www.w3.org/2000/svg\">");
            svgParts.AppendLine("<desc>SVG Output</desc>");


            for (int i = 0; i < text.Length; i++)
            {
                string ch = text[i].ToString();
                int chLen = Util.GetCharacterMatrizLenght(ch, characters);
                if (ch == "\n")
                {
                    line++;
                    cx = dotSpacing;
                    continue;
                }

                if (ch != " ")
                {
                    cy = symbolHorizontalSpacing * line;
                    svgParts.Append(ConvertCharacterToSvgImage(ch, cx, cy));

                    cx += (chLen * svgFactorHorizontalPosition) + dotSpacing;
                }
                else
                {
                    cx += dotSpacing;
                }
            }


            string SvgFooter = "\n</svg>";
            svgParts.AppendLine(SvgFooter);

            return svgParts.ToString();
        }
    }
}
