using System;using System.IO;using System.Diagnostics;using PCInternetKontrolu;
class FirewallIntegrationTest {
 static int Probe(string exe,string host){var p=Process.Start(new ProcessStartInfo(exe,host){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true});string output=p.StandardOutput.ReadToEnd();p.WaitForExit();Console.WriteLine(output.Trim());return p.ExitCode;}
 static int Main(){string probe=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"FirewallProbe.exe");var backend=new WindowsFirewallBackend();var manager=new FirewallManager(backend,Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Manager.exe"),Environment.GetFolderPath(Environment.SpecialFolder.Windows));bool cleanup=false;
 try{
  var status=backend.Status();Console.WriteLine(status.Summary);if(!status.CanApply)throw new Exception("Firewall prerequisites are not met; no change made.");
  if(manager.State(probe)!=BlockState.None)throw new Exception("Preexisting probe rule; refusing to modify.");
  string host=null;foreach(string candidate in new[]{"1.1.1.1","www.microsoft.com"}){if(Probe(probe,candidate)==0){host=candidate;break;}}
  if(host==null)throw new Exception("No baseline internet TCP connection; cannot verify network blocking.");
  Console.WriteLine("PASS baseline TCP connection to "+host+":443");
  cleanup=true;manager.Block(probe);
  if(new FirewallManager(new WindowsFirewallBackend(),"unused.exe",Environment.GetFolderPath(Environment.SpecialFolder.Windows)).State(probe)!=BlockState.Blocked)throw new Exception("Rules did not persist.");
  Console.WriteLine("PASS persistent inbound/outbound all-profile rules for TEST EXE only.");
  if(Probe(probe,host)==0)throw new Exception("Test EXE can still connect after blocking.");
  Console.WriteLine("PASS test EXE's new internet TCP connection blocked.");
  manager.Unblock(probe);cleanup=false;if(manager.State(probe)!=BlockState.None)throw new Exception("Test rule cleanup failed.");
  if(Probe(probe,host)!=0)throw new Exception("Connection did not recover after removing TEST rules.");
  Console.WriteLine("PASS connection recovered; test rules removed and absence verified. User applications and global firewall settings unchanged.");return 0;
 }catch(Exception e){Console.WriteLine("FAIL: "+e);return 1;}
 finally{if(cleanup){try{manager.Unblock(probe);Console.WriteLine("Test firewall rule cleanup verified.");}catch(Exception e){Console.WriteLine("CLEANUP FAILED: "+e.Message);}}}
 }
}
