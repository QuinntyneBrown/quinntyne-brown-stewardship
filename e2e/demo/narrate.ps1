param([string]$InputPath = '.local/live-demo/narration.json')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Speech
$speaker = New-Object System.Speech.Synthesis.SpeechSynthesizer
try {
    $voice = $speaker.GetInstalledVoices() | Where-Object { $_.Enabled -and $_.VoiceInfo.Name -like '*Zira*' } | Select-Object -First 1
    if ($voice) { $speaker.SelectVoice($voice.VoiceInfo.Name) }
    $speaker.Rate = 0
    $scenes = Get-Content -LiteralPath $InputPath -Raw | ConvertFrom-Json
    foreach ($scene in $scenes) {
        $speaker.SetOutputToWaveFile($scene.path)
        $speaker.Speak($scene.text)
        $speaker.SetOutputToNull()
    }
    Write-Output "Narrated $($scenes.Count) chapters with $($speaker.Voice.Name)."
} finally { $speaker.Dispose() }
