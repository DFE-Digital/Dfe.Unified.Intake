
We have the requirement to virus scan each file we want to send along to power automate in CheckYourAnswers page.

RSD ClamAV API (rsd-clamav-api) — a pure HTTP API.
Swagger: https://s184d01-rsd-frontdoor-extapp-clamapi-agfzh6gdgpegghfu.a03.azurefd.net/swagger/index.html
Clone repository: C:\Users\sazia\dfe\rsd-clamav-api

It expose REST endpoints:
- POST /scan/async — multipart file, accepts a file and returns immediately a response with a jobId indicating that it received the request and is dealing with it
- GET /scan/async/{jobId} to polls each job to find out if the file virus scan is ongoing, completed or fails. Polling could be every 5 seconds. Please make this an environment variable

There is a ClamAV api client nuget package to use to interact with ClamAV Api, please use it
https://www.nuget.org/packages/GovUK.Dfe.ClamAV.Api.Client

This Api client should be initialised with the following values, all exposed as configuration
(appsettings for the non-secret values, and user-secrets / Key Vault / environment variables for the
secret). **Never commit the client secret.**
"ClamAvApiClient:BaseUrl": "https://s184d01-rsd-frontdoor-extapp-clamapi-agfzh6gdgpegghfu.a03.azurefd.net/",
"ClamAvApiClient:ClientId": "d3531651-854e-4d65-9f08-5964816ca850",
"ClamAvApiClient:ClientSecret": "<supplied via user-secrets / Key Vault / env var — not stored here>",
"ClamAvApiClient:Authority": "https://login.microsoftonline.com/9c7d9dd3-840c-4b3f-818e-552865082e16/",
"ClamAvApiClient:Scope": "//c8c632a2-acf2-48ae-b349-2b6b069edd9c/.default"

> SECURITY: an earlier version of this file committed the real `ClientSecret` in plaintext. Treat that
> value as compromised — rotate the secret on the `d3531651-…` app registration in Azure, and set the
> new value locally with:
>
> ```
> dotnet user-secrets set "ClamAvApiClient:ClientSecret" "<new-secret>"
> ```

Currently we end the file to Power automate without first checking each file for virus.

Use the above details to add virus scan functionality before sending to power automate

1. When the user clicks on Submit button, befoe sending to Power automate, loop through the files, send each to ClamAv and store each response
2. Poll each uploaded file against ClamAv at 5 seconds interval
3. Only consider the virus checks a success when all ClamAv have reported all the files virus free. (GET /scan/async/{jobId} response status property value is clean)
4. Possible values for status are: queued, downloading, scanning, clean, infected, error
5. Once the virus checks are all clean, then we can proceed to sending the request to Power automate
6. Up until this step, after clicking the Submit button, the button should remain disabled
7. It will be nice to have ome form of gov.uk UI element visually displayed above the submit button to inform the user of the virus scan per file and its status, color coded as well why not. Make sure it aligns with gov.uk design guidelines

Tell me what you think first