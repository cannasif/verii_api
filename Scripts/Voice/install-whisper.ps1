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
$pythonExe = Join-Path $venvPath "Scripts/python.exe"

Write-Host "Using Python command: $pythonCommand"
Write-Host "Creating Python virtual environment at $venvPath"
& $pythonCommand -m venv $venvPath

Write-Host "Installing faster-whisper dependencies"
& $pythonExe -m pip install --upgrade pip
& $pythonExe -m pip install -r (Join-Path $scriptDir "requirements.txt")

Write-Host "Warming up Whisper model: $Model"
& $pythonExe (Join-Path $scriptDir "transcribe_whisper.py") --file (Join-Path $scriptDir "warmup.wav") --language tr --model $Model 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Warmup skipped. The model will download on first real request."
}

Write-Host ""
Write-Host "Use these appsettings values on the API server:"
Write-Host "Voice:TranscriptionEnabled=true"
Write-Host "Voice:TranscriptionExecutablePath=$pythonExe"
Write-Host "Voice:TranscriptionArgumentsTemplate=`"Scripts/Voice/transcribe_whisper.py`" --language {language} --file `"{input}`" --model $Model"
