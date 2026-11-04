# AdMob (SDK instalado, sem anúncios ainda)

O jogo antigo (`../caramelo-clicker-3`) usava anúncios recompensados do AdMob. Esta versão Unity já tem o **SDK Google Mobile Ads 11.6.0** (o `.unitypackage` oficial do GitHub `googleads/googleads-mobile-unity`, em `Assets/GoogleMobileAds` e `Assets/Plugins/Android`) e o External Dependency Manager (`com.unity.external-dependency-manager`), mas **nenhum código o chama**: não há inicialização nem anúncios. Os IDs abaixo ficam guardados para quando forem integrados. Para atualizar o SDK, importe o `.unitypackage` de uma versão nova por cima. IDs do AdMob não são segredos (vão dentro do app publicado), então podem ficar no repositório.

Conta/app no AdMob: "Cachorro Caramelo Clicker".

## App ID

`ca-app-pub-4380393767380236~6038175130`

Já está configurado em *Assets > Google Mobile Ads > Settings* (`Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset`), e o SDK o coloca no manifest do Android como `com.google.android.gms.ads.APPLICATION_ID`. O SDK deve trazer também a permissão `AD_ID` para o manifest, o que combina com a declaração do ID de publicidade no Play Console (ainda não conferido num build Android).

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
