<!-- Da jeg testede den originale implementation, crashede den med fejlen:

Collection was modified; enumeration operation may not execute

Problemet opstod, fordi loggeren brugte en delt List<LogLine>, som både applikationstråden og background-tråden læste og skrev til samtidig. Det gav race conditions og ustabil adfærd.

For at løse problemet erstattede jeg List<LogLine> med BlockingCollection<LogLine>, som er thread-safe og bedre egnet til producer/consumer-scenarier som asynkron logging. Dette gjorde loggeren mere stabil og fjernede concurrency-problemet. -->

## Overblik

Jeg har refaktoreret log-komponenten for at gøre den mere stabil, thread-safe og lettere at teste.

---

# Hvad var galt

- Den oprindelige løsning brugte en delt `List<LogLine>`, som ikke var thread-safe, som både applikationenstråden og background-tråden skrev til samtidigt. Det gav race conditions og fejl som "Collection was modified; enumeration operation may not execute".
- Der blev brugt polling med `Thread.Sleep`, hvilket var ineffektivt.
- Shutdown-adfærd var ustabil, og logs kunne gå tabt.
- Logging-fejl måtte ikke kunne vælte applikationen.
- Midnatsskift var svært at teste med `DateTime.Now`.

---

# Hvad jeg ændrede

## BlockingCollection

Jeg erstattede `List<LogLine>` med `BlockingCollection<LogLine>` for at få thread-safe producer/consumer-adfærd.

---

## Asynkron logging

Jeg introducerede en worker-thread (`MainLoop()`), som håndterer filskrivning i baggrunden.

`WriteLog()` lægger kun logs i køen og returnerer hurtigt.

---

## Stop-adfærd

### Stop_With_Flush()

- Skriver outstanding logs færdigt
- Venter på worker-threaden
- Lukker writer korrekt

### Stop_Without_Flush()

- Stopper hurtigt
- Discarder outstanding logs

---

## Filskift ved midnat

Loggeren opretter automatisk en ny fil, når datoen ændrer sig.

---

## IClock

Jeg introducerede `IClock`, `SystemClock` og `FakeClock` for at kunne teste tidsafhængig logik som midnatsskift.

---

# Unit tests

Jeg har lavet tests som verificerer:

- at logs bliver skrevet
- at der oprettes ny fil ved midnat
- at flush virker korrekt
- at no-flush discarder outstanding logs

---

# Manuel test

Jeg testede også løsningen manuelt ved at køre demo-applikationen og kontrollere de genererede logfiler.

---

# Hvis jeg havde haft mere tid

## async/await i stedet for Thread

Jeg ville undersøge en løsning med `Task` og `async/await`, da det er mere moderne .NET-praksis end manuel `Thread`.

Eksempel:
- cancellation kunne håndteres mere elegant
- shutdown-flow kunne blive simplere
- mindre manuel styring af threads

---

## Flere edge case-tests

Jeg ville tilføje tests for scenarier som:
- mange samtidige writes
- meget store mængder logs
- hurtig start/stop flere gange
- tomme eller null-lignende logs
- flere midnatsskift

Det ville gøre løsningen mere robust.

---

## Bedre fejlhåndtering

Lige nu håndteres logging-fejl lokalt i catch-blokkene, så de ikke vælter applikationen.

Hvis jeg havde mere tid, ville jeg overveje intern fallback-logging eller retry-strategier ved midlertidige fejl.

---

## Mere fleksibel konfiguration

Jeg ville gøre ting som:
- log-path
- filnavne
- flush-adfærd

mere fleksible, så de ikke var hardcoded direkte i koden.

Eksempel:
- log-path kunne vælges via en setting i stedet for altid at bruge `./LogTest`
- filnavne kunne tilpasses forskellige miljøer eller datoformater som 2026-05-19.log eller ErrorLog_20260519.log
- flush-adfærd kunne konfigureres afhængigt af performance eller sikkerhedskrav.