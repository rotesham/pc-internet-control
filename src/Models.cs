using System;
using System.Collections.Generic;
using System.IO;
namespace PCInternetKontrolu {
 public sealed class RuleSpec {
  public string Name,Path,Group=RuleIdentity.Group; public int Direction,Action=0,Profiles=int.MaxValue,Protocol=256;public bool Enabled=true;
  public string LocalAddresses="*",RemoteAddresses="*",Interfaces="All";
  public string ServiceName="",Description="",LocalPorts="",RemotePorts="",IcmpTypesAndCodes="";
  public string[] SpecificInterfaces=new string[0];public bool EdgeTraversal,UnsupportedSettings;
  // Only ordinary application-wide block rules can be replaced and restored exactly.
  public bool CanRestore{get{return Action==0&&Protocol==256&&LocalAddresses=="*"&&RemoteAddresses=="*"&&string.Equals(Interfaces,"All",StringComparison.OrdinalIgnoreCase)&&string.IsNullOrEmpty(ServiceName)&&SpecificInterfaces!=null&&SpecificInterfaces.Length==0&&string.IsNullOrEmpty(LocalPorts)&&string.IsNullOrEmpty(RemotePorts)&&string.IsNullOrEmpty(IcmpTypesAndCodes)&&!EdgeTraversal&&!UnsupportedSettings;}}
  public void RequireRestorable(){if(!CanRestore)throw new InvalidOperationException(UiText.Get("Dışarıdan değiştirilmiş/kapsamlı veya izin veren kural güvenle geri yüklenemez; hiçbir kural değiştirilmedi. Windows Güvenlik Duvarı'nda inceleyin."));}
  public RuleSpec Clone(){var copy=(RuleSpec)MemberwiseClone();copy.SpecificInterfaces=SpecificInterfaces==null?null:(string[])SpecificInterfaces.Clone();return copy;}
 }
 public sealed class ScanResult {public readonly List<string> Paths=new List<string>();public readonly List<string> Warnings=new List<string>();}
 public static class Safety {
  public static bool Within(string path,string folder){string p=RuleIdentity.Canonical(path).TrimEnd('\\');string f=RuleIdentity.Canonical(folder).TrimEnd('\\');return p.Equals(f,StringComparison.OrdinalIgnoreCase)||p.StartsWith(f+"\\",StringComparison.OrdinalIgnoreCase);}
  public static void CheckAncestors(string path){
   for(string current=RuleIdentity.Canonical(path);current!=null;current=Path.GetDirectoryName(current))
    if((File.GetAttributes(current)&FileAttributes.ReparsePoint)!=0)throw new ArgumentException(UiText.Get("Bağlantı/junction içeren yol güvenlik için reddedildi; gerçek klasör ve EXE yolunu seçin."));
  }
  public static void CheckExe(string path,string self,string windows){
   path=RuleIdentity.Canonical(path);
   if(!Path.GetExtension(path).Equals(".exe",StringComparison.OrdinalIgnoreCase))throw new ArgumentException(UiText.Get("Yalnız EXE dosyaları seçilebilir."));
   if(path.Equals(RuleIdentity.Canonical(self),StringComparison.OrdinalIgnoreCase))throw new ArgumentException(UiText.Get("Bu yönetim uygulaması engellenemez."));
   if(Within(path,windows))throw new ArgumentException(UiText.Get("Windows sistem klasöründeki dosyalar güvenlik için engellenmez."));
   if(!File.Exists(path))throw new ArgumentException(UiText.Get("EXE dosyası bulunamadı: ")+path);
   CheckAncestors(path);
  }
 }
 public static class FolderScanner {
  public static ScanResult Scan(string root,bool recursive,string self,string windows,int limit){
   root=RuleIdentity.Canonical(root);
   if(root.TrimEnd('\\').Equals(Path.GetPathRoot(root).TrimEnd('\\'),StringComparison.OrdinalIgnoreCase))throw new ArgumentException(UiText.Get("Tüm disk yerine bir uygulama klasörü seçin."));
   if(Safety.Within(root,windows))throw new ArgumentException(UiText.Get("Windows sistem klasörü taranamaz."));
   if(!Directory.Exists(root) || limit<1)throw new ArgumentException(UiText.Get("Klasör veya tarama sınırı geçersiz."));
   Safety.CheckAncestors(root);
   var result=new ScanResult();var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var pending=new Stack<string>();pending.Push(root);
   while(pending.Count>0 && result.Paths.Count<limit){
    string dir=pending.Pop();
    try{
     if((File.GetAttributes(dir)&FileAttributes.ReparsePoint)!=0 || Safety.Within(dir,windows))continue;
     string[] files=Directory.GetFiles(dir,"*.exe",SearchOption.TopDirectoryOnly);Array.Sort(files,StringComparer.OrdinalIgnoreCase);
     foreach(string f in files){try{Safety.CheckExe(f,self,windows);if(seen.Add(f))result.Paths.Add(f);}catch(ArgumentException e){result.Warnings.Add(e.Message);}if(result.Paths.Count>=limit)break;}
     if(recursive){string[] dirs=Directory.GetDirectories(dir);Array.Sort(dirs,StringComparer.OrdinalIgnoreCase);for(int i=dirs.Length-1;i>=0;i--)pending.Push(dirs[i]);}
    }catch(UnauthorizedAccessException){result.Warnings.Add(UiText.Get("Erişilemeyen klasör atlandı: ")+dir);}catch(IOException e){result.Warnings.Add(UiText.Get("Atlandı: ")+dir+" — "+e.Message);}
   }
   if(result.Paths.Count>=limit)result.Warnings.Add(UiText.Get("Tarama sınırı: ")+limit+UiText.Get(" EXE. Daha dar bir klasör seçebilirsiniz."));
   return result;
  }
 }
 public static class BlockRules {
  public static RuleSpec[] Create(string path){path=RuleIdentity.Canonical(path);return new[]{new RuleSpec{Name=RuleIdentity.Name(path,2),Path=path,Direction=2},new RuleSpec{Name=RuleIdentity.Name(path,1),Path=path,Direction=1}};}
 }
}
