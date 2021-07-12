using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace easypacker
{
	public static class Util
	{
		public static List<string> GetEntries( ref string line, string name )
		{
			List<string> list = new List<string>();
			using ( StringReader sr = new StringReader( line ) )
			{
				string tmp;
				while ( ( tmp = sr.ReadLine() ) != null ) 
				{					
					list.Add( GetEntry( ref tmp, name ) );			
				}
			}	
			return list;
		}

		public static string GetEntry( ref string line, string name )
		{
			string[] arr = Regex.Replace( line, @"\u0022+", "" ).Split( null );
			for ( int i = 0; i < arr.Length - 1; i++ )
			{
				if ( arr[i].Equals( name, StringComparison.OrdinalIgnoreCase ) )
				{
					return arr[ i + 1 ];
				}
			}

			return string.Empty;						
		}
	}
}
