﻿using System;
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
            for (var i = 0; i < text.Length; ++i)
            {
                if (!Char.IsLetter(text[i])) continue;
                var start = i++;
                while (i < text.Length &&
                       (Char.IsLetterOrDigit(text[i]) ||
                        text[i] == '-'))
                {
                    i++;
                }

                yield return text.Substring(start, i - start);
            }
        }
    }
}