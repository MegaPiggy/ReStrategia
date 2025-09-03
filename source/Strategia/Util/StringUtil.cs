using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSP;

namespace Strategia
{
    public static class StringUtil
    {
        static char[] vowels = { 'a', 'e', 'i', 'o', 'u' };

        public static string IntegerToRoman(int num)
        {
            var number = Math.Abs(num);

            if (number > 3999) throw new ArgumentOutOfRangeException(nameof(number), "insert value between 1 and 3999");

            var romanNumeral = string.Empty;

            if (number >= 1000) romanNumeral = "M" + IntegerToRoman(number - 1000);
            else if (number >= 900) romanNumeral = "CM" + IntegerToRoman(number - 900);
            else if (number >= 500) romanNumeral = "D" + IntegerToRoman(number - 500);
            else if (number >= 400) romanNumeral = "CD" + IntegerToRoman(number - 400);
            else if (number >= 100) romanNumeral = "C" + IntegerToRoman(number - 100);
            else if (number >= 90) romanNumeral = "XC" + IntegerToRoman(number - 90);
            else if (number >= 50) romanNumeral = "L" + IntegerToRoman(number - 50);
            else if (number >= 40) romanNumeral = "XL" + IntegerToRoman(number - 40);
            else if (number >= 10) romanNumeral = "X" + IntegerToRoman(number - 10);
            else if (number >= 9) romanNumeral = "IX" + IntegerToRoman(number - 9);
            else if (number >= 5) romanNumeral = "V" + IntegerToRoman(number - 5);
            else if (number >= 4) romanNumeral = "IV" + IntegerToRoman(number - 4);
            else if (number >= 1) romanNumeral = "I" + IntegerToRoman(number - 1);

            if (num < 0) romanNumeral = "-" + romanNumeral;
            return romanNumeral;
        }

        public static string ATrait(string trait)
        {
            return (vowels.Contains(trait.ToLower().First()) ? "an " : "a ") + trait.ToLower();
        }
    }
}
