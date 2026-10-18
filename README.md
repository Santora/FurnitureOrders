# Zakázkový systém pro výrobu nábytku

Webová aplikace (Blazor WebAssembly + ASP.NET Core Web API + MS SQL Server) pro evidenci zákazníků, materiálů, operací, výrobků, zakázek a jejich položek. Součástí bude AI asistent pro přípravu zakázky z textové poptávky.

> **Stav řešení:** rozpracováno. Hotové jsou evidence zákazníků, materiálů, operací a výrobků (seznam, detail, formulář, deaktivace). Ostatní části jsou popsány v sekci [Stav a plán](#stav-a-plán).

## Požadavky pro spuštění

- .NET 10 SDK
- Microsoft SQL Server (LocalDB, instalace nebo Docker)
- Nástroj `dotnet-ef` (`dotnet tool install --global dotnet-ef`)

## Konfigurace

### Připojení k databázi

Connection string je v `FurnitureOrders.Api/appsettings.json` (klíč `ConnectionStrings:DefaultConnection`). Výchozí hodnota míří na LocalDB:

```
Server=(localdb)\MSSQLLocalDB;Database=FurnitureOrders;Trusted_Connection=True;TrustServerCertificate=True
```

Hodnotu lze přepsat bez zásahu do kódu:

- proměnnou prostředí `ConnectionStrings__DefaultConnection`, nebo
- `dotnet user-secrets` v projektu `FurnitureOrders.Api`.

### CORS a adresa API

- `FurnitureOrders.Api/appsettings.json` → `Cors:AllowedOrigins` musí obsahovat adresu klienta (výchozí `https://localhost:7200`).
- `FurnitureOrders.Client/wwwroot/appsettings.json` → `ApiBaseUrl` musí ukazovat na adresu API (výchozí `https://localhost:7222/`).

Porty se řídí soubory `Properties/launchSettings.json` v obou projektech.

## Vytvoření databáze

Struktura databáze se vytváří pomocí EF Core migrací:

```bash
dotnet ef database update --project FurnitureOrders.Api
```

## Spuštění aplikace

V solution nastavte jako startovací oba projekty (`FurnitureOrders.Api` i `FurnitureOrders.Client`) a spusťte (F5), nebo ze dvou terminálů:

```bash
dotnet run --project FurnitureOrders.Api --launch-profile https
dotnet run --project FurnitureOrders.Client --launch-profile https
```

## Testovací data

*Zatím nejsou k dispozici.* Plánuje se idempotentní seed při startu API (opakované spuštění nevytváří duplicity).

## Testování API

Soubor `FurnitureOrders.Api/FurnitureOrders.Api.http` obsahuje ruční testovací požadavky (spustitelné přímo ve Visual Studiu) pro zákazníky, materiály, operace a výrobky: vytvoření, validační chyby, duplicitní kódy, filtrování, řazení a deaktivaci. Požadavky je třeba spouštět shora dolů, protože některé používají odpověď předchozího (`# @name`). Pevné kódy (`HM001`, `HO001`, `HP001`) je před opakovaným během nutné změnit, jinak server vrátí chybu o duplicitě.

## Automatizované testy

*Zatím nejsou k dispozici.* Projekt `FurnitureOrders.Tests` (xUnit) je připraven. Spuštění: `dotnet test`.

## Architektura

Solution má čtyři projekty:

| Projekt | Odpovědnost |
|---|---|
| `FurnitureOrders.Api` | Minimal API endpointy, aplikační služby, serverová validace, EF Core (`Domain/`, `Data/`, `Services/`, `Endpoints/`) |
| `FurnitureOrders.Client` | Blazor WebAssembly, uživatelské rozhraní, klienti pro volání API |
| `FurnitureOrders.Shared` | DTO, výčtové typy a validační atributy sdílené klientem i serverem |
| `FurnitureOrders.Tests` | Automatizované testy (xUnit) |

**Zdůvodnění:** aplikace je malá, proto entity, servisní vrstva a přístup k databázi nejsou v samostatných projektech. Oddělení odpovědností je zajištěno složkami a rozhraními (`ICustomerService`). Blazor WebAssembly je samostatný klient a všechna kritická logika, validace i budoucí komunikace s AI leží na serveru, aby klíče ani pravidla nebyla součástí klientské aplikace.

**Klient:** opakující se prvky rozhraní jsou vlastní komponenty ve složce `Components/` (`SortableHeader`, `RowActions`, `ListToolbar`, `FormSection`, `FormActions`, `ErrorList`, `DetailField`, `ActiveChip`). Volání API zajišťuje generický `CrudApiClient<TList, TEdit>`, který pro každou evidenci doplňuje jen cestu a sestavení dotazu. Serverové validační chyby se do formuláře přenášejí třídou `ServerValidation`. Nová evidence se tak přidává dodáním entity, DTO, služby, endpointů a tří stránek (seznam, formulář, detail).

**Validace:** pravidla jsou definována jednou jako DataAnnotations na DTO ve `Shared`. Klient je používá pro okamžitou zpětnou vazbu, server je znovu ověřuje před uložením (`ModelValidator`). Unikátnost kódů hlídá aplikační kontrola s čitelnou hláškou a navíc unikátní index v databázi (zachytí i souběžné požadavky). Chyby ze serveru se zobrazují u příslušných polí formuláře.

## Použité knihovny třetích stran

### MudBlazor (MIT)

Komponentová knihovna pro uživatelské rozhraní (formuláře, tabulky, dialogy, layout).

**Důvod použití:**

- Zadání hodnotí především datový model, validaci, architekturu a práci s AI, ne ruční stylování. Hotové komponenty uvolnily čas právě pro tyto části.
- Pokrývá všechny prvky požadované zadáním (textové a víceřádkové pole, číselné pole, přepínač, výběrový seznam, datumové pole, výběr související entity přes Autocomplete).
- Zajišťuje konzistentní vzhled a chování napříč všemi evidencemi.
- Dobře se napojuje na DataAnnotations validaci sdílenou klientem a serverem.
- Je open source a instaluje se z veřejného NuGetu.

**Způsob použití:** opakující se části rozhraní jsou zapouzdřeny do vlastních znovupoužitelných komponent nad MudBlazorem (viz Architektura), aby aplikační kód nebyl silně svázán s jeho API.

Ostatní závislosti jsou standardní balíčky Microsoftu (EF Core, ASP.NET Core).

### Bezpečnostní poznámka k závislostem

Balíček `System.Security.Cryptography.Pkcs` je v projektu `FurnitureOrders.Api` explicitně připnut na verzi 10.0.13, protože tranzitivně přitahovaná starší verze obsahuje zranitelnost CVE-2026-70162 (GHSA-2fmv-rv2c-59x7).

## Předpoklady a rozhodnutí

Zadání v některých místech nechává volbu na řešiteli. Přijatá rozhodnutí:

- **Odstranění záznamů:** u evidencí se používá deaktivace (příznak `IsActive`), ne fyzické mazání. Deaktivovaný záznam zůstává v historických datech, ale nenabízí se pro nové záznamy.
- **Číslo zákazníka:** generuje se automaticky ze SQL sekvence ve tvaru `Z` + 5 číslic (např. `Z00001`). Lze ho zadat i ručně. Sekvence může vynechat hodnoty (např. po neúspěšném uložení), proto čísla nejsou garantovaně souvislá. Ručně zadané číslo se kontroluje na unikátnost.
- **Kontrola e-mailu:** formát se kontroluje jen u vyplněné hodnoty.
- **IČO a DIČ:** zadání nepožaduje jejich ověření, kontroluje se pouze délka.
- **Číselníky:** typ materiálu a typ zákazníka jsou výčtové typy. Pracoviště a typ operace, skupina a typ výrobku jsou volný text (zadání uvádí jen příklady). Již použitá pracoviště a skupiny výrobků se nabízejí při zadávání a ve filtrech.
- **Nepovinné číselné hodnoty:** nákupní cena, minimální objednací množství, doby operací, hodinová sazba, rozměry, hmotnost a doba dodání jsou nepovinné, ale pokud jsou vyplněny, nesmí být záporné. Prodejní cena výrobku je povinná a nezáporná.
- **Společné prvky seznamů:** všechny evidence mají stejné ovládání (hledání podle kódu a názvu, filtr stavu, řazení kliknutím na záhlaví, doplňkový filtr podle typu nebo skupiny).
- **UI** je česky, názvy v kódu anglicky.

## Stav a plán

| Oblast | Stav |
|---|---|
| Zákazníci (seznam, filtr, řazení, detail, formulář, deaktivace) | hotovo |
| Navigace | hotovo |
| Materiály, operace, výrobky (základní evidence) | hotovo |
| Technologický postup výrobku (vnořené seznamy, pořadí operací) | plánováno |
| Zakázky a položky, výpočet cen na serveru | plánováno |
| Testovací data (seed) | plánováno |
| AI asistent pro přípravu zakázky z poptávky | plánováno |
| Automatizované testy | plánováno |
| Bonus: dynamický grid řízený JSON konfigurací | plánováno |

## Známá omezení

- Řešení je rozpracované, viz tabulka výše.
- Seznamy se zatím načítají celé, bez stránkování.
- Aplikace nemá přihlášení ani řízení oprávnění (mimo rozsah zadání).
