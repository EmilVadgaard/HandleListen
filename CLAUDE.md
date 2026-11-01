# HandleListen

Før du gør noget andet: læs **`PROJECT_NOTES.md`** i denne mappe. Den indeholder hele projektets historik, stack, arkitektur-beslutninger, kendte faldgruber, og hvad der er live i produktion lige nu. Den er skrevet specifikt til at give en ny session fuld kontekst hurtigt — behandl den som den primære kilde til sandhed om projektet, ikke denne fil.

Hvis du skal have adgang til produktionsserveren (SSH, deploy, hemmelige nøgler), ligger det i `local-only/README.md` — en mappe der med vilje er uden for git (se `.gitignore`). Den findes kun lokalt på maskiner Emil selv har sat op; hvis den ikke findes her, spørg Emil om at få den overført, i stedet for at gætte på serverens adgangsoplysninger.

Kort opsummeret, fra `PROJECT_NOTES.md`:
- **Stack:** ASP.NET Core 10 + EF Core 10 + SQLite (backend), Angular 22 zoneless + Angular Material 3 (frontend), SignalR til realtime, Capacitor til Android.
- **Deploy sker ALDRIG direkte af Claude** — kun forberedelse (publish, staging-upload); det sidste sudo-gatede skridt kører Emil selv, med et script Claude giver direkte i chatten.
- **Commits laves kun når Emil eksplicit ber om det.**
