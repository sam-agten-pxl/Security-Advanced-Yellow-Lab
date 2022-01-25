# Lab - Yellow Team

In dit lab gaan we gebruiken maken van de OAuth client credentials flow om veilige machine-to-machine communicatie op te zetten. 

## Verkenning van het project

Dit project bestaat uit 3 verschillende services: 
- een OAuth Identity Server (identity)
- een Web API (api) die data aanlevert over de abonnees van voetbalclub KRC Genk
- Een Vue Web App (web) die de data ophaalt van de Web API en visualiseert

Kijk ook zeker eens naar de `docker-compose.yml` file. Dat geeft je meteen een overzicht van de verschillende services. De doelstelling van dit lab is om de services aan te passen zodat de web service enkel de data kan ophalen van de api door middel van een geldige JWT token die uitgekeerd wordt door de identity server.

Zorg dat je docker geïnstalleerd hebt op je systeem. Run dan het volgende commando om de services op te starten binnen een docker container.

    docker compose up

Zodra de container actief is, open je je browser en ga je naar `http://localhost:8080`. Je zal zien dat er niet veel gebeurd. Laat ons onderzoeken wat er misloopt.

## CORS?

We hebben de services opgestart via docker, en niet lokaal laten lopen. Als je de services lokaal zou laten draaien zou je merken dat de services wel functioneren. Dit is een goede reden om je ontwikkeling niet te beperken tot de localhost omgeving. Door de services in docker te laten draaien worden we geconfronteerd met problemen die we ook zouden krijgen als het tijd is om de services te deployen. Laat ons eens analyzeren wat er misloopt met onze web app. Open je developer tools (F12 op chrome) en refresh de pagina. Inspecteer de console. We krijgen de volgende foutmelding:

    Access to fetch at 'http://localhost:5000/api/seatholders' from origin 'http://localhost:8080' has been blocked by CORS policy: No 'Access-Control-Allow-Origin' header is present on the requested resource. If an opaque response serves your needs, set the request's mode to 'no-cors' to fetch the resource with CORS disabled.

De wep app (`http://localhost:8080`) stuurt een request naar ons api endpoint (`http://localhost:5000/api/seatholders`). Onze api weigert echter de connectie van onze web app vanwege de "CORS Policy". 

![task](./task.png) Zoek eens op wat CORS betekent. Probeer ook te achterhalen hoe we dat zouden kunnen oplossen.

![task](./task.png) Denk er aan dat de services draaien binnen een docker container. De web api draait binnen de docker container op `http://api:80`. De web app draait binnen de container op `http://web:80`. Betekent dit dat request eigenlijk gestuurd wordt van `http://web:80` naar `http://api:80`? Of van `http://localhost:8080` naar `http://api:80`? Of van `http://localhost:8080` naar `http://localhost:5000`?


## CORS Policy toevoegen

CORS staat voor Cross-Origin Resource sharing en is een beveiliging. Van welke uri staan we toe dat er requests worden gedaan op onze API? Standaard mag dat alleen maar als de request komt van dezelfde `origin`. Daarom dat je dit probleem niet hebt als je alle services lokaal draait. Dan draaien die allemaal onder `localhost` en hebben ze dus dezelfde origin. Nu draaien de services binnen een docker container en hebben ze allemaal een andere origin, een situatie die realistischer is. De api weigert standaard dus connecties. We gaan daarom onze api aanpassen zodat we requests toestaan van buitenaf.

<b>LET OP!</b> 
De oplossingen die je standaard online vindt zijn vaak oplossing die de CORS policy op `*` zetten. Dat wilt zeggen dat de api elke cross-origin request zou toestaan. Wat is het nadeel van deze manier van werken? Wanneer zou je beter werken met een vaste lijst? Wanneer doe je dat beter niet?

Wij gaan onze api instellen zodat die enkel requests mag ontvangen van onze web app. In de file `StartUp` voeg je een constante toe:

    private const string corsPolicy = "_allowSpecificOrigins";

Dat is de naam van onze CORS policy. Nu moeten we nog definieren wat die policy precies inhoudt. In de `ConfigureService` methode kan je je middleware beheren/aanmaken. Voeg daar de volgende code aan toe:

    services.AddCors(options => {
                options.AddPolicy(name: corsPolicy,
                    builder => {
                        builder.WithOrigins("...");
                    });
            });

De parameter van `WithOrigins` moet je zelf instellen. Dat kan bijvoorbeeld `http://localhost:8080` zijn of `http://web`. Welke denk jij dat de juiste is? Waarom?

We hebben onze CORS policy gedefinieerd maar nog niet geactiveerd. Daarom voegen we nog de volgende regel toe aan onze `Configure` methode:

    app.UseCors(corsPolicy);

Rebuild de services en draai ze opnieuw binnen docker. Surf naar `http://localhost:8080`. Je zal zien dat de data nu wel kan opgehaald worden door de web app.

## Client Credentials

We gaan nu de client credentials flow gebruiken. Merk op dat we voor een single-page application zoals de web app eigenlijk geen client credentials zouden moeten gebruiken. Client credentials is bedoelt voor machine-to-machine communicatie, niet voor verkeer dat verloopt via de browser. We gaan op het einde zien waarom dat zo is. Als je deze api zou willen beschermen in een realistischer scenario, zouden we gebruiksbeheer moeten toevoegen, maar dit ligt wat buiten de scope van dit vak. Vooraleer we van start kunnen gaan is het belangrijk om nog eens bij te lezen over de Client Credentials flow.

![task](./task.png) Lees de informatie over client credentials nog eens na op https://auth0.com/docs/get-started/authentication-and-authorization-flow/client-credentials-flow.

In principe volgende we de volgende stappen:
1. We registreren de api als resource bij de identity server
2. We registreren de web app als client bij de identity server
3. De web app zal een JWT access token opvragen bij de identity server
4. De web app gebruikt de token om de data op te vragen bij de api
5. De api zal een back-channel request maken (niet via de browser) naar de identity server om de token te valideren
6. Indien de validatie lukt, zal de api de resource ter beschikking stellen van de web app

We beginnen bij de eerste twee stappen, namelijk het registreren van de resources bij de identity server. 

## Registratie van Client en Resource

De identity server weet in feite niets af van de api. Het enige wat de identity server heeft, is een lijst van clients met voor elke client een lijst van scopes. Denk bijvoorbeeld aan je studentenpas, daar staat op wat je wel of niet mag doen. De identity server keert de tokens uit en op dat token staat dan de scope: "wat kan er met dit token gedaan worden?". Het is de verantwoordelijkheid van de api om die scope te controleren.

Open het `Config` bestand in het identity server project.

Pas de `ApiScopes` aan:

    public static IEnumerable<ApiScope> ApiScopes =>
        new ApiScope[]
        {
            new ApiScope("krc-genk", "KRC Genk API")
        };

Op deze manier hebben we een nieuwe scope gedefinieerd: `krc-genk`. Een scope heeft op zich geen enkele betekenis, dat moet bepaald worden op niveau van de api.

Pas de `Clients` aan:

    public static IEnumerable<Client> Clients =>
        new Client[] 
        { 
            new Client
            {
                ClientId = "dataviz",
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                ClientSecrets = 
                {
                    new Secret("sec-adv".Sha256())
                },
                AllowedScopes = { "krc-genk" }
            }
        };

We registreren op deze manier een nieuwe client aan bij de identity server. Clients zijn processen die bij deze identity server een access token kunnen opvragen. Als de client `dataviz` bij de identity server een access token wilt opvragen moet die zichzelf kenbaar maken met het client secret `sec-adv`. Het access token dat uitgekeerd wordt beperkt zicht tot de scope `krc-genk`.

## Access token opvragen via Postman

Om onze identity server te testen gaan we een access token opvragen via Postman. Op die manier kunnen we zeker zijn dat de identity server werkt. Zorg dat je eerst opnieuw build en de services voor docker opnieuw opbouwt. De identity server luistert naar `http://localhost:5002`. Surf eens naar `http://localhost:5002/.well-known/openid-configuration`. Dat is een bekend endpoint van de identity server met een lijst van verschillende endpoints. Onder andere de endpoint die we nodig hebben om een token op te halen.
We zien hier onder andere dat de identity server de client credentials flow ondersteunt. We vinden ook het endpoint waar we tokens kunnen ophalen:

    "token_endpoint":"http://localhost:5002/connect/token"

Open Postman. Stel een nieuwe POST request op naar `http://localhost:5002/connect/token`. Als je deze request zo uitstuurt krijg je een invalid request. We moeten namelijk laten weten wie we zijn. Voeg de volgende waardes toe aan de `Body` van de request:

    client_id       dataviz
    client_secret   sec-adv
    grant_type      client_credentials
    scope           krc-genk

Zorg er ook voor dat de content-type in de header ingesteld is op `application/x-www-form-urlencoded`. Stuur opnieuw de request. De response zal er als volgt uitzien:

    {
        "access_token": "...",
        "expires_in": 3600,
        "token_type": "Bearer",
        "scope": "krc-genk"
    }

Kopieer de waarde van de access token. Surf naar https://jwt.ms. Paste de waarde van je access token in het `enter token` veld om je token te kunnen bekijken. Je zal zien dat de token bestaat uit 3 delen:

- Header
- Body
- Signature

![task](./task.png) Waarom hebben we deze signature nodig? Wat is deze signature?

Onder `iss` (kort voor issuer) vinden we terug dat de token uitgekeerd is door `http://localhost:5002`. Dit is informatie die door api gebruikt gaat worden om de token te valideren. We zien ook de `client_id`, `scope` en een expiration time (`exp`).

![task](./task.png) Waarom staat het `client_secret` niet in de token?

## Access token opvragen via Web App

Nu we weten hoe de request werkt, laat ons eens kijken of ook stap 3 voor elkaar krijgen: de access token opvragen via de web app.

Open het bestand `Home.vue` onder `web/src/views/`. In de `mounted` functie zie je de volgende regel staan:

    const resp = await fetch("http://localhost:5000/api/seatholders");

Dit is de request naar de api. Voordat we die doen gaan we eerst een access token opvragen. Voeg de volgende code toe voor de request naar de api:

    //Access token
    var details = {
      'client_id': 'dataviz',
      'client_secret': 'sec-adv',
      'grant_type': 'client_credentials'
    };

    var formBody = [];
    for (var property in details) {
      var encodedKey = encodeURIComponent(property);
      var encodedValue = encodeURIComponent(details[property]);
      formBody.push(encodedKey + "=" + encodedValue);
    }
    formBody = formBody.join("&");

    let requestOptions = {
      method: 'POST',
      headers: { "Content-Type" : "application/x-www-form-urlencoded"},
      body: formBody
    }

    const accessTokenResponse = await fetch('http://localhost:5002/connect/token', requestOptions)
    const accessTokenJson = await accessTokenResponse.json();
    const accessToken = accessTokenJson.access_token;
    console.log(accessToken);

Dat is heel wat code, maar wat hier eigenlijk gebeurt is dat we een POST request opstellen op dezelfde manier als we dat in Postman gedaan hebben en die doorsturen naar de web api.

Build opnieuw de services en surf naar `http://localhost:8080`. Je zal zien dat er opnieuw iets misloopt. Inspecteer eerst de logs van de identity server. Daar zien we het volgende:

    Token request validation success, {"ClientId": "dataviz", "ClientName": null, "GrantType": "client_credentials", "Scopes": "krc-genk", "AuthorizationCode": null, "RefreshToken": null, "UserName": null, "AuthenticationContextReferenceClasses": null, "Tenant": null, "IdP": null, "Raw": {"client_id": "dataviz", "client_secret": "***REDACTED***", "grant_type": "client_credentials"}, "$type": "TokenRequestValidationLog"}

Het lijkt er dus op dat de request wel degelijk goed aankomt. Laat ons dan eens kijken naar de log van de web app. Open opnieuw de developer tools en inspecteer de console in je browser. Je zal een bekende foutmelding zien: CORS. 

We kampen hier opnieuw met een CORS probleem omdat de request naar de identity server vanuit een andere origin komt, iets dat standaard niet toegestaan is door de CORS policy. Gelukkig hebben we geleerd hoe we dit probleem moeten aanpakken.

![task](./task.png) Lost het CORS probleem tussen de identity server en de web app op.

Nadat je het CORS probleem hebt aangepakt zou je de access token moeten zien in de console van de developer tools.

## Authorizatie op de API

Voordat we de access token doorsturen naar de api moeten we ook nog zeker zijn dat die api daar effectief iets mee doet. Open opnieuw `Startup.cs` bij de api. 
Voeg het volgende toe onder `ConfigureServices`: 

    services.AddAuthentication("Bearer")
        .AddJwtBearer("Bearer", options =>
        {
            options.Authority = "http://localhost:5002";
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false
            };
        });

    services.AddAuthorization(options => {
        options.AddPolicy("ApiScope", policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireClaim("scope", "krc-genk");
        });
    });

De eerste regel zorgt ervoor dat we een nieuw "authentication scheme" definiëren. Net zoals bij de CORS policy is dit louter de definitie van de middleware, nog niet de toepassing er van. We lezen hier dat authorizatie gebeurt aan de hand van JWT bearer tokens (access tokens) en dat we enkel tokens aanvaarden die uitgekeerd zijn door `http://localhost:5002`. In een realitischer scenario zou dat bijvoorbeeld Google kunnen zijn. We zetten ook HTTPS validatie uit, dat is niet zo verstanding, maar de uitbreiding naar HTTPS is een opdracht voor de PE. De validate audience is voor ons niet zo belangrijk en ook buiten de scope van dit vak.

<b>LET OP!</b> Om de `AddJwtBearer` methode te laten werken hebben je het `Microsoft.AspNetCore.Authentication.JwtBearer` package nodige (versie 3.0.0). Normaal gezien is dat pakket echter al geïnstalleerd. Als de compiler klaagt over deze methode moet je dat pakket zelf nog even toevoegen.

We voegen nog een Authorization scheme toe (niet te verwarren met bovenstaande authentication!). We zeggen hier eigenlijk dat enkele geauthenticeerde gebruikers toegang krijgen en die gebruikers moeten ook de machtiging `krc-genk` hebben.

Pas de laatste regel van de `Configure` methode aan als volgt:

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers()
            .RequireAuthorization("ApiScope");
    });

We activeren hier de authentication en authorization scheme die we net ingesteld hebben. Bovendien zeggen we nog dat al onze controllers standaard moeten luisteren naar onze `ApiScope` policy, wat wilt zeggen dat je een access token met de juiste scope moet hebben om aan onze endpoints te kunnen ziten.

Laat ons dit eens testen. Rebuild de services. De verwachting is dat de api ons nu toegang gaat ontzeggen omdat we geen geldig access token hebben (denk eraan, de web app vraagt de token op dit moment wel op maar stuurt die nog niet door naar de api).

Surf opnieuw naar de web app (`http://localhost:8080`). Open de developer console. We zien inderdaad een 401 (Unauthorized) error:

    GET http://localhost:5000/api/seatholders 401 (Unauthorized)

## Access token versturen (Postman)

We testen eerst of het ons lukt om de access token te versturen naar de api. Open opnieuw Postman. Stel een GET request in naar `http://localhost:5000/api/seatholders`. Druk op send. We krijgen opnieuw een 401 error.

Stel in de request een nieuw headerveld in: `Authorization`. De waarde van dat veld heeft de vorm "Bearer x", waarbij je x vervangt door de access token. De access token kan je krijgen door ofwel opnieuw de POST request te sturen ofwel door die te copy/pasten van je developer console. De web app vraagt immers al een token op en print die naar de console.

De waarde van het `Authorization` header veldje ziet er dan dus ongeveer zo uit:

    Bearer eyJhbGciOiJSUzI1NiIsImt...

Stuur opnieuw de request. Je zal merken dat deze request failed. In de logs van de api vinden we het volgende:

    System.Net.Http.HttpRequestException: Cannot assign requested address (localhost:5002)

Op het moment dat de api de token krijgt, probeert de token de validatie uit te voeren door contact op te nemen met de identity server op `http://localhost:5002`. Dat adres vindt de api in het issuer veld van de access token. De api kan echter geen connectie maken met dat adres. Dat komt omdat de localhost hier eigenlijk op de host machine draait. Wat we willen is dat de api de request doorstuurt naar de dockeromgeving van de identity server. In deze `back channels` is het adres van de identity server `http://identity`. De issuer is dus niet localhost.

Die issuer wordt standaard ingesteld door de identity server. Dat gaan we moeten veranderen. Open opnieuw de `Startup` file van de identity server. En pas de volgende code aan:

    var builder = services.AddIdentityServer(x => {
                x.IssuerUri = "http://identity";
            })
        .AddDeveloperSigningCredential()
        .AddInMemoryApiScopes(Config.ApiScopes)
        .AddInMemoryClients(Config.Clients);

Hier forceren we de identity server om het issuer veld handmatig in te stellen. De api staat op dit moment ook nog zo ingesteld dat enkel access tokens van de localhost aangenomen worden. Ook dat veranderen we in de `Startup` file van de api:

    services.AddAuthentication("Bearer")
        .AddJwtBearer("Bearer", options =>
        {
            options.Authority = "http://identity";
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false
            };
        });

Rebuild de services en herstart de docker container. Vraag opnieuw een token op. Gebruik jwt.ms om te valideren dat het issuer veld (`iss`) veranderd is naar `http://identity`. Verstuur dit access token opnieuw in de GET request van postman (vergeet de access token niet aan te passen in het `Authorization` veldje!). Je zou nu terug toegang moeten hebben tot de data.

## Access token versturen (Web App)
Er rest ons nog maar 1 ding, namelijk het meesturen van de access token in de request van de web app. Open opnieuw `Home.vue`. Voeg onderstaande code toe nadat we de access token hebben verkregen:

    //Use the access token to get what we want
    requestOptions.method = 'GET';
    requestOptions.headers = {
      "Content-Type" : "application/json",
      "Authorization" : "Bearer " + accessToken
    };
    requestOptions.body = null;

We hergebruiken het `requestOptions` object voor onze GET request. Geef ten slotte deze `requestOptions` mee aan de fetch request naar de api:

    const resp = await fetch("http://localhost:5000/api/seatholders", requestOptions);

Herbuild de services en refresh de web app. 

Jawadde. Opnieuw een CORS error:

    Access to fetch at 'http://localhost:5000/api/seatholders' from origin 'http://localhost:8080' has been blocked by CORS policy: Request header field authorization is not allowed by Access-Control-Allow-Headers in preflight response.

Dat is vreemd, want we hadden toch onze CORS policy aangepast zodat requests van de `http://localhost:8080` toegestaan werden? Het probleem ligt bij de `Authorization` header in de GET request. CORS policy blokkeert niet alleen requests van een bepaalde origin maar blokkeert die requests ook als die bijvoorbeeld headers bevatten die niet toegestaan zijn.

Open opnieuw de `Startup` file van de api. Pas de CORS policy verder aan:

    services.AddCors(options => {
        options.AddPolicy(name: corsPolicy,
            builder => {
                builder.WithOrigins("...")
                .AllowAnyHeader()
                .AllowCredentials();
            });
    });

We breiden hier onze CORS policy uit zodat die ook alle headers toestaat en ook credentials toelaat. Rebuild de services. Refresh `http://localhost:8080`. We krijgen nu opnieuw een resultaat. Vanaf nu wordt de data opgehaald via een Client Credentials Flow!

## Is dit veilig?

Neen.

Uiteraard printen we nu de access token af in de developer console, maar dat is zeker niet het grote probleem. Ga naar `http://localhost:8080` en open de developer console. In chrome, ga naar het networks tabblad. Refresh de pagina. Daar staat een request tussen: `token`. Klik op deze request en kijk eens naar het tabblad `Payload`. Daar staat ons `client_secret` te grabbel.

De Client Credentials flow is niet aangeraden voor single page applications. Bovendien doen we hier ook niet aan authenticatie van de gebruiker. De reden dat we dit zo opgebouwd hebben in dit lab is omdat het ons in staat stelt om eens geconfronteerd te worden met de developer nachtmerrie die CORS is en het ons in staat stelt om eens een JWT Bearer token flow op te stellen in een complexer ecosysteem. We hopen vooral dat je ziet dat het instellen van security in een productieomgeving een veel complexere taak is dan je services werkende te krijgen op je eigen machine.

![task](./task.png) Zou het gebruik van HTTPS ons helpen om de client secret in de web app te verbergen?

![task](./task.png) Welke andere OAuth flow zou hier toepasselijk zijn? Is dit wel een correcte case om OAuth toe te passen?
