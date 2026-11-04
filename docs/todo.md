"Progresso da v1: o save do Unity é de outro formato, então quem está na versão 36 começa do zero. Falta decidir se isso é aceitável ou se vale um conversor": usamos no formato do unity, mas migramos o save de usuários antigos para garantir
"Login do Google Play Games e cloud save: você pediu para ver depois. O jogo antigo tinha os dois, e o novo não tem nenhum dos dois ainda.": temos que ter os dois. O save do jogador fica no firestore se eu não me engano. Devemos manter assim. Login com o googleplay, mas sem forçar o usuário a logar caso ele esteja offline. Pode usar sdk do unity ou qualquer facilitador que tiver para isso


"Anúncios: o app novo não tem AdMob. Os IDs estão em docs/admob.md, que continua sem commit. Antes de enviar, confira no Play Console a declaração Contém anúncios e o Data safety.": pode manter sem anuncios, mas faça o setup do sdk

Outras coisas:
Resetar progresso: deve resetar progresso local e na nuvem, caso usuário esteja logado e online. Senão reseta local e sincroniza depois. Veja quais são as regras do outro projeto quanto a isso

Deletar conta: mesma coisa do item anterior.

Analytics: usavamos antes e vamos continuar usando agora

Crashlytics: também

Leaderboard google play: também

Faça dessa a sua lista de tarefas. Execute uma por vez e garanta que nada vaze para a versão webgl. A versão webgl deve continuar funcionando normalmente sem nada disso.