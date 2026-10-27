# Zakázkový systém pro výrobu nábytku

Webová aplikace (Blazor WebAssembly + ASP.NET Core Web API + MS SQL Server) pro evidenci zákazníků, materiálů, operací, výrobků, zakázek a jejich položek. Součástí je AI asistent pro přípravu zakázky z textové poptávky a bonusový dynamický grid řízený JSON konfigurací.

> **Stav řešení:** všechny povinné části jsou implementovány: evidence, technologický postup výrobku, zakázky s položkami, testovací data, AI asistent (server i klient) a automatizované testy. Bonus (dynamický grid) je implementován s popsanými omezeními. Podrobnosti jsou v sekci [Stav a plán](#stav-a-plán).

## Požadavky pro spuštění

- .NET 10 SDK
- Microsoft SQL Server: LocalDB (jen Windows), plná instalace, nebo Docker
- Nástroj `dotnet-ef` (`dotnet tool install --global dotnet-ef`), potřebný jen pro ruční práci s migracemi
- Důvěryhodný vývojový certifikát (`dotnet dev-certs https --trust`)
- Docker (volitelně, viz [Spuštění v Dockeru](#spuštění-v-dockeru))

## Konfigurace

### Připojení k databázi

Connection string je v `FurnitureOrders.Api/appsettings.json` (klíč `ConnectionStrings:DefaultConnection`). Výchozí hodnota míří na LocalDB:

```
Server=(localdb)\MSSQLLocalDB;Database=FurnitureOrders;Trusted_Connection=True;TrustServerCertificate=True
```

Hodnotu lze přepsat bez zásahu do kódu:

- proměnnou prostředí `ConnectionStrings__DefaultConnection`, nebo
- `dotnet user-secrets` v projektu `FurnitureOrders.Api`.

**SQL Server v Dockeru** (pro prostředí bez LocalDB):

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=<silné_heslo>" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
```

```
Server=localhost,1433;Database=FurnitureOrders;User Id=sa;Password=<silné_heslo>;TrustServerCertificate=True
```

### CORS a adresa API

- `FurnitureOrders.Api/appsettings.json` → `Cors:AllowedOrigins` musí obsahovat adresu klienta (výchozí `https://localhost:7200`).
- `FurnitureOrders.Client/wwwroot/appsettings.json` → `ApiBaseUrl` musí ukazovat na adresu API (výchozí `https://localhost:7222/`).

Porty se řídí soubory `Properties/launchSettings.json` v obou projektech.

## Vytvoření databáze

Při startu Api se migrace aplikují automaticky. Ručně:

```bash
dotnet ef database update --project FurnitureOrders.Api
```

## Spuštění aplikace

V solution nastavte jako startovací oba projekty (`FurnitureOrders.Api` i `FurnitureOrders.Client`) a spusťte (F5), nebo ze dvou terminálů:

```bash
dotnet run --project FurnitureOrders.Api --launch-profile https
dotnet run --project FurnitureOrders.Client --launch-profile https
```

## Spuštění v Dockeru

Součástí repozitáře je `Dockerfile` (vícefázové sestavení klienta a API, Nginx servíruje klienta a směruje volání API) a `docker-compose.yml` (aplikace a SQL Server):

```bash
docker compose up --build
```

Aplikace je pak dostupná na `http://localhost:8080`. Migrace a testovací data se aplikují při startu. Heslo SQL Serveru, poskytovatele a klíč AI služby lze přepsat proměnnými prostředí v `docker-compose.yml` (případně souborem `.env`). Výchozí heslo v compose je určeno jen pro vývoj. Bez API klíče AI služba hlásí, že není nakonfigurována, a zbytek aplikace funguje. Kontejner s aplikací obsahuje dva procesy (Nginx a API), což je zvolené pro jednoduchost této úlohy.

## Průběžná integrace (CI)

Workflow GitHub Actions (`.github/workflows/ci-cd.yml`) při každém pushi a pull requestu sestaví solution a spustí `dotnet test`. Testy nepotřebují SQL Server ani žádný tajný klíč (používají in-memory databázi a testovací implementaci AI). Část pro nasazení je záměrně jen připravená a zakomentovaná.

## Testovací data

Testovací data se vytvářejí **automaticky při startu Api** (spolu s aplikací migrací). Obojí řídí `Database:MigrateOnStartup` a `Database:SeedDemoData` v `appsettings.json` (výchozí `true`). Hodnoty lze vypnout nebo přepsat proměnnou prostředí, například `Database__SeedDemoData=false`.

Seed je **idempotentní**: každý záznam se vloží, jen pokud jeho kód ještě neexistuje, takže opakované spuštění nevytváří duplicity.

Rozsah: 10 zákazníků (`Z90001`–`Z90010`), 15 materiálů, 10 operací, 10 výrobků, technologický postup u 5 výrobků, 10 zakázek ve všech stavech a 24 položek. Všechna data jsou smyšlená. Kódy mají vlastní prefixy (`Z9xxxx`, `MAT-`, `OP-`, `VYR-`, `ZAK-9xxxx`), takže se nepletou s ručně zadanými záznamy ani s automaticky generovanými čísly.

Výrobky jsou pojmenované tak, aby šly vyzkoušet scénáře AI asistenta: *Psací stůl Alfa*, *Skříňka Beta*, dvojice *Police Delta* a *Police Epsilon* (nejednoznačné „police“) a žádná „Skříň Zeta“ (neexistující výrobek).

## Testování

### Ruční testování API

Soubor `FurnitureOrders.Api/FurnitureOrders.Api.http` obsahuje ruční testovací požadavky (spustitelné přímo ve Visual Studiu) pro zákazníky, materiály, operace, výrobky, technologický postup, zakázky a AI návrh zakázky: vytvoření, validační chyby, duplicitní kódy, filtrování, řazení, deaktivaci, změnu pořadí operací a výpočet cen. Všechny sekce si vytvářejí vlastní data s náhodnými kódy, takže jdou spouštět opakovaně. Požadavky je třeba spouštět v rámci sekce shora dolů, protože některé používají odpověď předchozího (`# @name`). Sekce AI vyžaduje `Ai:Provider` = `Fake`.

### Automatizované testy

Projekt `FurnitureOrders.Tests` (xUnit, EF Core InMemory). Spuštění: `dotnet test`. Testy pokrývají:

- výpočet ceny položky a zakázky, včetně zaokrouhlení,
- validační pravidla DTO (povinné hodnoty, formát e-mailu, nezáporné hodnoty, množství větší než nula),
- unikátnost kódů a pravidla zakázek (výpočet cen na serveru, neaktivní zákazník a výrobek, termíny, chyby u konkrétních řádků, cizí identifikátory položek),
- přiřazování výrobků v AI návrhu (`ProductMatcher`), zpracování výstupu modelu a normalizaci textu,
- scénáře AI návrhu: nic se neukládá, ceny pocházejí z databáze, pokus o změnu cen, vymyšlený kód výrobku, limity vstupu, chyba služby,
- idempotenci a správnost testovacích dat,
- načtení a validaci konfigurace dynamického gridu, vazbu sloupců na vlastnosti, filtry, řazení a formátování hodnot.

In-memory databáze nevynucuje unikátní indexy ani nezná SQL sekvence. Ty ověřují ruční požadavky v `.http`. Integrační testy nad skutečným SQL Serverem součástí řešení nejsou.

## Architektura

Solution má čtyři projekty:

| Projekt | Odpovědnost |
|---|---|
| `FurnitureOrders.Api` | Minimal API endpointy, aplikační služby, serverová validace, EF Core, integrace AI (`Domain/`, `Data/`, `Services/`, `Endpoints/`, `Ai/`) |
| `FurnitureOrders.Client` | Blazor WebAssembly, uživatelské rozhraní, klienti pro volání API, dynamický grid |
| `FurnitureOrders.Shared` | DTO, výčtové typy, výpočet cen, validační atributy a logika gridu sdílené klientem i serverem |
| `FurnitureOrders.Tests` | Automatizované testy (xUnit) |

**Zdůvodnění:** aplikace je malá, proto entity, servisní vrstva a přístup k databázi nejsou v samostatných projektech. Oddělení odpovědností je zajištěno složkami a rozhraními (například `ICustomerService`). Blazor WebAssembly je samostatný klient a všechna kritická logika, validace i komunikace s AI leží na serveru, aby klíče ani pravidla nebyla součástí klientské aplikace.

**Klient:** opakující se prvky rozhraní jsou vlastní komponenty ve složce `Components/` (`SortableHeader`, `RowActions`, `ListToolbar`, `FormSection`, `FormActions`, `ErrorList`, `DetailField`, `ActiveChip`, `OrderStatusChip`, `DatePickerField`, `DynamicGrid`). Volání API zajišťuje generický `CrudApiClient<TList, TEdit>`, který pro každou evidenci doplňuje jen cestu a sestavení dotazu. Serverové validační chyby se do formuláře přenášejí třídou `ServerValidation`. Nová evidence se tak přidává dodáním entity, DTO, služby, endpointů a tří stránek (seznam, formulář, detail).

**Validace:** pravidla jsou definována jednou jako DataAnnotations na DTO ve `Shared`. Klient je používá pro okamžitou zpětnou vazbu, server je znovu ověřuje před uložením (`ModelValidator`). Unikátnost kódů hlídá aplikační kontrola s čitelnou hláškou a navíc unikátní index v databázi (zachytí i souběžné požadavky). Chyby ze serveru se zobrazují u příslušných polí formuláře, u položek zakázky i u konkrétního řádku.

## Použité knihovny třetích stran

### MudBlazor 9.x (MIT)

Komponentová knihovna pro uživatelské rozhraní (formuláře, tabulky, dialogy, layout).

**Důvod použití:**

- Zadání hodnotí především datový model, validaci, architekturu a práci s AI, ne ruční stylování. Hotové komponenty uvolnily čas právě pro tyto části.
- Pokrývá všechny prvky požadované zadáním (textové a víceřádkové pole, číselné pole, přepínač, výběrový seznam, datumové pole, výběr související entity přes Autocomplete).
- Zajišťuje konzistentní vzhled a chování napříč všemi evidencemi.
- Dobře se napojuje na DataAnnotations validaci sdílenou klientem a serverem.
- Je open source a instaluje se z veřejného NuGetu.

**Způsob použití:** opakující se části rozhraní jsou zapouzdřeny do vlastních znovupoužitelných komponent nad MudBlazorem (viz Architektura), aby aplikační kód nebyl silně svázán s jeho API.

Ostatní závislosti jsou standardní balíčky Microsoftu (EF Core, ASP.NET Core). Volání AI služby probíhá přes `HttpClient` bez dalších knihoven. Testy používají xUnit a `Microsoft.EntityFrameworkCore.InMemory`.

### Bezpečnostní poznámka k závislostem

Balíček `System.Security.Cryptography.Pkcs` je v projektu `FurnitureOrders.Api` explicitně připnut na verzi 10.0.13, protože tranzitivně přitahovaná starší verze obsahuje zranitelnost CVE-2026-70162 (GHSA-2fmv-rv2c-59x7). Po aktualizaci nadřazených balíčků lze přímou referenci odstranit.

## AI asistent pro přípravu zakázky z poptávky

*Stav: server i klient implementovány a pokryty automatizovanými testy a testovací implementací. Ověření proti skutečné službě vyžaduje vlastní API klíč.*

**Poskytovatel a model:** Anthropic Claude přes Messages API. Model je konfigurovatelný (`Ai:Anthropic:Model`, výchozí `claude-haiku-5-5`).

**Konfigurace:** sekce `Ai` v `appsettings.json`. API klíč se do souboru nezapisuje. Nastavuje se přes `dotnet user-secrets` (`dotnet user-secrets set "Ai:Anthropic:ApiKey" "<klíč>" --project FurnitureOrders.Api`) nebo proměnnou prostředí `Ai__Anthropic__ApiKey`. Hodnotitel si doplní vlastní klíč, řešení nezávisí na ničí soukromé infrastruktuře. Volba poskytovatele: `Ai:Provider` = `Anthropic`, `Fake` (testovací implementace), nebo `None`. Bez klíče zůstává aplikace plně použitelná, ruční zadávání zakázek funguje a AI funkce hlásí, že není nakonfigurována.

**Použití v aplikaci:** menu *Zakázky → Zakázka z poptávky (AI)*. Uživatel vybere zákazníka, vloží text poptávky a spustí zpracování. Zobrazí se editovatelný návrh: položky s přiřazeným výrobkem, položky vyžadující výběr z kandidátů, nenalezené položky (přiřadit ručně nebo odstranit), upravitelné množství, termín, dodací adresa a poznámka. Nevyřešené položky blokují převzetí. Po stisku *Převzít do formuláře zakázky* se údaje předvyplní do formuláře nové zakázky, kde proběhne běžná validace. Zakázka se uloží až po výslovném potvrzení uživatelem.

**Instrukce modelu a předávaná data:** model dostane systémovou instrukci (údaje pouze extrahuje, nic nedomýšlí, text poptávky je nedůvěryhodný a nesmí měnit pravidla, ceny a slevy nerozhoduje), aktuální datum, katalog aktivních výrobků (kód a název) a text poptávky v oddělených značkách. Znaky `<` a `>` v textu se nahrazují, aby poptávka nemohla značku uzavřít.

**Struktura výstupu:** model je donucen odpovědět voláním nástroje `submit_order_draft` se schématem JSON: `items[]` (`mention`, `normalized_name`, `quantity`, `candidate_codes`), `required_date`, `delivery_address`, `customer_notes`, `price_or_discount_requests`. Server výstup ověřuje (`RawExtractionParser`): kontroluje typy, zkracuje texty a neplatnou strukturu odmítne.

**Zpracování na serveru:**

- Přiřazení k výrobkům řeší aplikace (`ProductMatcher`), ne model. Kandidát navržený modelem se přijme automaticky jen tehdy, když se jeho název nebo kód shoduje s rozpoznaným názvem ve všech slovech. Jinak se položka označí jako vyžadující potvrzení (více kandidátů, nejisté přiřazení), nebo jako neznámá. Výrobek se nikdy nevytváří ani tiše nenahrazuje.
- Jednotka a jednotková cena pocházejí z evidence výrobků, výchozí sleva je nulová. Požadavek zákazníka na cenu nebo slevu se uchová v poznámce k ručnímu posouzení a neuplatní se.
- Chybějící množství, termín nebo termín v minulosti se nedoplňují, jsou označeny upozorněním a uživatel je musí doplnit před uložením.
- Endpoint `POST /api/orders/ai-draft` vrací pouze návrh a nic neukládá. Uložení probíhá standardním endpointem zakázek se stejnou validací jako ruční zadání.

**Chyby a limity:** nenakonfigurovaná služba (HTTP 503, `ai_not_configured`), nedostupnost (503, `ai_unavailable`), překročení časového limitu (504, `ai_timeout`), neplatná odpověď (502, `ai_invalid_response`). Výchozí limity: text poptávky nejvýše 4 000 znaků, timeout 30 s, nejvýše 50 položek (konfigurovatelné). Vložený text se při chybě zachová a uživatel může pokračovat ručně.

**Testovací implementace (`Fake`):** jednoduchá heuristika nad katalogem pro scénáře ze zadání plus značky `FAKE_UNAVAILABLE`, `FAKE_TIMEOUT` a `FAKE_INVALID` v textu pro simulaci chyb. Neslouží jako náhrada skutečné integrace.

**Ověřovací scénáře:** soubor `FurnitureOrders.Api.http`, sekce „AI návrh zakázky“ (jednoznačná poptávka, neznámý výrobek, nejednoznačné označení, chybějící množství a termín, nedostupná služba, neplatná odpověď, pokus o změnu cen). Stejné scénáře pokrývají i automatizované testy.

## Bonus: dynamický přehledový grid

Znovupoužitelná komponenta `DynamicGrid<TItem>` zobrazuje přehledy podle konfigurace v JSON souborech (`FurnitureOrders.Client/wwwroot/grids/*.json`). Použita je pro dvě evidence: zákazníky (`/grid/customers`, konfigurace `customers.json`) a zakázky (`/grid/orders`, konfigurace `orders.json`). Odkazy jsou v menu *Přehledy (grid)*. Podrobná dokumentace přímo v kódu (anglicky) začíná úvodním přehledem na začátku souboru `FurnitureOrders.Shared/Grid/GridConfig.cs`.

### Jak přidat nový přehled

1. Do `wwwroot/grids/` přidat soubor `{název}.json` s popisem sloupců.
2. Přidat stránku s jednou komponentou. Komponenta o API nic neví, řádky jí dodává delegát:

```razor
@page "/grid/orders"
@inject OrderApiClient Api

<DynamicGrid TItem="OrderListItemDto" ConfigName="orders" LoadItems="LoadAsync" />

@code {
    private async Task<IReadOnlyList<OrderListItemDto>> LoadAsync(CancellationToken ct) =>
        await Api.ListAsync(new OrderQuery(null, null, null, null, null, "created", true), ct);
}
```

Komponenta ani žádná jiná třída se nemění.

### Konfigurace

```json
{
  "entity": "Order",
  "title": "Zakázky (dynamický grid)",
  "pageSize": 10,
  "detailUrl": "orders/{Id}",
  "defaultSort": { "field": "CreatedDate", "direction": "Descending" },
  "columns": [
    { "field": "OrderNumber", "label": "Číslo zakázky", "dataType": "String", "displayType": "DetailLink",
      "filterType": "TextBox", "sortable": true, "filterable": true, "visible": true, "order": 1 }
  ]
}
```

| Položka | Význam |
|---|---|
| `entity`, `title` | Povinné. `entity` určuje název souboru při exportu do CSV, `title` je nadpis přehledu. |
| `pageSize` | Počet řádků na stránku, povoleno 5 až 200. |
| `detailUrl` | Šablona odkazu pro zobrazení `DetailLink`, např. `orders/{Id}`. Zástupný výraz je název libovolné veřejné vlastnosti řádku, hodnota se do adresy vkládá s ošetřením speciálních znaků. |
| `defaultSort` | Počáteční řazení. Sloupec musí existovat a mít `sortable: true`. |
| `field` | Název vlastnosti DTO (nezáleží na velikosti písmen). Musí být v přehledu jedinečný. |
| `dataType` | Co hodnota je: `String`, `Integer`, `Decimal`, `Date`, `DateTime`, `Boolean`, `Enum`. Ověřuje se proti skutečnému typu vlastnosti. |
| `displayType` | Jak se hodnota zobrazí: `Text`, `Number`, `Decimal`, `Currency`, `Date`, `DateTime`, `Checkbox`, `Select`, `DetailLink`. |
| `filterType` | `TextBox`, `NumberRange`, `DateRange`, `CheckBox`, `Select`. Bez uvedení se odvodí z `dataType`. Použije se jen u sloupců s `filterable: true`. |
| `format` | Volitelný formátovací řetězec .NET (např. `N2`, `dd.MM.yyyy`). Chybný formát grid neshodí, zobrazí se surová hodnota. |
| `order`, `visible`, `sortable`, `filterable` | Pořadí zleva doprava, počáteční viditelnost, řazení kliknutím na záhlaví a zobrazení filtru. |
| `options` | Zdroj hodnot pro `Select`: `Static` (položky přímo v JSON), `Enum` (registrovaný výčet), `Distinct` (hodnoty z načtených dat), `Lookup` (poskytovatel, např. `customers`). |

`dataType` a `displayType` jsou oddělené záměrně: první říká, co hodnota je, a kontroluje se proti kódu, druhý jak vypadá. Smysluplné dvojice hlídá validátor (např. `Currency` jen pro číselný typ, `Checkbox` jen pro `Boolean`).

### Jak konfigurace putuje

`GridConfigLoader` (načtení souboru, název se omezuje na písmena, číslice, `_` a `-`) → `GridConfigParser` (JSON na objekt, nikdy nevyhodí výjimku kvůli obsahu) → `GridConfigValidator` (vnitřní konzistence, bez znalosti DTO) → `GridColumnBinder` (ověření proti skutečné vlastnosti DTO a jejímu typu, vytvoření přístupových funkcí) → `DynamicGrid` (volby, data, filtrování, řazení, stránkování, vykreslení).

Chyby ze všech kroků se shromažďují a zobrazí jako jeden seznam. Neplatná konfigurace se tak nikdy nezobrazí jako částečně funkční grid ani jako výjimka. Příklady kontrol: neznámý typ, `Currency` na textovém sloupci, `Select` bez `options`, `DetailLink` bez `detailUrl`, vlastnost, která v DTO neexistuje, nebo `string` označený jako `Integer`.

### Co je hotovo

- Konfigurace sloupce (viz tabulka výše), pět typů filtrů a čtyři zdroje hodnot výběru.
- Řazení, filtrování (filtry se kombinují logickým AND), stránkování, uživatelské skrývání sloupců, export aktuálního přehledu do CSV a více konfigurací na jednu evidenci (každá je samostatný soubor).
- Export do CSV obsahuje viditelné sloupce a všechny řádky, které prošly filtrem (ne jen aktuální stránku), ve stejném textu, jaký vidí uživatel. Textové hodnoty začínající `=`, `+`, `-` nebo `@` se prefixují apostrofem, aby je tabulkový procesor nevyhodnotil jako vzorec. Oddělovačem je středník a soubor má znak BOM kvůli otevření v Excelu v českém prostředí.
- Typová bezpečnost: pole z konfigurace se při načtení ověří proti DTO včetně datového typu a hodnoty se čtou předem sestavenými přístupovými funkcemi, ne reflexí pro každou buňku. Z konfigurace je dosažitelná jen veřejná čtecí vlastnost, nikdy metoda ani soukromý člen.
- Hodnoty pro porovnání a filtry se převádějí nezávisle na kultuře (jinak by se např. rozsah cen choval jinak podle nastavení prohlížeče), zobrazení respektuje kulturu uživatele.
- Rozšiřitelnost bez zásahu do komponenty: nový typ zobrazení je `GridCellRenderers.Register` plus název v `GridVocabulary`, nový výčet je řádek v `GridEnums`, nový číselník je záznam v `GridLookups`. Jednotlivý sloupec lze vykreslit vlastní šablonou (`CellTemplates`).
- Kontrolu konfigurací dodaných s aplikací zajišťuje automatizovaný test.

### Není hotovo a známá omezení

- Filtrování, řazení a stránkování probíhají v paměti klienta nad celým načteným seznamem. Pro stovky až nízké tisíce řádků je to v pořádku, pro větší objemy je potřeba dotaz na straně serveru.
- Data se načítají jednou při otevření přehledu, bez automatického obnovení. Změna dat vyžaduje opětovné otevření stránky (při chybě načtení je tlačítko *Zkusit znovu*).
- JSON se čte shovívavě: neznámá nebo chybně napsaná položka (např. `sortabel`) se ignoruje a sloupec dostane výchozí hodnotu. Přísnější čtení by bylo nastavení `UnmappedMemberHandling.Disallow` v `GridConfigParser`.
- Symbol měny je v zobrazení `Currency` pevně „Kč“, `format` ovlivňuje jen číselnou část.
- Ukládání konfigurace do databáze, uživatelská změna pořadí sloupců a jejich uložení mezi návštěvami.
- Nový typ filtru není jen konfigurační záležitost: vyžaduje úpravu `GridFiltering`, `GridVocabulary` a bloku filtrů v komponentě.
- Při změně `ConfigName` za běhu by se mohly dvě inicializace překrývat. Přehledy se v aplikaci otevírají vždy s pevným názvem konfigurace, takže se to neprojeví.

### Technická rozhodnutí a pokračování

Logika bez závislosti na Blazoru (modely konfigurace, parser, validace, filtrování, formátování, vazba sloupců) je ve `Shared`, aby byla testovatelná bez prohlížeče. V `Client` zůstává jen to, co potřebuje Blazor nebo služby aplikace: komponenta, načtení souboru, registr výčtů a registr číselníků. Výčty a číselníky jsou záměrně explicitní seznamy, aby konfigurační soubor mohl vyvolat jen to, co vývojář vědomě zpřístupnil. Projekt `Client` uvádí `FurnitureOrders.Shared` jako kořenové sestavení pro ořezávání (`TrimmerRootAssembly`), protože vlastnosti DTO se z konfigurace dohledávají podle názvu a bez toho by je při publikaci mohl ořezávač odstranit.

Dalším krokem by bylo přesunout filtrování a řazení na server přes parametrizovaný dotaz s whitelistem polí z konfigurace, uložit konfigurace do databáze s verzováním a přidat uživatelské nastavení sloupců.

## Předpoklady a rozhodnutí

Zadání v některých místech nechává volbu na řešiteli. Přijatá rozhodnutí:

- **Odstranění záznamů:** u evidencí se používá deaktivace (příznak `IsActive`), ne fyzické mazání. Deaktivovaný záznam zůstává v historických datech, ale nenabízí se pro nové záznamy.
- **Číslo zákazníka:** generuje se automaticky ze SQL sekvence ve tvaru `Z` + 5 číslic (např. `Z00001`). Lze ho zadat i ručně. Sekvence může vynechat hodnoty (např. po neúspěšném uložení), proto čísla nejsou garantovaně souvislá. Ručně zadané číslo se kontroluje na unikátnost.
- **Kontrola e-mailu:** formát se kontroluje jen u vyplněné hodnoty.
- **IČO a DIČ:** zadání nepožaduje jejich ověření, kontroluje se pouze délka.
- **Číselníky:** typ materiálu a typ zákazníka jsou výčtové typy. Pracoviště a typ operace, skupina a typ výrobku jsou volný text (zadání uvádí jen příklady). Již použitá pracoviště a skupiny výrobků se nabízejí při zadávání a ve filtrech.
- **Nepovinné číselné hodnoty:** nákupní cena, minimální objednací množství, doby operací, hodinová sazba, rozměry, hmotnost a doba dodání jsou nepovinné, ale pokud jsou vyplněny, nesmí být záporné. Prodejní cena výrobku je povinná a nezáporná.
- **Společné prvky seznamů:** všechny evidence mají stejné ovládání (hledání podle kódu a názvu, filtr stavu, řazení kliknutím na záhlaví, doplňkový filtr podle typu nebo skupiny).
- **Technologický postup:** materiály a operace výrobku se odebírají fyzicky (jsou to podřízené řádky výrobku). Pořadí se při přidání doplní automaticky, změna pořadí operací probíhá akcí nahoru/dolů a server pořadí přečísluje. Neaktivní materiál nebo operaci nelze k výrobku nově přiřadit. Měrná jednotka materiálu výrobku se při prázdné hodnotě převezme z materiálu. Po výběru operace se v dialogu předvyplní pracoviště a časy z evidence operací.
- **Zakázky:** zakázka se ukládá včetně položek jedním požadavkem. Cenu položky (`množství × cena × (1 − sleva/100)`, zaokrouhleno na 2 místa) i zakázky (součet cen položek) počítá server a klientem poslané ceny ignoruje. Pořadí položek určuje pořadí v odeslaném seznamu. Zakázka se nemaže, „deaktivací“ je stav *Zrušená*. Číslo zakázky se generuje ze sekvence (`ZAK-00001`), lze ho zadat i ručně. Neaktivního zákazníka ani výrobek nelze do zakázky nově přidat, u existující zakázky zůstanou původní hodnoty platné. Termín dodání ani skutečné dokončení nesmí být před datem vytvoření.
- **Zakázky v UI:** položky se editují přímo v tabulce ve formuláři zakázky. Po výběru výrobku se předvyplní měrná jednotka a cena, po výběru zákazníka prázdný kontakt a dodací adresa (vše lze přepsat). Zobrazené ceny jsou jen náhled, závazný výpočet provádí server při uložení.
- **Předání návrhu z AI:** návrh se do formuláře zakázky předává v paměti prohlížeče (`OrderDraftHandoff`), po obnovení stránky se ztratí.
- **Jazyk:** uživatelské rozhraní je česky, názvy v kódu anglicky.

## Stav a plán

| Oblast | Stav |
|---|---|
| Zákazníci, materiály, operace, výrobky (seznam, filtr, řazení, detail, formulář, deaktivace) | hotovo |
| Technologický postup výrobku (vnořené seznamy, pořadí operací) | hotovo |
| Zakázky a položky, výpočet cen na serveru | hotovo |
| Navigace | hotovo |
| Testovací data (seed) | hotovo |
| AI asistent (server i klient) | hotovo, ověření proti skutečné službě vyžaduje klíč |
| Automatizované testy | hotovo (bez integračních testů nad SQL Serverem) |
| Docker a CI | hotovo, nasazení do produkce viz roadmapa |
| Bonus: dynamický grid řízený JSON konfigurací | hotovo se zdokumentovanými omezeními |

Další kroky pro produkční provoz (autentizace a autorizace, správa tajných klíčů, oddělení migrací od běhu aplikace, zálohování, logování a monitoring, odolnost integrace s AI) jsou popsány v samostatném dokumentu `docs/production-roadmap.md`. Nebyly součástí zadání.

## Známá omezení

- Aplikace nemá přihlášení ani řízení oprávnění (mimo rozsah zadání).
- Seznamy evidencí se načítají celé, bez stránkování a bez filtrování na straně serveru, což by při velkém objemu dat bylo potřeba doplnit. Dynamický grid stránkuje na klientu.
- Ceny jsou bez DPH a v jedné měně (podle zadání).
- AI asistent posílá modelu katalog aktivních výrobků (nejvýše 500). Pro rozsáhlý katalog by bylo potřeba předvýběr kandidátů.
- Relativní termíny v poptávce („za týden“) model převádí jen tehdy, když jsou jednoznačné, jinak je termín označen jako chybějící.
- Automatické spouštění migrací a seedu při startu je vhodné pro vývoj a ukázku, ne pro produkci s více instancemi.
- Pro vývojové a ukázkové účely je v `docker-compose.yml` výchozí heslo SQL Serveru.

## Licence

Projekt je zveřejněn pod licencí MIT, viz soubor `LICENSE`.
