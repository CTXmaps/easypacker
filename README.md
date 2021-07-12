# easypacker
simple tool to auto-pack custom content into .bsp using bspzip.exe
works directly from Hammer Editor

usage:

1. Put easypacker.exe into "...\steamapps\common\Counter-Strike Global Offensive\bin\"
1. Open Hammer Editor. File -> Run Map -> Expert.
2. Add "New" compile entry, put it above "Copy File".
3. Set "Command" to absolute path where you put easypacker.exe.
4. Set "Parameters" to "$path\$file $gamedir"
now you can compile your map.

additional usage:

1. Create <mapname>_content.txt file, put it right next to <mapname>.vmf
2. Put a list of files, folders, that you want easypacker to pack. 
   
   example:
   
   maps\<mapname>.nav		//add nav file
   
   sound\ctxmgs\*.mp3		//add all mp3 files from folder
   
   sound\ctxmgs?\*.mp3		//add all mp3 files from folder and all subfolders (recursive)
   
   sound\ctxmgs\*.*		//add any files from folder
   
  
