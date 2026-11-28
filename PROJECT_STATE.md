# PROJECT_STATE — O Tesouro Perdido das Terras Secas (`Aventura_Inca`)

> **Propósito:** Fonte única de verdade técnica e arquitetural do repositório para alinhamento da equipe e de ferramentas de desenvolvimento.  
> **Escopo:** Entrega P2 — *Versão Arqueólogo Metamorfo*  
> **Documentação Complementar:** [GDD.md](./GDD.md) | [README.md](./README.md) | [CONTRIBUTING.md](./CONTRIBUTING.md)

---

## 1. Tech Stack Oficial

| Componente | Versão / Configuração Real | Referência no Repositório |
| :--- | :--- | :--- |
| **Unity Editor** | `6000.3.23f1` (`09d2ecc7fb28`) | `ProjectSettings/ProjectVersion.txt` |
| **Render Pipeline** | Universal Render Pipeline (URP `17.3.0`) + Post-Processing `3.5.4` | `Packages/manifest.json` |
| **Câmera** | Cinemachine `3.1.7` (`CinemachineBrain`, `CinemachineCamera`, `CinemachineOrbitalFollow`, `CinemachineInputAxisController`) | `Packages/manifest.json` |
| **Navegação / IA** | AI Navigation `2.0.16` (`NavMeshAgent` + `NavMeshSurface`) | `Packages/manifest.json` |
| **Interface (UI)** | uGUI `2.0.0` + TextMeshPro | `Packages/manifest.json` |
| **Sistema de Input** | **`Both` (`activeInputHandler: 2`)**: Legacy Input (`Input.GetAxisRaw`, `Input.GetButtonDown`, `Input.GetKey`/`GetKeyDown`) nos scripts de gameplay + New Input System (`1.20.0` / `InputAction`) no `CinemachineInputAxisController` e suporte futuro a controles *touch* Android | `ProjectSettings/ProjectSettings.asset`, `ControleJogador.cs`, `InventarioUI.cs` |
| **Outros Pacotes Ativos** | Timeline `1.8.13`, Entities `1.4.8`, Visual Scripting `1.9.12`, Test Framework `1.6.0` | `Packages/manifest.json` |
| **Scripting Define Symbols** | `SORTIFY`, `UNITY_POST_PROCESSING_STACK_V2` | `ProjectSettings/ProjectSettings.asset` |
| **Raiz Oficial de Assets** | `Assets/_Project/` (`Animations`, `Audio`, `Data`, `Materials`, `Models`, `Prefabs`, `Scenes`, `Scripts`, `Settings`, `Textures`) | `Assets/_Project/` |

---

## 2. Arquitetura e ADRs (*Architecture Decision Records*)

### ADR-01 — Física do Player (`CharacterController` + `Rigidbody` Cinemático)
- **Decisão:** O movimento, pulo e gravidade do protagonista são controlados **exclusivamente** pelo `CharacterController` em `ControleJogador.cs` (`character.Move`).
- **Motivo e Uso do `Rigidbody`:** Para garantir disparo confiável de `OnTriggerEnter` com armadilhas, coletáveis e inimigos sem conflito de física, o `Rigidbody` do player deve permanecer **estritamente cinemático** (`isKinematic = true`, `useGravity = false`, `constraints = 112` / Freeze Rotation). Interações com corpos dinâmicos do cenário ocorrem via `OnControllerColliderHit` aplicando `ForceMode.Impulse`.
- **Proibição:** Nunca habilitar física dinâmica (`isKinematic = false`) no mesmo `GameObject` que possui `CharacterController`.

### ADR-02 — Animação (Mixamo Humanoid + Blend Tree `Locomocao` / Inimigo Generic)
- **Protagonista (`Ch18_nonPBR` — Mixamo Rig Humanoid):** Controlado pelo `Assets/_Project/Animations/JogadorAnimator.controller`, tendo o Blend Tree 2D **`Locomocao`** (`inputX`, `inputY`) como **hub central** para Idle (`0,0`) e caminhada em 4 direções (`HumanM@Walk01_* [RM]`), com transições para **`Corrida`** (`Correndo == true`) e **`Jump`** (`NoChao == false`).
- **Sincronização de Passos:** Os clipes de caminhada e corrida disparam o `AnimationEvent` **`Passos(AnimationEvent evt)`**; o código filtra clipes secundários do Blend Tree (`peso >= 0.35`), aplica *cooldown* anti-duplicação (`0.26s` andando / `0.16s` correndo) e detecta aterrissagem após queda (`velocidadeYAnterior <= -3.0f`).
- **Inimigo (`SciFi Beast06 Bull` — Rig Generic):** Usa `SciFi Beast06 Bull Skin1Avatar` (`avatarSetup: 1`) e `SciFi Beast06 Bull Animator Controller.controller`, acionado via `animator.CrossFadeInFixedTime(...)` com os nomes dos estados (`Idle1`, `WalkF`, `Run`, `Attack1`, `Attack2`, `Attack3`).

### ADR-03 — Desacoplamento Cross-Prefab (Observer + Auto-Resolução)
- **Decisão:** Os 3 prefabs centrais em `Assets/_Project/Prefabs/Sistema/` (`Prefab_Jogador`, `Prefab_HUD_Sistema`, `Prefab_Camera`) **não podem manter referências diretas de cena entre si**.
- **Implementação:**
  - `VidaPersonagem` emite eventos estáticos (`AoVidaMudar`, `AoJogadorMorrer`, `AoJogadorRenascer`) consumidos por `HUDVida.cs` e `HUDInventario.cs`;
  - `ItemColetavel` emite `AoEntrarAlcance` / `AoSairAlcance` para avisos no HUD;
  - `InventarioUI`, `HUDInventario`, `ObjetivoReliquias` e `VinculadorCameraJogador` auto-resolvem suas dependências uma única vez em `Awake`/`Start` (custo `O(1)` por frame no `Update`);
  - `HUDVida` desativa `EventSystem` duplicado caso outro já esteja ativo na cena.

### ADR-04 — Regra de Git para Equipe em Paralelo no Unity
- **Fluxo de Branches:** `develop` concentra a integração diária (PRs de `feat/*` → `develop`); `main` recebe apenas marcos estáveis e entregas finais (`develop` → `main`).
- **Isolamento de Cenas (`.unity`):** Arquivos `.unity` geram colisões de `fileID` YAML quando editados em paralelo.
  - **Proibido** editar simultaneamente as cenas oficiais (`00_MenuPrincipal` / `Cena_MenuPrincipal`, `01_Fase1` / `Cena_principal`, `02_Fase2`, `03_Fase3`); apenas **1 responsável** por vez altera uma cena oficial.
  - Desenvolvedores devem trabalhar em **Scripts desacoplados + Prefabs** e validar alterações em **cenas de teste individuais** (pasta recomendada `Assets/_Project/Scenes/Testes/`, atualmente usando `Cena_Coletaveis_Inventario.unity`).
- **Controle de `.meta` e Sortify:** Toda criação/alteração real de asset em `Assets/_Project/` exige seu `.meta` no commit; ruídos de pacotes externos (`git diff -w -- "*.meta"` vazio) devem ser descartados antes do commit (ver `CONTRIBUTING.md`).

---

## 3. Divisão de Branches (Escopo P2)

Todas as branches de tarefa derivam de `develop` e retornam via Pull Request para `develop`:

| Branch | Escopo Principal | Entregáveis-Chave | Status Atual |
| :--- | :--- | :--- | :--- |
| `feat/coletaveis-inventario` | Sistema de itens, coleta, inventário, HUD e objetivo | `ItemDados`, `ItemColetavel`, `Inventario`, `InventarioUI`, `HUDInventario`, `SlotUI`, `ObjetivoReliquias`, prefabs `Coletavel_*`, `GeradorColetaveisAutomatico` | Integrada (base de `develop`) |
| `feat/morte-vidas` | Vida, dano, inimigo, morte/respawn e prefabs modulares | `VidaPersonagem` (eventos + respawn `[R]`), `Armadilha`, `InimigoPerseguidor` + `SciFi Beast06 Bull` (malhas/avatar/29 animações), `HUDVida`, `VinculadorCameraJogador`, `Prefab_Jogador`/`Prefab_HUD_Sistema`/`Prefab_Camera`, `OrganizadorHierarquiaSortify` | Ativa / Pronta para PR → `develop` |
| `feat/audio` | Trilha, passos sincronizados e efeitos sonoros contextuais | Música de fundo (`1. Dunes of Silence.wav`), 8 variações `Footstep_Sand_01..08` sincronizadas + aterrissagem, 6 SFX em `Assets/_Project/Audio/SFX/` | Implementada em `feat/morte-vidas` (falta replicar música de fundo nas futuras fases) |
| `feat/menu-cenas-build` | Menu Principal, multi-cenas, transição, Android e builds finais | `MenuPrincipalController`, `Cena_MenuPrincipal`, `PortalProximaFase`, Fase 1/2/3 no `EditorBuildSettings`, controles *touch* Android e builds `.exe` + `.apk` | Pendente (próxima frente) |

---

## 4. Contratos do Projeto

### 4.1. Tags e Layers (`ProjectSettings/TagManager.asset`)
- **Tags customizadas cadastradas:** nenhuma (`tags: []`).
- **Tags nativas obrigatórias:**
  - `Player` — Obrigatória no `GameObject` raiz do jogador (`Prefab_Jogador` / `Ch18_nonPBR`); usada por `ItemColetavel`, `Armadilha`, `InimigoPerseguidor` e `VinculadorCameraJogador`.
  - `MainCamera` — Obrigatória na câmera principal com `CinemachineBrain` e `AudioListener`; usada por `Camera.main` em `ControleJogador`, `ItemColetavel`, `VidaPersonagem`, `Armadilha` e `InimigoPerseguidor`.
- **Layers registradas:**
  - `0`: `Default`
  - `1`: `TransparentFX`
  - `2`: `Ignore Raycast` (ignorada no raycast de altura em `ItemColetavel.AjustarAlturaAoTerreno`)
  - `4`: `Water`
  - `5`: `UI` (obrigatória em todos os elementos do Canvas/HUD)

### 4.2. Parâmetros dos Animator Controllers

**`Assets/_Project/Animations/JogadorAnimator.controller`** (4 parâmetros ativos):

| Parâmetro | Tipo | Default | Origem / Uso |
| :--- | :--- | :--- | :--- |
| `inputX` | `Float` | `0` | `ControleJogador.cs` — Eixo horizontal do Blend Tree 2D `Locomocao` |
| `inputY` | `Float` | `0` | `ControleJogador.cs` — Eixo vertical do Blend Tree 2D `Locomocao` |
| `NoChao` | `Bool` | `false` | `ControleJogador.cs` — Controla transição `Locomocao`/`Corrida` ↔ `Jump` |
| `Correndo` | `Bool` | `false` | `ControleJogador.cs` — Controla transição `Locomocao` ↔ `Corrida` |

**`Assets/SciFi Beast06 Bull/SciFi Beast06 Bull Animator Controller.controller`** (`0` parâmetros):
- Controlado diretamente por nome de estado via `CrossFadeInFixedTime`: `Idle1`, `WalkF`, `Run`, `Attack1`, `Attack2`, `Attack3`.

### 4.3. Convenção de Nomes e Estrutura de Pastas
- **Scripts (`Assets/_Project/Scripts/`):** `PascalCase` em português para domínio do projeto, organizados por módulo:
  - Raiz: `ControleJogador.cs`, `VidaPersonagem.cs`, `Armadilha.cs`, `InimigoPerseguidor.cs` *(Nota: `Health.cs` e `IDamageable.cs` são legados não conectados à cena; `New Material.mat` em `Scripts/` deve ser movido/limpo)*
  - `Camera/`: `VinculadorCameraJogador.cs`
  - `Editor/`: `GeradorColetaveisAutomatico.cs`, `GeradorPrefabsSistema.cs`, `OrganizadorHierarquiaSortify.cs`
  - `Inventario/` & `Inventario/UI/`: `Inventario.cs`, `ItemColetavel.cs`, `ItemDados.cs`, `ObjetivoReliquias.cs`, `HUDInventario.cs`, `InventarioUI.cs`, `SlotUI.cs`
  - `UI/`: `HUDVida.cs`
- **Prefabs (`Assets/_Project/Prefabs/`):**
  - `Sistema/`: prefixo `Prefab_` (`Prefab_Jogador.prefab`, `Prefab_HUD_Sistema.prefab`, `Prefab_Camera.prefab`)
  - `Coletaveis/`: prefixo `Coletavel_` (`Coletavel_Moeda.prefab`, `Coletavel_Pocao.prefab`, `Coletavel_Reliquia.prefab`)
  - `UI/`: `Slot_Item.prefab`
- **Áudio (`Assets/_Project/Audio/`):**
  - `SFX/`: prefixo `SFX_` (`SFX_Coleta_Moeda`, `SFX_Coleta_Reliquia`, `SFX_Coleta_Pocao`, `SFX_Dano_Impacto`, `SFX_Morte`, `SFX_Ataque_Inimigo`)
- **Cenas (`Assets/_Project/Scenes/`):**
  - Cenas oficiais de Build (ordem alvo): `00_MenuPrincipal` (ou `Cena_MenuPrincipal`), `01_Fase1` (`Cena_principal.unity`), `02_Fase2` (ou `Cena_Fase2_Ruinas`), `03_Fase3` (ou `Cena_Fase3_Nave`).
  - Cenas individuais de desenvolvimento/testes: `Cena_Coletaveis_Inventario.unity` (novas cenas de teste devem ficar em `Assets/_Project/Scenes/Testes/`).
- **Hierarquia Padrão (Sortify):** `0: CAMERA`, `1: LUZ`, `2: TERRENO`, `3: PLAYER`, `4: HUD`, `5: COLETAVEIS` (`_MOEDAS`, `_RELIQUIAS`, `_POCOES`), `6: INIMIGOS`, `7: ARMADILHAS`, `8: SISTEMAS`, `9: AUDIO`.

---

## 5. Fase de Maturidade Atual

### Pronto no Repositório Hoje
- **Documentação & Governança:** `GDD.md` completo com Framework MDA, Regras e Core Loop; `README.md` e `CONTRIBUTING.md` atualizados; fluxo `develop` → `main` ativo.
- **Core Gameplay & Movimento:** `ControleJogador` com Blend Tree 2D, corrida, pulo, empurrão físico, 8 sons de passos sincronizados via `AnimationEvent` e som de aterrissagem.
- **Sistemas de Sobrevivência e IA:** `VidaPersonagem` com barra no HUD (`HUDVida`), morte e respawn `[R]`; `Armadilha` com dano e SFX; `InimigoPerseguidor` com `NavMeshAgent`, carência inicial (`3.5s`), *windup* de ataque (`0.7s`) + SFX, `SciFi Beast06 Bull` com `Avatar`, malhas e `29/29` animações restauradas.
- **Inventário, Coletáveis & Áudio:** `20` slots, moedas/poções/relíquias com `SFX_Coleta_*`, música de fundo (`1. Dunes of Silence.wav`), objetivo de relíquias e HUD reativo em tempo real.
- **Modularidade e Ferramentas:** `Prefab_Jogador`, `Prefab_HUD_Sistema` e `Prefab_Camera` gerados sem referências cruzadas de cena, mais 3 utilitários em `Aventura Inca/` (`GeradorColetaveisAutomatico`, `OrganizadorHierarquiaSortify`, `GeradorPrefabsSistema`).

### Pendente para Fechar a Entrega P2
1. **Menu Principal + 3 Cenas Jogáveis + Transição (`feat/menu-cenas-build`):** Criar `MenuPrincipalController.cs`, cena de Menu Principal, `PortalProximaFase.cs`, estruturar/integrar Fase 1 (`Cena_principal`), Fase 2 e Fase 3 usando os 3 Prefabs de sistema e registrá-las em `ProjectSettings/EditorBuildSettings.asset`.
2. **Replicação de Áudio nas Fases Finais:** Garantir o `AudioSource` de música de fundo em loop em todas as fases jogáveis.
3. **Suporte Android (Controles Touch):** Condicionar `Cursor.lockState` a `!Application.isMobilePlatform` e criar `Canvas_ControlesMobile` (joystick virtual + botões Pular, Correr, Coletar, Inventário e Respawn + rotação de câmera por toque).
4. **Conferência de Assets de Aula (`*`) & Higiene:** Validar todos os pacotes exigidos pela disciplina e limpar sobras (`Scripts/New Material.mat`, `AC_TouroCibernetico.controller`, `_Recovery` se não utilizados).
5. **Builds & Entrega Final:** Gerar e validar `.exe` (Windows) e `.apk` (Android) funcionando, exportar `GDD.md` para PDF (se exigido) e promover `develop` → `main`.
