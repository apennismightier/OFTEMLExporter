# OFT + EML Exporter (Ready for Vercel via Docker)

This is a complete, ready-to-deploy .NET 8 Minimal API that exports **OFT** (Windows Outlook template) and **EML** (preview/Mac). Included:
- `Program.cs` (the API)
- `OftEmlExporter.csproj` (NuGet ref: Aspose.Email)
- `Dockerfile` and `.dockerignore` (so Vercel can deploy via Docker)
- `openapi.yaml` (paste into ChatGPT → Create a GPT → Actions)

## Local quick start
```bash
dotnet restore
dotnet run
# open http://localhost:5000
```

## Test
```bash
curl -sX POST http://localhost:5000/preview \
 -H 'Content-Type: application/json' \
 -d '{"subject":"Hello","htmlBody":"<p>Hi {{FirstName}}</p>","to":["you@example.com"]}'
```
```bash
curl -sX POST http://localhost:5000/export \
 -H 'Content-Type: application/json' \
 -d '{"subject":"Hello","htmlBody":"<p>Hi {{FirstName}}</p>","formats":["oft","eml"],"to":["you@example.com"]}'
```

## Vercel deployment (Docker)
1. Push this folder to GitHub.
2. In Vercel → **Add New → Project** → Import your repo.
3. Vercel auto-detects the Dockerfile. Accept defaults and Deploy.
4. Copy the generated `https://<project>.vercel.app` URL.
5. Edit `openapi.yaml` → set `servers.url` to your URL.
6. In ChatGPT → **Create a GPT → Configure → Actions → Add an API**, paste `openapi.yaml`.

## Mac user flow
- Use the EML for **preview** and **File → Save as Template (.emltpl)** in Outlook for Mac.
- Keep the OFT for Windows colleagues.