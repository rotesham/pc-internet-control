using System;using System.IO;using PCInternetKontrolu;
class CoreTests {
 static int passed,failed;static string root,app,child,windows,self;
 static void Test(string n,Action a){try{a();Console.WriteLine("PASS "+n);passed++;}catch(Exception e){Console.WriteLine("FAIL "+n+": "+e.Message);failed++;}}
 static void Equal(object a,object b){if(!a.Equals(b))throw new Exception("Expected "+b+", got "+a);}
 static void Reject(Action a){try{a();}catch(ArgumentException){return;}throw new Exception("Unsafe input accepted");}
 static void WithJunction(Action<string> test){
  string alias=Path.Combine(root,"protected-alias");
  var start=new System.Diagnostics.ProcessStartInfo("cmd.exe","/c mklink /J \""+alias+"\" \""+windows+"\""){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
  using(var process=System.Diagnostics.Process.Start(start)){process.WaitForExit();if(process.ExitCode!=0)throw new Exception("Fixture junction creation failed: "+process.StandardError.ReadToEnd());}
  try{test(alias);}finally{Directory.Delete(alias);}
 }
 static int Main(){root=Path.Combine(Environment.GetEnvironmentVariable("TMPDIR") ?? Path.GetTempPath(),"PCInternetKontrolu-tests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);app=Path.Combine(root,"app.exe");self=Path.Combine(root,"manager.exe");windows=Path.Combine(root,"Windows");Directory.CreateDirectory(windows);Directory.CreateDirectory(Path.Combine(root,"sub"));child=Path.Combine(root,"sub","child.EXE");File.WriteAllText(app,"fixture");File.WriteAllText(child,"fixture");File.WriteAllText(self,"fixture");File.WriteAllText(Path.Combine(windows,"system.exe"),"fixture");File.WriteAllText(Path.Combine(root,"note.txt"),"fixture");
  try{
 Directory.CreateDirectory(Path.Combine(windows,"nested"));File.WriteAllText(Path.Combine(windows,"nested","protected.exe"),"fixture");
 Test("first run validation defaults to English",()=>{try{RuleIdentity.Canonical("");}catch(ArgumentException e){Equal(e.Message,"File path cannot be empty.");}});
 Test("stable path identity",()=>Equal(RuleIdentity.Name(app,2),RuleIdentity.Name(app.ToUpperInvariant(),2)));
 Test("directions distinct",()=>{if(RuleIdentity.Name(app,1)==RuleIdentity.Name(app,2))throw new Exception("Collision");});
 Test("empty rejected",()=>Reject(()=>RuleIdentity.Name("",2)));
 Test("invalid direction rejected",()=>Reject(()=>RuleIdentity.Name(app,99)));
 Test("normal exe allowed",()=>Safety.CheckExe(app,self,windows));
 Test("ancestor junction cannot alias protected exe",()=>WithJunction(alias=>Reject(()=>Safety.CheckExe(Path.Combine(alias,"nested","protected.exe"),self,windows))));
 Test("ancestor junction cannot alias self exe",()=>WithJunction(alias=>Reject(()=>Safety.CheckExe(Path.Combine(alias,"nested","protected.exe"),Path.Combine(windows,"nested","protected.exe"),Path.Combine(root,"OtherWindows")))));
 Test("nested scan root under ancestor junction refused",()=>WithJunction(alias=>Reject(()=>FolderScanner.Scan(Path.Combine(alias,"nested"),true,self,windows,500))));
 Test("self blocking rejected",()=>Reject(()=>Safety.CheckExe(self,self,windows)));
 Test("system exe rejected",()=>Reject(()=>Safety.CheckExe(Path.Combine(windows,"system.exe"),self,windows)));
 Test("missing exe rejected",()=>Reject(()=>Safety.CheckExe(Path.Combine(root,"missing.exe"),self,windows)));
 Test("non exe rejected",()=>Reject(()=>Safety.CheckExe(Path.Combine(root,"note.txt"),self,windows)));
 Test("root scan rejected",()=>Reject(()=>FolderScanner.Scan(Path.GetPathRoot(root),true,self,windows,500)));
 Test("recursive scan skips self and system",()=>Equal(FolderScanner.Scan(root,true,self,windows,500).Paths.Count,2));
 Test("top-level scan",()=>Equal(FolderScanner.Scan(root,false,self,windows,500).Paths.Count,1));
 Test("scan cap",()=>Equal(FolderScanner.Scan(root,true,self,windows,1).Paths.Count,1));
 Test("two block directions",()=>{var r=BlockRules.Create(app);Equal(r.Length,2);Equal(r[0].Direction,2);Equal(r[1].Direction,1);});
 Test("all profiles and protocols",()=>{foreach(var r in BlockRules.Create(app)){Equal(r.Profiles,int.MaxValue);Equal(r.Protocol,256);Equal(r.Action,0);Equal(r.Enabled,true);Equal(r.RemoteAddresses,"*");Equal(r.LocalAddresses,"*");Equal(r.Interfaces,"All");}});
 Test("rule paths and ownership",()=>{foreach(var r in BlockRules.Create(app)){Equal(r.Path,Path.GetFullPath(app));Equal(r.Group,RuleIdentity.Group);Equal(r.Name,RuleIdentity.Name(app,r.Direction));}});
 }finally{Directory.Delete(root,true);}
 Console.WriteLine("RESULT "+passed+" passed; "+failed+" failed. No firewall changes.");return failed==0?0:1;
 }
}
