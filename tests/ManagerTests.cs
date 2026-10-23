using System;using System.IO;using PCInternetKontrolu;
class ManagerTests {
 static int passed,failed;static string app,self,win,root;
 static void Test(string n,Action a){try{a();Console.WriteLine("PASS "+n);passed++;}catch(Exception e){Console.WriteLine("FAIL "+n+": "+e.Message);failed++;}}
 static void Equal(object a,object b){if(!a.Equals(b))throw new Exception("Expected "+b+", got "+a);}
 static void Fails(Action a){try{a();}catch(InvalidOperationException){return;}throw new Exception("Expected failure");}
 static FirewallManager M(FakeFirewallBackend b){return new FirewallManager(b,self,win);}
 static int Main(){root=Path.Combine(Environment.GetEnvironmentVariable("TMPDIR") ?? Path.GetTempPath(),"PCNet-manager-tests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);app=Path.Combine(root,"app.exe");File.WriteAllText(app,"fixture");self=Path.Combine(root,"manager.exe");win=Path.Combine(root,"Windows");
 try{
 Test("initial unblocked",()=>Equal(M(new FakeFirewallBackend()).State(app),BlockState.None));
 Test("apply verified pair",()=>{var b=new FakeFirewallBackend();var m=M(b);m.Block(app);Equal(b.Rules.Count,2);Equal(m.State(app),BlockState.Blocked);});
 Test("idempotent apply",()=>{var b=new FakeFirewallBackend();var m=M(b);m.Block(app);m.Block(app);Equal(b.Rules.Count,2);});
 Test("partial detection",()=>{var b=new FakeFirewallBackend();b.Put(BlockRules.Create(app)[0]);Equal(M(b).State(app),BlockState.Partial);});
 Test("service-limited pair is partial",()=>{var b=new FakeFirewallBackend();foreach(var r in BlockRules.Create(app)){r.ServiceName="fixture-service";b.Put(r);}Equal(M(b).State(app),BlockState.Partial);});
 Test("specific-interface pair is partial",()=>{var b=new FakeFirewallBackend();foreach(var r in BlockRules.Create(app)){r.SpecificInterfaces=new[]{"Fixture adapter"};b.Put(r);}Equal(M(b).State(app),BlockState.Partial);});
 Test("unknown scoped pair is partial",()=>{var b=new FakeFirewallBackend();foreach(var r in BlockRules.Create(app)){r.UnsupportedSettings=true;b.Put(r);}Equal(M(b).State(app),BlockState.Partial);});
 Test("disabled rule not blocked",()=>{var b=new FakeFirewallBackend();foreach(var r in BlockRules.Create(app)){r.Enabled=false;b.Put(r);}Equal(M(b).State(app),BlockState.Partial);});
 Test("unblock removes only app rules",()=>{var b=new FakeFirewallBackend();b.Rules["Other"] = new RuleSpec{Name="Other",Path=app,Group="Windows"};var m=M(b);m.Block(app);m.Unblock(app);Equal(b.Rules.Count,1);Equal(b.Rules["Other"].Group,"Windows");});
 Test("unblock missing file",()=>{var b=new FakeFirewallBackend();var m=M(b);m.Block(app);File.Delete(app);try{m.Unblock(app);Equal(m.State(app),BlockState.None);}finally{File.WriteAllText(app,"fixture");}});
 Test("failure rolls back new rules",()=>{var b=new FakeFirewallBackend();b.FailOnPutNumber=2;Fails(()=>M(b).Block(app));Equal(b.Rules.Count,0);});
 Test("failure restores old rule",()=>{var b=new FakeFirewallBackend();var r=BlockRules.Create(app)[0];r.Enabled=false;b.Put(r);b.FailOnPutNumber=3;Fails(()=>M(b).Block(app));Equal(b.Rules.Count,1);Equal(b.Rules[r.Name].Enabled,false);});
 Test("modified scoped and allow rules refused before writes",()=>{
  Action<RuleSpec>[] edits={r=>r.ServiceName="fixture-service",r=>r.SpecificInterfaces=new[]{"Fixture adapter"},r=>{r.Protocol=6;r.LocalPorts="443";r.RemotePorts="8443";},r=>r.LocalAddresses="127.0.0.1",r=>r.RemoteAddresses="192.0.2.1",r=>r.Interfaces="Wireless",r=>r.Action=1,r=>r.EdgeTraversal=true};
  foreach(var edit in edits){var b=new FakeFirewallBackend();var pair=BlockRules.Create(app);edit(pair[1]);foreach(var rule in pair)b.Rules[rule.Name]=rule.Clone();int writes=b.PutCalls;Fails(()=>M(b).Block(app));Equal(b.PutCalls,writes);foreach(var rule in pair)foreach(var field in typeof(RuleSpec).GetFields()){object actual=field.GetValue(b.Rules[rule.Name]),expected=field.GetValue(rule);if(actual is string[])Equal(string.Join("|",(string[])actual),string.Join("|",(string[])expected));else Equal(actual,expected);}}
 });
 Test("disabled profile refuses write",()=>{var b=new FakeFirewallBackend();b.Current.Public=false;Fails(()=>M(b).Block(app));Equal(b.PutCalls,0);});
 Test("policy override refuses write",()=>{var b=new FakeFirewallBackend();b.Current.PolicyBlocked=true;Fails(()=>M(b).Block(app));Equal(b.PutCalls,0);});
 Test("foreign collision preserved",()=>{var b=new FakeFirewallBackend();string name=RuleIdentity.Name(app,2);b.Rules[name]=new RuleSpec{Name=name,Path=app,Direction=2,Group="Other"};Fails(()=>M(b).Block(app));Equal(b.Rules[name].Group,"Other");Equal(b.Rules.Count,1);});
 }finally{Directory.Delete(root,true);}
 Console.WriteLine("RESULT "+passed+" passed; "+failed+" failed. Fake firewall only.");return failed==0?0:1;}
}
