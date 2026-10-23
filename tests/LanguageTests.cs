using System;using System.IO;using System.Globalization;using PCInternetKontrolu;
class LanguageTests {
 static void Equal(string actual,string expected){if(actual!=expected)throw new Exception("Expected "+expected+", got "+actual);}
 static int Main(){string path=Path.Combine(Environment.GetEnvironmentVariable("TMPDIR")??Path.GetTempPath(),"language-"+Guid.NewGuid().ToString("N")+".txt");try{
  CultureInfo.CurrentUICulture=new CultureInfo("tr-TR");
  UiText.Load(path);Equal(UiText.Language,"en");Equal(UiText.Get("PC İnternet Kontrolü"),"PC Internet Control");Equal(new FirewallStatus().Summary,"Domain: on • Private: on • Public: on");
  try{RuleIdentity.Canonical("");throw new Exception("Missing validation");}catch(ArgumentException e){Equal(e.Message,"File path cannot be empty.");}
  Console.WriteLine("PASS first run defaults to English independently of OS culture; English summary and validation");
  UiText.Select("tr",path);Equal(UiText.Get("PC İnternet Kontrolü"),"PC İnternet Kontrolü");Equal(new FirewallStatus().Summary,"Etki alanı: açık • Özel: açık • Genel: açık");
  try{RuleIdentity.Canonical("");throw new Exception("Missing validation");}catch(ArgumentException e){Equal(e.Message,"Dosya yolu boş olamaz.");}
  UiText.Select("en",null);UiText.Load(path);Equal(UiText.Language,"tr");Console.WriteLine("PASS Turkish summary and validation; local preference persists");
  File.WriteAllText(path,"unsupported");UiText.Load(path);Equal(UiText.Language,"en");Console.WriteLine("PASS invalid preference falls back to English");
  string missing=Path.Combine(Path.GetDirectoryName(path),"Uygulama","Engel yok.exe");string identity=RuleIdentity.Name(missing,2);
  foreach(string language in new[]{"en","tr"}){UiText.Select(language,null);Equal(RuleIdentity.Name(missing,2),identity);Equal(RuleIdentity.Group,"PC Internet Kontrolu");try{Safety.CheckExe(missing,Path.Combine(Path.GetDirectoryName(path),"self.exe"),Path.Combine(Path.GetDirectoryName(path),"Windows"));throw new Exception("Missing validation");}catch(ArgumentException e){Equal(e.Message,(language=="en"?"EXE file not found: ":"EXE dosyası bulunamadı: ")+missing);}}
  Console.WriteLine("PASS validation translates only its label; Turkish executable path, rule identity and ownership stay exact");return 0;
 }catch(Exception e){Console.WriteLine("FAIL "+e);return 1;}finally{if(File.Exists(path))File.Delete(path);}}
}
