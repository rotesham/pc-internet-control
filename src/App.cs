using System;using System.Drawing;using System.Windows.Forms;using System.IO;using System.Linq;using System.Collections.Generic;using System.Threading.Tasks;using System.Text;
namespace PCInternetKontrolu {
 public sealed class MainForm:Form {
  readonly IFirewallBackend backend;readonly FirewallManager manager;readonly bool demo,smoke;
  readonly string self=Application.ExecutablePath,windows=Environment.GetFolderPath(Environment.SpecialFolder.Windows);
  readonly Color navy=Color.FromArgb(22,39,60),muted=Color.FromArgb(85,102,121),green=Color.FromArgb(26,125,90),red=Color.FromArgb(168,52,48);
  readonly string data=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"PCInternetKontrolu");
  ListView list;Label firewallLabel,footer;CheckBox subfolders,selectAll;TextBox log;Button block,unblock;bool busy;ComboBox languageSelector;readonly string preferencePath;
  readonly List<Action> translations=new List<Action>();
  sealed class LogEntry {public string Time;public Func<string> Text;}
  readonly List<LogEntry> logEntries=new List<LogEntry>();
  public MainForm(IFirewallBackend backend,bool demo,bool smoke,string preferencePath=null){
   this.backend=backend;this.demo=demo;this.smoke=smoke;manager=new FirewallManager(backend,self,windows);Directory.CreateDirectory(data);this.preferencePath=preferencePath??Path.Combine(data,smoke?"ui-language-"+Guid.NewGuid().ToString("N")+".txt":"language.txt");UiText.Load(this.preferencePath);
   Text=UiText.Get("PC İnternet Kontrolü")+(demo?UiText.Get(" — GÜVENLİ DEMO"):"");ClientSize=new Size(950,800);MinimumSize=new Size(966,839);MaximumSize=new Size(966,839);MaximizeBox=false;StartPosition=FormStartPosition.CenterScreen;BackColor=Color.FromArgb(240,244,248);Font=new Font("Segoe UI",10);AutoScaleMode=AutoScaleMode.Dpi;Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);
   Build();LoadManaged();
   FormClosing+=delegate(object s,FormClosingEventArgs e){if(busy){e.Cancel=true;footer.Text=UiText.Get("İşlem sürüyor. Bitince pencereyi kapatabilirsiniz.");}};
   if(smoke)Shown+=delegate{BeginInvoke(new Action(RunSmoke));};
  }
  Label L(Control parent,string text,int x,int y,int w,int h,float size,Color color,bool bold){var label=new Label{Text=text,ForeColor=color,Font=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular)};label.SetBounds(x,y,w,h);parent.Controls.Add(label);Bind(label,text);return label;}
  Button B(string text,int x,int y,int w,Color bg,Color fg){var b=new Button{Text=text,BackColor=bg,ForeColor=fg,FlatStyle=FlatStyle.Flat,Font=new Font("Segoe UI",10,FontStyle.Bold),Cursor=Cursors.Hand};b.FlatAppearance.BorderSize=0;b.SetBounds(x,y,w,38);Controls.Add(b);Bind(b,text);return b;}
  void Bind(Control control,string text){string key=UiText.Key(text);translations.Add(()=>control.Text=UiText.Get(key));control.Text=UiText.Get(key);}
  void ApplyLanguage(){
   Text=UiText.Get("PC İnternet Kontrolü")+(demo?UiText.Get(" — GÜVENLİ DEMO"):"");
   foreach(var translate in translations)translate();
   list.Columns[0].Text=UiText.Get("Uygulama");list.Columns[1].Text=UiText.Get("Kural durumu");list.Columns[2].Text=UiText.Get("Tam dosya yolu");
   list.ContextMenuStrip.Items[0].Text=UiText.Get("Dosya yolunu kopyala");RenderLog();
   try{RefreshStates();}catch(Exception e){firewallLabel.Text=UiText.Get("Güvenlik Duvarı okunamadı: ")+e.Message;firewallLabel.ForeColor=red;}
  }
  void Build(){
   var header=new Panel{BackColor=navy};header.SetBounds(0,0,950,94);Controls.Add(header);
   L(header,UiText.Get("PC İnternet Kontrolü"),24,15,700,39,24,Color.White,true);
   languageSelector=new ComboBox{Name="languageSelector",DropDownStyle=ComboBoxStyle.DropDownList,AccessibleName="Language / Dil"};languageSelector.Items.AddRange(new object[]{"English","Türkçe"});languageSelector.SetBounds(758,25,168,28);header.Controls.Add(languageSelector);languageSelector.SelectedIndex=UiText.Language=="tr"?1:0;
   languageSelector.SelectedIndexChanged+=delegate{if(busy)return;try{UiText.Select(languageSelector.SelectedIndex==1?"tr":"en",preferencePath);}catch(IOException e){Append(()=>UiText.Get("Dil tercihi kaydedilemedi: ")+e.Message);}catch(UnauthorizedAccessException e){Append(()=>UiText.Get("Dil tercihi kaydedilemedi: ")+e.Message);}ApplyLanguage();};
   L(header,demo?UiText.Get("DEMO • Hiçbir gerçek güvenlik duvarı kuralı değiştirilmez."):UiText.Get("EXE veya klasör seç • Ağ erişimini engelle • İstediğinde geri aç"),26,60,890,24,10,Color.FromArgb(185,208,227),false);
   firewallLabel=L(this,UiText.Get("Güvenlik Duvarı denetleniyor..."),24,108,902,44,10,muted,true);
   var add=B(UiText.Get("EXE ekle"),24,162,174,Color.White,navy);add.Click+=delegate{PickFiles();};
   var folder=B(UiText.Get("Klasör tara"),210,162,174,Color.White,navy);folder.Click+=async delegate{await PickFolder();};
   var load=B(UiText.Get("Engellenenleri yükle"),396,162,250,Color.White,navy);load.Click+=delegate{LoadManaged();};
   var clear=B(UiText.Get("Listeyi temizle"),658,162,268,Color.White,navy);clear.Click+=delegate{list.Items.Clear();selectAll.Checked=false;UpdateButtons();Append(()=>UiText.Get("Liste temizlendi. Mevcut engeller kaldırılmadı."));};
   subfolders=new CheckBox{Text=UiText.Get("Alt klasörleri de tara"),Checked=true};subfolders.SetBounds(24,209,245,27);Controls.Add(subfolders);Bind(subfolders,UiText.Get("Alt klasörleri de tara"));
   selectAll=new CheckBox{Text=UiText.Get("Tümünü işaretle")};selectAll.SetBounds(290,209,220,27);Controls.Add(selectAll);Bind(selectAll,UiText.Get("Tümünü işaretle"));selectAll.CheckedChanged+=delegate{foreach(ListViewItem item in list.Items)item.Checked=selectAll.Checked;UpdateButtons();};
   L(this,UiText.Get("Yalnız işaretli satırlar değiştirilir."),540,211,386,26,9,muted,false);
   list=new ListView{View=View.Details,FullRowSelect=true,CheckBoxes=true,GridLines=false,HideSelection=false,MultiSelect=true,BackColor=Color.White,BorderStyle=BorderStyle.FixedSingle};list.SetBounds(24,245,902,276);list.Columns.Add(UiText.Get("Uygulama"),190);list.Columns.Add(UiText.Get("Kural durumu"),180);list.Columns.Add(UiText.Get("Tam dosya yolu"),510);Controls.Add(list);list.ItemChecked+=delegate{if(IsHandleCreated&&!busy)BeginInvoke(new Action(UpdateButtons));};
   var menu=new ContextMenuStrip();menu.Items.Add(UiText.Get("Dosya yolunu kopyala"),null,delegate{if(list.SelectedItems.Count>0)Clipboard.SetText((string)list.SelectedItems[0].Tag);});list.ContextMenuStrip=menu;
   block=B(demo?UiText.Get("DEMO: Seçilenleri engelle"):UiText.Get("Seçilenleri engelle"),24,537,439,red,Color.White);block.Click+=async delegate{await Apply(true,false);};
   unblock=B(UiText.Get("Seçilenlerin engelini kaldır"),487,537,439,Color.FromArgb(219,234,227),green);unblock.Click+=async delegate{await Apply(false,false);};
   L(this,UiText.Get("Engelleme internet + yerel ağın gelen/giden trafiğine uygulanır. Kurallar uygulama kapansa da kalır.\nKlasöre yeni EXE eklenirse yeniden tara. Başka servis üzerinden bağlantı kuran yardımcıları da seç."),24,587,902,46,9,muted,false);
   log=new TextBox{ReadOnly=true,Multiline=true,ScrollBars=ScrollBars.Vertical,BackColor=Color.White,BorderStyle=BorderStyle.FixedSingle,Font=new Font("Consolas",9)};log.SetBounds(24,643,902,91);Controls.Add(log);
   footer=L(this,"",24,753,702,31,9,muted,false);
   var help=B(UiText.Get("Sınırlar / kullanım"),758,746,168,navy,Color.White);help.Click+=delegate{MessageBox.Show(this,UiText.Get("• Bu sürüm masaüstü EXE dosyaları içindir.\n• Klasör taraması mevcut EXE'leri bulur; yeni dosyalar otomatik eklenmez.\n• Taşınan/kopyalanan EXE veya başka servis/yardımcı program ayrıca engellenmelidir.\n• Windows Güvenlik Duvarı açık olmalıdır. Bu araç genel ayarları değiştirmez.\n• Windows sistem dosyaları ve bu araç kendisi engellenmez.\n• İnternet ve yerel ağ bağlantısı kesilebilir; Windows'un yerel/loopback istisnaları ve başka program üzerinden bağlantılar için mutlak izolasyon garantisi yoktur.\n• Geri açma yalnız bu aracın kurallarını siler; başka kurallar bağlantıyı yine engelleyebilir.\n• Engellemeden sonra seçtiğin programı kapatıp yeniden açman önerilir.\n• Hiçbir engel otomatik uygulanmaz; önce satırları işaretleyip onay ver."),UiText.Get("Kullanım ve sınırlar"),MessageBoxButtons.OK,MessageBoxIcon.Information);};
  }
  void RenderLog(){log.Text=string.Join(Environment.NewLine,logEntries.Select(e=>e.Time+"  "+e.Text()))+(logEntries.Count==0?"":Environment.NewLine);log.SelectionStart=log.TextLength;log.ScrollToCaret();}
  void Append(Func<string> text){var entry=new LogEntry{Time=DateTime.Now.ToString("HH:mm:ss"),Text=text};logEntries.Add(entry);string line=entry.Time+"  "+text();log.AppendText(line+Environment.NewLine);try{File.AppendAllText(Path.Combine(data,demo?"demo.log":"islem.log"),DateTime.Now.ToString("yyyy-MM-dd ")+line+Environment.NewLine,Encoding.UTF8);}catch(IOException){}catch(UnauthorizedAccessException){}}
  void UpdateButtons(){if(block==null)return;bool any=list.CheckedItems.Count>0;block.Enabled=unblock.Enabled=!busy&&any;footer.Text=list.Items.Count+UiText.Get(" dosya • ")+list.CheckedItems.Count+UiText.Get(" işaretli")+(demo?UiText.Get(" • Güvenli demo"):UiText.Get(" • Kurallar kalıcıdır"));}
  void SetBusy(bool value){busy=value;foreach(Control c in Controls)if(c is Button)c.Enabled=!value;list.Enabled=subfolders.Enabled=selectAll.Enabled=languageSelector.Enabled=!value;UseWaitCursor=value;UpdateButtons();}
  void RefreshStates(){var status=backend.Status();firewallLabel.Text=(status.CanApply?UiText.Get("Güvenlik Duvarı hazır. "):UiText.Get("UYARI — Engelleme uygulanamaz. "))+status.Summary;firewallLabel.ForeColor=status.CanApply?green:red;var rules=manager.Managed();foreach(ListViewItem item in list.Items){var state=manager.StateFrom(rules,(string)item.Tag);item.SubItems[1].Text=state==BlockState.Blocked?(status.CanApply?UiText.Get("ENGELLİ"):UiText.Get("Kural var / etkin değil")):state==BlockState.Partial?UiText.Get("Kısmi / pasif kural"):UiText.Get("Engel yok");item.SubItems[1].ForeColor=state==BlockState.Blocked?green:state==BlockState.Partial?red:muted;item.UseItemStyleForSubItems=false;}UpdateButtons();}
  void AddPaths(IEnumerable<string> paths,bool validate,bool check){foreach(string value in paths){string path=RuleIdentity.Canonical(value);if(validate)Safety.CheckExe(path,self,windows);var old=list.Items.Cast<ListViewItem>().FirstOrDefault(i=>((string)i.Tag).Equals(path,StringComparison.OrdinalIgnoreCase));if(old!=null){if(check)old.Checked=true;continue;}var item=new ListViewItem(Path.GetFileName(path));item.SubItems.Add(UiText.Get("Denetleniyor"));item.SubItems.Add(path);item.Tag=path;item.Checked=check;list.Items.Add(item);}RefreshStates();}
  void PickFiles(){using(var dialog=new OpenFileDialog{Filter=UiText.Get("Uygulama (*.exe)|*.exe"),Multiselect=true,Title=UiText.Get("Ağ erişimini yöneteceğin EXE dosyalarını seç")}){if(dialog.ShowDialog(this)!=DialogResult.OK)return;foreach(string path in dialog.FileNames){try{AddPaths(new[]{path},true,true);}catch(Exception e){Append(()=>e.Message);}}}}
  async Task PickFolder(){using(var dialog=new FolderBrowserDialog{Description=UiText.Get("Uygulama klasörünü seç. Henüz engelleme uygulanmaz."),ShowNewFolderButton=false}){if(dialog.ShowDialog(this)!=DialogResult.OK)return;string root=dialog.SelectedPath;bool recursive=subfolders.Checked;SetBusy(true);try{var result=await Task.Run(()=>FolderScanner.Scan(root,recursive,self,windows,500));AddPaths(result.Paths,false,true);Append(()=>result.Paths.Count+UiText.Get(" EXE bulundu: ")+root);foreach(string warning in result.Warnings.Take(8))Append(()=>warning);}catch(Exception e){Append(()=>e.Message);MessageBox.Show(this,e.Message,UiText.Get("Klasör taranamadı"),MessageBoxButtons.OK,MessageBoxIcon.Warning);}finally{SetBusy(false);}}}
  void LoadManaged(){try{AddPaths(manager.Managed().Select(r=>r.Path).Distinct(StringComparer.OrdinalIgnoreCase),false,false);Append(()=>UiText.Get("Bu araçla oluşturulmuş mevcut engeller yüklendi."));}catch(Exception e){firewallLabel.Text=UiText.Get("Güvenlik Duvarı okunamadı: ")+e.Message;firewallLabel.ForeColor=red;Append(()=>e.Message);}}
  sealed class ChangeResult {public string Path,Error;}
  async Task Apply(bool blocking,bool automated){
   if(automated&&(!demo||!(backend is FakeFirewallBackend)))throw new InvalidOperationException("Automated UI changes require the fake firewall.");
   var paths=list.CheckedItems.Cast<ListViewItem>().Select(i=>(string)i.Tag).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
   if(paths.Count==0){footer.Text=UiText.Get("Önce değiştireceğin satırları işaretle.");return;}
   if(paths.Count>500){MessageBox.Show(this,UiText.Get("Tek işlemde en fazla 500 EXE seçin."),UiText.Get("Seçim sınırı"));return;}
   if(!automated){string text=(demo?UiText.Get("DEMO: Gerçek kurallar değiştirilmeyecek.\n\n"):"")+(blocking?paths.Count+UiText.Get(" EXE için gelen ve giden ağ erişimi tüm profillerde engellenecek. İnternet ve yerel ağ bağlantısı kesilebilir. Tarayıcı/VPN/uzaktan erişim gibi programları seçtiysen bağlantın kopabilir.\n\n"):paths.Count+UiText.Get(" EXE için yalnız bu aracın oluşturduğu engeller kaldırılacak. Diğer güvenlik kuralları değişmeyecek.\n\n"))+string.Join("\n",paths.Take(5))+(paths.Count>5?"\n...":"")+UiText.Get("\n\nDevam edilsin mi?");if(MessageBox.Show(this,text,blocking?UiText.Get("Ağ erişimini engelle"):UiText.Get("Engeli kaldır"),MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return;}
   SetBusy(true);
   try{
    var results=await Task.Run(()=>{var output=new List<ChangeResult>();foreach(string path in paths){try{if(blocking)manager.Block(path);else manager.Unblock(path);output.Add(new ChangeResult{Path=path});}catch(Exception e){output.Add(new ChangeResult{Path=path,Error=e.Message});}}return output;});
    foreach(var result in results)Append(()=>(result.Error==null?(blocking?UiText.Get("ENGELLENDİ: "):UiText.Get("ENGEL KALDIRILDI: ")):UiText.Get("HATA: "))+result.Path+(result.Error==null?"":" — "+result.Error));
    RefreshStates();int errors=results.Count(r=>r.Error!=null);int good=results.Count-errors;
    if(errors>0&&automated)throw new InvalidOperationException(string.Join("; ",results.Where(r=>r.Error!=null).Select(r=>r.Error)));
    if(!automated)MessageBox.Show(this,good+UiText.Get(" dosya için işlem doğrulandı. ")+errors+UiText.Get(" hata.\n\n")+(blocking?UiText.Get("Seçilen programları kapatıp yeniden açman önerilir. Klasöre eklenen yeni EXE'leri ayrıca tara."):UiText.Get("Yalnız bu aracın engelleri kaldırıldı; başka kurallar bağlantıyı hâlâ kısıtlayabilir.")),errors==0?UiText.Get("İşlem tamamlandı"):UiText.Get("Kısmen tamamlandı"),MessageBoxButtons.OK,errors==0?MessageBoxIcon.Information:MessageBoxIcon.Warning);
   }catch(Exception e){Append(()=>UiText.Get("İşlem hatası: ")+e.Message);if(automated)throw;MessageBox.Show(this,e.Message,UiText.Get("İşlem tamamlanamadı"),MessageBoxButtons.OK,MessageBoxIcon.Error);}
   finally{SetBusy(false);}
  }
  string RuleFingerprint(){return string.Join("\n",backend.Snapshot().OrderBy(r=>r.Name).Select(r=>string.Join("|",typeof(RuleSpec).GetFields().Select(f=>{object v=f.GetValue(r);return v is string[]?string.Join(",",(string[])v):Convert.ToString(v);})))) ;}
  void SmokeLanguage(int index){
   var fake=(FakeFirewallBackend)backend;int puts=fake.PutCalls,deletes=fake.DeleteCalls;string before=RuleFingerprint();
   string[] paths=list.Items.Cast<ListViewItem>().Select(i=>(string)i.Tag).ToArray();bool[] checks=list.Items.Cast<ListViewItem>().Select(i=>i.Checked).ToArray();
   languageSelector.SelectedIndex=index;
   if(fake.PutCalls!=puts||fake.DeleteCalls!=deletes||before!=RuleFingerprint())throw new Exception("Language switch wrote firewall state");
   if(!paths.SequenceEqual(list.Items.Cast<ListViewItem>().Select(i=>(string)i.Tag))||!checks.SequenceEqual(list.Items.Cast<ListViewItem>().Select(i=>i.Checked)))throw new Exception("Language switch changed paths or selection");
   bool english=index==0;
   if(Text!=(english?"PC Internet Control — SAFE DEMO":"PC İnternet Kontrolü — GÜVENLİ DEMO")||list.Columns[0].Text!=(english?"Application":"Uygulama")||list.Columns[1].Text!=(english?"Rule status":"Kural durumu")||list.Columns[2].Text!=(english?"Full file path":"Tam dosya yolu"))throw new Exception("Localized title/columns failed");
   if(block.Text!=(english?"DEMO: Block checked apps":"DEMO: Seçilenleri engelle")||unblock.Text!=(english?"Unblock checked apps":"Seçilenlerin engelini kaldır")||subfolders.Text!=(english?"Include subfolders":"Alt klasörleri de tara")||selectAll.Text!=(english?"Check all":"Tümünü işaretle")||list.ContextMenuStrip.Items[0].Text!=(english?"Copy file path":"Dosya yolunu kopyala"))throw new Exception("Localized controls failed");
   if(!firewallLabel.Text.Contains(english?"Domain: on":"Etki alanı: açık")||!footer.Text.Contains(english?" checked":" işaretli"))throw new Exception("Localized summaries failed");
  }
  async void RunSmoke(){string root=Path.Combine(Environment.GetEnvironmentVariable("TMPDIR") ?? Path.GetTempPath(),"PCNet-ui-"+Guid.NewGuid().ToString("N"));string report=Path.Combine(data,"ui-test.txt");try{
   if(!(backend is FakeFirewallBackend)||!demo)throw new Exception("Unsafe test backend");
   if(!Text.StartsWith("PC Internet Control")||UiText.Language!="en")throw new Exception("First run did not default to English");
   if(Controls.Find("languageSelector",true).OfType<ComboBox>().SingleOrDefault()==null)throw new Exception("Language selector is missing");
   SmokeLanguage(1);SmokeLanguage(0);
   Directory.CreateDirectory(root);Directory.CreateDirectory(Path.Combine(root,"helpers"));File.WriteAllText(Path.Combine(root,"DemoEditor.exe"),"fixture");File.WriteAllText(Path.Combine(root,"helpers","DemoUpdater.exe"),"fixture");
   var scan=FolderScanner.Scan(root,true,self,windows,500);AddPaths(scan.Paths,true,true);AddPaths(scan.Paths,true,true);if(list.Items.Count!=2)throw new Exception("Folder scan/dedup failed");
   foreach(int language in new[]{0,1}){
    SmokeLanguage(language);
    if(list.Items.Cast<ListViewItem>().Any(i=>i.SubItems[1].Text!=UiText.Get("Engel yok")))throw new Exception("Localized unblocked status failed");
    await Apply(true,true);if(manager.Managed().Count!=4||list.Items.Cast<ListViewItem>().Any(i=>i.SubItems[1].Text!=UiText.Get("ENGELLİ")))throw new Exception("UI block flow failed");
    if(!log.Text.Contains(UiText.Get("ENGELLENDİ: ")))throw new Exception("Localized operation log failed");
    SmokeLanguage(1-language);if(list.Items.Cast<ListViewItem>().Any(i=>i.SubItems[1].Text!=UiText.Get("ENGELLİ"))||!log.Text.Contains(UiText.Get("ENGELLENDİ: ")))throw new Exception("Existing statuses/log labels did not switch");SmokeLanguage(language);
    var restored=new MainForm(backend,true,false,preferencePath);if(restored.list.Items.Count!=2||restored.languageSelector.SelectedIndex!=language)throw new Exception("Rules/language preference reload failed");restored.Dispose();
    var fake=(FakeFirewallBackend)backend;fake.Current.Public=false;RefreshStates();if(list.Items.Cast<ListViewItem>().Any(i=>i.SubItems[1].Text!=UiText.Get("Kural var / etkin değil"))||!firewallLabel.Text.Contains(UiText.Get("KAPALI")))throw new Exception("Localized inactive status failed");fake.Current.Public=true;
    var rule=fake.Rules.Values.First();rule.UnsupportedSettings=true;RefreshStates();if(!list.Items.Cast<ListViewItem>().Any(i=>i.SubItems[1].Text==UiText.Get("Kısmi / pasif kural")))throw new Exception("Localized partial status failed");rule.UnsupportedSettings=false;RefreshStates();
    foreach(ListViewItem item in list.Items)item.SubItems[2].Text=@"C:\ExampleApps\"+Path.GetFileName((string)item.Tag);
    logEntries.Clear();log.Clear();Append(()=>UiText.Get("DEMO: Örnek uygulamalar için engelleme doğrulandı. Gerçek kurallar değiştirilmedi."));
    using(var image=new Bitmap(Width,Height)){DrawToBitmap(image,new Rectangle(0,0,Width,Height));image.Save(Path.Combine(data,language==0?"ui-preview.png":"ui-preview-tr.png"),System.Drawing.Imaging.ImageFormat.Png);}
    await Apply(false,true);if(manager.Managed().Count!=0||list.Items.Cast<ListViewItem>().Any(i=>i.SubItems[1].Text!=UiText.Get("Engel yok")))throw new Exception("UI unblock flow failed");
   }
   SmokeLanguage(0);
   File.WriteAllText(report,"PASS English default; English/Turkish controls, title, columns, statuses, summaries and log labels; persisted language; language switch preserves paths, checks and every rule field with zero Put/Delete calls; folder scan/dedup, block two EXEs, persisted-rule reload and unblock in both languages; fake firewall only.",Encoding.UTF8);
  }catch(Exception e){File.WriteAllText(report,"FAIL "+e,Encoding.UTF8);Environment.ExitCode=1;}finally{busy=false;if(Directory.Exists(root))Directory.Delete(root,true);if(File.Exists(preferencePath))File.Delete(preferencePath);Close();}}

 }
 public static class Program {
  [STAThread] public static void Main(string[] args){bool smoke=Array.IndexOf(args,"--smoke")>=0;bool demo=smoke||Array.IndexOf(args,"--demo")>=0;Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new MainForm(demo?(IFirewallBackend)new FakeFirewallBackend():new WindowsFirewallBackend(),demo,smoke));}
 }
}
