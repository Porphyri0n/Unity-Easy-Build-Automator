# Proje Implementasyon Dosyası
# Otonom Sürüm Kontrol ve Derleme Yöneticisi
## AutoBuild & Version Logger

---

## 1. Sistemin Temel Amacı ve Kapsamı

Bu aracın temel amacı, Unity'nin standart **File → Build Settings** arayüzünü bir kenara bırakarak; versiyon artırımını, dosya/klasör isimlendirmesini, sürüm notlarının (changelog) toplanmasını ve tüm bu sürecin tarihçesinin tutulmasını tek bir özel editör penceresi üzerinden otomatize etmektir.

---

## 2. Mimari ve Bileşenler

Sistem, Unity Editor ortamında (`UnityEditor` kütüphanesi ile) çalışacak **4 ana C# betiğinden** oluşacaktır:

| Bileşen (Sınıf) | İşlev |
|---|---|
| `AutoBuildWindow` | Kullanıcının göreceği arayüz. Değişiklik notlarını sorar, hedef klasörü seçtirir, platformu ve yeni versiyon numarasını gösterir. |
| `VersionController` | `PlayerSettings.bundleVersion` verisini okur. Semantic Versioning (Major.Minor.Patch) mantığına göre bir sonraki sürüm numarasını hesaplar. |
| `BuildPipelineManager` | Unity'nin `BuildPipeline.BuildPlayer()` metodunu kullanarak, belirtilen özel klasör ve `.exe` isimlendirmesiyle derlemeyi gerçekleştirir. |
| `ChangelogLogger` | İki yere kayıt yapar: (1) Proje dizinine sürüm notu. (2) Tool'un kendi içine gizli/sistem dosyası olarak detaylı derleme tarihçesi (JSON). |

---

## 3. Adım Adım Çalışma Mantığı ve Arayüz Tasarımı (UI/UX)

Kullanıcı Unity üst menüsünde **Tools → Auto Build Manager** seçeneğine tıkladığında özel bir `EditorWindow` açılır.

### Pencere (Window) İçeriği

**Sürüm Bilgisi** *(Salt Okunur)*
- Mevcut Sürüm: `v1.0.1`
- Hedeflenen Yeni Sürüm: `v1.0.2` *(bu değer sistem tarafından otomatik hesaplanıp ekrana yazdırılır)*

**Platform Bilgisi**
Mevcut Unity Build ayarlarından çekilir (örn: `StandaloneWindows64`).

**Değişiklik Notları** *(TextArea)*
Geliştiricinin "Bu sürümde ne eklendi?" sorusuna cevap vereceği geniş metin kutusu.

**Hedef Dizin Seçimi** *(Browse)*
Varsayılan bir yol (örn. `C:/Builds/`) görünür. Yanındaki **"Browse"** butonuna basılarak `EditorUtility.OpenFolderPanel` ile bilgisayardan yeni klasör seçilebilir.

**[ Derlemeyi Başlat ] Butonu**
Süreci tetikler.

---

## 4. Teknik Implementasyon Detayları

### A. İsimlendirme ve Dizin Manipülasyonu

Araç, Unity'ye nereye derleyeceğini söylemeden önce seçilen hedef dizin içerisinde yeni bir klasör ve dosya yolu oluşturur.

**Formül:**
```
SecilenDizin / [OyunAdı]_[Versiyon]_[Platform] / [OyunAdı]_[Versiyon]_[Platform].exe
```

**Örnek Çalışma:**

| Alan | Değer |
|---|---|
| Oyun Adı | `DeadMargin` *(PlayerSettings'den çekilir)* |
| Yeni Sürüm | `v1.2.4` |
| Platform | `Windows64` |
| Oluşacak Klasör | `D:/Builds/DeadMargin_v1.2.4_Windows64/` |
| Oluşacak Exe | `DeadMargin_v1.2.4_Windows64.exe` |

---

### B. Unity Derleme (Build) Arayüzünü Ezmek (Bypass)

`BuildPipeline.BuildPlayer` fonksiyonu kullanılarak Unity'nin standart kayıt penceresi tamamen atlanır.

```csharp
BuildPlayerOptions buildOptions = new BuildPlayerOptions();

// Sadece Build Settings'de işaretli sahneler alınır.
buildOptions.scenes = GetEnabledScenes();

// Kendi oluşturduğumuz klasör + exe yolu
buildOptions.locationPathName = generatedFullPath;

// Mevcut platform
buildOptions.target = EditorUserBuildSettings.activeBuildTarget;

// İsteğe bağlı Development Build vs. eklenebilir.
buildOptions.options = BuildOptions.None;

BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
```

---

### C. Loglama Sistemi (Veri Yapıları)

#### 1. Dahili Tool Logu — `InternalBuildHistory.json`

Tool, her başarılı derleme sonrasında kendi içinde (`Assets/Editor/BuildLogs/` altında) bu geçmişi biriktirir.

```json
{
  "builds": [
    {
      "timestamp": "2026-06-07T17:05:00",
      "version": "v1.2.4",
      "platform": "StandaloneWindows64",
      "duration_seconds": 145,
      "build_size_mb": 450.2,
      "changelog": "Network sync problemleri giderildi, yeni lobi eklendi.",
      "status": "Success"
    }
  ]
}
```

#### 2. Kullanıcı/Proje Çıktısı — `Changelog.md`

Oyuncuların veya test ekibinin okuması için, oluşturulan derleme klasörünün doğrudan içine (`.exe`'nin yanına) veya projenin kök dizinine otomatik bir Markdown dosyası yazdırılır.

```markdown
## Sürüm: v1.2.4
**Tarih:** 07 Haziran 2026
**Platform:** StandaloneWindows64

### Yenilikler ve Değişiklikler:
- Network sync problemleri giderildi.
- Yeni lobi eklendi.
```

---

## 5. Sürecin Sonucu (Execution Flow)

```
Geliştirici kodu yazar ve testini yapar
        ↓
Özel araç penceresini açar (Tools → Auto Build Manager)
        ↓
Ne yaptığını pencereye yazar, hedef klasörü seçer
        ↓
"Derlemeyi Başlat" butonuna basar
        ↓
Sistem arka planda otomatik olarak:
   ├─ Sürümü v1.0.1 → v1.0.2'ye yükseltip PlayerSettings'e kaydeder
   ├─ İsmi formüle göre ayarlayıp seçilen dizinde klasörü açar
   ├─ Unity derleme işlemini kendi progress bar'ı ile yürütür
   ├─ Derleme bittiğinde dahili JSON geçmişini günceller
   └─ Çıktı klasörüne .exe ve Changelog.md'yi yerleştirir
        ↓
Derlemenin yapıldığı klasörü otomatik olarak ekranda açar
(EditorUtility.RevealInFinder)
```
