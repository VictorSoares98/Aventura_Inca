# O Tesouro Perdido das Terras Secas (*O Segredo das Areias*)

Protótipo em Unity 3D de aventura arqueológica com ficção científica oculta — **Versão Arqueólogo Metamorfo**.

> Documentação completa de design disponível no [Documento de Design de Jogo (GDD.md)](./GDD.md).

---

## Sobre o Jogo

**O Tesouro Perdido das Terras Secas** (também referido como *O Segredo das Areias*) transporta o jogador para uma vasta região árida repleta de desfiladeiros esculpidos pelo vento, cânions profundos e antigas catacumbas soterradas. O mistério central envolve uma civilização perdida e os destroços de uma antiga nave espacial acidentada há séculos nas dunas — tratada pelos nativos e historiadores primitivos como um *"navio dos deuses"*.

- **Protagonista:** Um alienígena metamorfo infiltrado na Terra que assumiu a forma permanente de um arqueólogo clássico (chapéu de aba larga, jaqueta de couro desgastada, ferramentas de escavação e chicote adaptado), usando sua mudança de forma molecular para se disfarçar de rivais, enganar autoridades e acessar zonas restritas.
- **Objetivo:** Explorar as profundezas das ruínas e dos destroços soterrados, recuperar artefatos históricos valiosos e resgatar o **núcleo de energia alienígena** antes que facções rivais ou caçadores de relíquias corruptos o encontrem e exponham o segredo de sua espécie.
- **Desafios Previstos no GDD:** Calor extremo, tempestades de areia, desidratação, cavernas instáveis com risco de desmoronamento, fauna peçonhenta (serpentes e escorpiões), armadilhas mecânicas ancestrais, mercenários humanos, enigmas lógicos e gerenciamento energético da metamorfose.

---

## Funcionalidades Implementadas no Protótipo

O repositório atual (`Aventura_Inca`) implementa a base jogável em terceira pessoa em Unity:

- **Movimentação e Física (`ControleJogador.cs`):** Caminhada, corrida, pulo com gravidade, rotação alinhada à câmera, animação suavizada no `Animator`, áudio de passos e interação física (empurrão) com objetos rígidos.
- **Vida e Dano (`VidaPersonagem.cs`, `Armadilha.cs`):** Barra de vida no HUD, sistema de dano por armadilhas em ruínas e estado de morte com bloqueio de controles.
- **Inimigo Perseguidor (`InimigoPerseguidor.cs`):** IA com `NavMeshAgent` (e fallback de movimentação direta), alternância entre caminhada e corrida por distância e ataque letal por proximidade ou colisão.
- **Sistema de Inventário e Coletáveis (`Inventario.cs`, `ItemDados.cs`, `ItemColetavel.cs`):**
  - Inventário com `20` slots, empilhamento configurável e três categorias (`Moeda`, `Reliquia`, `Consumivel`);
  - Itens configurados em `ScriptableObject`: `Moeda_Ouro`, `Pocao_Vida` (cura ao usar) e `Reliquia_Inca`;
  - Coleta automática de moedas ou interativa (`E`) para relíquias e poções, com ajuste automático de altura ao terreno.
- **HUD e Objetivo (`HUDInventario.cs`, `InventarioUI.cs`, `SlotUI.cs`, `ObjetivoReliquias.cs`):** Contadores de moedas e relíquias (meta padrão de `5` relíquias), notificações de coleta, aviso de interação e interface de inventário (`I`).
- **Ferramenta de Editor (`GeradorColetaveisAutomatico.cs`):** Script de apoio para geração automática de coletáveis no Editor do Unity.

---

## Controles

| Tecla / Entrada | Ação |
| :--- | :--- |
| `W` / `A` / `S` / `D` ou Setas | Movimentar personagem |
| `Mouse` | Girar câmera |
| `Left Shift` (segurar ao andar para frente) | Correr |
| `Espaço` | Pular |
| `E` | Coletar item interativo no alcance |
| `I` | Abrir / fechar inventário |
| `Esc` | Fechar inventário |

---

## Tecnologias e Requisitos

- **Unity:** `6000.3.23f1`
- **Render Pipeline:** Universal Render Pipeline (URP) `17.3.0`
- **Pacotes Principais:**
  - `com.unity.cinemachine` (`3.1.7`)
  - `com.unity.ai.navigation` (`2.0.16`)
  - `com.unity.inputsystem` (`1.20.0`)
  - `com.unity.ugui` (`2.0.0`)

---

## Estrutura do Projeto

```text
Assets/
└── _Project/
    ├── Animations/          # Controllers de animação (JogadorAnimator, AC_TouroCibernetico)
    ├── Audio/               # Áudio ambiente
    ├── Data/Itens/          # ScriptableObjects (Moeda_Ouro, Pocao_Vida, Reliquia_Inca)
    ├── Materials/           # Materiais dos coletáveis e cenário
    ├── Models/              # Terrenos e modelos
    ├── Prefabs/             # Prefabs de coletáveis e UI (Slot_Item)
    ├── Scenes/              # Cena principal (Cena_principal.unity)
    └── Scripts/             # Scripts de gameplay, IA, armadilhas, inventário, UI e Editor
```

---

## Como Executar

1. Abra o **Unity Hub** e adicione este diretório como um projeto existente.
2. Certifique-se de abrir o projeto com o **Unity `6000.3.23f1`**.
3. No painel **Project**, navegue até `Assets/_Project/Scenes/` e abra `Cena_principal.unity`.
4. Pressione **Play** no Editor do Unity para testar a cena principal.

---

## Autores

- **Luan Victor das Chagas Soares**
- **Carlos Nathan**
- **Rafael de Souza do Carmo**
