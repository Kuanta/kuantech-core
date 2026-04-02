# GameManager.cs Analizi

## Genel Bakış

`GameManager.cs`, `KtCore` kütüphanesinin merkezi orkestrasyon bileşenidir. Oyunun genel yaşam döngüsünü, sahneler arası geçişi ve tüm alt sistemlerin (`SubManager`) yönetimini üstlenir. Kalıcı bir Singleton deseni kullanarak tüm sahnelerde varlığını sürdürür.

## Temel Konseptler ve Mimari

### 1. Sub-Manager (Alt Yönetici) Sistemi

`GameManager`'ın en temel mimari özelliği, `SubManager` adını verdiği alt yöneticileri yönetmesidir. Bu yöneticiler ikiye ayrılır:

*   **Persistent Sub-Managers (Kalıcı Yöneticiler):** `GameManager` GameObject'ine eklenen ve oyun çalıştığı sürece var olan yöneticilerdir (`PoolManager`, `SettingsManager` vb.). `GameManager`'ın `Awake` metodunda `GetComponentsInChildren` ile bulunup oyun başında bir kez `Initialize` edilirler.
*   **Scene-Specific Sub-Managers (Sahneye Özel Yöneticiler):** Sadece belirli bir sahnede var olan yöneticilerdir. Yeni bir sahne yüklendiğinde, `GameManager` sahnedeki `SceneSubManagerContainer` objesini bularak bu yöneticileri alır ve `Initialize` eder. Sahneden ayrılırken `Cleanup` metotları çağrılır.

### 2. Yaşam Döngüsü (Lifecycle) Yönetimi

`GameManager` aşağıdaki sıralı yaşam döngüsünü yönetir:

1.  **Başlatma (`StartGame`):** Oyun başlar. Kalıcı `SubManager`'lar bulunur.
2.  **Asenkron Initialization (`InitializeSubManagers`):** Tüm kalıcı `SubManager`'ların `Initialize` metotları `UniTask.WhenAll` ile asenkron ve paralel olarak çalıştırılır. Bu, oyunun başlangıç süresini kısaltır.
3.  **Sahne Değişimi (`ChangeScene`):**
    *   Mevcut sahneye özel `SubManager`'lar temizlenir (`Cleanup`).
    *   Tüm kalıcı `SubManager`'lara sahneden ayrılma sinyali verilir (`OnSceneLeave`).
    *   Yükleme ekranı aktif edilir.
    *   `SceneManager.LoadScene` ile yeni sahne yüklenir.
4.  **Yeni Sahne (`OnNewScene`):**
    *   Yeni sahnedeki `SceneSubManagerContainer` bulunur ve sahneye özel `SubManager`'lar alınır.
    *   Bu yeni `SubManager`'lar asenkron olarak `Initialize` edilir.
    *   Tüm `SubManager`'lara (kalıcı ve sahneye özel) yeni sahneye girildiği sinyali verilir (`OnSceneEntry`).

### 3. Servis Sağlayıcı (Service Locator) Deseni

`GetSubManagerByType<T>()` metodu ile herhangi bir sistem, ihtiyaç duyduğu yöneticiye `GameManager` üzerinden erişebilir. Bu, `GameManager`'ı merkezi bir servis sağlayıcı haline getirir.

### 4. Sahneler Arası Veri Transferi

`LevelTransitionData` adında bir `abstract class` bulunur. `ChangeScene` metodu çağrılırken bu sınıftan türetilmiş bir veri objesi `GameManager`'a verilebilir. Yeni sahnedeki sistemler, `GetLevelTransitionData()` metodu ile bu veriye erişerek bir önceki sahneden bilgi alabilir.

## Olası İyileştirme ve Tartışma Noktaları

Mevcut mimari oldukça güçlü ve esnektir. Aşağıdaki noktalar, gelecekteki olası geliştirmeler için birer fikir olarak değerlendirilebilir:

*   **`GetSubManagerByType<T>` Optimizasyonu:** Bu metot, yönetici listesi üzerinde bir döngü ile çalışır (O(N) karmaşıklık). Yönetici sayısı arttıkça performansı düşebilir. `SubManager`'lar initialize edilirken bir `Dictionary<Type, SubManager>` içinde saklanırsa, bu metot O(1) karmaşıklığında çalışarak çok daha hızlı hale getirilebilir.

*   **Yönetici Bulma Yöntemleri:** `FindObjectOfType<SceneSubManagerContainer>()` metodu, özellikle büyük sahnelerde yavaş olabilir. Alternatif olarak, `SceneSubManagerContainer`'ın `Awake` metodunda kendini `GameManager`'a statik bir event veya referans ile tanıtması (kaydettirmesi) düşünülebilir. Bu, `Find` işlemine olan bağımlılığı ortadan kaldırır.

*   **Bağımlılık Enjeksiyonu (Dependency Injection):** Mevcut Service Locator deseni oldukça yaygın ve kullanışlıdır. Ancak çok büyük projelerde, sistemler arasındaki bağımlılıkları gizleyebilir. Gelecekte, Zenject veya VContainer gibi bir Dependency Injection kütüphanesi entegre etmek, test edilebilirliği ve modülerliği daha da artırabilir. Bu, mevcut yapıya göre daha karmaşık bir kurulum gerektirir.
---

## Actor Sistemi Analizi

`Actor` sistemi, oyundaki tüm dinamik varlıkların (karakterler, yaratıklar, vb.) temelini oluşturan modüler bir yapıdır.

### Temel Mimarisi

1.  **Component Deseni:** Sistem, `Actor` ve `ActorModule` sınıfları üzerine kuruludur.
    *   **`Actor.cs`:** Ana sınıftır. Bir varlığın sahip olabileceği tüm modülleri içeren bir konteyner görevi görür. Modüllerin yaşam döngüsünü (`Initialize`, `ModuleUpdate`, `Cleanup` vb.) yönetir ve onlara olayları (event) iletir.
    *   **`ActorModule.cs`:** `Actor`'a eklenebilen her bir işlevsel parçanın (hareket, sağlık, envanter vb.) türediği `abstract` temel sınıftır. Her modül kendi mantığını ve verisini yönetir.
    *   **`Character.cs`:** `Actor`'dan türeyen ve muhtemelen oyuncu, NPC gibi daha spesifik varlıklar için kullanılacak olan özelleşmiş bir sınıftır. Şu an için büyük ölçüde boştur ve genişletilmeye hazırdır.

2.  **Yaşam Döngüsü ve Modül Yönetimi:**
    *   `Actor` `Initialize` olduğunda, kendisine alt obje olarak eklenmiş tüm `ActorModule`'ları `GetComponentsInChildren` ile bulur.
    *   Bulunan modülleri hem `Type` hem de `string ModuleId` ile iki ayrı Dictionary'de saklar. Bu, modüllere hem tipine hem de ID'sine göre hızlı erişim imkanı sağlamayı hedefler.
    *   `Update`, `FixedUpdate`, `LateUpdate` gibi Unity yaşam döngüsü metotlarını kendi üzerindeki tüm modüller için sırayla çağırır.

3.  **Veri Yönetimi ve Serialization:**
    *   Sistem, veri odaklı bir tasarıma sahiptir. `Actor` ve `ActorModule`'ların durumu, `ActorSerializableData` ve `ActorModuleSerializableData` sınıfları aracılığıyla kaydedilip yüklenebilir.
    *   `Actor`, state yönetimini `GetActorState` ve `LoadActorState` metotları ile yönetir. Bu metotlar, her bir modülün kendi state'ini oluşturmasını veya yüklemesini tetikler. `ModuleId` string'i, save dosyasındaki verinin doğru modüle eşleştirilmesi için anahtar olarak kullanılır.

4.  **Olay Yönelimli (Event-Driven) Tasarım:**
    *   `Actor` sınıfı, `OnDeathEvent`, `OnHitEvent`, `OnActorStateChanged` gibi `UnityAction` event'lerini yoğun olarak kullanır. Bu sayede modüller ve diğer sistemler, `Actor`'ın iç işleyişini bilmeden durum değişikliklerine tepki verebilir. Bu, sistemler arası bağımlılığı (coupling) azaltan güçlü bir yöntemdir.

### Olası İyileştirme ve Tartışma Noktaları

*   **ÇOK ÖNEMLİ - `GetModule<T>` Performansı:** `GetModule<T>` ve `GetModules<T>` metotları, `Modules` dictionary'si üzerinde `foreach` döngüsü ile çalışmaktadır. Bu, modüllere tipine göre erişimde ciddi bir performans sorununa yol açar. Dictionary'nin anahtarı `Type` olmasına rağmen, `is T` kontrolü ile lineer bir arama yapılmaktadır.
    *   **Çözüm Önerisi:** `Modules` dictionary'si `Dictionary<Type, List<ActorModule>>` olarak zaten doğru bir şekilde tanımlanmış. `GetModule<T>` metodu şu şekilde basitleştirilip O(1) hızına çıkarılabilir:
      ```csharp
      public T GetModule<T>() where T : ActorModule
      {
          if (Modules.TryGetValue(typeof(T), out var moduleList) && moduleList.Count > 0)
          {
              return moduleList[0] as T;
          }
          return null;
      }
      ```
      Bu değişiklik, oyun içinde sıkça çağrılması muhtemel bu fonksiyonda ciddi bir performans artışı sağlayacaktır.

*   **Handler vs. Modül Tutarlılığı:** `Actor` sınıfı, `FactionHandler` ve `MotionVectorsHandler` gibi bazı bileşenlere doğrudan referans tutarken, `VisualHandler`'ı `GetModule<ActorVisualHandler>()` ile almaktadır. Bu "Handler"ların da standart bir `ActorModule` olup `GetModule` ile alınması, sistemin tutarlılığını artırabilir. Eğer performans kritikse, `Initialize` sırasında `GetModule` ile alınıp bir değişkende saklanabilirler.

*   **Modül Başlatma (Initialization):** `Actor` `Initialize` olurken `GetComponentsInChildren` çağrısı yapar. Eğer bu `Actor`'lar bir object pool'dan sık sık spawn ediliyorsa, bu çağrı gereksiz yere tekrarlanabilir. Mevcut kodda `Spawn` metodu `Initialized` bayrağını kontrol edip sadece ilk seferde `Initialize` çağırdığı için bu bir sorun değil gibi görünüyor, ancak yine de akılda tutulması gereken bir performans detayıdır.
