[English](README.md) · **Türkçe**

# PC İnternet Kontrolü 🛡️

[![Windows build](https://github.com/rotesham/pc-internet-control/actions/workflows/build.yml/badge.svg)](https://github.com/rotesham/pc-internet-control/actions/workflows/build.yml)
![C#](https://img.shields.io/badge/C%23-.NET_Framework_4.8-512BD4)
[![MIT License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

**Windows masaüstü uygulamaları için EXE bazlı ağ erişimi yönetimi.** Bir EXE seçin veya klasördeki mevcut EXE'leri tarayın; yalnız seçtiğiniz dosyalar için blok kuralları oluşturun.

[⬇️ EXE sürümünü indir](https://github.com/rotesham/pc-internet-control/releases/latest) · [Kullanım](#kullanım) · [Sınırlar](#önemli-sınırlar) · [Güvenlik](SECURITY.md)

![Sahte kurallarla güvenli demo ekranı](docs/screenshot.png)

## Özellikler

- Çoklu **EXE ekleme**, klasör ve isteğe bağlı alt klasör tarama.
- Gelen ve giden trafik için **tüm ağ profillerinde** uygulama blok kuralları.
- Kalıcı kurallar; uygulamayı kapatmak veya bilgisayarı yeniden başlatmak engelleri silmez.
- Engellenenleri yeniden yükleme ve **yalnız bu aracın kurallarını** kaldırma.
- İşlem sonrası Windows'tan geri okuma, kısmi/pasif durum gösterimi ve başarısız çift-kural işleminde geri alma.
- Windows sistem klasörü ve yönetim uygulamasının kendisi için koruma.
- Genel güvenlik duvarı ayarlarını değiştirmez, profilleri kapatmaz veya başka kuralları sıfırlamaz.

## Dil seçimi

Varsayılan dil İngilizcedir. Uygulamadaki **English / Türkçe** seçicisinden dili değiştirebilirsiniz. Windows'un yerel dosya/seçim ve UAC düğmeleri sistem dilinde kalabilir.

## Kullanım

1. Releases ZIP'ini çıkarın ve `PCInternetKontrolu.exe` dosyasını açın.
2. Güvenlik Duvarı yönetimi için Windows'un **yönetici izni** gerekir; yalnız güvendiğiniz sürüme izin verin.
3. **EXE ekle** veya **Klasör tara** ile dosyaları listeleyin. Bu adım henüz engellemez.
4. İlgili satırları işaretleyip **Seçilenleri engelle** düğmesine basın; son onayda yolları kontrol edin.
5. Seçtiğiniz programı kapatıp yeniden açmanız önerilir.

**Geri açma:** `Engellenenleri yükle → satırları işaretle → Seçilenlerin engelini kaldır`.

**Listeyi temizle**, yalnız ekrandaki listeyi temizler; mevcut blok kurallarını kaldırmaz.

## Önemli sınırlar

> Bu araç ağsız sanal makine veya tam uygulama izolasyonu yerine geçmez. “Her koşulda hiçbir bağlantı mümkün değil” garantisi vermez.

- **İnternet ve yerel ağ** bağlantıları etkilenir. Yalnız interneti kesip LAN'ı koruyan mod yoktur.
- Kurallar **tam EXE yoluna** uygulanır. Klasör, joker karakterli canlı bir kural değildir; yalnız tarama anındaki dosyalar bulunur.
- Yeni eklenen, taşınan veya yeni sürüm klasörüne kurulan EXE'ler için yeniden tarayıp uygulayın. Tarama sınırı 500 EXE'dir; atlanan dosyalar günlüğe yazılır.
- Başka servis, tarayıcı veya yardımcı süreç üzerinden bağlantı kuran uygulamanın yardımcıları ayrıca değerlendirilmelidir. Loopback/yerel istisnalar olabilir.
- Yönetici yetkisine sahip yazılımlar veya kurum politikaları kuralları değiştirebilir/geçersiz kılabilir. Güvenlik Duvarı açık olmalıdır.
- Bu sürüm klasik masaüstü EXE'leri içindir; DLL, komut dosyası veya Store paket kimliği bazlı yönetim sunmaz.
- Tarayıcı/VPN/uzaktan erişim programını engellemek bağlantınızı kesebilir. Yolları dikkatle seçin.
- Uygulamayı silmek kuralları silmez. Önce kendi engellerinizi kaldırın veya Windows Güvenlik Duvarı'nda `PC Internet Kontrolu` grubunu inceleyin.

## Derleme ve test

Windows + .NET Framework C# derleyicisi gerekir; dış paket veya NuGet bağımlılığı yoktur. Windows 11 üzerinde yerel olarak test edilmiştir.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

Çıktı: `build\PCInternetKontrolu.exe`. Varsayılan testler gerçek kuralları değiştirmez.

```powershell
# Açıkça seçilen, yönetici yetkisi gerektiren canlı entegrasyon testi:
# yalnız ayrı FirewallProbe.exe engellenir ve test sonunda kuralları kaldırılır.
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -RunIntegration

# Güvenli görsel demo
.\build\PCInternetKontrolu.exe --demo
```

Canlı test: ayrı test EXE'si ile önce TCP bağlantısı kurulur, yalnız onun kuralları eklenir, yeni bağlantının engellendiği doğrulanır; kurallar kaldırılıp bağlantının geri geldiği kontrol edilir. Ağ koşulları testi etkileyebilir. Gerçek kullanıcı programları test hedefi yapılmaz.

## Gizlilik

Telemetri veya hesap girişi yoktur. Seçilen yollar ve sonuçlar yerel `%LOCALAPPDATA%\PCInternetKontrolu\islem.log` dosyasına yazılır; bu günlükleri herkese açık paylaşmayın. Canlı entegrasyon testi açıkça seçilirse bir genel internet uç noktasına bağlantı dener. Demo ekran görüntüsündeki yollar örnektir.

## English summary

An administrator Windows Firewall manager for selected executable paths. Adds inbound/outbound block rules across profiles, verifies changes and removes only its own rules. Folder mode scans existing EXEs; it is not a live folder policy. It blocks LAN traffic too and cannot guarantee isolation from helper services or alternate executable paths. Default tests use a fake firewall backend.

## Lisans

[MIT](LICENSE) · rotesham. İmzasız yazılım; inceleme ve testler mutlak güvenlik garantisi değildir.
