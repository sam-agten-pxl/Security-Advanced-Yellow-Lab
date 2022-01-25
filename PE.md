BEFORE: Need to accept the root.cert as trusted root ca (certs folder)

CORS

    services.AddCors(options => {
        options.AddPolicy(name: corsPolicy,
            builder => {
                builder.WithOrigins("http://web:80", "http://localhost:8080")
                .AllowAnyHeader();
            });
    });

    ...
    
    app.UseCors(corsPolicy);

Set up API for access token

    services.AddAuthentication("Bearer")
            .AddJwtBearer("Bearer", options =>
            {
                options.Authority = "https://localhost:5002";

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false
                };
            });

    ....


    app.UseAuthentication();
    app.UseAuthorization();


-> JWTBearer package

Postman request token
/.well-known/openid-configuration

Vue client

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

    const requestOptions = {
      method: 'POST',
      headers: { "Content-Type" : "application/x-www-form-urlencoded"},
      body: formBody
    }

    const accessTokenResponse = await fetch('https://localhost:5002/connect/token', requestOptions)
    const accessTokenJson = await accessTokenResponse.json();
    const accessToken = accessTokenJson.access_token;
    console.log(accessToken);

https://jwt.ms/


    //Use the access token to get what we want
    requestOptions.method = 'GET';
    requestOptions.headers = {
      "Content-Type" : "application/json",
      "Authorization" : "Bearer " + accessToken
    };
    requestOptions.body = null;



    const resp = await fetch("https://localhost:5100/api/seatholders", requestOptions);
    const json = await resp.json();
    this.data = Array.from(json, d => {
      return {
        areaCode: +d.areaCode,
        city: d.city
      }
    });

AllowAnyHeader in api (CORS blocks access token header)

Allow scope in API

    services.AddAuthorization(options => {
                options.AddPolicy("ApiScope", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("scope", "krc-genk");
                });
            });

Enforce it globally:

    app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers()
                    .RequireAuthorization("ApiScope");
            });


Client Credentials is wrong! Can't use authorization code flow (no user).

Answer? Revoke access token from api? No. Client secret and id is open in browser.


