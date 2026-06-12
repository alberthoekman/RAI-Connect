# RAI — Centraal Identiteits- & Integratiehub PoC

Proof of concept voor centrale identiteitscontrole en webhook routing.

---

## Vereisten

- Docker Desktop >= 4.x (met Compose v2)

---

## Snel starten

```bash
git clone <repo>
cd RAI
docker compose up --build
```

Bij het voor het eerst opstarten zullen de docker images gebouwd en de database geseed worden. Dit kan even duren.

| Service          | URL                            | Doel                                    |
|------------------|--------------------------------|-----------------------------------------|
| Admin SPA        | http://localhost:5173          | React UI — inloggen, gebruikers, rollen |
| Identity (IdP)   | http://localhost:5100          | OpenIddict OIDC/OAuth2-server           |
| Integratohub     | http://localhost:5200          | Webhook-ontvanger + -verzender          |
| CRM-mock         | http://localhost:5300          | Beveiligde pagina + contact aanmaken    |
| Ticketing-mock   | http://localhost:5400          | Beveiligde pagina + webhook-doel        |

**Standaard Admin account:** `admin@rai.local` / `Admin1234!`

---

## Demo-scenario's

### 1. SSO + RBAC

1. Open http://localhost:5173, klik op **Inloggen** en meld je aan als `admin@rai.local`.
2. Het tabblad **Overzicht** toont de gedecodeerde ID-tokenclaims, inclusief `roles` en `permissions`.
3. Maak via het tabblad **Gebruikers** een nieuwe collega aan en wijs een rol toe.
4. Schakel op het tabblad **Rollen** een toestemming uit (bijv. `users:read`) en gebruik daarna
   **Test beveiligd eindpunt** op het tabblad Gebruikers — verwacht `403 Forbidden`.
5. Schakel de toestemming weer in en probeer opnieuw — verwacht `200 OK`.
6. Open http://localhost:5300 in een **nieuw tabblad** — de beveiligde CRM-pagina laadt zonder
   tweede inlogprompt conform SSO.

### 2. Webhook-automatisering + retry

1. Maak op de CRM pagina een nieuw contact bij **Contact aanmaken**.
2. Ga naar http://localhost:5200/api/webhooks/outbox — het event verschijnt met `status: Pending`.
3. Ververs na enkele seconden — `status: Delivered`. Het ticket is zichtbaar op
   http://localhost:5400.
4. **Probeer demo opnieuw:** stop de Ticketing-service:
   ```bash
   docker compose stop ticketing
   ```
5. Maak nog een CRM-contact aan. De hub-dispatcher probeert het, met exponentieel toenemende
   wachttijden (5 s, 25 s, 125 s), opnieuw. Zie `status: Pending` en de stijgende `attemptCount` via
   het outbox-eindpunt.
6. Herstart Ticketing:
   ```bash
   docker compose start ticketing
   ```
   Bij de volgende verzendcyclus wordt het event afgeleverd en verschuift de status naar `Delivered`.

### 3. Statuscontroles

Elke service heeft `/health` beschikbaar:

```bash
for port in 5100 5200 5300 5400; do
  echo -n "http://localhost:$port/health  "
  curl -s http://localhost:$port/health
  echo
done
```

Alle endpoints moeten `Healthy` teruggeven.

Logs zijn live te volgen via `docker compose logs -f`.

---

## Tests uitvoeren

```bash
dotnet test
```

De tests dekken:
- HMAC ondertekening/verificatie en manipulatiedetectie
- RBAC-permissiehandler (verlenen, weigeren, niet-geauthenticeerd)
- Inkomende webhook: geldige handtekening, ongeldige handtekening, idempotentie
- Outbox-dispatcher: aflevering, planning van herproberingen, dead-letter na 5 pogingen

## Secrets

Kopieer `.env.example` naar `.env` en vervang de `change-in-prod` waarden naar wat je wil.
