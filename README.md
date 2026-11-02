# Caramel Dog Clicker

Cookie-clicker feito em Unity 6 (6000.3.27f1, 2D URP) que roda **somente como build WebGL**. Você clica no cachorro caramelo para ganhar petiscos.

Detalhes de arquitetura e do fluxo de trabalho com o Editor estão em [CLAUDE.md](CLAUDE.md).

## Gerar o build WebGL

Na raiz do projeto, com o Editor aberto:

```powershell
unity command build run --project-path "$PWD" -- --target WebGL --outputPath Build/WebGL --confirm true
unity command build status --project-path "$PWD" --format json   # repita até "completed"
```

Para testar localmente:

```powershell
python -m http.server 55553 -d Build/WebGL
```

e abra `http://localhost:55553`. A pasta `Build/` está no `.gitignore`.

## Deploy no Vercel

O Vercel não roda o Unity, então o deploy é feito a partir do build já gerado, pela CLI.

```powershell
ls Build\WebGL\Build          # confira os nomes dos arquivos (.br ou .unityweb)
cd Build\WebGL
copy ..\..\vercel.json .
npx vercel login              # só na primeira vez
npx vercel deploy --prod
```

Na primeira vez, a CLI faz algumas perguntas:

- Set up and deploy: **Y**
- Link to existing project: **N**
- Project name: o que quiser (por exemplo `caramel-dog-clicker`)
- Directory: **`./`**
- Framework: **Other**, sem build command

O link do projeto fica salvo em `Build\WebGL\.vercel`, então para atualizar basta gerar o build de novo e repetir `npx vercel deploy --prod`.

### Sobre o `vercel.json`

Ele define `Content-Encoding`/`Content-Type` para os arquivos Brotli (`.br`) dentro de `/Build/`. O build usa Brotli com Decompression Fallback, então o jogo também roda sem esses headers, só carrega mais devagar. Se os arquivos gerados tiverem outra extensão (como `.unityweb`), ajuste os padrões `source`.

O save usa `PlayerPrefs` (IndexedDB do navegador), então ele não é compartilhado entre domínios (`localhost` e `*.vercel.app`).
