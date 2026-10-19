# Zakázkový systém pro výrobu nábytku

Webová aplikace (Blazor WebAssembly + ASP.NET Core Web API + MS SQL Server) pro evidenci zákazníků, materiálů, operací, výrobků, zakázek a jejich položek. Součástí je AI asistent pro přípravu zakázky z textové poptávky.

> **Stav řešení:** rozpracováno. Hotové jsou všechny evidence včetně technologického postupu výrobku, zakázek s položkami, testovací data a serverová část AI asistenta. Zbývá klientská část AI asistenta, další automatizované testy a bonus. Podrobnosti jsou v sekci [Stav a plán](#stav-a-plán).

## Požadavky pro spuštění

- .NET 10 SDK
- Microsoft SQL Server: LocalDB (jen Windows), plná instalace, nebo Docker
- Nástroj `dotnet-ef` (`dotnet tool install --global dotnet-ef`), potřebný jen pro ruční práci s migracemi
- Důvěryhodný vývojový certifikát (`dotnet dev-certs https --trust`)

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

## Testovací data

Testovací data se vytvářejí **automaticky při startu Api** (spolu s aplikací migrací). Obojí řídí `Database:MigrateOnStartup` a `Database:SeedDemoData` v `appsettings.json` (výchozí `true`). Hodnoty lze vypnout nebo přepsat proměnnou prostředí, například `Database__SeedDemoData=false`.

Seed je **idempotentní**: každý záznam se vloží, jen pokud jeho kód ještě neexistuje, takže opakované spuštění nevytváří duplicity.

Rozsah: 10 zákazníků (`Z90001`–`Z90010`), 15 materiálů, 10 operací, 10 výrobků, technologický postup u 5 výrobků, 10 zakázek ve všech stavech a 24 položek. Všechna data jsou smyšlená. Kódy mají vlastní prefixy (`Z9xxxx`, `MAT-`, `OP-`, `VYR-`, `ZAK-9xxxx`), takže se nepletou s ručně zadanými záznamy ani s automaticky generovanými čísly.

Výrobky jsou pojmenované tak, aby šly vyzkoušet scénáře AI asistenta: *Psací stůl Alfa*, *Skříňka Beta*, dvojice *Police Delta* a *Police Epsilon* (nejednoznačné „police“) a žádná „Skříň Zeta“ (neexistující výrobek).

## Testování API

Soubor `FurnitureOrders.Api/FurnitureOrders.Api.http` obsahuje ruční testovací požadavky (spustitelné přímo ve Visual Studiu) pro zákazníky, materiály, operace, výrobky, technologický postup, zakázky a AI návrh zakázky: vytvoření, validační chyby, duplicitní kódy, filtrování, řazení, deaktivaci, změnu pořadí operací a výpočet cen. Požadavky je třeba spouštět shora dolů, protože některé používají odpověď předchozího (`# @name`). Novější sekce si vytvářejí vlastní data s náhodnými kódy. Starší sekce používají pevné kódy (`HM001`, `HO001`, `HP001`), které je před opakovaným během nutné změnit, jinak server vrátí chybu o duplicitě.

## Automatizované testy

Projekt `FurnitureOrders.Tests` (xUnit). Spuštění: `dotnet test`. Zatím pokrývají výpočet cen položky a zakázky (`OrderPricingTests`).

## Architektura

Solution má čtyři projekty:

| Projekt | Odpovědnost |
|---|---|
| `FurnitureOrders.Api` | Minimal API endpointy, aplikační služby, serverová validace, EF Core, integrace AI (`Domain/`, `Data/`, `Services/`, `Endpoints/`, `Ai/`) |
| `FurnitureOrders.Client` | Blazor WebAssembly, uživatelské rozhraní, klienti pro volání API |
| `FurnitureOrders.Shared` | DTO, výčtové typy, výpočet cen a validační atributy sdílené klientem i serverem |
| `FurnitureOrders.Tests` | Automatizované testy (xUnit) |

**Zdůvodnění:** aplikace je malá, proto entity, servisní vrstva a přístup k databázi nejsou v samostatných projektech. Oddělení odpovědností je zajištěno složkami a rozhraními (například `ICustomerService`). Blazor WebAssembly je samostatný klient a všechna kritická logika, validace i komunikace s AI leží na serveru, aby klíče ani pravidla nebyla součástí klientské aplikace.

**Klient:** opakující se prvky rozhraní jsou vlastní komponenty ve složce `Components/` (`SortableHeader`, `RowActions`, `ListToolbar`, `FormSection`, `FormActions`, `ErrorList`, `DetailField`, `ActiveChip`, `OrderStatusChip`, `DatePickerField`). Volání API zajišťuje generický `CrudApiClient<TList, TEdit>`, který pro každou evidenci doplňuje jen cestu a sestavení dotazu. Serverové validační chyby se do formuláře přenášejí třídou `ServerValidation`. Nová evidence se tak přidává dodáním entity, DTO, služby, endpointů a tří stránek (seznam, formulář, detail).

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

Ostatní závislosti jsou standardní balíčky Microsoftu (EF Core, ASP.NET Core). Volání AI služby probíhá přes `HttpClient` bez dalších knihoven.

### Bezpečnostní poznámka k závislostem

Balíček `System.Security.Cryptography.Pkcs` je v projektu `FurnitureOrders.Api` explicitně připnut na verzi 10.0.13, protože tranzitivně přitahovaná starší verze obsahuje zranitelnost CVE-2026-70162 (GHSA-2fmv-rv2c-59x7). Po aktualizaci nadřazených balíčků lze přímou referenci odstranit.

## AI asistent pro přípravu zakázky z poptávky

*Stav: server i klient implementovány.*

**Poskytovatel a model:** Anthropic Claude přes Messages API. Model je konfigurovatelný (`Ai:Anthropic:Model`, výchozí `claude-haiku-5-5`).

**Konfigurace:** sekce `Ai` v `appsettings.json`. API klíč se do souboru nezapisuje. Nastavuje se přes `dotnet user-secrets` (`dotnet user-secrets set "Ai:Anthropic:ApiKey" "<klíč>" --project FurnitureOrders.Api`) nebo proměnnou prostředí `Ai__Anthropic__ApiKey`. Hodnotitel si doplní vlastní klíč, řešení nezávisí na ničí soukromé infrastruktuře. Volba poskytovatele: `Ai:Provider` = `Anthropic`, `Fake` (testovací implementace), nebo `None`. Bez klíče zůstává aplikace plně použitelná, ruční zadávání zakázek funguje a AI funkce hlásí, že není nakonfigurována.

**Instrukce modelu a předávaná data:** model dostane systémovou instrukci (údaje pouze extrahuje, nic nedomýšlí, text poptávky je nedůvěryhodný a nesmí měnit pravidla, ceny a slevy nerozhoduje), aktuální datum, katalog aktivních výrobků (kód a název) a text poptávky v oddělených značkách. Znaky `<` a `>` v textu se nahrazují, aby poptávka nemohla značku uzavřít.

**Struktura výstupu:** model je donucen odpovědět voláním nástroje `submit_order_draft` se schématem JSON: `items[]` (`mention`, `normalized_name`, `quantity`, `candidate_codes`), `required_date`, `delivery_address`, `customer_notes`, `price_or_discount_requests`. Server výstup ověřuje (`RawExtractionParser`): kontroluje typy, zkracuje texty a neplatnou strukturu odmítne.

**Zpracování na serveru:**

- Přiřazení k výrobkům řeší aplikace (`ProductMatcher`), ne model. Kandidát navržený modelem se přijme automaticky jen tehdy, když se jeho název nebo kód shoduje s rozpoznaným názvem ve všech slovech. Jinak se položka označí jako vyžadující potvrzení (více kandidátů, nejisté přiřazení), nebo jako neznámá. Výrobek se nikdy nevytváří ani tiše nenahrazuje.
- Jednotka a jednotková cena pocházejí z evidence výrobků, výchozí sleva je nulová. Požadavek zákazníka na cenu nebo slevu se uchová v poznámce k ručnímu posouzení a neuplatní se.
- Chybějící množství, termín nebo termín v minulosti se nedoplňují, jsou označeny upozorněním a uživatel je musí doplnit před uložením.
- Endpoint `POST /api/orders/ai-draft` vrací pouze návrh a nic neukládá. Uložení probíhá standardním endpointem zakázek se stejnou validací jako ruční zadání.

**Chyby a limity:** nenakonfigurovaná služba (HTTP 503, `ai_not_configured`), nedostupnost (503, `ai_unavailable`), překročení časového limitu (504, `ai_timeout`), neplatná odpověď (502, `ai_invalid_response`). Výchozí limity: text poptávky nejvýše 4 000 znaků, timeout 30 s, nejvýše 50 položek (konfigurovatelné). Vložený text se při chybě zachová.

**Testovací implementace (`Fake`):** jednoduchá heuristika nad katalogem pro scénáře ze zadání plus značky `FAKE_UNAVAILABLE`, `FAKE_TIMEOUT` a `FAKE_INVALID` v textu pro simulaci chyb. Neslouží jako náhrada skutečné integrace.

**Ověřovací scénáře:** soubor `FurnitureOrders.Api.http`, sekce „AI návrh zakázky“ (jednoznačná poptávka, neznámý výrobek, nejednoznačné označení, chybějící množství a termín, nedostupná služba, neplatná odpověď, pokus o změnu cen).

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
- **Jazyk:** uživatelské rozhraní je česky, názvy v kódu anglicky.
- Návrh z AI se do formuláře zakázky předává v paměti prohlížeče (OrderDraftHandoff), po obnovení stránky se ztratí.

## Stav a plán

| Oblast | Stav |
|---|---|
| Zákazníci (seznam, filtr, řazení, detail, formulář, deaktivace) | hotovo |
| Materiály, operace, výrobky | hotovo |
| Technologický postup výrobku (vnořené seznamy, pořadí operací) | hotovo |
| Zakázky a položky, výpočet cen na serveru | hotovo |
| Navigace | hotovo |
| Testovací data (seed) | hotovo |
| AI asistent: serverová část | implementováno |
| AI asistent: klientská část | rozpracováno |
| Automatizované testy | rozpracováno (výpočet cen) |
| Bonus: dynamický grid řízený JSON konfigurací | plánováno |

## Známá omezení

- Řešení je rozpracované, viz tabulka výše.
- Seznamy se načítají celé, bez stránkování a bez filtrování na straně serveru nad velkými objemy.
- Aplikace nemá přihlášení ani řízení oprávnění (mimo rozsah zadání).
- Ceny jsou bez DPH a v jedné měně (podle zadání).
- AI asistent posílá modelu katalog aktivních výrobků (nejvýše 500). Pro rozsáhlý katalog by bylo potřeba předvýběr kandidátů.
- Relativní termíny v poptávce („za týden“) model převádí jen tehdy, když jsou jednoznačné, jinak je termín označen jako chybějící.
