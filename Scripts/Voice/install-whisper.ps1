param(
    [string]$Python = "",
    [string]$Model = "small"
)

$ErrorActionPreference = "Stop"

$pythonCommand = $Python
if ([string]::IsNullOrWhiteSpace($pythonCommand)) {
    $pythonCommand = (Get-Command python -ErrorAction SilentlyContinue)?.Source
}
if ([string]::IsNullOrWhiteSpace($pythonCommand)) {
    $pythonCommand = (Get-Command py -ErrorAction SilentlyContinue)?.Source
}
if ([string]::IsNullOrWhiteSpace($pythonCommand)) {
    throw "Python bulunamadı. Önce Python 3.12+ kurun veya -Python parametresine python.exe yolunu verin. Örnek: -Python `"C:\Program Files\Python312\python.exe`""
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptDir "../..")
$venvPath = Join-Path $repoRoot ".venv"
$cachePath = Join-Path $repoRoot ".whisper-cache"
$pythonExe = Join-Path $venvPath "Scripts/python.exe"

Write-Host "Using Python command: $pythonCommand"
Write-Host "Creating Python virtual environment at $venvPath"
& $pythonCommand -m venv $venvPath

New-Item -ItemType Directory -Force -Path $cachePath | Out-Null
$env:HF_HOME = $cachePath
$env:HF_HUB_DISABLE_SYMLINKS_WARNING = "1"

Write-Host "Installing faster-whisper dependencies"
& $pythonExe -m pip install --upgrade pip
& $pythonExe -m pip install -r (Join-Path $scriptDir "requirements.txt")

Write-Host "Downloading and warming up Whisper model: $Model"
& $pythonExe (Join-Path $scriptDir "prewarm_whisper.py") --model $Model

Write-Host "Granting IIS read/write access to Whisper runtime folders"
icacls $venvPath /grant "IIS_IUSRS:(OI)(CI)M" /T | Out-Null
icacls $cachePath /grant "IIS_IUSRS:(OI)(CI)M" /T | Out-Null

Write-Host ""
Write-Host "Use these appsettings values on the API server:"
Write-Host "Voice:TranscriptionEnabled=true"
Write-Host "Voice:TranscriptionExecutablePath=$pythonExe"
Write-Host "Voice:TranscriptionArgumentsTemplate=`"Scripts/Voice/transcribe_whisper.py`" --language {language} --file `"{input}`" --model $Model"
Write-Host "Whisper cache: $cachePath"
