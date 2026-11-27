# Guia de Contribuição e Boas Práticas (Unity + Git)

Este documento reúne os combinados da equipe para evitar conflitos de merge em cenas do Unity e garantir que apenas alterações reais sejam enviadas ao repositório.

---

## 1. Fluxo de Trabalho com Cenas (`.unity`)

Arquivos `.unity` são serializados em YAML com `fileID`s numéricos locais. Quando duas pessoas editam a mesma cena em branches diferentes, o Unity gera `fileID`s iguais para objetos distintos, causando conflitos difíceis e perigosos no Git.

- **Regra de ouro — 1 dono por cena:** Nunca duas pessoas editam `Assets/_Project/Scenes/Cena_principal.unity` simultaneamente. Caso precise alterar a cena principal, avise a equipe antes e só libere após o `push`.
- **Use cenas isoladas por feature:** Para desenvolver e testar novas mecânicas sem travar o restante do time, trabalhe em uma cena dedicada (ex.: `Assets/_Project/Scenes/Cena_Coletaveis_Inventario.unity`).
  - Se o Unity perguntar ao trocar de cena se deseja salvar alterações em `Cena_principal.unity` que não eram intencionais, selecione **Don't Save**.
- **Prefira Prefabs e Scripts:** Sempre que possível, encapsule sistemas (Inimigo, HUD, Inventário, Armadilhas, Coletáveis) em **Prefabs** (`Assets/_Project/Prefabs/`) e **Scripts** (`Assets/_Project/Scripts/`).
- **Integração final controlada:** Ao concluir as features na `main`, apenas um integrante abre `Cena_principal.unity`, monta os prefabs/ferramentas de cena (como os menus de `GeradorColetaveisAutomatico.cs`), testa em Play Mode e realiza o commit final.

---

## 2. Guia de Arquivos `.meta` (Alteração Real vs. Ruído do Unity)

No Unity, todo arquivo e pasta dentro de `Assets/` possui um arquivo `.meta` correspondente. Ele armazena:
1. O **`guid` único** que mantém todas as referências ligadas em cenas, prefabs e ScriptableObjects;
2. As **configurações de importação** definidas no Inspector (tipo de textura, animação, áudio, etc.).

### 2.1. Como inspecionar alterações em `.meta` antes de commitar

```powershell
# Ver resumo de todos os .meta modificados
git diff --stat -- "*.meta"

# Ver exatamente o que mudou no conteúdo (ignorando quebras de linha CRLF/LF)
git diff -w -- "*.meta"
```

Se `git diff -w -- "*.meta"` não exibir nenhuma saída, a modificação é apenas ruído de fim de linha (`LF` ↔ `CRLF`) e pode ser descartada com segurança.

### 2.2. Quando é modificação SEM intenção (Ruído — pode descartar)

Descarte quando notar:
- Dezenas de `.meta` de **pacotes de terceiros** modificados de uma só vez (ex.: pacotes de música, efeitos sonoros ou assets externos que você não editou);
- Mudança apenas de `serializedVersion`, ordem de campos vazios ou `externalObjects: {}` após reimportação automática do Editor ou abertura em subversão diferente do Unity;
- Diferença exclusiva de quebra de linha (`CRLF` / `LF`).

Para reverter ruído em uma pasta ou arquivo específico sem afetar seu trabalho:

```powershell
git checkout -- "Assets/Caminho/Da/PastaOuArquivo"
```

### 2.3. Quando é modificação REAL (DEVE commitar)

Commite sempre o `.meta` junto com o asset quando:
- **Criar um arquivo ou pasta nova (`??` / `A`):** Todo script, cena, prefab, material, sprite ou pasta criada precisa ir acompanhada de seu `.meta` no mesmo commit. Sem ele, o Unity gera um `guid` diferente na máquina dos colegas e quebra referências (`Missing Script` / `Missing Prefab`).
- **Alterar configurações no Inspector de Importação**, por exemplo:
  - **Textura / Ícone (`.png.meta`):** mudou `Texture Type` para `Sprite (2D and UI)` (`textureType: 8`), `Pixels Per Unit` ou compressão;
  - **Modelo 3D (`.fbx.meta`):** ajustou `Rig` (`Humanoid`/`Generic`), `Scale Factor`, `Loop Time` de animações ou `Generate Colliders`;
  - **Áudio (`.wav.meta`):** alterou deliberadamente `Load Type`, `Force To Mono` ou `Quality`;
- **Mover ou renomear um arquivo/pasta:** o `.meta` deve acompanhar a movimentação para preservar o `guid` original.

### 2.4. Regra de Ouro dos `.meta`
- **Nunca apague** o `.meta` de um asset existente nem **edite o campo `guid:`** manualmente (exceto ao duplicar intencionalmente um arquivo fora do Unity, garantindo um novo GUID único).
- Se o asset está em `Assets/_Project/` e você acabou de criá-lo ou configurá-lo no Inspector → **commite o `.meta`**.
- Se o arquivo pertence a um pacote externo que você nem abriu e o `git diff` mostra apenas `serializedVersion` ou `CRLF` → **descarte com `git checkout --`**.

---

## 3. Checklist Rápido Antes de Commitar

1. **Atualize sua branch com frequência** antes de iniciar novas tarefas:
   ```powershell
   git fetch origin
   git merge origin/main
   ```
2. **Revise o estado dos arquivos** antes de adicionar ao stage (evite `git add .` sem conferir):
   ```powershell
   git status --short
   ```
3. **Verifique os `.meta` modificados** com `git diff -w -- "*.meta"` e descarte ruídos de pacotes externos.
4. **Confirme que `Cena_principal.unity` não entrou por acidente** caso você não seja o responsável pela edição atual da cena principal:
   ```powershell
   git checkout -- Assets/_Project/Scenes/Cena_principal.unity
   ```
5. **Mantenha Pull Requests pequenos e focados** para facilitar revisão e integração na `main`.
