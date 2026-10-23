using System;using System.IO;using System.Collections.Generic;
namespace PCInternetKontrolu {
 // Exact resource keys only. Paths, rule identities, arguments and OS messages are never rewritten.
 public static class UiText {
  static string language="en";
  public static string Language {get{return language;}private set{language=value;}}
  static readonly Dictionary<string,string> English=new Dictionary<string,string>{
   {"PC İnternet Kontrolü","PC Internet Control"},
   {" — GÜVENLİ DEMO"," — SAFE DEMO"},
   {"İşlem sürüyor. Bitince pencereyi kapatabilirsiniz.","An operation is running. You can close this window when it finishes."},
   {"DEMO • Hiçbir gerçek güvenlik duvarı kuralı değiştirilmez.","DEMO • No real firewall rules are changed."},
   {"EXE veya klasör seç • Ağ erişimini engelle • İstediğinde geri aç","Select EXEs or a folder • Block network access • Restore it anytime"},
   {"Güvenlik Duvarı denetleniyor...","Checking firewall..."},
   {"EXE ekle","Add EXEs"},{"Klasör tara","Scan folder"},{"Engellenenleri yükle","Load blocked apps"},{"Listeyi temizle","Clear list"},
   {"Liste temizlendi. Mevcut engeller kaldırılmadı.","List cleared. Existing blocks were not removed."},
   {"Alt klasörleri de tara","Include subfolders"},{"Tümünü işaretle","Check all"},{"Yalnız işaretli satırlar değiştirilir.","Only checked rows will be changed."},
   {"Uygulama","Application"},{"Kural durumu","Rule status"},{"Tam dosya yolu","Full file path"},{"Dosya yolunu kopyala","Copy file path"},
   {"DEMO: Seçilenleri engelle","DEMO: Block checked apps"},{"Seçilenleri engelle","Block checked apps"},{"Seçilenlerin engelini kaldır","Unblock checked apps"},
   {"Engelleme internet + yerel ağın gelen/giden trafiğine uygulanır. Kurallar uygulama kapansa da kalır.\nKlasöre yeni EXE eklenirse yeniden tara. Başka servis üzerinden bağlantı kuran yardımcıları da seç.","Blocks apply to inbound/outbound internet and local network traffic, even after this tool closes.\nRescan for new EXEs. Also select helpers that connect through other services."},
   {"Sınırlar / kullanım","Help / limitations"},
   {"• Bu sürüm masaüstü EXE dosyaları içindir.\n• Klasör taraması mevcut EXE'leri bulur; yeni dosyalar otomatik eklenmez.\n• Taşınan/kopyalanan EXE veya başka servis/yardımcı program ayrıca engellenmelidir.\n• Windows Güvenlik Duvarı açık olmalıdır. Bu araç genel ayarları değiştirmez.\n• Windows sistem dosyaları ve bu araç kendisi engellenmez.\n• İnternet ve yerel ağ bağlantısı kesilebilir; Windows'un yerel/loopback istisnaları ve başka program üzerinden bağlantılar için mutlak izolasyon garantisi yoktur.\n• Geri açma yalnız bu aracın kurallarını siler; başka kurallar bağlantıyı yine engelleyebilir.\n• Engellemeden sonra seçtiğin programı kapatıp yeniden açman önerilir.\n• Hiçbir engel otomatik uygulanmaz; önce satırları işaretleyip onay ver.","• This version supports desktop EXE files.\n• Folder scans find existing EXEs; new files are not added automatically.\n• Moved/copied EXEs and other services/helpers must be blocked separately.\n• Windows Firewall must be enabled. This tool does not change global settings.\n• Windows system files and this tool cannot be blocked.\n• Internet and local network access may be interrupted. Windows local/loopback exceptions and connections through other programs prevent a guarantee of complete isolation.\n• Unblocking removes only this tool's rules; other rules may still block access.\n• Restart the selected programs after blocking.\n• No blocks are applied automatically. Check rows and confirm first."},
   {"Kullanım ve sınırlar","Help and limitations"},
   {" dosya • "," files • "},{" işaretli"," checked"},{" • Güvenli demo"," • Safe demo"},{" • Kurallar kalıcıdır"," • Rules persist"},
   {"Güvenlik Duvarı hazır. ","Firewall ready. "},{"UYARI — Engelleme uygulanamaz. ","WARNING — Blocking cannot be applied. "},
   {"ENGELLİ","BLOCKED"},{"Kural var / etkin değil","Rule exists / inactive"},{"Kısmi / pasif kural","Partial / inactive rule"},{"Engel yok","Not blocked"},{"Denetleniyor","Checking"},
   {"Uygulama (*.exe)|*.exe","Application (*.exe)|*.exe"},{"Ağ erişimini yöneteceğin EXE dosyalarını seç","Select EXEs whose network access you want to manage"},
   {"Uygulama klasörünü seç. Henüz engelleme uygulanmaz.","Select an application folder. No blocks will be applied yet."},
   {" EXE bulundu: "," EXEs found: "},{"Klasör taranamadı","Folder scan failed"},{"Bu araçla oluşturulmuş mevcut engeller yüklendi.","Existing blocks created by this tool were loaded."},{"Güvenlik Duvarı okunamadı: ","Could not read firewall: "},
   {"Önce değiştireceğin satırları işaretle.","Check the rows you want to change first."},{"Tek işlemde en fazla 500 EXE seçin.","Select no more than 500 EXEs per operation."},{"Seçim sınırı","Selection limit"},
   {"DEMO: Gerçek kurallar değiştirilmeyecek.\n\n","DEMO: Real rules will not be changed.\n\n"},
   {" EXE için gelen ve giden ağ erişimi tüm profillerde engellenecek. İnternet ve yerel ağ bağlantısı kesilebilir. Tarayıcı/VPN/uzaktan erişim gibi programları seçtiysen bağlantın kopabilir.\n\n"," EXEs will have inbound and outbound network access blocked on all profiles. Internet and local network access may be interrupted. Selecting browsers, VPNs or remote-access programs may disconnect you.\n\n"},
   {" EXE için yalnız bu aracın oluşturduğu engeller kaldırılacak. Diğer güvenlik kuralları değişmeyecek.\n\n"," EXEs will have only this tool's blocks removed. Other security rules will not change.\n\n"},
   {"\n\nDevam edilsin mi?","\n\nContinue?"},{"Ağ erişimini engelle","Block network access"},{"Engeli kaldır","Unblock"},
   {"ENGELLENDİ: ","BLOCKED: "},{"ENGEL KALDIRILDI: ","UNBLOCKED: "},{"HATA: ","ERROR: "},
   {" dosya için işlem doğrulandı. "," files verified. "},{" hata.\n\n"," errors.\n\n"},
   {"Seçilen programları kapatıp yeniden açman önerilir. Klasöre eklenen yeni EXE'leri ayrıca tara.","Restart the selected programs. Scan separately for new EXEs added to the folder."},
   {"Yalnız bu aracın engelleri kaldırıldı; başka kurallar bağlantıyı hâlâ kısıtlayabilir.","Only this tool's blocks were removed; other rules may still restrict access."},
   {"İşlem tamamlandı","Operation complete"},{"Kısmen tamamlandı","Partially complete"},{"İşlem hatası: ","Operation error: "},{"İşlem tamamlanamadı","Operation failed"},
   {"DEMO: Örnek uygulamalar için engelleme doğrulandı. Gerçek kurallar değiştirilmedi.","DEMO: Example application blocks verified. No real rules were changed."},
   {"Etki alanı: ","Domain: "},{"açık","on"},{"KAPALI","OFF"},{" • Özel: "," • Private: "},{" • Genel: "," • Public: "},{" • Kurum politikası yerel kuralları geçersiz kılıyor."," • Organization policy overrides local rules."},
   {"Dosya yolu boş olamaz.","File path cannot be empty."},{"Geçersiz yön.","Invalid direction."},
   {"Dışarıdan değiştirilmiş/kapsamlı veya izin veren kural güvenle geri yüklenemez; hiçbir kural değiştirilmedi. Windows Güvenlik Duvarı'nda inceleyin.","An externally modified/scoped or allow rule cannot be restored safely; no rules were changed. Review it in Windows Firewall."},
   {"Bağlantı/junction içeren yol güvenlik için reddedildi; gerçek klasör ve EXE yolunu seçin.","A path containing a link/junction was rejected for safety; select the real folder and EXE path."},
   {"Yalnız EXE dosyaları seçilebilir.","Only EXE files can be selected."},{"Bu yönetim uygulaması engellenemez.","This management application cannot be blocked."},
   {"Windows sistem klasöründeki dosyalar güvenlik için engellenmez.","Files in the Windows system folder cannot be blocked for safety."},{"EXE dosyası bulunamadı: ","EXE file not found: "},
   {"Tüm disk yerine bir uygulama klasörü seçin.","Select an application folder rather than an entire drive."},{"Windows sistem klasörü taranamaz.","The Windows system folder cannot be scanned."},{"Klasör veya tarama sınırı geçersiz.","Invalid folder or scan limit."},
   {"Erişilemeyen klasör atlandı: ","Inaccessible folder skipped: "},{"Atlandı: ","Skipped: "},{"Tarama sınırı: ","Scan limit: "},{" EXE. Daha dar bir klasör seçebilirsiniz."," EXEs. You can select a smaller folder."},
   {"Güvenlik Duvarı profilleri kapalı veya kurum politikası yerel kuralları engelliyor. Genel ayarlar otomatik değiştirilmedi.","Firewall profiles are disabled or organization policy blocks local rules. Global settings were not changed automatically."},
   {"Başka bir kuralla ad çakışması var; değiştirilmedi.","Name collision with another rule; nothing was changed."},{"Engelleme kuralları doğrulanamadı.","Block rules could not be verified."},
   {" Değişiklik geri alındı."," The change was rolled back."},{" Geri alma tamamlanamadı: "," Rollback could not be completed: "},{"Bazı kurallar kaldırılamadı; durumu yenileyin.","Some rules could not be removed; refresh the status."},
   {"Uygulamaya ait olmayan kurala dokunulmadı: ","A rule not owned by this application was left unchanged: "},{"Aynı adlı birden fazla kural var; değiştirilmedi.","Multiple rules have the same name; nothing was changed."},
   {"Kural kaydı doğrulanamadı: ","Rule registration could not be verified: "},{"Kural alanları Windows'ta uyuşmuyor.","Rule fields do not match in Windows."},{"Yalnız bu uygulamanın kuralları kaldırılabilir.","Only this application's rules can be removed."},{"Kural kaldırılamadı: ","Rule could not be removed: "},
   {"Dil tercihi kaydedilemedi: ","Could not save language preference: "}
  };
  public static string Get(string key){string value;return Language=="en"&&English.TryGetValue(key,out value)?value:key;}
  public static string Key(string text){if(English.ContainsKey(text))return text;foreach(var pair in English)if(pair.Value==text)return pair.Key;return text;}
  public static void Load(string path){string value="en";try{if(File.Exists(path))value=File.ReadAllText(path).Trim();}catch(IOException){}catch(UnauthorizedAccessException){}Language=value=="tr"?"tr":"en";}
  public static void Select(string language,string path){if(language!="en"&&language!="tr")throw new ArgumentException("Unsupported language.");Language=language;if(path!=null)File.WriteAllText(path,language);}
 }
}
