using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;

namespace ColorBot
{
	public static class ColorAnalyzer
	{
		public const double MinimumDarkContrast = 3.0;
		public const double MinimumLightContrast = 1.3;

		public static readonly Color LightBackground = FromHex("#ffffff");
		public static readonly Color DarkBackground = FromHex("#2c2f33");

		public static bool IsValid(string code)
		{
			try
			{
				FromHex(code);
				return true;
			}
			catch
			{
				return false;
			}
		}

		public static Color FromHex(string hex)
		{
			hex = hex.Replace("#", "");

			//standard #RRGGBB web format
			if(hex.Length == 6)
			{
				hex = $"FF{hex}";
			}
			//ARGB format
			else if(hex.Length == 8)
			{
				char[] array = hex.ToCharArray();
				array[0] = 'F';
				array[1] = 'F';
				hex = new string(array);
			}
			else
			{
				throw new ArgumentException($"Input string '{hex}' cannot be parsed as a hex value!");
			}

			int argb = Int32.Parse(hex, NumberStyles.HexNumber);
			return Color.FromArgb(argb);
		}



		public static double ContrastValue(this Color c)
		{
			return ((299F * c.R) + (587F * c.G) + (114F * c.B)) / 1000F;
		}

		//https://www.w3.org/TR/2008/REC-WCAG20-20081211/#relativeluminancedef
		public static double LuminanceValue(this Color c)
		{
			double R = Luminance(c.R);
			double G = Luminance(c.G);
			double B = Luminance(c.B);

			double L = (0.2126 * R) + (0.7152 * G) + (0.0722 * B);

			return L;
		}

		//if RsRGB <= 0.03928 then R = RsRGB/12.92 else R = ((RsRGB+0.055)/1.055) ^ 2.4
		public static double Luminance(byte value)
		{
			double ratioValue = value / 255F;
			if (ratioValue <= 0.03928)
			{
				return ratioValue / 12.92;
			}

			return Math.Pow(((0.055 + ratioValue) / 1.055), 2.4);
		}

		////https://www.w3.org/TR/2008/REC-WCAG20-20081211/#contrast-ratiodef
		public static double LuminanceRatio(Color c1, Color c2)
		{
			//(L1 + 0.05) / (L2 + 0.05)

			Color L1, L2;

			if (c1.GetBrightness() > c2.GetBrightness())
			{
				L1 = c1;
				L2 = c2;
			}
			else
			{
				L1 = c2;
				L2 = c1;
			}

			double ratio = (L1.LuminanceValue() + 0.05) / (L2.LuminanceValue() + 0.05);
			return Math.Min(ratio, 21);

		}

		public static double RatioBetween(this Color c1, Color c2)
		{
			return LuminanceRatio(c1, c2);
		}

		public static AnalysisResult AnalyzeColor(Color c1)
		{
			return new AnalysisResult()
			{
				TestedColor = c1,
				LightRatio = c1.RatioBetween(LightBackground),
				DarkRatio = c1.RatioBetween(DarkBackground),
				Passes = c1.RatioBetween(LightBackground) > MinimumLightContrast && c1.RatioBetween(DarkBackground) > MinimumDarkContrast
			};
		}
	}

	public struct AnalysisResult
	{
		public bool Passes;
		public Color TestedColor;
		public double LightRatio;
		public double DarkRatio;
	}
}
