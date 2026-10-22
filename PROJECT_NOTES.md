# Projekt-noter til Claude (og Emil)

**Regel for denne fil:** Claude opdaterer KUN dette dokument, når Emil eksplicit beder om det. Læs det ved sessionsstart for hurtigt at samle den røde tråd op igen — men rør ikke ved det uden at blive bedt om det.

**Stående instruks fra Emil:** Hver gang Claude beder Emil om at logge ind på serveren og indsætte et script, skal scriptet altid gives direkte og fuldt ud synligt i selve beskeden til Emil — ikke kun som output fra et tool-call, som måske ikke er tydeligt synligt for ham.

Sidst opdateret: 2026-10-21 (efter en meget lang session der byggede kalender-gentagelse/email-påmindelser/digest-mails, forum, admin-panel, konto-indstillinger, og rettede en produktions-fejl i AI-import).

---

## Hvor vi stoppede

**Alt beskrevet i denne fil er bygget, testet og deployet til produktion** — der ligger ingen uafsluttede deploys eller halve features. Det eneste åbne spørgsmål er: skal det 40MB `publish-output/`-build-output, som er committed i gittens allerførste commits (fra før denne session), ryddes ud af git? Emil har ikke svaret endnu — spørg ikke igen af dig selv, vent på at han nævner det, eller spørg hvis det naturligt kommer op.

**Emil er i gang med at designe 4 nye, store features, men har EKSPLICIT sagt han ikke er klar til at starte implementeringen endnu.** Start IKKE på nogen af dem uden at Emil selv siger "lad os gå i gang" — se detaljeret spec for hver i næste sektion. Når han er klar, er den anbefalede byggerækkefølge: **Print-knap → Planer → Min Side → Budget** (se hvorfor under hver).

---

## Næste store features (fuldt designet, IKKE startet — læs grundigt før I begynder)

### 1. Print-knap (den letteste af de fire)
Print-knap på Handleliste, Kalender, og potentielt andre sider. Ren frontend: browserens indbyggede `window.print()` + `@media print`-CSS der skjuler navigation/knapper og formaterer indholdet pænt til papir. **Ingen backend-ændring nødvendig.** Ingen afhængigheder til de andre tre — kan bygges helt isoleret, når som helst.

### 2. Planer
En "plan" er en liste, potentielt med datoer og to-do's. Tre typer:
- **Ugeplan:** et antal dage (standard 7, evt. konfigurerbart). Mens planen er i UDKAST (ikke aktiveret), vises dagene med relative labels: "1. dag", "2. dag" osv. — IKKE rigtige ugedage.
- **Månedsplan:** vælg en måned, tilføj to-do's til hver dag i måneden. Ligner kalenderen visuelt, men er bevidst et SEPARAT, lettere system — må IKKE fylde den rigtige Kalender-side op med indhold.
- **Standard:** ingen datoer overhovedet — bare én kolonne man tilføjer to-do's til, ligesom nuværende Madplan-UI.

**Livscyklus:**
- Der er KUN en "aktivér"-handling, ingen "afslut/arkivér"-handling.
- Ved aktivering af en ugeplan: "1. dag" bliver den kommende dag (i morgen), og resten følger kronologisk derfra — nu med rigtige datoer.
- Flere planer kan være aktive samtidig.
- Man kan stadig redigere/tilføje to-do's mens en plan er aktiv.
- Selve STRUKTUREN (antal dage) låses sandsynligvis ved aktivering — men dette er bevidst holdt åbent, kan løsnes senere hvis det føles for stift i praksis. Ikke en hård beslutning.
- En aktiv plan falder bare naturligt ud af Min Side's opslagstavle, når dens datoer er overstået (ingen explicit "færdig"-knap) — men planen selv forsvinder ikke, den er stadig tilgængelig fra Planer-listen, bare uden at blive vist på forsiden længere.

**To-do-elementet skal være bevidst MINIMALT:** titel + valgfri dato/tid + udført-flueben. INGEN gentagelse, INGEN påmindelser på det enkelte to-do — det findes allerede i Kalenderen. Planer er den lette, ikke-overvældende udgave, ikke en ny kalender.

**Sammenhæng til Min Side (se næste punkt):**
- Ugeplan- og månedsplan-to-do's (som HAR datoer efter aktivering) vises på Min Side's kronologiske opslagstavle, blandet med kalenderevents.
- Standardplan-to-do's (som IKKE har datoer) vises i stedet i sticky-notes-sidebaren på Min Side, sammen med brugerens egne noter.

**Hvorfor nr. 2 i rækkefølgen:** Min Side's fulde værdi (opslagstavle med rigtigt indhold) kræver at Planer findes først.

### 3. Min Side
Bliver den NYE standard-forside når man er logget ind (erstatter nuværende `redirectTo: 'handleliste'` i `app.routes.ts`).

**Layout, top til bund:**
1. **Øverst:** den aktuelle madplan — hvad man har bestemt man skal have at spise de kommende dage.
2. **Midt (hovedindhold):** en "opslagstavle" — visuelt inspireret af Facebook (kort i en feed), men **udelukkende personlig** indhold, ikke socialt/delt med andre. Viser, i kronologisk "kommende" rækkefølge:
   - Kommende events fra brugerens egne OG delte kalendere.
   - Ting brugeren selv tilføjer direkte til tavlen (manuelle opslag).
   - Ugeplan/månedsplan to-do's fra aktive planer (når Planer-featuren findes).
3. **Venstre side:** en sticky-notes-sidebar — rene personlige noter brugeren selv tilføjer. Modtager OGSÅ aktiverede standardplaners to-do's (de har ingen dato og passer derfor ikke i den kronologiske tavle).

**Designanbefaling (Claudes eget input, ikke eksplicit bekræftet af Emil, men ingen indvendinger):** byg opslagstavlen som et generisk "kort"-system fra start (event-kort, note-kort, todo-kort som forskellige korttyper via en fælles grænseflade) — i stedet for at hardcode kun kalender+noter nu og skulle bygge det om, når Planer-to-do's skal tilføjes som en tredje korttype senere.

**Hvorfor nr. 3 i rækkefølgen:** binder madplan + kalender + planer + noter sammen — giver mest værdi når Planer allerede findes.

### 4. Budget-planlægger (BYGGET og deployet 2026-10-22)
Tidsløst regneark der afspejler, hvordan en vilkårlig måned ser ud — IKKE måneds-baseret eller "gentagende", ingen rollover, intet gentagelses-system (modsat kalenderens Weekly/Monthly/Yearly).

**Indhold, som bygget:**
- **Faste indtægter/udgifter** (`BudgetFixedEntry`): navn + type (Income/Expense) + ét beløb. Redigeres direkte, ingen automatik.
- **Løse/budgetterede kategorier** (`BudgetVariableCategory`): navn + type + to redigerbare felter, "Forventet" og "Faktisk" — **afklaret med Emil:** ÉT tal man selv retter til løbende, IKKE en transaktionsliste.
- **Beregnet overskud:** vises som to levende tal, "Forventet overskud" (fast beløb + forventet-feltet) og "Faktisk overskud" (fast beløb + faktisk-feltet), begge beregnet client-side.
- **Delt med familien** via samme medlemskabs-/invitations-model som Kalender (`Budget`/`BudgetMember`/`BudgetInvite`/`BudgetAccessService`) — invitér/accepter smelter sammen, forlad giver en fuld kopi.

**EKSPLICIT UDSKUDT (del 2, ikke bygget endnu):** "sætte penge til side" + visualisering af vækst over tid (en graf over akkumuleret opsparing).

---

## Generel projektkontekst (den røde tråd)

### Hvad er HandleListen
Familie-app, p.t. ca. 9 rigtige brugere fordelt på flere familier (ikke kun Emils egen husstand — relevant for GDPR-vurdering, se Privatlivspolitik nedenfor). Startede som en simpel handleliste-app, er vokset til: delte handlelister, kalender (med gentagelse + email/push-påmindelser + digest-mails), opskrifter/madplaner, et offentligt opskrifts-forum, et admin-panel, konto-indstillinger, og et native Android-app-spor med push-notifikationer og AI-drevet opskrift-import.

### Stack
- **Backend:** ASP.NET Core 10 + EF Core 10 + SQLite. ASP.NET Identity med opake bearer-tokens (IKKE JWT) + refresh-tokens. SignalR til realtime.
- **Frontend:** Angular 22, **zoneless** (ingen Zone.js — signal-skrivninger trigger CD automatisk, men plain-felt-mutationer inde i `setTimeout`/`.subscribe()` kræver `ChangeDetectorRef.markForCheck()`). Angular Material 3 (theming understøtter lys/mørk via `color-scheme` indbygget).
- **Native:** Capacitor, kun Android indtil videre. Peger på live produktion, ikke en bundlet build — **næsten alt** kan opdateres uden ny APK, da appen bare er en indpakning om den levende hjemmeside. Kamera-funktionen til AI-import bruger et almindeligt HTML `capture`-attribut, ikke et natívt Capacitor-plugin.
- **Email:** Resend (REST, ingen SDK). Bruges til: bekræftelse/nulstilling af password (`ResendEmailSender`, Identity-bundet), OG kalender-email-påmindelser + digest-mails (`CalendarEmailSender`, en SELVSTÆNDIG søskende-klasse — IKKE en udvidelse af `ResendEmailSender`, da dens kontrakt er låst til Identity's `IEmailSender<IdentityUser>`-interface).
- **Push:** Firebase Cloud Messaging via `FirebaseAdmin` NuGet-pakke. Firebase-projekt: `handlelisten-cbdd3`. To Android-notifikationskanaler ("reminders" lydt, "reminders-quiet" stille) så gentagne påmindelser for samme event ikke brummer telefonen igen og igen.
- **AI:** Anthropic Claude via den officielle `Anthropic` NuGet-pakke (IKKE rå HTTP). Model: `claude-haiku-5-5` — valgt specifikt for lav pris, da AI-import er den ene feature med åben-ende-omkostning. Bruges til: AI-billede/tekst-opskrift-import, og "AI Ret" ingrediens-oprydning.

### Produktion
- Hetzner VPS: `167.233.197.61`, **domæne `emilvadgaardnielsen.dk`** (IKKE `handlelisten.dk` — det gættede jeg forkert på tidligere i et script og skal huskes).
- SSH: `ssh -i ~/.ssh/HandleListen emil@167.233.197.61` (nøgle findes allerede i denne maskines `~/.ssh/`).
- Caddy (`/etc/caddy/Caddyfile`): `/api/*` og `/hubs/*` → `localhost:5000`; alt andet serveres statisk fra `/var/www/handlelisten` med SPA-fallback (`try_files {path} /index.html`) — dybe links virker altså direkte.
- systemd-service **`handelisten.service`** (bemærk: "handel", IKKE "handle" — nem at skrive forkert, har allerede kostet en fejlslagen deploy én gang), `WorkingDirectory=/home/emil/publish`, køres som bruger `emil`.
- **Vigtigt:** `sudo` kræver password og kan IKKE automatiseres gennem SSH fra Claude. Claude kan skrive direkte til filer i `/home/emil/...` (emils egne mapper, ejet af `emil`), men kan IKKE genstarte services eller skrive til `/var/www/handlelisten` (ejet af `caddy`). Derfor bruges altid en staging-mappe + et færdigt script, Emil selv kører. **Giv altid scriptet direkte og fuldt synligt i beskeden, ikke kun via et tool-call.**
- `.env.secrets` i publish-mappen: `Resend__ApiKey`, `Firebase__ServiceAccountKeyPath`, `Anthropic__ApiKey`. Indlæses af **systemd** via `EnvironmentFile=-/home/emil/publish/.env.secrets` i unit-filen — IKKE læst af .NET-appen selv som en dotenv-fil. Det betyder: at droppe filen ind i en LOKAL dev-mappe gør INGENTING; for lokal test med en rigtig nøgle skal den sættes som et rigtigt miljøvariabel (`export Anthropic__ApiKey=...`) før `dotnet run`. `.gitignore` dækker nu `.env.*` (udvidet fra kun `.env` efter at have opdaget hullet).

### Etableret deployment-fremgangsmåde (brug denne hver gang)
1. Backup af rigtig prod-DB (`cp` til `/home/emil/backups/` på serveren).
2. Download en kopi lokalt, køre **hele** migrationskæden mod kopien (`dotnet ef database update --connection "..."`).
3. Verificere datatal (brugere/lister/items) er uændrede efter migration.
4. `dotnet publish -c Release` (backend) + `ng build` (frontend, production som default) — kun de dele, der reelt er ændret, behøver bygges/deployes.
5. Upload til staging-mapper på serveren (`/home/emil/publish-new`, `/home/emil/frontend-new`) — Claude kan gøre dette selv uden sudo. **KRITISK: kopiér altid `.env.secrets` OG `firebase-service-account.json` manuelt fra den GAMLE publish-mappe til den nye** — de er IKKE en del af `dotnet publish`-output og forsvinder stille, hvis man glemmer dem (skete én gang, slog push-notifikationer fuldstændig fra i flere dage uden fejl i loggen udover en nem-at-overse "Firebase is not configured"-advarsel).
6. Emil kører selv det sidste sudo-gated swap+restart-script — **giv ham scriptet direkte i beskeden hver gang.** Backend-ændringer kræver service-genstart (`stop` → kopier → `start`); rene frontend-ændringer kræver kun fil-swap i `/var/www/handlelisten`, ingen service-genstart.
7. Verificere live (migrationshistorik, datatal, curl mod nye endpoints).
8. Ryd staging-mapper op.

**Ny lektie om AI-relaterede ændringer specifikt:** test ALTID et ægte kald mod den rigtige Anthropic-API med den rigtige nøgle før deploy, hvis man ændrer et JSON-skema til structured output — en lokal opsætning uden nøgle giver kun en "ikke konfigureret"-besked og tester IKKE om skemaet reelt er gyldigt. (Se "Kendte faldgruber" for den konkrete fejl dette forårsagede.)

### Vigtige arkitektur-beslutninger (så vi ikke genopfinder dem)
- **Handleliste-varer:** navn + simpelt antal + kategori (nu fritekst med auto-udfyld-forslag, ikke en fast liste). INGEN enheder her — bevidst beslutning.
- **Opskrifts-ingredienser:** navn + antal + VALGFRI mængde+enhed (g/kg/ml/dl/l/stk) + valgfri kategori. Grupperes under kategori-overskrifter ved visning, ligesom Handlelisten. Begge steder har nu et blyant-ikon til INLINE redigering af navn+kategori (genbruger samme forslags-komponent som ved tilføjelse).
- **Kendte varer (KnownItem + KnownItemAlias):** central katalog. Matching ALTID i C#-hukommelse med Unicode-korrekt sammenligning, aldrig SQL `LOWER()`. Fuzzy whole-word-matching (regex) betyder "tsk stødt kanel" matcher en alias "kanel". Et fuldt admin-UI findes nu (se Administrator nedenfor) — tidligere krævede alle ændringer at Claude kørte rå SQL på serveren.
- **Known Item Request-kø:** når en import (link/AI-tekst/AI-billede/"AI Ret") ikke kan matche en ingrediens, lægges den automatisk i en kø til Owner — uanset hvem der importerede. Samme ingrediens der dukker op flere gange tæller op i stedet for at stable duplikater.
- **To-lags badge-system:** "Known" vs "History" — delt mellem handleliste-varer og opskrifts-ingredienser.
- **Opskriftsbøger:** ren medlemskabs-model. Invitation+accept smelter bøger sammen; at forlade giver en fuld kopi til den der forlader.
- **Opskrifts-forum:** offentligt, tværs-familie. Deling tager et ØJEBLIKSBILLEDE (frossen kopi) af opskriften — senere redigeringer af originalen påvirker IKKE forum-opslaget. "Tilføj til min bog" er ét klik, ingen gennemse-skærm. Et "AddedCount" tæller popularitet. Søgninger logges og top-15 vises som scroll-bare forslags-kort.
- **AI-import (billede/tekst/"AI Ret"):** gated bag en `AiImporter`-rolle (Owner har den altid automatisk) — IKKE givet til alle 9 brugere, da hvert kald koster reelle penge. Model valgt specifikt for lav pris (`claude-haiku-5-5`).
- **Kalender-gentagelse:** `CalendarEvent.Date` er KUN den første forekomst for en gentagende serie — alle senere forekomster BEREGNES on-the-fly (`RecurringEventService`), ALDRIG gemt som egne rækker. Månedlig/årlig klemmer dag-i-måneden til den sidste gyldige dag (dag 31 → dag 30/28 i kortere måneder, Feb 29 håndteres). `CalendarEventException`-tabel holder styr på enkelte-forekomst-sletninger uden at røre serien. Redigering af en gentagende serie rammer ALTID hele serien (ingen "kun denne forekomst"-redigering, kun sletning).
- **Påmindelser, to kanaler:** Push (eskalerer: hver time → hvert 15. min → hvert 5. min, aldrig stoppende før man ser den) og Email (ÉN gang, ingen gentagelse) — hver med sin egen leverings-tracking-tabel, nu nøglet på `(CalendarEventId, OccurrenceDate, UserId)` i stedet for bare `(CalendarEventId, UserId)`, da gentagelse betyder samme event-id kan have mange forskellige forekomster over tid.
- **Digest-mails:** dagligt/ugentligt/månedligt, alle sendes kl. 07:00 dansk tid (Emil bekræftede dette som fint, selvom "ugentligt" oprindeligt var tænkt som søndag AFTEN — nu søndag MORGEN kl. 07). Ingen mail sendes hvis perioden er tom. "Send nu"-knap genbruger samme indholds-logik.
- **Konto-sletning:** fjerner personlige rækker (push-tokens, digest-præferencer, påmindelser, blokeringer, known-item-requests, privatlivs-accept) + egne handlelister. For delt Kalender/Opskriftsbog: fjerner KUN medlemskabet — hvis det efterlader nul medlemmer, slettes hele strukturen OG dens indhold; indhold andre stadig har adgang til røres IKKE.
- **Blokering:** én-vejs (A blokerer B). Effekt: B kan ikke længere sende A invitationer — A's server lader som om B's email ikke findes (afslører ikke blokeringen for B).

### Kendte faldgruber (lært undervejs)
- **EF Core + SQLite kan ikke oversætte `string[].Contains()`** til SQL — brug altid en join-tabel.
- **Real-time-dublet-bug-mønster:** optimistisk lokal opdatering + egen SignalR-besked kan dubliere, hvis beskeden når frem før HTTP-svaret. Løsning: dedupe-by-id-tjek. Rettet de fleste steder, ikke systematisk 100% gennemgået alle steder.
- **Terminal/bash-encoding:** æ/ø/å i en bash `-d '...'`-streng til curl bliver ofte mangled. Skriv altid til en scratch-fil og brug `--data-binary "@fil" -H "Content-Type: application/json; charset=utf-8"`.
- **Migrationer med data-backfill:** skal indsættes MANUELT i migrationsfilen mellem "tilføj kolonne" og brug af kolonnen — senest gjort for `ReminderDeliveries.OccurrenceDate` (backfillet fra den tilhørende `CalendarEvent.Date`, da gentagelse blev indført).
- **Anthropic structured-output JSON-skema har skjulte begrænsninger**, som IKKE fejler lokalt uden en rigtig nøgle (giver bare "ikke konfigureret"): `"minimum"`/`"maximum"` er IKKE tilladt på `integer`/`number`-typer, og `null` er IKKE tilladt som enum-værdi når `type` er et array (f.eks. `["string","null"]` + `enum` der indeholder `null`). Brug i stedet en sentinel-streng som `"none"` og map den til `null` i C#, og håndhæv numeriske grænser i C# i stedet for skemaet. Dette forårsagede en reel produktions-fejl (AI-import svarede altid "fandt ingen opskrift", selvom den rigtige fejl var en 400 Bad Request fra Anthropic) — testet og rettet 2026-10-21.
- **systemd-service-navnet er `handelisten.service`** (ikke `handlelisten`) — forårsagede en fejlslagen deploy tidligere, da stop/start-kommandoerne brugte det forkerte navn.
- **`.env.secrets` læses af systemd, ikke af .NET-appen** — se Produktion-sektionen ovenfor. Skal ALTID kopieres manuelt ved deploy, ellers går push/email/AI-funktioner stille i stykker.
- **Ingen browser-automatiserings-værktøj er tilgængeligt i dette miljø** — UI-ændringer verificeres via kode-gennemgang + succesfuld build + backend curl-tests, IKKE faktisk visuel browser-test. Sig dette ærligt til Emil, når det er relevant, i stedet for at hævde en side er testet visuelt.
- **40MB `publish-output/`-mappe er committed i git** siden de allerførste commits — ikke dækket af `.gitignore`. Flagget til Emil, afventer hans svar om den skal ryddes.

### Hvad der er live i produktion lige nu
- **Handleliste:** flere delte lister, kan sammenlægges, inline-redigering af vare-navn/kategori, kategori med auto-udfyld.
- **Kalender:** delt, **gentagende events** (ugentligt/månedligt/årligt), push- OG email-påmindelser (uafhængigt valgbare per event), enkelt-forekomst- vs. hele-serie-sletning, **digest-mails** (dagligt/ugentligt/månedligt + "send nu").
- **Opskrifter:** delte opskriftsbøger, tags, søgning, kategoriserede ingredienser med inline-redigering, link-import (schema.org), **AI-import** (tekst/billede/kamera, rolle-gated), "AI Ret" ingrediens-oprydning.
- **Madplaner:** genererer redigerbar handleliste-preview.
- **Opskrifts-forum:** offentligt tværs-familie opslagstavle for opskrifter, søgning, "mest brugte"-sortering, populære-søgninger-kort.
- **Administrator** (Owner-only): known-item-request-kø med badge-indikator, fuld kendte-vare-manager (opret/redigér/slet/alias).
- **Indstillinger** (alle brugere): mørkt tema, skift adgangskode, digest-præferencer, brugerblokering, slet-min-konto.
- **Notifikations-klokke:** globalt synlig invitations-indikator (kalender + opskriftsbog) i headeren på alle sider.
- **Privatlivspolitik**-side + obligatorisk afkrydsningsfelt ved oprettelse.
- Automatisk token-fornyelse, push-token afmeldes ved log ud.
- Native Android-app via Capacitor (peger på live produktion), kamera-knap til AI-import.

### Distribution til familien
Ingen Play Store. Byg en APK, del den direkte (mail/Drive), familien installerer manuelt ("tillad installation fra ukendt kilde"). **Vigtig indsigt fra denne session:** da appen bare er en indpakning om den levende hjemmeside, kræver næsten INGEN af ændringerne i denne session en ny APK — kun ægte native-niveau-ændringer (nye Android-tilladelser, nye plugins) ville. Kamera-funktionen blev tilføjet uden at kræve en ny APK, da den bruger et almindeligt HTML-attribut, ikke et nativt plugin. iOS er udskudt (kræver Apple Developer-abonnement selv til TestFlight).
