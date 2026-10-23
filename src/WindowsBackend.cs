using System;using System.Linq;using System.Collections.Generic;
namespace PCInternetKontrolu {
 public sealed class WindowsFirewallBackend:IFirewallBackend {
  readonly Func<object> policyFactory,ruleFactory;
  public WindowsFirewallBackend():this(()=>Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2")),()=>Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"))){}
  internal WindowsFirewallBackend(Func<object> policyFactory,Func<object> ruleFactory){this.policyFactory=policyFactory;this.ruleFactory=ruleFactory;}
  dynamic Policy(){return policyFactory();}
  bool Unsupported(dynamic r){
   try{return (int)r.EdgeTraversalOptions!=0||(int)r.SecureFlags!=0||!string.IsNullOrEmpty((string)r.LocalAppPackageId)||!string.IsNullOrEmpty((string)r.LocalUserOwner)||!string.IsNullOrEmpty((string)r.LocalUserAuthorizedList)||!string.IsNullOrEmpty((string)r.RemoteUserAuthorizedList)||!string.IsNullOrEmpty((string)r.RemoteMachineAuthorizedList);}
   catch(Exception){return true;} // Unknown/unreadable scopes must never be silently dropped.
  }
  RuleSpec Read(dynamic r){
   int protocol=(int)r.Protocol;var interfaces=new List<string>();object specific=r.Interfaces;
   if(specific!=null)foreach(object item in (Array)specific)interfaces.Add((string)item);
   return new RuleSpec{Name=(string)r.Name,Path=(string)r.ApplicationName,Group=(string)r.Grouping,Direction=(int)r.Direction,Action=(int)r.Action,Profiles=(int)r.Profiles,Protocol=protocol,Enabled=(bool)r.Enabled,LocalAddresses=(string)r.LocalAddresses,RemoteAddresses=(string)r.RemoteAddresses,Interfaces=(string)r.InterfaceTypes,
    ServiceName=(string)r.ServiceName,SpecificInterfaces=interfaces.ToArray(),Description=(string)r.Description,EdgeTraversal=(bool)r.EdgeTraversal,UnsupportedSettings=Unsupported(r),
    LocalPorts=protocol==6||protocol==17?(string)r.LocalPorts:"",RemotePorts=protocol==6||protocol==17?(string)r.RemotePorts:"",IcmpTypesAndCodes=protocol==1||protocol==58?(string)r.IcmpTypesAndCodes:""};
  }
  List<RuleSpec> Find(dynamic policy,string name){var found=new List<RuleSpec>();foreach(dynamic r in policy.Rules){if((string)r.Name==name)found.Add(Read(r));}return found;}
  void RequireOwned(RuleSpec r){if(r.Group!=RuleIdentity.Group || r.Name!=RuleIdentity.Name(r.Path,r.Direction))throw new InvalidOperationException(UiText.Get("Uygulamaya ait olmayan kurala dokunulmadı: ")+r.Name);}
  public List<RuleSpec> Snapshot(){dynamic p=Policy();var list=new List<RuleSpec>();foreach(dynamic r in p.Rules){string name=(string)r.Name;if(name!=null&&name.StartsWith(RuleIdentity.Prefix,StringComparison.Ordinal))list.Add(Read(r));}return list;}
  public FirewallStatus Status(){dynamic p=Policy();return new FirewallStatus{Domain=(bool)p.FirewallEnabled[1],Private=(bool)p.FirewallEnabled[2],Public=(bool)p.FirewallEnabled[4],ActiveProfiles=(int)p.CurrentProfileTypes,PolicyBlocked=(int)p.LocalPolicyModifyState==1};}
  public void Put(RuleSpec spec){
   RequireOwned(spec);spec.RequireRestorable();dynamic p=Policy();var old=Find(p,spec.Name);
   if(old.Count>1)throw new InvalidOperationException(UiText.Get("Aynı adlı birden fazla kural var; değiştirilmedi."));
   foreach(var r in old){RequireOwned(r);r.RequireRestorable();}
   dynamic rule=ruleFactory();
   rule.Name=spec.Name;rule.Description=spec.Description;
   rule.Grouping=spec.Group;rule.ApplicationName=spec.Path;if(!string.IsNullOrEmpty(spec.ServiceName))rule.ServiceName=spec.ServiceName;rule.Protocol=spec.Protocol;
   rule.LocalAddresses=spec.LocalAddresses;rule.RemoteAddresses=spec.RemoteAddresses;rule.InterfaceTypes=spec.Interfaces;
   rule.Profiles=spec.Profiles;rule.Direction=spec.Direction;rule.Action=spec.Action;rule.EdgeTraversal=false;rule.Enabled=spec.Enabled;
   foreach(var prior in old)p.Rules.Remove(spec.Name);
   p.Rules.Add(rule);
   var check=Find(p,spec.Name);if(check.Count!=1)throw new InvalidOperationException(UiText.Get("Kural kaydı doğrulanamadı: ")+spec.Name);
   var actual=check[0];RequireOwned(actual);
   if(!actual.CanRestore || actual.Enabled!=spec.Enabled || actual.Direction!=spec.Direction || actual.Action!=spec.Action || actual.Profiles!=spec.Profiles || actual.Protocol!=spec.Protocol || actual.Description!=spec.Description || !actual.Path.Equals(spec.Path,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException(UiText.Get("Kural alanları Windows'ta uyuşmuyor."));
  }
  public void Delete(string name){
   if(!name.StartsWith(RuleIdentity.Prefix,StringComparison.Ordinal))throw new InvalidOperationException(UiText.Get("Yalnız bu uygulamanın kuralları kaldırılabilir."));
   dynamic p=Policy();var old=Find(p,name);foreach(var r in old)RequireOwned(r);foreach(var r in old)p.Rules.Remove(name);
   if(Find(p,name).Count!=0)throw new InvalidOperationException(UiText.Get("Kural kaldırılamadı: ")+name);
  }
 }
}
