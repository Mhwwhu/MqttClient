using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CmdLine
{
	public enum OutputForm
	{
		TEXT,
		HEX,
		BIN
	}
	public class OutputFormConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
		{
			return sourceType == typeof(string);
		}
		public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
		{
			if(value is string str)
			{
				return str.ToLower() switch
				{
					"text" => OutputForm.TEXT,
					"hex" => OutputForm.HEX,
					"bin" => OutputForm.BIN,
					_ => null
				};
			}
			return null;
		}
	}
}
