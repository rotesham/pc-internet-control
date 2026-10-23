using System;using System.Linq;using System.Collections.Generic;
namespace PCInternetKontrolu {
 public sealed class FirewallStatus {
  public bool Domain=true,Private=true,Public=true;public int ActiveProfiles=4;public bool PolicyBlocked;
  public bool CanApply{get{return Domain&&Private&&Public&&!PolicyBlocked;}}
  public string Summary{get{return UiText.Get("Etki alanı: ")+(Domain?UiText.Get("açık"):UiText.Get("KAPALI"))+UiText.Get(" • Özel: ")+(Private?UiText.Get("açık"):UiText.Get("KAPALI"))+UiText.Get(" • Genel: ")+(Public?UiText.Get("açık"):UiText.Get("KAPALI"))+(PolicyBlocked?UiText.Get(" • Kurum politikası yerel kuralları geçersiz kılıyor."):"");}}
 }
 public interface IFirewallBackend {List<RuleSpec> Snapshot();FirewallStatus Status();void Put(RuleSpec rule);void Delete(string name);}
 public sealed class FakeFirewallBackend:IFirewallBackend {
  public readonly Dictionary<string,RuleSpec> Rules=new Dictionary<string,RuleSpec>();public FirewallStatus Current=new FirewallStatus();public int PutCalls,DeleteCalls,FailOnPutNumber;
  public List<RuleSpec> Snapshot(){return Rules.Values.Select(r=>r.Clone()).ToList();}
  public FirewallStatus Status(){return Current;}
  public void Put(RuleSpec rule){PutCalls++;if(PutCalls==FailOnPutNumber)throw new InvalidOperationException("Simulated rule write failure");RuleSpec old;if(Rules.TryGetValue(rule.Name,out old)&&old.Group!=RuleIdentity.Group)throw new InvalidOperationException("Foreign rule collision");Rules[rule.Name]=rule.Clone();}
  public void Delete(string name){DeleteCalls++;RuleSpec old;if(Rules.TryGetValue(name,out old)&&old.Group!=RuleIdentity.Group)throw new InvalidOperationException("Foreign rule collision");Rules.Remove(name);}
 }
 public enum BlockState {None,Partial,Blocked}
 public sealed class FirewallManager {
  readonly IFirewallBackend backend;readonly string self,windows;
  public FirewallManager(IFirewallBackend backend,string self,string windows){this.backend=backend;this.self=self;this.windows=windows;}
  public bool Owns(RuleSpec rule){try{return rule.Group==RuleIdentity.Group && rule.Name==RuleIdentity.Name(rule.Path,rule.Direction);}catch(ArgumentException){return false;}}
  public List<RuleSpec> Managed(){return backend.Snapshot().Where(Owns).ToList();}
  static bool Complete(RuleSpec r){return r.Enabled&&(r.Profiles&7)==7&&r.CanRestore;}
  public BlockState State(string path){return StateFrom(Managed(),path);}
  public BlockState StateFrom(List<RuleSpec> managed,string path){
   path=RuleIdentity.Canonical(path);var rules=managed.Where(r=>r.Path.Equals(path,StringComparison.OrdinalIgnoreCase)).ToList();
   if(rules.Count==0)return BlockState.None;
   return rules.Any(r=>r.Direction==1&&Complete(r))&&rules.Any(r=>r.Direction==2&&Complete(r))?BlockState.Blocked:BlockState.Partial;
  }
  public void Block(string path){
   Safety.CheckExe(path,self,windows);
   if(!backend.Status().CanApply)throw new InvalidOperationException(UiText.Get("Güvenlik Duvarı profilleri kapalı veya kurum politikası yerel kuralları engelliyor. Genel ayarlar otomatik değiştirilmedi."));
   var desired=BlockRules.Create(path);var snapshot=backend.Snapshot();var old=new Dictionary<string,RuleSpec>();
   foreach(var rule in desired){var matches=snapshot.Where(r=>r.Name==rule.Name).ToList();if(matches.Count>1 || matches.Any(r=>!Owns(r)))throw new InvalidOperationException(UiText.Get("Başka bir kuralla ad çakışması var; değiştirilmedi."));if(matches.Count==1){matches[0].RequireRestorable();old[rule.Name]=matches[0];}}
   var attempted=new List<string>();
   try{foreach(var rule in desired){attempted.Add(rule.Name);backend.Put(rule);}if(State(path)!=BlockState.Blocked)throw new InvalidOperationException(UiText.Get("Engelleme kuralları doğrulanamadı."));}
   catch(Exception original){var rollback=new List<string>();foreach(string name in attempted){try{RuleSpec prior;if(old.TryGetValue(name,out prior))backend.Put(prior);else backend.Delete(name);}catch(Exception e){rollback.Add(e.Message);}}throw new InvalidOperationException(original.Message+(rollback.Count==0?UiText.Get(" Değişiklik geri alındı."):UiText.Get(" Geri alma tamamlanamadı: ")+string.Join("; ",rollback)),original);}
  }
  public void Unblock(string path){
   path=RuleIdentity.Canonical(path);foreach(var r in BlockRules.Create(path))backend.Delete(r.Name);
   if(State(path)!=BlockState.None)throw new InvalidOperationException(UiText.Get("Bazı kurallar kaldırılamadı; durumu yenileyin."));
  }
 }
}
