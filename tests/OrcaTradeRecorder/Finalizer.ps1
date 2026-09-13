$ErrorActionPreference='Stop'
$source=Get-Content -LiteralPath (Join-Path $PSScriptRoot '../../Orca Trades/Working_Suite/AddOns/OrcaTradeRecorderAddOn.cs') -Raw
$start=$source.IndexOf('internal static class OrcaTradeRecorderFinalizer')
$end=$source.IndexOf('internal static class OrcaTradeRecorderDiagnostics',$start)
$finalizer=$source.Substring($start,$end-$start)
$stub=@'
using System; using System.IO; using System.Linq; using System.Text; using System.Diagnostics; using System.Globalization; using System.Threading.Tasks; using System.Collections.Generic; using System.Web.Script.Serialization;
namespace FinalizerFixture {
public class OrcaTradeRecorderSettings {public string FfmpegPath;}
public class OrcaRecorderManifestEvent { public DateTime Utc; public string Type,Detail; }
public class OrcaRecorderCaptureManifest {
 public string CaptureId="fixture",BundleDirectory,ResultTitle="test",FinalVideoPath,FinalizationStatus="Pending",FinalizationError;
 public DateTime StartedUtc=DateTime.UtcNow.AddMinutes(-1),StoppedUtc=DateTime.UtcNow;
 public List<string> RawSegments=new List<string>();public List<OrcaRecorderManifestEvent> Events=new List<OrcaRecorderManifestEvent>();
}
public static class Checks {
 public static void Run(string root,string ffmpeg) {
  var settings=new OrcaTradeRecorderSettings{FfmpegPath=ffmpeg};
  var capture=new OrcaRecorderCaptureManifest{BundleDirectory=root};capture.RawSegments.Add(Path.Combine(root,"raw.mkv"));
  capture.RawSegments.Add(Path.Combine(root,"missing.mkv"));
  try {OrcaTradeRecorderFinalizer.FinalizeAsync(capture,settings).GetAwaiter().GetResult();throw new Exception("Missing segment accepted");}catch(FileNotFoundException){}
  if(capture.FinalizationStatus!="Pending" || !File.Exists(capture.RawSegments[0]))throw new Exception("Failed recovery changed evidence");
  capture.RawSegments.RemoveAt(1);capture.StoppedUtc=default(DateTime);
  try {OrcaTradeRecorderFinalizer.FinalizeAsync(capture,settings).GetAwaiter().GetResult();throw new Exception("Unstopped capture accepted");}catch(InvalidDataException){}
  capture.StoppedUtc=DateTime.UtcNow;OrcaTradeRecorderFinalizer.FinalizeAsync(capture,settings).GetAwaiter().GetResult();
  if(capture.FinalizationStatus!="Complete" || !File.Exists(capture.FinalVideoPath) || !File.Exists(capture.RawSegments[0]))throw new Exception("Finalization or raw preservation failed");
  var saved=new JavaScriptSerializer().Deserialize<OrcaRecorderCaptureManifest>(File.ReadAllText(Path.Combine(root,"capture.json")));
  if(saved.FinalVideoPath!=capture.FinalVideoPath || saved.FinalizationStatus!="Complete")throw new Exception("Published manifest incorrect");
 }
}
'@
Add-Type -TypeDefinition ($stub+$finalizer+'}') -ReferencedAssemblies System.Core,System.Web.Extensions
$settingsPath=Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'NinjaTrader 8/OrcaTradeRecorder.xml'
$settingsXml=[xml](Get-Content -LiteralPath $settingsPath -Raw)
$ffmpeg=$settingsXml.SelectSingleNode('//FfmpegPath').InnerText
$fixture=Join-Path $env:TEMP ('orca-finalizer-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fixture | Out-Null
& $ffmpeg -hide_banner -loglevel error -f lavfi -i 'color=c=black:s=64x64:r=10' -f lavfi -i 'anullsrc=r=44100:cl=mono' -t 0.3 -c:v libx264 -c:a aac (Join-Path $fixture 'raw.mkv')
if($LASTEXITCODE -ne 0){throw 'Synthetic test fixture generation failed'}
[FinalizerFixture.Checks]::Run($fixture,$ffmpeg)
Write-Output "PASS: missing-segment rejection, stop-boundary guard, real FFmpeg/ffprobe finalization, manifest publication, raw preservation. $fixture"
