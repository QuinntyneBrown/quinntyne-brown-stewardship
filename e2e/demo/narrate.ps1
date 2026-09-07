param([string]$InputPath = '.local/live-demo/narration.json', [string]$VoiceName = 'Microsoft Zira Desktop')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Speech
$speaker = New-Object System.Speech.Synthesis.SpeechSynthesizer
try {
    $voice = $speaker.GetInstalledVoices() | Where-Object { $_.Enabled -and $_.VoiceInfo.Name -eq $VoiceName } | Select-Object -First 1
    if (-not $voice) { throw "Required Windows narrator is not installed: $VoiceName" }
    $speaker.SelectVoice($voice.VoiceInfo.Name)
    $speaker.Rate = 0
    $scenes = Get-Content -LiteralPath $InputPath -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach ($scene in $scenes) {
        $speaker.SetOutputToWaveFile($scene.path)
        $speaker.Speak($scene.text)
        $speaker.SetOutputToNull()
    }
    Write-Output "Narrated $($scenes.Count) chapters with $($speaker.Voice.Name)."
} finally { $speaker.Dispose() }
