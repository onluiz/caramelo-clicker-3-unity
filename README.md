# Caramel Dog Clicker (Unity)

Clicker feito em Unity 6 (6000.3.27f1). Publicado como WebGL e como app Android (`com.cachorrocaramelo.clicker`).

## Assinatura do Android (Google Play)

O keystore de upload fica em `Keystore/carameloclicker-upload.jks` (alias `carameloclicker`). A pasta `Keystore/` e os arquivos `*.jks`/`*.keystore` estão no `.gitignore`: **nunca** versione o keystore nem as senhas. Guarde uma cópia do arquivo e das senhas em um local seguro.

As senhas não ficam no projeto. O script `Assets/Editor/AndroidSigning.cs` as lê de variáveis de ambiente ao abrir o projeto e antes de cada build:

| Variável | Conteúdo |
| --- | --- |
| `CARAMELO_KEYSTORE_PASS` | senha do keystore (arquivo `.jks`) |
| `CARAMELO_KEYSTORE_ALIAS_PASS` | senha da chave (alias) |

Para criar as variáveis no Windows, sem precisar de administrador (use aspas simples se a senha tiver símbolos):

```powershell
[Environment]::SetEnvironmentVariable("CARAMELO_KEYSTORE_PASS", 'sua-senha', "User")
[Environment]::SetEnvironmentVariable("CARAMELO_KEYSTORE_ALIAS_PASS", 'sua-senha', "User")
```

Feche e reabra o Unity depois de criar as variáveis. Sem elas, o Unity pede as senhas em *Project Settings > Player > Android > Publishing Settings*.

## Gerando o build para o Google Play

1. Aumente o **Bundle Version Code** em *Project Settings > Player > Android > Other Settings*. O Play só aceita um número maior que o último enviado.
2. Em *Build Profiles > Android*, marque **Build App Bundle (Google Play)** e gere o `.aab` em `Build/Android/` (pasta ignorada pelo Git).
3. Suba o `.aab` no Play Console numa faixa de teste e, depois de validar, promova para produção. Envie também o `.symbols.zip` do mesmo build para ter crashes nativos legíveis.
4. Volte o build target ativo para WebGL.
