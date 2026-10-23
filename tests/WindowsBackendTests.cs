using System;using System.Reflection;using PCInternetKontrolu;
// Public so the production dynamic binder can read this in-memory COM-shaped record.
public sealed class FirewallRecord {
 public string Name,ApplicationName,Grouping=RuleIdentity.Group,Description="external description";
 public bool RejectEmptyServiceName;string serviceName="fixture-service";
 public string ServiceName {get{return serviceName;}set{if(RejectEmptyServiceName&&string.IsNullOrEmpty(value))throw new ArgumentException("Empty ServiceName rejected like Windows COM");serviceName=value;}}
 public int Direction=2,Action=0,Profiles=int.MaxValue,Protocol=6;public bool Enabled=false,EdgeTraversal=true;
 public string LocalAddresses="*",RemoteAddresses="*",InterfaceTypes="All",LocalPorts="443",RemotePorts="8443",IcmpTypesAndCodes="";
 public object Interfaces=new string[]{"Fixture adapter"};
 public int EdgeTraversalOptions,SecureFlags;
 public string LocalAppPackageId="",LocalUserOwner="",LocalUserAuthorizedList="",RemoteUserAuthorizedList="",RemoteMachineAuthorizedList="";
}
public sealed class MemoryRules:System.Collections.Generic.List<FirewallRecord> {
 public int Adds,Removes,FailOnAdd;
 public new void Add(FirewallRecord rule){Adds++;if(Adds==FailOnAdd)throw new InvalidOperationException("Simulated Add failure");base.Add(rule);}
 public void Remove(string name){Removes++;RemoveAll(r=>r.Name==name);}
}
public sealed class MemoryPolicy {public MemoryRules Rules=new MemoryRules();public bool[] FirewallEnabled=new[]{true,true,true,true,true,true,true,true};public int CurrentProfileTypes=4,LocalPolicyModifyState=0;}
class WindowsBackendTests {
 static int passed,failed;
 static void Test(string name,Action test){try{test();Console.WriteLine("PASS "+name);passed++;}catch(Exception e){Console.WriteLine("FAIL "+name+": "+e.GetBaseException().Message);failed++;}}
 static void Equal(object actual,object expected){if(!object.Equals(actual,expected))throw new Exception("Expected "+expected+", got "+actual);}
 static object Field(RuleSpec rule,string name){var field=typeof(RuleSpec).GetField(name);if(field==null)throw new Exception(name+" not captured");return field.GetValue(rule);}
 static RuleSpec Read(FirewallRecord record){return (RuleSpec)typeof(WindowsFirewallBackend).GetMethod("Read",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(new WindowsFirewallBackend(),new object[]{record});}
 static FirewallRecord Ordinary(string path,int direction){return new FirewallRecord{Name=RuleIdentity.Name(path,direction),ApplicationName=path,Direction=direction,Protocol=256,ServiceName="",Interfaces=null,LocalPorts="",RemotePorts="",EdgeTraversal=false};}
 static WindowsFirewallBackend Backend(MemoryPolicy policy){return new WindowsFirewallBackend(()=>policy,()=>Ordinary("C:\\fixture\\new.exe",2));}
 static void Fails(Action action){try{action();}catch(InvalidOperationException){return;}throw new Exception("Expected refusal");}
 static void SameRecord(FirewallRecord actual,FirewallRecord expected){foreach(var field in typeof(FirewallRecord).GetFields())Equal(field.GetValue(actual),field.GetValue(expected));Equal(actual.ServiceName,expected.ServiceName);}
 static int Main(){
  var record=new FirewallRecord{Name=RuleIdentity.Name("C:\\fixture\\app.exe",2),ApplicationName="C:\\fixture\\app.exe"};
  Test("ordinary rule skips COM-invalid empty ServiceName setter",()=>{var p=new MemoryPolicy();var backend=new WindowsFirewallBackend(()=>p,()=>{var fresh=Ordinary(record.ApplicationName,2);fresh.RejectEmptyServiceName=true;return fresh;});backend.Put(BlockRules.Create(record.ApplicationName)[0]);Equal(p.Rules.Count,1);Equal(p.Rules[0].ServiceName,"");});
  Test("capture service restriction",()=>Equal(Field(Read(record),"ServiceName"),record.ServiceName));
  Test("capture specific interfaces",()=>Equal(((string[])Field(Read(record),"SpecificInterfaces"))[0],"Fixture adapter"));
  Test("capture local and remote ports",()=>{Equal(Field(Read(record),"LocalPorts"),"443");Equal(Field(Read(record),"RemotePorts"),"8443");});
  Test("capture external description",()=>Equal(Field(Read(record),"Description"),record.Description));
  Test("capture edge traversal",()=>Equal(Field(Read(record),"EdgeTraversal"),true));
  Test("backend refuses scoped or allow replacement before deletion",()=>{
   Action<FirewallRecord>[] edits={r=>r.ServiceName="fixture-service",r=>r.Interfaces=new[]{"Fixture adapter"},r=>{r.Protocol=6;r.LocalPorts="443";r.RemotePorts="8443";},r=>r.Action=1,r=>r.EdgeTraversal=true};
   foreach(var edit in edits){var p=new MemoryPolicy();var prior=Ordinary(record.ApplicationName,2);edit(prior);p.Rules.Add(prior);int adds=p.Rules.Adds;Fails(()=>Backend(p).Put(BlockRules.Create(record.ApplicationName)[0]));Equal(p.Rules.Removes,0);Equal(p.Rules.Adds,adds);Equal(p.Rules[0],prior);}
  });
  Test("unrecognized extended scopes refused without deletion",()=>{
   Action<FirewallRecord>[] edits={r=>r.LocalAppPackageId="fixture-package",r=>r.LocalUserOwner="fixture-owner",r=>r.LocalUserAuthorizedList="fixture-local",r=>r.RemoteUserAuthorizedList="fixture-remote",r=>r.RemoteMachineAuthorizedList="fixture-machine",r=>r.SecureFlags=1,r=>r.EdgeTraversalOptions=2};
   foreach(var edit in edits){var p=new MemoryPolicy();var prior=Ordinary(record.ApplicationName,2);edit(prior);p.Rules.Add(prior);Fails(()=>Backend(p).Put(BlockRules.Create(record.ApplicationName)[0]));Equal(p.Rules.Removes,0);Equal(p.Rules[0],prior);}
  });
  Test("backend rollback restores every ordinary captured setting",()=>{
   string root=System.IO.Path.Combine(Environment.GetEnvironmentVariable("TMPDIR")??System.IO.Path.GetTempPath(),"PCNet-backend-"+Guid.NewGuid().ToString("N"));System.IO.Directory.CreateDirectory(root);
   try{string path=System.IO.Path.Combine(root,"fixture.exe");System.IO.File.WriteAllText(path,"fixture");var p=new MemoryPolicy();var prior=Ordinary(path,2);prior.Profiles=3;prior.Description="Keep external description";p.Rules.Add(prior);p.Rules.FailOnAdd=3;var manager=new FirewallManager(Backend(p),System.IO.Path.Combine(root,"self.exe"),System.IO.Path.Combine(root,"Windows"));Fails(()=>manager.Block(path));Equal(p.Rules.Count,1);SameRecord(p.Rules[0],prior);}
   finally{System.IO.Directory.Delete(root,true);}
  });
  Console.WriteLine("RESULT "+passed+" passed; "+failed+" failed. In-memory COM records only.");return failed==0?0:1;
 }
}
