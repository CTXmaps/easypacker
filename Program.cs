using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

namespace easypacker
{
	class Program
	{
		public static readonly string[] MATERIAL_SHADERS =
		{
			"$basetexture",
			"$basetexture2",
			"$basetexture3",
			"$basetexture4",
			"$bumpmap",
			"$normalmap",
			"$detail",
			"$dudvmap",
			"$refracttinttexture",
			"$envmapmask",
			"$parallaxmap",
			"$ambientoccltexture",
			"$ambientocclusiontexture",
			"$hdrcompressedtexture",
			"$hdrbasetexture",
			"$lightwarptexture",
			"$phongwarptexture",
			"$blendmodulatetexture",
			"$refracttinttexture",
			"$phongexponenttexture",
			"$fallbackmaterial",
			"$bottommaterial",
			"$underwateroverlay",
			"$refracttexture",
			"$envmap",
			"$reflecttexture",
			"$fallbackmaterial"
		};

		public static readonly string[] SKYBOX_POSTFIXES =
		{
			"bk",
			"dn",
			"ft",
			"lf",
			"rt",
			"up"
		};

		private static List<string> content = new List<string>();
		private static string gamedir;
		private static int count = 0;
		private static long starttime = 0;

		static void Main( string[] args )
		{
			if ( args.Length != 2 )
			{
				Msg( "arguments are wrong. put \"$path\\$file $gamedir\" under \"Parameters:\"" );
				return;
			}

			gamedir = args[1] + "\\";
			string mapName = Path.GetFileNameWithoutExtension( args[0] );
			string bspFile = args[0] + ".bsp";
			string vmfFile = args[0] + ".vmf";

			if ( !File.Exists( bspFile ) )
			{
				Msg( "\"" + mapName + ".bsp\" not found" );
				return;				
			}

			if ( !File.Exists( vmfFile ) )
			{
				Msg( "\"" + mapName + ".vmf\" not found" );
				return;				
			}

			if ( !File.Exists( "bspzip.exe" ) )
			{
				Msg( "\"bspzip.exe\" not found" );
				return;
			}

			starttime = Timestamp();
			content.Clear();
			count = 0;

			string contentFile = args[0] + "_content.txt";
			if ( File.Exists( contentFile ) )
			{
				foreach ( string iter in File.ReadLines( contentFile ) )
				{
					string line = iter;
					if ( line.StartsWith( "//" ) )
						continue;

					if ( string.IsNullOrEmpty( line ) )
						continue;

					line = line.Replace( "<mapname>", mapName );

					bool recursive = false; if ( line.Contains( "?" ) ) { recursive = true; line = line.Replace( "?", "" ); };

					string[] files = Directory.GetFiles( gamedir + Path.GetDirectoryName( line ), Path.GetFileName( line ), recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly );					

					foreach ( string file in files )
						AddContentItem( file );
				}
			}
			else
			{
				Msg( "\"" + Path.GetFileNameWithoutExtension( args[0] ) + "_content.txt\" not found, skipping" );
			}

			//add nav, kv, txt
			AddContentItem( gamedir + "maps\\" + mapName + ".nav" );
			AddContentItem( gamedir + "maps\\" + mapName + ".kv" );
			AddContentItem( gamedir + "maps\\" + mapName + ".txt" );

			//add, scan soundscapes
			string tempstr;
			if ( AddContentItem( gamedir + "scripts\\soundscapes_" + mapName + ".txt" ) )
			{
				tempstr = File.ReadAllText( gamedir+ "\\scripts\\soundscapes_" + mapName + ".txt" );

				foreach ( string line in Util.GetEntries( ref tempstr, "wave" ) )
					AddContentItem( gamedir + "sound\\" + line );
			}

			//get vmf data
			string[] vmfdata = File.ReadAllLines( args[0] + ".vmf" );

			//scan vmf for skybox material
			foreach ( string line in vmfdata )
			{
				tempstr = line;
				string skyboxName = Util.GetEntry( ref tempstr, "skyname" );
				if ( !string.IsNullOrEmpty( skyboxName ) )
				{
					skyboxName = "materials\\skybox\\" + skyboxName;
					for ( int i = 0; i < SKYBOX_POSTFIXES.Length; i++ )
					{
						if ( AddContentItem( gamedir + skyboxName + SKYBOX_POSTFIXES[i] + ".vmt" ) )
						{
							//scan skybox material textures
							foreach ( string texture in GetMaterialTextures( skyboxName + SKYBOX_POSTFIXES[i] + ".vmt" ) )
								AddContentItem( gamedir + texture );
						}
					}
					break;
				}
			}

			//massive vmf scan
			foreach ( string line in vmfdata )
			{
				tempstr = line;

				//scan entity sound value by "message":
				AddContentItem( gamedir + "sound\\" + Util.GetEntry( ref tempstr, "message" ) );
				AddContentItem( gamedir + "sound\\" + Util.GetEntry( ref tempstr, "MoveSound" ) );

				//scan color_correction files and all others by "filename":
				AddContentItem( gamedir + Util.GetEntry( ref tempstr, "filename" ) );

				//scan entity model value by "model":
				string mdlName = Util.GetEntry( ref tempstr, "model" );
				if ( AddContentItem( gamedir + mdlName ) )
				{
						//scan model files:						
						string mdlNameNoExt = mdlName.Replace( Path.GetExtension( mdlName ), string.Empty );
						AddContentItem( gamedir + mdlNameNoExt + ".dx90.vtx" );
						AddContentItem( gamedir + mdlNameNoExt + ".vvd" );
						AddContentItem( gamedir + mdlNameNoExt + ".phy" );

						//scan model materails:
						foreach ( string material in GetModelMaterials( mdlName ) )
						{
							if ( AddContentItem( gamedir + material ) )
							{
								foreach ( string texture in GetMaterialTextures( material ) )
									AddContentItem( gamedir + texture );			
							}
						}
				}

				//scan brush materials
				string matName = "materials\\" + Util.GetEntry( ref tempstr, "material" ) + ".vmt";
				if ( AddContentItem( gamedir + matName ) )
				{
					foreach ( string texture in GetMaterialTextures( matName ) )
						AddContentItem( gamedir + texture );					
				}
			}

			Msg( count + " files found in " + ( Timestamp() - starttime ) + "s" );
			starttime = Timestamp();
			count = 0;

			//use bspzip to pack content
			File.WriteAllLines( "temp.txt", content );

			File.Copy( bspFile, bspFile + ".easypacker_backup", true );

			Process proc = new Process();
			proc.StartInfo.FileName = "bspzip.exe";
			proc.StartInfo.Arguments = "-addlist \"" + bspFile + "\" \"temp.txt\" \"" + bspFile + "\"";
			proc.StartInfo.UseShellExecute = false;
			proc.StartInfo.RedirectStandardOutput = true;
			proc.StartInfo.RedirectStandardError = true;
			proc.StartInfo.CreateNoWindow = true;
			proc.StartInfo.EnvironmentVariables["VPROJECT"] = gamedir;
			proc.OutputDataReceived += new DataReceivedEventHandler( OnBspzipDataRecieved );
			proc.ErrorDataReceived += new DataReceivedEventHandler( OnBspzipDataRecieved );
			proc.Start();
			proc.BeginOutputReadLine();
			proc.BeginErrorReadLine();
			proc.WaitForExit();

			File.Delete( "temp.txt" );
		}

		private static void OnBspzipDataRecieved( object proc, DataReceivedEventArgs dataObj )
		{
			string message = dataObj.Data;

			if ( string.IsNullOrEmpty( message ) )
				return;

			if ( message.Contains( "Opening" ) )
			{
				Msg( "opening bsp file" );
				return;
			}

			if ( message.Contains( "Writing" ) )
			{
				message = message.Substring( 21 );
				Msg( "writing bsp file: " + Path.GetFileName( message ) );
				return;			
			}

			if ( message.Contains( "Adding" ) )
			{
				message = message.Substring( 12 );
				count++;
				Msg( "added: " + Path.GetFileName( message ) );
				return;
			}

			if ( message.Contains( "Valve" ) )
			{
				Msg( count + " files added in " + ( Timestamp() - starttime ) + "s\n" );
				Msg( message + "\n" );
			}
		}

		private static void Msg( string msg )
		{
			Console.WriteLine( msg );
		}

		private static long Timestamp()
		{
			DateTime now = new DateTime( Stopwatch.GetTimestamp() );
			return ( ( DateTimeOffset )now ).ToUnixTimeSeconds();
		}

		private static bool AddContentItem( string item )	//item = full path to the file
		{
			if ( string.IsNullOrEmpty( item ) )
				return false;

			item = item.Replace( '/', '\\' );

			if ( item.EndsWith( "\\" ) )
				return false;

			if ( content.Contains( item ) )
				return false;			

			if ( !File.Exists( item ) )
				return false;		

			string shortname = item.Replace( gamedir, string.Empty );

			content.Add( shortname );
			content.Add( item );
			count++;

			Msg( "found: " + shortname );
			return true;
		}

		private static List<string> GetMaterialTextures( string material )
		{
			List<string> textures = new List<string>();

			string[] filedata = File.ReadAllLines( gamedir + "\\" + material );
			string entry, ext;

			for ( int i = 0; i < filedata.Length; i++ )
			{
				for ( int j = 0; j < MATERIAL_SHADERS.Length; j++ )
				{
					entry = Util.GetEntry( ref filedata[i], MATERIAL_SHADERS[j] );
					if ( !string.IsNullOrEmpty( entry ) )
					{
						ext = Path.GetExtension( entry );
						if ( !string.IsNullOrEmpty( ext ) )
						{
							entry.Replace( ext, string.Empty );
						}

						textures.Add( "materials" + "\\" + entry + ".vtf" );
					}					
				}
			}
			return textures;
		}

		private static List<string> GetModelMaterials( string modelName )	//this one is pretty bad, but works
		{	
			List<string> materials = new List<string>();

			string[] filedata = File.ReadAllLines( gamedir + "\\" + modelName );
			string last = filedata[ filedata.Length - 1 ];
			string[] arr = last.Split( '\0' );

			string tmp;
			for ( int i = 0; i < arr.Length; i++ )
			{
				if ( string.IsNullOrEmpty( arr[ i ] ) )
					continue;

				for ( int j = 0; j < arr.Length; j++ )
				{
					if ( string.IsNullOrEmpty( arr[ j ] ) )
						continue;

					tmp = "materials\\" + arr[i] + arr[j] + ".vmt";

					if ( tmp.IndexOfAny( Path.GetInvalidPathChars() ) < 0 )	
					{
						materials.Add( tmp );						
					}
				}
			}
			return materials;
		}
	}
}
