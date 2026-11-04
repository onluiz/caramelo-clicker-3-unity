# AdMob (ainda não integrado)

O jogo antigo (`../caramelo-clicker-3`) usava anúncios recompensados do AdMob. Esta versão Unity **ainda não tem anúncios**; os IDs abaixo ficam guardados para quando forem integrados. IDs do AdMob não são segredos (vão dentro do app publicado), então podem ficar no repositório.

Conta/app no AdMob: "Cachorro Caramelo Clicker".

## App ID

`ca-app-pub-4380393767380236~6038175130`

No Android ele vai no `AndroidManifest.xml` como `com.google.android.gms.ads.APPLICATION_ID` (o SDK do Google Mobile Ads para Unity pede isso em *Assets > Google Mobile Ads > Settings*).

## Unidades de anúncio recompensado

| Nome no AdMob | ID da unidade |
| --- | --- |
| `rewarded-offline-earnings` | `ca-app-pub-4380393767380236/2122743178` |
| `rewarded-money-bag` | `ca-app-pub-4380393767380236/8285391492` |
| `rewarded-daily-bonus` | `ca-app-pub-4380393767380236/7344980383` |
| `rewarded-video-offer` | `ca-app-pub-4380393767380236/3263420090` |
| `rewarded-production-boost` | `ca-app-pub-4380393767380236/2665577731` |
| `rewarded-accessory` | `ca-app-pub-4380393767380236/8343737831` |

Fonte: `src/game/monetization/adUnits.ts` do projeto antigo. O `docs/admob-setup.md` de lá está desatualizado: cita uma unidade `rewarded-gems` (`.../3825381663`) que o código não usa mais.

## Notas para quando integrar

- Em desenvolvimento use sempre a unidade de teste do Google, `ca-app-pub-3940256099942544/5224354917`. Clicar em anúncio real no próprio app pode suspender a conta do AdMob.
- O fluxo de consentimento (UMP) precisa de uma mensagem publicada em *AdMob > Privacidade e mensagens*; sem ela, usuários da Europa não recebem anúncios.
- No Play Console, *Contém anúncios* e o Data safety (identificador de publicidade) devem refletir o que o app faz de fato.
