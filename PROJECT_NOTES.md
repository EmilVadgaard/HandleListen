# Projekt-noter til Claude (og Emil)

**Regel for denne fil:** Claude opdaterer KUN dette dokument, når Emil eksplicit beder om det. Læs det ved sessionsstart for hurtigt at samle den røde tråd op igen — men rør ikke ved det uden at blive bedt om det.

**Stående instruks fra Emil:** Hver gang Claude beder Emil om at logge ind på serveren og indsætte et script, skal scriptet altid gives direkte og fuldt ud synligt i selve beskeden til Emil — ikke kun som output fra et tool-call, som måske ikke er tydeligt synligt for ham.

Sidst opdateret: 2026-11-01 (efter at have bygget og deployet alle fire planlagte features — Print-knap, Planer, Min Side, Budget — plus flere opfølgende bug-fixes og en lokal opsætning til at arbejde fra flere maskiner).

---

## Hvor vi stoppede

**Alle fire tidligere planlagte features (Print-knap, Planer, Min Side, Budget) er nu bygget, testet og deployet til produktion.** Se "De fire features" nedenfor for hvad hver indeholder, som bygget. Der ligger ingen uafsluttede deploys.

**Git er nu i sync:** `origin/main` på GitHub (`EmilVadgaard/HandleListen`) har alle commits — der havde ophobet sig 7 upushede commits på denne maskine (Planer, Min Side, Budget, bug-fixes), som blev pushet 2026-11-01.

**Ny opsætning til at arbejde fra flere maskiner:** `CLAUDE.md` i repo-roden peger en ny Claude-session mod denne fil først. `local-only/` (git-ignoreret, se `.gitignore`) indeholder SSH-nøglen til serveren + de rigtige hemmelige nøgler (`.env.secrets`, Firebase, google-services.json) + en `README.md` med server-adgang og deploy-proceduren — tanken er at hele repo-mappen kan `.zip`'es og tages med til en anden maskine med alt klar med det samme. Se `local-only/README.md` for detaljerne i stedet for at duplikere dem her.

**Åbent, ikke eskaleret spørgsmål:** det 40MB `publish-output/`-build-output, committed i gittens allerførste commits, er stadig i git. Emil har ikke svaret på om det skal ryddes — spørg ikke selv, vent på at han nævner det.

**"Sætte penge til side" + opsparings-graf (Budget del 2)** er stadig eksplicit udskudt, ikke startet.

---

## De fire features (alle bygget og deployet)

### 1. Print-knap
Ren frontend, ingen backend-ændring. `window.print()` + en global `@media print`-regel i `styles.css` (skjul alt undtagen `.print-area`, en `.no-print`-klasse til at skjule specifikke knapper/ikoner inde i et print-område). Findes nu på Handleliste, enkelt-opskrift (opskrift-dialogen), Madplan (to knapper: "kun navne" eller "med opskrifter" — den sidste henter hver opskrifts fulde ingredienser/fremgangsmåde på klik-tidspunktet og bygger et skjult print-kun-blok), og Planer (plan-detail-siden, uafhængigt af hvilken af de tre plantyper der vises).

### 2. Planer
Tre plan-typer: **Standard** (fri kolonne af to-do's, ingen dato — trække-og-slip omsortering via `@angular/cdk/drag-drop`), **Weekly/Ugeplan** (N dage, standard 7, relative "Dag 1..N"-labels indtil aktivering, derefter rigtige datoer fra `ActivatedDate = ClientToday + 1`), **Monthly/Månedsplan** (en valgt måned/år, visuelt en kalender-lignende gitter, men en FULDSTÆNDIG separat model — rører aldrig `CalendarEvent`).

**Datamodel:** `Plan { Id, UserId, Name, Type, IsActive, ActivatedDate, DayCount, Month, Year }`, `PlanTodo { Id, PlanId, Title, Done, DayIndex, DayOfMonth, Time, SortOrder }`. Ingen gemt "rigtig dato" nogen steder — altid beregnet i C# fra `Plan` + `PlanTodo` (ligesom `CalendarEvent.Date` for gentagende serier).

**Deling — eksplicit valgt til at genbruge ShoppingLists mønster, IKKE Kalenderens:** `PlanGuest { Id, PlanId, UserId }`, instant deling-ved-email (ingen invitér/accepter-håndtryk), en bruger kan eje/være gæst på mange planer samtidig (i modsætning til Kalender/Opskriftsbog/Budget, hvor man kun har ÉN delt container). Dette var en direkte, bevidst beslutning fra Emil, ikke en gætning.

**Kun ÉN "aktivér"-handling, ingen "afslut".** Flere planer kan være aktive samtidig. To-do's redigeres frit for evigt; kun STRUKTUREN (DayCount/Month/Year) låses ved aktivering (en simpel guard i `PATCH`-endpointet, ikke en hård arkitektur-beslutning — kan løsnes senere).

### 3. Min Side
Ny standard-forside efter login (`redirectTo: 'min-side'` i `app.routes.ts` — ÉT af TRE steder dette var hardcoded til `'handleliste'`, de andre to var `login.ts`'s post-login-navigation og `owner-guard.ts`'s fallback-redirect for ikke-ejere).

**Layout:** aktuel madplan øverst (kræver et nyt `MealPlan.IsCurrent`-flag + en "Markér som aktuel"-knap — madplaner havde IKKE noget "aktuel"-koncept før dette, kun en flad liste); en kronologisk tavle i midten (kommende kalenderevents + daterede personlige noter + aktive Weekly/Monthly-planers to-do's, alle slået sammen og sorteret client-side via et `FeedCard`-discriminated-union); en sidebar til venstre med klistermærke-noter (udaterede) + aktive Standard-planers to-do's (heller ingen dato).

**`PersonalNote { Id, UserId, Text, Date (nullable) }`** — helt ny, personlig, INGEN deling/SignalR. Én model dækker BÅDE "klistermærke" (Date = null) OG "manuelt tavle-opslag" (Date sat) — frontenden afgør placering udelukkende fra om Date er sat, ikke to separate entiteter.

**To nye aggregerings-endpoints på `PlanController`** (ikke en separat `MinSideController`): `GET /api/plans/upcoming-todos?today=&days=30` og `GET /api/plans/standard-todos` — begge genbruger samme materialiser-og-beregn-dato-i-C#-mønster som `GetById` allerede brugte.

### 4. Budget
Tidsløst regneark der afspejler, hvordan en vilkårlig måned ser ud — IKKE måneds-baseret eller "gentagende", ingen rollover.

**Indhold:** `BudgetFixedEntry { Name, Type (Income/Expense), Amount }` redigeres direkte. `BudgetVariableCategory { Name, Type, ExpectedAmount, ActualAmount }` — **afklaret med Emil efter et længe åbent spørgsmål:** ÉT redigerbart tal man selv retter til løbende for hver af de to felter, IKKE en transaktionsliste. Overskud beregnes client-side som to levende tal, "Forventet" og "Faktisk".

**Deling — mirrorer Kalenderens model denne gang** (IKKE Planer/ShoppingList-mønstret): `Budget { Id }`, `BudgetMember { BudgetId, UserId }`, `BudgetInvite { FromUserId, ToUserId, CreatedAt }`, `BudgetAccessService.GetOrCreateBudgetIdAsync` — invitér/accepter smelter sammen, forlad giver en fuld kopi. Dette var eksplicit specificeret af Emil ("samme model som Kalender/Opskriftsbog"), ikke en gætning — HandleListen har nu altså tre forskellige delings-mønstre i brug samtidig (se "Delings-modeller" i arkitektur-afsnittet).

---

## Generel projektkontekst (den røde tråd)

### Hvad er HandleListen
Familie-app, p.t. ca. 9 rigtige brugere fordelt på flere familier (ikke kun Emils egen husstand — relevant for GDPR-vurdering, se Privatlivspolitik nedenfor). Startede som en simpel handleliste-app, er vokset til: delte handlelister, kalender (med gentagelse + email/push-påmindelser + digest-mails), opskrifter/madplaner, et offentligt opskrifts-forum, et admin-panel, konto-indstillinger, Planer, Min Side, Budget, og et native Android-app-spor med push-notifikationer og AI-drevet opskrift-import.

### Stack
- **Backend:** ASP.NET Core 10 + EF Core 10 + SQLite. ASP.NET Identity med opake bearer-tokens (IKKE JWT) + refresh-tokens. SignalR til realtime.
- **Frontend:** Angular 22, **zoneless** (ingen Zone.js — signal-skrivninger trigger CD automatisk, men plain-felt-mutationer inde i `setTimeout`/`.subscribe()` kræver `ChangeDetectorRef.markForCheck()`). Angular Material 3 (theming understøtter lys/mørk via `color-scheme` indbygget).
- **Native:** Capacitor, kun Android indtil videre. Peger på live produktion, ikke en bundlet build — **næsten alt** kan opdateres uden ny APK, da appen bare er en indpakning om den levende hjemmeside. Kamera-funktionen til AI-import bruger et almindeligt HTML `capture`-attribut, ikke et natívt Capacitor-plugin.
- **Email:** Resend (REST, ingen SDK). Bruges til: bekræftelse/nulstilling af password (`ResendEmailSender`, Identity-bundet), OG kalender-email-påmindelser + digest-mails (`CalendarEmailSender`, en SELVSTÆNDIG søskende-klasse — IKKE en udvidelse af `ResendEmailSender`, da dens kontrakt er låst til Identity's `IEmailSender<IdentityUser>`-interface).
- **Push:** Firebase Cloud Messaging via `FirebaseAdmin` NuGet-pakke. Firebase-projekt: `handlelisten-cbdd3`. To Android-notifikationskanaler ("reminders" lydt, "reminders-quiet" stille) så gentagne påmindelser for samme event ikke brummer telefonen igen og igen.
- **AI:** Anthropic Claude via den officielle `Anthropic` NuGet-pakke (IKKE rå HTTP). Model: `claude-haiku-5-5` — valgt specifikt for lav pris, da AI-import er den ene feature med åben-ende-omkostning. Bruges til: AI-billede/tekst-opskrift-import, og "AI Ret" ingrediens-oprydning.

### Produktion
- Hetzner VPS: `167.233.197.61`, **domæne `emilvadgaardnielsen.dk`** (IKKE `handlelisten.dk`).
- SSH: `ssh -i ~/.ssh/HandleListen emil@167.233.197.61`. Nøglen findes i denne maskines `~/.ssh/`, OG som kopi i `local-only/ssh/` (git-ignoreret) til at tage med til andre maskiner.
- Caddy (`/etc/caddy/Caddyfile`): `/api/*` og `/hubs/*` → `localhost:5000`; alt andet serveres statisk fra `/var/www/handlelisten` med SPA-fallback.
- systemd-service **`handelisten.service`** (bemærk: "handel", IKKE "handle"), `WorkingDirectory=/home/emil/publish`, køres som bruger `emil`.
- **Vigtigt:** `sudo` kræver password og kan IKKE automatiseres gennem SSH fra Claude. Claude kan skrive direkte til filer i `/home/emil/...` (ejet af `emil`), men kan IKKE genstarte services eller skrive til `/var/www/handlelisten` (ejet af `caddy`). Derfor bruges altid en staging-mappe + et færdigt script, Emil selv kører. **Giv altid scriptet direkte og fuldt synligt i beskeden.**
- `.env.secrets` i publish-mappen: `Resend__ApiKey`, `Firebase__ServiceAccountKeyPath`, `Anthropic__ApiKey`. Indlæses af **systemd** (`EnvironmentFile=`), IKKE af .NET-appen selv — lokal test kræver et rigtigt eksporteret miljøvariabel (`export Anthropic__ApiKey=...`), ikke en droppet fil. En kopi ligger nu i `local-only/server-secrets/` til brug på en anden maskine (se `local-only/README.md`).

### Etableret deployment-fremgangsmåde (brug denne hver gang)
1. Backup af rigtig prod-DB (`cp` til `/home/emil/backups/` på serveren).
2. Download en kopi lokalt, køre **hele** migrationskæden mod kopien, verificere datatal uændrede.
3. `dotnet publish -c Release` (backend) + `ng build` (frontend) — kun de dele der reelt er ændret.
4. Upload til staging-mapper (`/home/emil/publish-new`, `/home/emil/frontend-new`) — Claude kan gøre dette selv uden sudo. **KRITISK: kopiér altid `.env.secrets` OG `firebase-service-account.json` manuelt fra den GAMLE publish-mappe til den nye.**
5. Emil kører selv det sidste sudo-gated swap+restart-script — **giv ham scriptet direkte i beskeden hver gang.** Backend-ændringer kræver service-genstart; rene frontend-ændringer kræver kun fil-swap i `/var/www/handlelisten`.
6. Verificere live (migrationshistorik, datatal, curl mod nye endpoints).
7. Ryd staging-mapper op.

### Vigtige arkitektur-beslutninger (så vi ikke genopfinder dem)

**Tre forskellige delings-modeller er nu i brug, bevidst — ikke en inkonsistens:**
1. **Invitér/accepter-håndtryk, én container pr. bruger** (Kalender, Opskriftsbog, Budget): `*Member`/`*Invite`-tabeller, invitation smelter de to containere sammen, at forlade giver en fuld kopi til den der forlader.
2. **Instant deling-ved-email, mange containere pr. bruger** (Handleliste, Planer): `*Guest`-tabel, ingen håndtryk, en bruger kan eje/være gæst på flere lister/planer samtidig.
3. **Ingen deling overhovedet** (Personlige noter): ren `UserId`-filtrering, ingen `*Guest`/`*Member`-tabel.

Når en ny feature skal deles, spørg EKSPLICIT hvilken af de tre der er tiltænkt — gæt ikke, det ændrer markant på omfanget.

- **Handleliste-varer:** navn + simpelt antal + kategori (fritekst med auto-udfyld-forslag). INGEN enheder her — bevidst beslutning.
- **Opskrifts-ingredienser:** navn + antal + VALGFRI mængde+enhed + valgfri kategori. Blyant-ikon til INLINE redigering af navn+kategori begge steder.
- **Kendte varer (KnownItem + KnownItemAlias):** central katalog, matching i C#-hukommelse (Unicode-korrekt), aldrig SQL `LOWER()`. Fuzzy whole-word-matching via regex. Fuldt admin-UI findes.
- **Known Item Request-kø:** ukendte ingredienser fra import lægges i en kø til Owner, uanset hvem der importerede.
- **Opskriftsbøger:** medlemskabs-model (se delings-model 1 ovenfor).
- **Opskrifts-forum:** offentligt, tværs-familie. Deling tager et ØJEBLIKSBILLEDE af opskriften. "AddedCount" tæller popularitet.
- **AI-import:** gated bag en `AiImporter`-rolle. Model valgt for lav pris.
- **Kalender-gentagelse:** `CalendarEvent.Date` er KUN første forekomst — alle senere BEREGNES on-the-fly, ALDRIG gemt som egne rækker. Samme princip genbrugt i Planer (`ResolveDate`-beregning i stedet for gemte datoer).
- **Påmindelser, to kanaler:** Push (eskalerer) og Email (én gang), separate leverings-tracking-tabeller nøglet på `(CalendarEventId, OccurrenceDate, UserId)`.
- **Digest-mails:** dagligt/ugentligt/månedligt, kl. 07:00 dansk tid.
- **Konto-sletning (`AccountController.DeleteMyAccount`):** fjerner personlige rækker + egne handlelister. For delings-model-1-containere: fjerner KUN medlemskabet, sletter hele containeren OG dens indhold hvis det efterlader nul medlemmer. For delings-model-2 (Planer): fjerner ejede planer helt (gæster mister adgang), fjerner egne gæste-rækker på andres planer. **VIGTIGT, lært to gange nu (se Kendte faldgruber):** denne metode skal manuelt udvides HVER GANG en ny feature tilføjer brugerejet data — den opdateres ikke automatisk.
- **Blokering:** én-vejs (A blokerer B). B's invitationer til A afvises som om A's email ikke findes.

### Kendte faldgruber (lært undervejs)
- **EF Core + SQLite kan ikke oversætte `string[].Contains()`** til SQL — brug altid en join-tabel.
- **EF Core + SQLite kan heller ikke oversætte `.Select(x => x.Y).DefaultIfEmpty(-1).MaxAsync()`** (brugt til at finde "næste sorteringsnummer") — brug i stedet `MaxAsync(x => (int?)x.Y) ?? -1`, en nullable-max, som oversættes fint.
- **At ORDER BY en projiceret DTO-record efter et `Join`** kan EF Core/SQLite heller ikke oversætte — sortér den RÅ entitets-query FØRST, projicer til DTO EFTER.
- **Glemt DI-registrering af en ny `*AccessService`:** Kalender/Opskriftsbog/Budgets access-services skal registreres i `Program.cs` (`builder.Services.AddScoped<XAccessService>()`) — det er IKKE automatisk, og fejler først ved kørsel ("Unable to resolve service"), ikke ved build. Planer/Handleliste har INGEN separat access-service (bare inline query i controlleren), så de rammer ikke denne fælde.
- **Konto-sletning skal udvides manuelt for hver ny brugerejet entitet** — ramt to gange (Planer/PersonalNotes først, så Budget), hvor en ny tabel blev tilføjet uden at `AccountController.DeleteMyAccount` fik en tilsvarende oprydnings-blok. Tjek ALTID denne metode igen, når en ny feature med `UserId`-ejede rækker bygges.
- **Native `<input type="time">` kan IKKE tvinges til 24-timers format via `lang`-attributten** — 12/24-timers visning styres af styresystemets/browserens regionsindstillinger, ikke siden. Løsning endte med at gå bort fra native time-input: et almindeligt tekstfelt med automatisk kolon-indsættelse mens man taster cifre (ingen AM/PM-koncept findes så, da det bare er to ciffergrupper).
- **Dialog-felter der kun initialiseres i en `startAdd()`/`startEdit()`-metode** (ikke som felt-default eller i en constructor) kan ende tomme, hvis brugeren rammer "gem" uden først at have trigget den metode — skete for `day-events-dialog.ts`'s `anchorDate`, som stod tom indtil man havde klikket "Redigér" på et EKSISTERENDE event mindst én gang. Et helt nyt event på en dag UDEN eksisterende events sendte derfor en tom dato og fik et 400 tilbage. Tjek altid om et felt faktisk bliver initialiseret på ALLE kodeveje ind i en dialog, ikke kun den du selv testede.
- **Real-time-dublet-bug-mønster:** optimistisk lokal opdatering + egen SignalR-besked kan dubliere, hvis beskeden når frem før HTTP-svaret. Løsning: dedupe-by-id-tjek.
- **Terminal/bash-encoding:** æ/ø/å i en bash `-d '...'`-streng til curl bliver ofte mangled. Skriv til en scratch-fil og brug `--data-binary "@fil" -H "Content-Type: application/json; charset=utf-8"`.
- **Migrationer med data-backfill** skal indsættes MANUELT i migrationsfilen.
- **Anthropic structured-output JSON-skema** tillader ikke `"minimum"`/`"maximum"` på tal, og ikke `null` som enum-værdi — brug en sentinel-streng, håndhæv grænser i C#.
- **systemd-service-navnet er `handelisten.service`** (ikke `handlelisten`).
- **`.env.secrets` læses af systemd, ikke af .NET-appen** — skal altid kopieres manuelt ved deploy.
- **Ingen browser-automatiserings-værktøj er tilgængeligt i dette miljø** — UI-ændringer verificeres via kode-gennemgang + build + backend curl-tests, IKKE faktisk visuel test. Sig dette ærligt, når det er relevant.
- **40MB `publish-output/`-mappe er committed i git** — afventer stadig Emils svar om oprydning.
- **ASP.NET Cores standard log-niveau (`Microsoft.AspNetCore: Warning`) logger IKKE almindelige request/response-cyklusser** (heller ikke rene 400'er uden exception) — kun identity-auth-fejl og uhåndterede exceptions (Error-niveau) kommer igennem. "Intet i loggen" beviser altså IKKE at en forespørgsel aldrig nåede serveren; brug browserens Network-fane (HAR-export er en god måde at få den rigtige status-kode og payload fra brugeren) når server-logs ikke viser noget.

### Hvad der er live i produktion lige nu
- **Min Side:** ny standard-forside — aktuel madplan, kronologisk opslagstavle (kalender + noter + plan-to-do's), sidebar med klistermærke-noter + standardplan-to-do's.
- **Handleliste:** flere delte lister, kan sammenlægges, inline-redigering, kategori-auto-udfyld, print-knap.
- **Kalender:** delt, gentagende events, push- og email-påmindelser, enkelt- vs. hele-serie-sletning, digest-mails. Tidspunkt indtastes nu med auto-kolon (intet native-picker-AM/PM-problem). "Nyt event på en tom dag"-bugen er rettet.
- **Opskrifter:** delte opskriftsbøger, tags, søgning, AI-import, "AI Ret", print af enkelt opskrift.
- **Madplaner:** kan markeres "aktuel" (vises på Min Side), genererer redigerbar handleliste-preview, print (kun navne / med fulde opskrifter).
- **Planer:** Standard/Ugeplan/Månedsplan, deling-ved-email (ligesom handlelister), print.
- **Budget:** faste indtægter/udgifter, løse kategorier med forventet/faktisk, levende overskud, delt via invitér/accepter som Kalenderen.
- **Opskrifts-forum:** offentligt tværs-familie opslagstavle, søgning, "mest brugte"-sortering.
- **Administrator** (Owner-only): known-item-request-kø, fuld kendte-vare-manager.
- **Indstillinger:** mørkt tema, skift adgangskode, digest-præferencer, brugerblokering, slet-min-konto (nu rydder Planer/Noter/Budget korrekt op også).
- **Notifikations-klokke:** globalt synlig invitations-indikator i headeren.
- **Privatlivspolitik**-side + obligatorisk afkrydsningsfelt.
- Automatisk token-fornyelse, push-token afmeldes ved log ud.
- Native Android-app via Capacitor, kamera-knap til AI-import.
- `CLAUDE.md` + `local-only/` (git-ignoreret) til at arbejde fra flere maskiner — se `local-only/README.md`.

### Distribution til familien
Ingen Play Store. Byg en APK, del den direkte, familien installerer manuelt. Da appen er en indpakning om den levende hjemmeside, kræver næsten INGEN frontend/backend-ændringer en ny APK. iOS er udskudt (kræver Apple Developer-abonnement).
