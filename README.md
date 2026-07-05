# Jellyfin.Plugin.Hardcover

A Jellyfin metadata plugin that fetches **book** and **author** metadata from
[Hardcover](https://hardcover.app) via its GraphQL API.

What it does:

- **Book provider** — searches Hardcover by title, and pulls title, overview,
  release date/year, community rating, cover art, and a link back to the
  Hardcover page. Also attaches contributors (author, translator, illustrator,
  editor) as People on the book.
- **Author provider** — searches Hardcover by name, and pulls bio, birth/death
  dates, photo, and a link back to the Hardcover author page. This runs
  automatically for the People created by the book provider, or on its own if
  you point a Person library entry at it.
- **Image provider** — supplies the book cover / author photo for either of
  the above once a Hardcover match exists.

## 1. Get a Hardcover API token

1. Log in at [hardcover.app](https://hardcover.app).
2. Go to **Account Settings → API** (`https://hardcover.app/account/api`).
3. Generate a Personal API Token and copy it. Tokens expire yearly and reset
   Jan 1st — you'll need to refresh it in the plugin config once a year.

## 2. Build the plugin

You need the **.NET 8 SDK** on the machine you build on (this doesn't have to
be your Jellyfin server — build anywhere, then copy the `.dll` over).

```bash
git clone <this repo> Jellyfin.Plugin.Hardcover
cd Jellyfin.Plugin.Hardcover
dotnet restore
dotnet publish --configuration Release --output publish
```

This produces `publish/Jellyfin.Plugin.Hardcover.dll`.

**Important — match your Jellyfin server version.** The `Jellyfin.Controller`
and `Jellyfin.Model` NuGet package versions in `Jellyfin.Plugin.Hardcover.csproj`
must match your running server version, or the plugin will fail to load with
an error like `Error loading assembly ... version mismatch` in the Jellyfin
log. Check your version:

```bash
jellyfin --version
# or Dashboard -> About in the web UI
```

Then edit the `<JellyfinVersion>` property near the top of the `.csproj` to
match (e.g. `10.9.11`), and re-run `dotnet restore`.

There's also a GitHub Actions workflow (`.github/workflows/build.yml`) set up
so pushing to `main` builds it automatically and tagging `vX.Y.Z` cuts a
release zip — handy if you want CI to build it on GitHub instead of on `arx`.

## 3. Install on your server

```bash
mkdir -p /var/lib/jellyfin/plugins/Hardcover
cp publish/Jellyfin.Plugin.Hardcover.dll /var/lib/jellyfin/plugins/Hardcover/
systemctl restart jellyfin   # or restart the Jellyfin container
```

(Path above is the standard Debian path; adjust if your Jellyfin data dir is
elsewhere, e.g. inside a Docker volume.)

## 4. Configure and enable

1. **Dashboard → Plugins → Hardcover Metadata** — paste your API token, save.
2. **Dashboard → Libraries → (your Books library) → Manage Library →
   Metadata downloaders** — tick **Hardcover** for both **Books** and, if
   Jellyfin lists it separately for your version, **Persons**. Order it above
   or below other providers depending on which source you want to win on
   conflicting fields.
3. Refresh metadata on a book (or the whole library) to trigger a lookup.

## 5. Turn this into an installable plugin repo (skip manual .dll copying)

Once this is pushed to GitHub, tagging a release triggers CI to build the
plugin, attach the `.zip` to a GitHub Release, and auto-update `manifest.json`
in the repo with the correct checksum and download URL. Point Jellyfin's
**Dashboard → Plugins → Repositories** at the raw `manifest.json` URL and it
shows up in the plugin catalog like any built-in source — install/update from
the UI, no file copying.

1. Push this repo to GitHub (see below if you're new to this).
2. On GitHub, go to **Releases → Draft a new release**, tag it `v1.0.0.0`
   (must start with `v`), publish it.
3. Wait ~1 minute for the **Actions** tab to finish the run — it builds the
   `.dll`, attaches `Jellyfin.Plugin.Hardcover.zip` to the release, and
   commits an updated `manifest.json` to `main`.
4. Your manifest URL is:
   `https://raw.githubusercontent.com/<your-username>/<repo-name>/main/manifest.json`
5. In Jellyfin: **Dashboard → Plugins → Repositories → Add Repository** →
   paste that URL as the repository URL (name it anything, e.g. "Hardcover") → Save.
6. **Dashboard → Plugins → Catalog** → find "Hardcover Metadata" under
   **Metadata** → Install → restart Jellyfin.
7. Configure the API token as in step 4 above.

Every time you tag a new release (bump the version in `Jellyfin.Plugin.Hardcover.csproj`'s
`AssemblyVersion`/`FileVersion` and in `build.yaml`'s `version` first), the
catalog entry gets a new version Jellyfin can update to with one click.



The Hardcover API is explicitly [in beta and can change without notice](https://docs.hardcover.app/api/getting-started/),
and the Jellyfin plugin ABI is version-sensitive, so a couple of things here
are "best effort, verify against your actual build":

- **GraphQL field names.** The book queries (`title`, `release_date`, `pages`,
  `rating`, `users_count`, `image { url }`, `contributions { author { id name } }`)
  are confirmed against Hardcover's docs and example queries. The author
  fields `bio`, `born_date`, `death_date` are the most likely names but aren't
  documented as explicitly — if author bios come back empty, open
  `https://hardcover.app` → GraphQL console (link in Getting Started docs),
  run `{ authors(limit: 1) { id name } }`, then use the schema explorer there
  to confirm the exact field names and adjust the query in
  `Api/HardcoverApiClient.cs` (`GetAuthorAsync`/`SearchAuthorsAsync`).
- **`PersonKind` enum values.** `Translator`, `Illustrator`, and `Editor` were
  added to Jellyfin's `PersonKind` enum for book support; if your target
  server version predates that, the compiler will point you at the missing
  member in `HardcoverBookProvider.MapContributionToPersonKind` — swap in
  whatever the closest equivalent is on your version (often just `Author` for
  all of them).
- **`RemoteSearchResult.Overview`.** Used to show "by \<author\>" in the
  manual-match picker. If that property doesn't exist on your target ABI,
  just delete that one assignment in `HardcoverBookProvider.ToSearchResult` —
  everything else works without it.

None of these affect the core flow (search → match → pull metadata → pull
image); they're minor polish that a `dotnet build` against your exact server
version will surface immediately as compiler errors if something moved.

## Project layout

```
Jellyfin.Plugin.Hardcover.csproj
HardcoverPlugin.cs                 # plugin entry point + config page registration
PluginServiceRegistrator.cs        # wires up the named HttpClient + HardcoverApiClient
Configuration/
  PluginConfiguration.cs           # API token + search tuning, saved by Jellyfin
  configPage.html                  # dashboard settings page
Api/
  HardcoverApiClient.cs            # GraphQL calls, auth, rate limiting, error handling
  Models/                          # DTOs matching Hardcover's snake_case GraphQL schema
Providers/
  HardcoverBookProvider.cs         # IRemoteMetadataProvider<Book, BookInfo>
  HardcoverPersonProvider.cs       # IRemoteMetadataProvider<Person, PersonLookupInfo>
  HardcoverImageProvider.cs        # IRemoteImageProvider for both of the above
```

## Rate limits

Hardcover caps the API at 60 requests/minute per token. The client throttles
itself to roughly one request per 1.1s, which is plenty for on-demand/library
scan metadata lookups but would be too slow for bulk backfilling a huge
library in one go — if you ever batch-refresh thousands of books, expect it
to take a while rather than hammering the API.
