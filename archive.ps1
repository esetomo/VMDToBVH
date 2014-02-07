[System.Reflection.Assembly]::LoadWithPartialName("System.IO.Compression.FileSystem")

$dir = $(Get-Location).ToString()
$filename = $dir + "\VMDToBVH.zip"

if([System.IO.File]::Exists($filename)){
    [System.IO.File]::Delete($filename)
}

$archive = [System.IO.Compression.ZipFile]::Open($filename, [System.IO.Compression.ZipArchiveMode]::Create)
foreach($file in [System.IO.Directory]::GetFiles($dir + "\bin\Release", "*.*", [System.IO.SearchOption]::AllDirectories))
{
    if([System.IO.Path]::GetExtension($file) -eq ".pdb"){
        continue
    }
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file, $file.Replace($dir + "\bin\Release\", ""))
}
$archive.Dispose()



