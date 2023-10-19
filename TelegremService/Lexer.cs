using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegremService
{
    public class Lexer
    {
        public IEnumerable<string> Tokenize(string text)
        {
            text = text.ToLower();
            var start = -1;
            for (var i = 0; i < text.Length; ++i)
            {
                if (Char.IsLetter(text[i]))
                {
                    start = i++;
                    while ((Char.IsLetterOrDigit(text[i]) ||
                        text[i] == '-') && i < text.Length)
                    {
                        i++;
                    }
                    yield return text.Substring(start, i - start);

                }
            }
        }
    }
}
