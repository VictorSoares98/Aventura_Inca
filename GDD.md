# Documento de Design de Jogo (GDD) — O Tesouro Perdido das Terras Secas

> **Subtítulo / Versão:** Versão Arqueólogo Metamorfo  
> **Título Alternativo:** *O Segredo das Areias*  
> **Autores:** Luan Victor das Chagas Soares, Carlos Nathan, Rafael de Souza do Carmo

---

## Ficha Técnica

| Atributo | Especificação |
| :--- | :--- |
| **Nome do Jogo** | O Tesouro Perdido das Terras Secas (*O Segredo das Areias*) |
| **Versão do Documento** | Versão Arqueólogo Metamorfo |
| **Gênero** | Aventura, Exploração Arqueológica, Furtividade e Ficção Científica Oculta (3D em terceira pessoa) |
| **Engine** | Unity `6000.3.23f1` |
| **Pipeline Gráfico** | Universal Render Pipeline (URP `17.3.0`) |
| **Principais Pacotes** | Cinemachine (`3.1.7`), AI Navigation (`2.0.16`), Input System (`1.20.0`), uGUI (`2.0.0`) |
| **Cena Principal do Protótipo** | `Assets/_Project/Scenes/Cena_principal.unity` |

---

## 1. Visão Geral e Nome do Jogo

- **Título Principal:** *O Tesouro Perdido das Terras Secas*
- **Título Alternativo:** *O Segredo das Areias*
- **Conceito Central:** Uma aventura em terceira pessoa que combina o imaginário clássico de expedições arqueológicas em regiões áridas com um mistério oculto de ficção científica. O jogador controla um alienígena metamorfo disfarçado de arqueólogo clássico em busca de artefatos valiosos e do núcleo de energia de uma nave ancestral soterrada nas dunas.

---

## 2. Tema, Cenário e Atmosfera

### Tema e Cenário
O jogo abandona os ambientes aquáticos tradicionais e transporta o jogador para uma vasta região árida, composta por:
- Desfiladeiros esculpidos pelo vento;
- Cânions profundos;
- Antigas catacumbas soterradas na terra seca.

O grande mistério da narrativa gira em torno de uma **civilização perdida** e dos destroços de uma **antiga nave espacial acidentada há séculos nas dunas** — conhecida e tratada pelos nativos e historiadores primitivos como um **"navio dos deuses"** caído na terra.

### Atmosfera
Mistério arqueológico clássico misturado com ficção científica oculta, marcado por poeira constante, calor sufocante, ruínas enigmáticas e perigos ancestrais.

---

## 3. Personagem Principal

O protagonista é um **alienígena metamorfo** infiltrado na Terra. Para camuflar sua verdadeira identidade extraterrestre e transitar livremente por sítios arqueológicos e acampamentos humanos, ele assumiu a forma física permanente de um **arqueólogo corajoso ao estilo clássico (estilo Indiana Jones)**, equipado com:
- Chapéu de aba larga;
- Jaqueta de couro desgastada;
- Ferramentas de escavação;
- Um chicote adaptado.

### Habilidade Distintiva
Utilizando sua capacidade de **mudança de forma molecular**, o protagonista pode mimetizar traços humanos para:
- Se disfarçar de rivais específicos;
- Enganar autoridades locais;
- Acessar zonas restritas e caminhos bloqueados sem levantar suspeitas.

---

## 4. Objetivo do Jogador

Explorar as profundezas das ruínas e os destroços da nave soterrada para:
1. Recuperar artefatos históricos de alto valor e relíquias raras;
2. Resgatar a tecnologia ultra-secreta escondida no coração da estrutura — o **núcleo de energia alienígena**;
3. Impedir que facções rivais ou caçadores de relíquias corruptos encontrem o núcleo antes e coloquem o segredo de sua espécie em risco.

---

## 5. Obstáculos e Desafios

Os desafios do jogo dividem-se em três pilares principais:

### 5.1. Ambiente — Perigos Naturais
- Calor extremo e risco de desidratação;
- Tempestades de areia impiedosas;
- Cavernas subterrâneas altamente instáveis, com risco iminente de desmoronamento.

### 5.2. Ameaças — Fauna e Rivais
- Animais peçonhentos do deserto (como serpentes e escorpiões);
- Armadilhas ancestrais mecânicas espalhadas pelas ruínas;
- Mercenários humanos à espreita em busca dos mesmos tesouros.

### 5.3. Complexidade — Mecânica e Destroços
- Destroços metálicos emperrados por séculos de acúmulo de areia;
- Portas trancadas por enigmas lógicos;
- Necessidade constante de gerenciar o uso da metamorfose para evitar esgotamento energético ou exposições acidentais da forma alienígena.

---

## 6. Mecânicas e Ações do Jogador (*O Que o Jogador Poderá Fazer*)

De acordo com a especificação conceitual, a experiência contempla sete frentes de interação:
1. **Locomoção e Exploração:** Explorar o deserto implacável, cânions profundos e ruínas subterrâneas a pé ou utilizando veículos de época adaptados.
2. **Escavação e Travessia:** Escavar locais soterrados, escalar paredões rochosos e utilizar ferramentas de precisão arqueológica.
3. **Investigação e Enigmas:** Decifrar hieróglifos, inscrições antigas e pistas deixadas por civilizações extintas.
4. **Coleta e Progressão:** Coletar tesouros valiosos, relíquias raras e artefatos de valor inestimável.
5. **Metamorfose Estratégica:** Utilizar a habilidade de metamorfose para se disfarçar de rivais específicos ou abrir caminhos bloqueados.
6. **Furtividade e Agilidade:** Evitar armadilhas mortais e criaturas hostis através de furtividade e agilidade.
7. **Infiltração Final:** Infiltrar-se no núcleo dos destroços para recuperar a tecnologia secreta de sua raça.

---

## 7. Estado Atual do Protótipo vs. Roadmap

A tabela e as listas abaixo documentam o que já está implementado na base técnica atual em `Assets/_Project/` em comparação ao escopo completo previsto no GDD.

### 7.1. Implementado no Protótipo Unity
- **Movimentação em Terceira Pessoa (`ControleJogador.cs`):** Caminhada, corrida para frente com `Left Shift`, pulo com gravidade, rotação alinhada à câmera, amortecimento de parâmetros no `Animator`, som de passos e empurrão físico de objetos com `Rigidbody`.
- **Sistema de Vida e Morte (`VidaPersonagem.cs`):** Controle de vida máxima (`100` por padrão), recebimento de dano, cura, barra de vida no HUD (`Image.fillAmount`) e desativação do controle ao morrer.
- **Armadilhas (`Armadilha.cs`):** Gatilhos que aplicam dano ao jogador (padrão `20`) com opção de autodestruição ao contato.
- **Inimigo Perseguidor (`InimigoPerseguidor.cs`):** Perseguição via `NavMeshAgent` (com fallback direcional caso não haja NavMesh assado na área), transição entre caminhada e corrida conforme distância, animação de ataque e golpe letal por proximidade ou colisão/trigger.
- **Inventário, Itens e Coletáveis (`Inventario.cs`, `ItemDados.cs`, `ItemColetavel.cs`):**
  - Inventário com capacidade padrão de `20` slots e suporte a itens empilháveis ou únicos (`ScriptableObject` via `ItemDados`);
  - Três tipos de item (`Moeda`, `Reliquia`, `Consumivel`), com os assets `Moeda_Ouro.asset`, `Pocao_Vida.asset` e `Reliquia_Inca.asset` em `Assets/_Project/Data/Itens/`;
  - Coleta automática (para moedas no modo `PorTipoDoItem`) ou interativa via tecla `E`, com animação de rotação/flutuação e ajuste automático de altura ao terreno.
- **HUD, UI do Inventário e Objetivo (`HUDInventario.cs`, `InventarioUI.cs`, `SlotUI.cs`, `ObjetivoReliquias.cs`):**
  - Contadores de moedas e progresso de relíquias (meta padrão de `5` relíquias com evento `UnityEvent` ao concluir);
  - Aviso contextual de interação, notificação temporária de coleta e painel de inventário abrindo/fechando com `I` (ou `Esc` para fechar) com exibição de nome, descrição e consumo de poções de vida.
- **Ferramenta de Editor (`GeradorColetaveisAutomatico.cs`):** Utilitário para geração automática de coletáveis na cena.

### 7.2. Planejado (Roadmap do GDD)
- Sistema de **metamorfose molecular** com barra/recurso de energia, disfarces de rivais e estado de suspeita/exposição acidental;
- **Veículos de época adaptados** para travessia do deserto e cânions;
- Mecânicas de **escavação, escalada de paredões e uso do chicote adaptado**;
- Sistema de **decifração de hieróglifos**, inscrições antigas e portas com enigmas lógicos;
- **Sobrevivência ambiental** (calor extremo, hidratação, tempestades de areia e desmoronamentos em cavernas);
- **Fauna peçonhenta** (serpentes e escorpiões) e **mercenários humanos** com comportamento de patrulha/vigilância;
- Fase final nos **destroços da nave espacial ("navio dos deuses")** e resgate do **núcleo de energia alienígena**.

---

## 8. Controles do Protótipo

| Entrada | Ação | Script Responsável |
| :--- | :--- | :--- |
| `W` / `A` / `S` / `D` ou Setas | Mover personagem | `ControleJogador.cs` |
| `Mouse` | Controlar direção da câmera | Câmera / Cinemachine |
| `Left Shift` (segurar + mover para frente) | Correr | `ControleJogador.cs` |
| `Espaço` | Pular | `ControleJogador.cs` |
| `E` | Coletar relíquias e consumíveis no alcance | `ItemColetavel.cs` |
| `I` | Abrir / fechar painel do inventário | `InventarioUI.cs` |
| `Esc` | Fechar painel do inventário (quando aberto) | `InventarioUI.cs` |
| Clique no slot do inventário | Visualizar detalhes / usar consumível (`Pocao_Vida`) | `InventarioUI.cs` / `SlotUI.cs` |
